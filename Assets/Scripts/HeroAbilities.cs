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
        internal float InitialAdvance = -1;
        public string Kind;
        public Vector2 Start, End;
        public Vector2 HitPoint, Direction;
        public Vector2 LaunchPoint;
        public bool HasLaunchPoint;
        public bool FacingLeft;
        public float Age, Lifetime;
        public float VisualPlaybackRate = 1;
        public float DamageDuration, LastDamageTickAge = -1;
        public int DamageTickCount, DamageTicksResolved;
        public HeroCombatState Source;
        public Vector2[] Targets;
        public int[] TargetIds;
        public int[] LastDamageTickTargetIds;
        public Vector2[] LastDamageTickPoints;
        internal int TargetId = -1;
        internal float Damage, Radius, SlowFraction, SlowDuration, LaneOffset, KnockbackDistance;
        internal bool Resolved;
        internal Vector2[] Route;
        internal float RouteLength;
        internal readonly HashSet<int> HitIds = new HashSet<int>();
    }

    public sealed class TowerProjectile
    {
        public float Age;
        internal float InitialAdvance = -1;
        public readonly ProjectileVisualState Visual = new ProjectileVisualState();
        public Vector2 Start, Position, Impact;
        public int TargetId;
        public TowerKind Kind;
        public int Level;
        public int VisualSocketIndex;
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
            return index >= 0 && index < 2 && CanAct(actor) && !(actor.Animation.IsBusy && actor.Animation.ManualCast) && actor.ManualCooldowns[index] <= 0 && actor.Mana + .00001f >= SkillManaCost(index, clone);
        }

        // Selecting or cancelling a target is UI-only. Only a valid confirmed cast spends resources.
        public bool TryCastSkill(int index, Vector2? target = null, bool clone = false)
        {
            if (!CanCastSkill(index, clone)) return false;
            if (RequiresSkillTarget(index, clone) && (!target.HasValue || !ValidSkillPoint(target.Value))) return false;
            HeroCombatState actor = GetControlledHero(clone);
            CancelAutomaticAction(actor);
            ManualSkillDefinition skill = Systems.Skill(actor.Kind, index);
            actor.Mana = Mathf.Max(0, actor.Mana - SkillManaCost(index, clone));
            actor.ManualCooldowns[index] = skill.cooldown;
            actor.PreciseManualCooldowns[index] = skill.cooldown;
            actor.LastCastName = skill.name;
            ActorActivity(actor, true);
            if (target.HasValue) ProjectileVisuals.Face(actor, target.Value);
            bool circe = actor.Kind == HeroKind.Circe;
            string action = circe ? (index == 0 ? "deer_cast" : "storm_cast") : (index == 0 ? "divine_spear" : "heel_arrow");
            float release = AnimateHero(actor, action, circe ? 1.4f : 1.8f,
                circe ? (index == 0 ? .3f : .34f) : (index == 0 ? .72f : 1.18f), 0, true, skill.animationSpeed);
            var targetIds = new List<int>();
            if (index == 1)
                foreach (Enemy enemy in Enemies) if (enemy.Targetable) targetIds.Add(enemy.Id);
            float damageMultiplier = circe ? actor.MagicDamageMultiplier : actor.DamageMultiplier;
            Vector2 selectedPoint = target ?? actor.Position;
            QueueContact(actor, release, () => {
                if (circe && index == 0) CastDeer(actor, skill, damageMultiplier);
                else if (circe) CastStorm(actor, skill, targetIds.ToArray(), damageMultiplier);
                else if (index == 0)
                {
                    Vector2 launchPoint = ProjectileVisuals.SkillReleasePoint(actor, "divine_spear");
                    AbilityEffects.Add(new AbilityEffect {
                        Kind = "spear", Start = actor.Position, End = selectedPoint, Source = actor,
                        FacingLeft = actor.FacingLeft, LaunchPoint = launchPoint, HasLaunchPoint = true,
                        Lifetime = ProjectileVisuals.SpearFlightDuration(launchPoint, selectedPoint), Damage = skill.damage * damageMultiplier,
                        Radius = skill.areaRadius
                    });
                }
                else CastArrowRain(actor, skill, targetIds.ToArray(), damageMultiplier);
            });
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
            ProjectileImpacts.Clear(); animationContacts.Clear();
            Hero.Regeneration.Reset();
            if (Clone != null) Clone.Regeneration.Reset();
        }

        private void CastStorm(HeroCombatState actor, ManualSkillDefinition skill, int[] targetIds = null, float damageMultiplier = -1)
        {
            var points = new List<Vector2>();
            var ids = new List<int>();
            // Preserve the input snapshot, including temporarily untargetable original
            // actors. Each damage tick independently checks whether that actor still exists.
            foreach (Enemy enemy in Enemies)
            {
                if (enemy.Dead || (targetIds != null && Array.IndexOf(targetIds, enemy.Id) < 0) || ids.Contains(enemy.Id)) continue;
                points.Add(ProjectileVisuals.EnemyHitPoint(enemy, Position(enemy.Distance)));
                ids.Add(enemy.Id);
            }
            float duration = Mathf.Max(.01f, skill.duration);
            AbilityEffects.Add(new AbilityEffect { Kind = "storm", Start = actor.Position, End = actor.Position,
                Source = actor, Lifetime = duration + .34f, DamageDuration = duration, DamageTickCount = Mathf.Max(1, skill.damageTicks),
                Damage = skill.damage * (damageMultiplier >= 0 ? damageMultiplier : actor.MagicDamageMultiplier),
                Targets = points.ToArray(), TargetIds = ids.ToArray() });
        }
        private void AdvanceStorm(AbilityEffect effect)
        {
            int count = Mathf.Max(1, effect.DamageTickCount);
            float duration = Mathf.Max(.01f, effect.DamageDuration);
            int due = Mathf.Clamp(Mathf.FloorToInt((effect.Age + .00001f) * count / duration), 0, count);
            while (effect.DamageTicksResolved < due)
            {
                int tick = ++effect.DamageTicksResolved;
                float damage = effect.Damage * tick / count - effect.Damage * (tick - 1) / count;
                effect.LastDamageTickAge = duration * tick / count;
                var hitIds = new List<int>();
                var hitPoints = new List<Vector2>();
                effect.LastDamageTickTargetIds = Array.Empty<int>();
                effect.LastDamageTickPoints = Array.Empty<Vector2>();
                if (effect.TargetIds == null) continue;
                for (int i = 0; i < effect.TargetIds.Length; i++)
                {
                    int id = effect.TargetIds[i];
                    Enemy enemy = Enemies.Find(candidate => candidate.Id == id && candidate.Targetable);
                    if (enemy == null) continue;
                    Vector2 point = ProjectileVisuals.EnemyHitPoint(enemy, Position(enemy.Distance));
                    if (effect.Targets != null && i < effect.Targets.Length) effect.Targets[i] = point;
                    hitIds.Add(id); hitPoints.Add(point);
                    ApplyHit(enemy, damage, true, false);
                }
                effect.LastDamageTickTargetIds = hitIds.ToArray();
                effect.LastDamageTickPoints = hitPoints.ToArray();
            }
        }
        private void CastArrowRain(HeroCombatState actor, ManualSkillDefinition skill, int[] targetIds = null, float damageMultiplier = -1)
        {
            float rate = Mathf.Max(.01f, skill.animationSpeed);
            AbilityEffects.Add(new AbilityEffect { Kind = "arrow_rise", Start = actor.Position, End = actor.Position, Source = actor,
                FacingLeft = actor.FacingLeft, LaunchPoint = ProjectileVisuals.SkillReleasePoint(actor, "heel_arrow"), HasLaunchPoint = true,
                Lifetime = .45f / rate, VisualPlaybackRate = rate });
            foreach (Enemy enemy in Enemies)
            {
                if (!enemy.Targetable || (targetIds != null && Array.IndexOf(targetIds, enemy.Id) < 0)) continue;
                AbilityEffects.Add(new AbilityEffect {
                    Kind = "arrow_rain", Start = actor.Position, End = Position(enemy.Distance), Source = actor,
                    HitPoint = ProjectileVisuals.EnemyHitPoint(enemy, Position(enemy.Distance)),
                    Lifetime = Mathf.Max(.01f, skill.visualDelay) / rate, TargetId = enemy.Id, VisualPlaybackRate = rate,
                    Damage = skill.damage * (damageMultiplier >= 0 ? damageMultiplier : actor.DamageMultiplier), SlowFraction = skill.slowFraction, SlowDuration = skill.slowDuration
                });
            }
        }
        private void CastDeer(HeroCombatState actor, ManualSkillDefinition skill, float damageMultiplier = -1)
        {
            Vector2[] route = new Vector2[Path.Length];
            for (int i = 0; i < Path.Length; i++) route[i] = Path[Path.Length - 1 - i];
            Vector2 origin = route[0];
            float length = 0;
            for (int i = 1; i < route.Length; i++) length += Vector2.Distance(route[i - 1], route[i]);
            for (int i = 0; i < skill.projectiles; i++)
                AbilityEffects.Add(new AbilityEffect {
                    Kind = "deer", Start = origin, End = origin, Source = actor, FacingLeft = true,
                    Lifetime = Mathf.Max(.01f, skill.duration), Damage = skill.damage * (damageMultiplier >= 0 ? damageMultiplier : actor.MagicDamageMultiplier),
                    SlowFraction = skill.slowFraction, SlowDuration = skill.slowDuration,
                    KnockbackDistance = Mathf.Max(0, skill.knockbackDistance),
                    Route = route, RouteLength = length,
                    LaneOffset = (i - (skill.projectiles - 1) * .5f) * Systems.deerLaneOffset * 2
                });
        }
        private void UpdateAbilityEffects(float dt)
        {
            for (int i = AbilityEffects.Count - 1; i >= 0; i--)
            {
                AbilityEffect effect = AbilityEffects[i];
                float previousAge = effect.Age;
                float step = effect.InitialAdvance >= 0 ? Mathf.Min(dt, effect.InitialAdvance) : dt; effect.InitialAdvance = -1;
                effect.Age = Mathf.Min(effect.Lifetime, effect.Age + step);
                Enemy arrowTarget = null;
                if (effect.Kind == "arrow_rain")
                {
                    arrowTarget = Enemies.Find(enemy => enemy.Id == effect.TargetId && enemy.Targetable);
                    if (arrowTarget == null) { AbilityEffects.RemoveAt(i); continue; }
                    Vector2 previousTip = ProjectileVisuals.RainTip(effect.Start, effect.HitPoint,
                        (previousAge / effect.Lifetime - .38f) / .62f);
                    effect.End = Position(arrowTarget.Distance);
                    effect.HitPoint = ProjectileVisuals.EnemyHitPoint(arrowTarget, effect.End);
                    Vector2 tip = ProjectileVisuals.RainTip(effect.Start, effect.HitPoint,
                        (effect.Age / effect.Lifetime - .38f) / .62f);
                    effect.Direction = ProjectileVisuals.Direction(tip - previousTip, effect.HitPoint - ProjectileVisuals.ArrowSky(effect.Start));
                }
                if (effect.Kind == "storm") AdvanceStorm(effect);
                else if (effect.Kind == "deer") AdvanceDeer(effect, previousAge);
                else if (!effect.Resolved && effect.Age >= effect.Lifetime)
                {
                    effect.Resolved = true;
                    if (effect.Kind == "spear")
                    {
                        ProjectileImpacts.Add(new ProjectileImpact { Position = effect.End,
                            Direction = ProjectileVisuals.Direction(effect.End - (effect.HasLaunchPoint ? effect.LaunchPoint : ProjectileVisuals.SpearHand(effect.Start, effect.FacingLeft))), Kind = "spear", Landed = true });
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
                        bool landed = ApplyHit(arrowTarget, effect.Damage, false, false);
                        if (landed) ApplySkillSlow(arrowTarget, effect.SlowFraction, effect.SlowDuration);
                        ProjectileImpacts.Add(new ProjectileImpact { Position = effect.HitPoint, Direction = effect.Direction, Kind = "arrow", Landed = landed });
                    }
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
                    effect.Direction = ProjectileVisuals.Direction(delta);
                    if (Mathf.Abs(delta.x) > .001f) effect.FacingLeft = delta.x < 0;
                    foreach (Enemy enemy in Enemies)
                    {
                        if (!enemy.Targetable || effect.HitIds.Contains(enemy.Id) || DistanceToSegment(Position(enemy.Distance), a, b) > Systems.deerHitRadius) continue;
                        effect.HitIds.Add(enemy.Id);
                        Vector2 contactPoint = ProjectileVisuals.EnemyHitPoint(enemy, Position(enemy.Distance));
                        if (ApplyHit(enemy, effect.Damage, true, false))
                        {
                            ApplySkillSlow(enemy, effect.SlowFraction, effect.SlowDuration);
                            if (!enemy.Dead && !enemy.IsBoss)
                                enemy.Distance = Mathf.Max(0, enemy.Distance - effect.KnockbackDistance);
                            ProjectileImpacts.Add(new ProjectileImpact { Kind = "deer", Position = contactPoint,
                                Direction = effect.Direction, Landed = true });
                        }
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
                float step = projectile.InitialAdvance >= 0 ? Mathf.Min(dt, projectile.InitialAdvance) : dt;
                projectile.InitialAdvance = -1; projectile.Age += step;
                Enemy target = Enemies.Find(enemy => enemy.Id == projectile.TargetId && enemy.Targetable);
                if (target == null) { TowerProjectiles.RemoveAt(i); continue; }
                projectile.Impact = Position(target.Distance);
                Vector2 hitPoint = ProjectileVisuals.EnemyHitPoint(target, projectile.Impact);
                if (!projectile.Visual.Initialized)
                    projectile.Visual.Begin(projectile.Position, ProjectileVisuals.TowerSocket(projectile.Start, projectile.Kind, projectile.Level, projectile.VisualSocketIndex), hitPoint);
                float travel = Systems.towerProjectileSpeed * step;
                if (Vector2.Distance(projectile.Position, projectile.Impact) <= travel)
                {
                    projectile.Visual.Advance(projectile.Position, projectile.Impact, projectile.Impact, hitPoint, true);
                    bool landed = false;
                    if (projectile.Kind == TowerKind.Ember)
                    {
                        foreach (Enemy enemy in Enemies)
                            if (enemy.Targetable && Vector2.Distance(Position(enemy.Distance), projectile.Impact) <= projectile.SplashRadius)
                                landed |= ApplyHit(enemy, projectile.Damage, true, false);
                    }
                    else landed = Hit(target, projectile.Damage, false);
                    ProjectileImpacts.Add(new ProjectileImpact { Position = hitPoint, Direction = projectile.Visual.Direction,
                        Kind = projectile.Kind == TowerKind.Ember ? "fireball" : "arrow", Landed = landed });
                    TowerProjectiles.RemoveAt(i);
                }
                else
                {
                    Vector2 previous = projectile.Position;
                    projectile.Position = Vector2.MoveTowards(previous, projectile.Impact, travel);
                    projectile.Visual.Advance(previous, projectile.Position, projectile.Impact, hitPoint, false);
                }
            }
        }
    }
}
