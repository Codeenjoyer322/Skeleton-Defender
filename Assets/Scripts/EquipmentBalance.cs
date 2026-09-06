using System;

namespace SkeletonDefender
{
    public enum ArmorKind { Leather, Chainmail, Silk }
    public enum ArtifactKind { None, AegisOfDawn, ZeusNail, AthenaMirror }

    // Values are snapshotted into each item when it is created, then saved with that item.
    [Serializable]
    public class EquipmentStats
    {
        public float DamageBonus;
        public float PhysicalArmor;
        public float DodgeChance;
        public float AttackSpeedBonus;
        public float MoveSpeedMultiplier = 1;
        public float MaxManaBonus;
        public float ManaRegenBonus;
        public float ManaCostReduction;
        public float CritChance;
        public float CritMultiplier = 2;
        public float SplashRadius;
        public float SplashFraction;
        public float MagicDamageMultiplier = 1;
        // Multiplies base regeneration only; the flat ManaRegenBonus is added afterwards.
        public float ManaRegenMultiplier = 1;
        public ArtifactKind Artifact;

        public EquipmentStats Copy() => new EquipmentStats {
            DamageBonus = DamageBonus, PhysicalArmor = PhysicalArmor, DodgeChance = DodgeChance,
            AttackSpeedBonus = AttackSpeedBonus, MoveSpeedMultiplier = MoveSpeedMultiplier,
            MaxManaBonus = MaxManaBonus, ManaRegenBonus = ManaRegenBonus,
            ManaCostReduction = ManaCostReduction, CritChance = CritChance, CritMultiplier = CritMultiplier,
            SplashRadius = SplashRadius, SplashFraction = SplashFraction, Artifact = Artifact,
            MagicDamageMultiplier = MagicDamageMultiplier, ManaRegenMultiplier = ManaRegenMultiplier
        };

        internal bool HasValidValues => Nonnegative(DamageBonus) && Nonnegative(PhysicalArmor) &&
            Nonnegative(DodgeChance) && Nonnegative(AttackSpeedBonus) && Nonnegative(MaxManaBonus) &&
            Nonnegative(ManaRegenBonus) && Nonnegative(ManaCostReduction) && Nonnegative(CritChance) &&
            Nonnegative(SplashRadius) && Nonnegative(SplashFraction) && Nonnegative(MoveSpeedMultiplier) &&
            MoveSpeedMultiplier > 0 && MoveSpeedMultiplier <= 1 && Nonnegative(CritMultiplier) && CritMultiplier >= 1 &&
            Nonnegative(MagicDamageMultiplier) && MagicDamageMultiplier <= 1 && Nonnegative(ManaRegenMultiplier) && ManaRegenMultiplier <= 1;

        internal static bool Nonnegative(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0;
    }

    [Serializable]
    public sealed class EquipmentBalance
    {
        private static readonly EquipmentBalance Fallback = new EquipmentBalance();
        public static EquipmentBalance Default => BalanceData.Current.equipment ?? Fallback;

        public float chainmailArmor = .30f;
        public float leatherArmor = .12f;
        public float leatherDodge = .08f;
        public float silkArmor = .06f;
        public float silkMana = 30;
        public float silkManaRegen = .3f;
        public float chainmailSlow = .20f;
        // Order follows EquipmentSlot: Helmet, Chest, Gloves, Belt, Trousers, Boots.
        public float[] slotWeights = { .15f, .35f, .10f, .05f, .20f, .15f };
        public float[] rarityScales = { 1, 1.5f, 2 };
        public float physicalArmorCap = .8f;
        public float dodgeCap = .6f;
        public float manaCostReductionCap = .5f;
        public float[] daggerCritChance = { .025f, .05f, .15f };
        public float[] daggerAttackSpeed = { .1f, .2f, .3f };
        public float swordSplashRadius = 30;
        public float[] swordSplashFractions = { .25f, .35f, .5f };
        public float[] staffManaCostReductions = { .1f, .15f, .2f };
        public float ordinaryWeaponChance = .5f;
        public bool grantStarterWeapons = true;
        public float circeSwordMagicMultiplier = .5f;
        public float circeChainmailMagicPenalty = 0;
        public float circeChainmailMovePenalty = .75f;
        public float circeChainmailManaRegenPenalty = .75f;
        public string source;

        internal void Validate()
        {
            float[] values = { chainmailArmor, leatherArmor, leatherDodge, silkArmor, silkMana,
                silkManaRegen, chainmailSlow, physicalArmorCap, dodgeCap, manaCostReductionCap,
                swordSplashRadius, ordinaryWeaponChance, circeSwordMagicMultiplier,
                circeChainmailMagicPenalty, circeChainmailMovePenalty, circeChainmailManaRegenPenalty };
            foreach (float value in values)
                if (!EquipmentStats.Nonnegative(value)) throw new InvalidOperationException("Invalid equipment balance value.");
            if (chainmailSlow >= 1 || physicalArmorCap >= 1 || dodgeCap >= 1 || manaCostReductionCap >= 1 || ordinaryWeaponChance > 1 ||
                circeSwordMagicMultiplier > 1 || circeChainmailMagicPenalty > 1 || circeChainmailMovePenalty >= 1 || circeChainmailManaRegenPenalty > 1)
                throw new InvalidOperationException("Invalid equipment balance fraction.");
            ValidateArray(slotWeights, 6); ValidateArray(rarityScales, 3);
            ValidateArray(daggerCritChance, 3); ValidateArray(daggerAttackSpeed, 3);
            ValidateArray(swordSplashFractions, 3); ValidateArray(staffManaCostReductions, 3);
            float weight = 0;
            foreach (float part in slotWeights) weight += part;
            if (Math.Abs(weight - 1) > .0001f) throw new InvalidOperationException("Armor slot weights must sum to one.");
        }

        private static void ValidateArray(float[] values, int count)
        {
            if (values == null || values.Length != count) throw new InvalidOperationException("Missing equipment balance array.");
            foreach (float value in values)
                if (!EquipmentStats.Nonnegative(value)) throw new InvalidOperationException("Invalid equipment balance array value.");
        }
    }
}
