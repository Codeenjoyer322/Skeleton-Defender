using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkeletonDefender
{
    // Fixed-tick presentation state. Age is real simulation time; PlaybackRate maps it
    // to authored frame time. Damage is scheduled from the same scaled contact marker.
    public sealed class CombatAnimationState
    {
        public string Action { get; private set; } = "idle";
        public float Age { get; private set; }
        public float Duration { get; private set; }
        public float PlaybackRate { get; private set; } = 1;
        public bool FacingLeft;
        public Direction8 FacingDirection = Direction8.East;
        public bool Moving;
        public int Serial { get; private set; }
        public bool ManualCast { get; private set; }
        public bool Terminal { get; private set; }
        public bool Protected { get; private set; }
        public bool IsBusy => Terminal || (Age < Duration && Action != "idle" && Action != "walk" && Action != "hop");
        public bool LocksMovement => IsBusy && Protected;
        public float ClipAge => Age * PlaybackRate;

        public float Play(string action, float authoredDuration, float maximumDuration = 0,
            bool protect = false, bool manual = false, bool terminal = false, float playbackMultiplier = 1)
        {
            authoredDuration = Mathf.Max(.01f, authoredDuration);
            float desiredDuration = authoredDuration / Mathf.Max(.01f, playbackMultiplier);
            Duration = maximumDuration > 0 ? Mathf.Min(desiredDuration, maximumDuration) : desiredDuration;
            PlaybackRate = authoredDuration / Duration;
            Action = action; Age = 0; Serial++; ManualCast = manual; Terminal = terminal; Protected = protect;
            return PlaybackRate;
        }
        public void Advance(float dt)
        {
            Age = Terminal ? Mathf.Min(Duration, Age + dt) : Age + dt;
            if (!Terminal && IsLocomotion) Age %= 3600f;
        }
        private bool IsLocomotion => Action == "idle" || Action == "walk" || Action == "hop";
        public void Locomotion(bool moving, bool left, bool frog = false)
        {
            Moving = moving;
            if (IsBusy) return;
            FacingLeft = left;
            string next = frog ? "hop" : moving ? "walk" : "idle";
            if (Action != next) Play(next, 1);
        }
        public void Reset() { Terminal = false; Protected = false; ManualCast = false; Moving = false; Play("idle", 1); }
    }

    public sealed class TowerArcherState
    {
        public readonly CombatAnimationState Animation = new CombatAnimationState();
        public int Index, TargetId;
        public Vector2 TargetPoint;
    }

    public sealed class EnemyCorpse
    {
        public Enemy Enemy;
        public string Role;
        public Vector2 Position;
        public float Remaining, Lifetime;
        public CombatAnimationState Animation => Enemy.Animation;
    }
    public sealed class HeroCorpse
    {
        public HeroCombatState Hero;
        public float Remaining, Lifetime;
        public Vector2 Position => Hero.Position;
        public CombatAnimationState Animation => Hero.Animation;
    }

    public static class CombatAnimationRoles
    {
        public static string Hero(HeroCombatState hero) => hero.Kind == HeroKind.Circe ? "circe" : "achilles";
        public static string Enemy(Enemy enemy)
        {
            if (enemy.IsFrog) return "blue_frog";
            if (enemy.Skeleton == SkeletonKind.Sarcophagus && !enemy.HasSarcophagus) return "sarcophagus_bare";
            if (enemy.IsSummoned && enemy.AnimationRole == "mini_mummy") return "mini_mummy";
            switch (enemy.Skeleton)
            {
                case SkeletonKind.TRex: return "trex";
                case SkeletonKind.Tutankhamun: return "tutankhamun";
                default: return enemy.Skeleton.ToString().ToLowerInvariant();
            }
        }
    }

    public sealed partial class GameModel
    {
        private sealed class AnimationContact
        {
            public float Remaining;
            public HeroCombatState Hero;
            public Enemy Enemy;
            public Tower Tower;
            public TowerArcherState Archer;
            public int TowerLevel;
            public int LifeSerial, AnimationSerial;
            public Action Resolve;
            public Action CancelForMovement;
        }
        private readonly List<AnimationContact> animationContacts = new List<AnimationContact>();
        public readonly List<EnemyCorpse> EnemyCorpses = new List<EnemyCorpse>();
        public readonly List<HeroCorpse> HeroCorpses = new List<HeroCorpse>();
        public int PendingAnimationContacts => animationContacts.Count;

        private static float ClipDuration(string role, string action, CombatAnimationState animation, float fallback)
            => AnimationLibrary.GetDirectional(role, action, animation.FacingDirection, animation.FacingLeft)?.Duration ?? fallback;
        private static float ClipContact(string role, string action, CombatAnimationState animation, float fallback)
        {
            var clip = AnimationLibrary.GetDirectional(role, action, animation.FacingDirection, animation.FacingLeft);
            return clip != null && clip.ContactTime > 0 ? clip.ContactTime : fallback;
        }
        private float AnimateHero(HeroCombatState actor, string action, float fallbackDuration,
            float fallbackContact, float interval = 0, bool manual = false, float playbackMultiplier = 1)
        {
            string role = CombatAnimationRoles.Hero(actor);
            actor.Animation.FacingLeft = actor.FacingLeft;
            float duration = ClipDuration(role, action, actor.Animation, fallbackDuration);
            float rate = actor.Animation.Play(action, duration, interval, true, manual, false, playbackMultiplier);
            actor.Animation.FacingLeft = actor.FacingLeft; actor.Animation.Moving = false;
            actor.CastPoseRemaining = manual ? actor.Animation.Duration : actor.CastPoseRemaining;
            ActorActivity(actor);
            return Mathf.Clamp(ClipContact(role, action, actor.Animation, fallbackContact) / rate, 0, actor.Animation.Duration);
        }
        private float AnimateEnemy(Enemy enemy, string action, float fallbackDuration = 1,
            float fallbackContact = .4f, float interval = 0, HeroCombatState target = null)
        {
            enemy.AnimationTarget = target;
            enemy.AnimationTargetLifeSerial = target == null ? 0 : target.LifeSerial;
            if (target != null) FaceEnemy(enemy, target.Position);
            string role = CombatAnimationRoles.Enemy(enemy);
            float duration = ClipDuration(role, action, enemy.Animation, fallbackDuration);
            float rate = enemy.Animation.Play(action, duration, interval, true); enemy.Animation.Moving = false;
            return Mathf.Clamp(ClipContact(role, action, enemy.Animation, fallbackContact) / rate, 0, enemy.Animation.Duration);
        }
        private void QueueContact(HeroCombatState actor, float delay, Action resolve, Action cancelForMovement = null)
        {
            animationContacts.Add(new AnimationContact { Hero = actor, LifeSerial = actor.LifeSerial,
                AnimationSerial = actor.Animation.Serial, Remaining = actor.Animation.Age + delay, Resolve = resolve, CancelForMovement = cancelForMovement });
        }
        private void CancelAutomaticAction(HeroCombatState actor)
        {
            // A requested active skill completes, then the retained destination resumes.
            // Ordinary attacks and automatic procs yield immediately to an explicit order
            // or a newly confirmed manual skill. Invalid skill inputs never reach here.
            if (actor.Animation.ManualCast && (actor.Animation.IsBusy || HasPendingContact(actor))) return;
            bool canceledCurrent = false;
            for (int i = animationContacts.Count - 1; i >= 0; i--)
            {
                var contact = animationContacts[i];
                if (contact.Hero != actor) continue;
                bool current = contact.LifeSerial == actor.LifeSerial && contact.AnimationSerial == actor.Animation.Serial;
                animationContacts.RemoveAt(i);
                if (current) { canceledCurrent = true; contact.CancelForMovement?.Invoke(); }
            }
            // A delayed volley member has not left the staff yet. Already initialized
            // projectiles are independent and keep flying toward their original targets.
            Projectiles.RemoveAll(projectile => projectile.Source == actor && !projectile.Visual.Initialized);
            if (canceledCurrent || actor.Animation.Protected)
            {
                actor.Animation.Reset();
                actor.CastPoseRemaining = 0;
            }
        }
        private void QueueContact(Enemy enemy, float delay, Action resolve)
        {
            animationContacts.Add(new AnimationContact { Enemy = enemy, AnimationSerial = enemy.Animation.Serial,
                Remaining = enemy.Animation.Age + delay, Resolve = resolve });
        }
        private void QueueContact(Tower tower, TowerArcherState archer, int level, float delay, Action resolve)
        {
            animationContacts.Add(new AnimationContact { Tower = tower, Archer = archer, TowerLevel = level,
                AnimationSerial = archer.Animation.Serial, Remaining = archer.Animation.Age + delay, Resolve = resolve });
        }
        private void FaceTowerArcher(Tower tower, TowerArcherState archer, Enemy target)
        {
            archer.TargetPoint = ProjectileVisuals.EnemyHitPoint(target, Position(target.Distance));
            archer.Animation.FacingDirection = ProjectileVisuals.TowerArcherDirection(Sites[tower.Site], tower.Level, archer.Index, archer.TargetPoint);
            float dx = archer.TargetPoint.x - ProjectileVisuals.TowerArcherGround(Sites[tower.Site], tower.Level, archer.Index).x;
            if (Mathf.Abs(dx) > .001f) archer.Animation.FacingLeft = dx < 0;
        }
        private void UpdateAnimationContacts(float dt)
        {
            // Iterate a stable prefix: a release can enqueue later sub-effects without
            // executing them twice during a large step.
            for (int i = animationContacts.Count - 1; i >= 0; i--)
            {
                AnimationContact pending = animationContacts[i];
                bool valid = ContactValid(pending);
                if (!valid) { animationContacts.RemoveAt(i); continue; }
                float age = pending.Hero != null ? pending.Hero.Animation.Age : pending.Archer != null ? pending.Archer.Animation.Age : pending.Enemy.Animation.Age;
                if (age + .00001f < pending.Remaining) continue;
                animationContacts.RemoveAt(i);
                int effectStart = AbilityEffects.Count, heroShotStart = Projectiles.Count, enemyShotStart = EnemyProjectiles.Count, towerShotStart = TowerProjectiles.Count;
                pending.Resolve();
                float remainder = Mathf.Max(0, age - pending.Remaining);
                for (int n = effectStart; n < AbilityEffects.Count; n++) AbilityEffects[n].InitialAdvance = remainder;
                for (int n = heroShotStart; n < Projectiles.Count; n++) Projectiles[n].InitialAdvance = remainder;
                for (int n = enemyShotStart; n < EnemyProjectiles.Count; n++) EnemyProjectiles[n].InitialAdvance = remainder;
                for (int n = towerShotStart; n < TowerProjectiles.Count; n++) TowerProjectiles[n].InitialAdvance = remainder;
            }
            // A later contact in this same tick can kill/knock down the owner of an
            // earlier queued cast. Remove it now, before exposing pending state to UI.
            animationContacts.RemoveAll(pending => !ContactValid(pending));
        }
        private bool ContactValid(AnimationContact pending)
            => pending.Tower != null ? Towers.Contains(pending.Tower) && pending.Tower.Level == pending.TowerLevel &&
                pending.Archer.Animation.Serial == pending.AnimationSerial && Enemies.Exists(enemy => enemy.Id == pending.Archer.TargetId && enemy.Targetable)
                : pending.Hero != null
                ? pending.Hero.Alive && pending.Hero.KnockdownRemaining <= 0 && pending.Hero.LifeSerial == pending.LifeSerial && pending.Hero.Animation.Serial == pending.AnimationSerial
                : pending.Enemy != null && !pending.Enemy.Dead && !pending.Enemy.IsFrog && pending.Enemy.Animation.Serial == pending.AnimationSerial;
        private bool HasPendingContact(HeroCombatState hero)
        {
            foreach (var item in animationContacts)
                if (item.Hero == hero && item.LifeSerial == hero.LifeSerial && item.AnimationSerial == hero.Animation.Serial) return true;
            return false;
        }
        private bool HasPendingContact(Enemy enemy)
        {
            foreach (var item in animationContacts)
                if (item.Enemy == enemy && item.AnimationSerial == enemy.Animation.Serial) return true;
            return false;
        }
        private void AdvanceAnimations(float dt)
        {
            Hero.Animation.Advance(dt);
            if (Clone != null) Clone.Animation.Advance(dt);
            foreach (Enemy enemy in Enemies) if (!enemy.Dead) enemy.Animation.Advance(dt);
            foreach (Tower tower in Towers) if (tower.Kind == TowerKind.Archer)
                foreach (TowerArcherState archer in tower.Archers) archer.Animation.Advance(dt);
            for (int i = EnemyCorpses.Count - 1; i >= 0; i--)
            {
                EnemyCorpse corpse = EnemyCorpses[i]; corpse.Animation.Advance(dt); corpse.Remaining -= dt;
                if (corpse.Remaining <= 0) EnemyCorpses.RemoveAt(i);
            }
            for (int i = HeroCorpses.Count - 1; i >= 0; i--)
            {
                HeroCorpse corpse = HeroCorpses[i]; corpse.Animation.Advance(dt); corpse.Remaining -= dt;
                if (corpse.Remaining <= 0) HeroCorpses.RemoveAt(i);
            }
        }
        private void FaceEnemy(Enemy enemy, Vector2 destination)
        {
            Vector2 direction = destination - Position(enemy.Distance);
            enemy.Animation.FacingDirection = AnimatedActors.DirectionFor(direction, enemy.Animation.FacingDirection);
            float x = direction.x;
            if (Mathf.Abs(x) > .001f) enemy.Animation.FacingLeft = x < 0;
        }
        private void FollowEnemyAnimationTarget(Enemy enemy, bool locked)
        {
            if (!locked) { enemy.AnimationTarget = null; return; }
            HeroCombatState target = enemy.AnimationTarget;
            // A closer actor cannot visually steal an already committed attack. Follow
            // its original living target through contact/recovery; keep the last pose
            // if that life ended or a mirror copy was removed from the battlefield.
            if (target == null || !target.Alive || target.LifeSerial != enemy.AnimationTargetLifeSerial ||
                (!ReferenceEquals(target, Hero) && !ReferenceEquals(target, Clone))) return;
            FaceEnemy(enemy, target.Position);
        }
        private void EnemyHurtAnimation(Enemy enemy)
        {
            if (enemy.Animation.IsBusy && enemy.Animation.Protected) return;
            string role = CombatAnimationRoles.Enemy(enemy);
            enemy.Animation.Play("hurt", ClipDuration(role, "hurt", enemy.Animation, .16f));
        }
        private void HeroHurtAnimation(HeroCombatState hero)
        {
            if (hero.Animation.IsBusy && hero.Animation.Protected) return;
            hero.Animation.Play("hurt", ClipDuration(CombatAnimationRoles.Hero(hero), "hurt", hero.Animation, .16f));
        }
        private void AddEnemyCorpse(Enemy enemy, string role = null)
        {
            role = role ?? CombatAnimationRoles.Enemy(enemy);
            string action = role == "blue_frog" ? "dissolve" : "death";
            float duration = ClipDuration(role, action, enemy.Animation, 1.2f);
            enemy.Animation.Play(action, duration, 0, true, false, true);
            float lifetime = role == "sarcophagus" ? (AnimationLibrary.GetDirectional(role, action, enemy.Animation.FacingDirection, enemy.Animation.FacingLeft)?.FindEvent("phase_two_ready")?.TimeSeconds ?? 1.21f) : duration + .6f;
            EnemyCorpses.Add(new EnemyCorpse { Enemy = enemy, Role = role, Position = Position(enemy.Distance), Lifetime = lifetime, Remaining = lifetime });
            // A crowded wave cannot grow the presentation list without bound.
            if (EnemyCorpses.Count > 128) EnemyCorpses.RemoveAt(0);
        }
        private void HeroDeathAnimation(HeroCombatState actor)
        {
            actor.Animation.Play("death", ClipDuration(CombatAnimationRoles.Hero(actor), "death", actor.Animation, 1.5f), 0, true, false, true);
            if (actor.IsClone)
            {
                float lifetime = actor.Animation.Duration + .6f;
                HeroCorpses.Add(new HeroCorpse { Hero = actor, Lifetime = lifetime, Remaining = lifetime });
                if (HeroCorpses.Count > 8) HeroCorpses.RemoveAt(0);
            }
        }
    }
}
