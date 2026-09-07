using System;
using UnityEditor;
using UnityEngine;

namespace SkeletonDefender.Editor
{
    public static class SmokeTests
    {
        private static void Check(bool condition, string message)
        { if (!condition) throw new Exception("GAMEPLAY TEST: " + message); }
        private static bool Near(float actual, float expected, float tolerance = .01f) => Mathf.Abs(actual - expected) <= tolerance;
        private static void Advance(GameModel model, float seconds, bool clearEnemies = false)
        {
            int ticks = Mathf.CeilToInt(seconds / GameModel.Tick);
            for (int i = 0; i < ticks; i++)
            {
                if (clearEnemies) model.Enemies.Clear();
                model.Step(GameModel.Tick);
            }
        }

        // Wait only for already accepted contacts; never skip startup in the live model.
        private static void StepAtContact(GameModel model)
        {
            model.Step(GameModel.Tick);
            for (int i = 0; i < 180 && model.PendingAnimationContacts > 0; i++) model.Step(GameModel.Tick);
        }

        [MenuItem("Skeleton Defender/Run gameplay checks")]
        public static void Run()
        {
            BalanceData.Reload();
            InventoryTests.Run();
            CheckDefinitionsAndWaves();
            CheckTowerScaling();
            CheckEconomy();
            CheckSchedules();
            CheckEarlyWaveCalls();
            CheckHeroesAndMovement();
            CheckIdleRegeneration();
            CheckCombat();
            CheckDeathAndRespawn();
            CheckTransformationsAndBossImmunity();
            CheckSeededProcs();
            CheckSkeletonAbilities();
            CheckTerminalStates();
            RunPlayabilityDiagnostics();
            Debug.Log("SKELETON_TESTS_PASSED: inventory; twenty waves; sixty-percent spawned threshold; declining rounded early bonus; planned/summoned counters; overlapping wave ownership; exact health, movement, regeneration and Ember frequency; boss immunity; seeded abilities and terminal guards.");
        }

        private static void CheckDefinitionsAndWaves()
        {
            BalanceData balance = BalanceData.Current;
            Check(balance.mapCount == 1 && balance.plannedMapCount == 10 && balance.wavesPerMap == 20 && balance.mapName == "Skeleton Cemetry",
                "Playable/planned maps, map title or twenty-wave campaign differs from request");
            Check(balance.heroes.Length == 2, "Unavailable third hero became playable");
            Check(Near(balance.Hero(0).hp, 250) && Near(balance.Hero(0).damage, 5) && Near(balance.Hero(0).attackInterval, 2), "Circe base stats differ from specification");
            Check(Near(balance.Hero(1).hp, 350) && Near(balance.Hero(1).damage, 2.5f) && Near(balance.Hero(1).attackInterval, 1), "Achilles base stats differ from specification");
            Check(balance.skeletons.Length == 14, "Fourteen requested skeleton variants were not defined");
            float[] hp = { 25, 30, 30, 150, 100, 100, 30, 75, 300, 40, 35, 30, 40, 10000 };
            float[] damage = { 5, 5, 15, 5, 50, 5, 5, 5, 5, 5, 5, 5, 5, 5 };
            float[] speed = { 1, 1.2f, .95f, 1, .25f, .9f, .75f, .7f, .6f, .85f, .7f, 2.5f, 1, .35f };
            int[] gold = { 2, 3, 3, 5, 3, 3, 3, 3, 6, 3, 3, 3, 3, 26 };
            var factory = new GameModel();
            float standardSpeed = factory.CreateEnemy(SkeletonKind.Normal).Speed;
            for (int kind = 0; kind < 14; kind++)
            {
                Enemy enemy = factory.CreateEnemy((SkeletonKind)kind);
                Check(Near(enemy.MaxHp, hp[kind]) && Near(enemy.AttackDamage, damage[kind]), "Skeleton health/damage multiplier differs from specification: " + (SkeletonKind)kind);
                Check(Near(enemy.Speed, standardSpeed * speed[kind]) && enemy.Reward == gold[kind], "Skeleton speed or gold reward differs from specification: " + (SkeletonKind)kind);
            }
            Check(Near(balance.spawnSchedule.bossGongSeconds, 5) && Near(balance.heroRules.respawnSeconds, 60), "Gong or resurrection duration differs from specification");
            Check(Near(balance.spawnSchedule.waveSlotSeconds, 90) && Near(balance.spawnSchedule.spawnWindowSeconds, 60), "Wave slots or spawn window differ from requested pacing");
            Check(factory.CreateEnemy(SkeletonKind.Normal, 0, true).Reward == 1, "Summoned skeleton reward is not one gold");

            // Sparse composition pairs are variant index, count, transcribed from the user document.
            int[][] documented = {
                Composition(0,50), Composition(0,50,1,20), Composition(0,50,2,25), Composition(0,50,3,10),
                Composition(0,50,3,5,2,10,1,5), Composition(0,75,4,50), Composition(0,75,5,25),
                Composition(0,75,7,25), Composition(0,75,8,15), Composition(0,100,8,20,4,50,5,10,7,20),
                Composition(0,100,9,25), Composition(0,100,10,20,7,10), Composition(0,100,8,5,7,10,4,25,5,10),
                Composition(0,100,8,7,7,15,4,30,5,20), Composition(0,150,10,30,8,20,11,100,9,25,12,50)
            };
            for (int wave = 1; wave <= 20; wave++)
                {
                    var plan = GameModel.BuildSpawnPlan(1, wave);
                    WaveDefinition definition = balance.Wave(wave);
                    Check(plan.Count == definition.count && plan.Count > 0, "Wrong scheduled enemy total on wave " + wave);
                    int bosses = 0; float previousTime = -1; int previousBatch = 0;
                    var skeletonCounts = new int[14];
                    var batchCounts = new System.Collections.Generic.Dictionary<int, int>();
                    for (int i = 0; i < plan.Count; i++)
                    {
                        SpawnEntry entry = plan[i];
                        Check(entry.Ordinal == i + 1 && entry.Time >= previousTime && entry.Batch >= previousBatch, "Spawn plan is not in chronological ordinal order");
                        Check(entry.Kind == EnemyKind.Skeleton, "Non-skeleton faction entered the first map");
                        Check(entry.Time >= 0 && entry.Time <= 60.01f, "Scheduled spawn falls outside the first sixty seconds");
                        skeletonCounts[(int)entry.Skeleton]++;
                        if (!batchCounts.ContainsKey(entry.Batch)) batchCounts[entry.Batch] = 0;
                        batchCounts[entry.Batch]++;
                        if (entry.IsBoss)
                        {
                            bosses++;
                            Check(wave == 20 && entry.Ordinal == 1, "Boss did not open the twentieth wave");
                        }
                        else
                        {
                            Check(entry.Rank == 1, "Skeleton variants were unintentionally scaled by former wave ranks");
                        }
                        previousTime = entry.Time; previousBatch = entry.Batch;
                    }
                    Check(bosses == (wave == 20 ? 1 : 0), "Incorrect boss count");
                    Check(definition.reinforced == (wave % 5 == 0), "Every fifth wave was not marked reinforced");
                    foreach (int count in batchCounts.Values)
                    {
                        float fraction = count / (float)plan.Count;
                        if (wave < 20) Check(fraction >= .1f - .00001f && fraction <= .2f + .00001f, "A reinforcement batch is outside ten to twenty percent of its wave");
                    }
                    for (int kind = 0; kind < 14; kind++)
                    {
                        Check(skeletonCounts[kind] == definition.skeletonCounts[kind], "Expanded composition differs from balance data");
                        if (wave <= 15) Check(skeletonCounts[kind] == documented[wave - 1][kind], "Wave " + wave + " differs from user's exact composition");
                        if (wave == 20) Check(skeletonCounts[kind] == (kind == (int)SkeletonKind.Boss ? 1 : 0), "Final wave contains unexpected escorts");
                    }
                }
        }

        private static int[] Composition(params int[] pairs)
        {
            var counts = new int[14];
            for (int i = 0; i < pairs.Length; i += 2) counts[pairs[i]] = pairs[i + 1];
            return counts;
        }

        private static void CheckTowerScaling()
        {
            float[,] expected = { { 3.8910025f, 7.0038045f, 10.1166065f }, { 2.991208171875f, 5.384174709375f, 7.777141246875f }, { 2.08075f, 3.74535f, 5.40995f } };
            for (int kind = 0; kind < 3; kind++)
                for (int level = 1; level <= 3; level++)
                    Check(Near(new Tower { Kind = (TowerKind)kind, Level = level }.Damage, expected[kind, level - 1] * 1.05f, .0001f),
                        "Tower damage differs from the requested values at level " + level + ": " + (TowerKind)kind);
            for (int level = 1; level <= 3; level++)
            {
                var tower = new Tower { Kind = TowerKind.Ember, Level = level };
                float oldInterval = 1.55f / (1 + (level - 1) * .09f);
                Check(Near(tower.Interval, oldInterval / .7f, .0001f) && Near((1 / tower.Interval) / (1 / oldInterval), .7f, .0001f),
                    "Thirty-percent slower Ember attack rate was not applied to frequency at level " + level);
            }
        }

