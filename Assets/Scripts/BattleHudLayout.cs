using UnityEngine;

namespace SkeletonDefender
{
    // All HUD rectangles use the same 1440 x 900 logical canvas as pointer input.
    public static class BattleHudLayout
    {
        public static Rect Hero => new Rect(36, 136, 72, 100);
        public static Rect Clone => new Rect(116, 174, 60, 88);
        public static Rect WaveStatus => new Rect(24, 788, 548, 88);
        public static Rect WaveButton => new Rect(588, 796, 306, 62);
        public static Rect Skill(int index) => new Rect(922 + index * 168, 796, 158, 62);
        public static Rect Tooltip => new Rect(954, 552, 462, 226);
        public static bool ContainsSkills(Vector2 point)
        {
            for (int i = 0; i < 3; i++) if (Skill(i).Contains(point)) return true;
            return false;
        }
        public static bool ContainsActor(Vector2 point, bool hasClone) =>
            Hero.Contains(point) || (hasClone && Clone.Contains(point));
    }
}
