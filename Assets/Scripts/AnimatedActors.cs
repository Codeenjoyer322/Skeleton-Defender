using UnityEngine;

namespace SkeletonDefender
{
    // PNG coordinates use the upper left. Model positions remain the actors' feet.
    // Rendering, selection and projectile sockets all share this conversion.
    public static class AnimatedActors
    {
        public const float HeroPixelScale = .75f * .9f;
        public const float EnemyBasePixelScale = .34f;
        // The two larger exceptions replace the common 10% increase, rather than stack with it.
        public static float EnemySizeMultiplier(Enemy enemy) => enemy.IsBoss ? 1.25f : enemy.Skeleton == SkeletonKind.TRex ? 1.15f : 1.10f;
        public static float EnemyPixelScale(Enemy enemy) => EnemyBasePixelScale * ProjectileVisuals.EnemyScale(enemy);
        public static AnimationClipData HeroClip(HeroCombatState actor) => AnimationLibrary.GetDirectional(
            CombatAnimationRoles.Hero(actor), actor.Alive ? actor.Animation.Action : "death", actor.Animation.FacingDirection, actor.FacingLeft);
        public static AnimationClipData EnemyClip(Enemy enemy, string role = null) => AnimationLibrary.GetDirectional(
            role ?? CombatAnimationRoles.Enemy(enemy), enemy.Animation.Action, enemy.Animation.FacingDirection, enemy.Animation.FacingLeft);

        public static Direction8 DirectionFor(Vector2 direction, Direction8 previous)
        {
            if (direction.sqrMagnitude < .0001f) return previous;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            // Retain a narrow dead band around the sector edge to prevent diagonal
            // animations flickering when following a slightly moving target.
            if (Mathf.Abs(Mathf.DeltaAngle((int)previous * 45, angle)) <= 27.5f) return previous;
            int index = Mathf.RoundToInt(angle / 45f);
            return (Direction8)((index % 8 + 8) % 8);
        }

        public static Vector2 Point(AnimationClipData clip, Vector2 ground, Vector2 pixel, float scale)
            => ground + (pixel - clip.GroundPivot) * scale;

        public static Matrix4x4 HeroPose(HeroCombatState actor)
        {
            if (!actor.Alive || actor.KnockdownRemaining <= 0) return Matrix4x4.identity;
            Vector3 pivot = actor.Position + new Vector2(0, -20);
            return Matrix4x4.Translate(pivot) * Matrix4x4.Rotate(Quaternion.Euler(0, 0, actor.FacingLeft ? -75 : 75)) * Matrix4x4.Translate(-pivot);
        }

        public static Vector2 HeroPoint(HeroCombatState actor, AnimationClipData clip, Vector2 pixel)
            => HeroPose(actor).MultiplyPoint3x4(Point(clip, actor.Position, pixel, HeroPixelScale));

        public static Rect HeroBounds(HeroCombatState actor)
        {
            var clip = HeroClip(actor);
            if (clip == null) return new Rect(actor.Position.x - 28, actor.Position.y - 61, 56, 73);
            Rect bounds = clip.OpaqueBounds;
            Vector2 origin = Point(clip, actor.Position, bounds.position, HeroPixelScale);
            return new Rect(origin, bounds.size * HeroPixelScale);
        }
    }
}