        private static void CheckEconomy()
        {
            var game = new GameModel();
            Check(game.Gold == 295 && !game.Build(-1, TowerKind.Archer) && !game.Build(0, (TowerKind)99), "Initial gold differs from 295 or invalid construction was accepted");
            Check(game.Build(1, TowerKind.Archer), "Initial construction failed");
            int afterBuild = game.Gold;
            Check(!game.Build(1, TowerKind.Frost) && game.Gold == afterBuild, "Duplicate construction spent money");
            Check(game.Upgrade(1) && game.At(1).Level == 2, "Upgrade failed");
            Check(!game.Upgrade(1), "Unaffordable upgrade accepted");
            int expectedGold = game.Gold + game.At(1).SellValue;
            Check(game.Sell(1) && game.Gold == expectedGold && game.At(1) == null, "Sale ledger mismatch");
            Check(!game.Sell(1) && game.Gold == expectedGold, "Repeated sale granted gold");
            Check(game.StartWave() && !game.StartWave() && game.Wave == 1, "Duplicate wave start accepted");
        }

        private static void CheckSchedules()
        {
            var cleared = new GameModel(1, HeroKind.Circe, 0, 123);
            var occupied = new GameModel(1, HeroKind.Circe, 0, 123);
            Advance(cleared, 2);
            Check(cleared.Wave == 0 && !cleared.HasStarted && cleared.Spawned == 0, "Battle started before the first manual wave command");
            cleared.StartWave(); occupied.StartWave();
            for (int tick = 0; tick < 18 * 60; tick++)
            {
                cleared.Enemies.Clear();
                cleared.Step(GameModel.Tick); occupied.Step(GameModel.Tick);
                Check(cleared.Spawned == occupied.Spawned && Near(cleared.WaveElapsed, occupied.WaveElapsed), "Clearing enemies accelerated scheduled reinforcements");
            }
            Check(occupied.Enemies.Count > cleared.Enemies.Count && occupied.Spawned < 50, "Schedule fixture did not reach separated batches");
            Check(!cleared.StartWave(), "Manual command advanced an already running wave");
            Advance(cleared, 71.5f, true);
            Check(cleared.Wave == 1 && cleared.WaveCleared && cleared.WaveSlotRemaining > 0, "Clearing all enemies started the next wave before ninety seconds");
            int settledGold = cleared.Gold;
            Check(!cleared.StartWave() && cleared.Gold == settledGold, "Manual command skipped the waiting period or duplicated clear bonus");
            Advance(cleared, .7f, true);
            Check(cleared.Wave == 2 && cleared.Gold == settledGold, "Next wave did not start automatically after its slot or repeated the bonus");

            var stalled = new GameModel(); stalled.StartWave();
            Enemy survivor = stalled.CreateEnemy(SkeletonKind.Normal, 0);
            survivor.WaveNumber = 1;
            survivor.Hp = survivor.MaxHp = 10000;
            for (int tick = 0; tick < 95 * 60; tick++)
            {
                stalled.Enemies.Clear(); survivor.Distance = 0; stalled.Enemies.Add(survivor);
                stalled.Step(GameModel.Tick);
            }
            Check(stalled.Wave == 1 && !stalled.WaveCleared && !stalled.StartWave(), "Next wave started while a prior-wave enemy remained alive");
            stalled.Enemies.Clear(); stalled.Step(GameModel.Tick); stalled.Step(GameModel.Tick);
            Check(stalled.Wave == 2, "Wave did not advance after both time and clear conditions were satisfied");
        }

        private static void CheckHeroesAndMovement()
        {
            var circe = new GameModel(1, HeroKind.Circe, 7, 4);
            var achilles = new GameModel(1, HeroKind.Achilles, 6, 4);
            Check(circe.Hero.Kind == HeroKind.Circe && Near(circe.Hero.MaxHp, 250) && Near(circe.Hero.Damage, 12), "Circe selection or weapon bonus was lost");
            Check(achilles.Hero.Kind == HeroKind.Achilles && Near(achilles.Hero.MaxHp, 350) && Near(achilles.Hero.Damage, 8.5f), "Achilles selection or fractional weapon damage was lost");
            Vector2 destination = new Vector2(350, 550), start = circe.Hero.Position;
            Check(circe.MoveHero(destination), "Move command rejected");
            Advance(circe, .5f);
            float travelled = Vector2.Distance(start, circe.Hero.Position);
            Check(Near(travelled, 54 * .5f, .01f), "Circe movement speed is not 135 reduced by sixty percent");
            Check(Vector2.Distance(circe.Hero.Position, destination) < Vector2.Distance(start, destination), "Hero did not approach the commanded location");
            Check(!circe.MoveHero(new Vector2(float.NaN, 10)), "Nonfinite move accepted");
            Vector2 achillesStart = achilles.Hero.Position;
            Check(achilles.MoveHero(destination), "Achilles move command rejected");
            Advance(achilles, .5f);
            Check(Near(Vector2.Distance(achillesStart, achilles.Hero.Position), 87.75f * .5f, .01f), "Achilles movement speed is not 135 reduced by thirty-five percent");
        }

        private static void CheckIdleRegeneration()
        {
            Check(Near(BalanceData.Current.heroRules.regenerationIdleSeconds, 4) &&
                Near(BalanceData.Current.heroRules.regenerationMaxHpFractionPerSecond, .05f), "Idle regeneration parameters differ from the request");
            // Exercise the reusable component separately from the hero implementation, including a tick crossing the delay.
            var reusable = new IdleRegeneration();
            Check(Near(reusable.Advance(3.9f, true, 100, 250, 4, .05f), 100), "Regeneration started before four idle seconds");
            float hp = reusable.Advance(.3f, true, 100, 250, 4, .05f);
            Check(Near(hp, 102.5f, .001f), "Threshold-crossing tick healed for time before the four-second delay");
            Check(Near(reusable.Advance(20, true, hp, 250, 4, .05f), 250), "Regeneration exceeded maximum health");
            reusable.Advance(1, false, 100, 250, 4, .05f);
            Check(Near(reusable.IdleSeconds, 0) && !reusable.IsRegenerating, "Activity did not reset reusable regeneration");
            Check(Near(reusable.Advance(20, true, 0, 250, 4, .05f), 0), "Regeneration resurrected a dead unit");

            foreach (HeroKind hero in new[] { HeroKind.Circe, HeroKind.Achilles })
            {
                var resting = new GameModel(1, hero, 0, 1); resting.Hero.Hp = 50;
                Advance(resting, 3.9f);
                Check(Near(resting.Hero.Hp, 50) && !resting.Hero.IsRegenerating, "Hero healed before four idle seconds in preparation");
                Advance(resting, .3f); float afterDelay = resting.Hero.Hp;
                Check(afterDelay > 50 && resting.Hero.IsRegenerating, "Idle hero did not begin regenerating");
                Advance(resting, 1);
                Check(Near(resting.Hero.Hp - afterDelay, resting.Hero.MaxHp * .05f, .02f), "Hero regeneration is not five percent of maximum HP per second");
                resting.SetPaused(true); float pausedHp = resting.Hero.Hp, pausedIdle = resting.Hero.IdleSeconds;
                Advance(resting, 10);
                Check(Near(resting.Hero.Hp, pausedHp) && Near(resting.Hero.IdleSeconds, pausedIdle), "Pause advanced regeneration or its idle timer");
                resting.SetPaused(false);
                Check(resting.MoveHero(resting.Hero.Position + new Vector2(-40, 0)), "Regeneration movement fixture rejected command");
                Advance(resting, .2f);
                Check(Near(resting.Hero.Hp, pausedHp) && resting.Hero.IdleSeconds < .02f, "Movement did not reset and suspend regeneration");
                Advance(resting, .2f); float stoppedHp = resting.Hero.Hp;
                Advance(resting, 3.5f);
                Check(Near(resting.Hero.Hp, stoppedHp), "Moving hero resumed healing without a fresh four-second idle interval");
            }

            var attacking = CombatFixture(HeroKind.Circe); attacking.Hero.Hp = 100;
            Advance(attacking, 4.2f, true); float beforeAttack = attacking.Hero.Hp;
            AddTarget(attacking, -120, 1000); attacking.Hero.AttackCooldown = 0;
            attacking.Step(GameModel.Tick);
            Check(Near(attacking.Hero.Hp, beforeAttack) && attacking.Hero.IdleSeconds < .02f && !attacking.Hero.IsRegenerating,
                "Basic attack failed to interrupt idle regeneration");

            var casting = CombatFixture(HeroKind.Circe); casting.Hero.Hp = 100;
            Advance(casting, 4.2f, true); float beforeCast = casting.Hero.Hp;
            AddTarget(casting, -120, 1000); casting.Hero.SunCooldown = 0;
            casting.Step(GameModel.Tick);
            Check(Near(casting.Hero.Hp, beforeCast) && casting.Hero.IdleSeconds < .02f, "Skill cast failed to interrupt idle regeneration");

            var damaged = CombatFixture(HeroKind.Circe); damaged.Hero.Hp = 100;
            Advance(damaged, 4.2f, true); float beforeDamage = damaged.Hero.Hp;
            Enemy attacker = AddTarget(damaged); attacker.AttackCooldown = 0;
            bool contactObserved = false;
            for (int tick = 0; tick < 120 && !contactObserved; tick++)
            {
                // AFK healing may continue during the enemy's anticipation. The actual
                // contact tick must apply all five damage and reset regeneration.
                beforeDamage = damaged.Hero.Hp;
                damaged.Step(GameModel.Tick);
                contactObserved = damaged.Hero.Hp < beforeDamage;
            }
            Check(contactObserved && Near(damaged.Hero.Hp, beforeDamage - 5) && damaged.Hero.IdleSeconds < .02f,
                "Incoming damage failed to interrupt regeneration or was healed in the same tick");

            var stunned = new GameModel(); stunned.Hero.Hp = 100;
            Advance(stunned, 4.2f); float beforeStun = stunned.Hero.Hp;
            stunned.Hero.KnockdownRemaining = 5;
            Advance(stunned, 4.9f);
            Check(Near(stunned.Hero.Hp, beforeStun) && stunned.Hero.IdleSeconds < .02f, "Knocked-down hero regenerated");
            Advance(stunned, .2f); Advance(stunned, 3.5f);
            Check(Near(stunned.Hero.Hp, beforeStun), "Regeneration did not wait again after knockdown");
        }

