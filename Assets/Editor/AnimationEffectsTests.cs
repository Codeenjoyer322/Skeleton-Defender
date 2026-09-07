#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SkeletonDefender.Editor
{
    // Read-only art and geometry checks: never open, load, or save a player profile.
    public static class AnimationEffectsTests
    {
        private static int checks;
        private static readonly string[] EffectIds = {
            "circe-fx-Fireball", "circe-fx-Fire_Impact", "circe-fx-Hex_Pulse",
            "circe-fx-Lightning_Segment", "circe-fx-Lightning_Impact", "circe-fx-Sun_Segment", "circe-fx-Sun_Impact",
            "achilles-fx-divine_spear_projectile", "achilles-fx-ordinary_arrow_projectile", "achilles-fx-blue_heel_arrow_rise_burst",
            "enemy-fx-pirate_bullet", "enemy-fx-pirate_muzzle_flash", "enemy-fx-enemy_summon_ground",
            "enemy-fx-sarcophagus_rebirth", "enemy-fx-warlock_fizzle_smoke", "enemy-fx-ninja_dodge_accent",
            "enemy-fx-execution_contact", "enemy-fx-boxing_contact", "enemy-fx-enemy_lightning_targeted"
        };

        private static void Check(bool condition, string message)
        { checks++; if (!condition) throw new Exception("ANIMATION EFFECTS: " + message); }
        private static void Near(Vector2 actual, Vector2 expected, string message)
        { Check(Vector2.Distance(actual, expected) < .002f, message + ": " + actual + " vs " + expected); }
        private static object Call(string method, params object[] arguments)
        {
            var info = typeof(SkeletonGame).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static);
            Check(info != null, "Missing shared runtime helper " + method);
            return info.Invoke(null, arguments);
        }

        [MenuItem("Skeleton Defender/Check animation effects")]
        public static void Run()
        {
            checks = 0;
            BalanceData.Reload();
            CatalogAndFrames();
            BeamGeometry();
            TargetedProjectileGeometry();
            HeelBurstTiming();
            PaletteAndCrystalPixels();
            LightningShapeAndSpearVisibility();
            DirectionSelection();
            EnemyDirectionalReleaseSocket();
            StormTickContact();
            DeerDirectionalCoverage();
            StormScreenGeometry();
            var path = Path.Combine(Path.GetDirectoryName(Application.dataPath), "work", "animation-effects-results.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, "{\"status\":\"passed\",\"assertions\":" + checks +
                ",\"effects\":19,\"deerDirections\":8,\"playerSaveTouched\":false,\"interactivePlaythrough\":false}");
            Debug.Log("SKELETON_ANIMATION_EFFECTS_PASSED: " + checks + " checks; 19 effect clips, eight authored deer directions, exact beam endpoints and homing anchors, slowed heel burst timing.");
        }

        private static void CatalogAndFrames()
        {
            foreach (string id in EffectIds)
            {
                var clip = AnimationLibrary.GetById(id);
                Check(clip != null, "Missing final effect " + id);
                Check(clip.Texture != null, "Missing runtime PNG " + id);
                Check(clip.Texture.width == clip.SheetWidth && clip.Texture.height == clip.SheetHeight,
                    "Unity changed the atlas dimensions: " + id);
                Check(clip.Texture.filterMode == FilterMode.Point && clip.Texture.wrapMode == TextureWrapMode.Clamp,
                    "Texture is not pixel-safe: " + id);
                Check(clip.Texture.mipmapCount == 1, "Mipmaps blur the pixel effect: " + id);
                Check(clip.Width > 0 && clip.Height > 0 && clip.Duration > 0 && clip.Frames.Length > 0,
                    "Empty animation " + id);
                float start = 0;
                for (int i = 0; i < clip.Frames.Length; i++)
                {
                    var frame = clip.Frames[i];
                    Check(frame.DurationSeconds > 0 && frame.X >= 0 && frame.Y >= 0 &&
                        frame.X + frame.Width <= clip.SheetWidth && frame.Y + frame.Height <= clip.SheetHeight,
                        "Frame outside PNG " + id + "/" + i);
                    Check(clip.FrameAt(start + frame.DurationSeconds * .5f, false) == i,
                        "Incorrect authored timing " + id + "/" + i);
                    Rect uv = clip.UV(i);
                    Check(uv.y >= 0 && uv.yMax <= 1.00001f && Math.Abs(uv.height - (float)frame.Height / clip.SheetHeight) < .00001f,
                        "Incorrect bottom-origin UV " + id + "/" + i);
                    start += frame.DurationSeconds;
                }
                Check(Mathf.Abs(start - clip.Duration) < .0001f, "Timeline length mismatch " + id);
            }
            foreach (bool left in new[] { false, true })
            {
                var clip = AnimationLibrary.GetById("circe-circe_deer-run-" + (left ? "west" : "east"));
                Check(clip != null && clip.Texture != null && clip.Frames.Length == 16,
                    "Missing full deer gallop " + left);
                Check(clip.Loop && Mathf.Abs(clip.Duration - .8f) < .0001f,
                    "Deer cycle changed " + left);
                Near(clip.GroundPivot, new Vector2(left ? 79 : 80, 128), "Deer lost its mirrored road anchor");
            }
        }

        private static void BeamGeometry()
        {
            var offsets = new[] { new Vector2(0, -205), new Vector2(0, 180), new Vector2(151, 0),
                new Vector2(-180, 0), new Vector2(163, -92), new Vector2(-74, 122) };
            foreach (string id in new[] { "circe-fx-Lightning_Segment", "circe-fx-Sun_Segment", "achilles-fx-blue_heel_arrow_rise_burst" })
            {
                var clip = AnimationLibrary.GetById(id);
                foreach (Vector2 offset in offsets)
                {
                    Vector2 start = new Vector2(397, 288), end = start + offset;
                    Rect rect = (Rect)Call("AnimationBeamRect", clip, start, end, .75f);
                    Vector2 sourceAxis = clip.EndPoint - clip.Origin;
                    float angle = (Mathf.Atan2(offset.y, offset.x) - Mathf.Atan2(sourceAxis.y, sourceAxis.x)) * Mathf.Rad2Deg;
                    Matrix4x4 matrix = RectPixelTransform(clip, rect, start, angle);
                    Near(matrix.MultiplyPoint3x4(clip.Origin), start, id + " start detached");
                    Near(matrix.MultiplyPoint3x4(clip.EndPoint), end, id + " missed exact target");
                    Vector2 axis = (clip.EndPoint - clip.Origin).normalized;
                    Vector2 perpendicular = new Vector2(-axis.y, axis.x);
                    Vector2 across = matrix.MultiplyPoint3x4(clip.Origin + perpendicular);
                    Check(Mathf.Abs(Vector2.Distance(across, start) - .75f) < .001f,
                        "Beam width was stretched by path length " + id);
                }
            }
        }

        private static void TargetedProjectileGeometry()
        {
            var fire = AnimationLibrary.GetById("circe-fx-Fireball");
            Near(fire.Origin, new Vector2(32, 16), "Fireball must use its energy core");
            Near(AnimationLibrary.GetById("achilles-fx-ordinary_arrow_projectile").Origin,
                new Vector2(57, 12), "Arrow must use its leading tip");
            Near(AnimationLibrary.GetById("achilles-fx-divine_spear_projectile").Origin,
                new Vector2(61, 16), "Spear must use its leading tip");
            foreach (Vector2 delta in new[] { Vector2.left, Vector2.right, Vector2.up, Vector2.down, new Vector2(-1, 2) })
            {
                var visual = new ProjectileVisualState();
                Vector2 logicalStart = new Vector2(400, 300), socket = logicalStart + new Vector2(25, -47);
                Vector2 ground = logicalStart + delta * 100, hit = ground + new Vector2(0, -24);
                visual.Begin(logicalStart, socket, hit);
                visual.Advance(logicalStart, ground, ground, hit, true);
                Near(visual.Tip, hit, "Flight ended outside the hurt point");
                foreach (string id in new[] { "circe-fx-Fireball", "achilles-fx-ordinary_arrow_projectile", "enemy-fx-pirate_bullet" })
                {
                    var clip = AnimationLibrary.GetById(id);
                    Rect rect = (Rect)Call("EffectAnimationRect", clip, visual.Tip, .65f);
                    float angle = (float)Call("EffectAnimationAngle", clip, visual.Direction);
                    Matrix4x4 matrix = RectPixelTransform(clip, rect, visual.Tip, angle);
                    Near(matrix.MultiplyPoint3x4(clip.Origin), hit, id + " rotated anchor drifted from live target");
                    Vector2 ahead = matrix.MultiplyPoint3x4(clip.Origin + clip.RotationAxis.normalized);
                    Near((ahead - hit).normalized, visual.Direction, id + " sprite faces away from flight");
                }
            }
        }

        private static Matrix4x4 RectPixelTransform(AnimationClipData clip, Rect rect, Vector2 pivot, float angle)
            => Matrix4x4.Translate(pivot) * Matrix4x4.Rotate(Quaternion.Euler(0, 0, angle)) *
                Matrix4x4.Translate(-pivot) * Matrix4x4.Translate(rect.position) *
                Matrix4x4.Scale(new Vector3(rect.width / clip.Width, rect.height / clip.Height, 1));

        private static Texture2D ReadPng(string resource)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Check(ImageConversion.LoadImage(texture, File.ReadAllBytes(Path.Combine(Application.dataPath, "Resources", resource + ".png"))),
                "Cannot decode original PNG for independent pixel checks: " + resource);
            return texture;
        }

        private static void PaletteAndCrystalPixels()
        {
            foreach (bool impact in new[] { false, true })
            {
                var clip = AnimationLibrary.GetById(impact ? "circe-fx-Fire_Impact" : "circe-fx-Fireball");
                Texture2D runtime = (Texture2D)Call("CirceMagicTexture", impact);
                Check(runtime != null && runtime.width == clip.SheetWidth && runtime.height == clip.SheetHeight &&
                    runtime.filterMode == FilterMode.Point && runtime.mipmapCount == 1,
                    "Cyan runtime atlas is missing or imported incorrectly");
                Texture2D original = ReadPng(clip.ResourcePath);
                Texture2D cyan = ReadPng(impact ? "AnimationOverrides/Circe_Magic_Impact" : "AnimationOverrides/Circe_Magic_Ball");
                try
                {
                    Check(cyan.width == original.width && cyan.height == original.height, "Palette swap changed atlas dimensions");
                    var oldPixels = original.GetPixels32(); var newPixels = cyan.GetPixels32();
                    int opaque = 0;
                    for (int i = 0; i < oldPixels.Length; i++)
                    {
                        Check(oldPixels[i].a == newPixels[i].a, "Palette swap changed transparent geometry");
                        if (newPixels[i].a == 0) continue;
                        opaque++;
                        Check(newPixels[i].b >= newPixels[i].g && newPixels[i].g > newPixels[i].r,
                            "Circe's spell contains an orange or red pixel");
                    }
                    Check(opaque > 100, "Cyan effect became invisible");
                }
                finally { UnityEngine.Object.DestroyImmediate(original); UnityEngine.Object.DestroyImmediate(cyan); }
            }
            foreach (string action in new[] { "idle", "walk", "hurt", "staff_attack", "hex_cast", "deer_cast", "storm_cast" })
                foreach (Direction8 direction in Enum.GetValues(typeof(Direction8)))
                {
                    var clip = AnimationLibrary.GetExact("circe", action, direction);
                    if (clip == null) continue;
                    bool left = direction == Direction8.West || direction == Direction8.NorthWest || direction == Direction8.SouthWest;
                    Texture2D texture = ReadPng(clip.ResourcePath);
                    try
                    {
                        var actor = new HeroCombatState { Kind = HeroKind.Circe, Hp = 100, MaxHp = 100,
                            Position = new Vector2(537, 328), FacingLeft = left };
                        actor.Animation.FacingDirection = direction;
                        actor.Animation.Play(action, clip.Duration);
                        float cursor = 0;
                        for (int f = 0; f < clip.Frames.Length; f++)
                        {
                            var frame = clip.Frames[f];
                            float sample = cursor + frame.DurationSeconds * .5f;
                            actor.Animation.Advance(sample - actor.Animation.Age);
                            Vector2 point = ProjectileVisuals.CirceStaffPixel(clip, f);
                            Near(ProjectileVisuals.HeroSocket(actor, true), AnimatedActors.Point(clip, actor.Position, point,
                                AnimatedActors.HeroPixelScale), "Socket did not use the currently drawn " + action + " frame " + f);
                            int px = frame.X + Mathf.RoundToInt(point.x), py = texture.height - 1 - (frame.Y + Mathf.RoundToInt(point.y));
                            Color32 pixel = texture.GetPixel(px, py);
                            Check(pixel.a > 200 && pixel.b >= 190 && pixel.g >= 155,
                                "Staff origin is not on a visible cyan crystal: " + action + " frame " + f + " left=" + left);
                            cursor += frame.DurationSeconds;
                        }
                    }
                    finally { UnityEngine.Object.DestroyImmediate(texture); }
                }
        }

        private static void LightningShapeAndSpearVisibility()
        {
            foreach (Vector2 target in new[] { new Vector2(150, 190), new Vector2(570, 342), new Vector2(880, 540) })
            {
                var points = (Vector2[])Call("LightningStrikePath", target);
                Near(points[points.Length - 1], target, "Lightning doesn't terminate on enemy");
                Check(Mathf.Abs(points[0].x - target.x) < .001f, "Strike is not above its target");
                for (int i = 1; i < points.Length; i++)
                    Check(Vector2.Distance(points[i - 1], points[i]) < 50 && points[i].y > points[i - 1].y,
                        "Lightning strip stretched into a giant sheet");
            }
            Check((float)Call("LightningOpacity", .34f, .9f) == 0, "Lightning remains a continuous light wall");
            Vector2 origin = new Vector2(800, 470);
            float near = ProjectileVisuals.SpearFlightDuration(origin, origin + Vector2.left * 50, true);
            float far = ProjectileVisuals.SpearFlightDuration(origin, origin + Vector2.left * 800, true);
            Check(near >= .5f && far <= .9f && far > near, "Spear flight is too short to see or ignores distance");
        }

        private static void HeelBurstTiming()
        {
            var clip = AnimationLibrary.GetById("achilles-fx-blue_heel_arrow_rise_burst");
            float rate = BalanceData.Current.heroSystems.Skill(HeroKind.Achilles, 1).animationSpeed;
            Check(Mathf.Abs(rate - .7f) < .0001f, "Achilles E visual speed is not 70%");
            float rise = .65f / .7f * .38f, lifetime = .45f / .7f;
            float sample = (float)Call("HeelArrowSampleAge", rise, rise, lifetime);
            Check(clip.FrameAt(sample, false) == 6, "Ordinary arrows must begin at the first blue burst frame");
            sample = (float)Call("HeelArrowSampleAge", 0f, rise, lifetime);
            Check(clip.FrameAt(sample, false) == 0, "Blue arrow must begin at its release origin");
            sample = (float)Call("HeelArrowSampleAge", lifetime, rise, lifetime);
            Check(clip.FrameAt(sample, false) == clip.Frames.Length - 1, "Rise effect must finish on its clear frame");
            sample = (float)Call("HeelArrowSampleAge", rise - .02f, rise, lifetime);
            Check(clip.FrameAt(sample, false) < 6, "Blue arrow bursts before the slowed rain launches");
        }

        private static void DeerDirectionalCoverage()
        {
            var ids = new HashSet<string>();
            for (int index = 0; index < 8; index++)
            {
                Direction8 direction = (Direction8)index;
                var clip = AnimationLibrary.GetExact("circe", "deer_run", direction);
                Check(clip != null && clip.Texture != null && clip.Loop && clip.Frames.Length >= 4,
                    "Missing authored deer gallop " + direction);
                Check(!clip.FlipX && ids.Add(clip.Id), "Deer direction is mirrored or aliases another direction " + direction);
                Vector2 delta = new Vector2(Mathf.Cos(index * Mathf.PI / 4), Mathf.Sin(index * Mathf.PI / 4));
                Check((AnimationClipData)Call("DeerAnimationClip", delta, delta.x < 0) == clip,
                    "Road tangent selects the wrong deer body " + direction);
                Check(clip.Texture.width == clip.SheetWidth && clip.Texture.height == clip.SheetHeight && clip.Texture.filterMode == FilterMode.Point,
                    "Deer atlas was resized or filtered " + direction);
            }
        }

        private static void StormScreenGeometry()
        {
            foreach (Vector2 target in new[] { new Vector2(48,180), new Vector2(560,490), new Vector2(1070,750) })
                for (int strike = 1; strike <= 4; strike++)
                {
                    var points = (Vector2[])Call("StormScreenStrikePath", target, strike);
                    Check(points[0].y == 0, "Storm starts inside the map instead of at the top of the screen");
                    Near(points[points.Length - 1], target, "Storm misses its current target");
                    for (int i = 1; i < points.Length; i++)
                        Check(points[i].y > points[i - 1].y && Mathf.Abs(points[i].x - target.x) <= 18.01f,
                            "Storm becomes a horizontal beam or an oversized image curtain");
                }
            float start = (float)Call("StormScreenFlash", 0f);
            Check(start > 0 && start <= .04f && (float)Call("StormScreenFlash", .08f) < start,
                "Full-screen flash is too strong or does not fade");
            Check((float)Call("StormScreenFlash", -.01f) == 0 && (float)Call("StormScreenFlash", .16f) == 0,
                "Screen stays illuminated outside the short strike flash");
        }

        private static void DirectionSelection()
        {
            Vector2[] vectors = { Vector2.right, new Vector2(1,1), Vector2.up, new Vector2(-1,1),
                Vector2.left, new Vector2(-1,-1), Vector2.down, new Vector2(1,-1) };
            string[] names = { "east", "south-east", "south", "south-west", "west", "north-west", "north", "north-east" };
            var actor = new HeroCombatState { Kind = HeroKind.Circe, Hp = 100, MaxHp = 100,
                Position = new Vector2(400,300) };
            for (int i = 0; i < vectors.Length; i++)
            {
                var expected = (Direction8)i;
                for (int previous = 0; previous < 8; previous++)
                    Check(AnimatedActors.DirectionFor(vectors[i], (Direction8)previous) == expected,
                        "Wrong view for map-space direction " + names[i]);
                Check(AnimationLibrary.DirectionName(expected) == names[i], "Manifest direction mapping " + expected);
                Check(AnimatedActors.DirectionFor(Vector2.zero, expected) == expected, "Standing hero lost its last view");
                ProjectileVisuals.Face(actor, actor.Position + vectors[i] * 70);
                Check(actor.Animation.FacingDirection == expected, "Facing did not follow movement or attack target");
                actor.Animation.Play("walk", 1);
                var exact = AnimationLibrary.GetExact("circe", "walk", expected);
                var rendered = AnimatedActors.HeroClip(actor);
                Check(rendered == (exact ?? AnimationLibrary.Get("circe", "walk", actor.FacingLeft)),
                    "Renderer ignored the authored directional atlas");
            }
            Vector2 AtAngle(float degrees) => new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));
            Check(AnimatedActors.DirectionFor(AtAngle(24), Direction8.East) == Direction8.East,
                "Slight target movement flickers at sector boundary");
            Check(AnimatedActors.DirectionFor(AtAngle(29), Direction8.East) == Direction8.SouthEast,
                "Hysteresis prevents entering the diagonal view");
            Check(AnimatedActors.DirectionFor(AtAngle(21), Direction8.SouthEast) == Direction8.SouthEast &&
                AnimatedActors.DirectionFor(AtAngle(16), Direction8.SouthEast) == Direction8.East,
                "Directional dead band does not work in both directions");
            var north = new AnimationClipData { action = "walk", direction = "north", width = 128,
                events = new[] { new AnimationEventData { name = "release", hasPosition = true, position = new Vector2(55,25) } },
                sockets = new[] { new AnimationSocketData { name = "staffTip", frameIndex = 0, position = new Vector2(47,29) },
                    new AnimationSocketData { name = "staffTip", frameIndex = 1, position = new Vector2(48,30) } } };
            Near(ProjectileVisuals.CirceStaffPixel(north, 0), new Vector2(47,29), "North crystal used an east-view fallback");
            Near(ProjectileVisuals.CirceStaffPixel(north, 1), new Vector2(48,30), "North crystal ignored frame-specific socket");
        }

        private static void EnemyDirectionalReleaseSocket()
        {
            // This synthetic NE view deliberately differs from both legacy E/W and its own recovery frame.
            // The fixture exercises the production lookup without writing a catalog or player save.
            AnimationLibrary.Get("warlock", "cast_fail", false);
            var entries = (Dictionary<string, AnimationClipData>)typeof(AnimationLibrary)
                .GetField("ByAction", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            const string key = "warlock/cast_fail/north-east";
            bool existed = entries.TryGetValue(key, out var previous);
            var clip = new AnimationClipData { character = "warlock", action = "cast_fail", direction = "north-east",
                width = 160, height = 160, groundPivot = new Vector2(80,130), duration = .9f,
                frames = new[] { new AnimationFrameData { durationSeconds = .44f },
                    new AnimationFrameData { durationSeconds = .12f }, new AnimationFrameData { durationSeconds = .34f } },
                events = new[] { new AnimationEventData { name = "fizzle_release", frameIndex = 1, timeSeconds = .44f,
                    hasPosition = true, position = new Vector2(48,39), socketName = "crystal" } },
                sockets = new[] { new AnimationSocketData { name = "crystal", frameIndex = 1, position = new Vector2(43,36) },
                    new AnimationSocketData { name = "crystal", frameIndex = 2, position = new Vector2(116,71) } } };
            try
            {
                entries[key] = clip;
                var model = new GameModel(1, HeroKind.Circe, new EquipmentStats(), 827);
                Enemy enemy = model.CreateEnemy(SkeletonKind.Warlock, 190);
                enemy.Animation.FacingDirection = Direction8.NorthEast;
                enemy.Animation.Play("cast_fail", .9f); enemy.Animation.Advance(.85f);
                var effect = new EnemyEffect { Source = enemy, FacingLeft = false, Position = new Vector2(330,240) };
                Near((Vector2)Call("EnemyEffectSocket", effect, "cast_fail", "fizzle_release"),
                    effect.Position + (new Vector2(43,36) - clip.GroundPivot) * AnimatedActors.EnemyPixelScale(enemy),
                    "Enemy fizzle did not use its actual NE release-frame crystal");
                effect.UsesVisualAnchors = true; effect.Position = new Vector2(281,194);
                enemy.Animation.FacingDirection = Direction8.SouthWest;
                Near((Vector2)Call("EnemyEffectSocket", effect, "cast_fail", "fizzle_release"), effect.Position,
                    "A frozen enemy release point was transformed again after the enemy turned");
            }
            finally { if (existed) entries[key] = previous; else entries.Remove(key); }
        }

        private static void StormTickContact()
        {
            var model = new GameModel(1, HeroKind.Circe, new EquipmentStats(), 804);
            Enemy enemy = model.CreateEnemy(SkeletonKind.Normal, 150);
            enemy.Hp = enemy.MaxHp = 1000; model.Enemies.Add(enemy);
            Vector2 captured = ProjectileVisuals.EnemyHitPoint(enemy, model.Position(enemy.Distance));
            var effect = new AbilityEffect { Kind = "storm", Age = 1.07f, LastDamageTickAge = 1,
                LastDamageTickTargetIds = new[] { enemy.Id }, LastDamageTickPoints = new[] { captured } };
            enemy.Distance += 37;
            Near((Vector2)Call("StormVisualTarget", model, effect, 0),
                ProjectileVisuals.EnemyHitPoint(enemy, model.Position(enemy.Distance)), "Tick flash did not follow the moving enemy");
            enemy.Dead = true;
            Near((Vector2)Call("StormVisualTarget", model, effect, 0), captured,
                "Lethal tick lost its captured contact point after target death");
            model.Enemies.Clear();
            Near((Vector2)Call("StormVisualTarget", model, effect, 0), captured,
                "Lethal tick lost its captured contact point after removal");
            Check((float)Call("LightningOpacity", .07f, .34f) > .99f &&
                (float)Call("LightningOpacity", .34f, .34f) == 0,
                "Each damage tick must flash then fade before the next 0.75-second tick");
        }
    }
}
#endif
