using System;
using UnityEditor;
using UnityEngine;

namespace SkeletonDefender.Editor
{
    // Isolated deterministic fixtures: never read or write the player's real save.
    public static class EquipmentAndAbilitiesTests
    {
        private static void Check(bool condition, string message)
        { if (!condition) throw new Exception("V06 CHECK: " + message); }
        private static bool Near(float value, float expected, float tolerance = .02f) => Mathf.Abs(value - expected) <= tolerance;

        [MenuItem("Skeleton Defender/Check equipment and active skills")]
        public static void Run()
        {
            BalanceData.Reload();
            InventoryTests.Run();
            Check(BalanceData.Current.economy.startingGold == 295, "Starting gold must be 295");
            TowerDamageIncrease();
            ManaAndGuards();
            MagicAndSnapshot();
            SpearMovementHistory();
            ProjectilesAndNinja();
            EquippedBonuses();
            ArtifactsAndClone();
            Debug.Log("SKELETON_V06_CHECKS_PASSED: inventory migration, grants, loot, equipment, mana, casting, snapshots, projectiles, ninja, artifacts and clone. No player save was touched; no map playthrough was run.");
        }

        private static void TowerDamageIncrease()
        {
            float[] before = { 3.8910025f, 2.991208171875f, 2.08075f };
            for (int kind = 0; kind < before.Length; kind++)
                for (int level = 1; level <= 3; level++)
                {
                    var tower = new Tower { Kind = (TowerKind)kind, Level = level };
                    Check(Near(tower.Damage, before[kind] * 1.05f * (1 + .8f * (level - 1)), .0001f),
                        "Requested five-percent increase missing: " + tower.Kind + " level " + level);
                }
        }

        private static GameModel Fixture(HeroKind kind, EquipmentStats stats = null)
        {
            var model = new GameModel(1, kind, stats ?? new EquipmentStats(), 17);
            model.StartWave(); Silence(model.Hero);
            return model;
        }

        private static void Silence(HeroCombatState hero)
        {
            hero.AttackCooldown = 9999; hero.FrogCooldown = 9999;
            hero.SunCooldown = 9999; hero.DecapitateCooldown = 9999;
        }

        private static Enemy Target(GameModel model, float distance, bool boss = false, float hp = 1000)
        {
            var enemy = model.CreateEnemy(boss ? SkeletonKind.Boss : SkeletonKind.Normal, distance);
            enemy.Hp = enemy.MaxHp = hp; enemy.AttackCooldown = enemy.SpecialCooldown = 9999;
            model.Enemies.Add(enemy); return enemy;
        }

        private static void Advance(GameModel model, float seconds)
        {
            int count = Mathf.CeilToInt(seconds / GameModel.Tick);
            for (int i = 0; i < count; i++) model.Step(GameModel.Tick);
        }

        private static void ManaAndGuards()
        {
            var model = new GameModel(1, HeroKind.Circe, new EquipmentStats(), 1);
            Check(Near(model.Hero.Mana, 100) && Near(model.Hero.MaxMana, 100), "Base mana is not 100");
            Check(!model.TryCastSkill(1), "Cast accepted before battle start");
            model.StartWave(); Silence(model.Hero); Target(model, 200);
            model.Hero.Mana = 99;
            Check(!model.TryCastSkill(1) && Near(model.SkillCooldownRemaining(1), 0) && Near(model.Hero.Mana, 99), "Failed cast spent mana or cooldown");
            Advance(model, 1);
            Check(Near(model.Hero.Mana, 100), "Mana does not regenerate at one per second");
            model.SetPaused(true);
            Check(!model.TryCastSkill(1), "Paused cast accepted");
            model.Hero.Mana = 50; Advance(model, 2);
            Check(Near(model.Hero.Mana, 50), "Pause changed mana");
            model.SetPaused(false); model.Hero.Mana = 100;
            Check(model.TryCastSkill(1) && Near(model.Hero.Mana, 0) && Near(model.SkillCooldownRemaining(1), 300), "Successful cast did not charge mana and cooldown once");
            Check(!model.TryCastSkill(1) && Near(model.Hero.Mana, 0), "Cooldown allowed a duplicate cast");
            model.SetPaused(true); Advance(model, 1);
            Check(Near(model.SkillCooldownRemaining(1), 300), "Pause changed skill cooldown");

            var spear = Fixture(HeroKind.Achilles);
            Check(!spear.TryCastSkill(0) && Near(spear.Hero.Mana, 100), "Untargeted spear spent mana");
            Check(!spear.TryCastSkill(0, new Vector2(float.NaN, 10)) && Near(spear.SkillCooldownRemaining(0), 0), "Invalid spear target spent cooldown");
            Check(spear.TryCastSkill(0, new Vector2(200, 200)) && Near(spear.Hero.Mana, 0), "Confirmed targeted spear was rejected");
        }