        private static void CheckEarlyWaveCalls()
        {
            var timing = BalanceData.Current.spawnSchedule;
            Check(timing.earlyWaveTrigger == "spawned" && Near(timing.earlyWaveFraction, .6f) && timing.earlyWaveBonus == 30,
                "Early wave rules differ from sixty percent spawned and a maximum thirty gold");
            var beforeStart = new GameModel();
            Check(beforeStart.PlannedRemaining == 0 && beforeStart.Remaining == 0 && beforeStart.SummonedRemaining == 0,
                "Uncalled first wave entered the remaining counter");
            var model = CombatFixture(HeroKind.Circe);
            Check(model.PlannedRemaining == 50 && model.Spawned == 0 && !model.CanCallNextWave && !model.TryCallNextWave(),
                "Calling the first wave did not immediately display its full original count");
            while (model.Spawned < 29 && model.Elapsed < 50)
            {
                model.Step(GameModel.Tick);
                Check(model.PlannedRemaining == 50, "An ordinary scheduled spawn changed the planned remainder");
            }
            Check(model.EarlyWaveSpawned == 29 && model.EarlyWaveRequiredSpawned == 30 && !model.CanCallNextWave,
                "Early call unlocked below thirty of fifty spawned enemies");
            WaveRun first = model.WaveRuns[0];
            int totalBeforeSummon = model.Remaining;
            Enemy summoned = model.CreateEnemy(SkeletonKind.Normal, 0, true);
            model.Enemies.Add(summoned);
            Check(model.PlannedRemaining == 50 && model.SummonedRemaining == 1 && model.Remaining == totalBeforeSummon + 1,
                "Summon altered the original counter or failed to increase the actual remainder");
            ProjectileHit(model, summoned, 1000);
            Check(first.OriginalKills == 0 && model.EarlyWaveSpawned == 29 && !model.CanCallNextWave &&
                model.PlannedRemaining == 50 && model.SummonedRemaining == 0,
                "Summon kill altered original accounting or enabled the spawn threshold");
            while (model.Spawned < 30 && model.Elapsed < 50) model.Step(GameModel.Tick);
            Check(model.CanCallNextWave && Near(model.EarlyWaveProgress, .6f) && model.EarlyWaveKills == 0 && model.EarlyWaveBonus == 30,
                "Exactly sixty percent spawned did not unlock the maximum bonus without any kills");
            Check(Near(first.EligibleAtWaveElapsed, first.Plan[29].Time, .001f), "Bonus decay did not start at the planned eligibility time");

            model.SetPaused(true); float pausedTime = model.Elapsed; int pausedGold = model.Gold;
            Advance(model, 5);
            Check(!model.CanCallNextWave && !model.TryCallNextWave() && Near(model.Elapsed, pausedTime) && model.Gold == pausedGold,
                "Paused battle accepted or rewarded an early call");
            model.SetPaused(false);
            var originals = model.Enemies.FindAll(enemy => enemy.WaveNumber == 1 && enemy.SpawnOrdinal > 0);
            ProjectileHit(model, originals[0], 1000);
            Check(model.PlannedRemaining == 49 && first.OriginalRemoved == 1 && first.OriginalKills == 1,
                "Killing an original did not reduce the planned remainder exactly once");
            originals[1].Distance = model.PathLength; model.Step(GameModel.Tick);
            Check(model.PlannedRemaining == 48 && first.OriginalRemoved == 2 && first.OriginalKills == 1 && model.Lives == 19,
                "An escaped original did not reduce the remainder without becoming a kill");

            int spawnedBeforeCall = first.Spawned, goldBeforeCall = model.Gold, payout = model.EarlyWaveBonus;
            var survivors = model.Enemies.ToArray();
            Check(model.TryCallNextWave() && model.Wave == 2 && model.Gold == goldBeforeCall + payout &&
                model.LastEarlyWaveBonus == payout && model.TotalEarlyWaveGold == payout && first.EarlyCallUsed,
                "Early call lost the pre-transition payout or did not pay exactly once");
            Check(model.PlannedRemaining == 48 + 70 && model.EarlyWaveSpawned == 0,
                "Overlapping counter did not retain the old remainder plus the entire newly called wave");
            foreach (Enemy survivor in survivors)
                Check(model.Enemies.Contains(survivor), "Early call deleted a surviving prior-wave enemy");
            Check(first.Spawned == spawnedBeforeCall && first.Spawned < first.Plan.Count && model.WaveRuns.Count == 2,
                "Early call discarded or instantly spawned the prior wave's queue");
            Check(!model.TryCallNextWave() && model.Gold == goldBeforeCall + payout,
                "Repeated button press farmed bonus gold");

            Advance(model, 25);
            Check(first.Spawned == first.Plan.Count && model.WaveRuns[1].Spawned > 0 && !first.Completed,
                "Overlapping waves did not retain independent spawn schedules");
            var remainingFirst = model.Enemies.FindAll(enemy => enemy.WaveNumber == 1);
            int reward = 26, goldBeforeClear = model.Gold;
            foreach (Enemy enemy in remainingFirst) reward += enemy.Reward;
            foreach (Enemy enemy in remainingFirst) ProjectileHit(model, enemy, 1000);
            Check(first.Completed && model.WavesCompleted == 1 && model.LastCompletedWave == 1 && model.Gold == goldBeforeClear + reward &&
                model.Wave == 2 && model.PlannedRemaining == 70,
                "Finishing an old wave lost or duplicated its bonus or left stale original-counter entries");
            int clearGold = model.Gold, completionSerial = model.WaveCompletionSerial;
            Advance(model, .2f);
            Check(model.Gold == clearGold && model.WaveCompletionSerial == completionSerial, "Completed prior wave paid a second bonus");

            var late = CombatFixture(HeroKind.Circe);
            while (!late.CanCallNextWave && late.Elapsed < 50) late.Step(GameModel.Tick);
            Enemy blocker = late.Enemies[0];
            float eligibleAt = late.WaveRuns[0].EligibleAtWaveElapsed;
            AdvanceWithBlockerUntil(late, blocker, (eligibleAt + 90) * .5f);
            Check(late.EarlyWaveBonus == 15, "Linear early-call bonus did not fall to fifteen at its midpoint");
            AdvanceWithBlockerUntil(late, blocker, 80);
            Check(late.EarlyWaveBonus == 5, "Bonus did not use rounded linear decay at eighty seconds");
            AdvanceWithBlockerUntil(late, blocker, 90.2f);
            Check(late.CanCallNextWave && late.EarlyWaveBonus == 0, "Eligible late call was blocked or still promised gold after ninety seconds");
            late.WaveRuns[0].EligibleAtWaveElapsed = late.WaveElapsed;
            Check(late.EarlyWaveBonus == 0, "Eligibility first reached after ninety seconds produced an invalid positive bonus");
            int lateGold = late.Gold;
            Check(late.TryCallNextWave() && late.Gold == lateGold && late.LastEarlyWaveBonus == 0 && late.TotalEarlyWaveGold == 0,
                "Late call paid gold or failed to capture its zero-value payout");

            var preparation = CombatFixture(HeroKind.Circe);
            for (int tick = 0; tick < 70 * 60 && !preparation.WaveCleared; tick++)
            {
                foreach (Enemy enemy in preparation.Enemies.ToArray()) ProjectileHit(preparation, enemy, 1000);
                preparation.Step(GameModel.Tick);
            }
            Check(preparation.Wave == 1 && preparation.WaveCleared && preparation.State == RunState.Preparing &&
                preparation.PlannedRemaining == 0 && preparation.SummonedRemaining == 0 && preparation.Remaining == 0,
                "Cleared preparation kept a stale original counter or counted an uncalled next wave");
            Advance(preparation, 2);
            Check(preparation.PlannedRemaining == 0, "Waiting between waves resurrected a completed original count");

            foreach (SkeletonKind kind in new[] { SkeletonKind.Tutankhamun, SkeletonKind.Sarcophagus })
            {
                var ownership = CombatFixture(HeroKind.Circe);
                while (!ownership.CanCallNextWave && ownership.Elapsed < 50) ownership.Step(GameModel.Tick);
                Enemy owner = AddSkeleton(ownership, kind, 200, false);
                Check(ownership.TryCallNextWave(), "Offspring fixture could not begin overlapping wave");
                if (kind == SkeletonKind.Tutankhamun)
                { owner.SpecialCooldown = 0; StepAtContact(ownership); owner.SpecialCooldown = 999; }
                else ProjectileHit(ownership, owner, 1000);
                Enemy child = ownership.Enemies.Find(enemy => enemy.IsSummoned);
                Check(child != null && child.WaveNumber == 1 && child.SpawnOrdinal == 0 && child.IsSummoned,
                    "Offspring inherited the latest wave instead of its parent's old wave: " + kind);
                int priorKills = ownership.WaveRuns[0].OriginalKills, latestKills = ownership.WaveRuns[1].OriginalKills;
                int plannedBeforeChild = ownership.PlannedRemaining;
                ProjectileHit(ownership, child, 1000);
                Check(ownership.WaveRuns[0].OriginalKills == priorKills && ownership.WaveRuns[1].OriginalKills == latestKills &&
                    ownership.PlannedRemaining == plannedBeforeChild,
                    "Offspring death changed an original-wave kill or remaining counter: " + kind);
            }
        }

