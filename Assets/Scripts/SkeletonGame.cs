using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkeletonDefender
{
    public sealed partial class SkeletonGame : MonoBehaviour
    {
        private enum ScreenMode { Menu, Selection, Inventory, Credits, Game, Exit }
        private ScreenMode screen;
        private GameModel game;
        private bool paused, muted;
        private int selected = -1;
        private float accumulator, bannerLife;
        private string banner = "";
        private Texture2D map, menu, pad, range;
        private readonly Texture2D[,] towers = new Texture2D[3, 3];
        private readonly Texture2D[,] enemies = new Texture2D[3, 2];
        private readonly Texture2D[,] skeletons = new Texture2D[14, 2];
        private Font font, boldFont;
        private GUIStyle label;
        private AudioSource audioSource;
        private AudioClip clickSound, buildSound, waveSound, winSound, hitSound;
        private AudioClip bossGong;
        private Texture2D frogArt;
        private bool heroSelected;
        private int bossSoundSerial;
        private readonly List<InventoryItem> runLoot = new List<InventoryItem>();
        private string lootNotice = "";
        private MapLootClaim lootClaim;
        private System.Random lootRandom;
        private float soundCooldown;
        private Vector2 mouse;
        private readonly Rect mapRect = new Rect(24, 124, 1056, 640);
        private static readonly Color Ink = PixelArt.C("0b1020"), Panel = PixelArt.C("151e33"), Edge = PixelArt.C("394a67"),
            Text = PixelArt.C("e0eaf2"), Dim = PixelArt.C("99abc3"), Gold = PixelArt.C("e8b979"), Green = PixelArt.C("53ded5"), Red = PixelArt.C("ed829e");

        private void Awake()
        {
            Application.targetFrameRate = 60;
            profile = InventoryStore.Load();
            // Bundle Cyrillic glyphs: WebGL cannot use the operating system's fallback fonts.
            font = Resources.Load<Font>("Fonts/Ubuntu-R");
            boldFont = Resources.Load<Font>("Fonts/Ubuntu-B");
            map = Resources.Load<Texture2D>("NeonGothic/cemetery_battlefield") ?? PixelArt.Map();
            menu = Resources.Load<Texture2D>("NeonGothic/moonlit_courtyard_menu") ?? PixelArt.Menu();
            pad = Resources.Load<Texture2D>("NeonGothic/building_pad") ?? PixelArt.Pad();
            range = Resources.Load<Texture2D>("NeonGothic/range_ring") ?? PixelArt.Ring();
            for (int k = 0; k < 3; k++) for (int l = 0; l < 3; l++)
                towers[k, l] = Resources.Load<Texture2D>("NeonGothic/tower_" + k + "_" + (l + 1)) ?? PixelArt.Tower((TowerKind)k, l + 1);
            for (int k = 0; k < 3; k++) for (int f = 0; f < 2; f++) enemies[k, f] = PixelArt.Enemy((EnemyKind)k, f);
            for (int k = 0; k < 14; k++) for (int f = 0; f < 2; f++) skeletons[k, f] = PixelArt.Skeleton((SkeletonKind)k, f);
            for (int k = 0; k < 2; k++) heroArt[k] = PixelArt.Hero((HeroKind)k);
            frogArt = PixelArt.Frog(); deerArt = PixelArt.Deer();
            audioSource = gameObject.AddComponent<AudioSource>(); audioSource.volume = .2f;
            clickSound = Tone(470, .05f); buildSound = Tone(740, .12f); waveSound = Tone(185, .25f); winSound = Tone(880, .5f); hitSound = Tone(140, .07f);
            bossGong = CreateBossGong();
        }
        private AudioClip Tone(float frequency, float length)
        {
            const int sampleRate = 22050; int count = (int)(sampleRate * length); float[] data = new float[count];
            for (int i = 0; i < count; i++) { float t = i / (float)sampleRate; data[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * .25f * (1 - i / (float)count); }
            var clip = AudioClip.Create("tone " + frequency, count, 1, sampleRate, false); clip.SetData(data, 0); return clip;
        }
        private void Sound(AudioClip clip) { if (!muted) audioSource.PlayOneShot(clip); }
        public void NewGame()
        {
            game = new GameModel(selectedMap, profile.SelectedHero, profile.EquippedStats(profile.SelectedHero), Environment.TickCount);
            runLoot.Clear(); lootNotice = ""; lootClaim = new MapLootClaim(); lootRandom = new System.Random();
            screen = ScreenMode.Game; paused = false; selected = -1; heroSelected = false; cloneSelected = false; CancelSkillAim(); accumulator = 0; bossSoundSerial = 0;
            Announce("Построй башни и выбери позицию для героя.", 5);
        }
        private void Announce(string text, float duration = 3) { banner = text; bannerLife = duration; }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (pendingEquipItem != null) pendingEquipItem = null;
                else if (aimingSkill >= 0) { CancelSkillAim(); return; }
                else if (screen == ScreenMode.Credits || screen == ScreenMode.Exit || screen == ScreenMode.Selection || screen == ScreenMode.Inventory) screen = ScreenMode.Menu;
                else if (screen == ScreenMode.Game && !Finished) SetPaused(!paused);
            }
            if (screen != ScreenMode.Game || paused) { CancelSkillAim(); return; }
            if (Finished) { CancelSkillAim(); game.Step(Mathf.Min(Time.unscaledDeltaTime, .15f)); return; }
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) SelectBattleHero();
            HandleManualKeys();
            if (Input.GetKeyDown(KeyCode.U)) Upgrade();
            if (bannerLife > 0) bannerLife -= Time.unscaledDeltaTime;
            soundCooldown -= Time.unscaledDeltaTime;
            RunState previous = game.State;
            int oldKills = game.Kills, oldLives = game.Lives, oldWave = game.Wave, oldCompleted = game.WaveCompletionSerial;
            accumulator += Mathf.Min(Time.unscaledDeltaTime, .15f);
            while (accumulator >= GameModel.Tick) { game.Step(GameModel.Tick); accumulator -= GameModel.Tick; }
            if (Finished) CancelSkillAim();
            if (game.Wave != oldWave) { Announce("ВОЛНА " + game.Wave + "  /  " + GameModel.TotalWaves, 3); Sound(waveSound); }
            if (game.BossSpawnSerial != bossSoundSerial) { bossSoundSerial = game.BossSpawnSerial; Sound(bossGong); Announce("ФИНАЛЬНЫЙ БОСС · ИММУНИТЕТ К МАГИИ", 5); }
            if (game.Lives < oldLives) Sound(waveSound);
            else if (game.Kills > oldKills && soundCooldown <= 0) { Sound(hitSound); soundCooldown = .18f; }
            if (game.WaveCompletionSerial != oldCompleted && !Finished)
            {
                var economy = BalanceData.Current.economy;
                int reward = economy.waveBonusBase + economy.waveBonusPerWave * game.LastCompletedWave;
                Announce("Волна " + game.LastCompletedWave + " отражена. Награда: +" + reward + " золота", 4); Sound(buildSound);
            }
            if (game.State == RunState.Victory && previous != RunState.Victory)
            {
                runLoot.AddRange(lootClaim.TryClaim(profile, true, lootRandom));
                lootNotice = runLoot.Count == 0 ? "На этот раз предмет не выпал." : InventoryStore.Save(profile)
                    ? "Предмет добавлен в инвентарь главного меню."
                    : "Предмет получен, но сохранить его не удалось. Не закрывай игру.";
                Sound(winSound);
            }
            if (game.State == RunState.Defeat && previous != RunState.Defeat)
            { lootNotice = "Предмет может выпасть после победы на уровне."; Sound(waveSound); }
        }
        private bool Finished => game != null && (game.State == RunState.Victory || game.State == RunState.Defeat);
        private void SetPaused(bool value)
        {
            paused = value; accumulator = 0; CancelSkillAim();
            if (game != null) game.SetPaused(value);
        }
        private void NextWave()
        {
            if (game != null && !paused && game.StartWave()) { Announce("ВОЛНА " + game.Wave + "  /  " + GameModel.TotalWaves, 2.5f); Sound(waveSound); }
        }
        private void CallEarlyWave()
        {
            if (game == null || paused || !game.TryCallNextWave()) return;
            Announce("ВОЛНА " + game.Wave + " ДОСРОЧНО · +" + game.LastEarlyWaveBonus + " ЗОЛОТА", 4);
            Sound(waveSound);
        }
        private void Build(TowerKind kind)
        {
            if (game.Build(selected, kind)) { Sound(buildSound); Announce("Башня построена", 1.5f); }
            else Announce("Недостаточно золота", 1.6f);
        }
        private void Upgrade()
        {
            if (selected >= 0 && game.Upgrade(selected)) { Sound(buildSound); Announce("Башня улучшена", 1.5f); }
        }
        private void CloseGame()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            screen = ScreenMode.Exit;