        private static void MagicAndSnapshot()
        {
            var storm = Fixture(HeroKind.Circe);
            Enemy ordinary = Target(storm, 200), boss = Target(storm, 400, true, 10000);
            Check(storm.TryCastSkill(1), "Storm rejected");
            Enemy later = Target(storm, 600);
            Advance(storm, .8f);
            Check(Near(ordinary.Hp, 990) && Near(boss.Hp, 10000) && Near(later.Hp, 1000), "Storm damage/snapshot/boss magic immunity failed");

            var rain = Fixture(HeroKind.Achilles);
            Enemy snapshot = Target(rain, 200), physicalBoss = Target(rain, 400, true, 10000);
            Check(rain.TryCastSkill(1) && Near(rain.SkillCooldownRemaining(1), 480), "Arrow rain cooldown differs from eight minutes");
            Enemy newcomer = Target(rain, 600);
            Advance(rain, 1.5f);
            Check(Near(snapshot.Hp, 990) && Near(newcomer.Hp, 1000) && Near(physicalBoss.Hp, 9990), "Arrow rain did not hit exactly its original target snapshot");
            Check(snapshot.SkillSlowRemaining > 0 && Near(snapshot.SkillSlowMultiplier, .5f) &&
                Near(physicalBoss.SkillSlowRemaining, 0), "Arrow rain slow or boss control immunity failed");

            var deer = Fixture(HeroKind.Circe);
            Enemy crossed = Target(deer, deer.PathLength - BalanceData.Current.heroRules.spawnDistanceFromExit - 100);
            Check(deer.TryCastSkill(0), "Deer cast rejected");
            Advance(deer, 3.2f);
            Check(Near(crossed.Hp, 980), "Two deer did not each damage a crossed enemy exactly once");
        }

        private static void ProjectilesAndNinja()
        {
            var arrows = Fixture(HeroKind.Circe);
            Check(arrows.Build(0, TowerKind.Archer), "Arrow tower fixture cannot build");
            Enemy victim = Target(arrows, 247);
            arrows.Step(GameModel.Tick);
            Check(Near(victim.Hp, 1000) && arrows.TowerProjectiles.Count > 0, "Arrow dealt damage before flight");
            Advance(arrows, .45f);
            Check(Near(victim.Hp, 1000 - new Tower { Kind = TowerKind.Archer }.Damage), "Arrow failed to damage at arrival");

            var fire = Fixture(HeroKind.Circe); Check(fire.Build(0, TowerKind.Ember), "Fire fixture cannot build");
            Enemy blastA = Target(fire, 247), blastB = Target(fire, 249);
            fire.Step(GameModel.Tick);
            Check(Near(blastA.Hp, 1000) && Near(blastB.Hp, 1000), "Fireball exploded before arrival");
            Advance(fire, .5f);
            Check(blastA.Hp < 1000 && blastB.Hp < 1000, "Fireball lost splash on arrival");

            var ninjaFight = Fixture(HeroKind.Circe);
            Enemy ninja = ninjaFight.CreateEnemy(SkeletonKind.Ninja, 247); ninja.DodgeReady = true;
            ninjaFight.Enemies.Add(ninja);
            Check(ninjaFight.TryCastSkill(1), "Ninja storm fixture rejected");
            Advance(ninjaFight, .1f);
            Check(Near(ninja.Hp, ninja.MaxHp - 10) && ninja.DodgeReady, "Manual magic consumed or was blocked by ninja physical dodge");
        }

        private static void SpearMovementHistory()
        {
            var model = Fixture(HeroKind.Achilles);
            Enemy walker = Target(model, 200);
            Advance(model, 2.1f);
            Check(Near(model.RecentForwardDistance(walker), walker.Speed * 2, .05f), "Spear history does not cover two seconds of actual travel");
            float before = walker.Distance;
            Check(model.TryCastSkill(0, model.Position(before)), "Moving-target spear was rejected");
            model.Step(.36f);
            Check(Near(walker.Hp, 985) && Near(walker.Distance, before + walker.Speed * .36f - walker.Speed, .1f), "Spear knockback is not half the last two seconds of travel");
            model.Hero.Position = model.Hero.Destination = model.Position(walker.Distance);
            Advance(model, 2.1f);
            Check(Near(model.RecentForwardDistance(walker), 0, .05f), "Stationary enemy retained stale movement history");
        }

        private static void EquippedBonuses()
        {
            var stats = new EquipmentStats { DamageBonus = 4, PhysicalArmor = .3f,
                AttackSpeedBonus = .2f, MoveSpeedMultiplier = .25f, MaxManaBonus = 30,
                ManaRegenBonus = .3f, ManaCostReduction = .1f,
                MagicDamageMultiplier = .5f, ManaRegenMultiplier = .25f };
            var mage = Fixture(HeroKind.Circe, stats);
            Check(Near(mage.Hero.Damage, 9 * .5f) && Near(mage.Hero.MaxMana, 130) &&
                Near(mage.Hero.ManaRegen, .55f) && Near(mage.SkillManaCost(0), 90), "Gear or base-mana penalty aggregation failed");
            Check(Near(mage.Hero.AttackInterval, 2 / 1.2f) && Near(mage.Hero.WalkSpeed, 54 * .25f), "Gear attack frequency or walk speed is incorrect");
            Enemy victim = Target(mage, 200); Check(mage.TryCastSkill(1), "Geared storm rejected");
            Advance(mage, .2f);
            Check(Near(victim.Hp, 995f), "Sword magic penalty was omitted or applied twice to a skill");
        }