        private static void AdvanceWithBlockerUntil(GameModel model, Enemy blocker, float waveSeconds)
        {
            while (model.WaveElapsed < waveSeconds)
            {
                model.Enemies.Clear(); blocker.Distance = 0; model.Enemies.Add(blocker);
                model.Step(GameModel.Tick);
            }
        }

        private static GameModel CombatFixture(HeroKind hero, int seed = 1)
        {
            var model = new GameModel(1, hero, 0, seed);
            model.StartWave();
            model.Hero.AttackCooldown = 999;
            model.Hero.FrogCooldown = 999;
            model.Hero.SunCooldown = 999;
            model.Hero.DecapitateCooldown = 999;
            return model;
        }

        private static Enemy AddTarget(GameModel model, float offset = 0, float hp = 1000, bool boss = false)
        {
            Enemy target = model.CreateEnemy(boss ? SkeletonKind.Boss : SkeletonKind.Normal,
                model.PathLength - BalanceData.Current.heroRules.spawnDistanceFromExit + offset);
            target.Hp = target.MaxHp = hp; target.AttackCooldown = 999; target.SpecialCooldown = 999;
            model.Enemies.Add(target);
            return target;
        }

        private static Enemy AddSkeleton(GameModel model, SkeletonKind kind, float offset = 0, bool nearHero = true, bool summoned = false)
        {
            Enemy target = model.CreateEnemy(kind,
                nearHero ? model.PathLength - BalanceData.Current.heroRules.spawnDistanceFromExit + offset : offset, summoned);
            target.AttackCooldown = 999;
            model.Enemies.Add(target);
            return target;
        }

        private static void CheckCombat()
        {
            var archer = CombatFixture(HeroKind.Circe);
            Enemy magicTarget = AddTarget(archer, -120, 10);
            archer.Hero.AttackCooldown = 0;
            StepAtContact(archer);
            Check(Near(magicTarget.Hp, 10) && archer.Projectiles.Count > 0, "Ranged attack dealt damage before projectile impact");
            Advance(archer, .8f);
            Check(Near(magicTarget.Hp, 5), "Circe basic projectile did not deal five damage");

            var swordsman = CombatFixture(HeroKind.Achilles);
            Enemy meleeTarget = AddTarget(swordsman, -20, 10);
            swordsman.Hero.AttackCooldown = 0;
            StepAtContact(swordsman);
            Check(Near(meleeTarget.Hp, 7.5f), "Achilles fractional 2.5 damage was rounded");

            var physical = CombatFixture(HeroKind.Achilles); Enemy armored = AddSkeleton(physical, SkeletonKind.Knight, -20);
            physical.Hero.AttackCooldown = 0; StepAtContact(physical);
            Check(Near(armored.Hp, armored.MaxHp - 1.25f), "Knight armor did not absorb fifty percent of physical damage");
            var magical = CombatFixture(HeroKind.Circe); Enemy armoredMagic = AddSkeleton(magical, SkeletonKind.Knight, -120);
            magical.Hero.AttackCooldown = 0; Advance(magical, 1.1f);
            Check(Near(armoredMagic.Hp, armoredMagic.MaxHp - 5), "Knight physical armor incorrectly absorbed magical damage");

            var attacked = CombatFixture(HeroKind.Circe);
            Enemy attacker = AddTarget(attacked); attacker.AttackCooldown = 0;
            StepAtContact(attacked);
            Check(Near(attacked.Hero.Hp, 245), "Enemy basic attack did not damage hero");
            Advance(attacked, .5f);
            Check(Near(attacked.Hero.Hp, 245), "Enemy attacked before its cooldown elapsed");
            Advance(attacked, .55f);
            Check(Near(attacked.Hero.Hp, 240), "Enemy did not attack after its cooldown elapsed");

            var sharedTarget = CombatFixture(HeroKind.Circe);
            Check(sharedTarget.Build(0, TowerKind.Archer) && sharedTarget.Build(1, TowerKind.Archer), "Double-shot fixture could not build");
            Enemy fragile = sharedTarget.CreateEnemy(SkeletonKind.Normal, 247);
            fragile.Hp = 3; fragile.AttackCooldown = 999;
            sharedTarget.Enemies.Add(fragile); int initialGold = sharedTarget.Gold;
            StepAtContact(sharedTarget);
            Advance(sharedTarget, .5f);
            Check(sharedTarget.Kills == 1 && sharedTarget.Gold == initialGold + fragile.Reward, "Two attackers granted duplicate kill rewards");
        }

        private static void CheckDeathAndRespawn()
        {
            var model = CombatFixture(HeroKind.Circe);
            Enemy boss = AddTarget(model, 0, 1000, true); boss.AttackCooldown = 0;
            model.Hero.Hp = 1;
            StepAtContact(model);
            Check(!model.Hero.Alive && Near(model.Hero.RespawnRemaining, 60, GameModel.Tick + .001f), "Hero death did not start a sixty-second respawn");
            Vector2 position = model.Hero.Position;
            Check(!model.MoveHero(new Vector2(250, 500)), "Dead hero accepted a movement order");
            Advance(model, 59.8f, true);
            Check(!model.Hero.Alive && Vector2.Distance(model.Hero.Position, position) < .01f, "Dead hero moved or returned before sixty seconds");
            Advance(model, .3f, true);
            Check(model.Hero.Alive && Near(model.Hero.Hp, model.Hero.MaxHp) && Near(model.Hero.RespawnRemaining, 0), "Hero did not return at full health after sixty seconds");
        }

