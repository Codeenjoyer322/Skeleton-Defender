using UnityEngine;

namespace SkeletonDefender
{
    // World positions in the combat model are ground anchors. Keep their flight times intact,
    // and map the visible projectile from the weapon socket to the target's body separately.
    public sealed class ProjectileVisualState
    {
        public bool Initialized { get; private set; }
        public Vector2 Tip { get; private set; }
        public Vector2 Direction { get; private set; }
        private Vector2 originOffset;
        private float travelled;

        public void Begin(Vector2 logicalStart, Vector2 socket, Vector2 targetHitPoint)
        {
            Initialized = true;
            originOffset = socket - logicalStart;
            Tip = socket;
            Direction = ProjectileVisuals.Direction(targetHitPoint - socket);
            travelled = 0;
        }

        public void Advance(Vector2 previous, Vector2 position, Vector2 logicalTarget, Vector2 hitPoint, bool arrived)
        {
            travelled += Vector2.Distance(previous, position);
            float remaining = Vector2.Distance(position, logicalTarget);
            float progress = travelled + remaining <= .00001f ? 1 : travelled / (travelled + remaining);
            Vector2 tip = arrived ? hitPoint : position + Vector2.Lerp(originOffset, hitPoint - logicalTarget, progress);
            Direction = ProjectileVisuals.Direction(tip - Tip, hitPoint - tip);
            Tip = tip;
        }
    }

    public sealed class ProjectileImpact
    {
        public Vector2 Position, Direction;
        public string Kind;
        public bool Landed;
        public float Age;
        public float Lifetime = .4f;
        public float Life = .4f;
    }

    public static class ProjectileVisuals
    {
        [System.Serializable] private sealed class TowerSocketEntry
        {
            public int kind, level;
            public float offsetX, offsetY;
        }
        [System.Serializable] private sealed class TowerSocketCatalog
        {
            public int schemaVersion;
            public TowerSocketEntry[] sockets;
            public TowerVisualEntry[] towers;
        }
        [System.Serializable] private sealed class TowerMuzzle
        {
            public int index;
            public float pixelX, pixelY;
        }
        [System.Serializable] private sealed class TowerVisualEntry
        {
            public int kind, level, width, height;
            public float groundPivotX, groundPivotY, drawWidth, drawHeight;
            public float opaqueX, opaqueY, opaqueWidth, opaqueHeight;
            public float footprintX, footprintY, footprintWidth, footprintHeight;
            public TowerMuzzle[] sockets;
            public TowerArcherAnchor[] archers;
        }
        [System.Serializable] private sealed class TowerArcherAnchor
        {
            public int index;
            public float pixelX, pixelY, scale = 1;
        }
        private static TowerSocketCatalog towerSocketCatalog;
        private static bool towerSocketsLoaded;
        private static readonly Vector2[] CirceReadyCrystal = {
            new Vector2(87,47), new Vector2(87,47), new Vector2(89,47), new Vector2(88,47) };
        private static readonly Vector2[] CirceHurtCrystal = {
            new Vector2(87,45), new Vector2(87,44), new Vector2(88,45), new Vector2(88,47) };
        private static readonly Vector2[] CirceWalkCrystal = {
            new Vector2(87,47), new Vector2(84,46), new Vector2(83,46), new Vector2(82,47),
            new Vector2(81,47), new Vector2(82,47), new Vector2(84,47), new Vector2(86,46),
            new Vector2(88,45), new Vector2(89,46), new Vector2(90,47), new Vector2(88,47) };
        public static Vector2 Direction(Vector2 delta, Vector2 fallback = default)
        {
            if (delta.sqrMagnitude > .000001f) return delta.normalized;
            return fallback.sqrMagnitude > .000001f ? fallback.normalized : Vector2.right;
        }

