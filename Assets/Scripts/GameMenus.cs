using System.Collections.Generic;
using UnityEngine;

namespace SkeletonDefender
{
    public sealed partial class SkeletonGame
    {
        private PlayerProfile profile;
        private int selectedMap = 1, inventoryTab, inventorySlotFilter = -1;
        private bool compatibleItemsOnly;
        private Vector2 inventoryScroll;
        private string inventoryMessage = "";
        private InventoryItem hoveredItem;
        private bool hoveredFromEquipment;
        private InventoryItem pendingEquipItem;
        private readonly GameModel[] heroPreviews = new GameModel[2];
        private readonly Texture2D[] heroArt = new Texture2D[2];
        private static readonly string[] EquipmentNames = { "Шлем", "Броня", "Перчатки", "Пояс", "Штаны", "Сапоги", "Оружие", "Артефакт" };

        private static string HeroName(HeroKind kind) => kind == HeroKind.Circe ? "ЦИРЦЕЯ" : "АХИЛЛ";
        private static string HeroDescription(HeroKind kind) => kind == HeroKind.Circe
            ? "Дочь Гелиоса, богиня с острова Ээя.\nМагические снаряды и сила солнца."
            : "Сын Фетиды, герой Троянской войны.\nМеч, уклонение и смертельные приёмы.";
        private static Color RarityColor(ItemRarity rarity) => rarity == ItemRarity.Common ? Text : rarity == ItemRarity.Rare ? PixelArt.C("85bddb") : PixelArt.C("c69be8");
        private static Color ItemColor(InventoryItem item) => item.Category == ItemCategory.Artifact ? Gold : RarityColor(item.Rarity);
        private static string RarityLabel(ItemRarity rarity) => InventoryItem.RarityName(rarity).ToUpperInvariant();
        private static string WeaponLabel(WeaponKind weapon) => InventoryItem.WeaponName(weapon);
        private static float BaseHeroHp(HeroKind hero) => BalanceData.Current.Hero((int)hero).hp;
        private static float BaseHeroDamage(HeroKind hero) => BalanceData.Current.Hero((int)hero).damage;
        private HeroCombatState PreviewHero(HeroKind kind)
        {
            if (heroPreviews[(int)kind] == null) heroPreviews[(int)kind] = new GameModel(1, kind, profile.EquippedStats(kind), 1);
            return heroPreviews[(int)kind].Hero;
        }
        private void SaveProfile()
        {
            heroPreviews[0] = heroPreviews[1] = null;
            if (!InventoryStore.Save(profile)) inventoryMessage = "Не удалось сохранить снаряжение. Не закрывай игру.";
        }
        private void MenuPage(string title, string subtitle)
        {
            DrawMenuBackground(); Fill(new Rect(0, 0, 1440, 900), new Color(.025f, .05f, .065f, .87f));
            Txt(title, 60, 35, 1050, 55, 34, Text, FontStyle.Bold);
            Txt(subtitle, 62, 94, 1130, 32, 17, Dim);
            if (Button(new Rect(1170, 45, 210, 46), "В ГЛАВНОЕ МЕНЮ", size: 14)) screen = ScreenMode.Menu;
        }
        private void SelectHero(HeroKind hero)
        { profile.SelectedHero = hero; inventoryMessage = ""; inventoryScroll = Vector2.zero; SaveProfile(); }

