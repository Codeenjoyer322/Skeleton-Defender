using UnityEngine;

namespace SkeletonDefender
{
    public sealed partial class SkeletonGame
    {
        private bool cloneSelected, aimingClone;
        private int aimingSkill = -1;
        private Texture2D deerArt;
        private HeroCombatState SelectedActor => game.GetControlledHero(cloneSelected) ?? game.Hero;
        private static readonly Color ManaBlue = PixelArt.C("79bfe5");
        private static string ManualSkillName(HeroKind hero, int index) => hero == HeroKind.Circe
            ? index == 0 ? "ОЛЕНИ ЦИРЦЕИ" : "ГРОЗА"
            : index == 0 ? "БОЖЕСТВЕННОЕ КОПЬЁ" : "СТРЕЛА ИЗ ПЯТКИ";
        private void CancelSkillAim() { aimingSkill = -1; }
        private void RequestManualSkill(int index)
        {
            if (paused || Finished || !game.CanCastSkill(index, cloneSelected)) return;
            if (game.RequiresSkillTarget(index, cloneSelected))
            {
                aimingSkill = index; aimingClone = cloneSelected;
                Announce("Укажи точку броска · ESC / правая кнопка — отмена", 5);
            }
            else if (game.TryCastSkill(index, null, cloneSelected))
            { CancelSkillAim(); Sound(buildSound); }
        }
        private void HandleManualKeys()
        {
            if (cloneSelected && (game.Clone == null || !game.Clone.Alive)) { cloneSelected = false; CancelSkillAim(); }
            if (aimingSkill >= 0 && (Input.GetMouseButtonDown(1) || !game.CanCastSkill(aimingSkill, aimingClone))) CancelSkillAim();
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) SelectBattleHero(true);
            if (Input.GetKeyDown(KeyCode.Q)) RequestManualSkill(0);
            if (Input.GetKeyDown(KeyCode.E)) RequestManualSkill(1);
            if (Input.GetKeyDown(KeyCode.R) && !cloneSelected && game.CanUseArtifact && game.TryUseArtifact())
            { CancelSkillAim(); Sound(buildSound); }
        }
        private bool HandleAimClick()
        {
            if (aimingSkill < 0 || Event.current.type != EventType.MouseDown) return false;
            if (Event.current.button == 1) { CancelSkillAim(); Event.current.Use(); return true; }
            if (Event.current.button != 0 || !mapRect.Contains(mouse)) return false;
            if (SkillBarContains(mouse)) { Event.current.Use(); return true; }
            Vector2 point = mouse - mapRect.position;
            if (game.TryCastSkill(aimingSkill, point, aimingClone)) { CancelSkillAim(); Sound(buildSound); }
            Event.current.Use(); return true;
        }
        private bool SkillBarContains(Vector2 p) => new Rect(438, 136, 630, 66).Contains(p);
        private void DrawSkillsBar()
        {
            HeroCombatState actor = SelectedActor;
            Txt((cloneSelected ? "НАВЫКИ КОПИИ" : "НАВЫКИ ГЕРОЯ") + " · " + actor.Mana.ToString("0") + "/" + actor.MaxMana.ToString("0") + " МАНЫ", 39, 181, 390, 22, 12, ManaBlue, FontStyle.Bold);
            int hovered = -1;
            for (int i = 0; i < 2; i++)
            {
                Rect r = new Rect(438 + i * 209, 136, 201, 66);
                bool enabled = !paused && !Finished && game.CanCastSkill(i, cloneSelected);
                bool aiming = aimingSkill == i && aimingClone == cloneSelected;
                Fill(r, aiming ? PixelArt.C("51452b") : PixelArt.C("10232c")); Outline(r, aiming ? Gold : enabled ? ManaBlue : Edge, aiming ? 2 : 1);
                Txt((i == 0 ? "Q  " : "E  ") + ManualSkillName(actor.Kind, i), r.x + 8, r.y + 8, 185, 22, 11, enabled || aiming ? Text : Dim, FontStyle.Bold);
                float cd = game.SkillCooldownRemaining(i, cloneSelected), cost = game.SkillManaCost(i, cloneSelected);
                string status = aiming ? "ВЫБЕРИ ТОЧКУ" : !actor.Alive ? "Герой пал" : actor.KnockdownRemaining > 0 ? "Нокдаун" : !game.HasStarted ? "Начни оборону" : cd > 0 ? "Откат " + ClockText(cd) : actor.Mana < cost ? "Маны не хватает" : "Готово";
                Txt(cost.ToString("0.###") + " маны · " + status, r.x + 8, r.y + 37, 185, 22, 11, aiming ? Gold : ManaBlue);
                if (enabled && GUI.Button(r, GUIContent.none, GUIStyle.none)) { RequestManualSkill(i); GUI.FocusControl(null); }
                if (r.Contains(mouse)) hovered = i;
            }
            DrawArtifactButton(new Rect(856, 136, 212, 66));
            if (aimingSkill >= 0)
                Txt("ПРИЦЕЛИВАНИЕ · ЛКМ — бросок · ESC / ПКМ — отмена", 448, 207, 610, 24, 12, Gold, FontStyle.Bold, TextAnchor.MiddleRight);
            else if (hovered >= 0 && !paused && !Finished) DrawSkillTooltip(actor, hovered);
        }
        private void DrawSkillTooltip(HeroCombatState actor, int index)
        {
            ManualSkillDefinition skill = BalanceData.Current.heroSystems.Skill(actor.Kind, index);
            float damage = skill.damage * (actor.Kind == HeroKind.Circe ? actor.MagicDamageMultiplier : actor.DamageMultiplier);
            string text;
            if (actor.Kind == HeroKind.Circe && index == 0)
                text = skill.projectiles + " оленя бегут " + skill.duration.ToString("0.#") + "с. Каждый наносит " + damage.ToString("0.###") + " магического урона один раз каждой цели. Замедление " + skill.slowFraction.ToString("0%") + " на " + skill.slowDuration.ToString("0.#") + "с.";
            else if (actor.Kind == HeroKind.Circe)
                text = "Гроза наносит " + damage.ToString("0.###") + " магического урона всем врагам на карте. Босс защищён от магии.";
            else if (index == 0)
                text = "Укажи точку: " + damage.ToString("0.###") + " физического урона в радиусе " + skill.areaRadius.ToString("0.#") + ". Отбрасывает на половину пути за последние 2с. Босса не отбрасывает. ESC / ПКМ — отмена без расхода маны.";
            else text = damage.ToString("0.###") + " физического урона каждому врагу, который был на карте при нажатии. Замедление " + skill.slowFraction.ToString("0%") + " на " + skill.slowDuration.ToString("0.#") + "с. Босса не замедляет.";
            float x = index == 0 ? 438 : 646;
            Box(new Rect(x, 213, 414, 194)); Outline(new Rect(x, 213, 414, 194), ManaBlue);
            Txt(ManualSkillName(actor.Kind, index), x + 14, 225, 386, 26, 16, ManaBlue, FontStyle.Bold);
            Txt(text, x + 14, 262, 386, 105, 14, Text);
            Txt("Мана " + game.SkillManaCost(index, cloneSelected).ToString("0.###") + " · Перезарядка " + ClockText(skill.cooldown), x + 14, 376, 386, 23, 13, Dim);
        }
        private void DrawArtifactButton(Rect r)
        {
            InventoryItem artifact = profile.EquippedItem(profile.SelectedHero, 7);
            bool passive = artifact != null && artifact.Artifact == ArtifactKind.AegisOfDawn;
            bool enabled = !paused && !Finished && !cloneSelected && game.CanUseArtifact;
            Fill(r, PixelArt.C("2b2927")); Outline(r, enabled || game.RageRemaining > 0 ? Gold : Edge);
            string name = artifact == null ? "АРТЕФАКТ НЕ НАДЕТ" : artifact.Name;
            Txt((passive ? "" : "R  ") + name, r.x + 8, r.y + 7, r.width - 16, 28, 11, Gold, FontStyle.Bold);
            string state = artifact == null ? "Надень в главном меню" : passive ? "Пассивно · возрождение 3с" : game.RageRemaining > 0 ? "ЯРОСТЬ " + ClockText(game.RageRemaining)
                : game.CloneRemaining > 0 ? "КОПИЯ " + ClockText(game.CloneRemaining) : cloneSelected ? "Использует только основной герой" : game.CanUseArtifact ? "Один раз за карту · 5 минут" : "Использован / недоступен";
            Txt(state, r.x + 8, r.y + 40, r.width - 16, 20, 10, Dim);
            if (enabled && GUI.Button(r, GUIContent.none, GUIStyle.none) && game.TryUseArtifact()) { CancelSkillAim(); Sound(buildSound); }
        }
        private void DrawAimMarker()
        {
            if (aimingSkill < 0 || !mapRect.Contains(mouse)) return;
            Vector2 p = mouse - mapRect.position;
            if (SkillBarContains(mouse)) return;
            HeroCombatState actor = game.GetControlledHero(aimingClone);
            if (actor == null) return;
            Vector2 hand = actor.Position + new Vector2(20, -48);
            DrawSpear(hand + Vector2.down * 40, Vector2.down, 55, Gold);
            DrawLine(actor.Position, p, 1, new Color(Gold.r, Gold.g, Gold.b, .55f));
            float radius = BalanceData.Current.heroSystems.Skill(actor.Kind, aimingSkill).areaRadius;
            Texture(new Rect(p.x - radius, p.y - radius, radius * 2, radius * 2), range, Gold);
            DrawLine(p + Vector2.left * 11, p + Vector2.right * 11, 2, Gold);
            DrawLine(p + Vector2.up * 11, p + Vector2.down * 11, 2, Gold);
        }
        private static void DrawArrow(Vector2 point, Vector2 direction, float length, Color color, float width = 2)
        {
            if (direction.sqrMagnitude < .001f) direction = Vector2.right;
            direction.Normalize(); Vector2 across = new Vector2(-direction.y, direction.x);
            DrawLine(point - direction * length, point, width, color);
            DrawLine(point - direction * 7 + across * 5, point, width, color);
            DrawLine(point - direction * 7 - across * 5, point, width, color);
            DrawLine(point - direction * (length - 4) + across * 4, point - direction * length, width, color);
        }
        private static void DrawSpear(Vector2 point, Vector2 direction, float length, Color color)
        {
            DrawArrow(point, direction, length, new Color(color.r, color.g, color.b, .22f), 11);
            DrawArrow(point, direction, length, color, 4);
            Fill(new Rect(point.x - 3, point.y - 3, 6, 6), PixelArt.C("fff2ad"));
        }
        private void DrawTravelingProjectiles()
        {
            foreach (TowerProjectile projectile in game.TowerProjectiles)
            {
                float total = Vector2.Distance(projectile.Start, projectile.Impact);
                float progress = total < .01f ? 1 : Mathf.Clamp01(Vector2.Distance(projectile.Start, projectile.Position) / total);
                Vector2 p = projectile.Position + new Vector2(0, Mathf.Lerp(-38, -14, progress));
                Vector2 direction = projectile.Impact - projectile.Start;
                if (projectile.Kind == TowerKind.Archer) DrawArrow(p, direction, 23, PixelArt.C("f2d9a0"), 3);
                else
                {
                    Color color = projectile.Kind == TowerKind.Ember ? PixelArt.C("ff9c42") : ManaBlue;
                    Vector2 tail = p - direction.normalized * 22;
                    DrawLine(tail, p, 9, new Color(color.r, color.g, color.b, .35f));
                    Fill(new Rect(p.x - 11, p.y - 11, 22, 22), new Color(color.r, color.g, color.b, .18f));
                    Fill(new Rect(p.x - 7, p.y - 7, 14, 14), color);
                    Fill(new Rect(p.x - 3, p.y - 4, 7, 7), PixelArt.C("fff0bc"));
                }
            }
        }
        private void DrawAbilityEffects()
        {
            var ascendingArrows = new System.Collections.Generic.HashSet<HeroCombatState>();
            foreach (AbilityEffect effect in game.AbilityEffects)
            {
                float t = effect.Lifetime <= 0 ? 1 : Mathf.Clamp01(effect.Age / effect.Lifetime);
                Vector2 start = effect.Start, end = effect.End;
                if (effect.Kind == "deer")
                {
                    Vector2 direction = end - start;
                    DrawLine(end - direction.normalized * 55, end, 12, new Color(.6f, .9f, .73f, .18f));
                    Matrix4x4 previous = GUI.matrix;
                    if (direction.x < 0) GUIUtility.ScaleAroundPivot(new Vector2(-1, 1), end);
                    Texture(new Rect(end.x - 29, end.y - 42 - Mathf.Abs(Mathf.Sin(effect.Age * 16)) * 7, 58, 48), deerArt, PixelArt.C("b5d6af")); GUI.matrix = previous;
                }
                else if (effect.Kind == "storm")
                {
                    if (t < .3f) Fill(new Rect(0, 0, 1056, 640), new Color(.68f, .8f, 1, .16f * (1 - t / .3f)));
                    for (int i = 0; i < 9; i++)
                    {
                        Vector2 p = new Vector2(100 + i * 103, 165 + i % 3 * 150);
                        DrawLightning(p, ManaBlue, 1 - t);
                    }
                    if (effect.Targets != null) foreach (Vector2 target in effect.Targets) DrawLightning(target, ManaBlue, .65f * (1 - t));
                }
                else if (effect.Kind == "spear")
                {
                    Vector2 hand = start + new Vector2(20, -55);
                    if (t < .25f) DrawSpear(hand + Vector2.down * 40, Vector2.down, 60, Gold);
                    else DrawSpear(Vector2.Lerp(hand, end, (t - .25f) / .75f), end - hand, 58, Gold);
                }
                else if (effect.Kind == "arrow_rain")
                {
                    Vector2 sky = start + new Vector2(0, -205);
                    if (t < .38f)
                    {
                        if (ascendingArrows.Add(effect.Source))
                        {
                            Vector2 heel = start + new Vector2(0, -4);
                            DrawArrow(Vector2.Lerp(heel, sky, t / .38f), Vector2.down, 31, ManaBlue, 4);
                        }
                    }
                    else
                    {
                        float progress = (t - .38f) / .62f;
                        if (progress < .25f) Outline(new Rect(sky.x - 18, sky.y - 18, 36, 36), ManaBlue, 3);
                        DrawArrow(Vector2.Lerp(sky, end + Vector2.down * 15, progress), end - sky, 22, PixelArt.C("bea98a"), 3);
                    }
                }
                else if (effect.Kind == "rage" || effect.Kind == "mirror")
                {
                    float radius = 24 + 50 * t;
                    Texture(new Rect(end.x - radius, end.y - radius, radius * 2, radius * 2), range,
                        effect.Kind == "rage" ? new Color(1, .65f, .25f, 1 - t) : new Color(.5f, .8f, 1, 1 - t));
                }
            }
        }
        private static void DrawLightning(Vector2 end, Color c, float opacity)
        {
            c.a = opacity;
            DrawLine(end + new Vector2(6, -155), end + new Vector2(-17, -76), 4, c);
            DrawLine(end + new Vector2(-17, -76), end + new Vector2(13, -88), 4, c);
            DrawLine(end + new Vector2(13, -88), end, 4, c);
        }
    }
}
