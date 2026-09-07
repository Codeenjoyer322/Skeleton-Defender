using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SkeletonDefender.Editor
{
    // Isolated deterministic combat fixtures. Never load or save the player's profile.
    public static class ProjectileTargetingTests
    {
        private static int checks;
        private static void Check(bool condition, string message)
        { checks++; if (!condition) throw new Exception("PROJECTILE TARGETING: " + message); }
        private static bool Near(Vector2 a, Vector2 b) => Vector2.Distance(a, b) < .001f;
        private static bool Near(float a, float b) => Mathf.Abs(a - b) < .001f;

        [MenuItem("Skeleton Defender/Check projectile targeting")]
        public static void Run()
        {
            checks = 0;
            BalanceData.Reload();
            Geometry();
            foreach (Vector2 side in new[] { Vector2.left, Vector2.right, Vector2.up, Vector2.down })
                foreach (float dt in new[] { 1f / 120, 1f / 30 }) MovingHeroProjectile(side, dt);
            MissingTargetAndPause();
            RainSnapshotAndContact();
            TowerContact();
            TowerArtworkSockets();
            TowerSocketFlightAndFrost();
            TwinArchersResolveIndependentShots();
            ArcherWindupCancellation();
            PirateNewLife();
            SpearPointAndStorm();
            EquipmentAndAbilitiesTests.Run();
            string report = "{\"status\":\"passed\",\"assertions\":" + checks + ",\"playerSaveTouched\":false,\"interactivePlaythrough\":false,\"existingEquipmentAndAbilitiesTests\":\"passed\"}";
            string path = Path.Combine(Path.GetDirectoryName(Application.dataPath), "work", "projectile-targeting-results.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, report);
            Debug.Log("SKELETON_PROJECTILE_TARGETING_PASSED: " + checks + " assertions; four approach sides, moving targets, different steps, exact visual contact, one damage event, lost targets, pause, overshoot, rain snapshot and hero life serial. No interactive playthrough or player save access.");
        }

        private static GameModel Fixture(HeroKind kind = HeroKind.Circe)
        {
            var model = new GameModel(1, kind, new EquipmentStats(), 71);
            model.StartWave();
            model.Hero.AttackCooldown = model.Hero.FrogCooldown = model.Hero.SunCooldown = model.Hero.DecapitateCooldown = 9999;
            model.Hero.Position = model.Hero.Destination = new Vector2(900, 600);
            return model;
        }
        private static Enemy Target(GameModel model, float distance = 200)
        {
            Enemy enemy = model.CreateEnemy(SkeletonKind.Normal, distance);
            enemy.Hp = enemy.MaxHp = 1000;
            enemy.AttackCooldown = enemy.SpecialCooldown = 9999;
            model.Enemies.Add(enemy); return enemy;
        }
        private static HeroProjectile Launch(GameModel model, Enemy target, float damage = 13)
        {
            var shot = new HeroProjectile { Start = model.Hero.Position, Position = model.Hero.Position,
                TargetId = target.Id, Source = model.Hero, Kind = ProjectileKind.Fireball, Damage = damage };
            model.Projectiles.Add(shot); return shot;
        }

        private static void Geometry()
        {
            var hero = new HeroCombatState { Kind = HeroKind.Circe, Hp = 100, MaxHp = 100, Position = new Vector2(300, 300) };
            hero.Animation.Play("staff_attack", 1.16f); hero.Animation.Advance(.55f);
            Vector2 right = ProjectileVisuals.HeroSocket(hero, true);
            var staffClip = AnimationLibrary.Get("circe", "staff_attack");
            Check(staffClip != null && Near(right, AnimatedActors.Point(staffClip, hero.Position,
                staffClip.ReleasePoint, AnimatedActors.HeroPixelScale)), "Staff socket disagrees with authored crystal release");
            hero.FacingLeft = true; hero.Animation.FacingDirection = Direction8.West;
            Vector2 left = ProjectileVisuals.HeroSocket(hero, true);
            var leftClip = AnimationLibrary.GetDirectional("circe", "staff_attack", Direction8.West, true);
            Check(leftClip != null && Near(left, AnimatedActors.Point(leftClip, hero.Position,
                leftClip.ReleasePoint, AnimatedActors.HeroPixelScale)), "Left staff socket disagrees with its own authored release");
            hero.KnockdownRemaining = 1;
            Vector2 transformedStaff = AnimatedActors.HeroPose(hero).MultiplyPoint3x4(left);
            Check(Near(ProjectileVisuals.HeroSocket(hero, true), transformedStaff), "Queued spell socket did not follow knocked-down sprite transform");
            hero.KnockdownRemaining = 0;
            foreach (Vector2 side in new[] { Vector2.left, Vector2.right, Vector2.up, Vector2.down, Vector2.one })
            {
                Vector2 start = new Vector2(300, 300), target = start + side * 200, hit = target + new Vector2(0, -24);
                var visual = new ProjectileVisualState();
                visual.Begin(start, right, hit);
                Check(Near(visual.Tip, right), "Flight did not start at socket");
                visual.Advance(start, target, target, hit, true);
                Check(Near(visual.Tip, hit), "Overshoot did not snap visible tip to target");
                Check(Near(visual.Direction.magnitude, 1), "Visual direction is not normalized");
                Check(Near(ProjectileVisuals.RainTip(start, hit, 1), hit), "Rain interpolation and target hit point differ");
            }
            Check(Near(ProjectileVisuals.Direction(Vector2.zero), Vector2.right), "Coincident points create invalid direction");
        }

        private static void MovingHeroProjectile(Vector2 side, float dt)
        {
            GameModel model = Fixture(); Enemy enemy = Target(model, 220);
            model.Hero.Position = model.Hero.Destination = model.Position(enemy.Distance) + side * 220;
            HeroProjectile shot = Launch(model, enemy);
            int damageEvents = 0, impactEvents = 0;
            for (int i = 0; i < 800 && model.Projectiles.Count > 0; i++)
            {
                if (i == 5) { enemy.SkillSlowRemaining = .12f; enemy.SkillSlowMultiplier = .5f; }
                if (i == 12) enemy.Distance += 17; // A target changes speed/position while the projectile is airborne.
                float hp = enemy.Hp;
                Vector2 previousLogical = shot.Position;
                // The impact belongs to the pose that was struck. Taking damage
                // immediately switches to a legacy hurt clip with its own chest
                // anchor; that later pose must not move an already completed hit.
                Vector2 struckPoseOffset = ProjectileVisuals.EnemyHitPoint(enemy, Vector2.zero);
                model.Step(dt);
                if (hp != enemy.Hp) damageEvents++;
                if (model.Projectiles.Count > 0)
                {
                    Check(Near(enemy.Hp, 1000), "Damage happened before arrival");
                    Check(Vector2.Distance(previousLogical, shot.Position) <= BalanceData.Current.heroRules.projectileSpeed * dt + .001f, "Visual change altered logical flight speed");
                    Check(Near(shot.Visual.Direction.magnitude, 1), "Direction failed for moving target");
                }
                else
                {
                    Check(model.ProjectileImpacts.Count == 1, "Arrival has no unique visible contact");
                    impactEvents += model.ProjectileImpacts.Count;
                    Vector2 hit = model.Position(enemy.Distance) + struckPoseOffset;
                    Check(Near(shot.Visual.Tip, hit) && Near(model.ProjectileImpacts[0].Position, hit), "Damage/visual contact was at a stale position");
                }
            }
            Check(model.Projectiles.Count == 0 && damageEvents == 1 && impactEvents == 1 && Near(enemy.Hp, 987), "Moving projectile did not arrive/damage exactly once");
            for (int i = 0; i < 20; i++) model.Step(dt);
            Check(Near(enemy.Hp, 987), "Removed projectile repeated its damage");
        }

        private static void MissingTargetAndPause()
        {
            GameModel model = Fixture(); Enemy enemy = Target(model); HeroProjectile shot = Launch(model, enemy);
            model.Step(.02f); Vector2 tip = shot.Visual.Tip, position = shot.Position;
            model.SetPaused(true); model.Step(3);
            Check(Near(shot.Position, position) && Near(shot.Visual.Tip, tip) && Near(enemy.Hp, 1000), "Pause moved projectile or dealt damage");
            model.SetPaused(false); model.Enemies.Remove(enemy); model.Step(.02f);
            Check(model.Projectiles.Count == 0 && model.ProjectileImpacts.Count == 0, "Disappeared target caused a phantom hit");
            GameModel overshoot = Fixture(); Enemy victim = Target(overshoot);
            overshoot.Hero.Position = overshoot.Hero.Destination = overshoot.Position(victim.Distance) + new Vector2(0, 100);
            HeroProjectile fast = Launch(overshoot, victim);
            Vector2 victimPoseOffset = ProjectileVisuals.EnemyHitPoint(victim, Vector2.zero);
            overshoot.Step(.8f);
            Check(overshoot.Projectiles.Count == 0 && Near(victim.Hp, 987), "Large tick missed or duplicated the impact");
            Check(Near(fast.Visual.Tip, overshoot.Position(victim.Distance) + victimPoseOffset), "Large tick overshot visible contact");
        }

        private static void RainSnapshotAndContact()
        {
            GameModel model = Fixture(HeroKind.Achilles); Enemy target = Target(model), vanished = Target(model, 300);
            Check(model.TryCastSkill(1), "Rain cast rejected");
            Enemy later = Target(model, 420); vanished.Dead = true;
            model.Step(1.18f / .7f + .02f); target.Distance += 24; model.Step(.02f);
            AbilityEffect arrow = model.AbilityEffects.Find(effect => effect.Kind == "arrow_rain");
            Check(arrow != null && Near(arrow.HitPoint, ProjectileVisuals.EnemyHitPoint(target, model.Position(target.Distance))), "Rain target point did not follow moving enemy");
            model.SetPaused(true); float age = arrow.Age; model.Step(2); model.SetPaused(false);
            Check(Near(age, arrow.Age), "Pause advanced rain");
            Vector2 struckPoseOffset = ProjectileVisuals.EnemyHitPoint(target, Vector2.zero);
            model.Step(arrow.Lifetime - arrow.Age + .001f);
            Check(Near(target.Hp, 990) && Near(later.Hp, 1000), "Rain lost original target snapshot or damaged twice");
            Check(model.ProjectileImpacts.Count == 1 && Near(model.ProjectileImpacts[0].Position, model.Position(target.Distance) + struckPoseOffset), "Rain contact did not meet current body position");
        }

        private static void TowerContact()
        {
            GameModel model = Fixture(); Enemy target = Target(model);
            var shot = new TowerProjectile { Start = model.Position(target.Distance) + new Vector2(0, 90),
                TargetId = target.Id, Damage = 7, Kind = TowerKind.Archer };
            shot.Position = shot.Start; model.TowerProjectiles.Add(shot);
            Vector2 struckPoseOffset = Vector2.zero;
            for (int i = 0; i < 100 && model.TowerProjectiles.Count > 0; i++)
            {
                struckPoseOffset = ProjectileVisuals.EnemyHitPoint(target, Vector2.zero);
                model.Step(.02f);
            }
            Check(model.TowerProjectiles.Count == 0 && Near(target.Hp, 993), "Tower projectile failed contact");
            Check(Near(shot.Visual.Tip, model.Position(target.Distance) + struckPoseOffset), "Tower arrow tip missed body");
        }

        [Serializable] private sealed class TowerSocketFixture
        { public int index; public float pixelX, pixelY; }
        [Serializable] private sealed class TowerArcherFixture
        { public int index, pixelX, pixelY; public float scale; }
        [Serializable] private sealed class TowerVisualFixture
        {
            public int kind, level, width, height;
            public float groundPivotX, groundPivotY, drawWidth, drawHeight;
            public int opaqueX, opaqueY, opaqueWidth, opaqueHeight;
            public TowerSocketFixture[] sockets;
            public TowerArcherFixture[] archers;
        }
        [Serializable] private sealed class TowerSocketFixtureCatalog
        { public int schemaVersion; public TowerVisualFixture[] towers; }
        private static void TowerArtworkSockets()
        {
            TextAsset manifest = Resources.Load<TextAsset>("NeonGothic/tower-sockets");
            Check(manifest != null, "Authored tower socket resource missing");
            var data = JsonUtility.FromJson<TowerSocketFixtureCatalog>(manifest.text);
            Check(data != null && data.schemaVersion == 2 && data.towers != null && data.towers.Length == 9, "New tower geometry catalog is incomplete");
            var visited = new System.Collections.Generic.HashSet<string>();
            Vector2 ground = new Vector2(231, 297);
            foreach (var tower in data.towers)
            {
                string key = tower.kind + "_" + tower.level;
                Check(tower.kind >= 0 && tower.kind < 3 && tower.level >= 1 && tower.level <= 3 && visited.Add(key), "Duplicate or invalid tower: " + key);
                Texture2D imported = Resources.Load<Texture2D>("NeonGothic/tower_" + key);
                Check(imported != null && imported.width == tower.width && imported.height == tower.height && imported.filterMode == FilterMode.Point,
                    "Tower artwork missing or wrong canvas/filter: " + key);
                Check(tower.sockets != null && tower.sockets.Length == (tower.kind == 0 && tower.level == 3 ? 2 : 1), "Missing individual elf bow / tower emitter: " + key);
                Rect bounds = ProjectileVisuals.TowerBounds(ground, (TowerKind)tower.kind, tower.level);
                Vector2 scale = new Vector2(tower.drawWidth / tower.width, tower.drawHeight / tower.height);
                Check(Near(bounds.position, ground - Vector2.Scale(new Vector2(tower.groundPivotX, tower.groundPivotY), scale)) &&
                    Near(bounds.size, new Vector2(tower.drawWidth, tower.drawHeight)), "Tower placement disagrees with authored ground pivot: " + key);
                var decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    Check(ImageConversion.LoadImage(decoded, File.ReadAllBytes(AssetDatabase.GetAssetPath(imported))), "Tower PNG cannot be decoded: " + key);
                    int x0=decoded.width, y0=decoded.height, x1=-1, y1=-1;
                    for (int y=0; y<decoded.height; y++) for(int x=0; x<decoded.width; x++)
                        if(decoded.GetPixel(x,decoded.height-1-y).a > 0) { x0=Math.Min(x0,x); x1=Math.Max(x1,x); y0=Math.Min(y0,y); y1=Math.Max(y1,y); }
                    Check(x0==tower.opaqueX && y0==tower.opaqueY && x1-x0+1==tower.opaqueWidth && y1-y0+1==tower.opaqueHeight,
                        "Selection geometry does not cover the actual opaque tower pixels: " + key);
                    Rect selection = ProjectileVisuals.TowerSelectionBounds(ground,(TowerKind)tower.kind,tower.level);
                    Rect baseOpaque = new Rect(bounds.position+Vector2.Scale(new Vector2(x0,y0),scale),Vector2.Scale(new Vector2(x1-x0+1,y1-y0+1),scale));
                    Check(ContainsBounds(selection,baseOpaque), "Tower selection misses the displayed base silhouette: " + key);
                    for(int index=0; index<tower.sockets.Length; index++)
                    {
                        var socket=tower.sockets[index];
                        Check(socket.index==index && socket.pixelX>=0 && socket.pixelX<decoded.width && socket.pixelY>=0 && socket.pixelY<decoded.height,
                            "Emitter index or pixel lies outside the tower: " + key);
                        if(tower.kind!=0) Check(decoded.GetPixel(Mathf.RoundToInt(socket.pixelX),decoded.height-1-Mathf.RoundToInt(socket.pixelY)).a>.99f, "Emitter is a transparent pixel: " + key);
                        Vector2 expected=bounds.position+Vector2.Scale(new Vector2(socket.pixelX,socket.pixelY),scale);
                        Check(Near(ProjectileVisuals.TowerSocket(ground,(TowerKind)tower.kind,tower.level,index),expected), "Arrow / spell does not originate from its own visible emitter: " + key);
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(decoded); }
                if(tower.kind==0) VerifyElfSockets(tower,ground,bounds,scale);
            }
            Check(Near(ProjectileVisuals.TowerSocket(ground), ground+new Vector2(0,-38)), "Legacy ground-only emitter fallback changed");
        }
        private static object CallTower(GameModel model, string method, params object[] args)
            => typeof(GameModel).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(model, args);
        private static void ReleaseArchers(GameModel model, float seconds = .2f)
        {
            CallTower(model, "AdvanceAnimations", seconds);
            CallTower(model, "UpdateAnimationContacts", seconds);
            CallTower(model, "UpdateTowerProjectiles", 0f);
            CallTower(model, "UpdateTowers", seconds);
        }
        private static bool ContainsBounds(Rect outer, Rect inner)
            => outer.xMin<=inner.xMin+.001f && outer.yMin<=inner.yMin+.001f && outer.xMax>=inner.xMax-.001f && outer.yMax>=inner.yMax-.001f;
        private static void VerifyElfSockets(TowerVisualFixture tower, Vector2 ground, Rect bounds, Vector2 scale)
        {
            Check(tower.archers!=null && tower.archers.Length==(tower.level==3?2:1), "Missing individual archer platform anchors");
            foreach(var archer in tower.archers)
            {
                Vector2 feet=bounds.position+Vector2.Scale(new Vector2(archer.pixelX,archer.pixelY),scale);
                Check(Near(ProjectileVisuals.TowerArcherGround(ground,tower.level,archer.index),feet),"Elf feet do not match its tower platform");
                foreach(Direction8 direction in Enum.GetValues(typeof(Direction8)))
                {
                    var idle=AnimationLibrary.GetExact("tower_elf","idle",direction);
                    var clip=AnimationLibrary.GetExact("tower_elf","attack",direction);
                    Check(idle!=null && clip!=null && idle.Texture!=null && clip.Texture!=null && !idle.FlipX && !clip.FlipX, "Missing true elf idle/attack direction: "+direction);
                    var cue=clip.FindEvent("release_projectile");
                    Check(cue!=null && cue.HasPosition && Near(cue.TimeSeconds,.2f) && Near(clip.Duration,.5f) && Near(clip.ContactTime,.2f),"Elf bow release metadata changed windup or interval");
                    int frameIndex=clip.FrameAt(.2f,false);
                    Check(frameIndex==cue.FrameIndex,"Bow release event and displayed frame disagree");
                    Vector2 pixel=clip.SocketAt("bow_muzzle",frameIndex,cue.Position);var frame=clip.Frames[frameIndex];
                    int x=Mathf.RoundToInt(pixel.x)-frame.sourceX,y=Mathf.RoundToInt(pixel.y)-frame.sourceY;
                    Check(x>=0 && y>=0 && x<frame.Width && y<frame.Height,"Elf release socket is outside the actual trimmed frame");
                    var png=new Texture2D(2,2,TextureFormat.RGBA32,false);
                    try
                    {
                        Check(ImageConversion.LoadImage(png,File.ReadAllBytes(AssetDatabase.GetAssetPath(clip.Texture))),"Elf atlas failed decoding");
                        Check(png.GetPixel(frame.X+x,png.height-1-frame.Y-y).a>.99f,"Elf arrow release is not on an opaque bow/arrow pixel");
                    }
                    finally { UnityEngine.Object.DestroyImmediate(png); }
                    float angle=(int)direction*Mathf.PI/4;Vector2 target=feet+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*150;
                    float actorScale=archer.scale*scale.x;Vector2 expected=feet+(pixel-clip.GroundPivot)*actorScale;
                    Check(Near(ProjectileVisuals.TowerSocket(ground,TowerKind.Archer,tower.level,archer.index,target),expected),"Elf release socket is not transformed from its actual aiming view");
                    Rect actorBounds=new Rect(feet+(clip.OpaqueBounds.position-clip.GroundPivot)*actorScale,clip.OpaqueBounds.size*actorScale);
                    Check(ContainsBounds(ProjectileVisuals.TowerSelectionBounds(ground,TowerKind.Archer,tower.level),actorBounds),"Tower selection omits an aiming elf silhouette");
                }
            }
        }
        private static void TowerSocketFlightAndFrost()
        {
            foreach (TowerKind kind in Enum.GetValues(typeof(TowerKind)))
                for (int level = 1; level <= 3; level++)
                {
                    var model = Fixture(); var enemy = Target(model, NearestRoadDistance(model, GameModel.Sites[0]));
                    var tower = new Tower { Site = 0, Kind = kind, Level = level };
                    model.Towers.Add(tower);
                    Vector2 ground = GameModel.Sites[0], hit = ProjectileVisuals.EnemyHitPoint(enemy, model.Position(enemy.Distance));
                    CallTower(model, "UpdateTowers", 0f);
                    if (kind == TowerKind.Archer) ReleaseArchers(model);
                    float damage = tower.Damage * (kind == TowerKind.Archer ? 1 - enemy.PhysicalArmor : 1);
                    if (kind == TowerKind.Frost)
                    {
                        Check(model.Shots.Count == 1 && model.TowerProjectiles.Count == 0, "Frost changed from an instant beam");
                        Shot shot = model.Shots[0];
                        Check(shot.UsesVisualAnchors && Near(shot.Start, ProjectileVisuals.TowerSocket(ground, kind, level)) && Near(shot.End, hit),
                            "Frost beam endpoints miss tower crystal or enemy body");
                        Check(Near(enemy.Hp, 1000 - damage) && enemy.Slow > 0, "Frost visual adjustment changed damage or slow");
                        continue;
                    }
                    Check(model.TowerProjectiles.Count == tower.ProjectileCount, "Tower did not launch its individual archer projectiles");
                    // The updater visits the last projectile first. Its hit uses the
                    // original target pose; the second arrow may already see the hurt pose.
                    var projectile = model.TowerProjectiles[model.TowerProjectiles.Count - 1];
                    Check(projectile.Level == level && projectile.Visual.Initialized && Near(projectile.Visual.Tip, kind == TowerKind.Archer ? ProjectileVisuals.TowerSocket(ground, kind, level, projectile.VisualSocketIndex, hit) : ProjectileVisuals.TowerSocket(ground, kind, level)),
                        "New tower shot did not appear at its level-specific emitter before first flight step");
                    Check(Near(projectile.Start, ground) && Near(projectile.Position, ground) && Near(enemy.Hp, 1000), "Tower emitter changed logical ground origin or hit immediately");
                    float seconds = Vector2.Distance(ground, model.Position(enemy.Distance)) / BalanceData.Current.heroSystems.towerProjectileSpeed;
                    CallTower(model, "UpdateTowerProjectiles", seconds - .001f);
                    Check(model.TowerProjectiles.Count == tower.ProjectileCount && Near(enemy.Hp, 1000), "Artwork socket shortened logical flight time");
                    CallTower(model, "UpdateTowerProjectiles", .002f);
                    Check(model.TowerProjectiles.Count == 0 && Near(enemy.Hp, 1000 - damage * tower.ProjectileCount) && Near(projectile.Visual.Tip, hit), "Tower arrival missed body or changed per-arrow damage");
                    CallTower(model, "UpdateTowerProjectiles", 1f);
                    Check(Near(enemy.Hp, 1000 - damage * tower.ProjectileCount) && model.ProjectileImpacts.Count == tower.ProjectileCount, "Tower arrival duplicated damage/impact");
                }
        }

        private static float NearestRoadDistance(GameModel model, Vector2 point)
        {
            float result = 0, best = float.MaxValue;
            for (float distance = 0; distance <= model.PathLength; distance += 1)
            {
                float candidate = Vector2.SqrMagnitude(model.Position(distance) - point);
                if (candidate < best) { best = candidate; result = distance; }
            }
            return result;
        }
        private static void TwinArchersResolveIndependentShots()
        {
            for (int level = 1; level <= 3; level++)
            {
                var model = Fixture(); var tower = new Tower { Site = 0, Kind = TowerKind.Archer, Level = level };
                model.Towers.Add(tower); Enemy target = Target(model, NearestRoadDistance(model, GameModel.Sites[0]));
                int expected = level == 3 ? 2 : 1;
                CallTower(model, "UpdateTowers", 0f);
                Check(model.TowerProjectiles.Count == 0 && model.PendingAnimationContacts == expected, "Archer skipped bow preparation");
                ReleaseArchers(model);
                Check(model.TowerProjectiles.Count == expected && Near(target.Hp, 1000), "Archer level changed its individual shot count or hit before flight");
                for (int i = 0; i < expected; i++)
                {
                    var shot = model.TowerProjectiles[i];
                    int index = shot.VisualSocketIndex;
                    Check(index >= 0 && index < expected && Near(shot.Damage, tower.Damage), "An elf's arrow split or duplicated the old level damage");
                    Check(Near(shot.Visual.Tip, ProjectileVisuals.TowerSocket(GameModel.Sites[0], TowerKind.Archer, level, index, ProjectileVisuals.EnemyHitPoint(target,model.Position(target.Distance)))), "Arrow did not start at its own elf bow");
                }
                if (level == 3) Check(!Near(model.TowerProjectiles[0].Visual.Tip, model.TowerProjectiles[1].Visual.Tip), "Twin elves share one visual emitter");
                model.SetPaused(true); model.Step(10);
                Check(model.TowerProjectiles.Count == expected && Near(target.Hp, 1000), "Pause advanced archer shots"); model.SetPaused(false);
                CallTower(model, "UpdateTowerProjectiles", 2f);
                float totalDamage = expected * tower.Damage * (1 - target.PhysicalArmor);
                Check(Near(target.Hp, 1000 - totalDamage) && model.ProjectileImpacts.Count == expected, "Archer pair did not land two full independent arrows");
                CallTower(model, "UpdateTowers", tower.Cooldown - .001f);
                Check(model.TowerProjectiles.Count == 0, "Second volley shortened the original per-elf attack interval");
                CallTower(model, "UpdateTowers", .002f);
                Check(model.TowerProjectiles.Count == 0 && model.PendingAnimationContacts == expected, "Repeated archer attack skipped its release marker");
                ReleaseArchers(model);
                Check(model.TowerProjectiles.Count == expected, "Pair did not repeat at the original attack interval");
                int kills = model.Kills, gold = model.Gold;
                target.Hp = .1f;
                CallTower(model, "UpdateTowerProjectiles", 2f);
                Check(target.Dead && model.Kills == kills + 1 && model.Gold == gold + target.Reward && model.TowerProjectiles.Count == 0,
                    "Two arrows duplicated a lethal reward or retained a phantom target");
            }
            var ninjaModel = Fixture(); var pair = new Tower { Site = 0, Kind = TowerKind.Archer, Level = 3 }; ninjaModel.Towers.Add(pair);
            Enemy ninja = ninjaModel.CreateEnemy(SkeletonKind.Ninja, NearestRoadDistance(ninjaModel, GameModel.Sites[0]));
            ninja.Hp = ninja.MaxHp = 1000; ninja.DodgeReady = true; ninjaModel.Enemies.Add(ninja);
            CallTower(ninjaModel, "UpdateTowers", 0f); ReleaseArchers(ninjaModel); CallTower(ninjaModel, "UpdateTowerProjectiles", 2f);
            Check(Near(ninja.Hp, 1000 - pair.Damage * (1 - ninja.PhysicalArmor)) && !ninja.DodgeReady &&
                ninjaModel.ProjectileImpacts.FindAll(impact => impact.Landed).Count == 1 && ninjaModel.ProjectileImpacts.FindAll(impact => !impact.Landed).Count == 1,
                "A single ready dodge must evade only one elf's arrow, leaving the second to hit");
        }
        private static void ArcherWindupCancellation()
        {
            foreach (string cancel in new[] { "sale", "target", "upgrade" })
            {
                var model=Fixture();var tower=new Tower {Site=0,Kind=TowerKind.Archer,Level=2};model.Towers.Add(tower);
                Enemy target=Target(model,NearestRoadDistance(model,GameModel.Sites[0]));
                CallTower(model,"UpdateTowers",0f);ReleaseArchers(model,.199f);
                Check(model.TowerProjectiles.Count==0 && Near(target.Hp,1000),"Arrow left the bow before the 0.20-second contact");
                if(cancel=="sale") Check(model.Sell(0),"Sale fixture rejected");
                else if(cancel=="target") model.Enemies.Remove(target);
                else tower.Level++;
                ReleaseArchers(model,.002f);
                Check(model.TowerProjectiles.Count==0 && model.PendingAnimationContacts==0 && Near(target.Hp,1000),"Canceled archer preparation still released a projectile: "+cancel);
            }
            var released=Fixture();var pair=new Tower {Site=0,Kind=TowerKind.Archer,Level=3};released.Towers.Add(pair);
            Enemy survivor=Target(released,NearestRoadDistance(released,GameModel.Sites[0]));
            CallTower(released,"UpdateTowers",0f);ReleaseArchers(released);
            Check(released.Sell(0),"Released tower sale rejected");CallTower(released,"UpdateTowerProjectiles",2f);
            Check(Near(survivor.Hp,1000-2*pair.Damage*(1-survivor.PhysicalArmor)),"Selling a tower canceled already released arrows");

            var delayed=Fixture();var archerTower=new Tower {Site=0,Kind=TowerKind.Archer};delayed.Towers.Add(archerTower);
            Enemy mover=Target(delayed,NearestRoadDistance(delayed,GameModel.Sites[0]));
            CallTower(delayed,"UpdateTowers",0f);mover.Distance+=90;
            Vector2 endpoint=ProjectileVisuals.EnemyHitPoint(mover,delayed.Position(mover.Distance));
            Vector2 expectedSocket=ProjectileVisuals.TowerSocket(GameModel.Sites[0],TowerKind.Archer,1,0,endpoint);
            CallTower(delayed,"AdvanceAnimations",.3f);CallTower(delayed,"UpdateAnimationContacts",.3f);
            Check(delayed.TowerProjectiles.Count==1,"Overdue bow contact lost or duplicated a shot");
            var overdue=delayed.TowerProjectiles[0];
            Check(Near(overdue.Visual.Tip,expectedSocket),"Overdue release used an old target angle or recovery bow position");
            CallTower(delayed,"UpdateTowerProjectiles",.3f);
            Check(Near(overdue.Age,.1f) && Near(Vector2.Distance(overdue.Position,GameModel.Sites[0]),28f) && Near(mover.Hp,1000),
                "A large step consumed pre-release time as projectile flight");
        }
        private static void PirateNewLife()
        {
            GameModel model = Fixture();
            var bullet = new EnemyProjectile { Target = model.Hero, TargetLifeSerial = model.Hero.LifeSerial,
                Position = model.Hero.Position + new Vector2(40, 0), Speed = 300, Damage = 10 };
            model.EnemyProjectiles.Add(bullet); model.Hero.LifeSerial++; model.Step(.5f);
            Check(model.EnemyProjectiles.Count == 0 && Near(model.Hero.Hp, model.Hero.MaxHp) && model.ProjectileImpacts.Count == 0, "Old projectile hit a new hero life");
            bullet = new EnemyProjectile { Target = model.Hero, TargetLifeSerial = model.Hero.LifeSerial,
                Position = model.Hero.Position + new Vector2(0, -40), Speed = 300, Damage = 10 };
            Vector2 struckHeroPoint = ProjectileVisuals.HeroHitPoint(model.Hero);
            model.EnemyProjectiles.Add(bullet); model.Step(.2f);
            Check(Near(model.Hero.Hp, model.Hero.MaxHp - 10) && Near(bullet.Visual.Tip, struckHeroPoint), "Pirate bullet damage and body contact differ");
        }

        private static void SpearPointAndStorm()
        {
            GameModel spear = Fixture(HeroKind.Achilles); Enemy enemy = Target(spear);
            Vector2 point = spear.Position(enemy.Distance);
            Check(spear.TryCastSkill(0, point), "Spear cast rejected");
            float flight = ProjectileVisuals.SpearFlightDuration(spear.Hero.Position, point, spear.Hero.FacingLeft);
            enemy.Distance += 200; spear.Step(.72f + flight + .01f);
            Check(Near(enemy.Hp, 1000) && Near(spear.ProjectileImpacts[0].Position, point), "Spear incorrectly homed instead of hitting clicked area");
            GameModel storm = Fixture(); Enemy target = Target(storm);
            Check(storm.TryCastSkill(1) && Near(target.Hp, 1000), "Storm damaged before the cast marker");
            storm.Step(.35f);
            Vector2 hit = ProjectileVisuals.EnemyHitPoint(target, storm.Position(target.Distance));
            Check(Near(target.Hp, 1000), "Storm dealt damage before its first distributed tick");
            AbilityEffect stormEffect = storm.AbilityEffects.Find(effect => effect.Kind == "storm");
            Check(stormEffect != null && Near(stormEffect.Targets[0], hit), "Storm did not capture its release endpoint");
            storm.Step(3);
            Check(Near(target.Hp, 990) && stormEffect.DamageTicksResolved == 4, "Storm did not deliver exactly its original total over three seconds");
        }
    }
}
