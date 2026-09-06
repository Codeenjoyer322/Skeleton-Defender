using System;

namespace SkeletonDefender
{
    [Serializable]
    public sealed class HeroSystemsData
    {
        public string source;
        public float baseMana = 100;
        public float manaRegenerationPerSecond = 1;
        public float towerProjectileSpeed = 280;
        public float artifactDuration = 300;
        public float aegisRespawnSeconds = 3;
        public float cloneIncomingDamageMultiplier = 1.5f;
        public float rageDamageMultiplier = 2;
        public float rageIncomingDamageMultiplier = .5f;
        public float rageAttackRateMultiplier = 2;
        public float spearHistorySeconds = 2;
        public float spearKnockbackFraction = .5f;
        public float deerHitRadius = 24;
        public float deerLaneOffset = 10;
        public ManualSkillDefinition[] circe;
        public ManualSkillDefinition[] achilles;
        public ManualSkillDefinition Skill(HeroKind kind, int index)
        {
            if (index < 0 || index > 1) throw new ArgumentOutOfRangeException(nameof(index));
            ManualSkillDefinition[] skills = kind == HeroKind.Circe ? circe : achilles;
            if (skills == null || skills.Length != 2) throw new InvalidOperationException("Missing manual hero skills in balance.json.");
            return skills[index];
        }
    }

    [Serializable]
    public sealed class ManualSkillDefinition
    {
        public string name;
        public float manaCost;
        public float cooldown;
        public float damage;
        public int projectiles;
        public float duration;
        public float slowFraction;
        public float slowDuration;
        public float areaRadius;
        public float visualDelay;
    }
}