        private static void CheckTransformationsAndBossImmunity()
        {
            var magical = CombatFixture(HeroKind.Circe, SeedFor(.05f, true, 2));
            Enemy boss = AddTarget(magical, -120, 10000, true);
            magical.Hero.FrogCooldown = 0; magical.Hero.AttackCooldown = 0;
            Advance(magical, 1.1f);
            Check(!boss.IsFrog && Near(boss.Hp, 10000), "Boss took Circe magic damage or was transformed by hex");
            foreach (ProjectileKind kind in new[] { ProjectileKind.Arcane, ProjectileKind.Fireball })
            {
                magical.Projectiles.Add(new HeroProjectile { Kind = kind, Damage = 200, TargetId = boss.Id, Position = magical.Position(boss.Distance) });
                StepAtContact(magical);
                Check(Near(boss.Hp, 10000) && !boss.IsFrog, "Boss accepted a magical projectile: " + kind);
            }
            var magicTowers = CombatFixture(HeroKind.Circe);
            Check(magicTowers.Build(0, TowerKind.Ember) && magicTowers.Build(1, TowerKind.Frost), "Magic immunity tower fixture could not build");
            Enemy towerBoss = magicTowers.CreateEnemy(SkeletonKind.Boss, 247);
            magicTowers.Enemies.Add(towerBoss); Advance(magicTowers, .2f);
            Check(Near(towerBoss.Hp, 10000) && Near(towerBoss.Slow, 0), "Ember or Frost bypassed complete boss magic immunity");
            var physical = CombatFixture(HeroKind.Achilles);
            Enemy physicalBoss = AddTarget(physical, -20, 10000, true);
            physical.Hero.AttackCooldown = 0; StepAtContact(physical);
            Check(Near(physicalBoss.Hp, 9997.5f), "Boss was incorrectly immune to physical melee damage");
            var arrows = CombatFixture(HeroKind.Circe);
            Check(arrows.Build(0, TowerKind.Archer), "Physical immunity fixture could not build");
            float arrowDistance=0,nearest=float.MaxValue;
            for(float distance=0;distance<=arrows.PathLength;distance++)
            {
                float separation=(arrows.Position(distance)-GameModel.Sites[0]).sqrMagnitude;
                if(separation<nearest){nearest=separation;arrowDistance=distance;}
            }
            Enemy arrowBoss = arrows.CreateEnemy(SkeletonKind.Boss, arrowDistance); arrows.Enemies.Add(arrowBoss);
            for(int tick=0;tick<90 && Near(arrowBoss.Hp,10000);tick++) arrows.Step(GameModel.Tick);
            Check(Near(arrowBoss.Hp, 10000 - 3.8910025f * 1.05f), "Archer physical damage did not damage the boss");

            var ordinary = CombatFixture(HeroKind.Circe, SeedFor(.05f, true, 2));
            Enemy target = AddTarget(ordinary, -120, 1000); int gold = ordinary.Gold;
            ordinary.Hero.FrogCooldown = 0; ordinary.Hero.AttackCooldown = 0; StepAtContact(ordinary);
            Check(target.IsFrog && Near(ordinary.Hero.FrogCooldown, 30), "Successful five-percent attack hex did not start thirty-second cooldown");
            ordinary.Hero.AttackCooldown = 999;
            Advance(ordinary, 2.8f);
            Check(target.IsFrog && ordinary.Kills == 0, "Ordinary frog died before its three-second duration");
            Advance(ordinary, .25f);
            Check(target.Dead && ordinary.Kills == 1 && ordinary.Gold == gold + target.Reward, "Ordinary frog did not disappear with exactly one reward");
            Advance(ordinary, .3f);
            Check(ordinary.Kills == 1 && ordinary.Gold == gold + target.Reward, "Frog disappearance granted a repeated reward");

            var missed = CombatFixture(HeroKind.Circe, SeedFor(.05f, false, 2));
            Enemy missedTarget = AddTarget(missed, -120, 1000);
            missed.Hero.FrogCooldown = 0; missed.Hero.AttackCooldown = 0; StepAtContact(missed);
            Check(!missedTarget.IsFrog && Near(missed.Hero.FrogCooldown, 0), "Missed five-percent hex rolled into a guaranteed transformation or started cooldown");
            var cooling = CombatFixture(HeroKind.Circe, SeedFor(.05f, true, 2));
            Enemy coolingTarget = AddTarget(cooling, -120, 1000);
            cooling.Hero.FrogCooldown = 10; cooling.Hero.AttackCooldown = 0; StepAtContact(cooling);
            Check(!coolingTarget.IsFrog && cooling.Hero.FrogCooldown > 9, "Hex ignored its successful-cast cooldown");
            var idleHex = CombatFixture(HeroKind.Circe, SeedFor(.05f, true));
            Enemy idleTarget = AddTarget(idleHex, -120, 1000);
            idleHex.Hero.FrogCooldown = 0; StepAtContact(idleHex);
            Check(!idleTarget.IsFrog, "Hex proc happened without a basic attack");

            foreach (HeroKind hero in new[] { HeroKind.Circe, HeroKind.Achilles })
            {
                var immune = CombatFixture(hero); Enemy immuneBoss = AddTarget(immune, 0, 777, true);
                if (hero == HeroKind.Circe) immune.Hero.SunCooldown = 0;
                else immune.Hero.DecapitateCooldown = 0;
                StepAtContact(immune);
                Check(!immuneBoss.Dead && Near(immuneBoss.Hp, 777), "Boss died to sunbeam or decapitation");
                var normal = CombatFixture(hero); Enemy normalTarget = AddTarget(normal, 0, 777);
                if (hero == HeroKind.Circe) normal.Hero.SunCooldown = 0;
                else normal.Hero.DecapitateCooldown = 0;
                StepAtContact(normal);
                Check(normalTarget.Dead && normal.Kills == 1, "Instant-kill skill failed against ordinary enemy");
            }
            var knife = CombatFixture(HeroKind.Achilles); Enemy knifeBoss = AddTarget(knife, 0, 777, true);
            knife.Projectiles.Add(new HeroProjectile { Kind = ProjectileKind.Knife, InstantKill = true, TargetId = knifeBoss.Id, Position = knife.Position(knifeBoss.Distance) });
            StepAtContact(knife);
            Check(!knifeBoss.Dead && Near(knifeBoss.Hp, 777), "Knife ignored boss instant-kill immunity");
        }

        private static int SeedFor(float chance, bool succeeds, int draw = 1)
        {
            for (int seed = 0; seed < 10000; seed++)
            {
                var random = new System.Random(seed);
                double result = 0;
                for (int i = 0; i < draw; i++) result = random.NextDouble();
                if ((result < chance) == succeeds) return seed;
            }
            throw new Exception("Could not construct seeded probability fixture");
        }

        private static void ProjectileHit(GameModel model, Enemy target, float damage, bool instantKill = false)
        {
            model.Projectiles.Add(new HeroProjectile {
                Kind = instantKill ? ProjectileKind.Knife : ProjectileKind.Arcane,
                Damage = damage, InstantKill = instantKill, TargetId = target.Id,
                Position = model.Position(target.Distance)
            });
            StepAtContact(model);
        }

        private static void CheckSkeletonAbilities()
        {
            var ninjaFight = CombatFixture(HeroKind.Circe);
            Enemy ninja = AddSkeleton(ninjaFight, SkeletonKind.Ninja); ninja.Hp = ninja.MaxHp = 100;
            var hitMethod = typeof(GameModel).GetMethod("Hit", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, new[] { typeof(Enemy), typeof(float), typeof(bool) }, null);
            Check(hitMethod != null, "Physical-hit fixture could not find the combat damage entry point");
            Check(!ninja.DodgeReady, "Ninja evade became ready before its first four seconds");
            hitMethod.Invoke(ninjaFight, new object[] { ninja, 5f, false });
            Check(Near(ninja.Hp, 95), "Ninja evaded before its initial cooldown");
            Advance(ninjaFight, 4.1f);
            Check(ninja.DodgeReady, "Ninja did not arm its guaranteed evade after four seconds");
            hitMethod.Invoke(ninjaFight, new object[] { ninja, 5f, false });
            Check(Near(ninja.Hp, 95) && !ninja.DodgeReady, "Ready ninja did not consume exactly one evade");
            hitMethod.Invoke(ninjaFight, new object[] { ninja, 5f, false });
            Check(Near(ninja.Hp, 90), "Ninja evade blocked more than one incoming hit");
            Advance(ninjaFight, 3.8f); Check(!ninja.DodgeReady, "Ninja evade rearmed too early");
            Advance(ninjaFight, .3f);
            Check(ninja.DodgeReady, "Ninja evade did not rearm after four seconds");
            ProjectileHit(ninjaFight, ninja, 5);
            Check(Near(ninja.Hp, 85) && ninja.DodgeReady, "Magical damage was dodged or consumed the physical dodge charge");
            ProjectileHit(ninjaFight, ninja, 0, true);
            Check(ninja.Dead && ninjaFight.Kills == 1 && ninja.DodgeReady, "Instant kill was dodged or consumed the physical dodge charge");

            var ninjaHexFight = CombatFixture(HeroKind.Circe, SeedFor(.05f, true, 2));
            Enemy hexNinja = AddSkeleton(ninjaHexFight, SkeletonKind.Ninja, -120);
            hexNinja.Hp = hexNinja.MaxHp = 1000; // Isolate hex from a simultaneous fireball proc before its deferred cast.
            Advance(ninjaHexFight, 4.1f);
            ninjaHexFight.Hero.FrogCooldown = 0; ninjaHexFight.Hero.AttackCooldown = 0;
            StepAtContact(ninjaHexFight);
            Check(hexNinja.IsFrog && hexNinja.DodgeReady && Near(ninjaHexFight.Hero.FrogCooldown, 30),
                "Successful hex was dodged, consumed the physical dodge charge or failed to start its cooldown");

            var summoning = CombatFixture(HeroKind.Circe);
            Enemy pharaoh = AddSkeleton(summoning, SkeletonKind.Tutankhamun, 200, false);
            Advance(summoning, 7.8f);
            Check(!summoning.Enemies.Exists(enemy => enemy.IsSummoned), "Pharaoh summoned before eight seconds");
            Advance(summoning, .65f);
            Enemy mummy = summoning.Enemies.Find(enemy => enemy.IsSummoned);
            Check(mummy != null && mummy.Skeleton == SkeletonKind.Normal && Near(mummy.MaxHp, 12) && Near(mummy.AttackDamage, 5) && mummy.Reward == 1,
                "Pharaoh summon did not have twelve HP, five damage and one gold");
            Advance(summoning, 8.1f);
            Check(summoning.LivingSummons(pharaoh.Id) == 1 && pharaoh.SummonsCreated == 1, "Pharaoh bypassed its one-living-mummy limit");
            int summonGold = summoning.Gold;
            ProjectileHit(summoning, mummy, 1000);
            Check(mummy.Dead && summoning.Gold == summonGold + 1, "Killing summoned mummy did not award exactly one gold");
            Advance(summoning, 8.1f);
            Check(summoning.LivingSummons(pharaoh.Id) == 1 && pharaoh.SummonsCreated == 2, "Pharaoh did not resume summoning after its first mummy died");
            pharaoh.SpecialCooldown = 999;

            var rebirth = CombatFixture(HeroKind.Circe);
            Enemy bearer = AddSkeleton(rebirth, SkeletonKind.Sarcophagus, 200, false);
            Check(bearer.HasSarcophagus, "Original sarcophagus bearer lacks its coffin");
            int rebirthGold = rebirth.Gold;
            ProjectileHit(rebirth, bearer, 1000);
            Enemy returned = rebirth.Enemies.Find(enemy => enemy.Skeleton == SkeletonKind.Sarcophagus && !enemy.Dead);
            Check(bearer.Dead && returned != null && !returned.HasSarcophagus && returned.IsSummoned && returned.Reward == 1 && Near(returned.MaxHp, 75),
                "Sarcophagus did not release exactly one coffin-free copy");
            Advance(rebirth, 1.22f);
            ProjectileHit(rebirth, returned, 1000);
            Check(rebirth.Kills == 2 && rebirth.Gold == rebirthGold + 4 && !rebirth.Enemies.Exists(enemy => enemy.Skeleton == SkeletonKind.Sarcophagus && !enemy.Dead),
                "Sarcophagus copy repeated its rebirth or the original and copy did not award four gold total");

            var crawlerFight = CombatFixture(HeroKind.Circe);
            Enemy crawler = AddSkeleton(crawlerFight, SkeletonKind.Crawler); crawler.AttackCooldown = 0;
            StepAtContact(crawlerFight);
            Check(Near(crawlerFight.Hero.Hp, 200), "Crawler bite did not deal tenfold ordinary damage");

            var pirateFight = CombatFixture(HeroKind.Circe);
            Enemy pirate = AddSkeleton(pirateFight, SkeletonKind.Pirate, -120);
            Advance(pirateFight, 4.9f);
            Check(pirateFight.EnemyProjectiles.Count == 0 && Near(pirateFight.Hero.Hp, 250), "Pirate fired before its five-second interval");
            Advance(pirateFight, .56f);
            Check(pirateFight.EnemyProjectiles.Count == 1 && Near(pirateFight.Hero.Hp, 250), "Pirate gun hit before its projectile arrived");
            Advance(pirateFight, .3f);
            Check(Near(pirateFight.Hero.Hp, 210), "Pirate projectile did not deal eightfold ordinary damage");
            pirateFight.EnemyProjectiles.Add(new EnemyProjectile {
                Position = pirateFight.Hero.Position, Damage = 40, Speed = 300,
                TargetLifeSerial = pirateFight.Hero.LifeSerial - 1
            });
            StepAtContact(pirateFight);
            Check(Near(pirateFight.Hero.Hp, 210), "Projectile from an earlier hero life damaged the current life");

            foreach (SkeletonKind kind in new[] { SkeletonKind.Samurai, SkeletonKind.TRex })
                foreach (bool succeeds in new[] { false, true })
                {
                    float chance = kind == SkeletonKind.Samurai ? .2f : .5f;
                    var execution = CombatFixture(HeroKind.Circe, SeedFor(chance, succeeds));
                    Enemy attacker = AddSkeleton(execution, kind); attacker.AttackCooldown = 0;
                    StepAtContact(execution);
                    Check(Near(execution.Hero.Hp, succeeds ? 0 : 245) && execution.Hero.Alive != succeeds,
                        "Seeded skeleton execution chance or fallback melee damage is incorrect: " + kind);
                    if (succeeds) Check(Near(execution.Hero.RespawnRemaining, 60, GameModel.Tick + .001f), "Skeleton execution bypassed normal hero resurrection");
                }

            foreach (bool succeeds in new[] { false, true })
            {
                var lightning = CombatFixture(HeroKind.Circe, SeedFor(.75f, succeeds));
                AddSkeleton(lightning, SkeletonKind.Warlock, 100, false);
                Advance(lightning, 3.8f);
                Check(Near(lightning.Hero.Hp, 250), "Warlock rolled lightning before four seconds");
                Advance(lightning, .68f);
                Check(lightning.Hero.Alive && Near(lightning.Hero.Hp, succeeds ? 1 : 250), "Seeded warlock lightning did not leave the hero at one HP or produce a miss");
                Check(lightning.EnemyEffects.Exists(effect => effect.Kind == (succeeds ? "lightning" : "smoke")), "Warlock success/failure effect is missing");
            }

            var boxing = CombatFixture(HeroKind.Circe);
            Enemy boxer = AddSkeleton(boxing, SkeletonKind.Boxer); boxer.AttackCooldown = 0;
            boxing.Hero.AttackCooldown = 0; StepAtContact(boxing);
            Check(Near(boxing.Hero.Hp, 200) && Near(boxing.Hero.KnockdownRemaining, 10) && boxing.Projectiles.Count == 0,
                "Boxer did not replace melee damage with twenty percent max HP and prevent hero attacks");
            Vector2 knockedPosition = boxing.Hero.Position;
            Check(!boxing.MoveHero(new Vector2(250, 500)), "Knocked-down hero accepted movement");
            Advance(boxing, 1.05f);
            Check(Near(boxing.Hero.Hp, 150) && boxing.Hero.KnockdownRemaining > 9.9f && Vector2.Distance(boxing.Hero.Position, knockedPosition) < .01f,
                "Second boxer hit did not refresh knockdown or applied an unrequested special cooldown");
            boxing.Enemies.Clear(); boxing.Hero.AttackCooldown = 999;
            Advance(boxing, 10.1f);
            Check(Near(boxing.Hero.KnockdownRemaining, 0) && boxing.MoveHero(new Vector2(250, 500)), "Hero did not recover movement after knockdown expired");
        }

