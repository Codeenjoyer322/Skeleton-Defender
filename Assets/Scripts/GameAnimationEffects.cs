using UnityEngine;

namespace SkeletonDefender
{
    public sealed partial class SkeletonGame
    {
        private const string CirceFireballFx = "circe-fx-Fireball";
        private const string OrdinaryArrowFx = "achilles-fx-ordinary_arrow_projectile";
        private const string DivineSpearFx = "achilles-fx-divine_spear_projectile";
        private const string HeelArrowFx = "achilles-fx-blue_heel_arrow_rise_burst";
        private static Texture2D circeMagicBall, circeMagicImpact;

        private static Texture2D CirceMagicTexture(bool impact)
        {
            if (impact)
                return circeMagicImpact != null ? circeMagicImpact :
                    circeMagicImpact = Resources.Load<Texture2D>("AnimationOverrides/Circe_Magic_Impact");
            return circeMagicBall != null ? circeMagicBall :
                circeMagicBall = Resources.Load<Texture2D>("AnimationOverrides/Circe_Magic_Ball");
        }

        private static bool DrawCirceMagic(bool impact, Vector2 anchor, Vector2 direction, float age, float scale)
        {
            var clip = AnimationLibrary.GetById(impact ? "circe-fx-Fire_Impact" : CirceFireballFx);
            Texture2D texture = CirceMagicTexture(impact);
            if (clip == null || texture == null) return false;
            Matrix4x4 previous = GUI.matrix;
            Color previousColor = GUI.color;
            try
            {
                RotateGuiLocal(EffectAnimationAngle(clip, direction), anchor);
                GUI.color = Color.white;
                GUI.DrawTextureWithTexCoords(EffectAnimationRect(clip, anchor, scale), texture,
                    clip.UV(clip.FrameAt(age, !impact)), true);
            }
            finally { GUI.matrix = previous; GUI.color = previousColor; }
            return true;
        }

        // This transform maps the authored endpoints exactly onto live world points. The
        // narrow axis retains its pixel scale, while only the length of the beam stretches.
        // Source pixels have a top-left origin; AnimationClipData.UV handles the texture flip.
        private static bool DrawAnimationBeam(string id, Vector2 start, Vector2 end,
            float age, float widthScale, Color tint)
        {
            AnimationClipData clip = AnimationLibrary.GetById(id);
            if (clip == null || clip.Texture == null) return false;
            Vector2 source = clip.EndPoint - clip.Origin;
            Vector2 target = end - start;
            if (source.sqrMagnitude < .0001f || target.sqrMagnitude < .0001f) return true;
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            try
            {
                float angle = (Mathf.Atan2(target.y, target.x) - Mathf.Atan2(source.y, source.x)) * Mathf.Rad2Deg;
                RotateGuiLocal(angle, start);
                GUI.color = tint;
                GUI.DrawTextureWithTexCoords(AnimationBeamRect(clip, start, end, widthScale), clip.Texture,
                    clip.UV(clip.FrameAt(age, clip.Loop)), true);
            }
            finally { GUI.color = previousColor; GUI.matrix = previousMatrix; }
            return true;
        }

        private static Rect AnimationBeamRect(AnimationClipData clip, Vector2 start, Vector2 end, float widthScale)
        {
            Vector2 source = clip.EndPoint - clip.Origin;
            float lengthScale = Vector2.Distance(start, end) / Mathf.Max(.0001f, source.magnitude);
            // Every delivered beam is authored along one canvas axis, including the
            // upward blue arrow. Retain a local-space rect, then rotate its origin.
            Vector2 scale = Mathf.Abs(source.x) > Mathf.Abs(source.y)
                ? new Vector2(lengthScale, widthScale) : new Vector2(widthScale, lengthScale);
            return new Rect(start - Vector2.Scale(clip.Origin, scale),
                Vector2.Scale(new Vector2(clip.Width, clip.Height), scale));
        }

        private static Matrix4x4 AnimationBeamMatrix(AnimationClipData clip, Vector2 start,
            Vector2 end, float widthScale)
        {
            Vector2 source = clip.EndPoint - clip.Origin;
            Vector2 target = end - start;
            float fromAngle = Mathf.Atan2(source.y, source.x) * Mathf.Rad2Deg;
            float toAngle = Mathf.Atan2(target.y, target.x) * Mathf.Rad2Deg;
            return Matrix4x4.Translate(start) * Matrix4x4.Rotate(Quaternion.Euler(0, 0, toAngle)) *
                Matrix4x4.Scale(new Vector3(target.magnitude / Mathf.Max(.0001f, source.magnitude), widthScale, 1)) *
                Matrix4x4.Rotate(Quaternion.Euler(0, 0, -fromAngle)) * Matrix4x4.Translate(-clip.Origin);
        }

