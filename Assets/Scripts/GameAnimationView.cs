using System.Collections.Generic;
using UnityEngine;

namespace SkeletonDefender
{
    public sealed partial class SkeletonGame
    {
        private struct BattleLayer
        {
            public float Y;
            public int Order;
            public Enemy Enemy;
            public EnemyCorpse Corpse;
            public HeroCombatState Hero;
            public HeroCorpse HeroCorpse;
            public Tower Tower;
        }
        private readonly List<BattleLayer> battleLayers = new List<BattleLayer>(256);

        private static bool DrawAnimation(string character, string action, bool facingLeft,
            Vector2 ground, float age, float pixelScale, Color tint, bool? loop = null)
        {
            AnimationClipData clip = AnimationLibrary.Get(character, action, facingLeft);
            if (clip == null) return false;
            return DrawAnimationFrame(clip, ground, clip.GroundPivot, age, pixelScale, tint, loop ?? clip.Loop);
        }

        private static bool DrawAnimationFrame(AnimationClipData clip, Vector2 anchor, Vector2 origin,
            float age, float pixelScale, Color tint, bool loop)
        {
            if (clip == null || clip.Texture == null || clip.Frames.Length == 0) return false;
            Rect destination = new Rect(anchor.x - origin.x * pixelScale, anchor.y - origin.y * pixelScale,
                clip.Width * pixelScale, clip.Height * pixelScale);
            Rect uv = clip.UV(clip.FrameAt(age, loop));
            Color old = GUI.color;
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(destination, clip.Texture, uv, true);
            GUI.color = old;
            return true;
        }

        private static bool DrawEffectById(string id, Vector2 anchor, Vector2 direction,
            float age, float pixelScale, Color tint)
        {
            AnimationClipData clip = AnimationLibrary.GetById(id);
            if (clip == null || clip.Texture == null) return false;
            Matrix4x4 before = GUI.matrix;
            Color previousColor = GUI.color;
            try
            {
                RotateGuiLocal(EffectAnimationAngle(clip, direction), anchor);
                GUI.color = tint;
                GUI.DrawTextureWithTexCoords(EffectAnimationRect(clip, anchor, pixelScale), clip.Texture,
                    clip.UV(clip.FrameAt(age, clip.Loop)), true);
            }
            finally { GUI.matrix = before; GUI.color = previousColor; }
            return true;
        }

        private static Rect EffectAnimationRect(AnimationClipData clip, Vector2 anchor, float pixelScale)
            => new Rect(anchor - clip.Origin * pixelScale, new Vector2(clip.Width, clip.Height) * pixelScale);

        private static float EffectAnimationAngle(AnimationClipData clip, Vector2 direction)
            => (Mathf.Atan2(direction.y, direction.x) - Mathf.Atan2(clip.RotationAxis.y, clip.RotationAxis.x)) * Mathf.Rad2Deg;

        private static Matrix4x4 EffectAnimationMatrix(AnimationClipData clip, Vector2 anchor, Vector2 direction, float pixelScale)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float authored = Mathf.Atan2(clip.RotationAxis.y, clip.RotationAxis.x) * Mathf.Rad2Deg;
            return Matrix4x4.Translate(anchor) * Matrix4x4.Rotate(Quaternion.Euler(0, 0, angle - authored))
                * Matrix4x4.Scale(new Vector3(pixelScale, pixelScale, 1)) * Matrix4x4.Translate(-clip.Origin);
        }

        private void DrawHeroPortrait(Rect box, HeroKind hero, Color tint)
        {
            var clip = AnimationLibrary.Get(hero == HeroKind.Circe ? "circe" : "achilles", "idle");
            if (clip == null) { Texture(box, heroArt[(int)hero], tint); return; }
            Rect bounds = clip.OpaqueBounds;
            float scale = Mathf.Min(box.width / Mathf.Max(1, bounds.width), box.height / Mathf.Max(1, bounds.height));
            Vector2 top = box.center - bounds.size * scale * .5f;
            Vector2 ground = top + (clip.GroundPivot - bounds.position) * scale;
            DrawAnimationFrame(clip, ground, clip.GroundPivot, Time.unscaledTime, scale, tint, true);
        }

