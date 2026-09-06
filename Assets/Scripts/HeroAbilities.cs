using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkeletonDefender
{
    internal sealed class ForwardMovement
    {
        public float StartTime, EndTime, Distance;
    }

    public sealed class AbilityEffect
    {
        public string Kind;
        public Vector2 Start, End;
        public float Age, Lifetime;
        public HeroCombatState Source;
        public Vector2[] Targets;
        internal int TargetId = -1;
        internal float Damage, Radius, SlowFraction, SlowDuration, LaneOffset;
        internal bool Resolved;
        internal Vector2[] Route;
        internal float RouteLength;
        internal readonly HashSet<int> HitIds = new HashSet<int>();
    }

    public sealed class TowerProjectile
    {
        public Vector2 Start, Position, Impact;
        public int TargetId;
        public TowerKind Kind;
        public float Damage, SplashRadius;
    }

    public sealed partial class GameModel
    {
        public HeroCombatState Clone { get; private set; }
        public float CloneRemaining { get; private set; }
        public float RageRemaining => Hero.RageRemaining;
        public bool ArtifactUsed { get; private set; }
        public readonly List<AbilityEffect> AbilityEffects = new List<AbilityEffect>();
        public readonly List<TowerProjectile> TowerProjectiles = new List<TowerProjectile>();
        private double preciseCloneRemaining;
        private HeroSystemsData Systems => BalanceData.Current.heroSystems;

        private HeroCombatState CreateActor(HeroKind kind, EquipmentStats equipment, bool clone, Vector2 position)
        {
            HeroDefinition definition = BalanceData.Current.Hero((int)kind);
            var actor = new HeroCombatState {
                Kind = kind, IsClone = clone, Equipment = equipment.Copy(),
                Hp = definition.hp, MaxHp = definition.hp, BaseDamage = definition.damage + equipment.DamageBonus,
                Position = position, Destination = position,
                FrogCooldown = kind == HeroKind.Circe && definition.skills[0].trigger != "basic_attack" ? definition.skills[0].cooldown : 0,
                SunCooldown = kind == HeroKind.Circe ? definition.skills[1].cooldown : 0,
                DecapitateCooldown = kind == HeroKind.Achilles ? definition.skills[0].cooldown : 0
            };
            actor.Mana = actor.MaxMana;
            return actor;
        }

        public HeroCombatState GetControlledHero(bool clone = false) => clone ? Clone : Hero;
        public float SkillManaCost(int index, bool clone = false)
        {
            HeroCombatState actor = GetControlledHero(clone);
            if (actor == null || index < 0 || index > 1) return 0;
            return Systems.Skill(actor.Kind, index).manaCost * (1 - Mathf.Clamp01(actor.Equipment.ManaCostReduction));
        }
        public float SkillCooldownRemaining(int index, bool clone = false)
        {
            HeroCombatState actor = GetControlledHero(clone);
            return actor == null || index < 0 || index > 1 ? 0 : actor.ManualCooldowns[index];
        }
        public bool RequiresSkillTarget(int index, bool clone = false) => index == 0 && GetControlledHero(clone)?.Kind == HeroKind.Achilles;
        private bool CanAct(HeroCombatState actor) => HasStarted && !Finished && !IsPaused && actor != null && actor.Alive && actor.KnockdownRemaining <= 0;
        public bool CanCastSkill(int index, bool clone = false)
        {
            HeroCombatState actor = GetControlledHero(clone);
            return index >= 0 && index < 2 && CanAct(actor) && actor.ManualCooldowns[index] <= 0 && actor.Mana + .00001f >= SkillManaCost(index, clone);
        }

        // Selecting or cancelling a target is UI-only. Only a valid confirmed cast spends resources.
        public bool TryCastSkill(int index, Vector2? target = null, bool clone = false)
        {
            if (!CanCastSkill(index, clone)) return false;
            if (RequiresSkillTarget(index, clone) && (!target.HasValue || !ValidSkillPoint(target.Value))) return false;
            HeroCombatState actor = GetControlledHero(clone);
            ManualSkillDefinition skill = Systems.Skill(actor.Kind, index);
            actor.Mana = Mathf.Max(0, actor.Mana - SkillManaCost(index, clone));
            actor.ManualCooldowns[index] = skill.cooldown;
            actor.PreciseManualCooldowns[index] = skill.cooldown;
            actor.LastCastName = skill.name; actor.CastPoseRemaining = .7f;
            ActorActivity(actor, true);
            if (actor.Kind == HeroKind.Circe && index == 0) CastDeer(actor, skill);
            else if (actor.Kind == HeroKind.Circe) CastStorm(actor, skill);
            else if (index == 0)
                AbilityEffects.Add(new AbilityEffect {
                    Kind = "spear", Start = actor.Position, End = target.Value, Source = actor,
                    Lifetime = Mathf.Max(.01f, skill.visualDelay), Damage = skill.damage * actor.DamageMultiplier,
                    Radius = skill.areaRadius
                });
            else CastArrowRain(actor, skill);
            return true;
        }
        private static bool ValidSkillPoint(Vector2 point) => Finite(point.x) && Finite(point.y) && point.x >= 0 && point.x <= 1056 && point.y >= 0 && point.y <= 640;

        public bool CanUseArtifact => CanAct(Hero) && !ArtifactUsed &&
            (Hero.Equipment.Artifact == ArtifactKind.ZeusNail || Hero.Equipment.Artifact == ArtifactKind.AthenaMirror);
        public bool TryUseArtifact()
        {
            if (!CanUseArtifact) return false;
            ArtifactUsed = true; ActorActivity(Hero, true);
            Hero.CastPoseRemaining = .7f;
            if (Hero.Equipment.Artifact == ArtifactKind.ZeusNail)
            {
                Hero.RageRemaining = Systems.artifactDuration; Hero.PreciseRageRemaining = Systems.artifactDuration; Hero.LastCastName = "Ярость Зевса";
                AbilityEffects.Add(new AbilityEffect { Kind = "rage", Start = Hero.Position, End = Hero.Position, Source = Hero, Lifetime = .8f });
            }
            else
            {
                Clone = CreateActor(Hero.Kind, Hero.Equipment, true, Hero.Position);
                CloneRemaining = Systems.artifactDuration; preciseCloneRemaining = Systems.artifactDuration; Hero.LastCastName = "Зеркало Афины";
                AbilityEffects.Add(new AbilityEffect { Kind = "mirror", Start = Hero.Position, End = Clone.Position, Source = Hero, Lifetime = .8f });
            }
            return true;
        }

        private void BeginActorTick(HeroCombatState actor)
        {
            if (actor == null) return;
            actor.ActivityThisTick = actor.ActivityPending; actor.ActivityPending = false;
        }
        private void ActorActivity(HeroCombatState actor, bool pending = false)
        {
            actor.ActivityThisTick = true;
            if (pending) actor.ActivityPending = true;
            actor.Regeneration.Reset();
        }
        private void UpdateActorSystems(HeroCombatState actor, float dt)
        {
            if (actor == null) return;
            actor.CastPoseRemaining = Mathf.Max(0, actor.CastPoseRemaining - dt);
            if (HasStarted)
            {
                if (Mathf.Abs((float)actor.PreciseRageRemaining - actor.RageRemaining) > .00001f) actor.PreciseRageRemaining = actor.RageRemaining;
                actor.PreciseRageRemaining = Math.Max(0, actor.PreciseRageRemaining - dt);
                actor.RageRemaining = (float)actor.PreciseRageRemaining;
                for (int i = 0; i < actor.ManualCooldowns.Length; i++)
                {
                    if (Mathf.Abs((float)actor.PreciseManualCooldowns[i] - actor.ManualCooldowns[i]) > .00001f)
                        actor.PreciseManualCooldowns[i] = actor.ManualCooldowns[i];
                    actor.PreciseManualCooldowns[i] = Math.Max(0, actor.PreciseManualCooldowns[i] - dt);
                    actor.ManualCooldowns[i] = (float)actor.PreciseManualCooldowns[i];
                }
            }
            if (actor.Alive) actor.Mana = Mathf.Min(actor.MaxMana, actor.Mana + actor.ManaRegen * dt);
        }
        private void RemoveClone()
        {
            if (Clone != null) { Clone.Hp = 0; Clone.LifeSerial++; Clone.Regeneration.Reset(); }
            Clone = null; CloneRemaining = 0; preciseCloneRemaining = 0;
        }
        private void FinishCombat()
        {
            Projectiles.Clear(); EnemyProjectiles.Clear(); TowerProjectiles.Clear(); AbilityEffects.Clear();
            Hero.Regeneration.Reset();
            if (Clone != null) Clone.Regeneration.Reset();
        }

        private void CastStorm(HeroCombatState actor, ManualSkillDefinition skill)
        {
            var points = new List<Vector2>();
            foreach (Enemy enemy in Enemies)
            {
                if (!enemy.Targetable) continue;
                points.Add(Position(enemy.Distance));
                ApplyHit(enemy, skill.damage * actor.MagicDamageMultiplier, true, false);
            }
            AbilityEffects.Add(new AbilityEffect { Kind = "storm", Start = actor.Position, End = actor.Position,
                Source = actor, Lifetime = .9f, Targets = points.ToArray() });
        }
        private void CastArrowRain(HeroCombatState actor, ManualSkillDefinition skill)
        {
            foreach (Enemy enemy in Enemies)
            {
                if (!enemy.Targetable) continue;
                AbilityEffects.Add(new AbilityEffect {
                    Kind = "arrow_rain", Start = actor.Position, End = Position(enemy.Distance), Source = actor,
                    Lifetime = Mathf.Max(.01f, skill.visualDelay), TargetId = enemy.Id,
                    Damage = skill.damage * actor.DamageMultiplier, SlowFraction = skill.slowFraction, SlowDuration = skill.slowDuration
                });
            }
        }
        private void CastDeer(HeroCombatState actor, ManualSkillDefinition skill)
        {
            Vector2[] route = UpstreamRoute(actor.Position);
            float length = 0;
            for (int i = 1; i < route.Length; i++) length += Vector2.Distance(route[i - 1], route[i]);
            for (int i = 0; i < skill.projectiles; i++)
                AbilityEffects.Add(new AbilityEffect {
                    Kind = "deer", Start = actor.Position, End = actor.Position, Source = actor,
                    Lifetime = Mathf.Max(.01f, skill.duration), Damage = skill.damage * actor.MagicDamageMultiplier,
                    SlowFraction = skill.slowFraction, SlowDuration = skill.slowDuration,
                    Route = route, RouteLength = length,
                    LaneOffset = (i - (skill.projectiles - 1) * .5f) * Systems.deerLaneOffset * 2
                });
        }
        private Vector2[] UpstreamRoute(Vector2 origin)
        {
            int segment = 0; Vector2 projection = Path[0]; float closest = float.PositiveInfinity;
            for (int i = 0; i < Path.Length - 1; i++)
            {
                Vector2 delta = Path[i + 1] - Path[i];
                float t = Mathf.Clamp01(Vector2.Dot(origin - Path[i], delta) / delta.sqrMagnitude);
                Vector2 point = Path[i] + delta * t;
                float distance = (origin - point).sqrMagnitude;
                if (distance < closest) { closest = distance; projection = point; segment = i; }
            }
            var route = new List<Vector2> { origin };
            if (Vector2.Distance(origin, projection) > .001f) route.Add(projection);
            for (int i = segment; i >= 0; i--) if (Vector2.Distance(route[route.Count - 1], Path[i]) > .001f) route.Add(Path[i]);
            if (route.Count == 1) route.Add(origin + Vector2.left);
            return route.ToArray();
        }

        private void UpdateAbilityEffects(float dt)
        {
            for (int i = AbilityEffects.Count - 1; i >= 0; i--)
            {
                AbilityEffect effect = AbilityEffects[i];
                float previousAge = effect.Age;
                effect.Age = Mathf.Min(effect.Lifetime, effect.Age + dt);
                if (effect.Kind == "deer") AdvanceDeer(effect, previousAge);
                else if (!effect.Resolved && effect.Age >= effect.Lifetime)
                {
                    effect.Resolved = true;
                    if (effect.Kind == "spear")
                    {
                        foreach (Enemy enemy in Enemies)
                        {
                            if (!enemy.Targetable || Vector2.Distance(Position(enemy.Distance), effect.End) > effect.Radius) continue;
                            bool landed = ApplyHit(enemy, effect.Damage, false, false);
                            if (landed && !enemy.Dead && !enemy.IsBoss)
                                enemy.Distance = Mathf.Max(0, enemy.Distance - RecentForwardDistance(enemy) * Systems.spearKnockbackFraction);
                        }
                    }
                    else if (effect.Kind == "arrow_rain")
                    {
                        Enemy target = Enemies.Find(enemy => enemy.Id == effect.TargetId && enemy.Targetable);
                        if (target != null && ApplyHit(target, effect.Damage, false, false))
                            ApplySkillSlow(target, effect.SlowFraction, effect.SlowDuration);
                    }
                }
                if (effect.Kind == "arrow_rain")
                {
                    Enemy target = Enemies.Find(enemy => enemy.Id == effect.TargetId && enemy.Targetable);
                    if (target != null) effect.End = Position(target.Distance);
                }
                if (effect.Age >= effect.Lifetime) AbilityEffects.RemoveAt(i);
            }
        }
        private void AdvanceDeer(AbilityEffect effect, float previousAge)
        {
            float fromDistance = effect.RouteLength * previousAge / effect.Lifetime;
            float toDistance = effect.RouteLength * effect.Age / effect.Lifetime;
            float startDistance = 0;
            for (int s = 1; s < effect.Route.Length; s++)
            {
                Vector2 start = effect.Route[s - 1], delta = effect.Route[s] - start;
                float length = delta.magnitude;
                if (length <= .0001f) continue;
                float endDistance = startDistance + length;
                if (fromDistance <= endDistance && toDistance >= startDistance)
                {
                    Vector2 lateral = new Vector2(-delta.y, delta.x) / length * effect.LaneOffset;
                    Vector2 a = start + delta * Mathf.Clamp01((fromDistance - startDistance) / length) + lateral;
                    Vector2 b = start + delta * Mathf.Clamp01((toDistance - startDistance) / length) + lateral;
                    effect.End = b;
                    foreach (Enemy enemy in Enemies)
                    {
                        if (!enemy.Targetable || effect.HitIds.Contains(enemy.Id) || DistanceToSegment(Position(enemy.Distance), a, b) > Systems.deerHitRadius) continue;
                        effect.HitIds.Add(enemy.Id);
                        if (ApplyHit(enemy, effect.Damage, true, false)) ApplySkillSlow(enemy, effect.SlowFraction, effect.SlowDuration);
                    }
                }
                startDistance = endDistance;
            }
        }
        private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 delta = b - a;
            float t = delta.sqrMagnitude > .00001f ? Mathf.Clamp01(Vector2.Dot(point - a, delta) / delta.sqrMagnitude) : 0;
            return Vector2.Distance(point, a + delta * t);
        }
        private static void ApplySkillSlow(Enemy enemy, float fraction, float duration)
        {
            if (enemy.Dead || enemy.IsBoss || fraction <= 0 || duration <= 0) return;
            float multiplier = 1 - Mathf.Clamp01(fraction);
            enemy.SkillSlowMultiplier = enemy.SkillSlowRemaining > 0 ? Mathf.Min(enemy.SkillSlowMultiplier, multiplier) : multiplier;
            enemy.SkillSlowRemaining = Mathf.Max(enemy.SkillSlowRemaining, duration);
        }
        private void RecordForwardMovement(Enemy enemy, float previousDistance, float dt)
        {
            float delta = enemy.Distance - previousDistance;
            if (delta > 0) enemy.MovementHistory.Add(new ForwardMovement { StartTime = Elapsed - dt, EndTime = Elapsed, Distance = delta });
            enemy.MovementHistory.RemoveAll(movement => movement.EndTime <= Elapsed - Systems.spearHistorySeconds);
        }
        public float RecentForwardDistance(Enemy enemy)
        {
            float threshold = Elapsed - Systems.spearHistorySeconds, distance = 0;
            foreach (ForwardMovement movement in enemy.MovementHistory)
            {
                float duration = movement.EndTime - movement.StartTime;
                if (duration <= 0 || movement.EndTime <= threshold) continue;
                distance += movement.Distance * Mathf.Clamp01((movement.EndTime - Mathf.Max(threshold, movement.StartTime)) / duration);
            }
            return distance;
        }

        private void UpdateTowerProjectiles(float dt)
        {
            for (int i = TowerProjectiles.Count - 1; i >= 0; i--)
            {
                TowerProjectile projectile = TowerProjectiles[i];
                Enemy target = Enemies.Find(enemy => enemy.Id == projectile.TargetId && enemy.Targetable);
                if (target == null) { TowerProjectiles.RemoveAt(i); continue; }
                projectile.Impact = Position(target.Distance);
                float travel = Systems.towerProjectileSpeed * dt;
                if (Vector2.Distance(projectile.Position, projectile.Impact) <= travel)
                {
                    if (projectile.Kind == TowerKind.Ember)
                    {
                        foreach (Enemy enemy in Enemies)
                            if (enemy.Targetable && Vector2.Distance(Position(enemy.Distance), projectile.Impact) <= projectile.SplashRadius)
                                ApplyHit(enemy, projectile.Damage, true, false);
                    }
                    else Hit(target, projectile.Damage, false);
                    TowerProjectiles.RemoveAt(i);
                }
                else projectile.Position = Vector2.MoveTowards(projectile.Position, projectile.Impact, travel);
            }
        }
    }
}