        private static float HeelArrowSampleAge(float age, float riseDuration, float lifetime)
        {
            const float burstTime = .37001f;
            return age < riseDuration ? age / Mathf.Max(.001f, riseDuration) * burstTime :
                burstTime + (age - riseDuration) / Mathf.Max(.001f, lifetime - riseDuration) * (.81f - burstTime);
        }

        private static float FullEffectAge(string id, float age, float lifetime)
        {
            AnimationClipData clip = AnimationLibrary.GetById(id);
            return clip == null ? age : Mathf.Clamp01(age / Mathf.Max(.001f, lifetime)) * clip.Duration;
        }

        private static AnimationClipData DeerAnimationClip(Vector2 motion, bool facingLeft)
        {
            Direction8 direction = AnimatedActors.DirectionFor(motion, facingLeft ? Direction8.West : Direction8.East);
            return AnimationLibrary.GetDirectional("circe", "deer_run", direction, facingLeft);
        }

        private static Vector2[] StormScreenStrikePath(Vector2 screenTarget, int strike)
        {
            const int segments = 12;
            var points = new Vector2[segments + 1];
            float phase = screenTarget.x * .037f + strike * 1.79f;
            points[0] = new Vector2(screenTarget.x + Mathf.Sin(phase) * 17, 0);
            for (int i = 1; i < segments; i++)
            {
                float t = i / (float)segments;
                // Jitter across the falling axis, rather than stretching a square FX atlas.
                float bend = Mathf.Sin(phase + i * 2.31f) * 18 * (1 - t);
                points[i] = new Vector2(screenTarget.x + bend, screenTarget.y * t);
            }
            points[segments] = screenTarget;
            return points;
        }

        private static float StormScreenFlash(float tickAge)
        {
            if (tickAge < 0 || tickAge >= .16f) return 0;
            return .035f * (1 - tickAge / .16f);
        }

        private void DrawStormScreenEffects()
        {
            float flash = 0;
            foreach (AbilityEffect effect in game.AbilityEffects)
            {
                if (effect.Kind != "storm" || effect.LastDamageTickAge < 0) continue;
                float age = effect.Age - effect.LastDamageTickAge;
                flash = Mathf.Max(flash, StormScreenFlash(age));
                if (age < 0 || age >= .34f || effect.LastDamageTickPoints == null) continue;
                float alpha = LightningOpacity(age, .34f);
                for (int targetIndex = 0; targetIndex < effect.LastDamageTickPoints.Length; targetIndex++)
                {
                    Vector2 target = StormVisualTarget(game, effect, targetIndex) + mapRect.position;
                    Vector2[] path = StormScreenStrikePath(target, effect.DamageTicksResolved);
                    for (int i = 1; i < path.Length; i++)
                    {
                        DrawLine(path[i - 1], path[i], 6, new Color(.38f, .78f, 1, alpha * .22f));
                        DrawLine(path[i - 1], path[i], 2, new Color(.84f, .96f, 1, alpha));
                    }
                    DrawEffectById("circe-fx-Lightning_Impact", target, Vector2.right,
                        FullEffectAge("circe-fx-Lightning_Impact", age, .34f), .6f, new Color(1, 1, 1, alpha));
                }
            }
            // One very soft flash for a strike, irrespective of its enemy count.
            if (flash > 0) Fill(new Rect(0, 0, 1440, 900), new Color(1, 1, 1, flash));
        }

        private static void DrawDeerContact(ProjectileImpact impact)
        {
            float t = Mathf.Clamp01(impact.Age / Mathf.Max(.001f, impact.Lifetime));
            float alpha = 1 - t;
            Vector2 forward = ProjectileVisuals.Direction(impact.Direction);
            Vector2 side = new Vector2(-forward.y, forward.x);
            DrawEffectById("enemy-fx-boxing_contact", impact.Position, forward,
                FullEffectAge("enemy-fx-boxing_contact", impact.Age, impact.Lifetime), .36f,
                new Color(.65f, 1, .91f, alpha));
            for (int i = -1; i <= 1; i++)
            {
                Vector2 ray = (forward * .65f + side * i).normalized;
                Vector2 tip = impact.Position + ray * (5 + 18 * t);
                DrawLine(tip - ray * (3 + 3 * alpha), tip, 2, new Color(.72f, 1, .87f, alpha));
            }
        }