        private void DrawInventoryHeroPortrait(Rect box, HeroKind hero)
        {
            Texture2D texture = HeroPortraits.Texture(hero);
            if (texture == null) { DrawHeroPortrait(box, hero, Color.white); return; }
            Rect bounds = HeroPortraits.Bounds(hero);
            float scale = Mathf.Min(box.width / Mathf.Max(1, bounds.width), box.height / Mathf.Max(1, bounds.height));
            Rect destination = new Rect(box.center - bounds.size * scale * .5f, bounds.size * scale);
            Rect uv = new Rect(bounds.x / texture.width, 1 - bounds.yMax / texture.height,
                bounds.width / texture.width, bounds.height / texture.height);
            Color previous = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTextureWithTexCoords(destination, texture, uv, true);
            GUI.color = previous;
        }

        private void DrawBattleLayers()
        {
            battleLayers.Clear();
            foreach (EnemyCorpse corpse in game.EnemyCorpses)
                battleLayers.Add(new BattleLayer { Y = corpse.Position.y, Order = 0, Corpse = corpse });
            foreach (HeroCorpse corpse in game.HeroCorpses)
                battleLayers.Add(new BattleLayer { Y = corpse.Position.y, Order = 0, HeroCorpse = corpse });
            foreach (Enemy enemy in game.Enemies)
                if (!enemy.Dead && enemy.AppearanceDelay <= 0) battleLayers.Add(new BattleLayer { Y = game.Position(enemy.Distance).y, Order = 1, Enemy = enemy });
            foreach (Tower tower in game.Towers)
                battleLayers.Add(new BattleLayer { Y = GameModel.Sites[tower.Site].y, Order = 2, Tower = tower });
            battleLayers.Add(new BattleLayer { Y = game.Hero.Position.y, Order = 3, Hero = game.Hero });
            if (game.Clone != null && game.Clone.Alive)
                battleLayers.Add(new BattleLayer { Y = game.Clone.Position.y, Order = 3, Hero = game.Clone });
            battleLayers.Sort((a, b) => { int y = a.Y.CompareTo(b.Y); return y != 0 ? y : a.Order.CompareTo(b.Order); });
            foreach (BattleLayer layer in battleLayers)
            {
                if (layer.Corpse != null) DrawEnemyCorpse(layer.Corpse);
                else if (layer.HeroCorpse != null) DrawHeroCorpse(layer.HeroCorpse);
                else if (layer.Enemy != null) DrawAnimatedEnemy(layer.Enemy);
                else if (layer.Hero != null) DrawActor(layer.Hero, heroSelected && cloneSelected == layer.Hero.IsClone);
                else DrawBattleTower(layer.Tower);
            }
        }

        private void DrawBattleTower(Tower tower)
        {
            Vector2 p = GameModel.Sites[tower.Site];
            Rect bounds = ProjectileVisuals.TowerBounds(p, tower.Kind, tower.Level);
            if (tower.Site == selected) Outline(new Rect(p.x - 54, p.y - 16, 108, 24), Gold, 2);
            Texture(bounds, towers[(int)tower.Kind, tower.Level - 1]);
            if (tower.Kind == TowerKind.Archer)
            {
                for (int index = 0; index < tower.ProjectileCount; index++)
                {
                    var state = tower.Archers[index];
                    var clip = AnimationLibrary.GetDirectional("tower_elf", state.Animation.Action,
                        state.Animation.FacingDirection, state.Animation.FacingLeft);
                    if (clip?.Texture == null) continue;
                    Vector2 feet = ProjectileVisuals.TowerArcherGround(p, tower.Level, index);
                    DrawAnimationFrame(clip, feet, clip.GroundPivot, state.Animation.ClipAge,
                        ProjectileVisuals.TowerArcherScale(tower.Level, index), Color.white, clip.Loop);
                }
            }
            if (tower.Flash > 0 && tower.Kind != TowerKind.Archer)
            {
                Vector2 socket = ProjectileVisuals.TowerSocket(p, tower.Kind, tower.Level);
                Fill(new Rect(socket.x - 3, socket.y - 3, 6, 6), Gold);
            }
        }

