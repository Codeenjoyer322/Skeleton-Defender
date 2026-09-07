#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SkeletonDefender.Editor
{
    public static class DeerVisualTests
    {
        private static int checks;
        private static void Check(bool value, string message)
        { checks++; if (!value) throw new Exception("DEER VISUAL: " + message); }
        private static void Set(AbilityEffect effect, string name, object value) =>
            typeof(AbilityEffect).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(effect, value);
        private static object Call(GameModel model, string name, params object[] args) =>
            typeof(GameModel).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(model, args);

        public static void Validate()
        {
            checks = 0;
            LaneJoinsAreContinuous();
            DirectionKeepsItsDeadBand();
            StrideSurvivesEveryViewChange();
            ActualCastAndFullRoute();
            SharedDepthOrder();
            Debug.Log("SKELETON_DEER_VISUAL_PASSED: " + checks + " route, stride and depth assertions; no player save touched.");
        }

        private static void LaneJoinsAreContinuous()
        {
            foreach (Vector2[] road in new[] { GameModel.Path,
                new[] { new Vector2(0, 0), new Vector2(100, 0), new Vector2(100, 100), new Vector2(0, 100) } })
            {
                float distance = 0;
                for (int vertex = 1; vertex < road.Length - 1; vertex++)
                {
                    distance += Vector2.Distance(road[vertex - 1], road[vertex]);
                    foreach (float lane in new[] { -10f, 10f })
                    {
                        DeerVisuals.SampleRoute(road, distance - .001f, lane, out var before, out var beforeTangent);
                        DeerVisuals.SampleRoute(road, distance + .001f, lane, out var after, out var afterTangent);
                        Check(Vector2.Distance(before, after) < .03f, "Lane jumps at polyline joint " + vertex);
                        Check(Vector2.Angle(beforeTangent, afterTangent) < .2f, "Visual heading jumps at joint " + vertex);
                    }
                    DeerVisuals.SampleRoute(road, distance, -10, out var first, out _);
                    DeerVisuals.SampleRoute(road, distance, 10, out var second, out _);
                    Check(Mathf.Abs(Vector2.Distance(first, second) - 20) < .002f, "Deer lanes merge or exchange sides at a corner");
                    Check(Vector2.Distance((first + second) * .5f, road[vertex]) < .002f, "Pair drifts from the road centre");
                }
            }
        }

        private static void DirectionKeepsItsDeadBand()
        {
            var effect = new AbilityEffect { Kind = "deer", Lifetime = 10, Age = 5 };
            foreach (float angle in new[] { 30f, 24f, 22f, 26f, 23f })
            {
                float radians = angle * Mathf.Deg2Rad;
                Set(effect, "Route", new[] { Vector2.zero, new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * 100 });
                Set(effect, "RouteLength", 100f);
                DeerVisuals.Update(effect);
                Check(effect.FacingDirection == Direction8.SouthEast, "View flickers on small heading noise at a sector edge");
                Check(DeerVisuals.Clip(effect) == AnimationLibrary.GetExact("circe", "deer_run", Direction8.SouthEast),
                    "Renderer disregards the persisted direction");
            }
            Set(effect, "Route", new[] { Vector2.zero, Vector2.right * 100 });
            DeerVisuals.Update(effect);
            Check(effect.FacingDirection == Direction8.East, "Deer refuses a real change of heading");
        }

        private static void StrideSurvivesEveryViewChange()
        {
            var first = new AbilityEffect { Age = .37f, VisualPlaybackRate = .7f };
            var second = new AbilityEffect { Age = first.Age, VisualPlaybackRate = .7f, VisualCycleOffset = .5f };
            foreach (Direction8 direction in Enum.GetValues(typeof(Direction8)))
            {
                first.FacingDirection = second.FacingDirection = direction;
                var clip = DeerVisuals.Clip(first);
                Check(clip != null && clip.Texture != null && clip.Frames.Length == 16 && Mathf.Abs(clip.Duration - .8f) < .001f,
                    "Missing native 16-frame deer view " + direction);
                Check(clip.FrameAt(DeerVisuals.SampleAge(first, clip), true) == 5,
                    "Changing view restarted the current stride " + direction);
                Check(clip.FrameAt(DeerVisuals.SampleAge(second, clip), true) == 13,
                    "Pair lost its fixed half-stride separation " + direction);
                // Cropped clips have different canvas sizes, but the ground anchor
                // reconstructs to the identical requested world point in every frame.
                Vector2 anchor = new Vector2(357.25f, 288.5f);
                for (int frame = 0; frame < clip.Frames.Length; frame++)
                {
                    Check(clip.Frames[frame].Width == clip.Width && clip.Frames[frame].Height == clip.Height &&
                        clip.Frames[frame].SourceOffset == Vector2.zero, "A trimmed frame would stretch or displace deer ground");
                    Check(Vector2.Distance(AnimatedActors.Point(clip, anchor, clip.GroundPivot, DeerVisuals.PixelScale), anchor) < .0001f,
                        "Native crop shifts the ground pivot");
                }
            }
            var east = AnimationLibrary.GetExact("circe", "deer_run", Direction8.East);
            first.Age = .8f / .7f - .001f;
            Check(east.FrameAt(DeerVisuals.SampleAge(first, east), true) == 15, "Stride skips its last frame");
            first.Age += .002f;
            Check(east.FrameAt(DeerVisuals.SampleAge(first, east), true) == 0, "Stride does not loop at its unchanged 70-percent speed");
        }

        private static void ActualCastAndFullRoute()
        {
            var model = new GameModel(1, HeroKind.Circe, new EquipmentStats(), 281);
            var skill = BalanceData.Current.heroSystems.Skill(HeroKind.Circe, 0);
            Call(model, "CastDeer", model.Hero, skill, -1f);
            var deer = model.AbilityEffects.FindAll(effect => effect.Kind == "deer");
            Check(deer.Count == 2, "Cast lost a runner");
            Check(Vector2.Distance(DeerVisuals.Ground(deer[0]), DeerVisuals.Ground(deer[1])) > 19.99f,
                "Both deer spawn on the same pixel and jump apart on the next step");
            Vector2 centre = GameModel.Path[GameModel.Path.Length - 1];
            Check(Vector2.Distance((deer[0].VisualGround + deer[1].VisualGround) * .5f, centre) < .001f,
                "Pair did not start at the castle");
            foreach (AbilityEffect effect in deer)
                Check(Mathf.Abs(effect.Lifetime - 12f / .7f) < .001f, "Visual fix changed the gameplay duration");
            var positions = new[] { deer[0].VisualGround, deer[1].VisualGround };
            float frameTime = 1f / 120;
            while (deer[0].Age < deer[0].Lifetime)
            {
                Call(model, "UpdateAbilityEffects", frameTime);
                for (int i = 0; i < deer.Count; i++)
                {
                    Check(Vector2.Distance(positions[i], deer[i].VisualGround) < 2,
                        "Visual runner teleports during a real road traversal");
                    positions[i] = deer[i].VisualGround;
                }
            }
            Check(model.AbilityEffects.Count == 0, "Expired deer remains in the draw list");
            Check(Vector2.Distance((deer[0].VisualGround + deer[1].VisualGround) * .5f, GameModel.Path[0]) < .01f,
                "Visual runner does not reach the enemy gate");
        }

        private static void SharedDepthOrder()
        {
            var model = new GameModel(1, HeroKind.Circe, new EquipmentStats(), 283);
            var back = new AbilityEffect { Kind = "deer", HasVisualGround = true };
            var front = new AbilityEffect { Kind = "deer", HasVisualGround = true };
            var enemy = model.CreateEnemy(SkeletonKind.Normal, model.PathLength * .5f);
            model.Enemies.Add(enemy);
            float y = model.Position(enemy.Distance).y;
            back.VisualGround = new Vector2(400, y - 10); front.VisualGround = new Vector2(400, y + 10);
            model.Hero.Position = new Vector2(400, y + 5);
            // Deliberately put the front animal first: list order is not depth order.
            model.AbilityEffects.Add(front); model.AbilityEffects.Add(back);
            Type layerType = typeof(SkeletonGame).GetNestedType("BattleLayer", BindingFlags.NonPublic);
            var layers = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(layerType));
            var fill = typeof(SkeletonGame).GetMethod("FillBattleLayers", BindingFlags.Static | BindingFlags.NonPublic);
            Action rebuild = () => fill.Invoke(null, new object[] { model, layers });
            Func<string, object, int> indexOf = (field, target) => {
                for (int i = 0; i < layers.Count; i++)
                    if (ReferenceEquals(layerType.GetField(field).GetValue(layers[i]), target)) return i;
                return -1;
            };
            rebuild();
            Check(layers.Count == 4 && indexOf("Deer", back) < indexOf("Enemy", enemy) &&
                indexOf("Enemy", enemy) < indexOf("Hero", model.Hero) && indexOf("Hero", model.Hero) < indexOf("Deer", front),
                "Deer do not overlap the other animal and actors in world-ground order");
            back.VisualGround = front.VisualGround;
            for (int repeat = 0; repeat < 30; repeat++)
            {
                rebuild();
                Check(indexOf("Deer", front) < indexOf("Deer", back), "Equal-depth runners swap silhouettes between frames");
            }
            back.VisualGround += new Vector2(0, 1);
            rebuild();
            Check(indexOf("Deer", front) < indexOf("Deer", back), "Crossing runner is not drawn in front after it moves closer");
            model.AbilityEffects.Clear(); rebuild();
            Check(layers.Count == 2, "Finished deer is still rendered or renders twice");
        }
    }
}
#endif