        private bool TryDrawHeroAnimationProjectile(HeroProjectile projectile)
        {
            // A queued spell is invisible until its actual release event initializes flight.
            if (projectile.Delay > 0 || !projectile.Visual.Initialized) return true;
            if (projectile.Kind == ProjectileKind.Knife) return false;
            float scale = projectile.Kind == ProjectileKind.Fireball ? .65f : .52f;
            if (projectile.Source != null && projectile.Source.Kind == HeroKind.Circe)
                return DrawCirceMagic(false, projectile.Visual.Tip, projectile.Visual.Direction, projectile.Age, scale);
            return DrawEffectById(CirceFireballFx, projectile.Visual.Tip, projectile.Visual.Direction,
                projectile.Age, scale, Color.white);
        }

        private void DrawAnimatedEnemyProjectiles()
        {
            foreach (EnemyProjectile projectile in game.EnemyProjectiles)
            {
                if (!projectile.Visual.Initialized) continue;
                if (!DrawEffectById("enemy-fx-pirate_bullet", projectile.Visual.Tip,
                    projectile.Visual.Direction, projectile.Age, .9f, Color.white))
                    DrawLine(projectile.Visual.Tip - projectile.Visual.Direction * 8,
                        projectile.Visual.Tip, 4, Gold);
            }
        }

        private bool TryDrawEnemyAnimationEffect(EnemyEffect effect)
        {
            float lifetime = effect.Age + effect.Life;
            Vector2 direction = effect.FacingLeft ? Vector2.left : Vector2.right;
            float scale = effect.Source == null ? 1 : ProjectileVisuals.EnemyScale(effect.Source);
            string id;
            switch (effect.Kind)
            {
                case "muzzle":
                    id = "enemy-fx-pirate_muzzle_flash";
                    return DrawEffectById(id, effect.Position,
                        ProjectileVisuals.Direction(effect.Target - effect.Position, direction),
                        FullEffectAge(id, effect.Age, lifetime), .6f, Color.white);
                case "summon":
                    id = "enemy-fx-enemy_summon_ground";
                    return DrawEffectById(id, effect.Position, Vector2.right,
                        FullEffectAge(id, effect.Age, lifetime), .85f, Color.white);
                case "rebirth":
                    id = "enemy-fx-sarcophagus_rebirth";
                    return DrawEffectById(id, effect.Position, Vector2.right,
                        FullEffectAge(id, effect.Age, lifetime), .8f * scale, Color.white);
                case "smoke":
                    id = "enemy-fx-warlock_fizzle_smoke";
                    Vector2 crystal = EnemyEffectSocket(effect, "cast_fail", "fizzle_release");
                    return DrawEffectById(id, crystal, Vector2.right,
                        FullEffectAge(id, effect.Age, lifetime), .7f, Color.white);
                case "dodge":
                    id = "enemy-fx-ninja_dodge_accent";
                    return DrawEffectById(id, effect.Position, direction,
                        FullEffectAge(id, effect.Age, lifetime), .65f * scale, Color.white);
                case "execution":
                case "knockdown":
                    id = effect.Kind == "execution" ? "enemy-fx-execution_contact" : "enemy-fx-boxing_contact";
                    Vector2 contact = effect.UsesVisualAnchors ? effect.Target : effect.Target + new Vector2(0, -28);
                    return DrawEffectById(id, contact, Vector2.right,
                        FullEffectAge(id, effect.Age, lifetime), .8f, Color.white);
                case "lightning":
                    Vector2 hitPoint = effect.UsesVisualAnchors ? effect.Target : effect.Target + new Vector2(0, -28);
                    bool beam = DrawTargetedLightning(hitPoint, effect.Age, effect.Life + effect.Age, .65f);
                    DrawEffectById("circe-fx-Lightning_Impact", hitPoint, Vector2.right,
                        FullEffectAge("circe-fx-Lightning_Impact", effect.Age, lifetime), .7f, Color.white);
                    return beam;
                default: return false;
            }
        }

