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
        private void CancelSkillAim()
        {
            aimingSkill = -1;
            if (banner == "Укажи точку броска · ESC / правая кнопка — отмена") bannerLife = 0;
        }
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
                Fill(r, aiming ? PixelArt.C("483653") : PixelArt.C("111b2e")); Outline(r, aiming ? Gold : enabled ? ManaBlue : Edge, aiming ? 2 : 1);
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
                text = skill.projectiles + " оленя бегут от защищаемого замка по всей дороге к входу " + skill.duration.ToString("0.#") + "с. Каждый наносит " + damage.ToString("0.###") + " магического урона один раз каждой цели. Замедление " + skill.slowFraction.ToString("0%") + " на " + skill.slowDuration.ToString("0.#") + "с.";
            else if (actor.Kind == HeroKind.Circe)
                text = "Гроза наносит суммарно " + damage.ToString("0.###") + " магического урона за 3с: четыре равных удара каждые 0,75с. Цели фиксируются при нажатии. Босс защищён от магии.";
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
            Fill(r, PixelArt.C("241c32")); Outline(r, enabled || game.RageRemaining > 0 ? Gold : Edge);
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
            Vector2 hand = ProjectileVisuals.SpearHand(actor.Position, p.x < actor.Position.x);
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
                Vector2 p = projectile.Visual.Initialized ? projectile.Visual.Tip : ProjectileVisuals.TowerSocket(projectile.Start);
                Vector2 direction = projectile.Visual.Initialized ? projectile.Visual.Direction
                    : ProjectileVisuals.Direction(projectile.Impact - p);
                if (projectile.Kind == TowerKind.Archer)
                {
                    if (!DrawEffectById(OrdinaryArrowFx, p, direction, projectile.Age, .5f, Color.white))
                        DrawArrow(p, direction, 23, PixelArt.C("f2d9a0"), 3);
                }
                else
                {
                    Color tint = projectile.Kind == TowerKind.Ember ? Color.white : ManaBlue;
                    if (DrawEffectById(CirceFireballFx, p, direction, projectile.Age, .55f, tint)) continue;
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
            foreach (AbilityEffect effect in game.AbilityEffects)
            {
                float t = effect.Lifetime <= 0 ? 1 : Mathf.Clamp01(effect.Age / effect.Lifetime);
                Vector2 start = effect.Start, end = effect.End;
                if (effect.Kind == "deer")
                {
                    Vector2 direction = effect.Direction;
                    // Keep the logical road position and the native four-leg gallop together;
                    // an independent bob or rotation would move hooves away from the path.
                    var deerClip = DeerAnimationClip(direction, effect.FacingLeft);
                    if (deerClip != null && deerClip.Texture != null)
                    {
                        DrawAnimationFrame(deerClip, end, deerClip.GroundPivot,
                            effect.Age * .7f + (effect.LaneOffset > 0 ? deerClip.Duration * .5f : 0),
                            .6f, Color.white, true);
                        continue;
                    }
                    DrawLine(end - direction.normalized * 55, end, 12, new Color(.6f, .9f, .73f, .18f));
                    Matrix4x4 previous = GUI.matrix;
                    if (effect.FacingLeft) GUIUtility.ScaleAroundPivot(new Vector2(-1, 1), end);
                    Texture(new Rect(end.x - 29, end.y - 42 - Mathf.Abs(Mathf.Sin(effect.Age * 16)) * 7, 58, 48), deerArt, PixelArt.C("b5d6af")); GUI.matrix = previous;
                }
                else if (effect.Kind == "storm")
                {
                    // DrawStormScreenEffects runs outside the map clip so its bolts can
                    // start at the actual top edge and its soft flash includes the HUD.
                    continue;
                }
                else if (effect.Kind == "spear")
                {
                    Vector2 hand = effect.HasLaunchPoint ? effect.LaunchPoint : ProjectileVisuals.SpearHand(start, effect.FacingLeft);
                    Vector2 point = Vector2.Lerp(hand, end, t);
                    Vector2 direction = ProjectileVisuals.Direction(end - hand);
                    // Keep the golden trail behind the rigid shaft, so the actual spearhead
                    // remains distinct instead of becoming a glowing line to the destination.
                    Vector2 trailEnd = point - direction * 46;
                    Vector2 trailStart = trailEnd - direction * Mathf.Min(38, Vector2.Distance(hand, point));
                    DrawLine(trailStart, trailEnd, 3, new Color(1, .73f, .2f, .45f));
                    if (!DrawEffectById(DivineSpearFx, point, direction, effect.Age, 1.15f, Color.white))
                        DrawSpear(point, end - hand, 58, Gold);
                }
                else if (effect.Kind == "arrow_rise")
                {
                    Vector2 hand = effect.HasLaunchPoint ? effect.LaunchPoint : ProjectileVisuals.HeelArrowHand(start, effect.FacingLeft);
                    Vector2 sky = ProjectileVisuals.ArrowSky(start);
                    // The source enters its first BURST at 370ms. Fit that event to the
                    // exact moment the per-target arrows begin, then let its sparks fade.
                    float riseDuration = Mathf.Max(.01f,
                        BalanceData.Current.heroSystems.Skill(HeroKind.Achilles, 1).visualDelay) * .38f /
                        Mathf.Max(.01f, effect.VisualPlaybackRate);
                    float sampleAge = HeelArrowSampleAge(effect.Age, riseDuration, effect.Lifetime);
                    if (!DrawAnimationBeam(HeelArrowFx, hand, sky, sampleAge, .75f, Color.white))
                        DrawArrow(Vector2.Lerp(hand, sky, Mathf.Clamp01(effect.Age / riseDuration)),
                            ProjectileVisuals.Direction(sky - hand), 31, ManaBlue, 4);
                }
                else if (effect.Kind == "arrow_rain")
                {
                    if (t < .38f) continue;
                    float progress = (t - .38f) / .62f;
                    Vector2 point = ProjectileVisuals.RainTip(start, effect.HitPoint, progress);
                    if (!DrawEffectById(OrdinaryArrowFx, point, effect.Direction, effect.Age, .5f, Color.white))
                        DrawArrow(point, effect.Direction, 22, PixelArt.C("bea98a"), 3);
                }
                else if (effect.Kind == "rage" || effect.Kind == "mirror")
                {
                    float radius = 24 + 50 * t;
                    Texture(new Rect(end.x - radius, end.y - radius, radius * 2, radius * 2), range,
                        effect.Kind == "rage" ? new Color(1, .65f, .25f, 1 - t) : new Color(.5f, .8f, 1, 1 - t));
                }
            }
        }
        private void DrawProjectileImpacts()
        {
            foreach (ProjectileImpact impact in game.ProjectileImpacts)
            {
                if (impact.Landed)
                {
                    if (impact.Kind == "deer") { DrawDeerContact(impact); continue; }
                    if (impact.Kind == "circe_magic" && DrawCirceMagic(true, impact.Position, Vector2.right,
                        FullEffectAge("circe-fx-Fire_Impact", impact.Age, impact.Lifetime), .65f)) continue;
                    string id = impact.Kind == "fireball" ? "circe-fx-Fire_Impact" :
                        impact.Kind == "spear" ? "circe-fx-Sun_Impact" : "enemy-fx-boxing_contact";
                    if (DrawEffectById(id, impact.Position, Vector2.right,
                        FullEffectAge(id, impact.Age, impact.Lifetime),
                        impact.Kind == "spear" ? 1 : impact.Kind == "fireball" ? .65f : .35f, Color.white)) continue;
                }
                float opacity = Mathf.Clamp01(impact.Life / Mathf.Max(.001f, impact.Lifetime));
                Color color = impact.Landed ? impact.Kind == "fireball" ? PixelArt.C("ffad51") : Gold : Dim;
                color.a = opacity;
                Vector2 p = impact.Position;
                float radius = 3 + (1 - opacity) * 7;
                DrawLine(p - impact.Direction * 5, p, 3, color);
                Outline(new Rect(p.x - radius, p.y - radius, radius * 2, radius * 2), color, 2);
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