        private static void CheckSeededProcs()
        {
            float fireChance = BalanceData.Current.Hero(0).skills[2].chance;
            Check(Near(fireChance, .1f), "Fireball proc probability differs from agreed ten percent");
            foreach (bool succeeds in new[] { false, true })
            {
                int seed = SeedFor(fireChance, succeeds);
                var first = CombatFixture(HeroKind.Circe, seed); Enemy a = AddTarget(first, -120);
                var second = CombatFixture(HeroKind.Circe, seed); Enemy b = AddTarget(second, -120);
                first.Hero.AttackCooldown = 0; second.Hero.AttackCooldown = 0;
                StepAtContact(first); StepAtContact(second);
                int fireballs = first.Projectiles.FindAll(projectile => projectile.Kind == ProjectileKind.Fireball).Count;
                Check(fireballs == (succeeds ? 3 : 0), "Seeded fireball attack did not produce the expected volley");
                foreach (HeroProjectile projectile in first.Projectiles)
                    if (projectile.Kind == ProjectileKind.Fireball) Check(Near(projectile.Damage, 20), "A fireball did not carry twenty damage");
                Advance(first, 1); Advance(second, 1);
                Check(Near(a.Hp, succeeds ? 935 : 995) && Near(a.Hp, b.Hp) && first.Kills == second.Kills, "Seeded ranged attack was not repeatable or dealt wrong damage");
            }

            float knifeChance = BalanceData.Current.Hero(1).skills[2].chance;
            foreach (bool succeeds in new[] { false, true })
            {
                var knife = CombatFixture(HeroKind.Achilles, SeedFor(knifeChance, succeeds));
                Enemy target = AddTarget(knife, -40); int gold = knife.Gold;
                knife.Hero.AttackCooldown = 0; StepAtContact(knife);
                Check(knife.Projectiles.Exists(projectile => projectile.Kind == ProjectileKind.Knife) == succeeds, "Knife did not roll on the seeded melee hit");
                Advance(knife, .5f);
                Check(target.Dead == succeeds && knife.Kills == (succeeds ? 1 : 0), "Knife did not instantly kill its ordinary target");
                Check(knife.Gold == gold + (succeeds ? target.Reward : 0), "Knife granted the wrong reward");
            }
            float dodgeChance = BalanceData.Current.Hero(1).skills[1].chance;
            foreach (bool succeeds in new[] { false, true })
            {
                var dodge = CombatFixture(HeroKind.Achilles, SeedFor(dodgeChance, succeeds));
                Enemy attacker = AddTarget(dodge); attacker.AttackCooldown = 0;
                StepAtContact(dodge);
                Check(Near(dodge.Hero.Hp, succeeds ? 350 : 345), "Seeded dodge did not apply to incoming damage");
            }
        }