        private static Vector2[] LightningStrikePath(Vector2 hitPoint)
        {
            // Four short pieces preserve the sharp authored pixels. A single atlas strip
            // stretched over the full height looks like a thick curtain in crowded waves.
            float height = Mathf.Clamp(hitPoint.y - 12, 72, 145);
            float side = Mathf.RoundToInt(hitPoint.x * .1f + hitPoint.y * .07f) % 2 == 0 ? 1 : -1;
            return new[] { hitPoint + new Vector2(0, -height),
                hitPoint + new Vector2(5 * side, -height * .76f),
                hitPoint + new Vector2(-4 * side, -height * .52f),
                hitPoint + new Vector2(3 * side, -height * .25f), hitPoint };
        }

        private static float LightningOpacity(float age, float lifetime)
        {
            float visible = Mathf.Min(.34f, lifetime);
            return age < .09f ? 1 : Mathf.Clamp01((visible - age) / Mathf.Max(.001f, visible - .09f));
        }

        private static Vector2 StormVisualTarget(GameModel model, AbilityEffect effect, int index)
        {
            if (effect.LastDamageTickTargetIds != null && index < effect.LastDamageTickTargetIds.Length)
            {
                int id = effect.LastDamageTickTargetIds[index];
                Enemy enemy = model.Enemies.Find(candidate => candidate.Id == id && !candidate.Dead);
                if (enemy != null) return ProjectileVisuals.EnemyHitPoint(enemy, model.Position(enemy.Distance));
            }
            return effect.LastDamageTickPoints[index];
        }

        private static bool DrawTargetedLightning(Vector2 hitPoint, float age, float lifetime, float widthScale)
        {
            float opacity = LightningOpacity(age, lifetime);
            if (opacity <= 0) return true;
            var points = LightningStrikePath(hitPoint);
            bool drawn = false;
            for (int i = 1; i < points.Length; i++)
                drawn |= DrawAnimationBeam("circe-fx-Lightning_Segment", points[i - 1], points[i],
                    age + i * .05f, widthScale, new Color(1, 1, 1, opacity));
            return drawn;
        }

        private static Vector2 EnemyEffectSocket(EnemyEffect effect, string action, string eventName)
        {
            // The event position can already be a socket (e.g. a muzzle). A failed spell's
            // old event stores the ground point, so use the authored staff-crystal anchor.
            if (effect.UsesVisualAnchors) return effect.Position;
            if (effect.Source == null) return effect.Position + new Vector2(0, -35);
            return ProjectileVisuals.EnemySkillReleasePoint(effect.Source, effect.Position, action, eventName);
        }

        private bool TryDrawAnimatedShot(Shot shot)
        {
            Vector2 hitPoint = shot.UsesVisualAnchors ? shot.End : shot.End + new Vector2(0, -13);
            float lifetime = shot.Age + shot.Life;
            string id;
            switch (shot.Skill)
            {
                case "sun":
                    bool beam = DrawAnimationBeam("circe-fx-Sun_Segment",
                        hitPoint + new Vector2(0, -180), hitPoint, shot.Age, .8f, Color.white);
                    id = "circe-fx-Sun_Impact";
                    DrawEffectById(id, hitPoint, Vector2.right,
                        FullEffectAge(id, shot.Age, lifetime), .85f, Color.white);
                    return beam;
                case "frog":
                    id = "circe-fx-Hex_Pulse";
                    return DrawEffectById(id, hitPoint, Vector2.right,
                        FullEffectAge(id, shot.Age, lifetime), .9f, Color.white);
                case "decapitate":
                    id = "enemy-fx-execution_contact";
                    return DrawEffectById(id, hitPoint, Vector2.right,
                        FullEffectAge(id, shot.Age, lifetime), .65f, Color.white);
                case "sword":
                    id = "enemy-fx-boxing_contact";
                    return DrawEffectById(id, hitPoint, Vector2.right,
                        FullEffectAge(id, shot.Age, lifetime), .3f, Color.white);
                default:
                    if (string.IsNullOrEmpty(shot.Skill) && shot.Kind == TowerKind.Frost)
                    {
                        Vector2 source = shot.UsesVisualAnchors ? shot.Start : ProjectileVisuals.TowerSocket(shot.Start);
                        float opacity = Mathf.Clamp01(shot.Life / Mathf.Max(.001f, lifetime));
                        DrawLine(source, hitPoint, 2, new Color(.6f, .86f, .88f, opacity));
                        return true;
                    }
                    // Tower hits already have a single ProjectileImpact. Do not draw the
                    // earlier cosmetic Shot ring on top of the actual contact animation.
                    return string.IsNullOrEmpty(shot.Skill);
            }
        }
    }
}