        // The same authored socket and ground pivot used by the PNG player.
        public static Vector2 HeroSocket(HeroCombatState actor, bool staff)
        {
            // A delayed burst may release after another pose has started. Its origin must
            // come from the sprite actually being drawn, not the attack's old release pose.
            var clip = AnimatedActors.HeroClip(actor);
            if (clip != null)
            {
                Vector2 pixel = clip.ReleasePoint;
                if (staff)
                {
                    int frame = clip.FrameAt(actor.Animation.ClipAge, clip.Loop);
                    pixel = CirceStaffPixel(clip, frame);
                    pixel = clip.SocketAt("staff_head", frame, pixel);
                    pixel = clip.SocketAt("staffTip", frame, pixel);
                }
                else
                {
                    int frame = clip.FrameAt(actor.Animation.ClipAge, clip.Loop);
                    // New views provide the hand's actual location in each frame. The
                    // legacy fallback belongs only to the delivered E/W sword frames.
                    pixel = clip.Direction == "east" || clip.Direction == "west"
                        ? new Vector2(clip.Direction == "west" ? 43 : 84, 73) : clip.ReleasePoint;
                    pixel = clip.SocketAt("weapon_hand", frame, pixel);
                    pixel = clip.SocketAt("throw_hand", frame, pixel);
                }
                return AnimatedActors.HeroPoint(actor, clip, pixel);
            }
            Vector2 offset = staff ? new Vector2(20.3f, -43.451923f) : new Vector2(18.9f, -9.759615f);
            return HeroTransform(actor).MultiplyPoint3x4(actor.Position + offset);
        }

        // White crystal cores measured from the delivered PNGs. The small cyan flame
        // above the core is not the emission point. Action-specific metadata takes priority.
        public static Vector2 CirceStaffPixel(AnimationClipData clip, int frame)
        {
            Vector2[] points = clip.Action == "walk" ? CirceWalkCrystal :
                clip.Action == "hurt" ? CirceHurtCrystal : CirceReadyCrystal;
            Vector2 pixel = clip.Direction == "east" || clip.Direction == "west"
                ? points[Mathf.Clamp(frame, 0, points.Length - 1)] : clip.ReleasePoint;
            if (clip.Direction == "west") pixel.x = clip.Width - 1 - pixel.x;
            return clip.SocketAt("staffTip", frame, clip.SocketAt("staff_head", frame, pixel));
        }

        // The procedural actor and its sockets use the same transform, including a hit/knockdown
        // pose while a previously queued projectile is being released.
        public static Matrix4x4 HeroTransform(HeroCombatState actor)
        {
            float rotation = actor.KnockdownRemaining > 0 ? 75 : actor.Kind == HeroKind.Achilles && actor.CastPoseRemaining > 0 &&
                actor.LastCastName == BalanceData.Current.heroSystems.Skill(actor.Kind, 1).name ? 28 : 0;
            Vector3 pivot = actor.Position + new Vector2(0, -20);
            Matrix4x4 turn = Matrix4x4.Translate(pivot) * Matrix4x4.Rotate(Quaternion.Euler(0, 0, rotation)) * Matrix4x4.Translate(-pivot);
            Vector3 anchor = actor.Position;
            return turn * Matrix4x4.Translate(anchor) * Matrix4x4.Scale(new Vector3(actor.FacingLeft ? -1 : 1, 1, 1)) * Matrix4x4.Translate(-anchor);
        }

        public static Vector2 SpearHand(Vector2 anchor, bool facingLeft)
        {
            var clip = AnimationLibrary.Get("achilles", "divine_spear", facingLeft);
            return clip == null ? anchor + new Vector2(facingLeft ? -20 : 20, -55)
                : AnimatedActors.Point(clip, anchor, clip.ReleasePoint, AnimatedActors.HeroPixelScale);
        }
        public static float SpearFlightDuration(Vector2 groundStart, Vector2 chosenTarget, bool facingLeft)
            => SpearFlightDuration(SpearHand(groundStart, facingLeft), chosenTarget);
        public static float SpearFlightDuration(Vector2 launchPoint, Vector2 chosenTarget)
            => Mathf.Clamp(Vector2.Distance(launchPoint, chosenTarget) / 650f, .5f, .9f);

