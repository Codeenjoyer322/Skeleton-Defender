using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SkeletonDefender
{
    public enum HeroKind { Circe, Achilles }
    public enum ItemCategory { Armor, Weapon, Artifact }
    public enum EquipmentSlot { Helmet, Chest, Gloves, Belt, Trousers, Boots, Weapon, Artifact }
    public enum ItemRarity { Common = 0, Rare = 1, Mystical = 2, Mysthical = Mystical }
    public enum WeaponKind { Sword = 0, Staff = 1, Dagger = 2 }
    public enum LootRollPolicy { SingleItem }

    [Serializable]
    public sealed class InventoryItem : EquipmentStats
    {
        public string Id;
        public WeaponKind Weapon;
        public ArmorKind Armor;
        public ItemRarity Rarity;
        // Zero identifies saved armor from before the v0.6.1 slot-weight revision.
        public int ArmorBalanceRevision;
        public ItemCategory ItemType = ItemCategory.Weapon;
        public EquipmentSlot ItemSlot = EquipmentSlot.Weapon;
        public ItemCategory Category => ItemType;
        public EquipmentSlot Slot => ItemSlot;
        public string Name => Category == ItemCategory.Artifact ? ArtifactName(Artifact) :
            (Category == ItemCategory.Armor ? ArmorName(Armor) + " · " + SlotName(Slot) : WeaponName(Weapon)) + " · " + RarityName(Rarity);

        public bool CanEquip(HeroKind hero)
        {
            if (hero != HeroKind.Circe && hero != HeroKind.Achilles) return false;
            if (Category == ItemCategory.Artifact) return true;
            if (Category == ItemCategory.Armor)
                return Armor == ArmorKind.Leather || Armor == ArmorKind.Silk ||
                    Armor == ArmorKind.Chainmail;
            return Category == ItemCategory.Weapon &&
                ((hero == HeroKind.Circe && (Weapon == WeaponKind.Staff || Weapon == WeaponKind.Sword)) ||
                 (hero == HeroKind.Achilles && (Weapon == WeaponKind.Sword || Weapon == WeaponKind.Dagger || Weapon == WeaponKind.Staff)));
        }

        public bool RequiresConfirmation(HeroKind hero) => hero == HeroKind.Circe &&
            ((Category == ItemCategory.Weapon && Weapon == WeaponKind.Sword) || (Category == ItemCategory.Armor && Armor == ArmorKind.Chainmail));

        public string EquipWarning(HeroKind hero)
        {
            if (!RequiresConfirmation(hero)) return "";
            EquipmentBalance balance = EquipmentBalance.Default;
            if (Category == ItemCategory.Weapon) return "Меч уменьшает весь магический урон Цирцеи на " + Percent(1 - balance.circeSwordMagicMultiplier) + ".";
            float weight = balance.slotWeights[(int)Slot];
            return "Цирцея: скорость передвижения −" + Percent(balance.circeChainmailMovePenalty * weight) +
                ", восстановление маны −" + Percent(balance.circeChainmailManaRegenPenalty * weight) +
                " от базового. Бонусы восстановления от вещей сохраняются. Замедление Ахилла не добавляется.";
        }

        public static string WeaponName(WeaponKind kind) => kind == WeaponKind.Sword ? "Меч" : kind == WeaponKind.Staff ? "Посох" : "Кинжал";
        public static string ArmorName(ArmorKind kind) => kind == ArmorKind.Leather ? "Кожа" : kind == ArmorKind.Chainmail ? "Кольчуга" : "Шёлк";
        public static string RarityName(ItemRarity rarity) =>
            rarity == ItemRarity.Common ? "Common" : rarity == ItemRarity.Rare ? "Rare" : "Mystical";
        public static string ArtifactName(ArtifactKind kind) => kind == ArtifactKind.AegisOfDawn ? "Aegis of Dawn" :
            kind == ArtifactKind.ZeusNail ? "Ноготь Зевса" : kind == ArtifactKind.AthenaMirror ? "Зеркало Афины" : "";
        public static string HeroName(HeroKind hero) => hero == HeroKind.Circe ? "Цирцея" : "Ахилл";
        public static string SlotName(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Helmet: return "Шлем";
                case EquipmentSlot.Chest: return "Броня";
                case EquipmentSlot.Gloves: return "Перчатки";
                case EquipmentSlot.Belt: return "Пояс";
                case EquipmentSlot.Trousers: return "Штаны";
                case EquipmentSlot.Boots: return "Сапоги";
                case EquipmentSlot.Weapon: return "Оружие";
                case EquipmentSlot.Artifact: return "Артефакт";
                default: return "";
            }
        }

        public string Description
        {
            get
            {
                if (Category == ItemCategory.Artifact)
                {
                    if (Artifact == ArtifactKind.AegisOfDawn) return "Воскрешение героя через 3 с после смерти. Действует, пока артефакт надет.";
                    if (Artifact == ArtifactKind.ZeusNail) return "Один раз за карту: ярость на 5 минут. Весь урон и частота атак ×2; входящий урон ÷2.";
                    if (Artifact == ArtifactKind.AthenaMirror) return "Один раз за карту: управляемая копия на 5 минут. Собственные HP, мана и навыки; получает на 50% больше урона. Не воскрешается и не использует артефакты.";
                }
                var parts = new List<string>();
                if (DamageBonus > 0)
                    parts.Add(Category == ItemCategory.Weapon && Weapon == WeaponKind.Staff ?
                        "Цирцея: бонус обычного урона +" + Number(DamageBonus) + "; Ахилл: без бонуса урона" :
                        "Урон обычной атаки +" + Number(DamageBonus));
                if (PhysicalArmor > 0) parts.Add("Физическая броня +" + Percent(PhysicalArmor));
                if (DodgeChance > 0) parts.Add("Уклонение +" + Percent(DodgeChance));
                if (AttackSpeedBonus > 0) parts.Add("Частота атак +" + Percent(AttackSpeedBonus));
                if (MoveSpeedMultiplier < 1) parts.Add((Category == ItemCategory.Armor && Armor == ArmorKind.Chainmail ? "Ахилл: скорость передвижения −" : "Скорость передвижения −") + Percent(1 - MoveSpeedMultiplier));
                if (MaxManaBonus > 0) parts.Add("Максимум маны +" + Number(MaxManaBonus));
                if (ManaRegenBonus > 0) parts.Add("Восстановление маны +" + Number(ManaRegenBonus) + "/с");
                if (ManaCostReduction > 0) parts.Add("Расход маны навыков −" + Percent(ManaCostReduction));
                if (CritChance > 0) parts.Add("Шанс критического удара +" + Percent(CritChance) + "; урон ×" + Number(CritMultiplier));
                if (SplashFraction > 0) parts.Add("Сплэш: " + Percent(SplashFraction) + " урона в радиусе " + Number(SplashRadius));
                if (Category == ItemCategory.Armor && Armor == ArmorKind.Chainmail) parts.Add(EquipWarning(HeroKind.Circe));
                parts.Add(RequiresConfirmation(HeroKind.Circe) ? "Ахилл; Цирцея — со штрафом (потребуется подтверждение)" :
                    CanEquip(HeroKind.Circe) && CanEquip(HeroKind.Achilles) ? "Цирцея / Ахилл" : CanEquip(HeroKind.Circe) ? "Цирцея" : "Ахилл");
                return string.Join("\n", parts);
            }
        }

        private static string Number(float value) => value.ToString("0.###", CultureInfo.GetCultureInfo("ru-RU"));
        private static string Percent(float value) => Number(value * 100) + "%";
        internal static bool ValidRarity(ItemRarity rarity) => (int)rarity >= 0 && (int)rarity <= 2;
        internal bool IsValid
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Id) || !HasValidValues) return false;
                if (Category == ItemCategory.Artifact)
                    return Slot == EquipmentSlot.Artifact && (int)Artifact >= 1 && (int)Artifact <= 3;
                if (!ValidRarity(Rarity) || Artifact != ArtifactKind.None) return false;
                if (Category == ItemCategory.Armor)
                    return (int)Slot >= 0 && (int)Slot < 6 && (int)Armor >= 0 && (int)Armor <= 2;
                return Category == ItemCategory.Weapon && Slot == EquipmentSlot.Weapon &&
                    ((Weapon == WeaponKind.Sword || Weapon == WeaponKind.Staff) ? DamageBonus > 0 : Weapon == WeaponKind.Dagger && DamageBonus == 0);
            }
        }
    }

    [Serializable]
    public sealed class HeroEquipment
    {
        public HeroKind Hero;
        public string[] ItemIds = new string[PlayerProfile.SlotCount];
        public HeroEquipment() { }
        public HeroEquipment(HeroKind hero) { Hero = hero; }
    }

    [Serializable]
    public sealed class PlayerProfile
    {
        public const int SlotCount = 8;
        public HeroKind SelectedHero;
        public int UnlockedMap = 1;
        public bool StarterArmorGranted;
        public bool StarterWeaponsGranted;
        public List<InventoryItem> Items = new List<InventoryItem>();
        public HeroEquipment[] Equipment = {
            new HeroEquipment(HeroKind.Circe), new HeroEquipment(HeroKind.Achilles)
        };

        public InventoryItem FindItem(string id) => string.IsNullOrEmpty(id) || Items == null
            ? null : Items.Find(item => item != null && item.Id == id);

        public HeroEquipment EquipmentFor(HeroKind hero)
        {
            if (!ValidHero(hero) || Equipment == null) return null;
            foreach (HeroEquipment record in Equipment)
                if (record != null && record.Hero == hero) return record;
            return null;
        }

        public InventoryItem EquippedItem(HeroKind hero, int slot)
        {
            HeroEquipment record = EquipmentFor(hero);
            if (!ValidSlot(slot) || record?.ItemIds == null || record.ItemIds.Length <= slot) return null;
            InventoryItem item = FindItem(record.ItemIds[slot]);
            return item != null && item.IsValid && (int)item.Slot == slot && item.CanEquip(hero) ? item : null;
        }

        public bool Equip(HeroKind hero, int slot, string itemId)
        {
            if (!ValidHero(hero) || !ValidSlot(slot)) return false;
            InventoryItem item = FindItem(itemId);
            if (item == null || !item.IsValid || (int)item.Slot != slot || !item.CanEquip(hero)) return false;
            Normalize();
            foreach (HeroEquipment record in Equipment)
                for (int i = 0; i < record.ItemIds.Length; i++)
                    if (record.ItemIds[i] == itemId && (record.Hero != hero || i != slot)) return false;
            EquipmentFor(hero).ItemIds[slot] = itemId;
            return true;
        }

        public bool Unequip(HeroKind hero, int slot)
        {
            HeroEquipment record = EquipmentFor(hero);
            if (!ValidSlot(slot) || record?.ItemIds == null || record.ItemIds.Length <= slot || string.IsNullOrEmpty(record.ItemIds[slot])) return false;
            record.ItemIds[slot] = null;
            return true;
        }

        public float EquippedWeaponBonus(HeroKind hero)
        {
            InventoryItem item = EquippedItem(hero, (int)EquipmentSlot.Weapon);
            return item == null || (hero == HeroKind.Achilles && item.Weapon == WeaponKind.Staff) ? 0 : item.DamageBonus;
        }

        public EquipmentStats EquippedStats(HeroKind hero) => AggregateStats(hero, null);

        public string PreviewEquipWarning(HeroKind hero, string itemId)
        {
            InventoryItem item = FindItem(itemId);
            if (item == null || !item.IsValid || !item.CanEquip(hero) || !item.RequiresConfirmation(hero)) return "";
            EquipmentStats after = AggregateStats(hero, item);
            float baseRegeneration = BalanceData.Current.heroSystems.manaRegenerationPerSecond;
            float regeneration = baseRegeneration * after.ManaRegenMultiplier + after.ManaRegenBonus;
            return item.EquipWarning(hero) + "\nПосле замены этого слота, с учётом всей экипировки:\n" +
                "Магический урон: " + (after.MagicDamageMultiplier * 100).ToString("0.##", CultureInfo.GetCultureInfo("ru-RU")) + "%\n" +
                "Базовое восстановление маны: " + (after.ManaRegenMultiplier * 100).ToString("0.##", CultureInfo.GetCultureInfo("ru-RU")) + "%\n" +
                "Итого маны/с с бонусами: " + regeneration.ToString("0.###", CultureInfo.GetCultureInfo("ru-RU")) + "\n" +
                "Скорость передвижения: " + (after.MoveSpeedMultiplier * 100).ToString("0.##", CultureInfo.GetCultureInfo("ru-RU")) + "%";
        }

        private EquipmentStats AggregateStats(HeroKind hero, InventoryItem replacement)
        {
            var total = new EquipmentStats();
            if (!ValidHero(hero)) return total;
            EquipmentBalance balance = EquipmentBalance.Default;
            balance.Validate();
            float movementPenalty = 0, heavyWeight = 0;
            for (int slot = 0; slot < SlotCount; slot++)
            {
                InventoryItem item = replacement != null && (int)replacement.Slot == slot ? replacement : EquippedItem(hero, slot);
                if (item == null) continue;
                if (!(hero == HeroKind.Achilles && item.Category == ItemCategory.Weapon && item.Weapon == WeaponKind.Staff))
                    total.DamageBonus += item.DamageBonus;
                total.PhysicalArmor += item.PhysicalArmor;
                total.DodgeChance += item.DodgeChance;
                total.AttackSpeedBonus += item.AttackSpeedBonus;
                bool circeChainmail = hero == HeroKind.Circe && item.Category == ItemCategory.Armor && item.Armor == ArmorKind.Chainmail;
                movementPenalty += circeChainmail ? balance.circeChainmailMovePenalty * balance.slotWeights[slot] : 1 - item.MoveSpeedMultiplier;
                total.MaxManaBonus += item.MaxManaBonus;
                total.ManaRegenBonus += item.ManaRegenBonus;
                total.ManaCostReduction += item.ManaCostReduction;
                total.CritChance += item.CritChance;
                total.CritMultiplier = Math.Max(total.CritMultiplier, item.CritMultiplier);
                total.SplashRadius = Math.Max(total.SplashRadius, item.SplashRadius);
                total.SplashFraction += item.SplashFraction;
                if (item.Category == ItemCategory.Artifact) total.Artifact = item.Artifact;
                if (circeChainmail)
                    heavyWeight += balance.slotWeights[slot];
                if (hero == HeroKind.Circe && item.Category == ItemCategory.Weapon && item.Weapon == WeaponKind.Sword)
                    total.MagicDamageMultiplier *= balance.circeSwordMagicMultiplier;
            }
            total.PhysicalArmor = Math.Min(balance.physicalArmorCap, total.PhysicalArmor);
            total.DodgeChance = Math.Min(balance.dodgeCap, total.DodgeChance);
            total.ManaCostReduction = Math.Min(balance.manaCostReductionCap, total.ManaCostReduction);
            total.CritChance = Math.Min(1, total.CritChance);
            total.MoveSpeedMultiplier = Math.Max(.01f, 1 - movementPenalty);
            total.MagicDamageMultiplier *= Math.Max(0, 1 - balance.circeChainmailMagicPenalty * heavyWeight);
            total.ManaRegenMultiplier = Math.Max(0, 1 - balance.circeChainmailManaRegenPenalty * heavyWeight);
            return total;
        }

        // Restore only owned, compatible equipment; one item cannot occupy multiple slots/heroes.
        public void Normalize()
        {
            if (!ValidHero(SelectedHero)) SelectedHero = HeroKind.Circe;
            UnlockedMap = Math.Max(1, Math.Min(3, UnlockedMap));
            var validItems = new List<InventoryItem>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (Items != null)
                foreach (InventoryItem item in Items)
                    if (item != null && item.IsValid && ids.Add(item.Id)) validItems.Add(item);
            Items = validItems;

            HeroEquipment[] restored = {
                new HeroEquipment(HeroKind.Circe), new HeroEquipment(HeroKind.Achilles)
            };
            var equippedIds = new HashSet<string>(StringComparer.Ordinal);
            if (Equipment != null)
                foreach (HeroEquipment old in Equipment)
                {
                    if (old == null || !ValidHero(old.Hero) || old.ItemIds == null) continue;
                    HeroEquipment target = restored[(int)old.Hero];
                    for (int slot = 0; slot < Math.Min(SlotCount, old.ItemIds.Length); slot++)
                    {
                        InventoryItem item = FindItem(old.ItemIds[slot]);
                        if (item == null || target.ItemIds[slot] != null || (int)item.Slot != slot || !item.CanEquip(old.Hero) || !equippedIds.Add(item.Id)) continue;
                        target.ItemIds[slot] = item.Id;
                    }
                }
            Equipment = restored;
        }

        private static bool ValidHero(HeroKind hero) => hero == HeroKind.Circe || hero == HeroKind.Achilles;
        private static bool ValidSlot(int slot) => slot >= 0 && slot < SlotCount;
    }

    [Serializable]
    public sealed class LootDefinition
    {
        public bool ruleConfirmed;
        public string policy;
        public float swordChance;
        public bool dropsOnVictoryOnly;
        public float commonChance;
        public float rareChance;
        public float mysticalChance;
        // Legacy JSON field is retained for compatibility with older exporters.
        public float mysthicalChance;
        public float artifactChance;
        public bool artifactDropsEnabled;
        public WeaponRollDefinition[] weapons;
        public string source;
    }

    [Serializable]
    public sealed class WeaponRollDefinition
    {
        public WeaponKind weapon;
        public ItemRarity rarity;
        public int minimum;
        public int maximum;
    }

    public static class InventoryLoot
    {
        public static void DamageRange(WeaponKind weapon, ItemRarity rarity, out int minimum, out int maximum)
        {
            if (weapon != WeaponKind.Sword && weapon != WeaponKind.Staff && weapon != WeaponKind.Dagger) throw new ArgumentOutOfRangeException(nameof(weapon));
            if (!InventoryItem.ValidRarity(rarity)) throw new ArgumentOutOfRangeException(nameof(rarity));
            if (weapon == WeaponKind.Dagger) { minimum = maximum = 0; return; }
            WeaponRollDefinition[] definitions = BalanceData.Current.loot?.weapons;
            if (definitions != null)
                foreach (WeaponRollDefinition definition in definitions)
                    if (definition != null && definition.weapon == weapon && definition.rarity == rarity)
                    {
                        if (definition.minimum <= 0 || definition.maximum < definition.minimum || definition.maximum == int.MaxValue)
                            throw new InvalidOperationException("Invalid weapon bonus range in balance.json.");
                        minimum = definition.minimum;
                        maximum = definition.maximum;
                        return;
                    }
            throw new InvalidOperationException("Missing weapon bonus range in balance.json.");
        }

        public static InventoryItem GenerateWeapon(WeaponKind weapon, ItemRarity rarity, System.Random random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            DamageRange(weapon, rarity, out int minimum, out int maximum);
            EquipmentBalance balance = EquipmentBalance.Default;
            balance.Validate();
            var item = new InventoryItem {
                Id = Guid.NewGuid().ToString("N"), Weapon = weapon, Rarity = rarity,
                ItemType = ItemCategory.Weapon, ItemSlot = EquipmentSlot.Weapon,
                DamageBonus = weapon == WeaponKind.Dagger ? 0 : random.Next(minimum, maximum + 1)
            };
            int quality = (int)rarity;
            if (weapon == WeaponKind.Sword)
            { item.SplashRadius = balance.swordSplashRadius; item.SplashFraction = balance.swordSplashFractions[quality]; }
            else if (weapon == WeaponKind.Staff) item.ManaCostReduction = balance.staffManaCostReductions[quality];
            else
            { item.CritChance = balance.daggerCritChance[quality]; item.AttackSpeedBonus = balance.daggerAttackSpeed[quality]; }
            return item;
        }

        public static InventoryItem GenerateArmor(ArmorKind armor, EquipmentSlot slot, ItemRarity rarity)
        {
            if ((int)armor < 0 || (int)armor > 2) throw new ArgumentOutOfRangeException(nameof(armor));
            if ((int)slot < 0 || (int)slot >= 6) throw new ArgumentOutOfRangeException(nameof(slot));
            if (!InventoryItem.ValidRarity(rarity)) throw new ArgumentOutOfRangeException(nameof(rarity));
            EquipmentBalance balance = EquipmentBalance.Default;
            balance.Validate();
            float weight = balance.slotWeights[(int)slot], scale = weight * balance.rarityScales[(int)rarity];
            var item = new InventoryItem { Id = Guid.NewGuid().ToString("N"), ItemType = ItemCategory.Armor,
                ItemSlot = slot, Armor = armor, Rarity = rarity, ArmorBalanceRevision = InventoryStore.CurrentArmorBalanceRevision };
            if (armor == ArmorKind.Chainmail)
            { item.PhysicalArmor = balance.chainmailArmor * scale; item.MoveSpeedMultiplier = 1 - balance.chainmailSlow * weight; }
            else if (armor == ArmorKind.Leather)
            { item.PhysicalArmor = balance.leatherArmor * scale; item.DodgeChance = balance.leatherDodge * scale; }
            else
            { item.PhysicalArmor = balance.silkArmor * scale; item.MaxManaBonus = balance.silkMana * scale; item.ManaRegenBonus = balance.silkManaRegen * scale; }
            return item;
        }

        public static InventoryItem GenerateArtifact(ArtifactKind artifact)
        {
            if ((int)artifact < 1 || (int)artifact > 3) throw new ArgumentOutOfRangeException(nameof(artifact));
            return new InventoryItem { Id = Guid.NewGuid().ToString("N"), ItemType = ItemCategory.Artifact,
                ItemSlot = EquipmentSlot.Artifact, Artifact = artifact };
        }
    }

    // Owned by one battle. A victory resolves one cumulative roll, including a no-drop outcome.
    public sealed class MapLootClaim
    {
        public bool Claimed { get; private set; }

        public List<InventoryItem> TryClaim(PlayerProfile profile, bool victory, System.Random random)
        {
            LootDefinition definition = BalanceData.Current.loot;
            if (definition == null || !definition.ruleConfirmed || definition.policy != "SingleItem" || !definition.dropsOnVictoryOnly)
                throw new InvalidOperationException("The confirmed single-item victory loot rule is missing in balance.json.");
            return ResolveClaim(profile, victory, random);
        }

        public List<InventoryItem> TryClaim(PlayerProfile profile, bool victory, System.Random random, LootRollPolicy policy, float swordChance)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (policy != LootRollPolicy.SingleItem) throw new ArgumentOutOfRangeException(nameof(policy));
            if (!ValidChance(swordChance)) throw new ArgumentOutOfRangeException(nameof(swordChance));
            // Compatibility overload. The former two-weapon split is superseded by the three-type gear rule.
            return TryClaim(profile, victory, random);
        }

        private List<InventoryItem> ResolveClaim(PlayerProfile profile, bool victory, System.Random random)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (random == null) throw new ArgumentNullException(nameof(random));
            var awards = new List<InventoryItem>();
            if (Claimed || !victory) return awards;

            LootDefinition definition = BalanceData.Current.loot;
            if (definition == null || !ValidChance(definition.commonChance) || !ValidChance(definition.rareChance) ||
                !ValidChance(definition.mysticalChance) || !ValidChance(definition.mysthicalChance) || !ValidChance(definition.artifactChance))
                throw new InvalidOperationException("Invalid loot probabilities in balance.json.");
            double common = definition.commonChance, rare = definition.rareChance;
            double mystical = definition.mysticalChance;
            double artifacts = definition.artifactDropsEnabled ? definition.artifactChance : 0;
            if (common + rare + mystical + artifacts > 1) throw new InvalidOperationException("Exclusive loot probabilities exceed one.");
            EquipmentBalance balance = EquipmentBalance.Default;
            balance.Validate();
            double roll = random.NextDouble();
            if (roll < artifacts) awards.Add(InventoryLoot.GenerateArtifact((ArtifactKind)random.Next(1, 4)));
            else if (roll < artifacts + mystical + rare + common)
            {
                ItemRarity rarity = roll < artifacts + mystical ? ItemRarity.Mystical :
                    roll < artifacts + mystical + rare ? ItemRarity.Rare : ItemRarity.Common;
                if (random.NextDouble() < balance.ordinaryWeaponChance)
                    awards.Add(InventoryLoot.GenerateWeapon((WeaponKind)random.Next(0, 3), rarity, random));
                else awards.Add(InventoryLoot.GenerateArmor((ArmorKind)random.Next(0, 3), (EquipmentSlot)random.Next(0, 6), rarity));
            }
            // Do not leave a partial award if generation fails before all rolls are resolved.
            profile.Normalize();
            profile.Items.AddRange(awards);
            Claimed = true;
            return awards;
        }

        private static bool ValidChance(float chance) => !float.IsNaN(chance) && !float.IsInfinity(chance) && chance >= 0 && chance <= 1;
    }

    public static class InventoryStore
    {
        public const int SchemaVersion = 2;
        public const int CurrentArmorBalanceRevision = 1;
        // Keep the original storage key so existing installations are migrated in place.
        private const string SaveKey = "SkeletonDefender.PlayerProfile.v1";
        private static bool protectUnknownSave;

        [Serializable]
        private sealed class SaveEnvelope
        {
            public int Version;
            public PlayerProfile Profile;
        }

        public static string Serialize(PlayerProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            profile.Normalize();
            return JsonUtility.ToJson(new SaveEnvelope { Version = SchemaVersion, Profile = profile });
        }

        public static PlayerProfile Deserialize(string json)
        {
            return ReadEnvelope(json, out _, out _, out _);
        }

        private static PlayerProfile ReadEnvelope(string json, out int version, out bool recognized, out bool armorUpdated)
        {
            version = 0;
            armorUpdated = false;
            recognized = string.IsNullOrWhiteSpace(json);
            if (recognized) return new PlayerProfile();
            try
            {
                SaveEnvelope envelope = JsonUtility.FromJson<SaveEnvelope>(json);
                if (envelope == null) return new PlayerProfile();
                version = envelope.Version;
                if ((version != 1 && version != SchemaVersion) || envelope.Profile == null) return new PlayerProfile();
                recognized = true;
                if (version == 1 && envelope.Profile.Items != null)
                    foreach (InventoryItem item in envelope.Profile.Items)
                    {
                        if (item == null) continue;
                        // v1 contains weapons only. Preserve the saved roll and IDs; do not roll new affixes.
                        item.ItemType = ItemCategory.Weapon; item.ItemSlot = EquipmentSlot.Weapon;
                        item.MoveSpeedMultiplier = 1; item.CritMultiplier = 2;
                        item.MagicDamageMultiplier = 1; item.ManaRegenMultiplier = 1;
                    }
                envelope.Profile.Normalize();
                armorUpdated = UpgradeArmorBalance(envelope.Profile);
                return envelope.Profile;
            }
            catch (ArgumentException) { return new PlayerProfile(); }
            catch (InvalidOperationException) { return new PlayerProfile(); }
            catch (FormatException) { return new PlayerProfile(); }
        }

        public static bool UpgradeArmorBalance(PlayerProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (profile.Items == null) return false;
            bool changed = false;
            float[] oldWeights = { .15f, .30f, .10f, .10f, .20f, .15f };
            EquipmentBalance balance = EquipmentBalance.Default;
            balance.Validate();
            foreach (InventoryItem item in profile.Items)
            {
                if (item == null || !item.IsValid || item.Category != ItemCategory.Armor || item.ArmorBalanceRevision != 0) continue;
                float scale = balance.slotWeights[(int)item.Slot] / oldWeights[(int)item.Slot];
                // Preserve the original rarity and rolled strength; revise only the share assigned to this slot.
                item.PhysicalArmor *= scale;
                item.DodgeChance *= scale;
                item.MaxManaBonus *= scale;
                item.ManaRegenBonus *= scale;
                item.MoveSpeedMultiplier = 1 - (1 - item.MoveSpeedMultiplier) * scale;
                item.ArmorBalanceRevision = CurrentArmorBalanceRevision;
                changed = true;
            }
            return changed;
        }

        public static bool GrantStarterEquipment(PlayerProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            EquipmentBalance balance = EquipmentBalance.Default;
            bool armor = !profile.StarterArmorGranted;
            bool weapons = balance.grantStarterWeapons && !profile.StarterWeaponsGranted;
            if (!armor && !weapons) return false;
            // Stage the complete grant before mutating the profile, and keep existing equipment untouched.
            var additions = new List<InventoryItem>();
            if (armor)
                foreach (ArmorKind kind in new[] { ArmorKind.Chainmail, ArmorKind.Silk })
                    for (int slot = 0; slot < 6; slot++)
                    {
                        InventoryItem item = InventoryLoot.GenerateArmor(kind, (EquipmentSlot)slot, ItemRarity.Common);
                        item.Id = "starter-v06-" + kind + "-" + slot;
                        if (profile.FindItem(item.Id) == null) additions.Add(item);
                    }
            if (weapons)
                foreach (WeaponKind kind in new[] { WeaponKind.Sword, WeaponKind.Staff })
                {
                    InventoryItem item = InventoryLoot.GenerateWeapon(kind, ItemRarity.Common, new System.Random(6006 + (int)kind));
                    item.Id = "starter-v06-" + kind;
                    if (profile.FindItem(item.Id) == null) additions.Add(item);
                }
            profile.Normalize();
            profile.Items.AddRange(additions);
            if (armor) profile.StarterArmorGranted = true;
            if (weapons) profile.StarterWeaponsGranted = true;
            return true;
        }

        public static PlayerProfile Load()
        {
            string json = PlayerPrefs.GetString(SaveKey, "");
            PlayerProfile profile = ReadEnvelope(json, out int version, out bool recognized, out bool armorUpdated);
            protectUnknownSave = !recognized;
            if (protectUnknownSave)
                Debug.LogWarning("Inventory save could not be read. It is preserved unchanged; local saving is disabled for this session.");
            bool changed = GrantStarterEquipment(profile);
            if (!protectUnknownSave && (changed || armorUpdated || version == 1))
            {
                // Preserve the original v1 text as well as the migrated item ownership.
                if (version == 1 && !PlayerPrefs.HasKey(SaveKey + ".before-v2"))
                {
                    try { PlayerPrefs.SetString(SaveKey + ".before-v2", json); }
                    catch (PlayerPrefsException exception) { Debug.LogWarning("Could not back up v1 inventory: " + exception.Message); return profile; }
                }
                if (armorUpdated && !PlayerPrefs.HasKey(SaveKey + ".before-armor-revision-1"))
                {
                    try { PlayerPrefs.SetString(SaveKey + ".before-armor-revision-1", json); }
                    catch (PlayerPrefsException exception) { Debug.LogWarning("Could not back up armor inventory: " + exception.Message); return profile; }
                }
                Save(profile);
            }
            return profile;
        }

        public static bool Save(PlayerProfile profile)
        {
            if (protectUnknownSave)
            { Debug.LogWarning("Local saving is disabled to preserve the unreadable inventory save."); return false; }
            string json = Serialize(profile);
            try
            {
                PlayerPrefs.SetString(SaveKey, json);
                PlayerPrefs.Save();
                return true;
            }
            catch (PlayerPrefsException exception)
            {
                Debug.LogWarning("Could not save local inventory: " + exception.Message);
                return false;
            }
        }
    }
}
