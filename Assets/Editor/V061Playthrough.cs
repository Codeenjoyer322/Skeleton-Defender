using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace SkeletonDefender.Editor
{
    // Plays the public model exactly as a player can: no combat-state setters, injected enemies,
    // hypothetical balance, extra inventory, free money, or access to the player's saved profile.
    public static class V061Playthrough
    {
        [Serializable] public sealed class ActionEntry
        {
            public float seconds;
            public int wave, gold;
            public string command;
            public Vector2 point;
        }
        [Serializable] public sealed class WaveEntry
        {
            public int number, lives, gold, kills;
            public float seconds, heroHp;
        }
        [Serializable] public sealed class RunReport
        {
            public string policy, hero, outcome;
            public int seed, wavesReached, wavesCompleted, lives, kills, gold, heroDeaths, earlyWaveGold;
            public float elapsed, preparationSeconds, finalHeroHp;
            public string[] loadout, board;
            public EquipmentStats equipment;
            public int[] skillCasts = new int[2];
            public int peakEnemies, peakSummoners;
            public int originalKills, summonedKills, livesAfterWave3 = -1, livesAfterWave5 = -1;
            public float wave3Duration = -1;
            public int[] leakedOriginalsByKind = new int[14];
            public int[] leakedSummonsByKind = new int[14];
            public List<ActionEntry> actions = new List<ActionEntry>();
            public List<WaveEntry> waveStarts = new List<WaveEntry>();
            public List<WaveEntry> waveCompletions = new List<WaveEntry>();
        }
        [Serializable] public sealed class FullReport
        {
            public string generatedUtc, balanceSha256, rules;
            public bool victoryReached;
            public List<RunReport> runs = new List<RunReport>();
        }
        private sealed class Policy
        {
            public string Name;
            public HeroKind Hero;
            public bool Control;
            public bool Fire;
            public bool AggressiveCalls;
            public int ExpandBeforeUpgrade = 6;
            public int MinimumSkillWave = 1;
            public bool SaveForUpgrade;
            public bool OpeningEmber;
            public bool FullOpeningEmber;
            public bool HuntSummoners;
            public bool EmberToThree;
        }
        private sealed class Pilot
        {
            public bool Recovering;
            public Vector2 RestPoint;
            public float LastMoveLogged = -100;
        }
        private static readonly int[] SiteOrder = { 1, 0, 3, 4, 2, 5, 9, 7, 8, 6 };
        private static string ReportPath => Path.Combine(Path.GetDirectoryName(Application.dataPath), "work", "v061-full-playthrough.json");

        [Serializable] public sealed class CandidateReport
        {
            public string label;
            public bool experimental = true;
            public int wave3Normal, wave3Tutankhamun, summonLimit, summonMaxAlive;
            public float summonInterval, summonedHp;
            public List<RunReport> runs = new List<RunReport>();
        }
        [Serializable] public sealed class ComparisonReport
        {
            public string generatedUtc, rules;
            public List<CandidateReport> candidates = new List<CandidateReport>();
        }
        private static Policy[] BalancedPolicies() => new[] {
            new Policy { Name = "Circe: starter silk/staff, Ember0 level3, summoner hunting", Hero = HeroKind.Circe, OpeningEmber = true, EmberToThree = true, ExpandBeforeUpgrade = 4, SaveForUpgrade = true, MinimumSkillWave = 3, HuntSummoners = true },
            new Policy { Name = "Achilles: starter chain/sword, four upgraded archers", Hero = HeroKind.Achilles, ExpandBeforeUpgrade = 4, SaveForUpgrade = true, MinimumSkillWave = 3 }
        };

        [MenuItem("Skeleton Defender/Compare bounded-summoning balance candidates (0.6.2)")]
        public static void RunBalanceComparison()
        {
            BalanceData.Reload();
            BalanceData data = BalanceData.Current;
            SkeletonDefinition rule = data.Skeleton((int)SkeletonKind.Tutankhamun);
            WaveDefinition third = data.Wave(3);
            int oldLimit = rule.summonLimit, oldAlive = rule.summonMaxAlive;
            float oldInterval = rule.summonInterval, oldHp = rule.summonedHp;
            int[] oldComposition = third.skeletonCounts;
            var report = new ComparisonReport {
                generatedUtc = DateTime.UtcNow.ToString("O"),
                rules = "EXPERIMENTAL CANDIDATE COMPARISON. The listed Tutankhamun summon settings and wave3 composition are intentionally changed in the in-memory BalanceData BEFORE each new battle. Actual source JSON is never written; all temporary values are restored in finally. No HP/gold/lives/enemy mutations during gameplay. Same two legal starter-equipment policies and seed14257 for every candidate. Stop only after completion of wave5 or defeat. Tower/hero/item balance is unchanged."
            };
            report.candidates.Add(new CandidateReport { label = "A: limit only", wave3Normal = 50, wave3Tutankhamun = 25, summonInterval = 8, summonLimit = 2, summonMaxAlive = 1, summonedHp = 12 });
            report.candidates.Add(new CandidateReport { label = "B: gentle intro", wave3Normal = 67, wave3Tutankhamun = 8, summonInterval = 8, summonLimit = 2, summonMaxAlive = 1, summonedHp = 12 });
            report.candidates.Add(new CandidateReport { label = "C: moderate intro", wave3Normal = 63, wave3Tutankhamun = 12, summonInterval = 10, summonLimit = 2, summonMaxAlive = 1, summonedHp = 15 });
            string path = Path.Combine(Path.GetDirectoryName(ReportPath), "v062-balance-comparison.json");
            try
            {
                foreach (CandidateReport candidate in report.candidates)
                {
                    rule.summonInterval = candidate.summonInterval; rule.summonLimit = candidate.summonLimit;
                    rule.summonMaxAlive = candidate.summonMaxAlive; rule.summonedHp = candidate.summonedHp;
                    third.skeletonCounts = (int[])oldComposition.Clone();
                    third.skeletonCounts[(int)SkeletonKind.Normal] = candidate.wave3Normal;
                    third.skeletonCounts[(int)SkeletonKind.Tutankhamun] = candidate.wave3Tutankhamun;
                    foreach (Policy policy in BalancedPolicies())
                    {
                        RunReport run = Play(policy, 14257, 5); candidate.runs.Add(run);
                        File.WriteAllText(path, JsonUtility.ToJson(report, true));
                        Debug.Log("SKELETON_V062_CANDIDATE: " + candidate.label + " | " + run.hero + " | " + run.outcome +
                            " | completed=" + run.wavesCompleted + " livesAfter3=" + run.livesAfterWave3 + " livesAfter5=" + run.livesAfterWave5 + " kills=" + run.kills);
                    }
                }
            }
            finally
            {
                rule.summonInterval = oldInterval; rule.summonLimit = oldLimit; rule.summonMaxAlive = oldAlive; rule.summonedHp = oldHp;
                third.skeletonCounts = oldComposition;
            }
            Debug.Log("SKELETON_V062_COMPARISON_COMPLETE: " + path);
        }

        [MenuItem("Skeleton Defender/Validate chosen live balance on extra seeds (0.6.2)")]
        public static void RunBalanceValidation()
        {
            BalanceData.Reload();
            var report = new FullReport { generatedUtc = DateTime.UtcNow.ToString("O"), rules = "FINAL LIVE JSON; no candidate overrides. Two fixed extra seeds for both heroes, legal starter items and commands, completion of wave5 required for checkpoint." };
            string path = Path.Combine(Path.GetDirectoryName(ReportPath), "v062-balance-validation.json");
            foreach (int seed in new[] { 314159, 271828 })
                foreach (Policy policy in BalancedPolicies())
                {
                    RunReport run = Play(policy, seed, 5); report.runs.Add(run);
                    File.WriteAllText(path, JsonUtility.ToJson(report, true));
                    Debug.Log("SKELETON_V062_VALIDATION: " + run.hero + " seed=" + seed + " | " + run.outcome + " | lives3=" + run.livesAfterWave3 + " lives5=" + run.livesAfterWave5);
                }
            Debug.Log("SKELETON_V062_VALIDATION_COMPLETE: " + path);
        }

        [MenuItem("Skeleton Defender/Play full map on chosen live balance (0.6.2)")]
        public static void RunBalancedFullMap()
        {
            BalanceData.Reload();
            var report = new FullReport { generatedUtc = DateTime.UtcNow.ToString("O"), rules = "FINAL LIVE JSON; no candidate overrides. Legal starter items and player commands only. Full20-wave attempt with seed14257; stop after first victory or two hero policies." };
            string path = Path.Combine(Path.GetDirectoryName(ReportPath), "v062-full-playthrough.json");
            foreach (Policy policy in BalancedPolicies())
            {
                RunReport run = Play(policy, 14257); report.runs.Add(run); report.victoryReached |= run.outcome == "Victory";
                File.WriteAllText(path, JsonUtility.ToJson(report, true));
                Debug.Log("SKELETON_V062_FULL: " + run.hero + " | " + run.outcome + " | wave=" + run.wavesReached + " completed=" + run.wavesCompleted + " lives=" + run.lives + " seconds=" + run.elapsed);
                if (report.victoryReached) break;
            }
            Debug.Log("SKELETON_V062_FULL_COMPLETE: " + path);
        }

        [MenuItem("Skeleton Defender/Play full map with starter equipment (0.6.1)")]
        public static void Run()
        {
            BalanceData.Reload();
            string balancePath = Path.Combine(Application.dataPath, "Resources", "balance.json");
            string hash;
            using (SHA256 sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(balancePath))).Replace("-", "").ToLowerInvariant();
            var report = new FullReport {
                generatedUtc = DateTime.UtcNow.ToString("O"), balanceSha256 = hash,
                rules = "Deterministic automated full-map playthrough using actual balance.json and only legally granted Common starter items. Fixed 1/60s simulation; decisions every 0.25s. Commands are Equip, Build, Upgrade, Sell, MoveHero, TryCastSkill, TryCallNextWave and StartWave. No direct changes to HP, gold, lives, enemies, cooldowns, deaths or wave schedules. No PlayerPrefs/save reads or writes. Main goal is a complete victory; failed attempts are preserved. Skills target visible enemies, recovery uses legal movement, final defence uses earned gold and ordinary sell refunds. Seed14257 for all policies; stop after the first victory or the listed bounded attempts."
            };
            var policies = new[] {
                new Policy { Name = "Circe: physical coverage, moving spellcaster", Hero = HeroKind.Circe },
                new Policy { Name = "Circe: physical coverage plus central ice", Hero = HeroKind.Circe, Control = true },
                new Policy { Name = "Achilles: physical coverage and protected blocking", Hero = HeroKind.Achilles, Control = true },
                new Policy { Name = "Circe: upgraded front archers", Hero = HeroKind.Circe, ExpandBeforeUpgrade = 4 },
                new Policy { Name = "Circe: mixed centre with controlled early calls", Hero = HeroKind.Circe, Control = true, Fire = true, AggressiveCalls = true }
            };
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            foreach (Policy policy in policies)
            {
                RunReport run = Play(policy, 14257);
                report.runs.Add(run); report.victoryReached |= run.outcome == RunState.Victory.ToString();
                File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
                Debug.Log("SKELETON_V061_PLAYTHROUGH: " + run.policy + " | " + run.outcome + " | wave=" + run.wavesReached +
                    " completed=" + run.wavesCompleted + " time=" + run.elapsed + " lives=" + run.lives + " kills=" + run.kills + " skills=" + run.skillCasts[0] + "/" + run.skillCasts[1]);
                if (report.victoryReached) break;
            }
            Debug.Log("SKELETON_V061_PLAYTHROUGH_COMPLETE: victory=" + report.victoryReached + "; report=" + ReportPath);
        }

        [MenuItem("Skeleton Defender/Continue full-map playthrough attempts (0.6.1)")]
        public static void RunFollowups()
        {
            BalanceData.Reload();
            if (!File.Exists(ReportPath)) throw new InvalidOperationException("Initial playthrough report is missing.");
            string original = File.ReadAllText(ReportPath);
            string archive = Path.Combine(Path.GetDirectoryName(ReportPath), "v061-full-playthrough-initial-attempts.json");
            if (!File.Exists(archive)) File.WriteAllText(archive, original);
            FullReport report = JsonUtility.FromJson<FullReport>(original);
            report.rules += " Follow-up policies preserve Q/E until wave3, use the ordinary full wave slots, and reserve gold for planned upgrades instead of spending the reserved sum on extra level1 towers. Every completed WaveRun is recorded independently.";
            var policies = new[] {
                new Policy { Name = "Follow-up: four archers to level2, Circe spells reserved for summoners", Hero = HeroKind.Circe, ExpandBeforeUpgrade = 4, SaveForUpgrade = true, MinimumSkillWave = 3 },
                new Policy { Name = "Follow-up: front Ember2 plus archers, Circe spells on summoners", Hero = HeroKind.Circe, OpeningEmber = true, ExpandBeforeUpgrade = 5, SaveForUpgrade = true, MinimumSkillWave = 3 },
                new Policy { Name = "Follow-up: opening Ember2 concentration, Circe spells on summoners", Hero = HeroKind.Circe, OpeningEmber = true, FullOpeningEmber = true, ExpandBeforeUpgrade = 4, SaveForUpgrade = true, MinimumSkillWave = 3 },
                new Policy { Name = "Follow-up: three upgraded archers, Circe spells on summoners", Hero = HeroKind.Circe, ExpandBeforeUpgrade = 3, SaveForUpgrade = true, MinimumSkillWave = 3 },
                new Policy { Name = "Follow-up: four upgraded archers, Achilles skills on summoners", Hero = HeroKind.Achilles, ExpandBeforeUpgrade = 4, SaveForUpgrade = true, MinimumSkillWave = 3 }
            };
            foreach (Policy policy in policies)
            {
                RunReport run = Play(policy, 14257);
                report.runs.Add(run); report.victoryReached |= run.outcome == RunState.Victory.ToString();
                File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
                Debug.Log("SKELETON_V061_PLAYTHROUGH: " + run.policy + " | " + run.outcome + " | wave=" + run.wavesReached +
                    " completed=" + run.wavesCompleted + " time=" + run.elapsed + " lives=" + run.lives + " kills=" + run.kills + " skills=" + run.skillCasts[0] + "/" + run.skillCasts[1]);
                if (report.victoryReached) break;
            }
            Debug.Log("SKELETON_V061_PLAYTHROUGH_COMPLETE: victory=" + report.victoryReached + "; report=" + ReportPath);
        }

        [MenuItem("Skeleton Defender/Final summoner-hunting playthrough attempts (0.6.1)")]
        public static void RunFinalAttempts()
        {
            BalanceData.Reload();
            FullReport report = JsonUtility.FromJson<FullReport>(File.ReadAllText(ReportPath));
            report.rules += " Final variants prioritize killing living Tutankhamun summoners with legal hero positioning: stay45 pixels beside and24 path pixels behind a visible summoner, not beside its faster summoned front. One policy concentrates earned gold in front Ember level3. No direct target/HP changes. Leak counts distinguish original enemies from summons.";
            var policies = new[] {
                new Policy { Name = "Final: Circe hunts summoners, Ember0 level3 concentration", Hero = HeroKind.Circe, OpeningEmber = true, EmberToThree = true, ExpandBeforeUpgrade = 4, SaveForUpgrade = true, MinimumSkillWave = 3, HuntSummoners = true },
                new Policy { Name = "Final: Circe hunts summoners, four upgraded archers", Hero = HeroKind.Circe, ExpandBeforeUpgrade = 4, SaveForUpgrade = true, MinimumSkillWave = 3, HuntSummoners = true }
            };
            foreach (Policy policy in policies)
            {
                RunReport run = Play(policy, 14257);
                report.runs.Add(run); report.victoryReached |= run.outcome == RunState.Victory.ToString();
                File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
                Debug.Log("SKELETON_V061_PLAYTHROUGH: " + run.policy + " | " + run.outcome + " | wave=" + run.wavesReached +
                    " completed=" + run.wavesCompleted + " time=" + run.elapsed + " lives=" + run.lives + " kills=" + run.kills + " skills=" + run.skillCasts[0] + "/" + run.skillCasts[1]);
                if (report.victoryReached) break;
            }
            Debug.Log("SKELETON_V061_PLAYTHROUGH_COMPLETE: victory=" + report.victoryReached + "; report=" + ReportPath);
        }

        private static RunReport Play(Policy policy, int seed, int stopAfterCompletedWave = 0)
        {
            var profile = new PlayerProfile();
            InventoryStore.GrantStarterEquipment(profile);
            var loadout = new List<string>();
            foreach (InventoryItem item in profile.Items)
            {
                bool suitable = item.Category == ItemCategory.Armor && item.Armor == (policy.Hero == HeroKind.Circe ? ArmorKind.Silk : ArmorKind.Chainmail) ||
                    item.Category == ItemCategory.Weapon && item.Weapon == (policy.Hero == HeroKind.Circe ? WeaponKind.Staff : WeaponKind.Sword);
                if (suitable && profile.Equip(policy.Hero, (int)item.Slot, item.Id)) loadout.Add(item.Name + " | " + item.Description);
            }
            var model = new GameModel(1, policy.Hero, profile.EquippedStats(policy.Hero), seed);
            var report = new RunReport { policy = policy.Name, hero = policy.Hero.ToString(), seed = seed, loadout = loadout.ToArray(), equipment = profile.EquippedStats(policy.Hero) };
            var pilot = new Pilot();
            for (int i = 0; i < 30 && Buy(model, policy, report); i++) { }
            Vector2 opening = policy.Hero == HeroKind.Circe ? new Vector2(234, 245) : model.Position(360);
            if (model.MoveHero(opening)) Log(model, report, "Move before first wave", opening);
            while (Vector2.Distance(model.Hero.Position, model.Hero.Destination) > .1f && report.preparationSeconds < 30)
            { model.Step(GameModel.Tick); report.preparationSeconds += GameModel.Tick; }
            if (!model.StartWave()) throw new InvalidOperationException("Could not legally start the first wave.");
            Log(model, report, "Start wave 1");
            int previousWave = 0;
            var recordedCompletions = new HashSet<int>();
            bool alive = model.Hero.Alive;
            const int maximumTicks = 60 * 60 * 60;
            for (int tick = 0; tick < maximumTicks && model.State != RunState.Victory && model.State != RunState.Defeat; tick++)
            {
                if (tick % 15 == 0)
                {
                    for (int i = 0; i < 30 && Buy(model, policy, report); i++) { }
                    CommandHero(model, policy, pilot, report);
                    if (model.Wave >= policy.MinimumSkillWave) CastSkills(model, report, policy.HuntSummoners);
                    bool safeEarlyCall = policy.AggressiveCalls && (model.Remaining == 0 || model.Enemies.Count <= 5 && model.Hero.Alive && model.Hero.Hp > model.Hero.MaxHp * .7f);
                    if (safeEarlyCall && model.CanCallNextWave && model.TryCallNextWave()) Log(model, report, "Call next wave, bonus " + model.LastEarlyWaveBonus);
                }
                Enemy[] beforeTick = model.Enemies.ToArray();
                model.Step(GameModel.Tick);
                foreach (Enemy enemy in beforeTick)
                    if (enemy.Dead && enemy.Hp > 0 && enemy.Distance >= model.PathLength)
                    {
                        int[] leaks = enemy.IsSummoned ? report.leakedSummonsByKind : report.leakedOriginalsByKind;
                        leaks[(int)enemy.Skeleton]++;
                    }
                report.peakEnemies = Mathf.Max(report.peakEnemies, model.Enemies.Count);
                report.peakSummoners = Mathf.Max(report.peakSummoners, model.Enemies.FindAll(enemy => enemy.Targetable && enemy.Skeleton == SkeletonKind.Tutankhamun).Count);
                if (alive && !model.Hero.Alive) { report.heroDeaths++; Log(model, report, "Hero died; natural respawn timer"); }
                alive = model.Hero.Alive;
                if (model.Wave != previousWave) { report.waveStarts.Add(Snapshot(model, model.Wave)); previousWave = model.Wave; }
                foreach (WaveRun waveRun in model.WaveRuns)
                    if (waveRun.Completed && recordedCompletions.Add(waveRun.Number)) report.waveCompletions.Add(Snapshot(model, waveRun.Number));
                if (float.IsNaN(model.Hero.Hp) || float.IsInfinity(model.Elapsed)) throw new InvalidOperationException("Non-finite playthrough state.");
                if (stopAfterCompletedWave > 0 && recordedCompletions.Contains(stopAfterCompletedWave)) break;
            }
            report.outcome = model.State == RunState.Victory || model.State == RunState.Defeat ? model.State.ToString() :
                stopAfterCompletedWave > 0 && recordedCompletions.Contains(stopAfterCompletedWave) ? "CompletedWave" + stopAfterCompletedWave : "TimeLimit";
            report.wavesReached = model.Wave; report.wavesCompleted = model.WavesCompleted;
            report.elapsed = model.Elapsed; report.lives = model.Lives; report.kills = model.Kills; report.gold = model.Gold;
            report.finalHeroHp = model.Hero.Hp; report.earlyWaveGold = model.TotalEarlyWaveGold;
            foreach (WaveRun waveRun in model.WaveRuns) report.originalKills += waveRun.OriginalKills;
            report.summonedKills = model.Kills - report.originalKills;
            WaveEntry thirdStart = report.waveStarts.Find(wave => wave.number == 3), thirdEnd = report.waveCompletions.Find(wave => wave.number == 3);
            WaveEntry fifthEnd = report.waveCompletions.Find(wave => wave.number == 5);
            if (thirdEnd != null) { report.livesAfterWave3 = thirdEnd.lives; if (thirdStart != null) report.wave3Duration = thirdEnd.seconds - thirdStart.seconds; }
            if (fifthEnd != null) report.livesAfterWave5 = fifthEnd.lives;
            var board = new List<string>();
            foreach (Tower tower in model.Towers) board.Add("site " + tower.Site + ": " + tower.Kind + " level " + tower.Level);
            report.board = board.ToArray();
            return report;
        }

        private static bool Buy(GameModel model, Policy policy, RunReport report)
        {
            bool bossDefence = model.Wave >= 20 || model.Wave == 19 && model.WaveCleared;
            if (bossDefence)
                foreach (int site in SiteOrder)
                    if (model.At(site) != null && model.At(site).Kind != TowerKind.Archer && model.Sell(site)) { Log(model, report, "Sell magic tower " + site + " for boss defence"); return true; }
            if (!bossDefence && policy.OpeningEmber)
            {
                if (model.At(0) == null)
                {
                    if (!model.Build(0, TowerKind.Ember)) return false;
                    Log(model, report, "Build opening Ember at0"); return true;
                }
                if (policy.FullOpeningEmber && model.At(0).Level < 2)
                {
                    if (!model.Upgrade(0)) return false;
                    Log(model, report, "Upgrade opening Ember0 to2"); return true;
                }
                foreach (int openingSite in new[] { 1, 3 })
                    if (model.At(openingSite) == null)
                    {
                        if (!model.Build(openingSite, TowerKind.Archer)) return false;
                        Log(model, report, "Build opening Archer at" + openingSite); return true;
                    }
                if (model.At(0).Level < 2)
                {
                    if (!model.Upgrade(0)) return false;
                    Log(model, report, "Upgrade opening Ember0 to2"); return true;
                }
                if (policy.EmberToThree && model.At(0).Level < 3)
                {
                    if (!model.Upgrade(0)) return false;
                    Log(model, report, "Upgrade concentrated Ember0 to3"); return true;
                }
            }
            int built = model.Towers.Count;
            if (built >= policy.ExpandBeforeUpgrade)
            {
                int target = built == SiteOrder.Length ? 3 : 2;
                foreach (int site in SiteOrder)
                {
                    Tower tower = model.At(site);
                    if (tower != null && tower.Level < target)
                    {
                        if (model.Upgrade(site)) { Log(model, report, "Upgrade " + site + " to " + tower.Level); return true; }
                        if (policy.SaveForUpgrade) return false;
                    }
                }
            }
            foreach (int site in SiteOrder)
            {
                if (model.At(site) != null) continue;
                TowerKind kind = !bossDefence && policy.Control && site == 3 ? TowerKind.Frost :
                    !bossDefence && policy.Fire && site == 1 ? TowerKind.Ember : TowerKind.Archer;
                if (model.Build(site, kind)) { Log(model, report, "Build " + kind + " at " + site); return true; }
                return false;
            }
            return false;
        }

        private static void CommandHero(GameModel model, Policy policy, Pilot pilot, RunReport report)
        {
            HeroCombatState hero = model.Hero;
            if (!hero.Alive || hero.KnockdownRemaining > 0) return;
            Enemy front = null, boss = null, summoner = null;
            foreach (Enemy enemy in model.Enemies)
                if (enemy.Targetable)
                {
                    if (front == null || enemy.Distance > front.Distance) front = enemy;
                    if (enemy.IsBoss) boss = enemy;
                    if (enemy.Skeleton == SkeletonKind.Tutankhamun && (summoner == null || enemy.Distance > summoner.Distance)) summoner = enemy;
                }
            if (!pilot.Recovering && hero.Hp < hero.MaxHp * .43f)
            {
                pilot.Recovering = true; pilot.RestPoint = SafeRecoveryPoint(model);
            }
            if (pilot.Recovering && hero.Hp > hero.MaxHp * .98f) pilot.Recovering = false;
            Vector2 destination;
            if (pilot.Recovering) destination = pilot.RestPoint;
            else if (boss != null) destination = model.Position(Mathf.Max(300, boss.Distance + 10));
            else if (policy.HuntSummoners && summoner != null)
            {
                float distance = Mathf.Max(0, summoner.Distance - 24);
                Vector2 centre = model.Position(distance);
                Vector2 direction = (model.Position(distance + 2) - model.Position(Mathf.Max(0, distance - 2))).normalized;
                Vector2 normal = new Vector2(-direction.y, direction.x) * 45;
                Vector2 a = centre + normal, b = centre - normal;
                destination = Vector2.Distance(hero.Position, a) < Vector2.Distance(hero.Position, b) ? a : b;
            }
            else if (front != null)
            {
                float distance = Mathf.Min(model.PathLength - 20, front.Distance + (hero.Kind == HeroKind.Circe ? 50 : 14));
                destination = model.Position(distance);
                if (hero.Kind == HeroKind.Circe)
                {
                    Vector2 direction = (model.Position(distance + 2) - model.Position(distance - 2)).normalized;
                    Vector2 normal = new Vector2(-direction.y, direction.x) * 80;
                    Vector2 a = destination + normal, b = destination - normal;
                    destination = Vector2.Distance(hero.Position, a) < Vector2.Distance(hero.Position, b) ? a : b;
                }
            }
            else destination = hero.Kind == HeroKind.Circe ? new Vector2(234, 245) : model.Position(360);
            if (Vector2.Distance(hero.Destination, destination) > 10 && model.MoveHero(destination) && model.Elapsed - pilot.LastMoveLogged >= 2)
            { Log(model, report, pilot.Recovering ? "Retreat to rest" : policy.HuntSummoners && summoner != null ? "Move beside visible summoner" : "Move to cover visible front", destination); pilot.LastMoveLogged = model.Elapsed; }
        }

        private static Vector2 SafeRecoveryPoint(GameModel model)
        {
            Vector2[] candidates = { new Vector2(38, 36), new Vector2(40, 590), new Vector2(330, 595), new Vector2(680, 38), new Vector2(1000, 590), new Vector2(1010, 40) };
            Vector2 best = candidates[0]; float bestScore = float.NegativeInfinity;
            foreach (Vector2 candidate in candidates)
            {
                float danger = 0;
                foreach (Enemy enemy in model.Enemies)
                    if (enemy.Targetable)
                    {
                        float distance = Vector2.Distance(candidate, model.Position(enemy.Distance));
                        danger += Mathf.Max(0, 210 - distance);
                    }
                float score = -Vector2.Distance(model.Hero.Position, candidate) - danger * 3;
                if (score > bestScore) { bestScore = score; best = candidate; }
            }
            return best;
        }

        private static void CastSkills(GameModel model, RunReport report, bool huntSummoners = false)
        {
            int alive = 0, summoners = 0; Enemy front = null, lastSummoner = null;
            foreach (Enemy enemy in model.Enemies)
                if (enemy.Targetable)
                {
                    alive++; if (front == null || enemy.Distance > front.Distance) front = enemy;
                    if (enemy.Skeleton == SkeletonKind.Tutankhamun) { summoners++; if (lastSummoner == null || enemy.Distance > lastSummoner.Distance) lastSummoner = enemy; }
                }
            if (front == null) return;
            bool emergency = front.Distance > model.PathLength - 380;
            if (model.Hero.Kind == HeroKind.Circe)
            {
                float spellFront = huntSummoners && lastSummoner != null ? lastSummoner.Distance : front.Distance;
                bool deerWorthwhile = huntSummoners && summoners >= 3 || alive >= 14 || emergency && alive >= 4;
                if (model.CanCastSkill(0) && deerWorthwhile && NearestPathDistance(model, model.Hero.Position) >= spellFront - 60)
                    Cast(model, report, 0, null);
                if (model.CanCastSkill(1) && (alive >= 16 || emergency && alive >= 3)) Cast(model, report, 1, null);
            }
            else
            {
                if (model.CanCastSkill(1) && (alive >= 18 || emergency && alive >= 4 || front.IsBoss)) Cast(model, report, 1, null);
                if (model.CanCastSkill(0))
                {
                    Enemy best = null; float bestScore = 0;
                    foreach (Enemy centre in model.Enemies)
                    {
                        if (!centre.Targetable) continue;
                        float score = 0;
                        foreach (Enemy enemy in model.Enemies)
                            if (enemy.Targetable && Vector2.Distance(model.Position(centre.Distance), model.Position(enemy.Distance)) <= 80)
                                score += 1 + enemy.Distance / model.PathLength;
                        if (score > bestScore) { bestScore = score; best = centre; }
                    }
                    if (best != null && (bestScore >= 6 || emergency || best.IsBoss)) Cast(model, report, 0, model.Position(best.Distance));
                }
            }
        }
        private static float NearestPathDistance(GameModel model, Vector2 point)
        {
            float result = 0, closest = float.PositiveInfinity;
            for (float distance = 0; distance <= model.PathLength; distance += 10)
            {
                float gap = (model.Position(distance) - point).sqrMagnitude;
                if (gap < closest) { closest = gap; result = distance; }
            }
            return result;
        }
        private static void Cast(GameModel model, RunReport report, int index, Vector2? target)
        {
            if (!model.TryCastSkill(index, target)) return;
            report.skillCasts[index]++; Log(model, report, "Cast " + (index == 0 ? "Q" : "E"), target ?? model.Hero.Position);
        }
        private static WaveEntry Snapshot(GameModel model, int wave) => new WaveEntry {
            number = wave, seconds = model.Elapsed, lives = model.Lives, gold = model.Gold, kills = model.Kills, heroHp = model.Hero.Hp
        };
        private static void Log(GameModel model, RunReport report, string command, Vector2 point = default(Vector2))
        { report.actions.Add(new ActionEntry { seconds = model.Elapsed, wave = model.Wave, gold = model.Gold, command = command, point = point }); }
    }
}
