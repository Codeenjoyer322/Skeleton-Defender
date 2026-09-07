using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SkeletonDefender.Editor
{
    // Pure model fixtures: no player profile or save files are opened.
    public static class AnimationCombatTests
    {
        private static int checks;
        private static void Check(bool value, string message)
        { checks++; if (!value) throw new Exception("ANIMATION COMBAT: " + message); }
        private static bool Near(float a, float b, float tolerance = .002f) => Mathf.Abs(a - b) < tolerance;
        private static void Advance(GameModel model, float duration)
        {
            while (duration > .000001f) { float dt = Mathf.Min(duration, GameModel.Tick); model.Step(dt); duration -= dt; }
        }
        private static object Call(GameModel model, string method, params object[] arguments)
            => typeof(GameModel).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(model, arguments);
        private static GameModel Fixture(HeroKind kind = HeroKind.Circe, EquipmentStats equipment = null)
        {
            var model = new GameModel(1, kind, equipment ?? new EquipmentStats(), 871);
            model.StartWave(); Silence(model.Hero);
            model.Hero.Position = model.Hero.Destination = new Vector2(950, 580);
            return model;
        }
        private static void Silence(HeroCombatState hero)
        { hero.AttackCooldown = hero.FrogCooldown = hero.SunCooldown = hero.DecapitateCooldown = 9999; }
        private static Enemy Target(GameModel model, SkeletonKind kind = SkeletonKind.Normal, float distance = 300)
        {
            var enemy = model.CreateEnemy(kind, distance);
            enemy.Hp = enemy.MaxHp = 10000; enemy.AttackCooldown = enemy.SpecialCooldown = 9999;
            model.Enemies.Add(enemy); return enemy;
        }

        [MenuItem("Skeleton Defender/Check animation combat contacts")]
        public static void Run()
        {
            checks = 0; BalanceData.Reload();
            Check(AnimationLibrary.IsAvailable, "Imported catalog unavailable");
            AuthoredMarkers();
            BasicContact(false); BasicContact(true);
            StaffReleaseAndMovingTarget();
            EnemyMeleeAndCancellation();
            EnemyCommittedTargetFacing();
            EnemySpecialCommittedTargetFacing();
            ManualCostsSnapshotAndPause();
            SpearAndRainRelease();
            DeathCancelRespawnAndCorpses();
            DeathFramesRemainTerminalInEveryDirection();
            FrogAndCoffin();
            CanceledHexCooldown();
            foreach (HeroKind kind in new[] { HeroKind.Circe, HeroKind.Achilles })
                foreach (bool clone in new[] { false, true }) CommandedMovementHasPriority(kind, clone);
            MovementCancelsAutomaticAnticipation();
            MovementPreservesReleasedProjectiles();
            MovementWaitsForManualSkill();
            HexWaitsForStaffVolley();
            ManualSkillInterruptsOnlyUnreleasedAutomaticShots();
            StormDamageOverTime();
            StormSnapshotAndSourceLifetime();
            DeerFullRouteAndContacts();
            SlowerHeelArrowPreservesCombatRules();
            DirectionalReleaseAnchorIsFrozen();
            WarlockSmokeKeepsReleaseAnchor();
            string path = Path.Combine(Path.GetDirectoryName(Application.dataPath), "work", "animation-combat-results.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, "{\"status\":\"passed\",\"assertions\":" + checks + ",\"playerSaveTouched\":false}");
            Debug.Log("SKELETON_ANIMATION_COMBAT_PASSED: " + checks + " deterministic assertions.");
        }
        private static void WarlockSmokeKeepsReleaseAnchor()
        {
            foreach (Direction8 direction in Enum.GetValues(typeof(Direction8)))
            {
                var model = Fixture(); var warlock = Target(model, SkeletonKind.Warlock);
                float angle = (int)direction * Mathf.PI / 4;
                Vector2 ground = model.Position(warlock.Distance);
                model.Hero.Position = model.Hero.Destination = ground + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 80;
                float previousChance = warlock.Variant.lightningChance;
                try
                {
                    warlock.Variant.lightningChance = 0; warlock.SpecialCooldown = 0;
                    Call(model, "UpdateEnemyAbility", warlock, 0f);
                    Check(warlock.Animation.Action == "cast_fail" && warlock.Animation.FacingDirection == direction, "Fizzle fixture did not select actual cast direction");
                    var clip = AnimationLibrary.GetDirectional("warlock", "cast_fail", direction, warlock.Animation.FacingLeft);
                    Vector2 expected = ProjectileVisuals.EnemySkillReleasePoint(warlock, ground, "cast_fail", "fizzle_release");
                    Advance(model, clip.ContactTime + .001f);
                    var smoke = model.EnemyEffects.Find(effect => effect.Kind == "smoke" && effect.Source == warlock);
                    Check(smoke != null && smoke.UsesVisualAnchors && Vector2.Distance(smoke.Position, expected) < .001f,
                        "Fizzle smoke did not freeze at its release socket");
                    warlock.Distance += 250; warlock.Animation.FacingDirection = (Direction8)(((int)direction + 4) % 8);
                    warlock.Animation.FacingLeft = !warlock.Animation.FacingLeft; warlock.Animation.Play("walk", 1);
                    var rendererAnchor = (Vector2)typeof(SkeletonGame).GetMethod("EnemyEffectSocket", BindingFlags.Static | BindingFlags.NonPublic)
                        .Invoke(null, new object[] { smoke, "cast_fail", "fizzle_release" });
                    Check(Vector2.Distance(rendererAnchor, expected) < .001f && Vector2.Distance(smoke.Position, expected) < .001f,
                        "Existing smoke followed the caster's later movement or opposite-facing pose");
                }
                finally { warlock.Variant.lightningChance = previousChance; }
            }
        }
        private static void AuthoredMarkers()
        {
            Check(Near(AnimationLibrary.Get("achilles", "divine_spear").ContactTime, .72f), "Q marker");
            Check(Near(AnimationLibrary.Get("achilles", "heel_arrow").ContactTime, 1.18f), "E marker");
            Check(Near(AnimationLibrary.Get("circe", "staff_attack").ContactTime, .55f), "Staff marker");
            foreach (SkeletonKind kind in Enum.GetValues(typeof(SkeletonKind)))
            {
                var model = Fixture(); var enemy = model.CreateEnemy(kind);
                string role = CombatAnimationRoles.Enemy(enemy);
                foreach (bool left in new[] { false, true })
                {
                    var clip = AnimationLibrary.Get(role, "attack", left);
                    Check(clip != null && clip.ContactTime > 0 && clip.ContactTime < clip.Duration, role + " missing attack contact");
                    Check(AnimationLibrary.Get(role, "death", left) != null, role + " missing death");
                }
            }
        }
        private static void BasicContact(bool rage)
        {
            GameModel model = Fixture(HeroKind.Achilles); Enemy enemy = Target(model);
            model.Hero.Position = model.Hero.Destination = model.Position(enemy.Distance) + new Vector2(0, 10);
            model.Hero.AttackCooldown = 0;
            if (rage) model.Hero.RageRemaining = 10;
            model.Step(GameModel.Tick);
            Check(model.Hero.Animation.Action == "attack" && Near(enemy.Hp, 10000), "Melee hit before attack frame");
            float contact = AnimationLibrary.Get("achilles", "attack").ContactTime / model.Hero.Animation.PlaybackRate;
            float damage = model.Hero.Damage * (1 - enemy.PhysicalArmor);
            Advance(model, contact - .001f);
            Check(Near(enemy.Hp, 10000), "Melee anticipation applied damage");
            Advance(model, .002f);
            Check(Near(enemy.Hp, 10000 - damage), "Contact did not apply one melee hit");
            float hp = enemy.Hp;
            Advance(model, .03f);
            Check(Near(enemy.Hp, hp), "Contact applied twice");
            Check(model.Hero.Animation.Duration <= model.Hero.AttackInterval + .001f, "Animation slows rage attack cadence");
        }
        private static void StaffReleaseAndMovingTarget()
        {
            var model = Fixture(); Enemy enemy = Target(model);
            model.Hero.Position = model.Hero.Destination = model.Position(enemy.Distance) + new Vector2(0, 120);
            model.Hero.AttackCooldown = 0;
            model.Step(GameModel.Tick);
            float release = .55f / model.Hero.Animation.PlaybackRate;
            Advance(model, release - .001f);
            Check(model.Projectiles.Count == 0 && Near(enemy.Hp, 10000), "Staff created a projectile during anticipation");
            Advance(model, .002f);
            Check(model.Projectiles.Count > 0 && model.Projectiles[0].Visual.Initialized, "Release lacks visible projectile origin");
            model.Hero.AttackCooldown = 9999;
            float hp = enemy.Hp; int changes = 0;
            // Freeze passive proc randomness: fixture seed has no triple shot at this release.
            int released = model.Projectiles.Count;
            enemy.Distance += 25;
            for (int i = 0; i < 180 && model.Projectiles.Count > 0; i++)
            {
                float before = enemy.Hp; model.Step(GameModel.Tick);
                if (!Near(before, enemy.Hp)) changes++;
            }
            Check(enemy.Hp < hp && changes <= released, "Moving target lost homing contact or took duplicate hits");
            Check(model.Projectiles.Count == 0, "Released projectile never resolved");
        }
        private static void EnemyMeleeAndCancellation()
        {
            var model = Fixture(); Enemy enemy = Target(model);
            model.Hero.Position = model.Hero.Destination = model.Position(enemy.Distance) + new Vector2(0, 10);
            enemy.AttackCooldown = 0; float hp = model.Hero.Hp;
            model.Step(GameModel.Tick);
            float contact = AnimationLibrary.Get("normal", "attack").ContactTime / enemy.Animation.PlaybackRate;
            Advance(model, contact - .001f);
            Check(Near(model.Hero.Hp, hp), "Enemy hit before contact");
            Advance(model, .002f);
            Check(Near(model.Hero.Hp, hp - enemy.AttackDamage), "Enemy contact damage missing");

            var canceled = Fixture(); Enemy dying = Target(canceled);
            canceled.Hero.Position = canceled.Hero.Destination = canceled.Position(dying.Distance);
            dying.AttackCooldown = 0; canceled.Step(GameModel.Tick); hp = canceled.Hero.Hp;
            Call(canceled, "Kill", dying); Advance(canceled, .8f);
            Check(Near(canceled.Hero.Hp, hp), "Dead enemy released a melee hit");
        }
        private static void EnemyCommittedTargetFacing()
        {
            foreach (Direction8 direction in Enum.GetValues(typeof(Direction8)))
                foreach (bool targetClone in new[] { false, true })
                {
                    var model = Fixture(HeroKind.Circe, new EquipmentStats { Artifact = ArtifactKind.AthenaMirror });
                    Check(model.TryUseArtifact(), "Facing fixture could not create mirror copy");
                    Silence(model.Clone);
                    var enemy = Target(model);
                    HeroCombatState target = targetClone ? model.Clone : model.Hero;
                    HeroCombatState other = targetClone ? model.Hero : model.Clone;
                    Vector2 ground = model.Position(enemy.Distance);
                    float angle = (int)direction * Mathf.PI / 4;
                    Vector2 vector = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    target.Position = target.Destination = ground + vector * 16;
                    other.Position = other.Destination = ground - vector * 30;
                    enemy.AttackCooldown = 0; model.Step(GameModel.Tick);
                    Check(enemy.Animation.Action == "attack" && enemy.Animation.FacingDirection == direction,
                        "Enemy did not begin by facing its actual selected actor");
                    enemy.AttackCooldown = 9999;
                    float contact = AnimationLibrary.GetDirectional("normal", "attack", direction, enemy.Animation.FacingLeft).ContactTime / enemy.Animation.PlaybackRate;
                    float targetHp = target.Hp, otherHp = other.Hp;
                    Direction8 movedDirection = (Direction8)(((int)direction + 2) % 8);
                    float movedAngle = (int)movedDirection * Mathf.PI / 4;
                    vector = new Vector2(Mathf.Cos(movedAngle), Mathf.Sin(movedAngle));
                    target.Position = target.Destination = ground + vector * 30;
                    other.Position = other.Destination = ground - vector * 5;
                    Advance(model, contact * .5f);
                    Check(enemy.Animation.FacingDirection == movedDirection && Near(target.Hp, targetHp) && Near(other.Hp, otherHp),
                        "Closer second actor stole the pending attack facing or caused an early hit");
                    Advance(model, contact * .5f + .001f);
                    float incoming = enemy.AttackDamage * (targetClone ? BalanceData.Current.heroSystems.cloneIncomingDamageMultiplier : 1);
                    Check(Near(target.Hp, targetHp - incoming) && Near(other.Hp, otherHp),
                        "Swapping actor distances changed the committed hit recipient or damage");
                    Direction8 last = enemy.Animation.FacingDirection;
                    if (targetClone) Call(model, "RemoveClone");
                    else Call(model, "DamageActor", target, target.Hp, false, true);
                    target.Position = target.Destination = ground + new Vector2(0, -30);
                    float recovery = enemy.Animation.Duration - enemy.Animation.Age;
                    Advance(model, Mathf.Min(.02f, recovery * .5f));
                    Check(enemy.Animation.FacingDirection == last,
                        "Dead/removed target changed facing or allowed another actor to steal recovery");
                    Advance(model, enemy.Animation.Duration - enemy.Animation.Age + GameModel.Tick);
                    enemy.AttackCooldown = 0; model.Step(GameModel.Tick);
                    Direction8 next = (Direction8)(((int)movedDirection + 4) % 8);
                    Check(enemy.Animation.Action == "attack" && enemy.Animation.FacingDirection == next,
                        "Next attack failed to acquire the surviving nearer actor after recovery");
                }
        }
        private static void EnemySpecialCommittedTargetFacing()
        {
            foreach (SkeletonKind kind in new[] { SkeletonKind.Pirate, SkeletonKind.Warlock })
            {
                var model = Fixture(HeroKind.Circe, new EquipmentStats { Artifact = ArtifactKind.AthenaMirror });
                Check(model.TryUseArtifact(), "Special-facing fixture could not create mirror copy");
                Silence(model.Clone);
                var enemy = Target(model, kind);
                Vector2 ground = model.Position(enemy.Distance);
                model.Hero.Position = model.Hero.Destination = ground + new Vector2(80, 0);
                model.Clone.Position = model.Clone.Destination = ground + new Vector2(-120, 0);
                float chance = enemy.Variant.lightningChance;
                try
                {
                    if (kind == SkeletonKind.Warlock) enemy.Variant.lightningChance = 0;
                    enemy.SpecialCooldown = 0; model.Step(GameModel.Tick);
                    string action = kind == SkeletonKind.Pirate ? "pistol_shot" : "cast_fail";
                    Check(enemy.Animation.Action == action && enemy.Animation.FacingDirection == Direction8.East,
                        "Special did not initially face its chosen ranged actor");
                    float contact = AnimationLibrary.GetDirectional(CombatAnimationRoles.Enemy(enemy), action, Direction8.East, false).ContactTime;
                    model.Hero.Position = model.Hero.Destination = ground + new Vector2(0, -80);
                    model.Clone.Position = model.Clone.Destination = ground + new Vector2(-5, 0);
                    float heroHp = model.Hero.Hp, cloneHp = model.Clone.Hp;
                    Advance(model, contact * .5f);
                    Check(enemy.Animation.FacingDirection == Direction8.North,
                        "A nearby clone redirected a committed ranged casting pose");
                    Advance(model, contact * .5f + .001f);
                    Check(enemy.Animation.FacingDirection == Direction8.North && Near(model.Clone.Hp, cloneHp),
                        "Ranged release turned toward or damaged the wrong actor");
                    if (kind == SkeletonKind.Pirate)
                    {
                        Check(model.EnemyProjectiles.Count == 1 && model.EnemyProjectiles[0].Target == model.Hero,
                            "Pistol release retargeted the projectile to the closer copy");
                        Advance(model, .4f);
                        Check(Near(model.Hero.Hp, heroHp - enemy.Variant.projectileDamage) && Near(model.Clone.Hp, cloneHp),
                            "Committed pistol shot missed, changed recipient, or duplicated damage");
                    }
                    else
                    {
                        Check(model.EnemyEffects.Exists(effect => effect.Source == enemy && effect.Kind == "smoke") && Near(model.Hero.Hp, heroHp),
                            "Fizzle direction tracking changed the cast result");
                    }
                }
                finally { enemy.Variant.lightningChance = chance; }
            }
        }
        private static void ManualCostsSnapshotAndPause()
        {
            var model = Fixture(); Enemy original = Target(model), boss = Target(model, SkeletonKind.Boss, 500);
            Check(model.TryCastSkill(1), "Storm rejected");
            Check(Near(model.Hero.Mana, 0) && Near(model.Hero.ManualCooldowns[1], 300), "Resources not charged once at cast input");
            Enemy later = Target(model, SkeletonKind.Normal, 700);
            model.SetPaused(true); model.Step(10);
            Check(Near(model.Hero.Animation.Age, 0) && Near(original.Hp, 10000), "Pause advanced cast");
            model.SetPaused(false);
            Advance(model, .339f); Check(Near(original.Hp, 10000), "Storm damage before release");
            Advance(model, .002f);
            Check(Near(original.Hp, 10000), "Storm applied full damage at release instead of gradually");
            Check(!model.TryCastSkill(1), "Duplicate storm spent resources");
            Advance(model, 3.01f);
            Check(Near(original.Hp, 9990) && Near(boss.Hp, 10000) && Near(later.Hp, 10000), "Storm snapshot or boss immunity changed");
            Advance(model, 1); Check(Near(original.Hp, 9990), "Storm repeated a hit");
        }
        private static void SpearAndRainRelease()
        {
            var model = Fixture(HeroKind.Achilles); Enemy enemy = Target(model);
            Vector2 point = model.Position(enemy.Distance);
            Check(model.TryCastSkill(0, point), "Q input rejected");
            model.MoveHero(new Vector2(800, 580)); Vector2 standing = model.Hero.Position;
            model.Step(.719f);
            Check(model.AbilityEffects.Count == 0 && Near(enemy.Hp, 10000), "Spear appeared before .720 marker");
            Check(Vector2.Distance(model.Hero.Position, standing) < .001f, "Q lost its casting position");
            model.Step(.002f);
            var spear = model.AbilityEffects.Find(effect => effect.Kind == "spear");
            Check(spear != null && spear.Age < .003f, "Large step consumed the spear's flight before release");
            Check(Vector2.Distance(spear.End, point) < .001f, "Q target point was retargeted");

            var rain = Fixture(HeroKind.Achilles); Enemy original = Target(rain);
            Check(rain.TryCastSkill(1), "E input rejected"); Enemy later = Target(rain, SkeletonKind.Normal, 700);
            rain.Step(1.18f / .7f - .001f); Check(rain.AbilityEffects.Count == 0, "Heel arrow emitted before its slowed release marker");
            rain.Step(.002f);
            Check(rain.AbilityEffects.FindAll(effect => effect.Kind == "arrow_rain").Count == 1, "E did not preserve input snapshot");
            Check(rain.AbilityEffects.Exists(effect => effect.Kind == "arrow_rise"), "E has no unique rise/burst effect");
            Advance(rain, .65f / .7f + .01f);
            Check(Near(original.Hp, 9990) && Near(later.Hp, 10000), "E flight duplicated/retargeted hits");
        }
        private static void DeathCancelRespawnAndCorpses()
        {
            var model = Fixture(HeroKind.Circe, new EquipmentStats { Artifact = ArtifactKind.AegisOfDawn });
            Enemy enemy = Target(model);
            Check(model.TryCastSkill(1), "Death-cancel storm rejected");
            Call(model, "DamageActor", model.Hero, model.Hero.Hp, false, true);
            Check(!model.Hero.Alive && model.Hero.Animation.Action == "death", "Hero death animation missing");
            Advance(model, .5f);
            Check(Near(enemy.Hp, 10000), "Dead hero finished unreleased storm");
            Advance(model, 2.6f);
            Check(model.Hero.Alive && model.Hero.Animation.Action != "death", "Aegis respawn did not clear terminal animation");
            int gold = model.Gold, kills = model.Kills;
            Call(model, "Kill", enemy); Call(model, "Kill", enemy);
            Check(model.EnemyCorpses.Count == 1 && model.Kills == kills + 1 && model.Gold == gold + enemy.Reward, "Visual corpse duplicated rewards");
            model.Step(GameModel.Tick);
            Check(!model.Enemies.Contains(enemy) && model.EnemyCorpses.Count == 1, "Corpse stayed targetable in live roster");
            Advance(model, 5);
            Check(model.EnemyCorpses.Count == 0, "Expired corpse leaked");
        }
        private static void DeathFramesRemainTerminalInEveryDirection()
        {
            foreach (Direction8 direction in Enum.GetValues(typeof(Direction8)))
            {
                float angle = (int)direction * Mathf.PI / 4;
                Vector2 toward = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                foreach (HeroKind kind in new[] { HeroKind.Achilles, HeroKind.Circe })
                {
                    var model = Fixture(kind); var enemy = Target(model);
                    model.Hero.Position = model.Hero.Destination = model.Position(enemy.Distance) - toward * 10;
                    model.Hero.AttackCooldown = 0;
                    Call(model, "UpdateActorCombat", model.Hero, GameModel.Tick);
                    Check(model.Hero.Animation.Action == (kind == HeroKind.Circe ? "staff_attack" : "attack") && model.PendingAnimationContacts > 0,
                        "Terminal hold fixture did not begin an ordinary attack: " + kind + "/" + direction);
                    Check(model.Hero.Animation.FacingDirection == direction, "Ordinary attack selected wrong direction for death fixture");
                    Call(model, "DamageActor", model.Hero, model.Hero.Hp, false, true);
                    var death = AnimatedActors.HeroClip(model.Hero);
                    Check(death != null && death.Action == "death", "Dead hero renderer retained the attack clip");
                    Advance(model, death.Duration + .2f);
                    Check(!model.Hero.Alive && model.Hero.Animation.Terminal && model.Hero.Animation.Action == "death" &&
                        model.Hero.Animation.FacingDirection == direction, "Dead hero returned to locomotion/attack or changed facing before respawn");
                    Check(Near(model.Hero.Animation.ClipAge, death.Duration) && death.FrameAt(model.Hero.Animation.ClipAge, false) == death.Frames.Length - 1,
                        "Hero did not hold the final death frame");
                    Check(Near(enemy.Hp, 10000) && model.Projectiles.Count == 0 && model.PendingAnimationContacts == 0,
                        "Dead hero completed an unlaunched ordinary attack");
                    model.SetPaused(true); float age = model.Hero.Animation.Age; model.Step(8);
                    Check(Near(model.Hero.Animation.Age, age) && !model.Hero.Alive, "Paused dead hero advanced toward respawn");
                }
                var enemies = Fixture(); var dying = Target(enemies);
                enemies.Hero.Position = enemies.Hero.Destination = enemies.Position(dying.Distance) + toward * 10;
                Call(enemies, "BeginEnemyAttack", dying, enemies.Hero);
                Check(dying.Animation.Action == "attack" && dying.Animation.FacingDirection == direction, "Enemy death fixture did not start directional attack");
                float heroHp = enemies.Hero.Hp;
                Call(enemies, "Kill", dying);
                var corpse = enemies.EnemyCorpses.Find(item => item.Enemy == dying);
                var clip = AnimationLibrary.GetDirectional(corpse.Role, "death", direction, dying.Animation.FacingLeft);
                Advance(enemies, clip.Duration + .1f);
                Check(enemies.EnemyCorpses.Contains(corpse) && !enemies.Enemies.Contains(dying) && dying.Animation.Terminal && dying.Animation.Action == "death",
                    "Enemy corpse returned to attack/idle or disappeared before its terminal frame hold");
                Check(Near(dying.Animation.ClipAge, clip.Duration) && clip.FrameAt(dying.Animation.ClipAge, false) == clip.Frames.Length - 1 &&
                    Near(enemies.Hero.Hp, heroHp) && enemies.PendingAnimationContacts == 0, "Dead enemy looped its death or released a pending hit");
                Advance(enemies, .51f);
                Check(!enemies.EnemyCorpses.Contains(corpse), "Terminal frame hold prevented corpse cleanup");
            }
        }
        private static void CanceledHexCooldown()
        {
            var model = Fixture(); Enemy target = Target(model);
            model.Hero.FrogCooldown = 0;
            Check((bool)Call(model, "ActorTryHex", model.Hero, target), "Hex fixture was not accepted");
            Check(Near(model.Hero.FrogCooldown, 0), "Hex cooldown began before successful transformation");
            Call(model, "Kill", target);
            Advance(model, .2f);
            Check(Near(model.Hero.FrogCooldown, 0) && model.PendingAnimationContacts == 0, "Lost-target hex spent cooldown or stayed queued");
        }

        private static void CommandedMovementHasPriority(HeroKind kind, bool useClone)
        {
            GameModel model = Fixture(kind, useClone ? new EquipmentStats { Artifact = ArtifactKind.AthenaMirror } : null);
            if (useClone) Check(model.TryUseArtifact(), "Movement clone fixture rejected mirror");
            HeroCombatState actor = model.GetControlledHero(useClone);
            Silence(actor);
            Enemy enemy = Target(model);
            actor.Position = actor.Destination = model.Position(enemy.Distance) + new Vector2(0, 5);
            actor.AttackCooldown = actor.FrogCooldown = actor.SunCooldown = actor.DecapitateCooldown = 0;
            Vector2 destination = actor.Position + new Vector2(40, 0);
            float hp = actor.Hp, mana = actor.Mana;
            Check(model.MoveHero(destination, useClone), "Explicit movement rejected");
            int ticks = Mathf.CeilToInt(40 / actor.WalkSpeed / GameModel.Tick) + 3;
            for (int i = 0; i < ticks && actor.HasMoveOrder; i++)
            {
                Vector2 before = actor.Position;
                float expected = Mathf.Min(actor.WalkSpeed * GameModel.Tick, Vector2.Distance(before, destination));
                model.Step(GameModel.Tick);
                Check(Near(Vector2.Distance(before, actor.Position), expected, .001f), kind + "/" + useClone + " stopped to attack during a movement order");
                if (actor.HasMoveOrder)
                {
                    Check(!actor.Animation.Protected && model.PendingAnimationContacts == 0, "An automatic skill started while commanded moving");
                    Check(Near(enemy.Hp, 10000) && !enemy.IsFrog, "Movement emitted an automatic hit or hex");
                }
            }
            Check(!actor.HasMoveOrder && Vector2.Distance(actor.Position, destination) < .001f, "Hero failed to reach commanded destination");
            Check(actor.Animation.Protected && model.PendingAnimationContacts > 0, "Automatic combat did not resume at arrival");
            Check(Near(actor.Hp, hp) && Near(actor.Mana, mana), "A movement command changed health or mana");
            if (useClone) Check(Vector2.Distance(model.Hero.Position, new Vector2(950, 580)) < .001f, "Clone movement moved the original");
        }
        private static void MovementCancelsAutomaticAnticipation()
        {
            foreach (HeroKind kind in new[] { HeroKind.Circe, HeroKind.Achilles })
            {
                var model = Fixture(kind); Enemy enemy = Target(model);
                model.Hero.Position = model.Hero.Destination = model.Position(enemy.Distance) + new Vector2(0, 5);
                model.Hero.AttackCooldown = 0;
                model.Step(GameModel.Tick);
                float cooldown = model.Hero.AttackCooldown;
                Check(model.PendingAnimationContacts > 0, "Cancellation fixture did not start a basic attack");
                Check(model.MoveHero(model.Hero.Position + new Vector2(40, 0)), "Basic cancellation move rejected");
                Check(model.PendingAnimationContacts == 0 && !model.Hero.Animation.Protected, "Move retained an unlaunched basic attack");
                Check(Near(model.Hero.AttackCooldown, cooldown), "Move reset basic cooldown and permits attack cancellation abuse");
                Advance(model, .15f);
                Check(Near(enemy.Hp, 10000) && model.Hero.HasMoveOrder, "Canceled attack dealt damage or blocked departure");

                var passive = Fixture(kind); Enemy passiveTarget = Target(passive);
                passive.Hero.Position = passive.Hero.Destination = passive.Position(passiveTarget.Distance) + new Vector2(0, 5);
                if (kind == HeroKind.Circe) passive.Hero.SunCooldown = 0; else passive.Hero.DecapitateCooldown = 0;
                passive.Step(GameModel.Tick);
                Check(passive.MoveHero(passive.Hero.Position + new Vector2(40, 0)), "Passive cancellation move rejected");
                Check(passive.PendingAnimationContacts == 0 && Near(kind == HeroKind.Circe ? passive.Hero.SunCooldown : passive.Hero.DecapitateCooldown, 0), "Unused automatic skill lost its cooldown on movement cancellation");
            }
        }
        private static void MovementPreservesReleasedProjectiles()
        {
            var model = Fixture(); Enemy enemy = Target(model);
            model.Hero.Position = model.Hero.Destination = model.Position(enemy.Distance) + new Vector2(0, 120);
            Call(model, "ActorLaunch", model.Hero, enemy, ProjectileKind.Arcane, 17f, false, 0f);
            Call(model, "ActorLaunch", model.Hero, enemy, ProjectileKind.Fireball, 99f, false, .2f);
            HeroProjectile released = model.Projectiles[0];
            Check(released.Visual.Initialized && !model.Projectiles[1].Visual.Initialized, "Volley cancellation fixture has wrong release state");
            Check(model.MoveHero(model.Hero.Position + new Vector2(40, 0)), "Released projectile move rejected");
            Check(model.Projectiles.Count == 1 && model.Projectiles[0] == released, "Movement removed a launched projectile or kept an unreleased volley member");
            Advance(model, 1);
            Check(Near(enemy.Hp, 9983) && model.Projectiles.Count == 0, "Released projectile stopped homing or duplicated damage after departure");
        }
        private static void MovementWaitsForManualSkill()
        {
            foreach (HeroKind kind in new[] { HeroKind.Circe, HeroKind.Achilles })
            {
                var model = Fixture(kind); Target(model);
                Vector2 start = model.Hero.Position, destination = start + new Vector2(-40, 0);
                Check(model.TryCastSkill(1) && model.MoveHero(destination), "Manual-cast queued move rejected");
                float duration = model.Hero.Animation.Duration;
                Advance(model, duration - .01f);
                Check(Vector2.Distance(model.Hero.Position, start) < .001f && model.Hero.Destination == destination,
                    "Move interrupted a paid manual skill or lost its destination");
                Advance(model, .1f);
                Check(Vector2.Distance(model.Hero.Position, start) > 0 && model.Hero.HasMoveOrder,
                    "Movement did not resume after manual skill recovery");
            }
        }
        private static void ManualSkillInterruptsOnlyUnreleasedAutomaticShots()
        {
            var model = Fixture(); Enemy enemy = Target(model);
            model.Hero.Position = model.Hero.Destination = model.Position(enemy.Distance) + new Vector2(0, 120);
            model.Hero.Animation.Play("staff_attack", 1.16f, 0, true);
            Call(model, "ActorLaunch", model.Hero, enemy, ProjectileKind.Arcane, 17f, false, 0f);
            Call(model, "ActorLaunch", model.Hero, enemy, ProjectileKind.Fireball, 99f, false, .2f);
            HeroProjectile released = model.Projectiles[0];
            Check(model.TryCastSkill(1), "Manual override rejected during automatic staff recovery");
            Check(model.Hero.Animation.Action == "storm_cast" && model.Projectiles.Count == 1 && model.Projectiles[0] == released,
                "Manual skill retained an unlaunched automatic fireball or deleted the flying shot");
            Check(Near(model.Hero.Mana, 0), "Manual override charged incorrect mana");
            Advance(model, 3.4f);
            Check(Near(enemy.Hp, 9973) && model.Projectiles.Count == 0,
                "Automatic flight plus manual storm duplicated/lost contact after override");

            var invalid = Fixture(HeroKind.Achilles); Enemy victim = Target(invalid);
            invalid.Hero.Position = invalid.Hero.Destination = invalid.Position(victim.Distance) + new Vector2(0, 5);
            invalid.Hero.AttackCooldown = 0; invalid.Step(GameModel.Tick);
            int serial = invalid.Hero.Animation.Serial, pending = invalid.PendingAnimationContacts;
            Check(!invalid.TryCastSkill(0), "Untargeted Q was accepted");
            Check(invalid.Hero.Animation.Serial == serial && invalid.PendingAnimationContacts == pending && Near(invalid.Hero.Mana, 100),
                "Invalid Q canceled an automatic attack or spent mana");
        }
        private static void HexWaitsForStaffVolley()
        {
            SkillDefinition fire = BalanceData.Current.Hero((int)HeroKind.Circe).skills[2];
            SkillDefinition hex = BalanceData.Current.Hero((int)HeroKind.Circe).skills[0];
            float fireChance = fire.chance, hexChance = hex.chance;
            try
            {
                fire.chance = hex.chance = 1;
                var model = Fixture(); Enemy enemy = Target(model);
                model.Hero.Position = model.Hero.Destination = model.Position(enemy.Distance) + new Vector2(0, 120);
                model.Hero.AttackCooldown = model.Hero.FrogCooldown = 0;
                model.Step(GameModel.Tick);
                float release = .55f / model.Hero.Animation.PlaybackRate;
                Advance(model, release + .001f);
                Check(model.Hero.Animation.Action == "staff_attack" && model.Projectiles.Count == 4,
                    "Successful hex replaced the body pose on the staff-release frame");
                Advance(model, .21f);
                Check(model.Hero.Animation.Action == "staff_attack" && !model.Projectiles.Exists(p => p.Delay > 0),
                    "Hex interrupted a staggered fireball before it left the staff");
                float remaining = model.Hero.Animation.Duration - model.Hero.Animation.Age;
                Advance(model, remaining + .001f);
                Check(model.Hero.Animation.Action == "hex_cast" && !enemy.IsFrog && Near(model.Hero.FrogCooldown, 0),
                    "Deferred hex did not start after staff recovery or charged cooldown before success");
                Advance(model, .15f);
                Check(enemy.IsFrog && model.Hero.FrogCooldown > 29.9f, "Deferred hex failed its original one-time proc");
            }
            finally { fire.chance = fireChance; hex.chance = hexChance; }
        }
        private static void FrogAndCoffin()
        {
            var model = Fixture(); Enemy frog = Target(model);
            frog.FrogRemaining = .01f; model.Step(.02f);
            Check(model.EnemyCorpses.Count == 1 && model.EnemyCorpses[0].Role == "blue_frog" && frog.Animation.Action == "dissolve", "Hex expiry played skeleton death");
            Enemy coffin = Target(model, SkeletonKind.Sarcophagus, 700);
            Call(model, "Kill", coffin); model.Step(GameModel.Tick);
            Check(model.EnemyCorpses.Exists(c => c.Enemy == coffin && c.Role == "sarcophagus"), "Coffin death lost phase-one identity");
            Enemy bare = model.Enemies.Find(e => e.Skeleton == SkeletonKind.Sarcophagus && CombatAnimationRoles.Enemy(e) == "sarcophagus_bare");
            Check(bare != null && bare.AppearanceDelay > 1 && !bare.Targetable, "Rebirth did not reserve its hidden second phase");
            float before = bare.Distance; int remaining = model.Remaining;
            Advance(model, 1);
            Check(Near(before, bare.Distance) && bare.AppearanceDelay > 0 && model.Remaining >= remaining, "Hidden rebirth moved or disappeared from wave count");
            Advance(model, .25f);
            Check(bare.Targetable && bare.AppearanceDelay <= 0 && !model.EnemyCorpses.Exists(c => c.Enemy == coffin), "Coffin body and active bare skeleton overlap after emergence");
        }
        private static void StormDamageOverTime()
        {
            var model = Fixture(); Enemy original = Target(model), boss = Target(model, SkeletonKind.Boss, 500);
            original.Hp = original.MaxHp = 1000;
            Check(model.TryCastSkill(1), "Timed storm input rejected");
            Enemy later = Target(model, SkeletonKind.Normal, 700); later.Hp = later.MaxHp = 1000;
            model.Step(.34f);
            AbilityEffect storm = model.AbilityEffects.Find(effect => effect.Kind == "storm");
            Check(storm != null && Near(storm.DamageDuration, 3) && Near(storm.Lifetime, 3.34f), "Storm duration or final visual tail changed");
            Advance(model, .749f);
            Check(storm.DamageTicksResolved == 0 && Near(original.Hp, 1000), "Storm damage preceded its first 0.75-second tick");
            Advance(model, .002f);
            Check(storm.DamageTicksResolved == 1 && Near(original.Hp, 997.5f), "First storm tick is not one quarter of total damage");
            float age = storm.Age, hp = original.Hp;
            model.SetPaused(true); model.Step(20);
            Check(Near(storm.Age, age) && Near(original.Hp, hp) && storm.DamageTicksResolved == 1, "Pause advanced timed storm damage");
            model.SetPaused(false);
            for (int tick = 2; tick <= 4; tick++)
            {
                Advance(model, .75f);
                Check(storm.DamageTicksResolved == tick && Near(original.Hp, 1000 - 2.5f * tick), "Storm damage was not evenly distributed at tick " + tick);
            }
            Check(Near(original.Hp, 990) && Near(boss.Hp, 10000) && Near(later.Hp, 1000), "Timed storm changed total damage, magic immunity, or input target snapshot");
            Check(model.AbilityEffects.Contains(storm) && Near(storm.LastDamageTickAge, 3), "Final lightning tick has no visual tail");
            Advance(model, .4f);
            Check(!model.AbilityEffects.Contains(storm) && Near(original.Hp, 990), "Storm tail repeated damage or did not expire");

            var largeStep = Fixture(); Enemy largeTarget = Target(largeStep); largeTarget.Hp = largeTarget.MaxHp = 1000;
            Check(largeStep.TryCastSkill(1), "Large-step storm rejected");
            largeStep.Step(.34f);
            AbilityEffect largeStorm = largeStep.AbilityEffects.Find(effect => effect.Kind == "storm");
            largeStep.Step(3.1f);
            Check(largeStorm.DamageTicksResolved == 4 && Near(largeTarget.Hp, 990), "Large step lost or duplicated pending storm ticks");
        }
        private static void StormSnapshotAndSourceLifetime()
        {
            var model = Fixture(); Enemy survivor = Target(model), fragile = Target(model, SkeletonKind.Normal, 450);
            survivor.Hp = survivor.MaxHp = 1000; fragile.Hp = fragile.MaxHp = 1;
            model.Hero.RageRemaining = 10;
            Check(model.TryCastSkill(1), "Storm lifetime fixture rejected");
            Enemy later = Target(model, SkeletonKind.Normal, 650); later.Hp = later.MaxHp = 1000;
            model.Step(.34f);
            AbilityEffect storm = model.AbilityEffects.Find(effect => effect.Kind == "storm");
            model.Hero.RageRemaining = 0;
            int kills = model.Kills, gold = model.Gold;
            Advance(model, .751f);
            Check(fragile.Dead && model.Kills == kills + 1 && model.Gold == gold + fragile.Reward, "First storm tick did not resolve a lethal hit exactly once");
            Check(Array.IndexOf(storm.LastDamageTickTargetIds, fragile.Id) >= 0 && storm.LastDamageTickPoints.Length == storm.LastDamageTickTargetIds.Length,
                "Lethal storm tick lost the final visible hit point");
            Call(model, "DamageActor", model.Hero, model.Hero.Hp, false, true);
            Check(!model.Hero.Alive, "Storm source-death fixture remained alive");
            Advance(model, .75f);
            Check(Array.IndexOf(storm.LastDamageTickTargetIds, fragile.Id) < 0, "Later storm ticks kept striking a previous corpse");
            Advance(model, 2);
            Check(storm.DamageTicksResolved == 4 && Near(survivor.Hp, 980) && Near(later.Hp, 1000),
                "Released storm stopped after source death, lost its cast damage bonus, or acquired a later target");
            Check(model.Kills == kills + 1 && model.Gold == gold + fragile.Reward, "Repeated storm ticks duplicated the lethal target's reward");
        }
        private static void DeerFullRouteAndContacts()
        {
            var model = Fixture();
            Enemy[] targets = { Target(model, SkeletonKind.Normal, 60), Target(model, SkeletonKind.Normal, model.PathLength * .5f), Target(model, SkeletonKind.Normal, model.PathLength - 60) };
            foreach (Enemy target in targets)
            {
                target.Hp = target.MaxHp = 1000;
                target.SkillSlowRemaining = 30; target.SkillSlowMultiplier = 0;
            }
            Check(model.TryCastSkill(0), "Full-route deer rejected");
            model.Step(.3f);
            var deer = model.AbilityEffects.FindAll(effect => effect.Kind == "deer");
            Check(deer.Count == 2, "Deer cast did not create exactly two independent runners");
            foreach (AbilityEffect effect in deer)
            {
                Check(effect.Start == GameModel.Path[GameModel.Path.Length - 1] && Near(effect.Lifetime, 12f / .7f), "Deer did not spawn at the castle with 70 percent of its former speed");
                var route = (Vector2[])typeof(AbilityEffect).GetField("Route", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(effect);
                Check(route.Length == GameModel.Path.Length, "Deer omitted part of the road");
                for (int i = 0; i < route.Length; i++)
                    Check(route[i] == GameModel.Path[GameModel.Path.Length - 1 - i], "Deer route did not reverse the full castle-to-entry road");
            }
            Advance(model, 3f / .7f);
            foreach (AbilityEffect effect in deer)
                Check(model.AbilityEffects.Contains(effect) && Near(Vector2.Distance(effect.End, model.Position(model.PathLength * .75f)), BalanceData.Current.heroSystems.deerLaneOffset, .05f),
                    "A deer expired at the former 3-second limit or did not travel one quarter of the full road");
            Advance(model, 9f / .7f + .05f);
            foreach (AbilityEffect effect in deer)
                Check(!model.AbilityEffects.Contains(effect) && Near(Vector2.Distance(effect.End, GameModel.Path[0]), BalanceData.Current.heroSystems.deerLaneOffset, .01f), "Deer failed to finish at the enemy entry");
            foreach (Enemy target in targets) Check(Near(target.Hp, 980), "Each deer must hit enemies across the full route exactly once");

            var contact = Fixture(); Enemy blocked = Target(contact, SkeletonKind.Normal, contact.PathLength - 60);
            blocked.Hp = blocked.MaxHp = 1000;
            contact.Hero.Position = contact.Hero.Destination = contact.Position(blocked.Distance);
            float before = blocked.Distance;
            Check(contact.TryCastSkill(0), "Deer contact fixture rejected");
            contact.Step(.3f);
            // Advance the released runners alone so walking cannot conceal exact push distance.
            Call(contact, "UpdateAbilityEffects", 60f / contact.PathLength * (12f / .7f) + .2f);
            Check(Near(blocked.Hp, 980) && Near(blocked.SkillSlowMultiplier, .5f) && blocked.SkillSlowRemaining > 2,
                "Deer contact changed the two 10-damage hits or the existing 50-percent slow");
            Check(Near(blocked.Distance, before - 24) && Near(contact.RecentForwardDistance(blocked), 0) && contact.ProjectileImpacts.Exists(impact => impact.Kind == "deer" && impact.Landed),
                "Each deer must push 12 road units backwards without recording forward movement");
            Call(contact, "UpdateAbilityEffects", 18f);
            Check(Near(blocked.Hp, 980) && Near(blocked.Distance, before - 24), "A deer hit or pushed the same target again later in its route");

            var edges = Fixture(); Enemy entry = Target(edges, SkeletonKind.Normal, 8), immune = Target(edges, SkeletonKind.Boss, 8);
            Check(edges.TryCastSkill(0), "Deer edge fixture rejected"); edges.Step(.3f);
            float bossDistance = immune.Distance;
            Call(edges, "UpdateAbilityEffects", 18f);
            Check(Near(entry.Distance, 0) && Near(entry.Hp, 9980), "Deer push crossed the entrance or lost the second hit");
            Check(Near(immune.Distance, bossDistance) && Near(immune.Hp, 10000) && Near(immune.SkillSlowRemaining, 0), "Deer changed boss magic/control immunity");
        }
        private static void SlowerHeelArrowPreservesCombatRules()
        {
            foreach (bool clone in new[] { false, true })
            {
                var model = Fixture(HeroKind.Achilles, clone ? new EquipmentStats { Artifact = ArtifactKind.AthenaMirror } : null);
                if (clone) Check(model.TryUseArtifact(), "Slowed E clone fixture rejected");
                HeroCombatState actor = model.GetControlledHero(clone); Silence(actor);
                Enemy target = Target(model, SkeletonKind.Normal, model.PathLength * .25f);
                actor.RageRemaining = 10;
                Check(model.TryCastSkill(1, null, clone) && Near(actor.Mana, 0) && Near(model.SkillCooldownRemaining(1, clone), 480),
                    "Slowed E changed its 100 mana / 480 second cooldown");
                actor.RageRemaining = 0;
                Enemy later = Target(model, SkeletonKind.Normal, model.PathLength * .7f);
                float release = 1.18f / .7f, flight = .65f / .7f;
                Check(Near(actor.Animation.PlaybackRate, .7f), "E body animation was not slowed with its projectiles");
                model.Step(release - .01f);
                Check(model.AbilityEffects.Count == 0 && Near(target.Hp, 10000), "E released before slowed body contact");
                float clipAge = actor.Animation.ClipAge, cooldown = model.SkillCooldownRemaining(1, clone);
                model.SetPaused(true); model.Step(8);
                Check(Near(actor.Animation.ClipAge, clipAge) && Near(model.SkillCooldownRemaining(1, clone), cooldown), "Pause advanced slowed E body or cooldown");
                model.SetPaused(false); model.Step(.011f);
                AbilityEffect rise = model.AbilityEffects.Find(effect => effect.Kind == "arrow_rise");
                AbilityEffect rain = model.AbilityEffects.Find(effect => effect.Kind == "arrow_rain");
                Check(model.AbilityEffects.FindAll(effect => effect.Kind == "arrow_rain").Count == 1,"Slowed E added a target outside its input snapshot");
                Check(rise != null && rain != null && Near(rise.Lifetime, .45f / .7f) && Near(rain.Lifetime, flight) && Near(rise.VisualPlaybackRate, .7f),
                    "E rise and rain did not use one common 70-percent playback rate");
                Check(Near(actor.Animation.ClipAge, 1.18f, .003f) && rain.Age < .002f, "Release marker or first flight frame drifted during a large step");
                target.Distance += 70;
                Call(model, "DamageActor", actor, actor.Hp, false, true);
                Advance(model, flight - rain.Age - .002f);
                Check(Near(target.Hp, 10000), "Slowed E damage arrived at the former faster timing");
                Advance(model, .004f);
                Check(Near(target.Hp, 9980) && Near(later.Hp, 10000) && Near(target.SkillSlowMultiplier, .5f) && target.SkillSlowRemaining > 4.9f,
                    "Slowed E changed cast snapshot, rage snapshot, one-hit damage, slow, or released-shot survival after source death");
                Check(Vector2.Distance(rain.End, model.Position(target.Distance)) < .01f && model.ProjectileImpacts.Exists(impact => impact.Kind == "arrow" && impact.Landed && Vector2.Distance(impact.Position, rain.HitPoint) < .01f),
                    "Slowed E did not track the moving enemy to its real hit point");
                Advance(model, .5f); Check(Near(target.Hp, 9980), "Slowed E duplicated impact damage");
            }
            var canceled = Fixture(HeroKind.Achilles); Enemy survivor = Target(canceled);
            Check(canceled.TryCastSkill(1), "Canceled slowed E rejected"); canceled.Step(1.3f);
            Call(canceled, "DamageActor", canceled.Hero, canceled.Hero.Hp, false, true); Advance(canceled, 2);
            Check(Near(survivor.Hp, 10000) && canceled.AbilityEffects.Count == 0, "Death before the slowed release failed to cancel E");
        }
        private static void DirectionalReleaseAnchorIsFrozen()
        {
            // Temporary in-memory clips prove the directional path before every future
            // manual-skill view has authored art. No catalog or PNG is written by the test.
            var entries = (Dictionary<string, AnimationClipData>)typeof(AnimationLibrary)
                .GetField("ByAction", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            foreach (int index in new[] { 0, 1 })
            {
                string action = index == 0 ? "divine_spear" : "heel_arrow";
                string key = "achilles/" + action + "/north-east";
                entries.TryGetValue(key, out AnimationClipData old);
                var clip = JsonUtility.FromJson<AnimationClipData>(JsonUtility.ToJson(AnimationLibrary.Get("achilles", action)));
                clip.id = "test-directional-" + action; clip.direction = "north-east";
                clip.groundPivot = new Vector2(64, 100);
                float release = index == 0 ? .72f : 1.18f;
                int frame = clip.FrameAt(release, false);
                Vector2 socket = index == 0 ? new Vector2(112, 28) : new Vector2(94, 33);
                clip.events = new[] { new AnimationEventData { name = "release", frameIndex = frame,
                    timeSeconds = release, hasPosition = true, position = socket, socketName = "throw_hand" } };
                clip.sockets = new[] {
                    new AnimationSocketData { name = "throw_hand", frameIndex = frame, position = socket },
                    new AnimationSocketData { name = "throw_hand", frameIndex = clip.Frames.Length - 1, position = new Vector2(15, 95) }
                };
                entries[key] = clip;
                try
                {
                    bool clone = index == 1;
                    var model = Fixture(HeroKind.Achilles, clone ? new EquipmentStats { Artifact = ArtifactKind.AthenaMirror } : null);
                    if (clone) Check(model.TryUseArtifact(), "Directional release clone rejected");
                    HeroCombatState actor = model.GetControlledHero(clone); Silence(actor);
                    actor.Position = actor.Destination = new Vector2(500, 450);
                    actor.Animation.FacingDirection = Direction8.NorthEast; actor.FacingLeft = false;
                    Target(model);
                    Vector2 target = new Vector2(850, 100);
                    Check(model.TryCastSkill(index, index == 0 ? target : (Vector2?)null, clone), "Directional manual skill rejected");
                    model.Step(release / BalanceData.Current.heroSystems.Skill(HeroKind.Achilles, index).animationSpeed + .001f);
                    AbilityEffect effect = model.AbilityEffects.Find(candidate => candidate.Kind == (index == 0 ? "spear" : "arrow_rise"));
                    Vector2 expected = new Vector2(500, 450) + (socket - clip.GroundPivot) * AnimatedActors.HeroPixelScale;
                    Check(effect != null && effect.HasLaunchPoint && Vector2.Distance(effect.LaunchPoint, expected) < .001f,
                        action + " ignored the directional release socket");
                    if (index == 0)
                        Check(Near(effect.Lifetime, Mathf.Clamp(Vector2.Distance(expected, target) / 650f, .5f, .9f)), "Spear flight used a different origin than its visible release");
                    actor.Position += new Vector2(100, 60); actor.FacingLeft = true; actor.Animation.FacingDirection = Direction8.West;
                    model.Step(.1f);
                    Check(Vector2.Distance(effect.LaunchPoint, expected) < .001f && effect.Source == actor,
                        "Released " + action + " followed the caster's subsequent position or direction");
                    actor.Animation.FacingDirection = Direction8.NorthEast; actor.FacingLeft = false;
                    actor.Animation.Advance(clip.Duration + 1);
                    Vector2 overdueExpected = actor.Position + (socket - clip.GroundPivot) * AnimatedActors.HeroPixelScale;
                    Check(Vector2.Distance(ProjectileVisuals.SkillReleasePoint(actor, action), overdueExpected) < .001f,
                        "Late release callback sampled the recovery frame's hand instead of its release event");
                }
                finally { if (old == null) entries.Remove(key); else entries[key] = old; }
            }
        }
    }
}
