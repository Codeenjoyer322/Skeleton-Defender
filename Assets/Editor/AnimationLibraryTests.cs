using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SkeletonDefender.Editor
{
    // Artifact/runtime contract tests only. No player profiles or game saves.
    public static class AnimationLibraryTests
    {
        static int checks;
        static void Check(bool condition, string message)
        {
            checks++;
            if (!condition) throw new Exception("ANIMATION LIBRARY: " + message);
        }
        static bool Near(float a, float b) => Mathf.Abs(a - b) < .00001f;
        static bool Near(Vector2 a, Vector2 b) => Vector2.Distance(a, b) < .00001f;

        static bool Directional(AnimationClipData clip) => clip.Id.StartsWith("neongothic-", StringComparison.Ordinal);
        static AnimationClipData Legacy(string character, string action, string direction = "east")
        {
            foreach (var clip in AnimationLibrary.All)
                if (!Directional(clip) && clip.Character == character && clip.Action == action && clip.Direction == direction) return clip;
            return null;
        }

        [MenuItem("Skeleton Defender/Check animation library")]
        public static void Run()
        {
            checks = 0;
            Check(AnimationLibrary.IsAvailable, "Catalog missing");
            Check(AnimationLibrary.All.Count >= 233, "Delivered corpus is incomplete");
            var ids = new HashSet<string>();
            var textures = new HashSet<string>();
            long rgbaBytes = 0;
            var legacyTextures = new HashSet<string>();
            foreach (var clip in AnimationLibrary.All)
            {
                Check(ids.Add(clip.Id), "Duplicate clip id " + clip.Id);
                Check(AnimationLibrary.GetById(clip.Id) == clip, "Id lookup " + clip.Id);
                if (Directional(clip))
                {
                    bool exact = false;
                    foreach (Direction8 direction in Enum.GetValues(typeof(Direction8)))
                        if (AnimationLibrary.DirectionName(direction) == clip.Direction)
                            exact = AnimationLibrary.GetExact(clip.Character, clip.Action, direction) == clip;
                    Check(exact, "Exact authored direction lookup " + clip.Id);
                    Check(!clip.FlipX, "Directional art must not be a reflected fallback " + clip.Id);
                }
                else legacyTextures.Add(clip.ResourcePath);
                Check(clip.Id.IndexOf("demo", StringComparison.OrdinalIgnoreCase) < 0, "Baked demo imported " + clip.Id);
                Check(clip.Frames.Length > 0 && clip.Duration > 0, "Empty clip " + clip.Id);
                Check(clip.Texture != null, "Missing runtime atlas " + clip.ResourcePath);
                Check(clip.Texture.width == clip.SheetWidth && clip.Texture.height == clip.SheetHeight, "Unity resized an atlas " + clip.Id);
                Check(clip.Texture.filterMode == FilterMode.Point, "Bilinear pixel art " + clip.Id);
                Check(clip.Texture.mipmapCount == 1, "Mipmap memory/blur " + clip.Id);
                Check(clip.Texture.wrapMode == TextureWrapMode.Clamp, "Atlas wrap " + clip.Id);
                Check(clip.Texture.format == TextureFormat.RGBA32, "Compressed/changed alpha texture " + clip.Id);
                if (textures.Add(clip.ResourcePath))
                {
                    rgbaBytes += (long)clip.SheetWidth * clip.SheetHeight * 4;
                    var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip.Texture)) as TextureImporter;
                    Check(importer != null && !importer.isReadable, "Retained redundant CPU texture " + clip.Id);
                    Check(importer.npotScale == TextureImporterNPOTScale.None, "NPOT scaling " + clip.Id);
                    Check(importer.textureCompression == TextureImporterCompression.Uncompressed, "Compression setting " + clip.Id);
                    Check(importer.sRGBTexture && !importer.alphaIsTransparency, "Source pixel preservation flags " + clip.Id);
                    foreach (string platform in new[] { "Standalone", "WebGL" })
                    {
                        var settings = importer.GetPlatformTextureSettings(platform);
                        Check(settings.overridden && settings.format == TextureImporterFormat.RGBA32, platform + " platform format " + clip.Id);
                        Check(settings.maxTextureSize >= Mathf.Max(clip.SheetWidth, clip.SheetHeight), platform + " shrinking limit " + clip.Id);
                    }
                }
                float elapsed = 0;
                for (int i = 0; i < clip.Frames.Length; i++)
                {
                    var f = clip.Frames[i];
                    Check(f.Width > 0 && f.Height > 0 && f.DurationSeconds > 0, "Invalid frame " + clip.Id);
                    Check(clip.FrameAt(elapsed, false) == i, "Exact frame boundary " + clip.Id + " #" + i);
                    Check(clip.FrameAt(elapsed + f.DurationSeconds / 2, false) == i, "Frame midpoint " + clip.Id + " #" + i);
                    if (i > 0) Check(clip.FrameAt(elapsed - .00001f, false) == i - 1, "Pre-boundary frame " + clip.Id + " #" + i);
                    Rect uv = clip.UV(i);
                    float left = (clip.FlipX ? uv.x + uv.width : uv.x) * clip.SheetWidth;
                    float top = (1 - uv.y - uv.height) * clip.SheetHeight;
                    Check(Mathf.Abs(left - f.X) < .0001f && Mathf.Abs(top - f.Y) < .0001f, "UV origin " + clip.Id);
                    Check(Mathf.Abs(Mathf.Abs(uv.width) * clip.SheetWidth - f.Width) < .0001f &&
                          Mathf.Abs(uv.height * clip.SheetHeight - f.Height) < .0001f, "UV extent " + clip.Id);
                    elapsed += f.DurationSeconds;
                }
                Check(Near(elapsed, clip.Duration), "Timeline duration " + clip.Id);
                Check(clip.FrameAt(-10, false) == 0 && clip.FrameAt(float.NaN, false) == 0, "Invalid ages " + clip.Id);
                Check(clip.FrameAt(clip.Duration + 10, false) == clip.Frames.Length - 1, "Death/once final hold " + clip.Id);
                Check(clip.FrameAt(clip.Duration, true) == 0, "Loop wrap " + clip.Id);
                foreach (var e in clip.Events)
                {
                    Check(clip.FrameAt(e.TimeSeconds, false) == e.FrameIndex, "Event does not enter authored frame " + clip.Id + " " + e.Name);
                    Check(e.TimeSeconds >= 0 && e.TimeSeconds < clip.Duration, "Event outside timeline " + clip.Id);
                    if (e.HasPosition)
                        Check(e.Position.x >= 0 && e.Position.x < clip.Width && e.Position.y >= 0 && e.Position.y < clip.Height, "Socket outside canvas " + clip.Id);
                }
                if (!string.IsNullOrEmpty(clip.AliasOf))
                    Check(clip.Texture == AnimationLibrary.GetById(clip.AliasOf).Texture, "Alias duplicated atlas " + clip.Id);
                if (!Directional(clip) && clip.Direction == "east" && (clip.Category != "effects" || clip.Action == "deer_run"))
                {
                    var west = Legacy(clip.Character, clip.Action, "west");
                    Check(west != null, "Missing left action " + clip.Id);
                    Check(Near(clip.Duration, west.Duration) && clip.Frames.Length == west.Frames.Length, "Direction timing " + clip.Id);
                    Check(Near(west.GroundPivot, new Vector2(clip.Width - 1 - clip.GroundPivot.x, clip.GroundPivot.y)), "Ground mirror " + clip.Id);
                    Check(Near(west.HitPoint, new Vector2(clip.Width - 1 - clip.HitPoint.x, clip.HitPoint.y)), "Hit point mirror " + clip.Id);
                    foreach (var e in clip.Events)
                    {
                        var other = west.FindEvent(e.Name);
                        Check(other != null && Near(e.TimeSeconds, other.TimeSeconds), "Direction event time " + clip.Id);
                        if (e.HasPosition) Check(other.HasPosition && Near(other.Position, new Vector2(clip.Width - 1 - e.Position.x, e.Position.y)), "Direction event socket " + clip.Id);
                    }
                }
            }
            Check(legacyTextures.Count == 201, "Original corpus atlas count changed");
            var fire = Legacy("circe", "staff_attack");
            Check(Near(fire.ReleasePoint, new Vector2(102, 58)) && Near(fire.ReleaseTime, .55f), "Circe staff release contract");
            Check(Near(Legacy("circe", "staff_attack", "west").ReleasePoint, new Vector2(25, 58)), "Circe west staff release contract");
            Check(Near(Legacy("achilles", "divine_spear").ReleasePoint, new Vector2(102, 59)), "Spear source contract");
            Check(Near(Legacy("achilles", "heel_arrow").ReleasePoint, new Vector2(83, 48)), "Heel arrow source contract");
            Check(Near(Legacy("pirate", "pistol_shot").ReleasePoint, new Vector2(114, 66)), "Pistol barrel contract");
            Check(Legacy("normal", "walk").Texture == Legacy("sarcophagus_bare", "walk").Texture, "Bare skeleton duplicated atlas");
            var burst = AnimationLibrary.GetById("achilles-fx-blue_heel_arrow_rise_burst");
            Check(Near(burst.Origin, new Vector2(64, 120)) && Near(burst.EndPoint, new Vector2(64, 24)) && Near(burst.RotationAxis, Vector2.down), "Blue arrow rise axis");
            foreach (HeroKind hero in new[] { HeroKind.Achilles, HeroKind.Circe })
            {
                Texture2D portrait = HeroPortraits.Texture(hero);
                Check(portrait != null, "Missing front inventory portrait " + hero);
                Rect bounds = HeroPortraits.Bounds(hero);
                Check(bounds.width > 10 && bounds.height > 30 && bounds.x >= 0 && bounds.y >= 0 &&
                    bounds.xMax <= portrait.width && bounds.yMax <= portrait.height, "Front portrait crop " + hero);
                Check(portrait.filterMode == FilterMode.Point && portrait.mipmapCount == 1 &&
                    portrait.format == TextureFormat.RGBA32, "Front portrait pixel import " + hero);
            }
            Check(Near(AnimatedActors.HeroPixelScale / .75f, .90f), "Hero size must be 90% of 0.7.0");
            foreach (SkeletonKind kind in Enum.GetValues(typeof(SkeletonKind)))
            {
                var enemy = new Enemy { Skeleton = kind, IsBoss = kind == SkeletonKind.Boss };
                float expected = kind == SkeletonKind.Boss ? 1.25f : kind == SkeletonKind.TRex ? 1.15f : 1.10f;
                Check(Near(AnimatedActors.EnemyPixelScale(enemy) / (.34f * enemy.Variant.visualScale), expected),
                    "Size exception stacked with the common increase: " + kind);
            }
            string path = Path.Combine(Path.GetDirectoryName(Application.dataPath), "work", "animation-library-results.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, "{\"status\":\"passed\",\"assertions\":" + checks + ",\"clips\":" + AnimationLibrary.All.Count +
                ",\"uniqueTextures\":" + textures.Count + ",\"rgba32Bytes\":" + rgbaBytes + ",\"playerSaveTouched\":false}");
            Debug.Log("SKELETON_ANIMATION_LIBRARY_PASSED: " + checks + " assertions, " + AnimationLibrary.All.Count + " clips, " + textures.Count + " exact-size point-filtered atlases.");
        }
    }
}
