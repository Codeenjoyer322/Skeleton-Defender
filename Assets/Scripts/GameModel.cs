using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkeletonDefender
{
    public enum TowerKind { Archer, Ember, Frost }
    public enum EnemyKind { Skeleton, Orc, Chaos }
    public enum RunState { Preparing, Wave, Victory, Defeat }
    public enum ProjectileKind { Arcane, Fireball, Knife }
    public enum SkeletonKind { Normal, Ninja, Tutankhamun, Giant, Crawler, Pirate, Samurai, Sarcophagus, TRex, Knight, Warlock, Mad, Boxer, Boss }

    [Serializable]
    public sealed class Tower
    {
        public int Site;
        public TowerKind Kind;
        public int Level = 1;
        public int Invested;
        public float Cooldown;
        public float Flash;
        public TowerDefinition Definition => BalanceData.Current.Tower((int)Kind);
        public float Range => Definition.rangeBase + Level * Definition.rangePerLevel;
        public float Damage => Definition.baseDamage * (1 + (Level - 1) * Definition.damagePerAdditionalLevel);
        public float Interval => Definition.baseInterval / (1 + (Level - 1) * Definition.attackRatePerAdditionalLevel);
        public int UpgradeCost => Level >= Definition.maxLevel ? 0 : GameModel.Cost(Kind) + Level * Definition.upgradeCostPerLevel;
        public int SellValue => Mathf.FloorToInt(Invested * BalanceData.Current.economy.towerSellFraction);
    }

    public sealed class Enemy
    {
        public int Id;
        public EnemyKind Kind;
        public SkeletonKind Skeleton;
        public int Rank = 1;
        public bool IsBoss;
        public bool IsSummoned;
        public int SummonerId = -1;
        public int SummonsCreated;
        public int WaveNumber;
        public int SpawnOrdinal;
        public bool HasSarcophagus;
        public float Distance;
        public float Hp;
        public float MaxHp;
        public float Slow;
        public float SkillSlowRemaining;
        public float SkillSlowMultiplier = 1;
        internal readonly List<ForwardMovement> MovementHistory = new List<ForwardMovement>();
        public float HitFlash;
        public float AttackCooldown;
        public float FrogRemaining;
        public float SpecialCooldown;
        public float DodgeCooldown;
        public bool DodgeReady;
        public bool Dead;
        public bool IsFrog => FrogRemaining > 0;
        public bool Targetable => !Dead && (!IsFrog || IsBoss);
        public EnemyRankDefinition Definition => IsBoss ? BalanceData.Current.boss : BalanceData.Current.Rank(Rank);
        public EnemyFamilyDefinition Family => BalanceData.Current.Family((int)Kind);
        public SkeletonDefinition Variant => BalanceData.Current.Skeleton(IsBoss ? (int)SkeletonKind.Boss : (int)Skeleton);
        public int Reward => IsSummoned ? Variant.summonedReward : Variant.reward;
        public int LeakDamage => IsBoss ? Family.bossLeakDamage : Family.leakDamage;
        public float Speed => Variant.walkSpeed;
        public float AttackDamage => Variant.damage;
        public float AttackInterval => Variant.attackInterval;
        public float PhysicalArmor => Mathf.Clamp01(Variant.physicalArmor);
        public bool MagicalImmune => Variant.magicalImmune;
    }

    public sealed class HeroCombatState
    {
        public HeroKind Kind;
        public float Hp;
        public float MaxHp;
        public float BaseDamage;
        public float Damage { get => BaseDamage * DamageMultiplier * (Kind == HeroKind.Circe ? Equipment.MagicDamageMultiplier : 1); set => BaseDamage = value; }
        public EquipmentStats Equipment = new EquipmentStats();
        public bool IsClone;
        public float Mana;
        public readonly float[] ManualCooldowns = new float[2];
        internal readonly double[] PreciseManualCooldowns = new double[2];
        public float RageRemaining;
        internal double PreciseRageRemaining;
        public string LastCastName;
        public float CastPoseRemaining;
        internal bool ActivityThisTick;
        internal bool ActivityPending;
        public Vector2 Position;
        public Vector2 Destination;
        public float RespawnRemaining;
        public float AttackCooldown;
        public float FrogCooldown;
        public float SunCooldown;
        public float DecapitateCooldown;
        public float Flash;
        public float KnockdownRemaining;
        public int LifeSerial;
        public readonly IdleRegeneration Regeneration = new IdleRegeneration();
        public float IdleSeconds => Regeneration.IdleSeconds;
        public bool IsRegenerating => Regeneration.IsRegenerating;
        public bool Alive => Hp > 0;
        public HeroDefinition Definition => BalanceData.Current.Hero((int)Kind);
        public float Range => Kind == HeroKind.Circe ? BalanceData.Current.heroRules.circeRange : BalanceData.Current.heroRules.achillesRange;
        public float DamageMultiplier => RageRemaining > 0 ? BalanceData.Current.heroSystems.rageDamageMultiplier : 1;
        public float MagicDamageMultiplier => DamageMultiplier * Equipment.MagicDamageMultiplier;
        public float AttackInterval => Definition.attackInterval / Mathf.Max(.01f, 1 + Equipment.AttackSpeedBonus) /
            (RageRemaining > 0 ? BalanceData.Current.heroSystems.rageAttackRateMultiplier : 1);
        public float WalkSpeed => (Kind == HeroKind.Circe ? BalanceData.Current.heroRules.circeWalkSpeed : BalanceData.Current.heroRules.achillesWalkSpeed) * Equipment.MoveSpeedMultiplier;
        public float PhysicalArmor => Mathf.Clamp(Equipment.PhysicalArmor, 0, BalanceData.Current.equipment.physicalArmorCap);
        public float DodgeChance => Mathf.Clamp((Kind == HeroKind.Achilles ? Definition.skills[1].chance : 0) + Equipment.DodgeChance, 0, BalanceData.Current.equipment.dodgeCap);
        public float MaxMana => Mathf.Max(0, BalanceData.Current.heroSystems.baseMana + Equipment.MaxManaBonus);
        public float ManaRegen => Mathf.Max(0, BalanceData.Current.heroSystems.manaRegenerationPerSecond * Equipment.ManaRegenMultiplier + Equipment.ManaRegenBonus);
    }

    // Independent of heroes, targeting and rendering, so future allied units can reuse it.
    public sealed class IdleRegeneration
    {
        public float IdleSeconds { get; private set; }
        public bool IsRegenerating { get; private set; }
        private double preciseIdle;
        public void Reset() { preciseIdle = 0; IdleSeconds = 0; IsRegenerating = false; }
        public float Advance(float dt, bool resting, float hp, float maxHp, float delay, float fractionPerSecond)
        {
            if (float.IsNaN(dt) || float.IsInfinity(dt) || dt <= 0) return hp;
            if (!resting || hp <= 0 || maxHp <= 0) { Reset(); return hp; }
            double previous = preciseIdle; preciseIdle += dt; IdleSeconds = (float)preciseIdle;
            float activeSeconds = (float)(Math.Max(0, preciseIdle - delay) - Math.Max(0, previous - delay));
            IsRegenerating = activeSeconds > 0 && fractionPerSecond > 0 && hp < maxHp;
            return IsRegenerating ? Mathf.Min(maxHp, hp + maxHp * fractionPerSecond * activeSeconds) : hp;
        }
    }

    public sealed class WaveRun
    {
        public int Number;
        public float Elapsed;
        public int Spawned;
        public int OriginalKills;
        public int OriginalRemoved => RemovedOrdinals.Count;
        public float EligibleAtWaveElapsed = -1;
        public bool Completed;
        public bool EarlyCallUsed;
        public readonly List<SpawnEntry> Plan = new List<SpawnEntry>();
        internal readonly HashSet<int> KilledOrdinals = new HashSet<int>();
        internal readonly HashSet<int> RemovedOrdinals = new HashSet<int>();
        internal double StartedAt;
    }

    public sealed class HeroProjectile
    {
        public Vector2 Start;
        public Vector2 Position;
        public int TargetId;
        public ProjectileKind Kind;
        public float Damage;
        public bool InstantKill;
        public float Delay;
        public HeroCombatState Source;
        public float SplashRadius;
        public float SplashFraction;
    }

    [Serializable]
    public sealed class SpawnEntry
    {
        public int Ordinal;
        public int Batch;
        public float Time;
        public EnemyKind Kind;
        public SkeletonKind Skeleton;
        public int Rank;
        public bool IsBoss;
    }

    public sealed class EnemyProjectile
    {
        public Vector2 Position;
        public Vector2 Start;
        public float Damage;
        public float Speed;
        public int SourceId;
        public int TargetLifeSerial;
        public HeroCombatState Target;
    }

    public sealed class EnemyEffect
    {
        public Vector2 Position;
        public Vector2 Target;
        public string Kind;
        public float Life = .6f;
    }

    public sealed class Shot
    {
        public Vector2 Start;
        public Vector2 End;
        public TowerKind Kind;
        public string Skill;
        public float Life = .23f;
    }

    public sealed class Popup
    {
        public Vector2 Position;
        public string Text;
        public float Life = 1.2f;
        public bool Damage;
    }

    // All game actions and random rolls happen in this fixed-tick simulation.
    public sealed partial class GameModel
    {
        public static int TotalWaves => BalanceData.Current.wavesPerMap;
        public const float Tick = 1f / 60f;
        public static readonly Vector2[] Path = {
            new Vector2(-32, 154), new Vector2(150, 154), new Vector2(150, 330),
            new Vector2(370, 330), new Vector2(370, 130), new Vector2(585, 130),
            new Vector2(585, 450), new Vector2(800, 450), new Vector2(800, 280), new Vector2(991, 280)
        };
        public static readonly Vector2[] Sites = {
            new Vector2(74, 244), new Vector2(252, 219), new Vector2(264, 415),
            new Vector2(471, 245), new Vector2(475, 54), new Vector2(690, 217),
            new Vector2(499, 535), new Vector2(710, 535), new Vector2(899, 372), new Vector2(881, 181)
        };
        public static int[] WaveCounts => Array.ConvertAll(BalanceData.Current.waves, wave => wave.count);
        public readonly List<Enemy> Enemies = new List<Enemy>();
        public readonly List<Tower> Towers = new List<Tower>();
        public readonly List<Shot> Shots = new List<Shot>();
        public readonly List<HeroProjectile> Projectiles = new List<HeroProjectile>();
        public readonly List<EnemyProjectile> EnemyProjectiles = new List<EnemyProjectile>();
        public readonly List<EnemyEffect> EnemyEffects = new List<EnemyEffect>();
        public readonly List<Popup> Popups = new List<Popup>();
        public readonly List<SpawnEntry> SpawnPlan = new List<SpawnEntry>();
        public readonly List<WaveRun> WaveRuns = new List<WaveRun>();
        public HeroCombatState Hero { get; }
        public int Map { get; }
        public int Gold { get; private set; }
        public int Lives { get; private set; }
        public int Wave { get; private set; }
        public int Kills { get; private set; }
        public int Spawned => CurrentWaveRun?.Spawned ?? 0;
        public int WaveBonus { get; private set; }
        public int BossSpawnSerial { get; private set; }
        public int WavesCompleted { get; private set; }
        public int WaveCompletionSerial { get; private set; }
        public int LastCompletedWave { get; private set; }
        public int LastCompletedWaveBonus => WaveBonus;
        public float Elapsed { get; private set; }
        public float WaveElapsed { get; private set; }
        public bool HasStarted => Wave > 0;
        public bool WaveCleared => CurrentWaveRun != null && CurrentWaveRun.Completed;
        public bool IsPaused { get; private set; }
        public int EarlyWaveKills => CurrentWaveRun?.OriginalKills ?? 0;
        public int EarlyWaveSpawned => Spawned;
        public int EarlyWaveRequiredKills => HasStarted ? EarlyThreshold(CurrentWaveRun) : 0;
        public int EarlyWaveRequiredSpawned => EarlyWaveRequiredKills;
        public int LastEarlyWaveBonus { get; private set; }
        public int TotalEarlyWaveGold { get; private set; }
        public int EarlyWaveBonus
        {
            get
            {
                if (!CanCallNextWave) return 0;
                float eligibleAt = EligibilityTime(CurrentWaveRun);
                float slot = BalanceData.Current.spawnSchedule.waveSlotSeconds;
                if (eligibleAt < 0 || eligibleAt >= slot || WaveElapsed >= slot) return 0;
                int cap = Mathf.Max(0, BalanceData.Current.spawnSchedule.earlyWaveBonus);
                float ratio = Mathf.Clamp01((slot - WaveElapsed) / (slot - eligibleAt));
                return Mathf.Clamp(Mathf.RoundToInt(cap * ratio), 0, cap);
            }
        }
        public float EarlyWaveProgress
        {
            get
            {
                if (!HasStarted || ActiveWave.count <= 0) return 0;
                string trigger = BalanceData.Current.spawnSchedule.earlyWaveTrigger;
                if (trigger == "elapsed") return Mathf.Clamp01(WaveElapsed / BalanceData.Current.spawnSchedule.waveSlotSeconds);
                return Mathf.Clamp01((trigger == "spawned" ? EarlyWaveSpawned : EarlyWaveKills) / (float)ActiveWave.count);
            }
        }
        public bool CanCallNextWave => !Finished && !IsPaused && HasStarted && Wave < TotalWaves &&
            !CurrentWaveRun.EarlyCallUsed && ActiveWave.count > 0 && EligibilityTime(CurrentWaveRun) >= 0;
        public float WaveSlotRemaining => HasStarted ? Mathf.Max(0, BalanceData.Current.spawnSchedule.waveSlotSeconds - WaveElapsed) : BalanceData.Current.spawnSchedule.waveSlotSeconds;
        public float NextWaveIn => HasStarted && Wave < TotalWaves ? WaveSlotRemaining : -1;
        public float MapTimeRemaining => Mathf.Max(0, BalanceData.Current.spawnSchedule.targetMapSeconds - Elapsed);
        public RunState State { get; private set; } = RunState.Preparing;
        public WaveDefinition ActiveWave => Wave > 0 ? BalanceData.Current.Wave(Wave) : null;
        public int Remaining
        {
            get { int remaining = Enemies.Count; foreach (WaveRun run in WaveRuns) remaining += run.Plan.Count - run.Spawned; return remaining; }
        }
        public int PlannedRemaining
        {
            get
            {
                int remaining = 0;
                foreach (WaveRun run in WaveRuns) if (!run.Completed) remaining += Mathf.Max(0, run.Plan.Count - run.OriginalRemoved);
                return remaining;
            }
        }
        public int PlannedEnemiesRemaining => PlannedRemaining;
        public int SummonedRemaining
        {
            get
            {
                int remaining = 0;
                foreach (Enemy enemy in Enemies) if (enemy.IsSummoned && !enemy.Dead) remaining++;
                foreach (Enemy enemy in pendingSpawns) if (enemy.IsSummoned && !enemy.Dead) remaining++;
                return remaining;
            }
        }
        public float NextSpawnIn
        {
            get
            {
                float next = float.PositiveInfinity;
                foreach (WaveRun run in WaveRuns)
                    if (run.Spawned < run.Plan.Count) next = Mathf.Min(next, Mathf.Max(0, run.Plan[run.Spawned].Time - run.Elapsed));
                return float.IsPositiveInfinity(next) ? -1 : next;
            }
        }
        public float PathLength { get; }
        private int nextId;
        private readonly float[] lengths;
        private readonly System.Random random;
        private readonly Vector2 heroSpawn;
        private readonly List<Enemy> pendingSpawns = new List<Enemy>();
        private double preciseElapsed;
        private WaveRun CurrentWaveRun => WaveRuns.Count == 0 ? null : WaveRuns[WaveRuns.Count - 1];
        private bool Finished => State == RunState.Victory || State == RunState.Defeat;

        public GameModel() : this(1, HeroKind.Circe, 0, 1) { }

        public GameModel(int map, HeroKind hero, float weaponBonus, int seed)
            : this(map, hero, WeaponStats(weaponBonus), seed) { }

        private static EquipmentStats WeaponStats(float weaponBonus)
        {
            if (!Finite(weaponBonus) || weaponBonus < 0) throw new ArgumentOutOfRangeException(nameof(weaponBonus));
            return new EquipmentStats { DamageBonus = weaponBonus };
        }

        public GameModel(int map, HeroKind hero, EquipmentStats equipment, int seed)
        {
            BalanceData data = BalanceData.Current;
            if (map < 1 || map > data.mapCount) throw new ArgumentOutOfRangeException(nameof(map));
            if (hero != HeroKind.Circe && hero != HeroKind.Achilles) throw new ArgumentOutOfRangeException(nameof(hero));
            if (equipment == null) throw new ArgumentNullException(nameof(equipment));
            if (!equipment.HasValidValues) throw new ArgumentException("Invalid equipment statistics.", nameof(equipment));
            Map = map; random = new System.Random(seed);
            Gold = data.economy.startingGold; Lives = data.economy.startingLives;
            lengths = new float[Path.Length - 1];
            for (int i = 0; i < lengths.Length; i++) { lengths[i] = Vector2.Distance(Path[i], Path[i + 1]); PathLength += lengths[i]; }
            heroSpawn = Position(PathLength - data.heroRules.spawnDistanceFromExit);
            Hero = CreateActor(hero, equipment, false, heroSpawn);
        }

        public static int Cost(TowerKind kind) => BalanceData.Current.Tower((int)kind).cost;
        public static string TowerName(TowerKind kind) => BalanceData.Current.Tower((int)kind).name.ToUpperInvariant();
        public static EnemyKind KindFor(int wave, int index) => EnemyKind.Skeleton;
        public static EnemyKind MapFaction(int map) => (EnemyKind)(map - 1);

        // Batch and member times are absolute. Killing an enemy cannot advance this schedule.
        public static List<SpawnEntry> BuildSpawnPlan(int map, int wave)
        {
            BalanceData data = BalanceData.Current;
            if (map < 1 || map > data.mapCount || wave < 1 || wave > data.wavesPerMap)
                throw new ArgumentOutOfRangeException();
            WaveDefinition definition = data.Wave(wave);
            SpawnScheduleDefinition timing = data.spawnSchedule;
            if (definition.skeletonCounts == null || definition.skeletonCounts.Length != data.skeletons.Length)
                throw new InvalidOperationException("Wave skeleton composition is missing: " + wave);
            int count = 0;
            foreach (int value in definition.skeletonCounts)
            {
                if (value < 0) throw new InvalidOperationException("Negative skeleton count.");
                count += value;
            }
            if (count != definition.count || count < 1) throw new InvalidOperationException("Skeleton counts do not match total: " + wave);
            if (timing.fractionPerBatch <= 0 || timing.fractionPerBatch > 1 || timing.spawnWindowSeconds <= timing.initialDelay)
                throw new InvalidOperationException("Invalid spawn timing.");
            var plan = new List<SpawnEntry>(count);
            int maximumBatchSize = Mathf.Max(1, Mathf.FloorToInt(count * timing.fractionPerBatch));
            int batches = Mathf.CeilToInt(count / (float)maximumBatchSize);
            float period = (timing.spawnWindowSeconds - timing.initialDelay) / batches;
            int[] assigned = new int[data.skeletons.Length];
            int ordinal = 0;
            for (int batch = 0; batch < batches; batch++)
            {
                int end = (int)((long)count * (batch + 1) / batches);
                int batchSize = end - ordinal;
                if (count > 1 && batchSize / (float)count < timing.minimumFractionPerBatch - .00001f)
                    throw new InvalidOperationException("Wave cannot satisfy batch percentage limits: " + wave);
                for (int member = 0; ordinal < end; member++)
                {
                    ordinal++;
                    int chosen = -1;
                    float largestDeficit = float.NegativeInfinity;
                    for (int k = 0; k < assigned.Length; k++)
                    {
                        if (assigned[k] >= definition.skeletonCounts[k]) continue;
                        float deficit = ordinal * definition.skeletonCounts[k] / (float)count - assigned[k];
                        if (deficit > largestDeficit) { largestDeficit = deficit; chosen = k; }
                    }
                    assigned[chosen]++;
                    bool boss = chosen == (int)SkeletonKind.Boss;
                    float memberOffset = batchSize > 1 ? member * period * timing.batchActiveFraction / (batchSize - 1) : 0;
                    plan.Add(new SpawnEntry {
                        Ordinal = ordinal, Batch = batch + 1,
                        Time = timing.initialDelay + batch * period + memberOffset,
                        Kind = EnemyKind.Skeleton, Skeleton = (SkeletonKind)chosen, Rank = boss ? 0 : 1, IsBoss = boss
                    });
                }
            }
            return plan;
        }

        public Vector2 Position(float distance)
        {
            for (int i = 0; i < lengths.Length; i++)
            {
                if (distance <= lengths[i]) return Vector2.Lerp(Path[i], Path[i + 1], distance / lengths[i]);
                distance -= lengths[i];
            }
            return Path[Path.Length - 1];
        }

        public bool MoveHero(Vector2 destination, bool clone = false)
        {
            HeroCombatState actor = GetControlledHero(clone);
            if (Finished || IsPaused || actor == null || !actor.Alive || actor.KnockdownRemaining > 0 || !Finite(destination.x) || !Finite(destination.y)) return false;
            actor.Destination = new Vector2(Mathf.Clamp(destination.x, 12, 1044), Mathf.Clamp(destination.y, 12, 628));
            return true;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public Tower At(int site) => Towers.Find(tower => tower.Site == site);

        public bool Build(int site, TowerKind kind)
        {
            if (Finished || site < 0 || site >= Sites.Length || (int)kind < 0 || (int)kind >= 3 || At(site) != null || Gold < Cost(kind)) return false;
            Gold -= Cost(kind);
            Towers.Add(new Tower { Site = site, Kind = kind, Invested = Cost(kind) });
            return true;
        }
        public bool Upgrade(int site)
        {
            Tower tower = At(site);
            if (Finished || tower == null || tower.Level >= tower.Definition.maxLevel || Gold < tower.UpgradeCost) return false;
            int cost = tower.UpgradeCost; Gold -= cost; tower.Invested += cost; tower.Level++;
            return true;
        }
        public bool Sell(int site)
        {
            Tower tower = At(site);
            if (Finished || tower == null) return false;
            Gold += tower.SellValue; Towers.Remove(tower); return true;
        }
        public void SetPaused(bool paused) { IsPaused = paused; }

        public bool StartWave()
        {
            if (State != RunState.Preparing || HasStarted || IsPaused) return false;
            BeginWave(); return true;
        }

        public bool TryCallNextWave()
        {
            if (!CanCallNextWave) return false;
            RecordEligibility(CurrentWaveRun);
            int awarded = EarlyWaveBonus;
            CurrentWaveRun.EarlyCallUsed = true;
            LastEarlyWaveBonus = awarded; TotalEarlyWaveGold += awarded; Gold += awarded;
            BeginWave();
            return true;
        }

        private static int EarlyThreshold(WaveRun run)
        {
            float fraction = BalanceData.Current.spawnSchedule.earlyWaveFraction;
            if (run == null || run.Plan.Count == 0 || !Finite(fraction) || fraction <= 0 || fraction > 1) return 0;
            return (int)Math.Ceiling(run.Plan.Count * (decimal)fraction);
        }

        private static float EligibilityTime(WaveRun run)
        {
            if (run == null) return -1;
            if (run.EligibleAtWaveElapsed >= 0) return run.EligibleAtWaveElapsed;
            int threshold = EarlyThreshold(run);
            if (threshold <= 0) return -1;
            SpawnScheduleDefinition timing = BalanceData.Current.spawnSchedule;
            if (timing.earlyWaveTrigger == "spawned" && run.Spawned >= threshold)
                return run.Plan[threshold - 1].Time;
            if (timing.earlyWaveTrigger == "kills" && run.OriginalKills >= threshold) return run.Elapsed;
            if (timing.earlyWaveTrigger == "elapsed" && run.Elapsed >= timing.waveSlotSeconds * timing.earlyWaveFraction)
                return timing.waveSlotSeconds * timing.earlyWaveFraction;
            return -1;
        }

        private static void RecordEligibility(WaveRun run)
        {
            if (run.EligibleAtWaveElapsed < 0) run.EligibleAtWaveElapsed = EligibilityTime(run);
        }

        private void BeginWave()
        {
            Wave++; WaveElapsed = 0;
            var run = new WaveRun { Number = Wave, StartedAt = preciseElapsed };
            run.Plan.AddRange(BuildSpawnPlan(Map, Wave)); WaveRuns.Add(run);
            SpawnPlan.Clear(); SpawnPlan.AddRange(run.Plan); State = RunState.Wave;
        }

        public Enemy CreateEnemy(SkeletonKind kind, float distance = 0, bool summoned = false, bool hasSarcophagus = true)
        {
            if ((int)kind < 0 || (int)kind >= BalanceData.Current.skeletons.Length || !Finite(distance))
                throw new ArgumentOutOfRangeException(nameof(kind));
            SkeletonDefinition variant = BalanceData.Current.Skeleton((int)kind);
            return new Enemy {
                Id = nextId++, Kind = EnemyKind.Skeleton, Skeleton = kind, Rank = kind == SkeletonKind.Boss ? 0 : 1,
                IsBoss = kind == SkeletonKind.Boss, IsSummoned = summoned, WaveNumber = Wave,
                HasSarcophagus = variant.rebirth && hasSarcophagus,
                Distance = Mathf.Clamp(distance, 0, PathLength), Hp = variant.hp, MaxHp = variant.hp,
                DodgeCooldown = variant.evadeInterval,
                SpecialCooldown = variant.summonInterval > 0 ? variant.summonInterval :
                    variant.projectileInterval > 0 ? variant.projectileInterval : variant.lightningInterval
            };
        }

        private void Spawn(WaveRun run, SpawnEntry entry)
        {
            Enemy enemy = CreateEnemy(entry.Skeleton);
            enemy.WaveNumber = run.Number; enemy.SpawnOrdinal = entry.Ordinal;
            Enemies.Add(enemy); run.Spawned++;
            RecordEligibility(run);
            if (entry.IsBoss) BossSpawnSerial++;
        }

        public void Step(float dt)
        {
            if (Finished || IsPaused || !Finite(dt) || dt <= 0) return;
            BeginActorTick(Hero); BeginActorTick(Clone);
            for (int i = Shots.Count - 1; i >= 0; i--) { Shots[i].Life -= dt; if (Shots[i].Life <= 0) Shots.RemoveAt(i); }
            for (int i = Popups.Count - 1; i >= 0; i--) { Popups[i].Life -= dt; if (Popups[i].Life <= 0) Popups.RemoveAt(i); }
            for (int i = EnemyEffects.Count - 1; i >= 0; i--) { EnemyEffects[i].Life -= dt; if (EnemyEffects[i].Life <= 0) EnemyEffects.RemoveAt(i); }
            foreach (Tower tower in Towers) tower.Flash = Mathf.Max(0, tower.Flash - dt);
            UpdateHeroMovementAndRespawn(dt);
            if (!HasStarted) { UpdateHeroRegeneration(dt); return; }
            preciseElapsed += dt; Elapsed = (float)preciseElapsed;
            foreach (WaveRun run in WaveRuns) { run.Elapsed = (float)(preciseElapsed - run.StartedAt); RecordEligibility(run); }
            WaveElapsed = CurrentWaveRun.Elapsed;
            foreach (WaveRun run in WaveRuns)
                while (!run.Completed && run.Spawned < run.Plan.Count && run.Plan[run.Spawned].Time <= run.Elapsed + .00001f)
                    Spawn(run, run.Plan[run.Spawned]);
            UpdateEnemies(dt);
            FlushSpawns();
            Enemies.RemoveAll(enemy => enemy.Dead);
            if (Lives <= 0) { State = RunState.Defeat; FinishCombat(); return; }
            UpdateEnemyProjectiles(dt);
            UpdateAbilityEffects(dt);
            if (State == RunState.Wave) { UpdateHeroCombat(dt); if (Clone != null) UpdateActorCombat(Clone, dt); }
            UpdateProjectiles(dt);
            UpdateTowerProjectiles(dt);
            UpdateTowers(dt);
            FlushSpawns();
            Enemies.RemoveAll(enemy => enemy.Dead);
            CompleteWaves();
            UpdateHeroRegeneration(dt);
        }

        private void CompleteWaves()
        {
            foreach (WaveRun run in WaveRuns)
            {
                if (run.Completed || run.Spawned != run.Plan.Count || Enemies.Exists(enemy => enemy.WaveNumber == run.Number && !enemy.Dead)) continue;
                run.Completed = true; WavesCompleted++; WaveCompletionSerial++; LastCompletedWave = run.Number;
                EconomyDefinition economy = BalanceData.Current.economy;
                WaveBonus = economy.waveBonusBase + run.Number * economy.waveBonusPerWave;
                Gold += WaveBonus;
            }
            if (Wave == TotalWaves && WavesCompleted == TotalWaves && Remaining == 0 && pendingSpawns.Count == 0)
            {
                State = RunState.Victory; FinishCombat(); return;
            }
            if (CurrentWaveRun.Completed && Wave < TotalWaves && WaveSlotRemaining <= 0) BeginWave();
            else State = WavesCompleted == Wave ? RunState.Preparing : RunState.Wave;
        }

        private void HeroActivity()
        {
            ActorActivity(Hero);
        }

        private void UpdateHeroRegeneration(float dt)
        {
            UpdateActorRegeneration(Hero, dt);
            if (Clone != null) UpdateActorRegeneration(Clone, dt);
        }

        private void UpdateActorRegeneration(HeroCombatState actor, float dt)
        {
            if (Finished || !actor.Alive) { actor.Regeneration.Reset(); return; }
            HeroRulesDefinition rules = BalanceData.Current.heroRules;
            bool resting = !actor.ActivityThisTick && actor.KnockdownRemaining <= 0 && actor.CastPoseRemaining <= 0 &&
                (actor.Position - actor.Destination).sqrMagnitude <= .0001f;
            actor.Hp = actor.Regeneration.Advance(dt, resting, actor.Hp, actor.MaxHp,
                rules.regenerationIdleSeconds, rules.regenerationMaxHpFractionPerSecond);
        }

        private void FlushSpawns()
        {
            if (pendingSpawns.Count == 0) return;
            Enemies.AddRange(pendingSpawns); pendingSpawns.Clear();
        }

        private void UpdateHeroMovementAndRespawn(float dt)
        {
            UpdateActorSystems(Hero, dt);
            UpdateActorMovement(Hero, dt);
            if (Clone != null)
            {
                preciseCloneRemaining = Math.Max(0, preciseCloneRemaining - dt);
                CloneRemaining = (float)preciseCloneRemaining;
                if (!Clone.Alive || CloneRemaining <= 0) RemoveClone();
                else { UpdateActorSystems(Clone, dt); UpdateActorMovement(Clone, dt); }
            }
        }

        private void UpdateActorMovement(HeroCombatState actor, float dt)
        {
            actor.Flash = Mathf.Max(0, actor.Flash - dt);
            if (!actor.Alive || actor.KnockdownRemaining > 0) ActorActivity(actor);
            actor.KnockdownRemaining = Mathf.Max(0, actor.KnockdownRemaining - dt);
            if (!actor.Alive)
            {
                if (actor.IsClone) return;
                actor.RespawnRemaining = Mathf.Max(0, actor.RespawnRemaining - dt);
                if (actor.RespawnRemaining <= 0)
                {
                    actor.Hp = actor.MaxHp; actor.Position = heroSpawn; actor.Destination = heroSpawn;
                    actor.KnockdownRemaining = 0; actor.LifeSerial++;
                    Popups.Add(new Popup { Position = actor.Position, Text = "Герой вернулся" });
                }
                return;
            }
            if (actor.KnockdownRemaining <= 0)
            {
                Vector2 before = actor.Position;
                actor.Position = Vector2.MoveTowards(actor.Position, actor.Destination, actor.WalkSpeed * dt);
                if ((before - actor.Position).sqrMagnitude > .000001f) ActorActivity(actor);
            }
        }

        private void UpdateEnemies(float dt)
        {
            foreach (Enemy enemy in Enemies)
            {
                if (enemy.Dead) continue;
                enemy.HitFlash = Mathf.Max(0, enemy.HitFlash - dt);
                enemy.Slow = Mathf.Max(0, enemy.Slow - dt);
                enemy.SkillSlowRemaining = Mathf.Max(0, enemy.SkillSlowRemaining - dt);
                enemy.MovementHistory.RemoveAll(movement => movement.EndTime <= Elapsed - Systems.spearHistorySeconds);
                enemy.AttackCooldown = Mathf.Max(0, enemy.AttackCooldown - dt);
                if (enemy.IsFrog)
                {
                    enemy.FrogRemaining = Mathf.Max(0, enemy.FrogRemaining - dt);
                    if (!enemy.IsFrog && !enemy.IsBoss) Kill(enemy);
                    continue;
                }
                if (enemy.Variant.evadeInterval > 0 && !enemy.DodgeReady)
                {
                    enemy.DodgeCooldown = Mathf.Max(0, enemy.DodgeCooldown - dt);
                    if (enemy.DodgeCooldown <= .00001f) enemy.DodgeReady = true;
                }
                UpdateEnemyAbility(enemy, dt);
                HeroCombatState defender = NearestActor(Position(enemy.Distance), BalanceData.Current.heroRules.enemyMeleeRange);
                if (defender != null)
                {
                    if (enemy.AttackCooldown <= 0)
                    {
                        enemy.AttackCooldown = enemy.AttackInterval;
                        AttackActor(enemy, defender);
                    }
                }
                else
                {
                    float multiplier = enemy.Slow > 0 ? BalanceData.Current.Tower((int)TowerKind.Frost).slowSpeedMultiplier : 1f;
                    if (enemy.SkillSlowRemaining > 0) multiplier = Mathf.Min(multiplier, enemy.SkillSlowMultiplier);
                    float before = enemy.Distance;
                    enemy.Distance += enemy.Speed * multiplier * dt;
                    RecordForwardMovement(enemy, before, dt);
                }
                if (enemy.Distance >= PathLength)
                {
                    ResolveOriginal(enemy, false);
                    enemy.Dead = true; Lives = Mathf.Max(0, Lives - enemy.LeakDamage);
                    Popups.Add(new Popup { Position = Path[Path.Length - 1], Text = "−" + enemy.LeakDamage, Damage = true });
                }
            }
        }

        private HeroCombatState NearestActor(Vector2 origin, float range)
        {
            HeroCombatState result = null;
            float best = range;
            if (Hero.Alive && Vector2.Distance(origin, Hero.Position) <= best)
            { result = Hero; best = Vector2.Distance(origin, Hero.Position); }
            if (Clone != null && Clone.Alive && Vector2.Distance(origin, Clone.Position) < best)
                result = Clone;
            return result;
        }

        private void UpdateEnemyAbility(Enemy enemy, float dt)
        {
            SkeletonDefinition variant = enemy.Variant;
            if (variant.summonInterval <= 0 && variant.projectileInterval <= 0 && variant.lightningInterval <= 0) return;
            enemy.SpecialCooldown = Mathf.Max(0, enemy.SpecialCooldown - dt);
            if (enemy.SpecialCooldown > .00001f) return;
            Vector2 origin = Position(enemy.Distance);
            if (variant.summonInterval > 0)
            {
                enemy.SpecialCooldown = variant.summonInterval;
                if (variant.summonLimit > 0 && enemy.SummonsCreated >= variant.summonLimit) return;
                if (variant.summonMaxAlive > 0 && LivingSummons(enemy.Id) >= variant.summonMaxAlive) return;
                pendingSpawns.Add(CreateSummonedEnemy(enemy, SkeletonKind.Normal, true));
                enemy.SummonsCreated++;
                EnemyEffects.Add(new EnemyEffect { Kind = "summon", Position = origin, Target = origin });
            }
            else if (variant.projectileInterval > 0)
            {
                HeroCombatState target = NearestActor(origin, variant.projectileRange);
                if (target == null) return;
                enemy.SpecialCooldown = variant.projectileInterval;
                EnemyProjectiles.Add(new EnemyProjectile {
                    Start = origin, Position = origin, SourceId = enemy.Id, Target = target, TargetLifeSerial = target.LifeSerial,
                    Damage = variant.projectileDamage, Speed = variant.projectileSpeed
                });
            }
            else
            {
                enemy.SpecialCooldown = variant.lightningInterval;
                HeroCombatState target = Hero.Alive ? Hero : (Clone != null && Clone.Alive ? Clone : null);
                if (target == null) return;
                if (!Roll(variant.lightningChance))
                {
                    EnemyEffects.Add(new EnemyEffect { Kind = "smoke", Position = origin, Target = origin });
                    return;
                }
                if (Hero.Alive && Clone != null && Clone.Alive && random.Next(2) == 1) target = Clone;
                EnemyEffects.Add(new EnemyEffect { Kind = "lightning", Position = origin, Target = target.Position, Life = .8f });
                if (!ActorDodges(target)) DamageActor(target, Mathf.Max(0, target.Hp - 1), false, true);
            }
        }

        private void EnemyMeleeAttack(Enemy enemy) => AttackActor(enemy, Hero);
        private void AttackActor(Enemy enemy, HeroCombatState actor)
        {
            if (!actor.Alive || ActorDodges(actor)) return;
            SkeletonDefinition variant = enemy.Variant;
            if (variant.instantKillChance > 0 && Roll(variant.instantKillChance))
            {
                EnemyEffects.Add(new EnemyEffect { Kind = "execution", Position = Position(enemy.Distance), Target = actor.Position });
                DamageActor(actor, actor.Hp, false, true); return;
            }
            if (variant.knockdownDuration > 0)
            {
                DamageActor(actor, actor.MaxHp * variant.knockdownMaxHpFraction, false);
                if (actor.Alive)
                {
                    actor.KnockdownRemaining = variant.knockdownDuration; actor.Destination = actor.Position;
                    EnemyEffects.Add(new EnemyEffect { Kind = "knockdown", Position = actor.Position, Target = actor.Position });
                }
            }
            else DamageActor(actor, enemy.AttackDamage, true);
        }

        private bool HeroDodges() => ActorDodges(Hero);
        private bool ActorDodges(HeroCombatState actor)
        {
            bool dodged = actor.DodgeChance > 0 && Roll(actor.DodgeChance);
            if (dodged) Popups.Add(new Popup { Position = actor.Position, Text = "Уклонение" });
            return dodged;
        }

        private void DamageHero(float damage) => DamageActor(Hero, damage, true);
        private void DamageActor(HeroCombatState actor, float damage, bool physical, bool absolute = false)
        {
            if (!actor.Alive || damage <= 0) return;
            ActorActivity(actor);
            if (!absolute)
            {
                if (physical) damage *= 1 - actor.PhysicalArmor;
                if (actor.IsClone) damage *= Systems.cloneIncomingDamageMultiplier;
                if (actor.RageRemaining > 0) damage *= Systems.rageIncomingDamageMultiplier;
            }
            actor.Hp = Mathf.Max(0, actor.Hp - damage); actor.Flash = .18f;
            if (!actor.Alive)
            {
                actor.KnockdownRemaining = 0; actor.Destination = actor.Position;
                actor.CastPoseRemaining = 0; actor.LifeSerial++;
                if (actor.IsClone) RemoveClone();
                else actor.RespawnRemaining = actor.Equipment.Artifact == ArtifactKind.AegisOfDawn ?
                    Systems.aegisRespawnSeconds : BalanceData.Current.heroRules.respawnSeconds;
            }
        }

        private void UpdateEnemyProjectiles(float dt)
        {
            for (int i = EnemyProjectiles.Count - 1; i >= 0; i--)
            {
                EnemyProjectile projectile = EnemyProjectiles[i];
                HeroCombatState target = projectile.Target ?? Hero;
                if (!target.Alive || projectile.TargetLifeSerial != target.LifeSerial) { EnemyProjectiles.RemoveAt(i); continue; }
                float travel = projectile.Speed * dt;
                if (Vector2.Distance(projectile.Position, target.Position) <= travel)
                {
                    if (!ActorDodges(target)) DamageActor(target, projectile.Damage, true);
                    EnemyProjectiles.RemoveAt(i);
                }
                else projectile.Position = Vector2.MoveTowards(projectile.Position, target.Position, travel);
            }
        }

        private bool TryEvade(Enemy enemy)
        {
            if (!enemy.DodgeReady || enemy.Variant.evadeInterval <= 0) return false;
            enemy.DodgeReady = false; enemy.DodgeCooldown = enemy.Variant.evadeInterval;
            Vector2 point = Position(enemy.Distance);
            EnemyEffects.Add(new EnemyEffect { Kind = "dodge", Position = point, Target = point });
            return true;
        }

        private Enemy Nearest(Vector2 origin, float range, bool allowBoss = true, bool allowFrogs = true, bool magical = false)
        {
            Enemy result = null; float best = range;
            foreach (Enemy enemy in Enemies)
            {
                if (!enemy.Targetable || (!allowFrogs && enemy.IsFrog) || (!allowBoss && InstantKillProtected(enemy)) || (magical && enemy.MagicalImmune)) continue;
                float distance = Vector2.Distance(origin, Position(enemy.Distance));
                if (distance <= best) { best = distance; result = enemy; }
            }
            return result;
        }

        private void UpdateHeroCombat(float dt) => UpdateActorCombat(Hero, dt);

        private void UpdateActorCombat(HeroCombatState actor, float dt)
        {
            if (!actor.Alive) return;
            if (actor.KnockdownRemaining > 0)
            {
                actor.AttackCooldown = Mathf.Max(0, actor.AttackCooldown - dt);
                actor.FrogCooldown = Mathf.Max(0, actor.FrogCooldown - dt);
                actor.SunCooldown = Mathf.Max(0, actor.SunCooldown - dt);
                actor.DecapitateCooldown = Mathf.Max(0, actor.DecapitateCooldown - dt);
                return;
            }
            actor.AttackCooldown = Mathf.Max(0, actor.AttackCooldown - dt);
            if (actor.Kind == HeroKind.Circe)
            {
                actor.FrogCooldown = Mathf.Max(0, actor.FrogCooldown - dt);
                actor.SunCooldown = Mathf.Max(0, actor.SunCooldown - dt);
                if (actor.Definition.skills[0].trigger == "cooldown" && actor.FrogCooldown <= 0)
                {
                    Enemy target = Nearest(actor.Position, actor.Range, true, false, true);
                    if (target != null) ActorTryHex(actor, target);
                }
                if (actor.SunCooldown <= 0)
                {
                    Enemy target = Nearest(actor.Position, actor.Range, false, true, true);
                    if (target != null) { ActorSkillShot(actor, target, "sun", TowerKind.Ember); TryInstantKill(target, true); actor.SunCooldown = actor.Definition.skills[1].cooldown; }
                }
            }
            else
            {
                actor.DecapitateCooldown = Mathf.Max(0, actor.DecapitateCooldown - dt);
                if (actor.DecapitateCooldown <= 0)
                {
                    Enemy target = Nearest(actor.Position, actor.Range, false);
                    if (target != null) { ActorSkillShot(actor, target, "decapitate", TowerKind.Archer); TryInstantKill(target); actor.DecapitateCooldown = actor.Definition.skills[0].cooldown; }
                }
            }
            if (actor.AttackCooldown > 0) return;
            Enemy victim = Nearest(actor.Position, actor.Range, true, true, actor.Kind == HeroKind.Circe);
            if (victim == null) return;
            actor.AttackCooldown = actor.AttackInterval; actor.Flash = .18f;
            float basicDamage = actor.Damage;
            if (actor.Equipment.CritChance > 0 && Roll(actor.Equipment.CritChance)) basicDamage *= Mathf.Max(1, actor.Equipment.CritMultiplier);
            if (actor.Kind == HeroKind.Circe)
            {
                ActorLaunch(actor, victim, ProjectileKind.Arcane, basicDamage, false, 0);
                SkillDefinition fire = actor.Definition.skills[2];
                if (Roll(fire.chance))
                    for (int i = 0; i < fire.projectiles; i++) ActorLaunch(actor, victim, ProjectileKind.Fireball, fire.damagePerProjectile * actor.MagicDamageMultiplier, false, i * .1f);
                SkillDefinition hex = actor.Definition.skills[0];
                if (hex.trigger == "basic_attack" && actor.FrogCooldown <= 0 && Roll(hex.chance)) ActorTryHex(actor, victim);
            }
            else
            {
                ActorSkillShot(actor, victim, "sword", TowerKind.Archer);
                bool landed = Hit(victim, basicDamage, false);
                if (landed) ApplyBasicSplash(victim, basicDamage, false, actor.Equipment.SplashRadius, actor.Equipment.SplashFraction);
                if (landed && Roll(actor.Definition.skills[2].chance))
                {
                    Enemy knifeTarget = Nearest(actor.Position, BalanceData.Current.heroRules.knifeRange, false);
                    if (knifeTarget != null) ActorLaunch(actor, knifeTarget, ProjectileKind.Knife, 0, true, 0);
                }
            }
        }

        private bool Roll(float chance) => random.NextDouble() < chance;
        private bool TryHex(Enemy target) => ActorTryHex(Hero, target);
        private bool ActorTryHex(HeroCombatState actor, Enemy target)
        {
            if (!target.Targetable || target.IsFrog || target.MagicalImmune || (target.IsBoss && !actor.Definition.skills[0].transformsBoss)) return false;
            ActorSkillShot(actor, target, "frog", TowerKind.Frost);
            target.FrogRemaining = actor.Definition.skills[0].duration;
            actor.FrogCooldown = actor.Definition.skills[0].cooldown;
            return true;
        }
        private void SkillShot(Enemy target, string skill, TowerKind kind) => ActorSkillShot(Hero, target, skill, kind);
        private void ActorSkillShot(HeroCombatState actor, Enemy target, string skill, TowerKind kind)
        {
            ActorActivity(actor);
            Shots.Add(new Shot { Start = actor.Position, End = Position(target.Distance), Kind = kind, Skill = skill, Life = skill == "sun" ? .55f : .23f });
        }
        private void Launch(Enemy target, ProjectileKind kind, float damage, bool instantKill, float delay) => ActorLaunch(Hero, target, kind, damage, instantKill, delay);
        private void ActorLaunch(HeroCombatState actor, Enemy target, ProjectileKind kind, float damage, bool instantKill, float delay)
        {
            ActorActivity(actor);
            Projectiles.Add(new HeroProjectile { Start = actor.Position, Position = actor.Position, TargetId = target.Id, Kind = kind, Damage = damage, InstantKill = instantKill, Delay = delay, Source = actor,
                SplashRadius = kind == ProjectileKind.Arcane ? actor.Equipment.SplashRadius : 0,
                SplashFraction = kind == ProjectileKind.Arcane ? actor.Equipment.SplashFraction : 0 });
        }
        private void UpdateProjectiles(float dt)
        {
            for (int i = Projectiles.Count - 1; i >= 0; i--)
            {
                HeroProjectile projectile = Projectiles[i];
                Enemy target = Enemies.Find(enemy => enemy.Id == projectile.TargetId && enemy.Targetable);
                if (target == null) { Projectiles.RemoveAt(i); continue; }
                if (projectile.Delay > 0) { projectile.Delay -= dt; continue; }
                Vector2 destination = Position(target.Distance);
                float travel = BalanceData.Current.heroRules.projectileSpeed * dt;
                if (Vector2.Distance(projectile.Position, destination) <= travel)
                {
                    if (projectile.InstantKill) TryInstantKill(target);
                    else if (Hit(target, projectile.Damage, true))
                        ApplyBasicSplash(target, projectile.Damage, true, projectile.SplashRadius, projectile.SplashFraction);
                    Projectiles.RemoveAt(i);
                }
                else projectile.Position = Vector2.MoveTowards(projectile.Position, destination, travel);
            }
        }

        private void UpdateTowers(float dt)
        {
            foreach (Tower tower in Towers)
            {
                tower.Cooldown -= dt;
                if (tower.Cooldown > 0) continue;
                Enemy target = null; Vector2 origin = Sites[tower.Site];
                foreach (Enemy enemy in Enemies)
                    if (enemy.Targetable && !(tower.Kind != TowerKind.Archer && enemy.MagicalImmune) && Vector2.Distance(Position(enemy.Distance), origin) <= tower.Range && (target == null || enemy.Distance > target.Distance)) target = enemy;
                if (target == null) { tower.Cooldown = 0; continue; }
                Vector2 impact = Position(target.Distance);
                tower.Cooldown = tower.Interval; tower.Flash = .18f;
                if (tower.Kind != TowerKind.Frost)
                {
                    TowerProjectiles.Add(new TowerProjectile {
                        Start = origin, Position = origin, Impact = impact, TargetId = target.Id, Kind = tower.Kind,
                        Damage = tower.Damage, SplashRadius = tower.Definition.splashRadiusBase + tower.Level * tower.Definition.splashRadiusPerLevel
                    });
                }
                else
                {
                    Shots.Add(new Shot { Start = origin, End = impact, Kind = tower.Kind });
                    bool landed = Hit(target, tower.Damage, true);
                    if (landed && !target.Dead && !target.IsBoss) target.Slow = tower.Definition.slowDurationBase + tower.Level * tower.Definition.slowDurationPerLevel;
                }
            }
        }

        private bool Hit(Enemy enemy, float damage, bool magical)
        {
            // Only Archer shots and Achilles's ordinary hits may consume the Ninja's charge.
            return ApplyHit(enemy, damage, magical, !magical);
        }
        private bool ApplyHit(Enemy enemy, float damage, bool magical, bool canEvade)
        {
            if (!enemy.Targetable || damage <= 0 || (magical && enemy.MagicalImmune) || (canEvade && TryEvade(enemy))) return false;
            float armor = magical ? 0 : enemy.PhysicalArmor;
            enemy.Hp -= damage * (1 - Mathf.Clamp01(armor)); enemy.HitFlash = .12f;
            if (enemy.Hp <= 0) Kill(enemy);
            return true;
        }
        private void ApplyBasicSplash(Enemy primary, float damage, bool magical, float radius, float fraction)
        {
            if (radius <= 0 || fraction <= 0) return;
            Vector2 impact = Position(primary.Distance);
            foreach (Enemy enemy in Enemies)
                if (enemy != primary && enemy.Targetable && Vector2.Distance(Position(enemy.Distance), impact) <= radius)
                    ApplyHit(enemy, damage * fraction, magical, false);
        }
        private bool TryInstantKill(Enemy enemy, bool magical = false)
        {
            if (!enemy.Targetable || (magical && enemy.MagicalImmune) || InstantKillProtected(enemy)) return false;
            Kill(enemy); return true;
        }
        private static bool InstantKillProtected(Enemy enemy) => enemy.IsBoss && BalanceData.Current.heroRules.bossesImmuneToInstantKill;
        private void Kill(Enemy enemy)
        {
            if (enemy.Dead) return;
            ResolveOriginal(enemy, true);
            enemy.Hp = 0; enemy.Dead = true; Gold += enemy.Reward; Kills++;
            if (enemy.Reward > 0) Popups.Add(new Popup { Position = Position(enemy.Distance), Text = "+" + enemy.Reward });
            if (enemy.HasSarcophagus && enemy.Variant.rebirth)
            {
                enemy.HasSarcophagus = false;
                pendingSpawns.Add(CreateSummonedEnemy(enemy, enemy.Skeleton));
                EnemyEffects.Add(new EnemyEffect { Kind = "rebirth", Position = Position(enemy.Distance), Target = Position(enemy.Distance), Life = .9f });
            }
        }
        private void ResolveOriginal(Enemy enemy, bool killed)
        {
            if (enemy.IsSummoned || enemy.SpawnOrdinal <= 0) return;
            WaveRun owner = WaveRuns.Find(run => run.Number == enemy.WaveNumber);
            if (owner == null || enemy.SpawnOrdinal > owner.Plan.Count || !owner.RemovedOrdinals.Add(enemy.SpawnOrdinal)) return;
            if (killed && owner.KilledOrdinals.Add(enemy.SpawnOrdinal)) owner.OriginalKills++;
            RecordEligibility(owner);
        }
        public int LivingSummons(int summonerId)
        {
            int count = 0;
            foreach (Enemy enemy in Enemies) if (!enemy.Dead && enemy.IsSummoned && enemy.SummonerId == summonerId) count++;
            foreach (Enemy enemy in pendingSpawns) if (!enemy.Dead && enemy.IsSummoned && enemy.SummonerId == summonerId) count++;
            return count;
        }
        private Enemy CreateSummonedEnemy(Enemy owner, SkeletonKind kind, bool timedSummon = false)
        {
            Enemy summoned = CreateEnemy(kind, owner.Distance, true, false);
            summoned.WaveNumber = owner.WaveNumber; summoned.SummonerId = owner.Id;
            if (timedSummon && owner.Variant.summonedHp > 0)
                summoned.Hp = summoned.MaxHp = owner.Variant.summonedHp;
            return summoned;
        }
    }
}
