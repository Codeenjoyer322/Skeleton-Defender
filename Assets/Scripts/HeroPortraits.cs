using System;
using UnityEngine;

namespace SkeletonDefender
{
    // Inventory portraits are the original front views, independent of combat facing.
    public static class HeroPortraits
    {
        [Serializable] private sealed class PortraitBounds
        {
            public float x, y, width, height;
        }
        [Serializable] private sealed class PortraitEntry
        {
            public string hero;
            public PortraitBounds bounds;
        }
        [Serializable] private sealed class PortraitCatalog
        {
            public PortraitEntry[] portraits;
        }
        private static readonly Texture2D[] textures = new Texture2D[2];
        private static PortraitCatalog catalog;
        private const string Folder = "AnimationOverrides/HeroFronts/";

        public static Texture2D Texture(HeroKind hero)
        {
            int index = (int)hero;
            return textures[index] != null ? textures[index] :
                textures[index] = Resources.Load<Texture2D>(Folder + (hero == HeroKind.Circe ? "circe" : "achilles"));
        }

        public static Rect Bounds(HeroKind hero)
        {
            if (catalog == null)
            {
                TextAsset json = Resources.Load<TextAsset>(Folder + "portraits");
                catalog = json == null ? new PortraitCatalog() : JsonUtility.FromJson<PortraitCatalog>(json.text);
            }
            string id = hero == HeroKind.Circe ? "circe" : "achilles";
            if (catalog.portraits != null)
                foreach (PortraitEntry portrait in catalog.portraits)
                    if (portrait.hero == id && portrait.bounds != null)
                        return new Rect(portrait.bounds.x, portrait.bounds.y, portrait.bounds.width, portrait.bounds.height);
            Texture2D texture = Texture(hero);
            return texture == null ? default : new Rect(0, 0, texture.width, texture.height);
        }
    }
}