        private void DrawAnimatedEnemy(Enemy enemy)
        {
            Vector2 ground = game.Position(enemy.Distance);
            float scale = AnimatedActors.EnemyPixelScale(enemy);
            AnimationClipData clip = AnimatedActors.EnemyClip(enemy);
            Color tint = enemy.HitFlash > 0 ? new Color(1, .88f, .82f) : Color.white;
            Texture(new Rect(ground.x - 12 * ProjectileVisuals.EnemyScale(enemy), ground.y - 4,
                24 * ProjectileVisuals.EnemyScale(enemy), 9), pad, new Color(0, 0, 0, .22f));
            if (clip != null)
                DrawAnimationFrame(clip, ground, clip.GroundPivot, enemy.Animation.ClipAge, scale, tint, clip.Loop);
            else
            {
                float s = ProjectileVisuals.EnemyScale(enemy);
                Texture(new Rect(ground.x - 12 * s, ground.y - 24 * s, 24 * s, 29 * s),
                    enemy.IsFrog ? frogArt : skeletons[(int)enemy.Skeleton, 0], tint);
            }
            var standing = AnimationLibrary.GetDirectional(CombatAnimationRoles.Enemy(enemy), "idle",
                enemy.Animation.FacingDirection, enemy.Animation.FacingLeft) ?? clip;
            float top = standing == null ? ground.y - 30 * ProjectileVisuals.EnemyScale(enemy)
                : ground.y + (standing.OpaqueBounds.yMin - standing.GroundPivot.y) * scale - 9;
            float width = enemy.IsBoss ? 68 : 34;
            Fill(new Rect(ground.x - width / 2, top, width, 6), Ink);
            Fill(new Rect(ground.x - width / 2 + 1, top + 1, (width - 2) * Mathf.Clamp01(enemy.Hp / enemy.MaxHp), 4),
                enemy.IsBoss ? Red : enemy.Slow > 0 ? ManaBlue : Green);
            if (enemy.IsBoss) Txt("БОСС", ground.x - 40, top - 19, 80, 18, 11, Gold, FontStyle.Bold, TextAnchor.MiddleCenter);
            if (enemy.Slow > 0 || enemy.SkillSlowRemaining > 0)
                Fill(new Rect(ground.x - 11, ground.y + 6, 22, 2), ManaBlue);
        }

        private void DrawEnemyCorpse(EnemyCorpse corpse)
        {
            var clip = AnimationLibrary.GetDirectional(corpse.Role, corpse.Animation.Action,
                corpse.Animation.FacingDirection, corpse.Animation.FacingLeft);
            if (clip == null) return;
            float alpha = Mathf.Clamp01(corpse.Remaining / .5f);
            DrawAnimationFrame(clip, corpse.Position, clip.GroundPivot, corpse.Animation.ClipAge,
                AnimatedActors.EnemyPixelScale(corpse.Enemy), new Color(1, 1, 1, alpha), false);
        }

        private void DrawHeroCorpse(HeroCorpse corpse)
        {
            var clip = AnimationLibrary.GetDirectional(CombatAnimationRoles.Hero(corpse.Hero), "death",
                corpse.Animation.FacingDirection, corpse.Hero.FacingLeft);
            if (clip == null) return;
            DrawAnimationFrame(clip, corpse.Position, clip.GroundPivot, corpse.Animation.ClipAge,
                AnimatedActors.HeroPixelScale, new Color(.74f, .88f, 1, Mathf.Clamp01(corpse.Remaining / .5f)), false);
        }
    }
}
