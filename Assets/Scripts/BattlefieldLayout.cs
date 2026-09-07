using System;
using UnityEngine;

namespace SkeletonDefender
{
    // The same geometry is read by the native Aseprite map-authoring script.
    public static class BattlefieldLayout
    {
        [Serializable] private sealed class LayoutData
        {
            public Vector2[] path;
            public Vector2[] sites;
        }
        private static readonly LayoutData Data = Load();
        public static Vector2[] Path => Data.path;
        public static Vector2[] Sites => Data.sites;

        private static LayoutData Load()
        {
            var source = Resources.Load<TextAsset>("NeonGothic/battlefield-layout");
            if (source == null) throw new InvalidOperationException("Missing battlefield layout.");
            var data = JsonUtility.FromJson<LayoutData>(source.text);
            if (data.path == null || data.path.Length < 2 || data.sites == null || data.sites.Length != 10)
                throw new InvalidOperationException("Invalid battlefield route or tower sites.");
            return data;
        }
    }
}
