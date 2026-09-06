using UnityEngine;

namespace SkeletonDefender
{
    public sealed partial class SkeletonGame
    {
        private void SelectBattleHero(bool clone = false)
        {
            if (clone && (game.Clone == null || !game.Clone.Alive)) return;
            heroSelected = true; selected = -1; cloneSelected = clone; CancelSkillAim();
        }
        private AudioClip CreateBossGong()
        {
            const int rate = 22050;
            float seconds = BalanceData.Current.spawnSchedule.bossGongSeconds;
            float[] data = new float[Mathf.RoundToInt(rate * seconds)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate;
                float wave = Mathf.Sin(2 * Mathf.PI * 110 * t) * .48f
                    + Mathf.Sin(2 * Mathf.PI * 173.4f * t + .2f * Mathf.Sin(t * 13)) * .24f
                    + Mathf.Sin(2 * Mathf.PI * 257.3f * t) * .15f
                    + Mathf.Sin(2 * Mathf.PI * 389.8f * t) * .1f;
                data[i] = wave * Mathf.Min(1, t * 200) * Mathf.Exp(-t * .8f) * Mathf.Min(1, (seconds - t) * 5);
            }
            AudioClip clip = AudioClip.Create("Mini-boss gong 5 seconds", data.Length, 1, rate, false);
            clip.SetData(data, 0); return clip;
        }
        private void DrawBattleHero()
        {
            DrawActor(game.Hero, heroSelected && !cloneSelected);
            if (game.Clone != null) DrawActor(game.Clone, heroSelected && cloneSelected);
        }
        private void DrawActor(HeroCombatState actor, bool selectedActor)
        {
            if (!actor.Alive) return;
            Vector2 p = actor.Position;
            Color accent = actor.IsClone ? ManaBlue : Gold;
            if (selectedActor)
            {
                Texture(new Rect(p.x - 28, p.y - 14, 56, 36), pad, accent);
                if (Vector2.Distance(actor.Destination, p) > 6)
                    Outline(new Rect(actor.Destination.x - 9, actor.Destination.y - 6, 18, 12), accent, 2);
            }
            if (!actor.IsClone && game.RageRemaining > 0)
            {
                float pulse = 54 + 4 * Mathf.Sin(game.Elapsed * 7);
                Texture(new Rect(p.x - pulse / 2, p.y - 20, pulse, 40), range, PixelArt.C("ef9c49"));
                Txt("ЯРОСТЬ", p.x - 44, p.y - 101, 88, 18, 11, Gold, FontStyle.Bold, TextAnchor.MiddleCenter);
            }
            bool knockedDown = actor.KnockdownRemaining > 0;
            Matrix4x4 before = GUI.matrix;
            bool arrowPose = actor.Kind == HeroKind.Achilles && actor.LastCastName == BalanceData.Current.heroSystems.Skill(actor.Kind, 1).name;
            bool spearPose = actor.Kind == HeroKind.Achilles && actor.LastCastName == BalanceData.Current.heroSystems.Skill(actor.Kind, 0).name;
            float rotation = knockedDown ? 75 : actor.CastPoseRemaining > 0 && arrowPose ? 28 : 0;
            if (rotation != 0) GUIUtility.RotateAroundPivot(rotation, p + new Vector2(0, -20));
            Texture(new Rect(p.x - 28, p.y - 61, 56, 73), heroArt[(int)actor.Kind],
                actor.Flash > 0 ? new Color(1, .8f, .6f) : actor.IsClone ? new Color(.64f, .85f, 1, .86f) : Color.white);
            GUI.matrix = before;
            if (actor.CastPoseRemaining > 0 && spearPose)
                DrawSpear(p + new Vector2(21, -96), Vector2.down, 50, Gold);
            Fill(new Rect(p.x - 28, p.y - 76, 56, 7), Ink);
            Fill(new Rect(p.x - 27, p.y - 75, 54 * Mathf.Clamp01(actor.Hp / actor.MaxHp), 5), actor.IsRegenerating ? Green : accent);
            Fill(new Rect(p.x - 28, p.y - 66, 56, 4), Ink);
            Fill(new Rect(p.x - 27, p.y - 65, 54 * Mathf.Clamp01(actor.Mana / actor.MaxMana), 2), ManaBlue);
            if (actor.IsClone) Txt("КОПИЯ " + ClockText(game.CloneRemaining), p.x - 66, p.y - 95, 132, 18, 10, ManaBlue, FontStyle.Bold, TextAnchor.MiddleCenter);
            if (actor.IsRegenerating)
            {
                float rise = game.Elapsed % 1f * 16;
                Fill(new Rect(p.x + 29, p.y - 44 - rise, 13, 4), Green);
                Fill(new Rect(p.x + 34, p.y - 49 - rise, 4, 14), Green);
            }
            if (knockedDown) Txt("НОКДАУН " + Mathf.CeilToInt(actor.KnockdownRemaining) + "с", p.x - 67, p.y - 115, 134, 23, 12, Red, FontStyle.Bold, TextAnchor.MiddleCenter);
        }
        private void DrawHeroCard()
        {
            DrawActorCard(game.Hero, new Rect(24, 788, 198, 88), false);
            Rect cloneCard = new Rect(230, 788, 198, 88);
            if (game.Clone != null) DrawActorCard(game.Clone, cloneCard, true);
            else
            {
                Box(cloneCard);
                Txt("3 · КОПИЯ", cloneCard.x + 10, 802, 178, 22, 13, Dim, FontStyle.Bold);
                Txt("Зеркало Афины\nсоздаёт копию героя", cloneCard.x + 10, 832, 178, 36, 12, Dim);
            }
        }
        private void DrawActorCard(HeroCombatState actor, Rect r, bool clone)
        {
            Box(r);
            bool chosen = cloneSelected == clone;
            if (chosen) Outline(r, clone ? ManaBlue : Gold, 2);
            Texture(new Rect(r.x + 7, r.y + 13, 36, 47), heroArt[(int)actor.Kind], actor.Alive ? clone ? ManaBlue : Color.white : Dim);
            Txt((clone ? "3 · КОПИЯ" : "2 · " + HeroName(actor.Kind)), r.x + 50, r.y + 7, 143, 19, 12, clone ? ManaBlue : Gold, FontStyle.Bold);
            Txt(actor.Alive ? actor.Hp.ToString("0") + "/" + actor.MaxHp.ToString("0") + " HP" : clone ? "Копия погибла" : "Возрождение " + Mathf.CeilToInt(actor.RespawnRemaining) + "с", r.x + 50, r.y + 29, 143, 20, 12, actor.Alive ? Text : Red);
            Fill(new Rect(r.x + 50, r.y + 52, 137, 5), Ink);
            Fill(new Rect(r.x + 50, r.y + 52, 137 * Mathf.Clamp01(actor.Mana / actor.MaxMana), 5), ManaBlue);
            Txt(actor.Mana.ToString("0") + "/" + actor.MaxMana.ToString("0") + " MP" + (clone ? " · " + ClockText(game.CloneRemaining) : ""), r.x + 50, r.y + 62, 143, 18, 10, ManaBlue);
            if (!paused && !Finished && (!clone || actor.Alive) && GUI.Button(r, GUIContent.none, GUIStyle.none)) { SelectBattleHero(clone); Sound(clickSound); }
        }
        private void DrawHeroSidebar()
        {
            HeroCombatState hero = SelectedActor;
            var definition = BalanceData.Current.Hero((int)hero.Kind);
            Txt(cloneSelected ? "УПРАВЛЕНИЕ КОПИЕЙ · 3" : "УПРАВЛЕНИЕ ГЕРОЕМ · 2", 1125, 148, 270, 25, 12, cloneSelected ? ManaBlue : Gold, FontStyle.Bold);
            Texture(new Rect(1133, 187, 57, 74), heroArt[(int)hero.Kind], hero.IsClone ? ManaBlue : Color.white);
            Txt(HeroName(hero.Kind), 1203, 192, 187, 31, 22, Gold, FontStyle.Bold);
            Txt(hero.IsClone ? "Осталось " + ClockText(game.CloneRemaining) : game.RageRemaining > 0 ? "Ярость " + ClockText(game.RageRemaining) : "ЛКМ по карте — движение", 1203, 230, 187, 43, 12, Dim);
            Stat("Здоровье", hero.Hp.ToString("0") + "/" + hero.MaxHp.ToString("0"), 283);
            Stat("Мана", hero.Mana.ToString("0") + "/" + hero.MaxMana.ToString("0"), 316);
            Stat("Урон", hero.Damage.ToString("0.###"), 349);
            Stat("Атак / сек.", (1f / hero.AttackInterval).ToString("0.###"), 382);
            Txt("Броня " + hero.PhysicalArmor.ToString("0.#%") + " · Уклонение " + hero.DodgeChance.ToString("0.#%"), 1127, 420, 270, 25, 12, Dim);
            Txt("Мана +" + hero.ManaRegen.ToString("0.###") + "/с · Ход " + hero.WalkSpeed.ToString("0.##"), 1127, 449, 270, 24, 12, ManaBlue);
            string condition = !hero.Alive ? "Возрождение через " + Mathf.CeilToInt(hero.RespawnRemaining) + "с" : hero.KnockdownRemaining > 0 ? "НОКДАУН " + Mathf.CeilToInt(hero.KnockdownRemaining) + "с" : hero.IsRegenerating ? "Восстановление HP +" + (hero.MaxHp * BalanceData.Current.heroRules.regenerationMaxHpFractionPerSecond).ToString("0.#") + "/с" : "Для лечения: " + BalanceData.Current.heroRules.regenerationIdleSeconds.ToString("0.#") + "с покоя";
            Txt(condition, 1127, 478, 270, 23, 12, hero.KnockdownRemaining > 0 ? Red : Dim);
            Fill(new Rect(1125, 510, 270, 1), Edge);
            Txt("АВТОМАТИЧЕСКИЕ СПОСОБНОСТИ", 1127, 527, 270, 23, 11, Gold, FontStyle.Bold);
            if (hero.Kind == HeroKind.Circe)
            {
                SkillLine("ХЕКС", definition.skills[0].chance.ToString("0.#%") + " при атаке · не действует на босса", hero.FrogCooldown, 562);
                SkillLine("СОЛНЕЧНЫЙ ЛУЧ", "Не действует на босса", hero.SunCooldown, 622);
                Txt("ОГНЕННЫЙ ЗАЛП · " + definition.skills[2].chance.ToString("0.#%") + "\n" + definition.skills[2].projectiles + " снаряда по " + definition.skills[2].damagePerProjectile.ToString("0.#"), 1127, 688, 269, 53, 13, Dim);
            }
            else
            {
                SkillLine("ОБЕЗГЛАВЛИВАНИЕ", "Не действует на босса", hero.DecapitateCooldown, 562);
                Txt("БРОСОК НОЖА · " + definition.skills[2].chance.ToString("0.#%") + "\nПри обычном ударе · кроме босса", 1127, 626, 269, 49, 13, Dim);
                Txt("Q / E — навыки выбранного героя\nR — артефакт основного героя", 1127, 692, 269, 48, 12, ManaBlue);
            }
        }
        private void SkillLine(string title, string description, float cooldown, float y)
        {
            Txt(title, 1127, y, 220, 24, 12, Text, FontStyle.Bold);
            Txt(cooldown > 0 ? Mathf.CeilToInt(cooldown) + "с" : "ГОТОВ", 1343, y, 50, 24, 11, Gold, align: TextAnchor.UpperRight);
            Txt(description, 1127, y + 25, 270, 26, 12, Dim);
        }
        private void DrawEnemyEffects()
        {
            foreach (var projectile in game.EnemyProjectiles)
            {
                Vector2 p = projectile.Position + new Vector2(0, -18);
                Fill(new Rect(p.x - 3, p.y - 2, 8, 4), Gold);
            }
            foreach (var effect in game.EnemyEffects)
            {
                Vector2 p = effect.Position;
                Color c = effect.Kind == "smoke" ? Dim : effect.Kind == "lightning" ? PixelArt.C("bd9bef") : Gold;
                if (effect.Kind == "lightning")
                {
                    Vector2 target = effect.Target + new Vector2(0, -20);
                    DrawLine(target + new Vector2(12, -140), target + new Vector2(-12, -75), 4, c);
                    DrawLine(target + new Vector2(-12, -75), target + new Vector2(8, -70), 4, c);
                    DrawLine(target + new Vector2(8, -70), target, 4, c);
                }
                else if (effect.Kind == "smoke")
                    Texture(new Rect(p.x - 12, p.y - 65, 24, 24), pad, Dim);
                else if (effect.Kind == "summon" || effect.Kind == "rebirth")
                    Outline(new Rect(p.x - 25, p.y - 20, 50, 36), PixelArt.C("ac95c6"), 2);
                else if (effect.Kind == "dodge")
                    Txt("УКЛОНЕНИЕ", p.x - 60, p.y - 78, 120, 23, 11, Dim, FontStyle.Bold, TextAnchor.MiddleCenter);
                else if (effect.Kind == "execution")
                {
                    Vector2 target = effect.Target + new Vector2(0, -24);
                    DrawLine(target + new Vector2(-18, -18), target + new Vector2(18, 18), 5, Red);
                    DrawLine(target + new Vector2(-18, 18), target + new Vector2(18, -18), 5, Red);
                }
            }
        }
        private void DrawEnemyTooltip()
        {
            if (paused || Finished || !mapRect.Contains(mouse)) return;
            Vector2 local = mouse - mapRect.position;
            Enemy hovered = null;
            float nearest = 28;
            foreach (Enemy enemy in game.Enemies)
            {
                float distance = Vector2.Distance(local, game.Position(enemy.Distance) + new Vector2(0, -18));
                if (!enemy.Dead && distance < nearest) { nearest = distance; hovered = enemy; }
            }
            if (hovered == null) return;
            bool summoner = hovered.Skeleton == SkeletonKind.Tutankhamun;
            float height = summoner ? 222 : 128;
            float x = Mathf.Clamp(local.x + 22, 8, 766);
            float y = Mathf.Clamp(local.y - height - 10, summoner ? 88 : 48, mapRect.height - height - 8);
            Box(new Rect(x, y, 280, height));
            Txt(hovered.IsSummoned ? hovered.Skeleton == SkeletonKind.Normal ? "Мини-мумия" : "Скелет без саркофага" : hovered.Variant.name, x + 12, y + 9, 256, 29, 18, Gold, FontStyle.Bold);
            Txt("HP  " + hovered.Hp.ToString("0.#") + " / " + hovered.MaxHp.ToString("0.#"), x + 12, y + 40, 256, 23, 15, Text);
            string attack = hovered.Variant.magicalImmune ? "Иммунитет к магии · Удар: " + hovered.AttackDamage.ToString("0.#") : hovered.Skeleton == SkeletonKind.Boxer ? "Удар: 20% макс. HP · Нокдаун 10с" : hovered.Skeleton == SkeletonKind.Pirate ? "Удар: 5 · Пистолет: 40" : "Удар: " + hovered.AttackDamage.ToString("0.#");
            Txt(attack, x + 12, y + 68, 256, 23, 13, Text);
            Txt("Награда: " + hovered.Reward + " золота" + (hovered.Skeleton == SkeletonKind.Knight ? " · Физ. защита 50%" : ""), x + 12, y + 100, 256, 21, 13, Dim);
            if (summoner)
            {
                var variant = hovered.Variant;
                int living = game.LivingSummons(hovered.Id);
                string totalLimit = variant.summonLimit > 0 ? variant.summonLimit.ToString() : "∞";
                string aliveLimit = variant.summonMaxAlive > 0 ? variant.summonMaxAlive.ToString() : "∞";
                float mummyHp = variant.summonedHp > 0 ? variant.summonedHp : BalanceData.Current.Skeleton((int)SkeletonKind.Normal).hp;
                Txt("Призвано: " + hovered.SummonsCreated + "/" + totalLimit + " · живых: " + living + "/" + aliveLimit,
                    x + 12, y + 130, 256, 22, 13, Text);
                Txt("Призыв каждые " + variant.summonInterval.ToString("0.#") + "с · Мумия " + mummyHp.ToString("0.#") + " HP",
                    x + 12, y + 157, 256, 22, 12, Dim);
                bool exhausted = variant.summonLimit > 0 && hovered.SummonsCreated >= variant.summonLimit;
                bool atCapacity = variant.summonMaxAlive > 0 && living >= variant.summonMaxAlive;
                string next = Mathf.CeilToInt(hovered.SpecialCooldown) + "с";
                string status = exhausted ? "Больше не призывает" : hovered.IsFrog ? "Призыв приостановлен: хекс"
                    : atCapacity ? "Лимит живых · повтор через " + next
                    : variant.summonLimit <= 0 ? "Без лимита · до призыва " + next : "До призыва: " + next;
                Txt(status, x + 12, y + 185, 256, 23, 12, exhausted ? Dim : Gold);
            }
        }
    }
}