        private static void CheckTerminalStates()
        {
            var defeat = new GameModel(); defeat.StartWave();
            for (int i = 0; i < BalanceData.Current.economy.startingLives; i++)
                defeat.Enemies.Add(defeat.CreateEnemy(SkeletonKind.Normal, defeat.PathLength));
            defeat.Step(GameModel.Tick);
            Check(defeat.State == RunState.Defeat && defeat.Lives == 0 && defeat.Kills == 0, "Escaping enemies did not defeat the base without kill rewards");
            int defeatGold = defeat.Gold; defeat.Hero.Hp = 100; float defeatedHp = defeat.Hero.Hp;
            Advance(defeat, 10);
            Check(defeat.Gold == defeatGold && !defeat.StartWave() && !defeat.Build(0, TowerKind.Archer) && !defeat.TryCallNextWave() && Near(defeat.Hero.Hp, defeatedHp),
                "Defeat still permits economy changes, early-call gold or regeneration");

            // Clear ordinary waves directly to test timing independently of tower balance.
            var victory = new GameModel(); victory.StartWave();
            int priorWave = 1; float priorStart = 0;
            for (int tick = 0; tick < 35 * 60 * 60 && victory.Wave < 20; tick++)
            {
                victory.Enemies.Clear();
                victory.Step(GameModel.Tick);
                if (victory.Wave != priorWave)
                {
                    Check(victory.Wave == priorWave + 1 && victory.Elapsed >= priorStart + 89.99f, "Automatic wave sequence skipped a wave or its ninety-second slot");
                    priorWave = victory.Wave; priorStart = victory.Elapsed;
                }
            }
            Check(victory.Wave == 20 && victory.State != RunState.Victory && victory.Elapsed >= 1709.99f, "Final wave began before nineteen ninety-second slots");
            for (int tick = 0; tick < 600 && victory.Spawned == 0; tick++) victory.Step(GameModel.Tick);
            Enemy finalBoss = victory.Enemies.Find(enemy => enemy.IsBoss);
            Check(finalBoss != null && victory.BossSpawnSerial == 1 && victory.Spawned == 1, "Final boss did not spawn exactly once");
            Check(victory.Build(0, TowerKind.Archer), "Final victory fixture could not build a physical tower");
            finalBoss.Hp = .1f;
            victory.Step(GameModel.Tick);
            for (int tick = 0; tick < 180 && !finalBoss.Dead; tick++) victory.Step(GameModel.Tick);
            Check(victory.State == RunState.Victory && finalBoss.Dead && victory.Elapsed < 1800,
                "Killing the final boss did not immediately complete the map or introduced a mandatory thirty-minute wait");
            int victoryGold = victory.Gold; victory.Hero.Hp = 100; float finalHp = victory.Hero.Hp;
            Advance(victory, 10);
            Check(victory.Gold == victoryGold && !victory.StartWave() && !victory.Build(0, TowerKind.Archer) && !victory.TryCallNextWave() && Near(victory.Hero.Hp, finalHp),
                "Victory granted repeated rewards, early-call gold, regeneration or further construction");

            // Isolate the final gate with every earlier wave still pending; threshold behavior is exercised by real kills above.
            var overlap = CombatFixture(HeroKind.Achilles);
            while (overlap.Wave < 20)
            {
                overlap.WaveRuns[overlap.Wave - 1].Spawned = overlap.EarlyWaveRequiredSpawned;
                overlap.WaveRuns[overlap.Wave - 1].EligibleAtWaveElapsed = 0;
                Check(overlap.TryCallNextWave(), "Final-gate fixture could not call the next wave");
            }
            Advance(overlap, .7f);
            Enemy overlapBoss = overlap.Enemies.Find(enemy => enemy.IsBoss);
            Check(overlapBoss != null, "Overlapping final wave did not spawn its boss");
            overlapBoss.Distance = overlap.PathLength - BalanceData.Current.heroRules.spawnDistanceFromExit;
            overlapBoss.Hp = .1f; overlapBoss.AttackCooldown = 999;
            overlap.Hero.AttackCooldown = 0; StepAtContact(overlap);
            Check(overlapBoss.Dead && overlap.State != RunState.Victory && overlap.WavesCompleted < 20,
                "Killing the final boss completed the map while earlier waves still had pending enemies");
            overlap.Hero.AttackCooldown = 999;
            Advance(overlap, 61, true);
            Check(overlap.State == RunState.Victory && overlap.WavesCompleted == 20,
                "Finishing all pending overlapping waves did not complete the already defeated boss's map");
        }

        [Serializable]
        private sealed class PlayabilityReport
        {
            public string generatedUtc;
            public string strategy;
            public PlayabilityRun[] runs;
        }

        [Serializable]
        private sealed class PlayabilityRun
        {
            public int map;
            public string hero;
            public int seed;
            public string outcome;
            public int wave;
            public int kills;
            public int lives;
            public int gold;
            public float heroHp;
            public float simulationSeconds;
        }

        private sealed class StrategyDefinition
        {
            public string Name;
            public int[] Sites;
            public TowerKind[] Kinds;
            public int OpeningCount = 2;
            public int LevelBeforeExpansion = 1;
            public Vector2? Rally;
            public bool CallEarly;
            public float ClearedCallSeconds;
            public bool ConvertToArchers;
            public bool RetreatAndRecover;
        }

        [Serializable]
        private sealed class StrategyReport
        {
            public string generatedUtc;
            public string rules;
            public StrategyRun[] runs;
        }

        [Serializable]
        private sealed class StrategyRun
        {
            public string strategy;
            public int[] siteOrder;
            public string[] towerKinds;
            public int openingCount;
            public int levelBeforeExpansion;
            public string heroOrders;
            public bool callEarly;
            public string earlyCallPolicy;
            public int earlyCalls;
            public int earlyCallGold;
            public bool convertToArchers;
            public bool hypothesisOverride;
            public float tutankhamunHp;
            public PlayabilityRun result;
            public string[] finalTowers;
            public int peakEnemies;
            public float[] waveStartSeconds;
        }

        [MenuItem("Skeleton Defender/Search tower strategies (separate diagnostic)")]
        public static void RunStrategySearch()
        {
            BalanceData.Reload();
            // 0.8.2 removed old site 6; keep the order and tower kind of each surviving place.
            int[] front = { 0, 1, 3, 4, 2, 5, 6, 8, 7 };
            var fire = new TowerKind[front.Length];
            for (int i = 0; i < fire.Length; i++) fire[i] = TowerKind.Ember;
            var mixed = (TowerKind[])fire.Clone(); mixed[2] = TowerKind.Archer; mixed[3] = TowerKind.Frost; mixed[5] = TowerKind.Archer; mixed[7] = TowerKind.Archer;
            var strategies = new[] {
                new StrategyDefinition { Name = "Fire coverage then physical boss defence", Sites = front, Kinds = fire, ConvertToArchers = true, RetreatAndRecover = true },
                new StrategyDefinition { Name = "Control: front fire with recovering front hero", Sites = front, Kinds = fire, LevelBeforeExpansion = 2, Rally = new Vector2(150, 280), ConvertToArchers = true, RetreatAndRecover = true },
                new StrategyDefinition { Name = "Control: mixed towers with recovering front hero", Sites = front, Kinds = mixed, LevelBeforeExpansion = 2, Rally = new Vector2(150, 280), ConvertToArchers = true, RetreatAndRecover = true },
                new StrategyDefinition { Name = "Mixed towers with early wave calls", Sites = front, Kinds = mixed, LevelBeforeExpansion = 2, Rally = new Vector2(150, 280), CallEarly = true, ConvertToArchers = true, RetreatAndRecover = true },
                new StrategyDefinition { Name = "Mixed towers with cleared wave calls after 80 seconds", Sites = front, Kinds = mixed, LevelBeforeExpansion = 2, Rally = new Vector2(150, 280), CallEarly = true, ClearedCallSeconds = 80, ConvertToArchers = true, RetreatAndRecover = true }
            };
            ExecuteStrategies(strategies, "strategy-results.json");
        }