#elif UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        private void OnDestroy()
        {
            Destroy(map); Destroy(menu); Destroy(pad); Destroy(range);
            foreach (Texture2D t in towers) if (t != null) Destroy(t);
            foreach (Texture2D t in enemies) if (t != null) Destroy(t);
            foreach (Texture2D t in skeletons) if (t != null) Destroy(t);
            foreach (Texture2D t in heroArt) if (t != null) Destroy(t);
            Destroy(clickSound); Destroy(buildSound); Destroy(waveSound); Destroy(winSound); Destroy(hitSound); Destroy(bossGong); Destroy(frogArt); Destroy(deerArt);
        }
        private void OnGUI()
        {
            if (font == null) return;
            if (label == null) label = new GUIStyle { font = font, richText = false, clipping = TextClipping.Clip, padding = new RectOffset(0, 0, 0, 0) };
            GUI.color = Color.white; GUI.DrawTexture(new Rect(0, 0, UnityEngine.Screen.width, UnityEngine.Screen.height), Texture2D.blackTexture);
            float scale = Mathf.Min(UnityEngine.Screen.width / 1440f, UnityEngine.Screen.height / 900f);
            float ox = (UnityEngine.Screen.width - 1440 * scale) / 2, oy = (UnityEngine.Screen.height - 900 * scale) / 2;
            Matrix4x4 old = GUI.matrix; GUI.matrix = Matrix4x4.TRS(new Vector3(ox, oy, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            mouse = Event.current.mousePosition;
            Fill(new Rect(0, 0, 1440, 900), Ink);
            if (screen == ScreenMode.Menu) DrawMenu();
            else if (screen == ScreenMode.Selection) DrawSelection();
            else if (screen == ScreenMode.Inventory) DrawInventory();
            else if (screen == ScreenMode.Credits) DrawCredits();
            else if (screen == ScreenMode.Exit) DrawExit();
            else DrawGame();
            GUI.matrix = old; GUI.color = Color.white;
        }
        private static void Fill(Rect r, Color color) { GUI.color = color; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white; }
        private static void Outline(Rect r, Color color, int width = 1)
        {
            Fill(new Rect(r.x, r.y, r.width, width), color); Fill(new Rect(r.x, r.yMax - width, r.width, width), color);
            Fill(new Rect(r.x, r.y, width, r.height), color); Fill(new Rect(r.xMax - width, r.y, width, r.height), color);
        }
        private static void Texture(Rect r, Texture2D t, Color? tint = null) { GUI.color = tint ?? Color.white; GUI.DrawTexture(r, t); GUI.color = Color.white; }
        private void Txt(string text, float x, float y, float width, float height, int size = 18, Color? color = null, FontStyle style = FontStyle.Normal, TextAnchor align = TextAnchor.UpperLeft)
        {
            label.font = style == FontStyle.Bold ? boldFont : font;
            label.fontSize = size; label.fontStyle = FontStyle.Normal; label.alignment = align; label.wordWrap = true; label.normal.textColor = color ?? Text;
            GUI.Label(new Rect(x, y, width, height), text, label);
        }
        private bool Button(Rect r, string text, bool primary = false, bool enabled = true, int size = 19)
        {
            bool hover = r.Contains(mouse) && enabled;
            Color bg = primary ? (hover ? PixelArt.C("91fff0") : PixelArt.C("4bd5cf"))
                : (hover ? PixelArt.C("26344f") : Panel);
            if (!enabled) bg = PixelArt.C("161c2d");
            Fill(new Rect(r.x, r.y + 4, r.width, r.height), PixelArt.C("070c18")); Fill(r, bg);
            Outline(r, enabled ? (primary ? PixelArt.C("b7fff3") : (hover ? Green : Edge)) : Edge);
            Fill(new Rect(r.x + 3, r.y + 3, r.width - 6, 1), new Color(1, 1, 1, primary ? .25f : .08f));
            if (enabled && !primary) Fill(new Rect(r.x + 1, r.y + 10, 2, Mathf.Max(2, r.height - 20)), hover ? Green : PixelArt.C("786193"));
            // Stepped corner cuts keep the metalwork on the same pixel grid as the art.
            Fill(new Rect(r.x, r.y, 2, 2), Ink); Fill(new Rect(r.xMax - 2, r.yMax - 2, 2, 2), Ink);
            Txt(text, r.x + 8, r.y, r.width - 16, r.height, size, !enabled ? Dim * .7f : primary ? Ink : Text, FontStyle.Bold, TextAnchor.MiddleCenter);
            bool clicked = enabled && GUI.Button(r, GUIContent.none, GUIStyle.none);
            if (clicked) { GUI.FocusControl(null); Sound(clickSound); }
            return clicked;
        }
        private void Box(Rect r)
        {
            Fill(new Rect(r.x + 3, r.y + 5, r.width, r.height), PixelArt.C("080d1b"));
            Fill(r, Panel); Outline(r, Edge);
            Fill(new Rect(r.x + 5, r.y + 3, r.width - 10, 1), PixelArt.C("2d3c55"));
            Fill(new Rect(r.x, r.y, 16, 2), Green * .65f);
            Fill(new Rect(r.xMax - 16, r.yMax - 2, 16, 2), PixelArt.C("98588c"));
        }
        private void DrawMenuBackground()
        {
            Texture(new Rect(0, 0, 1440, 900), menu);
            DrawGothicMenuLights();
            // A stepped scrim preserves a crisp pixel edge and keeps type readable.
            for (int i = 0; i < 16; i++) Fill(new Rect(i * 48, 0, 48, 900), new Color(.025f, .035f, .075f, Mathf.Max(0, .69f - i * .046f)));
            float t = Time.unscaledTime;
            for (int i = 0; i < 19; i++)
            {
                float x = 775 + Mathf.Repeat(i * 47 + t * (3 + i % 3), 600);
                float y = 520 + Mathf.Sin(t * .45f + i * 2) * 80 + (i % 4) * 48;
                Fill(new Rect(x, y, 2, 2), i % 3 == 0 ? new Color(.94f, .3f, .65f, .35f + .15f * Mathf.Sin(t + i)) : new Color(.27f, .96f, .91f, .3f + .15f * Mathf.Sin(t + i)));
            }
            Fill(new Rect(0, 856, 1440, 44), new Color(.025f, .035f, .075f, .92f));
            Fill(new Rect(40, 855, 1360, 1), Edge);
        }
        private void DrawMenu()
        {
            DrawMenuBackground();
            Fill(new Rect(112, 139, 30, 3), Gold);
            Txt("TOWER DEFENCE   /   ГЛАВА I", 158, 127, 500, 28, 15, Gold, FontStyle.Bold);
            Txt("SKELETON", 108, 184, 700, 91, 78, Text, FontStyle.Bold);
            Txt("DEFENDER", 109, 272, 690, 82, 70, Green, FontStyle.Bold);
            Txt("Удержи последний рубеж.", 114, 376, 540, 36, 25, Text);
            Txt("Строй башни. Останови орду. Встреть рассвет.", 115, 418, 540, 48, 18, Dim);
            if (Button(new Rect(112, 491, 368, 62), "НАЧАТЬ", true, size: 23)) screen = ScreenMode.Selection;
            if (Button(new Rect(112, 571, 368, 54), "ИНВЕНТАРЬ")) { screen = ScreenMode.Inventory; inventoryMessage = ""; }
            if (Button(new Rect(112, 641, 368, 54), "АВТОРЫ")) screen = ScreenMode.Credits;
            if (Button(new Rect(112, 711, 368, 54), "ЗАКРЫТЬ")) CloseGame();
            Txt("01   " + BalanceData.Current.mapName.ToUpperInvariant(), 825, 788, 520, 27, 16, Gold, FontStyle.Bold, TextAnchor.MiddleRight);
            Txt("20 волн. Около 30 минут. Два героя.", 925, 817, 420, 26, 15, Dim, align: TextAnchor.MiddleRight);
            Txt("SKELETON DEFENDER    ·    ПРОТОТИП " + Application.version, 40, 866, 710, 23, 13, Dim);
            if (Button(new Rect(1245, 862, 155, 29), muted ? "ЗВУК: ВЫКЛ" : "ЗВУК: ВКЛ", size: 12)) muted = !muted;
        }
        private void DrawCredits()
        {
            DrawMenuBackground(); Fill(new Rect(0, 0, 1440, 856), new Color(.03f, .06f, .075f, .65f));
            Txt("АВТОРЫ", 300, 179, 840, 72, 43, Text, FontStyle.Bold, TextAnchor.MiddleCenter);
            Fill(new Rect(685, 270, 70, 2), Gold);
            // Intentionally empty: credits are supplied by the project owner later.
            if (Button(new Rect(530, 698, 380, 62), "НАЗАД В МЕНЮ")) screen = ScreenMode.Menu;
        }
        private void DrawExit()
        {
            DrawMenuBackground(); Fill(new Rect(0, 0, 1440, 900), new Color(.03f, .06f, .075f, .75f));
            Txt("ДО ВСТРЕЧИ НА РУБЕЖЕ", 250, 300, 940, 70, 38, Gold, FontStyle.Bold, TextAnchor.MiddleCenter);
            Txt("Теперь можно закрыть вкладку браузера.", 350, 404, 740, 60, 22, Text, align: TextAnchor.MiddleCenter);
            if (Button(new Rect(530, 550, 380, 62), "ВЕРНУТЬСЯ В МЕНЮ")) screen = ScreenMode.Menu;
        }
        private void DrawGame()
        {
            DrawHud();
            Outline(new Rect(mapRect.x - 2, mapRect.y - 2, mapRect.width + 4, mapRect.height + 4), Edge, 2);
            GUI.BeginGroup(mapRect);
            Texture(new Rect(0, 0, 1056, 640), map);
            DrawRange();
            for (int i = 0; i < GameModel.Sites.Length; i++)
            {
                Vector2 p = GameModel.Sites[i]; Tower tower = game.At(i);
                if (tower != null) continue;
                Texture(new Rect(p.x - 34, p.y - 20, 68, 48), pad);
                bool hover = new Rect(mapRect.x + p.x - 35, mapRect.y + p.y - 38, 70, 76).Contains(mouse);
                Color color = i == selected ? Gold : hover ? Text : Green;
                Fill(new Rect(p.x - 9, p.y - 2, 18, 4), color); Fill(new Rect(p.x - 2, p.y - 9, 4, 18), color);
                if (i == selected) Outline(new Rect(p.x - 36, p.y - 24, 72, 53), Gold, 2);
            }
            DrawBattleLayers();
            DrawEnemyEffects();
            DrawTravelingProjectiles();
            DrawAbilityEffects();
            DrawProjectileImpacts();
            foreach (Shot shot in game.Shots)
            {
                if (TryDrawAnimatedShot(shot)) continue;
                Vector2 a = shot.UsesVisualAnchors ? shot.Start : shot.Start + new Vector2(0, -38);
                Vector2 b = shot.UsesVisualAnchors ? shot.End : shot.End + new Vector2(0, -13);
                if (!string.IsNullOrEmpty(shot.Skill))
                {
                    Color effect = shot.Skill == "sun" ? Gold : shot.Skill == "frog" ? PixelArt.C("79c7e2") : shot.Skill == "decapitate" ? Red : Text;
                    if (shot.Skill == "sun") DrawLine(b + new Vector2(0, -180), b, 7, effect);
                    else if (shot.Skill == "frog") Outline(new Rect(b.x - 24, b.y - 24, 48, 48), effect, 3);
                    else DrawLine(a, b, 3, effect);
                    continue;
                }
                // The model's projectile list supplies the complete visible flight. Shots show impact only.
                Color c = shot.Kind == TowerKind.Archer ? PixelArt.C("e0c891") : shot.Kind == TowerKind.Ember ? PixelArt.C("f2ab68") : PixelArt.C("98dce0");
                float progress = Mathf.Clamp01(1 - shot.Life / .23f);
                if (shot.Kind != TowerKind.Archer)
                {
                    float r = shot.Kind == TowerKind.Ember ? 17 : 8;
                    Outline(new Rect(b.x - r, b.y - r, r * 2, r * 2), new Color(c.r, c.g, c.b, 1 - progress), 2);
                }
            }
            foreach (HeroProjectile projectile in game.Projectiles)
            {
                if (projectile.Delay > 0) continue;
                if (!projectile.Visual.Initialized) continue;
                if (TryDrawHeroAnimationProjectile(projectile)) continue;
                Vector2 p = projectile.Visual.Tip;
                Vector2 direction = projectile.Visual.Direction;
                Color c = projectile.Kind == ProjectileKind.Knife ? Text : PixelArt.C("ef9d59");
                float size = projectile.Kind == ProjectileKind.Fireball ? 9 : 6;
                if (projectile.Kind == ProjectileKind.Knife)
                { DrawArrow(p, direction, 13, c, 3); continue; }
                DrawLine(p - direction * 15, p, size * .7f, new Color(c.r, c.g, c.b, .45f));
                Fill(new Rect(p.x - size / 2, p.y - size / 2, size, size), c);
                Fill(new Rect(p.x - 1, p.y - 1, 3, 3), Text);
            }
            foreach (Popup popup in game.Popups)
                Txt(popup.Text, popup.Position.x - 30, popup.Position.y - 65 - (1.2f - popup.Life) * 23, 70, 30, 18, popup.Damage ? Red : Gold, FontStyle.Bold, TextAnchor.MiddleCenter);
            DrawEnemyTooltip();
            DrawAimMarker();
            Fill(new Rect(14, 15, 402, 31), new Color(.025f, .035f, .075f, .86f));
            Txt(BalanceData.Current.mapName.ToUpperInvariant(), 27, 23, 390, 25, 13, Text, FontStyle.Bold);
            if (bannerLife > 0)
            {
                Fill(new Rect(230, 580, 625, 42), new Color(.025f, .035f, .075f, .94f));
                Outline(new Rect(230, 580, 625, 42), Edge);
                Txt(banner, 240, 580, 605, 42, 17, Gold, align: TextAnchor.MiddleCenter);
            }
            GUI.EndGroup();
            HandleSites(); DrawSidebar(); DrawBottom(); DrawSkillsBar();
            DrawStormScreenEffects();
            if (paused) DrawPause();
            else if (Finished) DrawResult();
        }
        private static void DrawLine(Vector2 a, Vector2 b, float width, Color color)
        {
            Matrix4x4 before = GUI.matrix; Vector2 delta = b - a;
            RotateGuiLocal(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, a);
            Fill(new Rect(a.x, a.y - width / 2, delta.magnitude, width), color); GUI.matrix = before;
        }

        // Unity's helper returns R * M, where its un-clipped pivot was obtained with
        // M temporarily set to identity. Our canvas already has letterboxing/scale,
        // so that local rotation must instead be applied before the canvas: M * R.
        // Keep Unity's native un-clipping, and change only the multiplication order.
        private static void RotateGuiLocal(float angle, Vector2 pivot)
        {
            Matrix4x4 canvas = GUI.matrix;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Vector2 beforePoint = GUIUtility.GUIToScreenPoint(pivot);
            Vector2 beforeX = GUIUtility.GUIToScreenPoint(pivot + Vector2.right) - beforePoint;
            Vector2 beforeY = GUIUtility.GUIToScreenPoint(pivot + Vector2.up) - beforePoint;
#endif
            GUIUtility.RotateAroundPivot(angle, pivot);
            GUI.matrix = canvas * GUI.matrix * canvas.inverse;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!guiRotationChecked && Event.current.type == EventType.Repaint && Mathf.Abs(angle) > 1)
            {
                guiRotationChecked = true;
                Vector2 afterPoint = GUIUtility.GUIToScreenPoint(pivot);
                Vector2 afterX = GUIUtility.GUIToScreenPoint(pivot + Vector2.right) - afterPoint;
                float radians = angle * Mathf.Deg2Rad;
                Vector2 expectedX = beforeX * Mathf.Cos(radians) + beforeY * Mathf.Sin(radians);
                Debug.Assert(Vector2.Distance(beforePoint, afterPoint) < .05f && Vector2.Distance(afterX, expectedX) < .05f,
                    "GUI rotation moved its actual screen pivot or changed the local axis inside the map group.");
            }
#endif
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static bool guiRotationChecked;
#endif
        private void DrawRange()
        {
            if (selected < 0) return;
            Vector2 p = GameModel.Sites[selected]; Tower tower = game.At(selected);
            float r = tower?.Range ?? 174;
            Texture(new Rect(p.x - r, p.y - r, 2 * r, 2 * r), range,
                tower != null && tower.Kind == TowerKind.Ember ? Gold : Green);
        }
        private void HandleSites()
        {
            if (paused || Finished || Event.current.type != EventType.MouseDown || Event.current.button != 0 || !mapRect.Contains(mouse)) return;
            if (HandleAimClick() || SkillBarContains(mouse)) return;
            Vector2 local = mouse - mapRect.position;
            if (game.Clone != null && game.Clone.Alive && AnimatedActors.HeroBounds(game.Clone).Contains(AnimatedActors.HeroPose(game.Clone).inverse.MultiplyPoint3x4(local)))
            { SelectBattleHero(true); Event.current.Use(); return; }
            if (game.Hero.Alive && AnimatedActors.HeroBounds(game.Hero).Contains(AnimatedActors.HeroPose(game.Hero).inverse.MultiplyPoint3x4(local)))
            { SelectBattleHero(); Event.current.Use(); return; }
            int nearest = -1; float best = 47;
            float frontTowerY = float.NegativeInfinity;
            for (int i = 0; i < GameModel.Sites.Length; i++)
            {
                Vector2 p = GameModel.Sites[i];
                Tower tower = game.At(i);
                if (tower != null)
                {
                    if (ProjectileVisuals.TowerSelectionBounds(p, tower.Kind, tower.Level).Contains(local) && p.y >= frontTowerY)
                    { frontTowerY = p.y; nearest = i; }
                    continue;
                }
                float d = Vector2.Distance(local, p);
                if (float.IsNegativeInfinity(frontTowerY) && d < best) { best = d; nearest = i; }
            }
            if (nearest >= 0) { selected = nearest; heroSelected = false; }
            else if (heroSelected) game.MoveHero(local, cloneSelected);
            else selected = -1;
            Sound(clickSound); Event.current.Use();
        }
        private void DrawHud()
        {
            Box(new Rect(24, 20, 1392, 80));
            Txt("SKELETON", 47, 36, 230, 28, 24, Text, FontStyle.Bold);
            Txt("DEFENDER", 49, 65, 200, 21, 14, Gold, FontStyle.Bold);
            Fill(new Rect(293, 38, 1, 43), Edge);
            Txt("ЗОЛОТО", 327, 34, 160, 20, 12, Dim); Txt(game.Gold.ToString(), 327, 53, 160, 34, 28, Gold, FontStyle.Bold);
            Txt("КРЕПОСТЬ", 489, 34, 170, 20, 12, Dim); Txt(game.Lives + " / " + BalanceData.Current.economy.startingLives, 489, 53, 170, 34, 28, game.Lives <= 6 ? Red : Text, FontStyle.Bold);
            Txt("ВОЛНА", 684, 34, 170, 20, 12, Dim); Txt(game.Wave + " / " + GameModel.TotalWaves, 684, 53, 170, 34, 28, Text, FontStyle.Bold);
            Txt("ПОБЕЖДЕНО", 872, 34, 180, 20, 12, Dim); Txt(game.Kills.ToString(), 872, 53, 150, 34, 28, Green, FontStyle.Bold);
            Txt("ВРЕМЯ", 1044, 34, 145, 20, 12, Dim); Txt(ClockText(game.Elapsed), 1044, 57, 145, 30, 22, Text, FontStyle.Bold);
            bool active = !paused && !Finished;
            if (Button(new Rect(1196, 38, 198, 42), "ПАУЗА  /  ESC", enabled: active, size: 15)) SetPaused(true);
        }
        private void DrawSidebar()
        {
            bool active = !paused && !Finished;
            Box(new Rect(1104, 124, 312, 640));
            Fill(new Rect(1105, 125, 310, 4), Gold);
            if (heroSelected) { DrawHeroSidebar(); return; }
            Tower selectedTower = selected < 0 ? null : game.At(selected);
            if (selectedTower != null)
            {
                Txt("УЛУЧШЕНИЕ БАШНИ", 1125, 148, 270, 25, 13, Gold, FontStyle.Bold);
                Texture(new Rect(1205, 197, 108, 144), towers[(int)selectedTower.Kind, selectedTower.Level - 1]);
                Txt(GameModel.TowerName(selectedTower.Kind), 1123, 355, 274, 55, 20, Text, FontStyle.Bold, TextAnchor.MiddleCenter);
                Txt("УРОВЕНЬ " + selectedTower.Level + " / 3", 1126, 416, 266, 25, 14, Gold, FontStyle.Bold, TextAnchor.MiddleCenter);
                Fill(new Rect(1125, 458, 270, 1), Edge);
                Stat("Урон", (selectedTower.ProjectileCount > 1 ? selectedTower.ProjectileCount + " × " : "") + selectedTower.Damage.ToString("0.##"), 480);
                Stat("Дальность", selectedTower.Range.ToString("0"), 515);
                Stat("Выстрел", selectedTower.Interval.ToString("0.00") + " сек.", 550);
                if (selectedTower.Kind == TowerKind.Archer && selectedTower.ProjectileCount > 1)
                    Txt("2 лучника · 2 стрелы за атаку", 1125, 582, 270, 18, 12, Gold, align: TextAnchor.MiddleCenter);
                string up = selectedTower.Level >= 3 ? "МАКСИМАЛЬНЫЙ УРОВЕНЬ" : "УЛУЧШИТЬ  ·  " + selectedTower.UpgradeCost;
                if (Button(new Rect(1124, 603, 272, 55), up, true, active && selectedTower.Level < 3 && game.Gold >= selectedTower.UpgradeCost, 15)) Upgrade();
                if (Button(new Rect(1124, 677, 272, 44), "ПРОДАТЬ  ·  +" + selectedTower.SellValue, enabled: active, size: 15))
                { game.Sell(selected); Sound(buildSound); Announce("Башня продана", 1.5f); }
            }
            else
            {
                Txt(selected >= 0 ? "СТРОИТЕЛЬСТВО" : "АРСЕНАЛ", 1125, 148, 270, 26, 14, Gold, FontStyle.Bold);
                Txt(selected >= 0 ? "Выбери башню для этой позиции." : "Нажми на площадку + на карте,\nчтобы поставить башню.", 1125, 183, 270, 64, 17, Text);
                BuildCard(TowerKind.Archer, 266, "Быстрые точные выстрелы.\nФизический урон одной цели.", active);
                BuildCard(TowerKind.Ember, 413, "Магический урон по группе.\nВзрыв в области попадания.", active);
                BuildCard(TowerKind.Frost, 560, "Замедляет врагов на 45%.\nДаёт другим башням время.", active);
                Txt("ПОСТРОЙКА БАШНИ — КНОПКОЙ МЫШИ", 1124, 734, 276, 19, 10, Dim, align: TextAnchor.MiddleCenter);
            }
        }
        private void Stat(string title, string value, float y)
        { Txt(title, 1127, y, 135, 27, 17, Dim); Txt(value, 1263, y, 128, 27, 18, Text, FontStyle.Bold, TextAnchor.UpperRight); }
        private void BuildCard(TowerKind kind, float y, string description, bool active)
        {
            Fill(new Rect(1124, y, 272, 131), PixelArt.C("111b2e")); Outline(new Rect(1124, y, 272, 131), Edge);
            Texture(new Rect(1130, y + 13, 54, 72), towers[(int)kind, 0]);
            string name = kind == TowerKind.Archer ? "СТРЕЛКОВАЯ" : kind == TowerKind.Ember ? "ОГНЕННАЯ" : "ЛЕДЯНАЯ";
            Txt(name, 1194, y + 11, 192, 24, 15, Text, FontStyle.Bold);
            Txt(description, 1194, y + 40, 186, 54, 13, Dim);
            int cost = GameModel.Cost(kind);
            bool can = active && selected >= 0 && game.Gold >= cost;
            if (Button(new Rect(1194, y + 93, 187, 28), "ПОСТРОИТЬ  ·  " + cost, true, can, 12)) Build(kind);
        }
        private void DrawBottom()
        {
            DrawHeroCard();
            Box(new Rect(438, 788, 642, 88));
            int upcoming = Mathf.Min(game.Wave + (game.State == RunState.Preparing ? 1 : 0), GameModel.TotalWaves);
            string status = game.Wave == 0 ? "ПОДГОТОВКА К ОБОРОНЕ" : "ВРАГОВ ОСТАЛОСЬ: " + game.PlannedRemaining;
            Txt(status, 454, 800, 350, 25, 14, Gold, FontStyle.Bold);
            string waveInfo = "Волна " + upcoming + ": " + BalanceData.Current.Wave(upcoming).count + (upcoming == GameModel.TotalWaves ? " босс" : " врагов") + " · Отражено: " + game.WavesCompleted + " / " + GameModel.TotalWaves;
            if (game.SummonedRemaining > 0) waveInfo = "Ещё призванных: " + game.SummonedRemaining + " · " + waveInfo;
            Txt(waveInfo, 454, 839, 610, 30, 13, Dim);
            for (int i = 0; i < GameModel.TotalWaves; i++)
            {
                Color c = i < game.Wave ? Gold : Edge;
                if (i < game.WaveRuns.Count && game.WaveRuns[i].Completed) c = Green;
                Fill(new Rect(807 + i * 12, 813, 8, 14), c);
            }
            bool ready = game.Wave == 0 && game.State == RunState.Preparing && !paused && !Finished;
            if (ready)
            {
                if (Button(new Rect(1104, 788, 312, 62), "НАЧАТЬ ОБОРОНУ", true, true, 18)) NextWave();
            }
            else if (game.CanCallNextWave && !paused && !Finished) DrawEarlyWaveButton();
            else
            {
                Box(new Rect(1104, 788, 312, 62));
                string title = game.Wave >= GameModel.TotalWaves ? "ФИНАЛЬНАЯ ВОЛНА" : game.State == RunState.Preparing ? "АВТОСТАРТ " + ClockText(game.NextWaveIn) : "ВОЛНА ИДЁТ";
                Txt(title, 1112, 796, 296, 24, 16, Dim, FontStyle.Bold, TextAnchor.MiddleCenter);
                string hint = game.Wave >= GameModel.TotalWaves ? "Босс уязвим к физическому урону" : "Для вызова вышло: " + game.EarlyWaveSpawned + " / " + game.EarlyWaveRequiredSpawned;
                Txt(hint, 1112, 824, 296, 21, 12, Dim, align: TextAnchor.MiddleCenter);
            }
            Txt("2 / ПРОБЕЛ — ГЕРОЙ  ·  3 — КОПИЯ", 1104, 861, 312, 20, 10, Dim, align: TextAnchor.MiddleCenter);
        }
        private void DrawEarlyWaveButton()
        {
            Rect r = new Rect(1104, 788, 312, 62);
            bool hover = r.Contains(mouse);
            Fill(new Rect(r.x, r.y + 4, r.width, r.height), PixelArt.C("0a151b"));
            Fill(r, hover ? PixelArt.C("264858") : PixelArt.C("1b3449"));
            Outline(r, Gold, 2);
            float pulse = .55f + .15f * Mathf.Sin(game.Elapsed * 3);
            Fill(new Rect(r.x + 3, r.y + 3, r.width - 6, 2), new Color(Gold.r, Gold.g, Gold.b, pulse));
            // A small pixel horn makes the call-to-arms control distinct from building buttons.
            Fill(new Rect(1119, 821, 14, 5), Gold);
            Fill(new Rect(1129, 814, 8, 12), Gold);
            Fill(new Rect(1135, 806, 8, 19), Gold);
            Fill(new Rect(1142, 801, 5, 27), Gold);
            Fill(new Rect(1123, 826, 5, 6), Gold);
            Txt("ВЫЗВАТЬ ВОЛНУ " + (game.Wave + 1), 1154, 794, 252, 29, 17, Gold, FontStyle.Bold, TextAnchor.MiddleCenter);
            Txt("+" + game.EarlyWaveBonus + " золота · враги останутся", 1154, 825, 252, 20, 12, Text, align: TextAnchor.MiddleCenter);
            if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { GUI.FocusControl(null); CallEarlyWave(); }
        }
        private static string ClockText(float seconds)
        {
            int value = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return (value / 60).ToString("00") + ":" + (value % 60).ToString("00");
        }
        private void Overlay() { Fill(new Rect(0, 0, 1440, 900), new Color(.025f, .05f, .065f, .86f)); }
        private void DrawPause()
        {
            Overlay(); Box(new Rect(456, 190, 528, 530));
            Txt("ПЕРЕДЫШКА", 490, 225, 460, 60, 38, Text, FontStyle.Bold, TextAnchor.MiddleCenter);
            Txt("Орда подождёт.", 490, 299, 460, 36, 19, Dim, align: TextAnchor.MiddleCenter);
            if (Button(new Rect(512, 376, 416, 62), "ПРОДОЛЖИТЬ", true)) SetPaused(false);
            if (Button(new Rect(512, 456, 416, 55), "НАЧАТЬ ЗАНОВО")) NewGame();
            if (Button(new Rect(512, 530, 416, 55), "В ГЛАВНОЕ МЕНЮ")) { screen = ScreenMode.Menu; paused = false; }
            if (Button(new Rect(512, 624, 416, 38), muted ? "ЗВУК: ВЫКЛЮЧЕН" : "ЗВУК: ВКЛЮЧЁН", size: 14)) muted = !muted;
        }
        private void DrawResult()
        {
            Overlay(); Box(new Rect(400, 95, 640, 715));
            bool win = game.State == RunState.Victory;
            Txt(win ? "РАССВЕТ ВСТРЕЧЕН" : "РУБЕЖ ПАЛ", 434, 131, 572, 64, 40, win ? Gold : Red, FontStyle.Bold, TextAnchor.MiddleCenter);
            Txt(win ? "Уровень " + game.Map + " пройден. Все " + GameModel.TotalWaves + " волн отражены." : "Попробуй другую расстановку и позицию героя.", 442, 212, 556, 64, 20, Text, align: TextAnchor.MiddleCenter);
            Txt("ВОЛНЫ", 475, 295, 140, 27, 13, Dim, align: TextAnchor.MiddleCenter);
            Txt(game.Wave + " / " + GameModel.TotalWaves, 475, 329, 140, 45, 28, Text, FontStyle.Bold, TextAnchor.MiddleCenter);
            Txt("ПОБЕЖДЕНО", 650, 295, 140, 27, 13, Dim, align: TextAnchor.MiddleCenter);
            Txt(game.Kills.ToString(), 650, 329, 140, 45, 28, Gold, FontStyle.Bold, TextAnchor.MiddleCenter);
            Txt("КРЕПОСТЬ", 825, 295, 140, 27, 13, Dim, align: TextAnchor.MiddleCenter);
            Txt(game.Lives + " / " + BalanceData.Current.economy.startingLives, 825, 329, 140, 45, 28, Text, FontStyle.Bold, TextAnchor.MiddleCenter);
            Fill(new Rect(446, 397, 548, 1), Edge);
            Txt("ДОБЫЧА", 448, 414, 544, 28, 14, Gold, FontStyle.Bold, TextAnchor.MiddleCenter);
            if (runLoot.Count == 0)
                Txt(lootNotice, 452, 466, 536, 92, 19, Dim, align: TextAnchor.MiddleCenter);
            else
            {
                for (int i = 0; i < runLoot.Count && i < 3; i++)
                {
                    InventoryItem item = runLoot[i];
                    Txt(item.Name, 458, 454 + i * 35, 524, 29, 18, ItemColor(item), FontStyle.Bold, TextAnchor.MiddleCenter);
                }
                Txt(lootNotice, 452, 565, 536, 46, 15, Dim, align: TextAnchor.MiddleCenter);
            }
            if (Button(new Rect(510, 636, 420, 60), "ИГРАТЬ ЕЩЁ", true)) NewGame();
            if (Button(new Rect(510, 717, 420, 52), "В ГЛАВНОЕ МЕНЮ")) screen = ScreenMode.Menu;
        }
    }
}
