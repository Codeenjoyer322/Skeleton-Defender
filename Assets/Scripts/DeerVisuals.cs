using UnityEngine;

namespace SkeletonDefender
{
    public static class DeerVisuals
    {
        public const float PixelScale = .6f;
        private const float AuthoredCycleSeconds = .8f;

        public static Vector2 Ground(AbilityEffect effect) => effect.HasVisualGround ? effect.VisualGround : effect.End;

        public static AnimationClipData Clip(AbilityEffect effect) => AnimationLibrary.GetDirectional(
            "circe", "deer_run", effect.FacingDirection, effect.FacingLeft);

        // One continuous gait clock survives a change of view. The second runner
        // remains half a stride apart, regardless of its screen-space lane or depth.
        public static float SampleAge(AbilityEffect effect, AnimationClipData clip) =>
            Mathf.Repeat(effect.Age * effect.VisualPlaybackRate / AuthoredCycleSeconds + effect.VisualCycleOffset, 1) * clip.Duration;

        public static void Update(AbilityEffect effect)
        {
            if (effect.Route == null || effect.Route.Length < 2) return;
            float distance = effect.RouteLength * Mathf.Clamp01(effect.Age / Mathf.Max(.0001f, effect.Lifetime));
            SampleRoute(effect.Route, distance, effect.LaneOffset, out Vector2 ground, out Vector2 tangent);
            effect.VisualGround = ground;
            effect.HasVisualGround = true;
            effect.FacingDirection = AnimatedActors.DirectionFor(tangent, effect.FacingDirection);
            if (Mathf.Abs(tangent.x) > .001f) effect.FacingLeft = tangent.x < 0;
        }

        public static void SampleRoute(Vector2[] route, float distance, float laneOffset,
            out Vector2 ground, out Vector2 tangent)
        {
            ground = route == null || route.Length == 0 ? Vector2.zero : route[0];
            tangent = Vector2.left;
            if (route == null || route.Length < 2) return;
            float remaining = Mathf.Max(0, distance);
            for (int segment = 1; segment < route.Length; segment++)
            {
                Vector2 delta = route[segment] - route[segment - 1];
                float length = delta.magnitude;
                if (length <= .0001f) continue;
                if (remaining > length && segment < route.Length - 1)
                {
                    remaining -= length;
                    tangent = delta / length;
                    ground = route[segment] + new Vector2(-tangent.y, tangent.x) * laneOffset;
                    continue;
                }
                float t = Mathf.Clamp01(remaining / length);
                Vector2 along = delta / length;
                Vector2 enter = VertexTangent(route, segment - 1, along);
                Vector2 leave = VertexTangent(route, segment, along);
                tangent = Vector2.Lerp(enter, leave, t).normalized;
                if (tangent.sqrMagnitude < .0001f) tangent = along;
                // Both adjacent segments share exactly the same normal at a join.
                ground = Vector2.Lerp(route[segment - 1], route[segment], t) +
                    new Vector2(-tangent.y, tangent.x) * laneOffset;
                return;
            }
        }

        private static Vector2 VertexTangent(Vector2[] route, int vertex, Vector2 fallback)
        {
            Vector2 before = vertex > 0 ? (route[vertex] - route[vertex - 1]).normalized : Vector2.zero;
            Vector2 after = vertex + 1 < route.Length ? (route[vertex + 1] - route[vertex]).normalized : Vector2.zero;
            Vector2 sum = before + after;
            return sum.sqrMagnitude > .0001f ? sum.normalized : fallback;
        }
    }
}