        private void DrawSelection()
        {
            MenuPage("ВЫБОР ГЕРОЯ", "Характеристики учитывают надетое снаряжение. Управление навыками в бою: Q / E.");
            for (int i = 0; i < 3; i++)
            {
                float x = 60 + i * 448; Rect card = new Rect(x, 154, 420, 484); Box(card);
                if (i == 2)
                {
                    Texture(new Rect(x + 135, 198, 150, 195), heroArt[1], new Color(.16f, .21f, .23f));
                    Txt("СКОРО", x + 30, 442, 360, 46, 28, Dim, FontStyle.Bold, TextAnchor.MiddleCenter);
                    Txt("COMING SOON", x + 30, 494, 360, 30, 15, Dim, align: TextAnchor.MiddleCenter);
                    Button(new Rect(x + 28, 573, 364, 43), "НЕДОСТУПНО", enabled: false, size: 16); continue;
                }
                HeroKind kind = (HeroKind)i; HeroCombatState hero = PreviewHero(kind); bool chosen = kind == profile.SelectedHero;
                if (chosen) Outline(card, Gold, 2);
                Texture(new Rect(x + 163, 176, 92, 120), heroArt[i]);
                Txt(HeroName(kind), x + 22, 303, 376, 38, 27, chosen ? Gold : Text, FontStyle.Bold, TextAnchor.MiddleCenter);
                Txt(HeroDescription(kind), x + 28, 347, 364, 51, 15, Dim, align: TextAnchor.MiddleCenter);
                Txt("HP " + hero.MaxHp.ToString("0") + "   УРОН " + hero.Damage.ToString("0.###"), x + 25, 407, 370, 29, 20, Text, FontStyle.Bold, TextAnchor.MiddleCenter);
                Txt("Броня " + hero.PhysicalArmor.ToString("0.#%") + "  ·  Уклонение " + hero.DodgeChance.ToString("0.#%"), x + 25, 444, 370, 25, 15, Dim, align: TextAnchor.MiddleCenter);
                Txt("Мана " + hero.MaxMana.ToString("0.#") + "  ·  +" + hero.ManaRegen.ToString("0.###") + " / сек.", x + 25, 476, 370, 25, 15, PixelArt.C("91c5e6"), align: TextAnchor.MiddleCenter);
                Txt("Атака " + (1f / hero.AttackInterval).ToString("0.###") + " / сек.  ·  Ход " + hero.WalkSpeed.ToString("0.##"), x + 25, 508, 370, 25, 15, Dim, align: TextAnchor.MiddleCenter);
                if (Button(new Rect(x + 28, 573, 364, 43), chosen ? "ВЫБРАН" : "ВЫБРАТЬ", chosen, size: 17)) SelectHero(kind);
            }
            selectedMap = 1;
            Txt("УРОВЕНЬ 1  /  " + BalanceData.Current.plannedMapCount, 62, 663, 400, 28, 15, Gold, FontStyle.Bold);
            Button(new Rect(62, 704, 565, 57), BalanceData.Current.mapName, true, size: 23);
            Txt("20 волн · остальные уровни пока недоступны", 63, 778, 850, 29, 16, Dim);
            var timing = BalanceData.Current.spawnSchedule;
            Txt("Вызов после выхода " + timing.earlyWaveFraction.ToString("0%") + " врагов: от " + timing.earlyWaveBonus + " до 0 золота. Босс невосприимчив к магии.", 63, 815, 880, 28, 15, Dim);
            if (Button(new Rect(980, 722, 400, 73), "В БОЙ", true, size: 24)) NewGame();
        }

