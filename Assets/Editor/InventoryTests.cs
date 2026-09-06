using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SkeletonDefender.Editor
{
    public static class InventoryTests
    {
        private static void Check(bool condition, string message)
        { if (!condition) throw new Exception("INVENTORY TEST: " + message); }
        private static void Near(float actual, float expected, string message)
        { Check(Math.Abs(actual - expected) < .0001f, message + " (" + actual + " vs " + expected + ")"); }

        [MenuItem("Skeleton Defender/Run inventory checks")]
        public static void Run()
        {
            CheckEquipment(); CheckSaves(); CheckArmorRevision(); CheckRolls(); CheckClaims();
            Debug.Log("SKELETON_INVENTORY_TESTS_PASSED: equipment, wrong-gear penalties, stable item rolls, v1 migration, once-only starters, inclusive weapon ranges, 54 armor variants, three artifacts, exclusive victory loot, atomic claims. No user save was modified.");
        }

        private static void CheckEquipment()
        {
            var profile = new PlayerProfile();
            Check(profile.Items.Count == 0 && profile.Equipment.Length == 2 && profile.Equipment[0].ItemIds.Length == 8, "Raw profile constructor grants gear or misses slots");
            EquipmentStats empty = profile.EquippedStats(HeroKind.Circe);
            Near(empty.MoveSpeedMultiplier, 1, "Unarmed speed"); Near(empty.CritMultiplier, 2, "Unarmed critical multiplier");
            Near(empty.MagicDamageMultiplier, 1, "Unarmed magic"); Near(empty.ManaRegenMultiplier, 1, "Unarmed regeneration");
            Check(empty.Artifact == ArtifactKind.None && empty.DamageBonus == 0 && empty.PhysicalArmor == 0, "Unarmed bonus");
            var random = new System.Random(713);
            InventoryItem sword = InventoryLoot.GenerateWeapon(WeaponKind.Sword, ItemRarity.Rare, random);
            InventoryItem staff = InventoryLoot.GenerateWeapon(WeaponKind.Staff, ItemRarity.Mystical, random);
            InventoryItem dagger = InventoryLoot.GenerateWeapon(WeaponKind.Dagger, ItemRarity.Common, random);
            profile.Items.AddRange(new[] { sword, staff, dagger });
            Check(staff.CanEquip(HeroKind.Achilles) && !staff.RequiresConfirmation(HeroKind.Achilles) && !profile.Equip(HeroKind.Circe, 6, dagger.Id), "Weapon compatibility");
            Check(!profile.Equip(HeroKind.Circe, 0, staff.Id) && !profile.Equip(HeroKind.Circe, -1, staff.Id) && !profile.Equip(HeroKind.Circe, 8, staff.Id) &&
                !profile.Equip((HeroKind)99, 6, staff.Id) && !profile.Equip(HeroKind.Circe, 6, "missing"), "Invalid slot, hero or ownership accepted");
            Check(profile.Equip(HeroKind.Circe, 6, staff.Id) && profile.Equip(HeroKind.Achilles, 6, sword.Id), "Compatible weapon rejected");
            Check(!profile.Equip(HeroKind.Circe, 6, sword.Id), "One item equipped by two heroes");
            Near(profile.EquippedWeaponBonus(HeroKind.Circe), staff.DamageBonus, "Legacy bonus API");
            Check(profile.Unequip(HeroKind.Achilles, 6) && !profile.Unequip(HeroKind.Achilles, 6), "Unequip idempotence");
            Check(sword.CanEquip(HeroKind.Circe) && sword.RequiresConfirmation(HeroKind.Circe) && !sword.RequiresConfirmation(HeroKind.Achilles), "Sword warning eligibility");
            Check(profile.PreviewEquipWarning(HeroKind.Circe, sword.Id).Contains("50%") && profile.EquippedItem(HeroKind.Circe, 6).Id == staff.Id, "Prospective warning changed equipment or missed penalty");
            Check(profile.Equip(HeroKind.Circe, 6, sword.Id), "Confirmed wrong sword rejected");
            Near(profile.EquippedStats(HeroKind.Circe).MagicDamageMultiplier, .5f, "Sword penalty");
            foreach (ArmorKind kind in new[] { ArmorKind.Chainmail, ArmorKind.Leather, ArmorKind.Silk })
            {
                var owner = new PlayerProfile(); HeroKind hero = kind == ArmorKind.Chainmail ? HeroKind.Achilles : HeroKind.Circe;
                for (int part = 0; part < 6; part++)
                {
                    InventoryItem armor = InventoryLoot.GenerateArmor(kind, (EquipmentSlot)part, ItemRarity.Common);
                    owner.Items.Add(armor); Check(owner.Equip(hero, part, armor.Id), "Matching armor slot rejected");
                    if (kind == ArmorKind.Silk) Check(armor.CanEquip(HeroKind.Achilles) && !armor.RequiresConfirmation(HeroKind.Achilles), "Achilles should accept silk without penalty");
                    if (kind == ArmorKind.Leather) Check(armor.CanEquip(HeroKind.Circe) && armor.CanEquip(HeroKind.Achilles), "Leather compatibility");
                }
                EquipmentStats stats = owner.EquippedStats(hero);
                Near(stats.PhysicalArmor, kind == ArmorKind.Chainmail ? .30f : kind == ArmorKind.Leather ? .12f : .06f, "Full Common armor");
                Near(stats.DodgeChance, kind == ArmorKind.Leather ? .08f : 0, "Full Common dodge");
                Near(stats.MoveSpeedMultiplier, kind == ArmorKind.Chainmail ? .8f : 1, "Full Common movement");
                Near(stats.MaxManaBonus, kind == ArmorKind.Silk ? 30 : 0, "Full Common mana");
                Near(stats.ManaRegenBonus, kind == ArmorKind.Silk ? .3f : 0, "Full Common mana regeneration");
            }
            for (int part = 0; part < 6; part++)
            {
                InventoryItem heavy = InventoryLoot.GenerateArmor(ArmorKind.Chainmail, (EquipmentSlot)part, ItemRarity.Common);
                profile.Items.Add(heavy);
                Check(heavy.RequiresConfirmation(HeroKind.Circe) && !string.IsNullOrEmpty(heavy.EquipWarning(HeroKind.Circe)), "Missing chainmail warning");
                Check(profile.Equip(HeroKind.Circe, part, heavy.Id), "Confirmed chainmail rejected");
            }
            EquipmentStats wrong = profile.EquippedStats(HeroKind.Circe);
            Near(wrong.MagicDamageMultiplier, .5f, "Chainmail should not add a magic penalty to sword"); Near(wrong.ManaRegenMultiplier, .25f, "Chainmail base mana penalty");
            Near(wrong.MoveSpeedMultiplier, .25f, "Circe chainmail movement penalty must replace the ordinary penalty");
            InventoryItem chest = InventoryLoot.GenerateArmor(ArmorKind.Chainmail, EquipmentSlot.Chest, ItemRarity.Rare); profile.Items.Add(chest);
            string preview = profile.PreviewEquipWarning(HeroKind.Circe, chest.Id);
            Check(preview.Contains("50%") && preview.Contains("25%") && preview.Contains("Базовое восстановление") && preview.Contains("0,25"), "Prospective replacement missed the base-only regeneration penalty");
            Check(profile.PreviewEquipWarning(HeroKind.Circe, "missing") == "", "Unowned item warning");
            InventoryItem artifact = InventoryLoot.GenerateArtifact(ArtifactKind.AthenaMirror); profile.Items.Add(artifact);
            Check(profile.Equip(HeroKind.Circe, 7, artifact.Id) && !profile.Equip(HeroKind.Achilles, 7, artifact.Id), "Artifact ownership");
            EquipmentStats copy = profile.EquippedStats(HeroKind.Circe).Copy(); copy.DamageBonus = 999; copy.Artifact = ArtifactKind.None;
            Check(profile.EquippedWeaponBonus(HeroKind.Circe) != 999 && profile.EquippedStats(HeroKind.Circe).Artifact == ArtifactKind.AthenaMirror, "Snapshot shares item state");
            InventoryItem exaggerated = profile.EquippedItem(HeroKind.Circe, 0);
            exaggerated.PhysicalArmor = 5; exaggerated.DodgeChance = 5; exaggerated.ManaCostReduction = 5;
            EquipmentStats capped = profile.EquippedStats(HeroKind.Circe);
            Near(capped.PhysicalArmor, .8f, "Armor cap"); Near(capped.DodgeChance, .6f, "Dodge cap"); Near(capped.ManaCostReduction, .5f, "Mana cost cap");

            var silkAchilles = new PlayerProfile();
            for (int part = 0; part < 6; part++)
            {
                InventoryItem silk = InventoryLoot.GenerateArmor(ArmorKind.Silk, (EquipmentSlot)part, ItemRarity.Common);
                silkAchilles.Items.Add(silk); Check(silkAchilles.Equip(HeroKind.Achilles, part, silk.Id), "Achilles could not equip silk");
            }
            EquipmentStats silkStats = silkAchilles.EquippedStats(HeroKind.Achilles);
            Near(silkStats.MaxManaBonus, 30, "Silk lost maximum mana on Achilles"); Near(silkStats.ManaRegenBonus, .3f, "Silk lost regeneration on Achilles");
            Near(silkStats.MoveSpeedMultiplier, 1, "Silk slows Achilles"); Near(silkStats.MagicDamageMultiplier, 1, "Silk penalizes damage"); Near(silkStats.ManaRegenMultiplier, 1, "Silk penalizes base regeneration");
            InventoryItem achillesStaff = InventoryLoot.GenerateWeapon(WeaponKind.Staff, ItemRarity.Mystical, new System.Random(113));
            silkAchilles.Items.Add(achillesStaff); Check(silkAchilles.Equip(HeroKind.Achilles, 6, achillesStaff.Id), "Achilles could not equip a staff");
            EquipmentStats staffStats = silkAchilles.EquippedStats(HeroKind.Achilles);
            Near(staffStats.DamageBonus, 0, "Staff should not add damage for Achilles"); Near(staffStats.ManaCostReduction, .2f, "Staff mana reduction missing for Achilles");
            Near(silkAchilles.EquippedWeaponBonus(HeroKind.Achilles), 0, "Legacy effective bonus API still adds staff damage for Achilles");
            Check(achillesStaff.DamageBonus > 0, "Staff's saved damage roll was deleted");
            Near(staffStats.MagicDamageMultiplier, 1, "Staff added a damage penalty");
            Check(achillesStaff.Description.Contains("Цирцея: бонус обычного урона") && achillesStaff.Description.Contains("Ахилл: без бонуса урона"), "Staff tooltip should identify each hero's effective damage bonus");
            Check(silkAchilles.Unequip(HeroKind.Achilles, 6) && silkAchilles.Equip(HeroKind.Circe, 6, achillesStaff.Id), "Shared staff cannot be transferred to Circe after unequipping");
            Near(silkAchilles.EquippedWeaponBonus(HeroKind.Circe), achillesStaff.DamageBonus, "Transferred staff lost Circe damage bonus");
            Near(silkAchilles.EquippedStats(HeroKind.Circe).DamageBonus, achillesStaff.DamageBonus, "Circe aggregate staff bonus mismatch");

            var mixed = new PlayerProfile();
            InventoryItem heavyChest = InventoryLoot.GenerateArmor(ArmorKind.Chainmail, EquipmentSlot.Chest, ItemRarity.Common);
            InventoryItem silkBelt = InventoryLoot.GenerateArmor(ArmorKind.Silk, EquipmentSlot.Belt, ItemRarity.Common);
            mixed.Items.AddRange(new[] { heavyChest, silkBelt }); mixed.Equip(HeroKind.Circe, 1, heavyChest.Id); mixed.Equip(HeroKind.Circe, 3, silkBelt.Id);
            EquipmentStats mixedStats = mixed.EquippedStats(HeroKind.Circe);
            Near(mixedStats.MoveSpeedMultiplier, .7375f, "Chest movement must use its 35 percent share of the 75 percent total");
            Near(mixedStats.ManaRegenMultiplier, .7375f, "Chest base regeneration share"); Near(mixedStats.ManaRegenBonus, .015f, "Silk belt retains its full flat mana regeneration");
            Near(mixedStats.MagicDamageMultiplier, 1, "Chainmail still penalizes Circe magic");
            Check(heavyChest.EquipWarning(HeroKind.Circe).Contains("26,25%") && heavyChest.Description.Contains("от базового"), "Warning/tooltip fails to identify base-only penalty");
        }

        private static void CheckArmorRevision()
        {
            var old = new PlayerProfile { SelectedHero = HeroKind.Achilles, StarterArmorGranted = true, StarterWeaponsGranted = true };
            // Explicit v0.6 rolls: chest held 30% of a set, belt 10%. Other slots are unchanged.
            var chest = new InventoryItem { Id = "saved-heavy-chest", ItemType = ItemCategory.Armor, ItemSlot = EquipmentSlot.Chest,
                Armor = ArmorKind.Chainmail, Rarity = ItemRarity.Common, PhysicalArmor = .09f, MoveSpeedMultiplier = .94f };
            var belt = new InventoryItem { Id = "saved-heavy-belt", ItemType = ItemCategory.Armor, ItemSlot = EquipmentSlot.Belt,
                Armor = ArmorKind.Chainmail, Rarity = ItemRarity.Mystical, PhysicalArmor = .06f, MoveSpeedMultiplier = .98f };
            var silkChest = new InventoryItem { Id = "saved-silk-chest", ItemType = ItemCategory.Armor, ItemSlot = EquipmentSlot.Chest,
                Armor = ArmorKind.Silk, Rarity = ItemRarity.Rare, PhysicalArmor = .027f, MaxManaBonus = 13.5f, ManaRegenBonus = .135f };
            var leatherBelt = new InventoryItem { Id = "saved-leather-belt", ItemType = ItemCategory.Armor, ItemSlot = EquipmentSlot.Belt,
                Armor = ArmorKind.Leather, Rarity = ItemRarity.Common, PhysicalArmor = .012f, DodgeChance = .008f };
            var helmet = new InventoryItem { Id = "saved-silk-helmet", ItemType = ItemCategory.Armor, ItemSlot = EquipmentSlot.Helmet,
                Armor = ArmorKind.Silk, Rarity = ItemRarity.Common, PhysicalArmor = .009f, MaxManaBonus = 4.5f, ManaRegenBonus = .045f };
            InventoryItem sword = InventoryLoot.GenerateWeapon(WeaponKind.Sword, ItemRarity.Mystical, new System.Random(101));
            InventoryItem staff = InventoryLoot.GenerateWeapon(WeaponKind.Staff, ItemRarity.Rare, new System.Random(102));
            old.Items.AddRange(new[] { chest, belt, silkChest, leatherBelt, helmet, sword, staff });
            old.Equip(HeroKind.Achilles, 1, chest.Id); old.Equip(HeroKind.Achilles, 3, belt.Id); old.Equip(HeroKind.Achilles, 6, sword.Id);
            old.Equip(HeroKind.Circe, 0, helmet.Id); old.Equip(HeroKind.Circe, 1, silkChest.Id); old.Equip(HeroKind.Circe, 6, staff.Id);
            string swordBefore = JsonUtility.ToJson(sword), staffBefore = JsonUtility.ToJson(staff);
            PlayerProfile migrated = InventoryStore.Deserialize(InventoryStore.Serialize(old));
            Check(migrated.Items.Count == 7 && migrated.SelectedHero == HeroKind.Achilles && !InventoryStore.GrantStarterEquipment(migrated), "Armor migration changed ownership or starter flags");
            Near(migrated.FindItem(chest.Id).PhysicalArmor, .105f, "Saved chest weight"); Near(migrated.FindItem(chest.Id).MoveSpeedMultiplier, .93f, "Saved chainmail chest slow");
            Near(migrated.FindItem(belt.Id).PhysicalArmor, .03f, "Saved Mystical belt weight"); Near(migrated.FindItem(belt.Id).MoveSpeedMultiplier, .99f, "Saved belt slow");
            Near(migrated.FindItem(silkChest.Id).MaxManaBonus, 15.75f, "Saved Rare silk chest mana"); Near(migrated.FindItem(silkChest.Id).ManaRegenBonus, .1575f, "Saved Rare silk chest regeneration");
            Near(migrated.FindItem(leatherBelt.Id).DodgeChance, .004f, "Saved leather belt dodge"); Near(migrated.FindItem(helmet.Id).MaxManaBonus, 4.5f, "Unchanged helmet weight drifted");
            Check(JsonUtility.ToJson(migrated.FindItem(sword.Id)) == swordBefore && JsonUtility.ToJson(migrated.FindItem(staff.Id)) == staffBefore, "Armor revision changed weapon rolls");
            foreach (InventoryItem item in migrated.Items)
                if (item.Category == ItemCategory.Armor) Check(item.ArmorBalanceRevision == InventoryStore.CurrentArmorBalanceRevision, "Armor revision flag missing");
            Check(migrated.EquippedItem(HeroKind.Achilles, 1)?.Id == chest.Id && migrated.EquippedItem(HeroKind.Achilles, 3)?.Id == belt.Id &&
                migrated.EquippedItem(HeroKind.Circe, 1)?.Id == silkChest.Id && migrated.EquippedItem(HeroKind.Circe, 6)?.Id == staff.Id, "Armor revision removed equipped references");
            string once = InventoryStore.Serialize(migrated);
            Check(!InventoryStore.UpgradeArmorBalance(migrated) && InventoryStore.Serialize(migrated) == once, "Second in-memory migration changed stats");
            Check(InventoryStore.Serialize(InventoryStore.Deserialize(once)) == once, "Reload repeated armor migration");
            InventoryItem newChest = InventoryLoot.GenerateArmor(ArmorKind.Chainmail, EquipmentSlot.Chest, ItemRarity.Common);
            InventoryItem newBelt = InventoryLoot.GenerateArmor(ArmorKind.Chainmail, EquipmentSlot.Belt, ItemRarity.Common);
            Near(newChest.PhysicalArmor, .105f, "New chest did not use new weights"); Near(newBelt.PhysicalArmor, .015f, "New belt did not use new weights");
            Check(newChest.ArmorBalanceRevision == 1 && newBelt.ArmorBalanceRevision == 1, "New items could be migrated twice");
        }

        private static void CheckSaves()
        {
            const string legacy = "{\"Version\":1,\"Profile\":{\"SelectedHero\":1,\"UnlockedMap\":3,\"Items\":[{\"Id\":\"old-staff\",\"Weapon\":1,\"Rarity\":2,\"DamageBonus\":19},{\"Id\":\"old-sword\",\"Weapon\":0,\"Rarity\":1,\"DamageBonus\":6}],\"Equipment\":[{\"Hero\":0,\"ItemIds\":[null,null,null,null,null,null,\"old-staff\",null]},{\"Hero\":1,\"ItemIds\":[null,null,null,null,null,null,\"old-sword\",null]}]}}";
            PlayerProfile profile = InventoryStore.Deserialize(legacy);
            Check(profile.Items.Count == 2 && profile.SelectedHero == HeroKind.Achilles && profile.UnlockedMap == 3, "v1 ownership or metadata lost");
            InventoryItem oldStaff = profile.FindItem("old-staff");
            Check(oldStaff.Rarity == ItemRarity.Mystical && oldStaff.Name.Contains("Mystical") && oldStaff.Category == ItemCategory.Weapon && oldStaff.Slot == EquipmentSlot.Weapon, "v1 rarity/category migration");
            Near(profile.EquippedWeaponBonus(HeroKind.Circe), 19, "v1 staff roll changed"); Near(profile.EquippedWeaponBonus(HeroKind.Achilles), 6, "v1 sword lost");
            Near(oldStaff.ManaCostReduction, 0, "v1 received newly rolled affixes");
            Check(InventoryStore.GrantStarterEquipment(profile) && profile.Items.Count == 16, "Existing-save starter grant");
            Near(profile.EquippedWeaponBonus(HeroKind.Circe), 19, "Grant replaced equipped staff"); Near(profile.EquippedWeaponBonus(HeroKind.Achilles), 6, "Grant replaced sword");
            Check(!InventoryStore.GrantStarterEquipment(profile) && profile.Items.Count == 16, "Repeated starter grant");
            PlayerProfile restored = InventoryStore.Deserialize(InventoryStore.Serialize(profile));
            Check(restored.Items.Count == 16 && !InventoryStore.GrantStarterEquipment(restored), "Starter flags not persisted");
            restored.StarterArmorGranted = restored.StarterWeaponsGranted = false;
            Check(InventoryStore.GrantStarterEquipment(restored) && restored.Items.Count == 16, "Stable starter IDs failed after recovered flags");
            var fresh = new PlayerProfile(); InventoryStore.GrantStarterEquipment(fresh);
            Check(fresh.Items.Count == 14, "New profile should have 12 armor pieces and two weapons");
            foreach (HeroKind hero in new[] { HeroKind.Circe, HeroKind.Achilles })
                for (int slot = 0; slot < 8; slot++) Check(fresh.EquippedItem(hero, slot) == null, "Starter gear auto-equipped");
            foreach (string corrupt in new[] { "", " ", "null", "[]", "{not json", "{}", "{\"Version\":999,\"Profile\":{}}", "{\"Version\":1,\"Profile\":null}" })
                Check(InventoryStore.Deserialize(corrupt).Items.Count == 0, "Malformed save injected items");
            const string invalid = "{\"Version\":1,\"Profile\":{\"SelectedHero\":999,\"UnlockedMap\":-20,\"Items\":[{\"Id\":\"x\",\"Weapon\":1,\"Rarity\":0,\"DamageBonus\":4},{\"Id\":\"x\",\"Weapon\":0,\"Rarity\":0,\"DamageBonus\":3},{\"Id\":\"bad\",\"Weapon\":9,\"Rarity\":0,\"DamageBonus\":4}],\"Equipment\":[{\"Hero\":0,\"ItemIds\":[\"x\",null,null,null,null,null,\"x\",null]},{\"Hero\":1,\"ItemIds\":[null,null,null,null,null,null,\"x\",null]}]}}";
            PlayerProfile cleaned = InventoryStore.Deserialize(invalid);
            Check(cleaned.Items.Count == 1 && cleaned.UnlockedMap == 1 && cleaned.SelectedHero == HeroKind.Circe, "Duplicate IDs/invalid metadata survived");
            Check(cleaned.EquippedItem(HeroKind.Circe, 0) == null && cleaned.EquippedWeaponBonus(HeroKind.Circe) == 4 && cleaned.EquippedWeaponBonus(HeroKind.Achilles) == 0, "Invalid equipped references survived");
            Check(InventoryStore.Deserialize("{\"Version\":1,\"Profile\":{\"Items\":null,\"Equipment\":null}}").Equipment.Length == 2, "Missing collections not repaired");
            InventoryItem chain = fresh.Items.Find(item => item.Category == ItemCategory.Armor && item.Armor == ArmorKind.Chainmail && item.Slot == EquipmentSlot.Chest);
            InventoryItem sword = fresh.Items.Find(item => item.Category == ItemCategory.Weapon && item.Weapon == WeaponKind.Sword);
            fresh.Equip(HeroKind.Circe, 1, chain.Id); fresh.Equip(HeroKind.Circe, 6, sword.Id);
            restored = InventoryStore.Deserialize(InventoryStore.Serialize(fresh));
            Check(restored.EquippedItem(HeroKind.Circe, 1)?.Id == chain.Id && restored.EquippedItem(HeroKind.Circe, 6)?.Id == sword.Id, "Confirmed wrong gear removed on load");
        }

        private static void CheckRolls()
        {
            var random = new System.Random(819); var profile = new PlayerProfile(); var ids = new HashSet<string>();
            int[,] min = { { 1, 5, 10 }, { 1, 5, 9 } }, max = { { 3, 7, 15 }, { 4, 9, 20 } };
            for (int weapon = 0; weapon < 3; weapon++)
                for (int rarity = 0; rarity < 3; rarity++)
                {
                    var seen = new HashSet<int>();
                    for (int roll = 0; roll < 512; roll++)
                    {
                        InventoryItem item = InventoryLoot.GenerateWeapon((WeaponKind)weapon, (ItemRarity)rarity, random);
                        Check(ids.Add(item.Id) && item.Category == ItemCategory.Weapon && item.Slot == EquipmentSlot.Weapon, "Weapon ID/category invalid");
                        if (weapon < 2) Check(item.DamageBonus >= min[weapon, rarity] && item.DamageBonus <= max[weapon, rarity] && item.DamageBonus == (int)item.DamageBonus, "Inclusive weapon range");
                        else
                        {
                            Near(item.DamageBonus, 0, "Dagger flat damage"); Near(item.CritChance, new[] { .025f, .05f, .15f }[rarity], "Dagger crit");
                            Near(item.AttackSpeedBonus, new[] { .1f, .2f, .3f }[rarity], "Dagger attack frequency");
                        }
                        seen.Add((int)item.DamageBonus); if (roll == 0) profile.Items.Add(item);
                    }
                    if (weapon < 2) Check(seen.Count == max[weapon, rarity] - min[weapon, rarity] + 1, "Weapon endpoint never reached");
                }
            for (int kind = 0; kind < 3; kind++)
                for (int slot = 0; slot < 6; slot++)
                    for (int rarity = 0; rarity < 3; rarity++)
                    {
                        InventoryItem item = InventoryLoot.GenerateArmor((ArmorKind)kind, (EquipmentSlot)slot, (ItemRarity)rarity);
                        Check(ids.Add(item.Id) && item.Category == ItemCategory.Armor && (int)item.Slot == slot && item.PhysicalArmor > 0, "Armor variant invalid"); profile.Items.Add(item);
                    }
            for (int kind = 1; kind <= 3; kind++)
            {
                InventoryItem item = InventoryLoot.GenerateArtifact((ArtifactKind)kind);
                Check(ids.Add(item.Id) && item.Category == ItemCategory.Artifact && item.Slot == EquipmentSlot.Artifact &&
                    !item.Name.Contains("Common") && !item.Name.Contains("Rare") && !item.Name.Contains("Mystical") && !string.IsNullOrEmpty(item.Description), "Artifact rarity/description"); profile.Items.Add(item);
            }
            Check(profile.Items.Count == 66, "Expected nine weapons, 54 armor pieces and three artifacts");
            string json = InventoryStore.Serialize(profile); PlayerProfile restored = InventoryStore.Deserialize(json);
            Check(restored.Items.Count == 66, "New kinds dropped by serialization");
            foreach (InventoryItem item in profile.Items) Check(JsonUtility.ToJson(restored.FindItem(item.Id)) == JsonUtility.ToJson(item), "Saved item stats/type changed");
            EquipmentBalance balance = EquipmentBalance.Default; float oldArmor = balance.chainmailArmor, oldCrit = balance.daggerCritChance[0];
            try
            {
                balance.chainmailArmor = .7f; balance.daggerCritChance[0] = .55f; restored = InventoryStore.Deserialize(json);
                foreach (InventoryItem item in profile.Items) Check(JsonUtility.ToJson(restored.FindItem(item.Id)) == JsonUtility.ToJson(item), "Balance edit altered owned item");
            }
            finally { balance.chainmailArmor = oldArmor; balance.daggerCritChance[0] = oldCrit; }
        }

        private sealed class ScriptedRandom : System.Random
        {
            private readonly Queue<double> doubles; private readonly Queue<int> integers;
            public int DoubleCalls, IntegerCalls, ThrowOnIntegerCall;
            public ScriptedRandom(double[] rolls, params int[] picks) { doubles = new Queue<double>(rolls); integers = new Queue<int>(picks); }
            public override double NextDouble()
            { DoubleCalls++; if (doubles.Count == 0) throw new InvalidOperationException("Unexpected probability roll."); return doubles.Dequeue(); }
            public override int Next(int minimum, int maximum)
            {
                IntegerCalls++; if (IntegerCalls == ThrowOnIntegerCall) throw new InvalidOperationException("Interrupted award fixture.");
                int value = integers.Count == 0 ? minimum : integers.Dequeue();
                if (value < minimum || value >= maximum) throw new InvalidOperationException("Fixture pick outside requested range."); return value;
            }
        }

        private static void CheckClaims()
        {
            LootDefinition d = BalanceData.Current.loot;
            Check(d.ruleConfirmed && d.policy == "SingleItem" && d.dropsOnVictoryOnly && d.artifactDropsEnabled, "Victory rule missing");
            Near(d.artifactChance, .001f, "Artifact chance"); Near(d.mysticalChance, .025f, "Mystical chance"); Near(d.rareChance, .1f, "Rare chance"); Near(d.commonChance, .2f, "Common chance");
            Near(EquipmentBalance.Default.ordinaryWeaponChance, .5f, "Weapon/armor split");
            double a = d.artifactChance, m = a + d.mysticalChance, r = m + d.rareChance, c = r + d.commonChance;
            Near((float)(1 - c), .674f, "No-drop probability");
            double[] rolls = { 0, a - 1e-9, a, m - 1e-9, m, r - 1e-9, r, c - 1e-9, c, .999 };
            int[] expected = { -2, -2, 2, 2, 1, 1, 0, 0, -1, -1 };
            for (int i = 0; i < rolls.Length; i++)
            {
                var claim = new MapLootClaim(); var profile = new PlayerProfile();
                List<InventoryItem> awards = claim.TryClaim(profile, true, new ScriptedRandom(new[] { rolls[i], .1 }));
                Check(claim.Claimed && awards.Count == (expected[i] == -1 ? 0 : 1) && profile.Items.Count == awards.Count, "Exclusive boundary count");
                if (expected[i] == -2) Check(awards[0].Category == ItemCategory.Artifact, "Artifact boundary");
                else if (expected[i] >= 0) Check((int)awards[0].Rarity == expected[i] && awards[0].Category == ItemCategory.Weapon, "Rarity boundary");
                var retry = new ScriptedRandom(new double[0]); int count = profile.Items.Count;
                Check(claim.TryClaim(profile, true, retry).Count == 0 && profile.Items.Count == count && retry.DoubleCalls == 0, "Claim rerolled");
            }
            for (int type = 0; type < 3; type++)
            {
                InventoryItem weapon = new MapLootClaim().TryClaim(new PlayerProfile(), true, new ScriptedRandom(new[] { r, .499999 }, type))[0];
                Check(weapon.Category == ItemCategory.Weapon && (int)weapon.Weapon == type, "Weapon branch unreachable");
                InventoryItem artifact = new MapLootClaim().TryClaim(new PlayerProfile(), true, new ScriptedRandom(new[] { 0d }, type + 1))[0];
                Check((int)artifact.Artifact == type + 1, "Artifact branch unreachable");
                for (int slot = 0; slot < 6; slot++)
                {
                    InventoryItem armor = new MapLootClaim().TryClaim(new PlayerProfile(), true, new ScriptedRandom(new[] { r, .5 }, type, slot))[0];
                    Check(armor.Category == ItemCategory.Armor && (int)armor.Armor == type && (int)armor.Slot == slot, "Armor branch or category boundary");
                }
            }
            var defeated = new MapLootClaim(); var empty = new PlayerProfile(); var noRandom = new ScriptedRandom(new double[0]);
            Check(defeated.TryClaim(empty, false, noRandom).Count == 0 && !defeated.Claimed && noRandom.DoubleCalls == 0 && empty.Items.Count == 0, "Defeat consumed claim");
            var interrupted = new MapLootClaim(); bool threw = false;
            try { interrupted.TryClaim(empty, true, new ScriptedRandom(new[] { r, .1 }, 0) { ThrowOnIntegerCall = 2 }); }
            catch (InvalidOperationException) { threw = true; }
            Check(threw && !interrupted.Claimed && empty.Items.Count == 0, "Interrupted generation partially mutated inventory");
            bool invalidPolicy = false, invalidChance = false;
            try { new MapLootClaim().TryClaim(empty, true, noRandom, (LootRollPolicy)99, .5f); } catch (ArgumentOutOfRangeException) { invalidPolicy = true; }
            try { new MapLootClaim().TryClaim(empty, true, noRandom, LootRollPolicy.SingleItem, float.NaN); } catch (ArgumentOutOfRangeException) { invalidChance = true; }
            Check(invalidPolicy && invalidChance, "Invalid legacy API parameters accepted");
        }
    }
}
