using System.Collections.Generic;
using UnityEngine;

namespace SkeletonDefender
{
    public sealed partial class SkeletonGame
    {
        private static readonly Dictionary<string, Texture2D> inventoryIconTextures = new Dictionary<string, Texture2D>();

        // Every item uses an original transparent 48px Aseprite icon. Rarity remains
        // the interface's border/text treatment, so material highlights retain their colors.
        private static Texture2D IconTexture(InventoryItem item)
        {
            string key = InventoryIconKey(item);
            if (key == null) return null;
            if (!inventoryIconTextures.TryGetValue(key, out var texture))
            {
                texture = Resources.Load<Texture2D>("NeonGothic/Icons/" + key);
                inventoryIconTextures[key] = texture;
            }
            return texture;
        }

        private static string InventoryIconKey(InventoryItem item)
        {
            if (item == null) return null;
            switch (item.Category)
            {
                case ItemCategory.Armor:
                    if (item.Slot < EquipmentSlot.Helmet || item.Slot > EquipmentSlot.Boots ||
                        item.Armor < ArmorKind.Leather || item.Armor > ArmorKind.Silk) return null;
                    return item.Armor.ToString().ToLowerInvariant() + "_" + item.Slot.ToString().ToLowerInvariant();
                case ItemCategory.Weapon:
                    switch (item.Weapon)
                    {
                        case WeaponKind.Sword: return "weapon_sword";
                        case WeaponKind.Dagger: return "weapon_dagger";
                        case WeaponKind.Staff: return "weapon_staff";
                        default: return null;
                    }
                case ItemCategory.Artifact:
                    switch (item.Artifact)
                    {
                        case ArtifactKind.AegisOfDawn: return "artifact_aegis_of_dawn";
                        case ArtifactKind.ZeusNail: return "artifact_zeus_nail";
                        case ArtifactKind.AthenaMirror: return "artifact_athena_mirror";
                        default: return null;
                    }
                default: return null;
            }
        }
    }
}