        private void DrawInventory()
        {
            bool oldEnabled = GUI.enabled;
            GUI.enabled = oldEnabled && pendingEquipItem == null;
            hoveredItem = null;
            hoveredFromEquipment = false;
            MenuPage("ИНВЕНТАРЬ", "Наведи курсор на название предмета, чтобы увидеть свойства. Кнопки справа — надеть или снять.");
            for (int i = 0; i < 2; i++)
                if (Button(new Rect(60 + i * 218, 145, 202, 48), HeroName((HeroKind)i), profile.SelectedHero == (HeroKind)i, size: 17)) SelectHero((HeroKind)i);
            DrawEquipmentPanel();
            string[] tabs = { "БРОНЯ", "ОРУЖИЕ", "АРТЕФАКТЫ" };
            for (int i = 0; i < 3; i++)
                if (Button(new Rect(530 + i * 291, 145, 270, 48), tabs[i], inventoryTab == i, size: 17))
                { inventoryTab = i; inventorySlotFilter = -1; inventoryScroll = Vector2.zero; inventoryMessage = ""; }
            Box(new Rect(530, 218, 850, 592));
            if (inventoryTab == 0)
            {
                for (int i = -1; i < 6; i++)
                    if (Button(new Rect(546 + (i + 1) * 117, 232, 108, 31), i < 0 ? "Все части" : EquipmentNames[i], inventorySlotFilter == i, size: 12))
                    { inventorySlotFilter = i; inventoryScroll = Vector2.zero; }
            }
            else Txt(inventoryTab == 1 ? "Мечи · Кинжалы · Посохи" : "Артефакты не имеют редкости. Один слот на героя.", 550, 238, 810, 25, 15, Dim);
            if (Button(new Rect(548, 277, 338, 31), compatibleItemsOnly ? "ФИЛЬТР: ДЛЯ ЭТОГО ГЕРОЯ" : "ФИЛЬТР: ВСЕ ГЕРОИ", compatibleItemsOnly, size: 12))
            { compatibleItemsOnly = !compatibleItemsOnly; inventoryScroll = Vector2.zero; }
            var items = new List<InventoryItem>();
            foreach (InventoryItem item in profile.Items)
                if ((int)item.Category == inventoryTab && (inventorySlotFilter < 0 || (int)item.Slot == inventorySlotFilter)
                    && (!compatibleItemsOnly || item.CanEquip(profile.SelectedHero))) items.Add(item);
            Txt(items.Count + " предметов", 1070, 282, 277, 24, 13, Dim, align: TextAnchor.UpperRight);
            Rect viewport = new Rect(546, 324, 818, 464);
            if (items.Count == 0)
                Txt("В ЭТОМ РАЗДЕЛЕ ПОКА ПУСТО\n\nНайденные предметы появятся здесь.", 600, 440, 720, 125, 20, Dim, align: TextAnchor.MiddleCenter);
            else
            {
                Vector2 savedMouse = mouse;
                inventoryScroll = GUI.BeginScrollView(viewport, inventoryScroll, new Rect(0, 0, 790, items.Count * 96));
                mouse = savedMouse - viewport.position + inventoryScroll;
                for (int i = 0; i < items.Count; i++) DrawInventoryRow(items[i], i * 96);
                GUI.EndScrollView(); mouse = savedMouse;
            }
            Txt(inventoryMessage.Length == 0 ? "Снять предмет можно кнопкой в сумке или нажатием на его слот слева." : inventoryMessage,
                534, 829, 840, 46, 15, inventoryMessage.Length == 0 ? Dim : Gold);
            GUI.enabled = oldEnabled;
            if (pendingEquipItem == null) DrawItemTooltip();
            else DrawEquipConfirmation();
        }
        private void DrawEquipmentPanel()
        {
            Box(new Rect(60, 218, 436, 592));
            HeroCombatState hero = PreviewHero(profile.SelectedHero);
            Texture(new Rect(81, 236, 66, 86), heroArt[(int)hero.Kind]);
            Txt(HeroName(hero.Kind), 165, 243, 305, 31, 23, Gold, FontStyle.Bold);
            Txt("HP " + hero.MaxHp.ToString("0") + "  ·  Урон " + hero.Damage.ToString("0.###"), 165, 282, 305, 27, 17, Text);
            Txt("Броня " + hero.PhysicalArmor.ToString("0.#%") + "   Уклонение " + hero.DodgeChance.ToString("0.#%"), 83, 332, 390, 25, 15, Dim);
            Txt("Мана " + hero.MaxMana.ToString("0.#") + "   Восстановление +" + hero.ManaRegen.ToString("0.###") + "/с", 83, 365, 390, 25, 15, PixelArt.C("91c5e6"));
            Txt("Атак/с " + (1f / hero.AttackInterval).ToString("0.###") + "   Передвижение " + hero.WalkSpeed.ToString("0.##"), 83, 398, 390, 25, 15, Dim);
            Txt("СНАРЯЖЕНИЕ · 8 СЛОТОВ", 83, 449, 390, 24, 13, Gold, FontStyle.Bold);
            for (int slot = 0; slot < 8; slot++)
            {
                Rect r = new Rect(78 + slot % 2 * 208, 484 + slot / 2 * 76, 198, 67);
                Fill(r, PixelArt.C("122126")); Outline(r, Edge);
                InventoryItem item = profile.EquippedItem(profile.SelectedHero, slot);
                Txt(EquipmentNames[slot], r.x + 9, r.y + 5, 180, 19, 12, Dim);
                Txt(item == null ? "Пусто" : item.Name, r.x + 9, r.y + 26, 180, 37, 13, item == null ? Dim : ItemColor(item));
                if (item == null) continue;
                if (r.Contains(mouse)) { hoveredItem = item; hoveredFromEquipment = true; }
                if (GUI.Button(r, GUIContent.none, GUIStyle.none))
                { profile.Unequip(profile.SelectedHero, slot); inventoryMessage = "Предмет снят."; SaveProfile(); Sound(clickSound); }
            }
        }
        private void DrawInventoryRow(InventoryItem item, float y)
        {
            Rect r = new Rect(0, y, 790, 86); Fill(r, PixelArt.C("122126")); Outline(r, Edge);
            Fill(new Rect(1, y + 1, 4, 84), ItemColor(item));
            bool compatible = item.CanEquip(profile.SelectedHero);
            bool equipped = profile.EquippedItem(profile.SelectedHero, (int)item.Slot)?.Id == item.Id;
            HeroKind other = profile.SelectedHero == HeroKind.Circe ? HeroKind.Achilles : HeroKind.Circe;
            bool onOther = profile.EquippedItem(other, (int)item.Slot)?.Id == item.Id;
            Txt(item.Name, 18, y + 10, 540, 29, 18, ItemColor(item), FontStyle.Bold);
            string detail = EquipmentNames[(int)item.Slot] + (item.Category == ItemCategory.Weapon ? "  ·  Урон +" + item.DamageBonus.ToString("0.###") : "");
            if (profile.SelectedHero == HeroKind.Achilles && item.Category == ItemCategory.Weapon && item.Weapon == WeaponKind.Staff)
                detail = "Расход маны −" + item.ManaCostReduction.ToString("0.#%") + " · без бонуса урона";
            Txt(detail, 18, y + 45, 540, 24, 14, Dim);
            string owner = onOther ? other == HeroKind.Circe ? "НА ЦИРЦЕЕ" : "НА АХИЛЛЕ" : equipped ? "СНЯТЬ" : compatible ? "НАДЕТЬ" : "ДРУГОЙ ГЕРОЙ";
            if (Button(new Rect(575, y + 20, 198, 45), owner, equipped, (compatible || equipped) && !onOther, 12))
            {
                if (equipped)
                {
                    if (profile.Unequip(profile.SelectedHero, (int)item.Slot))
                    { inventoryMessage = "Предмет снят."; SaveProfile(); }
                    else inventoryMessage = "Не удалось снять предмет.";
                }
                else if (item.RequiresConfirmation(profile.SelectedHero)) pendingEquipItem = item;
                else EquipFromBag(item);
            }
            // Only text opens the description. The action button stays unobstructed and never opens it.
            Rect itemText = new Rect(18, y + 8, 540, 66);
            if (itemText.Contains(mouse) && mouse.y >= inventoryScroll.y && mouse.y < inventoryScroll.y + 464)
            { hoveredItem = item; hoveredFromEquipment = false; }
        }
        private void DrawItemTooltip()
        {
            if (hoveredItem == null) return;
            // Show details in the opposite column; bag action buttons begin at x=1121.
            float x = hoveredFromEquipment ? 546 : 72, y = hoveredFromEquipment ? 324 : 236;
            Rect r = new Rect(x, y, 414, 384); Fill(r, PixelArt.C("0e1c23")); Outline(r, ItemColor(hoveredItem), 2);
            Txt(hoveredItem.Name, x + 16, y + 13, 382, 51, 19, ItemColor(hoveredItem), FontStyle.Bold);
            string description = hoveredItem.Description;
            string warning = hoveredItem.EquipWarning(profile.SelectedHero);
            if (!string.IsNullOrEmpty(warning)) description += "\n" + warning;
            Txt(description, x + 16, y + 70, 382, 272, 14, Text);
            string eligibility = hoveredItem.CanEquip(HeroKind.Circe) && hoveredItem.CanEquip(HeroKind.Achilles) ? "Обоим героям" : hoveredItem.CanEquip(HeroKind.Circe) ? "Для Цирцеи" : "Для Ахилла";
            Txt(eligibility + " · " + EquipmentNames[(int)hoveredItem.Slot], x + 16, y + 351, 382, 22, 13, Dim);
        }
        private void EquipFromBag(InventoryItem item)
        {
            inventoryMessage = profile.Equip(profile.SelectedHero, (int)item.Slot, item.Id) ? "Предмет надет." : "Не удалось надеть предмет.";
            SaveProfile();
        }
        private void DrawEquipConfirmation()
        {
            Overlay(); Box(new Rect(395, 166, 650, 570));
            Txt("СНАРЯЖЕНИЕ СО ШТРАФОМ", 425, 193, 590, 44, 25, Gold, FontStyle.Bold, TextAnchor.MiddleCenter);
            Txt(pendingEquipItem.Name, 428, 254, 584, 48, 21, ItemColor(pendingEquipItem), FontStyle.Bold, TextAnchor.MiddleCenter);
            Txt(profile.PreviewEquipWarning(profile.SelectedHero, pendingEquipItem.Id), 438, 318, 564, 225, 18, Text);
            Txt("Предмет будет надет только после подтверждения.", 438, 561, 564, 35, 15, Dim, align: TextAnchor.MiddleCenter);
            if (Button(new Rect(422, 643, 303, 55), "НАДЕТЬ ВСЁ РАВНО", true, size: 16))
            { InventoryItem item = pendingEquipItem; pendingEquipItem = null; EquipFromBag(item); }
            if (Button(new Rect(746, 643, 271, 55), "ОТМЕНА", size: 16)) pendingEquipItem = null;
        }
    }
}
