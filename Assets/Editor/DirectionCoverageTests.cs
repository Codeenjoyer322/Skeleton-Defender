using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace SkeletonDefender.Editor
{
    // Final delivered-asset acceptance check. It deliberately fails on a partial pack.
    // No model, inventory, player profile, save, or network service is opened.
    public static class DirectionCoverageTests
    {
        [Serializable] private sealed class DirectionalCatalog
        { public int schemaVersion; public AnimationClipData[] clips; }
        [Serializable] private sealed class CheckedView
        {
            public string role, action, direction, clipId, texture, aliasOf, eventKind;
            public int frames, distinctFrames;
        }
        [Serializable] private sealed class CoverageReport
        {
            public string status, checkedUtc, directionalCatalogSha256;
            public int assertions, requiredViews, loadedTextures;
            public bool playerSaveTouched, interactiveVisualReview;
            public CheckedView[] checkedViews;
            public string[] failures;
        }
        private static readonly string[] EnemyRoles = {
            "normal", "ninja", "tutankhamun", "giant", "crawler", "pirate", "samurai", "sarcophagus",
            "trex", "knight", "warlock", "mad", "boxer", "boss", "sarcophagus_bare", "mini_mummy"
        };
        private static readonly string[] Directions = {
            "east", "south-east", "south", "south-west", "west", "north-west", "north", "north-east"
        };
        private static readonly string[,] SpecialActions = {
            { "pirate", "pistol_shot" }, { "warlock", "lightning_cast" }, { "warlock", "cast_fail" },
            { "tutankhamun", "summon" }, { "samurai", "execution" }, { "trex", "execution_bite" }
        };
        private static readonly List<string> failures = new List<string>();
        private static readonly List<CheckedView> checkedViews = new List<CheckedView>();
        private static readonly HashSet<string> textures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static int checks, required;

        private static bool Check(bool condition, string message)
        {
            checks++;
            if (!condition) failures.Add(message);
            return condition;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static string Normalize(string direction) => (direction ?? "").ToLowerInvariant().Replace('_', '-');
        private static string Key(string role, string action, string direction) => role + "/" + action + "/" + direction;

        // Keep independent reports even when one asset assertion fails. One editor
        // launch checks both the delivered pack and the combat/equipment regressions.
        public static void RunFinalPackage()
        {
            var errors = new List<string>();
            try { Debug.Log("SKELETON_BATTLEFIELD_LAYOUT_PASSED: " + BattlefieldLayoutTests.Validate()); }
            catch (Exception exception) { errors.Add("battlefield layout: " + exception.Message); Debug.LogException(exception); }
            try { Run(); }
            catch (Exception exception) { errors.Add("direction coverage: " + exception.Message); Debug.LogException(exception); }
            try { AnimationIntegrationTests.Run(); }
            catch (Exception exception) { errors.Add("gameplay integration: " + exception.Message); Debug.LogException(exception); }
            if (errors.Count > 0) throw new Exception("FINAL ANIMATION PACKAGE: " + string.Join("; ", errors));
            Debug.Log("SKELETON_FINAL_ANIMATION_PACKAGE_PASSED: direction coverage and all six gameplay suites.");
        }

        [MenuItem("Skeleton Defender/Check final eight-direction coverage")]
        public static void Run()
        {
            checks = required = 0; failures.Clear(); checkedViews.Clear(); textures.Clear();
            var text = Resources.Load<TextAsset>("AnimationPack/directional-catalog");
            Check(text != null, "Missing AnimationPack/directional-catalog; legacy E/W fallback is not a completed pack.");
            var catalog = text == null ? null : JsonUtility.FromJson<DirectionalCatalog>(text.text);
            Check(catalog != null && catalog.schemaVersion == 1 && catalog.clips != null, "Directional catalog schema or clips are missing.");
            var records = new Dictionary<string, AnimationClipData>(StringComparer.OrdinalIgnoreCase);
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (catalog?.clips != null)
                foreach (var clip in catalog.clips)
                {
                    if (!Check(clip != null && !string.IsNullOrWhiteSpace(clip.id), "Directional catalog contains an unnamed clip.")) continue;
                    Check(ids.Add(clip.id), "Duplicate directional clip ID: " + clip.id);
                    string key = Key(clip.character, clip.action, Normalize(clip.direction));
                    if (Check(!records.ContainsKey(key), "Duplicate directional role/action/view: " + key)) records.Add(key, clip);
                }

            CheckAction(records, "achilles", "walk");
            CheckAction(records, "achilles", "attack");
            CheckAction(records, "circe", "walk");
            CheckAction(records, "circe", "staff_attack");
            foreach (string role in EnemyRoles)
            {
                CheckAction(records, role, "walk");
                CheckAction(records, role, "attack");
            }
            // Hexed enemies remain immobile. Eight proper standing views are required,
            // but inventing frog walk/attack actions is outside the approved gameplay.
            CheckAction(records, "blue_frog", "idle");
            for (int i = 0; i < SpecialActions.GetLength(0); i++)
                CheckAction(records, SpecialActions[i, 0], SpecialActions[i, 1]);
            CheckAction(records, "circe", "deer_run");
            CheckAction(records, "tower_elf", "idle");
            CheckAction(records, "tower_elf", "attack");

            var report = new CoverageReport {
                status = failures.Count == 0 ? "passed" : "failed", checkedUtc = DateTime.UtcNow.ToString("o"),
                directionalCatalogSha256 = text == null ? "" : TextHash(text.text),
                assertions = checks, requiredViews = required, loadedTextures = textures.Count,
                playerSaveTouched = false, interactiveVisualReview = false,
                checkedViews = checkedViews.ToArray(), failures = failures.ToArray()
            };
            string output = Path.Combine(Path.GetDirectoryName(Application.dataPath), "work", "animation-direction-coverage-results.json");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(report, true), new System.Text.UTF8Encoding(false));
            if (failures.Count > 0)
                throw new Exception("DIRECTION COVERAGE FAILED: " + failures.Count + " issues. " +
                    string.Join(" | ", failures.GetRange(0, Math.Min(12, failures.Count))) + " Full report: " + output);
            Debug.Log("SKELETON_DIRECTION_COVERAGE_PASSED: " + required + " required views, " + textures.Count +
                " loaded native textures, " + checks + " assertions; no E/W fallback accepted. Player save untouched.");
        }

        private static string TextHash(string content)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content))).Replace("-", "").ToLowerInvariant();
        }

        private static void CheckAction(Dictionary<string, AnimationClipData> records, string role, string action)
        {
            var visualDirections = new Dictionary<string, string>(StringComparer.Ordinal);
            float commonDuration = -1, commonContact = -1;
            for (int d = 0; d < Directions.Length; d++)
            {
                required++;
                string direction = Directions[d], key = Key(role, action, direction);
                if (!Check(records.TryGetValue(key, out AnimationClipData declared), "Missing delivered view: " + key)) continue;
                AnimationClipData clip = AnimationLibrary.GetExact(role, action, (Direction8)d);
                if (!Check(clip != null && clip.Id == declared.Id, "Runtime exact lookup does not select the delivered view: " + key)) continue;
                if (!Check(clip.Character == role && clip.Action == action && Normalize(clip.Direction) == direction,
                    "View metadata does not match its required direction: " + key)) continue;
                Check(!clip.FlipX, "Mirrored runtime fallback is not an authored direction: " + key);
                if (!Check(clip.Frames != null && clip.Frames.Length > 0 && Finite(clip.Duration) && clip.Duration > 0,
                    "View has no usable animation: " + key)) continue;
                // Following a moving target may change the view during one committed
                // action. Its simulation clock and scheduled contact must still select
                // the same phase in every delivered direction.
                if (action != "idle")
                {
                    if (commonDuration < 0) { commonDuration = clip.Duration; commonContact = clip.ContactTime; }
                    else
                    {
                        Check(Mathf.Abs(commonDuration - clip.Duration) < .0002f,
                            "Changing view would change the action duration: " + key);
                        if (RequiredEventKind(action,role) != "none") Check(Mathf.Abs(commonContact - clip.ContactTime) < .0002f,
                            "Changing view would desynchronize the scheduled contact: " + key);
                    }
                }
                Texture2D texture = clip.Texture;
                if (!Check(texture != null, "Runtime texture cannot be loaded: " + key + " → " + clip.ResourcePath)) continue;
                string asset = AssetDatabase.GetAssetPath(texture);
                textures.Add(asset);
                bool nativeSize = texture.width == clip.SheetWidth && texture.height == clip.SheetHeight;
                Check(nativeSize, "Unity resized the atlas: " + key);
                Check(texture.filterMode == FilterMode.Point && texture.mipmapCount == 1 && texture.wrapMode == TextureWrapMode.Clamp,
                    "Imported view is blurred, mipmapped, or wrapping: " + key);
                // A tight union crop can remove transparent pixels below lifted feet.
                // The original registration remains fixed: Circe's south cast has
                // (64,100) - crop(35,28) = (29,72) on a 64x70 cropped canvas.
                // Requiring the anchor inside the crop would introduce visible jumps.
                Check(Finite(clip.GroundPivot.x) && Finite(clip.GroundPivot.y) &&
                    clip.GroundPivot.x >= -clip.Width && clip.GroundPivot.x <= clip.Width * 2 &&
                    clip.GroundPivot.y >= -clip.Height && clip.GroundPivot.y <= clip.Height * 2,
                    "Ground registration is non-finite or implausibly far from its cropped canvas: " + key);

                if (!string.IsNullOrEmpty(clip.AliasOf))
                {
                    var original = AnimationLibrary.GetById(clip.AliasOf);
                    bool sameActor = original != null && original.Character == role;
                    bool sharedBody = original != null && original.Character == "normal" && original.Action == action &&
                        (role == "sarcophagus_bare" || role == "mini_mummy");
                    Check(original != null && (sameActor || sharedBody) && Normalize(original.Direction) == direction && original.Texture == texture &&
                        original.Width == clip.Width && original.Height == clip.Height,
                        "Alias substitutes another direction/actor, changes canvas, or has no source: " + key);
                }
                bool framesValid = ValidateTimeline(clip, key, RequiredEventKind(action,role) != "none");
                var row = new CheckedView { role = role, action = action, direction = direction,
                    clipId = clip.Id, texture = asset, aliasOf = clip.AliasOf, frames = clip.Frames.Length, eventKind = RequiredEventKind(action,role) };
                checkedViews.Add(row);
                if (!framesValid || !nativeSize) continue;
                try
                {
                    // Read the source pixels independently of the runtime player. Merely
                    // assigning eight names to one static atlas is not eight real views.
                    string[] hashes = InspectSourcePixels(clip, asset, key);
                    if (hashes == null) continue;
                    row.distinctFrames = new HashSet<string>(hashes, StringComparer.Ordinal).Count;
                    if (action != "idle") Check(row.distinctFrames >= 2, "Walk/attack is a static picture: " + key);
                    string sequence = string.Join("/", hashes);
                    if (visualDirections.TryGetValue(sequence, out string prior))
                        Check(false, "Two directions use identical frame pixels: " + Key(role, action, prior) + " and " + key);
                    else visualDirections.Add(sequence, direction);
                }
                catch (Exception ex) { Check(false, "Cannot inspect source PNG for " + key + ": " + ex.Message); }
            }
        }

        private static string RequiredEventKind(string action, string role = "")
        {
            if (action == "walk" || action == "idle" || action == "deer_run") return "none";
            if (role == "tower_elf" && action == "attack") return "release";
            if (action == "staff_attack" || action == "pistol_shot" || action == "lightning_cast") return "release";
            if (action == "cast_fail") return "fizzle_release";
            if (action == "summon") return "summon";
            return "contact";
        }
        private static bool MatchesRequiredEvent(string action, string name, string role = "")
        {
            string required = RequiredEventKind(action,role);
            name = (name ?? "").ToLowerInvariant();
            if (required == "fizzle_release") return name.Contains("release") && (name.Contains("fizzle") || name.Contains("fail"));
            return required != "none" && name.Contains(required);
        }
        private static bool ValidateTimeline(AnimationClipData clip, string key, bool attack)
        {
            bool valid = true;
            string eventKind = RequiredEventKind(clip.Action,clip.Character);
            if(clip.Action == "deer_run") Check(clip.Loop && Mathf.Abs(clip.Duration-.8f)<.0002f,"Directional deer run must preserve its 0.8-second authored cycle: "+key);
            if(clip.Character == "tower_elf" && clip.Action == "attack") Check(!clip.Loop && Mathf.Abs(clip.Duration-.5f)<.0002f && Mathf.Abs(clip.ContactTime-.2f)<.0002f,
                "Elf preparation/release must remain 0.5/0.2 seconds: "+key);
            float[] starts = new float[clip.Frames.Length + 1];
            for (int i = 0; i < clip.Frames.Length; i++)
            {
                var frame = clip.Frames[i];
                if (frame == null) { Check(false, "Null frame in " + key); valid = false; continue; }
                valid &= Check(frame.Width > 0 && frame.Height > 0 && frame.X >= 0 && frame.Y >= 0 &&
                    frame.X + frame.Width <= clip.SheetWidth && frame.Y + frame.Height <= clip.SheetHeight &&
                    frame.sourceX >= 0 && frame.sourceY >= 0 && frame.sourceX + frame.Width <= clip.Width && frame.sourceY + frame.Height <= clip.Height &&
                    Finite(frame.DurationSeconds) && frame.DurationSeconds > 0, "Invalid atlas rectangle/timing: " + key + " frame " + i);
                starts[i + 1] = starts[i] + frame.DurationSeconds;
            }
            valid &= Check(Mathf.Abs(starts[starts.Length - 1] - clip.Duration) < .0002f, "Frame times do not sum to clip duration: " + key);
            bool hasCombatEvent = false;
            if (clip.Events != null)
                foreach (var cue in clip.Events)
                {
                    if (!Check(cue != null, "Null animation event: " + key)) continue;
                    bool inside = cue.FrameIndex >= 0 && cue.FrameIndex < clip.Frames.Length && Finite(cue.TimeSeconds) &&
                        cue.TimeSeconds >= starts[Mathf.Clamp(cue.FrameIndex, 0, starts.Length - 1)] - .00001f &&
                        cue.FrameIndex + 1 < starts.Length && cue.TimeSeconds < starts[cue.FrameIndex + 1] - .00001f;
                    Check(inside, "Event is outside its named frame: " + key + " / " + cue.Name);
                    bool combatEvent = MatchesRequiredEvent(clip.Action, cue.Name,clip.Character);
                    if (combatEvent && inside && cue.TimeSeconds > 0)
                    {
                        hasCombatEvent = true;
                        // The model consumes ContactTime for both types. Do not accept a
                        // stray earlier melee marker on a projectile or summon alias.
                        Check(Mathf.Abs(clip.ContactTime - cue.TimeSeconds) < .00001f,
                            "Model contact time selects a different event from " + eventKind + ": " + key);
                        if (eventKind != "contact")
                            Check(Mathf.Abs(clip.ReleaseTime - cue.TimeSeconds) < .00001f,
                                "Release hook does not select the actual emission event: " + key);
                    }
                    if (cue.HasPosition)
                        Check(Finite(cue.Position.x) && Finite(cue.Position.y) && cue.Position.x >= 0 && cue.Position.x < clip.Width &&
                            cue.Position.y >= 0 && cue.Position.y < clip.Height, "Release/contact point outside actor canvas: " + key);
                    if ((eventKind == "release" || clip.Action == "cast_fail") && combatEvent)
                        Check(cue.HasPosition, "Ranged cast has no actual emission point: " + key);
                    if(clip.Character == "tower_elf" && combatEvent && inside)
                    {
                        AnimationSocketData socket=Array.Find(clip.Sockets ?? Array.Empty<AnimationSocketData>(), s=>s.name=="bow_muzzle" && s.frameIndex==cue.FrameIndex);
                        Check(cue.Name=="release_projectile" && socket!=null && Vector2.Distance(socket.position,cue.Position)<.001f,
                            "Elf release event and explicit bow socket disagree: "+key);
                    }
                }
            if (attack) Check(hasCombatEvent, "Action has no valid nonzero " + eventKind + " event: " + key);
            return valid;
        }

        private static string[] InspectSourcePixels(AnimationClipData clip, string asset, string key)
        {
            if (!Check(!string.IsNullOrEmpty(asset) && File.Exists(asset), "Source PNG is missing: " + key)) return null;
            Texture2D decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!Check(ImageConversion.LoadImage(decoded, File.ReadAllBytes(asset), false), "Cannot decode source PNG: " + key)) return null;
                if (!Check(decoded.width == clip.SheetWidth && decoded.height == clip.SheetHeight, "Source dimensions disagree with manifest: " + key)) return null;
                Color32[] pixels = decoded.GetPixels32();
                var hashes = new string[clip.Frames.Length];
                using (SHA256 sha = SHA256.Create())
                    for (int i = 0; i < clip.Frames.Length; i++)
                    {
                        AnimationFrameData frame = clip.Frames[i];
                        byte[] canvas = new byte[clip.Width * clip.Height * 4];
                        int opaque = 0;
                        for (int y = 0; y < frame.Height; y++)
                            for (int x = 0; x < frame.Width; x++)
                            {
                                Color32 pixel = pixels[(decoded.height - 1 - frame.Y - y) * decoded.width + frame.X + x];
                                if (pixel.a == 0) continue;
                                int px = frame.sourceX + x, py = frame.sourceY + y;
                                if (px < 0 || py < 0 || px >= clip.Width || py >= clip.Height) continue;
                                int offset = (py * clip.Width + px) * 4;
                                canvas[offset] = pixel.r; canvas[offset + 1] = pixel.g; canvas[offset + 2] = pixel.b; canvas[offset + 3] = pixel.a; opaque++;
                            }
                        Check(opaque > 0, "Empty actor frame: " + key + " #" + i);
                        hashes[i] = BitConverter.ToString(sha.ComputeHash(canvas)).Replace("-", "");
                    }
                return hashes;
            }
            finally { UnityEngine.Object.DestroyImmediate(decoded); }
        }
    }
}
