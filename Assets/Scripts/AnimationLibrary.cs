using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkeletonDefender
{
    // Screen/map coordinates have positive Y toward the bottom of the screen.
    public enum Direction8 { East, SouthEast, South, SouthWest, West, NorthWest, North, NorthEast }
    [Serializable]
    public sealed class AnimationFrameData
    {
        public int x, y, w, h, sourceX, sourceY;
        public float durationSeconds;
        public int X => x;
        public int Y => y;
        public int Width => w;
        public int Height => h;
        public Vector2 SourceOffset => new Vector2(sourceX, sourceY);
        public float DurationSeconds => durationSeconds;
    }

    [Serializable]
    public sealed class AnimationEventData
    {
        public string name, socketName;
        public int frameIndex;
        public float timeSeconds;
        public bool hasPosition;
        public Vector2 position;
        public string Name => name;
        public string SocketName => socketName;
        public int FrameIndex => frameIndex;
        public float TimeSeconds => timeSeconds;
        public bool HasPosition => hasPosition;
        public Vector2 Position => position;
    }

    [Serializable]
    public sealed class AnimationSocketData
    {
        public string name;
        public int frameIndex;
        public Vector2 position;
    }

    /// <summary>Pixel positions use the original canvas top-left, including for west clips.</summary>
    [Serializable]
    public sealed class AnimationClipData
    {
        public string id, character, action, category, direction, resourcePath, aliasOf, sourceSheetSha256, sourceMetadata;
        public int width, height, sheetWidth, sheetHeight;
        public float opaqueX, opaqueY, opaqueWidth, opaqueHeight;
        public float duration, displayScale;
        public bool loop, flipX;
        public Vector2 groundPivot, hitPoint, origin, endPoint, rotationAxis;
        public AnimationFrameData[] frames;
        public AnimationEventData[] events;
        public AnimationSocketData[] sockets;
        [NonSerialized] Texture2D texture;

        public string Id => id;
        public string Character => character;
        public string Action => action;
        public string Category => category;
        public string Direction => direction;
        public string AliasOf => aliasOf;
        public string ResourcePath => resourcePath;
        public int Width => width;
        public int Height => height;
        public int SheetWidth => sheetWidth;
        public int SheetHeight => sheetHeight;
        public float Duration => duration;
        public float DisplayScale => displayScale;
        public bool Loop => loop;
        public bool FlipX => flipX;
        public Vector2 GroundPivot => groundPivot;
        public Vector2 HitPoint => hitPoint;
        public Vector2 Origin => origin;
        public Vector2 EndPoint => endPoint;
        public Vector2 RotationAxis => rotationAxis;
        public Rect OpaqueBounds => new Rect(opaqueX, opaqueY, opaqueWidth, opaqueHeight);
        public AnimationFrameData[] Frames => frames;
        public AnimationEventData[] Events => events;
        public AnimationSocketData[] Sockets => sockets;
        public Texture2D Texture => texture != null ? texture : texture = AnimationLibrary.LoadTexture(resourcePath);

        public int FrameAt(float age, bool shouldLoop)
        {
            if (frames == null || frames.Length == 0) return 0;
            if (float.IsNaN(age) || age <= 0) return 0;
            if (float.IsInfinity(age)) return shouldLoop ? 0 : frames.Length - 1;
            float cursor = shouldLoop && duration > 0 ? age % duration : age;
            for (int i = 0; i < frames.Length; i++)
            {
                // Authored millisecond boundaries may round by a few ULPs when
                // accumulated as Unity floats. Enter the new frame at its cue.
                if (cursor < frames[i].durationSeconds - .000001f) return i;
                cursor -= frames[i].durationSeconds;
            }
            return frames.Length - 1;
        }

        public int FrameAt(float age) => FrameAt(age, loop);

        /// <summary>Unity bottom-left UV rect; includes metadata-only idle reflection.</summary>
        public Rect UV(int index)
        {
            if (frames == null || frames.Length == 0 || sheetWidth <= 0 || sheetHeight <= 0) return default;
            var frame = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
            float u = (float)frame.x / sheetWidth;
            float v = 1f - (float)(frame.y + frame.h) / sheetHeight;
            float uw = (float)frame.w / sheetWidth;
            return new Rect(flipX ? u + uw : u, v, flipX ? -uw : uw, (float)frame.h / sheetHeight);
        }

        public AnimationEventData FindEvent(string eventName)
        {
            if (events != null)
                foreach (var entry in events)
                    if (string.Equals(entry.name, eventName, StringComparison.OrdinalIgnoreCase)) return entry;
            return null;
        }

        public Vector2 ReleasePoint
        {
            get
            {
                if (events != null)
                {
                    foreach (var e in events)
                        if (e.hasPosition && e.name.IndexOf("release", StringComparison.OrdinalIgnoreCase) >= 0) return e.position;
                    foreach (var e in events)
                        if (e.hasPosition) return e.position;
                }
                return origin;
            }
        }

        public Vector2 SocketAt(string socketName, int frameIndex, Vector2 fallback)
        {
            AnimationSocketData best = null;
            if (sockets != null)
                foreach (var socket in sockets)
                    if (string.Equals(socket.name, socketName, StringComparison.OrdinalIgnoreCase) && socket.frameIndex <= frameIndex &&
                        (best == null || socket.frameIndex > best.frameIndex)) best = socket;
            return best == null ? fallback : best.position;
        }

        public float ReleaseTime
        {
            get
            {
                if (events != null)
                    foreach (var e in events)
                        if (e.name.IndexOf("release", StringComparison.OrdinalIgnoreCase) >= 0 || e.name.IndexOf("summon", StringComparison.OrdinalIgnoreCase) >= 0)
                            return e.timeSeconds;
                return ContactTime;
            }
        }

        public float ContactTime
        {
            get
            {
                if (events != null)
                    foreach (var e in events)
                        if (e.name.IndexOf("contact", StringComparison.OrdinalIgnoreCase) >= 0 || e.name.IndexOf("release", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            e.name.IndexOf("summon", StringComparison.OrdinalIgnoreCase) >= 0) return e.timeSeconds;
                return 0;
            }
        }
    }

    public static class AnimationLibrary
    {
        [Serializable] sealed class Catalog { public int schemaVersion; public AnimationClipData[] clips; }
        static readonly Dictionary<string, AnimationClipData> ById = new Dictionary<string, AnimationClipData>(StringComparer.OrdinalIgnoreCase);
        static readonly Dictionary<string, AnimationClipData> ByAction = new Dictionary<string, AnimationClipData>(StringComparer.OrdinalIgnoreCase);
        static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();
        static AnimationClipData[] all;
        static bool loaded;
        public static IReadOnlyList<AnimationClipData> All { get { EnsureLoaded(); return all; } }
        public static bool IsAvailable { get { EnsureLoaded(); return all.Length > 0; } }

        static string Key(string character, string action, string direction) => character + "/" + action + "/" + direction;

        static void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;
            var text = Resources.Load<TextAsset>("AnimationPack/catalog");
            var data = text == null ? null : JsonUtility.FromJson<Catalog>(text.text);
            RegisterCatalog(data);
            // Genuine extra views are delivered independently. Existing E/W art remains
            // intact; a missing view is never manufactured by rotating its body texture.
            var directionalText = Resources.Load<TextAsset>("AnimationPack/directional-catalog");
            var directional = directionalText == null ? null : JsonUtility.FromJson<Catalog>(directionalText.text);
            RegisterCatalog(directional);
            all = new List<AnimationClipData>(ById.Values).ToArray();
        }

        static void RegisterCatalog(Catalog data)
        {
            if (data == null || data.schemaVersion != 1 || data.clips == null) return;
            foreach (var clip in data.clips)
            {
                if (clip == null || string.IsNullOrEmpty(clip.id)) continue;
                ById[clip.id] = clip;
                ByAction[Key(clip.character, clip.action, NormalizeDirection(clip.direction))] = clip;
            }
        }

        public static AnimationClipData Get(string character, string action, bool facingLeft = false)
        {
            EnsureLoaded();
            if (ByAction.TryGetValue(Key(character, action, facingLeft ? "west" : "east"), out var clip)) return clip;
            return ByAction.TryGetValue(Key(character, action, "none"), out clip) ? clip : null;
        }

        public static string DirectionName(Direction8 direction)
        {
            switch (direction)
            {
                case Direction8.SouthEast: return "south-east";
                case Direction8.South: return "south";
                case Direction8.SouthWest: return "south-west";
                case Direction8.West: return "west";
                case Direction8.NorthWest: return "north-west";
                case Direction8.North: return "north";
                case Direction8.NorthEast: return "north-east";
                default: return "east";
            }
        }

        private static string NormalizeDirection(string direction)
        {
            if (string.IsNullOrEmpty(direction)) return "none";
            return direction.ToLowerInvariant().Replace("_", "-")
                .Replace("southeast", "south-east").Replace("southwest", "south-west")
                .Replace("northeast", "north-east").Replace("northwest", "north-west");
        }

        public static AnimationClipData GetExact(string character, string action, Direction8 direction)
        {
            EnsureLoaded();
            return ByAction.TryGetValue(Key(character, action, DirectionName(direction)), out var clip) ? clip : null;
        }

        public static AnimationClipData GetDirectional(string character, string action, Direction8 direction, bool legacyFacingLeft)
        {
            var exact = GetExact(character, action, direction);
            // Compatibility only, not a claim that the missing direction has art.
            return exact ?? Get(character, action, legacyFacingLeft);
        }

        public static bool HasFullDirectionSet(string character, string action)
        {
            for (int i = 0; i < 8; i++)
            {
                var clip = GetExact(character, action, (Direction8)i);
                if (clip == null || clip.FlipX || clip.Frames == null || clip.Frames.Length == 0) return false;
            }
            return true;
        }

        public static AnimationClipData GetById(string id)
        {
            EnsureLoaded();
            return id != null && ById.TryGetValue(id, out var clip) ? clip : null;
        }

        internal static Texture2D LoadTexture(string path)
        {
            if (!Textures.TryGetValue(path, out var texture))
            {
                texture = Resources.Load<Texture2D>(path);
                Textures[path] = texture;
            }
            return texture;
        }
    }
}