        private static void ExecuteStrategies(StrategyDefinition[] strategies, string reportFile)
        {
            const int seed = 14257;
            const int maximumTicks = 60 * 60 * 60;
            var runs = new System.Collections.Generic.List<StrategyRun>();
            var report = new StrategyReport {
                generatedUtc = DateTime.UtcNow.ToString("O"),
                rules = "Actual current JSON only; no health hypotheses or stat overrides. No equipment, free gold, deleted enemies or save writes. Build opening towers, upgrade before expanding; after all sites are built, upgrade to maximum. Before the final boss, sell magical towers at the ordinary refund and build/upgrade archers using earned gold. Hero retreats below 30% HP and returns at 98%; at the boss, hold the first overlapping archer positions. Optional early calls use the public API and the actual dynamic bonus. Seed14257; defeat is an observation, not a failed assertion."
            };
            string directory = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), "work");
            System.IO.Directory.CreateDirectory(directory);
            foreach (StrategyDefinition strategy in strategies)
                foreach (HeroKind hero in new[] { HeroKind.Circe, HeroKind.Achilles })
                {
                    var model = new GameModel(1, hero, 0, seed);
                    var waveStarts = new System.Collections.Generic.List<float>();
                    bool retreating = false;
                    Vector2 retreatPoint = model.Hero.Position;
                    int tick = 0, previousWave = 0, peakEnemies = 0;
                    for (; tick < maximumTicks && model.State != RunState.Victory && model.State != RunState.Defeat; tick++)
                    {
                        if (tick % 30 == 0)
                        {
                            // Commands spend only earned gold and use exactly the public player API.
                            bool finalDefence = strategy.ConvertToArchers && (model.Wave == 20 || (model.Wave == 19 && model.WaveCleared));
                            for (int action = 0; action < 40 && (finalDefence ? BuyPhysicalBossDefence(model, strategy.Sites) : BuyForStrategy(model, strategy)); action++) { }
                            if (strategy.RetreatAndRecover) CommandStrategyHero(model, strategy, ref retreating, ref retreatPoint);
                            else if (strategy.Rally.HasValue) model.MoveHero(strategy.Rally.Value);
                            if (strategy.CallEarly && model.CanCallNextWave && (strategy.ClearedCallSeconds <= 0 ||
                                (model.WaveCleared && model.Remaining == 0 && model.WaveElapsed >= strategy.ClearedCallSeconds)))
                                model.TryCallNextWave();
                        }
                        if (!model.HasStarted) model.StartWave();
                        model.Step(GameModel.Tick);
                        if (model.Wave != previousWave) { waveStarts.Add(model.Elapsed); previousWave = model.Wave; }
                        peakEnemies = Mathf.Max(peakEnemies, model.Enemies.Count);
                        if (tick % 60 == 0) CheckFiniteBattle(model);
                    }
                    CheckFiniteBattle(model);
                    var towers = new System.Collections.Generic.List<string>();
                    foreach (Tower tower in model.Towers) towers.Add(tower.Site + ":" + tower.Kind + ":L" + tower.Level);
                    var run = new StrategyRun {
                        strategy = strategy.Name, siteOrder = strategy.Sites,
                        towerKinds = Array.ConvertAll(strategy.Kinds, kind => kind.ToString()),
                        openingCount = strategy.OpeningCount, levelBeforeExpansion = strategy.LevelBeforeExpansion,
                        heroOrders = (strategy.Rally.HasValue ? "Rally to " + strategy.Rally.Value : "Initial exit position") + "; recover below 30% HP; tank the final boss near overlapping archers",
                        callEarly = strategy.CallEarly, convertToArchers = strategy.ConvertToArchers,
                        earlyCallPolicy = !strategy.CallEarly ? "none" : strategy.ClearedCallSeconds > 0 ? "all_clear_after_" + strategy.ClearedCallSeconds + "_seconds" : "first_eligible_threshold",
                        earlyCalls = model.WaveRuns.FindAll(wave => wave.EarlyCallUsed).Count,
                        earlyCallGold = model.TotalEarlyWaveGold,
                        hypothesisOverride = false,
                        tutankhamunHp = BalanceData.Current.Skeleton((int)SkeletonKind.Tutankhamun).hp,
                        result = new PlayabilityRun { map = 1, hero = InventoryItem.HeroName(hero), seed = seed,
                            outcome = model.State.ToString(), wave = model.Wave, kills = model.Kills,
                            lives = model.Lives, gold = model.Gold, heroHp = model.Hero.Hp, simulationSeconds = model.Elapsed },
                        finalTowers = towers.ToArray(), peakEnemies = peakEnemies, waveStartSeconds = waveStarts.ToArray()
                    };
                    runs.Add(run); report.runs = runs.ToArray();
                    System.IO.File.WriteAllText(System.IO.Path.Combine(directory, reportFile), JsonUtility.ToJson(report, true));
                    Debug.Log("SKELETON_STRATEGY: " + strategy.Name + " | " + JsonUtility.ToJson(run.result));
                    Check(model.State == RunState.Victory || model.State == RunState.Defeat,
                        "Strategy did not terminate within sixty simulated minutes: " + strategy.Name + " hero=" + hero + " wave=" + model.Wave);
                }
            Debug.Log("SKELETON_STRATEGY_SEARCH_COMPLETE: " + runs.Count + " runs recorded in work/" + reportFile + ".");
        }

        private static bool BuyForStrategy(GameModel model, StrategyDefinition strategy)
        {
            int built = 0;
            foreach (int site in strategy.Sites) if (model.At(site) != null) built++;
            if (built >= strategy.OpeningCount)
                foreach (int site in strategy.Sites)
                {
                    Tower tower = model.At(site);
                    int targetLevel = built == strategy.Sites.Length ? tower.Definition.maxLevel : strategy.LevelBeforeExpansion;
                    if (tower != null && tower.Level < targetLevel) return model.Upgrade(site);
                }
            for (int i = 0; i < strategy.Sites.Length; i++)
                if (model.At(strategy.Sites[i]) == null) return model.Build(strategy.Sites[i], strategy.Kinds[i]);
            return false;
        }

        private static bool BuyPhysicalBossDefence(GameModel model, int[] sites)
        {
            foreach (int site in sites)
                if (model.At(site) != null && model.At(site).Kind != TowerKind.Archer) return model.Sell(site);
            foreach (int site in sites)
                if (model.At(site) == null) return model.Build(site, TowerKind.Archer);
            foreach (int site in sites)
                if (model.At(site).Level < model.At(site).Definition.maxLevel) return model.Upgrade(site);
            return false;
        }

        private static void CommandStrategyHero(GameModel model, StrategyDefinition strategy, ref bool retreating, ref Vector2 retreatPoint)
        {
            if (!model.Hero.Alive) { retreating = false; return; }
            Enemy boss = model.Enemies.Find(enemy => enemy.IsBoss && !enemy.Dead);
            if (!retreating && model.Hero.Hp < model.Hero.MaxHp * .3f)
            {
                retreating = true;
                Vector2 threat = boss != null ? model.Position(boss.Distance) : model.Hero.Position;
                Vector2[] safePoints = { new Vector2(50, 580), new Vector2(330, 580), new Vector2(950, 590), new Vector2(990, 40), new Vector2(50, 40) };
                float closest = float.PositiveInfinity;
                foreach (Vector2 point in safePoints)
                {
                    float distance = Vector2.Distance(model.Hero.Position, point);
                    if (Vector2.Distance(point, threat) > 240 && distance < closest) { retreatPoint = point; closest = distance; }
                }
            }
            if (retreating && model.Hero.Hp >= model.Hero.MaxHp * .98f) retreating = false;
            if (retreating) model.MoveHero(retreatPoint);
            else if (boss != null)
                model.MoveHero(model.Position(Mathf.Max(300, boss.Distance + 12)));
            else if (strategy.Rally.HasValue) model.MoveHero(strategy.Rally.Value);
            else model.MoveHero(model.Position(model.PathLength - BalanceData.Current.heroRules.spawnDistanceFromExit));
        }

        [MenuItem("Skeleton Defender/Run playability diagnostics")]
        public static void RunPlayabilityDiagnostics()
        {
            const int seed = 14257;
            const int maximumTicks = 60 * 60 * 60;
            int[] sites = { 1, 3, 0, 5, 2, 6, 7, 4, 8 };
            TowerKind[] kinds = { TowerKind.Archer, TowerKind.Ember, TowerKind.Archer, TowerKind.Frost, TowerKind.Ember, TowerKind.Ember, TowerKind.Archer, TowerKind.Archer, TowerKind.Archer };
            var runs = new System.Collections.Generic.List<PlayabilityRun>();
            var report = new PlayabilityReport {
                generatedUtc = DateTime.UtcNow.ToString("O"),
                strategy = "Existing prototype tower purchase order, then sequential upgrades; stationary hero at initial position; no equipment; ordinary seeded combat. Victory is not required."
            };
            string directory = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), "work");
            System.IO.Directory.CreateDirectory(directory);
            for (int map = 1; map <= BalanceData.Current.mapCount; map++)
                foreach (HeroKind hero in new[] { HeroKind.Circe, HeroKind.Achilles })
                {
                    // Real combat only: no fixture removal, stat overrides, free gold or equipment.
                    var model = new GameModel(map, hero, 0, seed);
                    int tick = 0;
                    for (; tick < maximumTicks && model.State != RunState.Victory && model.State != RunState.Defeat; tick++)
                    {
                        if (tick % 30 == 0)
                        {
                            bool allBuilt = true;
                            for (int i = 0; i < sites.Length; i++)
                            {
                                if (model.At(sites[i]) != null) continue;
                                model.Build(sites[i], kinds[i]);
                                allBuilt = false;
                                break;
                            }
                            if (allBuilt)
                                foreach (int site in sites)
                                    if (model.At(site).Level < model.At(site).Definition.maxLevel)
                                    { model.Upgrade(site); break; }
                        }
                        if (!model.HasStarted) model.StartWave();
                        model.Step(GameModel.Tick);
                        if (tick % 60 == 0) CheckFiniteBattle(model);
                    }
                    CheckFiniteBattle(model);
                    var run = new PlayabilityRun {
                        map = map, hero = InventoryItem.HeroName(hero), seed = seed,
                        outcome = model.State.ToString(), wave = model.Wave, kills = model.Kills,
                        lives = model.Lives, gold = model.Gold, heroHp = model.Hero.Hp,
                        simulationSeconds = tick * GameModel.Tick
                    };
                    runs.Add(run);
                    report.runs = runs.ToArray();
                    System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "playability-results.json"), JsonUtility.ToJson(report, true));
                    Debug.Log("SKELETON_PLAYABILITY: " + JsonUtility.ToJson(run));
                    Check(model.State == RunState.Victory || model.State == RunState.Defeat,
                        "Real battle did not terminate within sixty simulated minutes: map=" + map + " hero=" + hero + " wave=" + model.Wave);
                }
        }

        private static void CheckFiniteBattle(GameModel model)
        {
            Check(Finite(model.Hero.Hp) && Finite(model.Hero.MaxHp) && Finite(model.Hero.Position.x) && Finite(model.Hero.Position.y) &&
                Finite(model.Hero.RespawnRemaining) && Finite(model.Hero.KnockdownRemaining) && Finite(model.Hero.IdleSeconds) && Finite(model.WaveElapsed), "Battle hero or clock contains NaN/infinity");
            foreach (Enemy enemy in model.Enemies)
                Check(Finite(enemy.Hp) && Finite(enemy.Distance) && Finite(enemy.AttackCooldown) && Finite(enemy.FrogRemaining) && Finite(enemy.SpecialCooldown) && Finite(enemy.DodgeCooldown), "Battle enemy contains NaN/infinity");
            foreach (HeroProjectile projectile in model.Projectiles)
                Check(Finite(projectile.Position.x) && Finite(projectile.Position.y) && Finite(projectile.Delay), "Battle projectile contains NaN/infinity");
            foreach (EnemyProjectile projectile in model.EnemyProjectiles)
                Check(Finite(projectile.Position.x) && Finite(projectile.Position.y) && Finite(projectile.Speed), "Enemy projectile contains NaN/infinity");
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
