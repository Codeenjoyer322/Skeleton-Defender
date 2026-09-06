using System;
using UnityEngine;

namespace SkeletonDefender
{
    // This resource is the shared source for gameplay and the balance workbook.
    [Serializable]
    public sealed class BalanceData
    {
        public int schemaVersion;
        public int mapCount;
        public int plannedMapCount;
        public string mapName;
        public int wavesPerMap;
        public EconomyDefinition economy;
        public HeroDefinition[] heroes;
        public EnemyRankDefinition[] enemyRanks;
        public EnemyRankDefinition boss;
        public EnemyFamilyDefinition[] enemyFamilies;
        public SkeletonDefinition[] skeletons;
        public TowerDefinition[] towers;
        public WaveDefinition[] waves;
        public SpawnScheduleDefinition spawnSchedule;
        public HeroRulesDefinition heroRules;
        public HeroSystemsData heroSystems = new HeroSystemsData();
        public EquipmentBalance equipment = new EquipmentBalance();
        public LootDefinition loot = new LootDefinition();
        public string[] pending;

        private static BalanceData current;
        public static BalanceData Current
        {
            get
            {
                if (current != null) return current;
                TextAsset resource = Resources.Load<TextAsset>("balance");
                if (resource == null) throw new InvalidOperationException("Missing Resources/balance.json.");
                current = JsonUtility.FromJson<BalanceData>(resource.text);
                if (current == null || current.schemaVersion != 1)
                    throw new InvalidOperationException("Unsupported balance data.");
                return current;
            }
        }

        public HeroDefinition Hero(int kind) => heroes[kind];
        public EnemyRankDefinition Rank(int rank) => enemyRanks[rank - 1];
        public EnemyFamilyDefinition Family(int kind) => enemyFamilies[kind];
        public TowerDefinition Tower(int kind) => towers[kind];
        public WaveDefinition Wave(int number) => waves[number - 1];
        public SkeletonDefinition Skeleton(int kind) => skeletons[kind];
        public static void Reload() { current = null; }
    }

    [Serializable]
    public sealed class EconomyDefinition
    {
        public int startingGold;
        public int startingLives;
        public int waveBonusBase;
        public int waveBonusPerWave;
        public float towerSellFraction;
        public string source;
    }

    [Serializable]
    public sealed class HeroDefinition
    {
        public string id;
        public string name;
        public string role;
        public float hp;
        public float damage;
        public float attackInterval;
        public SkillDefinition[] skills;
        public string source;
    }

    [Serializable]
    public sealed class SkillDefinition
    {
        public string id;
        public string name;
        public float cooldown;
        public float duration;
        public float chance;
        public int projectiles;
        public float damagePerProjectile;
        public bool instantKill;
        public bool transformsBoss;
        public string trigger;
        public string source;
    }

    [Serializable]
    public sealed class EnemyRankDefinition
    {
        public int rank;
        public float hp;
        public float damage;
        public float attacksPerSecond;
        public float physicalArmor;
        public float magicalArmor;
        public string source;
        public float AttackInterval => 1f / attacksPerSecond;
    }

    [Serializable]
    public sealed class EnemyFamilyDefinition
    {
        public string id;
        public string name;
        public string bossName;
        public float walkSpeed;
        public int reward;
        public int leakDamage;
        public int bossReward;
        public int bossLeakDamage;
        public string source;
    }

    [Serializable]
    public sealed class SkeletonDefinition
    {
        public string id;
        public string name;
        public float hp;
        public float damage;
        public float attackInterval;
        public float walkSpeed;
        public float speedMultiplier;
        public float visualScale;
        public float physicalArmor;
        public int reward;
        public bool magicalImmune;
        public int summonedReward;
        public float evadeInterval;
        public float summonInterval;
        public int summonLimit;
        public int summonMaxAlive;
        public float summonedHp;
        public float projectileInterval;
        public float projectileDamage;
        public float projectileRange;
        public float projectileSpeed;
        public float instantKillChance;
        public float lightningInterval;
        public float lightningChance;
        public float knockdownDuration;
        public float knockdownMaxHpFraction;
        public bool rebirth;
        public string source;
    }

    [Serializable]
    public sealed class TowerDefinition
    {
        public string id;
        public string name;
        public int cost;
        public int maxLevel;
        public float baseDamage;
        public float damagePerAdditionalLevel;
        public float rangeBase;
        public float rangePerLevel;
        public float baseInterval;
        public float attackRatePerAdditionalLevel;
        public int upgradeCostPerLevel;
        public float splashRadiusBase;
        public float splashRadiusPerLevel;
        public float slowSpeedMultiplier;
        public float slowDurationBase;
        public float slowDurationPerLevel;
        public string source;
    }

    [Serializable]
    public sealed class WaveDefinition
    {
        public int number;
        public int count;
        public int rank;
        public int bossOrdinal;
        public int difficultyTier;
        public int[] rankCounts;
        public int[] skeletonCounts;
        public bool reinforced;
        public string source;
    }

    [Serializable]
    public sealed class SpawnScheduleDefinition
    {
        public float initialDelay;
        public float batchInterval;
        public float fractionPerBatch;
        public float minimumFractionPerBatch;
        public float withinBatchInterval;
        public float bossGongSeconds;
        public float waveSlotSeconds;
        public float spawnWindowSeconds;
        public float targetMapSeconds;
        public float batchActiveFraction;
        public string earlyWaveTrigger;
        public float earlyWaveFraction;
        public int earlyWaveBonus;
        public string source;
    }

    [Serializable]
    public sealed class HeroRulesDefinition
    {
        public float circeRange;
        public float achillesRange;
        public float enemyMeleeRange;
        public float heroWalkSpeed;
        public float circeWalkSpeed;
        public float achillesWalkSpeed;
        public float projectileSpeed;
        public float knifeRange;
        public float spawnDistanceFromExit;
        public float respawnSeconds;
        public float regenerationIdleSeconds;
        public float regenerationMaxHpFractionPerSecond;
        public bool bossesImmuneToInstantKill;
        public string deathRule;
        public string source;
    }
}