        // Capture this once when the cast releases. A flight must not switch sockets
        // when its caster turns, moves, or changes pose after letting the weapon go.
        public static Vector2 SkillReleasePoint(HeroCombatState actor, string action)
        {
            var clip = AnimationLibrary.GetDirectional(CombatAnimationRoles.Hero(actor), action,
                actor.Animation.FacingDirection, actor.FacingLeft);
            if (clip == null)
                return action == "heel_arrow" ? HeelArrowHand(actor.Position, actor.FacingLeft)
                    : SpearHand(actor.Position, actor.FacingLeft);

            int frame = clip.FrameAt(actor.Animation.ClipAge, false);
            Vector2 pixel = clip.ReleasePoint;
            pixel = clip.SocketAt("weapon_hand", frame, pixel);
            pixel = clip.SocketAt("throw_hand", frame, pixel);
            pixel = clip.SocketAt("projectile_release", frame, pixel);
            if (clip.Events != null)
                foreach (var cue in clip.Events)
                    if (cue.Name != null && cue.Name.IndexOf("release", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // The named release socket is authoritative for this ability;
                        // generic hand sockets may describe another part of the pose.
                        if (cue.HasPosition) pixel = cue.Position;
                        // A large simulation step may resolve the callback while the
                        // actor is already on a recovery frame. Freeze the authored cue.
                        if (!string.IsNullOrEmpty(cue.SocketName)) pixel = clip.SocketAt(cue.SocketName, cue.FrameIndex, pixel);
                        break;
                    }
            return AnimatedActors.HeroPoint(actor, clip, pixel);
        }
        public static Vector2 HeelArrowHand(Vector2 anchor, bool facingLeft)
        {
            var clip = AnimationLibrary.Get("achilles", "heel_arrow", facingLeft);
            return clip == null ? anchor + new Vector2(facingLeft ? -15 : 15, -50)
                : AnimatedActors.Point(clip, anchor, clip.ReleasePoint, AnimatedActors.HeroPixelScale);
        }
        public static Vector2 HeroHitPoint(HeroCombatState actor)
        {
            var clip = AnimatedActors.HeroClip(actor);
            return clip == null ? HeroTransform(actor).MultiplyPoint3x4(actor.Position + new Vector2(0, -27))
                : AnimatedActors.HeroPoint(actor, clip, clip.HitPoint);
        }
        public static float EnemyScale(Enemy enemy) => enemy.Variant.visualScale *
            (enemy.IsSummoned && enemy.Skeleton == SkeletonKind.Normal ? .75f : 1) * AnimatedActors.EnemySizeMultiplier(enemy);
        public static Vector2 EnemyHitPoint(Enemy enemy, Vector2 anchor)
        {
            var clip = AnimatedActors.EnemyClip(enemy);
            return clip == null ? anchor + new Vector2(0, -12 * EnemyScale(enemy))
                : AnimatedActors.Point(clip, anchor, clip.HitPoint, AnimatedActors.EnemyPixelScale(enemy));
        }
        public static Vector2 PirateSocket(Enemy enemy, Vector2 anchor)
            => EnemySkillReleasePoint(enemy, anchor, "pistol_shot", "release_projectile");

        public static Vector2 EnemySkillReleasePoint(Enemy enemy, Vector2 anchor, string action, string eventName)
        {
            string role = CombatAnimationRoles.Enemy(enemy);
            var clip = AnimationLibrary.GetDirectional(role, action, enemy.Animation.FacingDirection, enemy.Animation.FacingLeft);
            if (clip == null)
            {
                Vector2 offset = role == "pirate" ? new Vector2(enemy.Animation.FacingLeft ? -11 : 11, -10) * EnemyScale(enemy)
                    : new Vector2(enemy.Animation.FacingLeft ? -16 : 16, -35);
                return anchor + offset;
            }
            AnimationEventData cue = clip.FindEvent(eventName);
            Vector2 pixel = cue != null && cue.HasPosition ? cue.Position : clip.ReleasePoint;
            int releaseFrame = cue?.FrameIndex ?? clip.FrameAt(clip.ReleaseTime, false);
            string socketName = cue != null && !string.IsNullOrEmpty(cue.SocketName) ? cue.SocketName
                : role == "pirate" ? "pistol_muzzle" : "staff_head";
            pixel = clip.SocketAt(socketName, releaseFrame, pixel);
            return AnimatedActors.Point(clip, anchor, pixel, AnimatedActors.EnemyPixelScale(enemy));
        }
        public static Vector2 TowerSocket(Vector2 anchor) => anchor + new Vector2(0, -38);
        private static void LoadTowerGeometry()
        {
            if (!towerSocketsLoaded)
            {
                towerSocketsLoaded = true;
                TextAsset data = Resources.Load<TextAsset>("NeonGothic/tower-sockets");
                if (data != null) towerSocketCatalog = JsonUtility.FromJson<TowerSocketCatalog>(data.text);
            }
        }
        private static TowerVisualEntry TowerGeometry(TowerKind kind, int level)
        {
            LoadTowerGeometry();
            if (towerSocketCatalog?.schemaVersion == 2 && towerSocketCatalog.towers != null)
                foreach (var tower in towerSocketCatalog.towers)
                    if (tower.kind == (int)kind && tower.level == level && tower.width > 0 && tower.height > 0)
                        return tower;
            return null;
        }
        public static Rect TowerBounds(Vector2 ground, TowerKind kind, int level)
        {
            var tower = TowerGeometry(kind, level);
            if (tower == null) return new Rect(ground.x - 72, ground.y - 134, 144, 192);
            Vector2 scale = new Vector2(tower.drawWidth / tower.width, tower.drawHeight / tower.height);
            return new Rect(ground.x - tower.groundPivotX * scale.x, ground.y - tower.groundPivotY * scale.y,
                tower.drawWidth, tower.drawHeight);
        }
        // The site is the centre of the foundation, not its foremost bottom pixel.
        // The artwork, bow platforms, emitters and selection all share TowerBounds.
        public static Rect TowerFootprintBounds(Vector2 ground, TowerKind kind, int level)
        {
            var tower = TowerGeometry(kind, level);
            if (tower == null || tower.footprintWidth <= 0 || tower.footprintHeight <= 0)
                return new Rect(ground.x - 40, ground.y - 20, 80, 40);
            Rect bounds = TowerBounds(ground, kind, level);
            Vector2 scale = new Vector2(bounds.width / tower.width, bounds.height / tower.height);
            return new Rect(bounds.position + Vector2.Scale(new Vector2(tower.footprintX, tower.footprintY), scale),
                Vector2.Scale(new Vector2(tower.footprintWidth, tower.footprintHeight), scale));
        }
        public static Vector2 TowerSocket(Vector2 anchor, TowerKind kind, int level, int archerIndex = 0)
        {
            var tower = TowerGeometry(kind, level);
            if (tower?.sockets != null)
                foreach (var muzzle in tower.sockets)
                    if (muzzle.index == archerIndex)
                    {
                        Rect bounds = TowerBounds(anchor, kind, level);
                        return bounds.position + new Vector2(muzzle.pixelX * bounds.width / tower.width,
                            muzzle.pixelY * bounds.height / tower.height);
                    }
            if (towerSocketCatalog != null && towerSocketCatalog.schemaVersion == 1 && towerSocketCatalog.sockets != null)
                foreach (var socket in towerSocketCatalog.sockets)
                    if (socket.kind == (int)kind && socket.level == level)
                        return anchor + new Vector2(socket.offsetX, socket.offsetY) * 2;
            return anchor + (TowerSocket(anchor) - anchor) * 2;
        }
        public static Rect TowerSelectionBounds(Vector2 ground, TowerKind kind, int level)
        {
            var tower = TowerGeometry(kind, level);
            Rect bounds = TowerBounds(ground, kind, level);
            if (tower == null || tower.opaqueWidth <= 0 || tower.opaqueHeight <= 0) return bounds;
            Vector2 scale = new Vector2(bounds.width / tower.width, bounds.height / tower.height);
            Rect selected = new Rect(bounds.x + tower.opaqueX * scale.x, bounds.y + tower.opaqueY * scale.y,
                tower.opaqueWidth * scale.x, tower.opaqueHeight * scale.y);
            if (kind == TowerKind.Archer && tower.archers != null)
                foreach (var archer in tower.archers)
                    for (int index = 0; index < 8; index++)
                    {
                        var clip = AnimationLibrary.GetExact("tower_elf", "attack", (Direction8)index);
                        if (clip == null) continue;
                        float actorScale = TowerArcherScale(level, archer.index);
                        Vector2 feet = TowerArcherGround(ground, level, archer.index);
                        Rect opaque = clip.OpaqueBounds;
                        Rect actor = new Rect(feet + (opaque.position - clip.GroundPivot) * actorScale,
                            opaque.size * actorScale);
                        selected = Rect.MinMaxRect(Mathf.Min(selected.xMin, actor.xMin), Mathf.Min(selected.yMin, actor.yMin),
                            Mathf.Max(selected.xMax, actor.xMax), Mathf.Max(selected.yMax, actor.yMax));
                    }
            return selected;
        }
        public static Vector2 TowerArcherGround(Vector2 ground, int level, int index)
        {
            var tower = TowerGeometry(TowerKind.Archer, level);
            Rect bounds = TowerBounds(ground, TowerKind.Archer, level);
            if (tower?.archers != null)
                foreach (var archer in tower.archers)
                    if (archer.index == index)
                        return bounds.position + new Vector2(archer.pixelX * bounds.width / tower.width,
                            archer.pixelY * bounds.height / tower.height);
            return ground + new Vector2(index == 0 ? -18 : 18, -105);
        }
        public static float TowerArcherScale(int level, int index)
        {
            var tower = TowerGeometry(TowerKind.Archer, level);
            if (tower?.archers != null)
                foreach (var archer in tower.archers)
                    if (archer.index == index) return archer.scale * tower.drawWidth / tower.width;
            return 1;
        }
        public static Direction8 TowerArcherDirection(Vector2 ground, int level, int index, Vector2 target)
        {
            return AnimatedActors.DirectionFor(target - TowerArcherGround(ground, level, index), Direction8.South);
        }
        public static Vector2 TowerSocket(Vector2 ground, TowerKind kind, int level, int index, Vector2 target)
        {
            if (kind != TowerKind.Archer) return TowerSocket(ground, kind, level, index);
            Direction8 direction = TowerArcherDirection(ground, level, index, target);
            var clip = AnimationLibrary.GetDirectional("tower_elf", "attack", direction, target.x < ground.x);
            if (clip == null) return TowerSocket(ground, kind, level, index);
            var release = clip.FindEvent("release_projectile");
            int frame = release?.FrameIndex ?? clip.FrameAt(.2f, false);
            Vector2 pixel = clip.SocketAt("bow_muzzle", frame,
                release != null && release.HasPosition ? release.Position : clip.ReleasePoint);
            return AnimatedActors.Point(clip, TowerArcherGround(ground, level, index), pixel, TowerArcherScale(level, index));
        }
        public static Vector2 ArrowSky(Vector2 start) => start + new Vector2(0, -205);
        public static Vector2 RainTip(Vector2 start, Vector2 hitPoint, float progress) => Vector2.Lerp(ArrowSky(start), hitPoint, Mathf.Clamp01(progress));

        public static void Face(HeroCombatState actor, Vector2 target)
        {
            actor.Animation.FacingDirection = AnimatedActors.DirectionFor(target - actor.Position, actor.Animation.FacingDirection);
            float dx = target.x - actor.Position.x;
            if (Mathf.Abs(dx) > .001f) actor.FacingLeft = dx < 0;
        }
    }
}