        private static void ArtifactsAndClone()
        {
            var rage = Fixture(HeroKind.Circe, new EquipmentStats { Artifact = ArtifactKind.ZeusNail, PhysicalArmor = .3f });
            Enemy stormVictim = Target(rage, 200);
            Check(rage.TryUseArtifact() && !rage.TryUseArtifact() && Near(rage.RageRemaining, 300), "Rage use limit or duration failed");
            Check(Near(rage.Hero.Damage, 10) && Near(rage.Hero.AttackInterval, 1), "Rage did not double basic damage/frequency");
            Check(rage.TryCastSkill(1), "Raging storm rejected"); Advance(rage, .1f);
            Check(Near(stormVictim.Hp, 980), "Rage did not double skill damage");
            Enemy rageAttacker = Target(rage, rage.PathLength - BalanceData.Current.heroRules.spawnDistanceFromExit);
            rageAttacker.AttackCooldown = 0; float rageHp = rage.Hero.Hp;
            rage.Step(GameModel.Tick);
            Check(Near(rage.Hero.Hp, rageHp - 1.75f), "Rage did not halve incoming damage after armor");
            float rageBefore = rage.RageRemaining; rage.SetPaused(true); Advance(rage, 1);
            Check(Near(rage.RageRemaining, rageBefore), "Pause consumed rage duration");

            var mirror = Fixture(HeroKind.Circe, new EquipmentStats { Artifact = ArtifactKind.AthenaMirror, PhysicalArmor = .2f });
            Check(mirror.TryUseArtifact() && mirror.Clone != null && !mirror.TryUseArtifact(), "Mirror clone or once-per-map limit failed");
            Silence(mirror.Clone);
            Check(mirror.Clone.IsClone && Near(mirror.Clone.Mana, 100), "Clone did not start with its own mana");
            Enemy target = Target(mirror, 200);
            Check(mirror.TryCastSkill(1, null, true) && Near(mirror.Hero.Mana, 100) && Near(mirror.Clone.Mana, 0) &&
                Near(mirror.SkillCooldownRemaining(1), 0), "Clone shared mana or skill cooldown with the original");
            Check(mirror.TryCastSkill(1), "Original could not cast after clone's cast"); Advance(mirror, .1f);
            Check(Near(target.Hp, 980), "Original and clone could not use the skill independently");
            Vector2 originalDestination = mirror.Hero.Destination;
            Check(mirror.MoveHero(new Vector2(200, 300), true) && mirror.Hero.Destination == originalDestination, "Clone movement changed original destination");
            mirror.Clone.Position = mirror.Clone.Destination = mirror.Position(400);
            Enemy cloneAttacker = Target(mirror, 400); cloneAttacker.AttackCooldown = 0;
            float originalHp = mirror.Hero.Hp, copyHp = mirror.Clone.Hp;
            mirror.Step(GameModel.Tick);
            Check(Near(mirror.Clone.Hp, copyHp - 6) && Near(mirror.Hero.Hp, originalHp), "Enemy did not target clone or apply its 50% extra damage");
            float copyTime = mirror.CloneRemaining; mirror.SetPaused(true); Advance(mirror, 1);
            Check(Near(mirror.CloneRemaining, copyTime), "Pause consumed clone duration");
            mirror.SetPaused(false);

            // Keep a single distant fixture enemy alive while only duration timers advance.
            for (int i = 0; i < 18010 && mirror.Clone != null; i++)
            {
                mirror.Enemies.Clear(); cloneAttacker.Distance = 0; cloneAttacker.AttackCooldown = 9999;
                mirror.Enemies.Add(cloneAttacker); mirror.Step(GameModel.Tick);
            }
            Check(mirror.Clone == null && !mirror.TryUseArtifact(), "Clone did not expire after five minutes or mirror could be reused");

            var aegis = Fixture(HeroKind.Circe, new EquipmentStats { Artifact = ArtifactKind.AegisOfDawn });
            Enemy killer = Target(aegis, aegis.PathLength - BalanceData.Current.heroRules.spawnDistanceFromExit);
            killer.AttackCooldown = 0; aegis.Hero.Hp = 1;
            aegis.Step(GameModel.Tick);
            Check(!aegis.Hero.Alive && Near(aegis.Hero.RespawnRemaining, 3), "Aegis did not set three-second resurrection");
            killer.Distance = 0; killer.AttackCooldown = 9999;
            Advance(aegis, 2.8f); Check(!aegis.Hero.Alive, "Aegis resurrected too early");
            Advance(aegis, .3f); Check(aegis.Hero.Alive && Near(aegis.Hero.Hp, aegis.Hero.MaxHp), "Aegis did not resurrect at full health");
        }
    }
}
