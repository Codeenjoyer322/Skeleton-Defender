using System;
using UnityEngine;

namespace SkeletonDefender
{
    // Small, original bitmap assets generated from a fixed palette. Replace these
    // textures with Aseprite exports without changing the simulation or UI flow.
    public static class PixelArt
    {
        public static Color C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out Color c); return c; }
        private sealed class Canvas
        {
            public readonly int W, H;
            private readonly Color32[] pixels;
            public Canvas(int w, int h, string fill = null) { W = w; H = h; pixels = new Color32[w * h]; if (fill != null) R(0, 0, w, h, fill); }
            public void P(int x, int y, Color c) { if (x >= 0 && x < W && y >= 0 && y < H) pixels[(H - 1 - y) * W + x] = c; }
            public void R(int x, int y, int w, int h, string hex)
            {
                Color c = C(hex);
                for (int iy = Mathf.Max(0, y); iy < Mathf.Min(H, y + h); iy++)
                    for (int ix = Mathf.Max(0, x); ix < Mathf.Min(W, x + w); ix++) P(ix, iy, c);
            }
            public void E(int x, int y, int rx, int ry, string hex)
            {
                Color c = C(hex);
                for (int iy = -ry; iy <= ry; iy++) for (int ix = -rx; ix <= rx; ix++)
                    if (ix * ix / (float)(rx * rx) + iy * iy / (float)(ry * ry) <= 1) P(x + ix, y + iy, c);
            }
            public void L(Vector2 a, Vector2 b, int width, string hex)
            {
                int steps = Mathf.CeilToInt(Vector2.Distance(a, b));
                for (int i = 0; i <= steps; i++) { Vector2 p = Vector2.Lerp(a, b, i / (float)Mathf.Max(1, steps)); E((int)p.x, (int)p.y, width, width, hex); }
            }
            public void Roof(int x, int y, int width, int height, string hex)
            { for (int i = 0; i < height; i++) { int half = Mathf.RoundToInt(width * .5f * i / height); R(x - half, y + i, half * 2 + 1, 1, hex); } }
            public Texture2D Texture(string name)
            {
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = name };
                tex.SetPixels32(pixels); tex.Apply(false, true); return tex;
            }
        }
        private static void Tree(Canvas c, int x, int y, int s, bool dark = false)
        {
            c.E(x, y + 3 * s, 10 * s, 4 * s, "101e22");
            c.R(x - 2 * s, y - 8 * s, 4 * s, 13 * s, "403c37");
            string back = dark ? "10242b" : "173d3b", front = dark ? "163039" : "245047";
            for (int j = 0; j < 3; j++)
            {
                int cy = y - 34 * s + j * 9 * s;
                c.Roof(x, cy, (19 + j * 4) * s, 18 * s, back);
                c.Roof(x - 2 * s, cy + 2 * s, (10 + j * 3) * s, 14 * s, front);
            }
        }
        private static void Castle(Canvas c, int x, int y, int s)
        {
            c.E(x + 34 * s, y + 77 * s, 49 * s, 12 * s, "101d24");
            c.R(x - 9 * s, y + 70 * s, 87 * s, 10 * s, "333e40");
            c.R(x + 4 * s, y + 24 * s, 61 * s, 50 * s, "69716a");
            c.R(x + 5 * s, y + 26 * s, 29 * s, 46 * s, "818476");
            for (int j = 0; j < 6; j++)
            {
                c.R(x + 4 * s, y + (29 + j * 8) * s, 61 * s, s, "515e5d");
                for (int k = 0; k < 5; k++) c.R(x + (7 + k * 13 + (j % 2) * 5) * s, y + (30 + j * 8) * s, s, 7 * s, "5e6a63");
            }
            for (int i = 0; i < 2; i++)
            {
                int tx = x + (i == 0 ? -5 : 51) * s;
                c.R(tx, y + 6 * s, 23 * s, 67 * s, "434f53");
                c.R(tx + 2 * s, y + 7 * s, 10 * s, 64 * s, "7d8176");
                for (int j = 0; j < 8; j++) c.R(tx + s, y + (13 + j * 8) * s, 21 * s, s, "36474b");
                c.Roof(tx + 11 * s, y - 18 * s, 36 * s, 27 * s, "223f43");
                c.Roof(tx + 8 * s, y - 15 * s, 22 * s, 23 * s, "37605a");
                c.R(tx + 8 * s, y + 20 * s, 7 * s, 13 * s, "17262c");
                c.R(tx + 9 * s, y + 21 * s, 3 * s, 9 * s, "e5b964");
                c.R(tx + 10 * s, y - 31 * s, s, 16 * s, "adab8a");
                c.R(tx + 11 * s, y - 30 * s, 13 * s, 7 * s, "c99959");
                c.R(tx + 22 * s, y - 28 * s, 5 * s, 5 * s, "ac7146");
            }
            for (int i = 0; i < 6; i++) c.R(x + (17 + i * 6) * s, y + 20 * s, 4 * s, 7 * s, "9b9a81");
            c.R(x + 26 * s, y + 44 * s, 19 * s, 31 * s, "182529");
            c.E(x + 35 * s, y + 45 * s, 9 * s, 9 * s, "182529");
            c.R(x + 29 * s, y + 48 * s, 12 * s, 27 * s, "4c4639");
            for (int i = 0; i < 4; i++) c.R(x + (29 + i * 4) * s, y + 48 * s, s, 27 * s, "282d2b");
            for (int i = 0; i < 2; i++)
            {
                int fx = x + (i == 0 ? 21 : 48) * s;
                c.R(fx, y + 49 * s, 2 * s, 11 * s, "382f2b");
                c.E(fx + s, y + 46 * s, 3 * s, 5 * s, "be6942");
                c.R(fx, y + 43 * s, 2 * s, 5 * s, "f3d083");
            }
        }
        public static Texture2D Map()
        {
            var c = new Canvas(528, 320, "263d38"); var rng = new System.Random(416);
            for (int i = 0; i < 950; i++)
            {
                int x = rng.Next(528), y = rng.Next(320);
                c.R(x, y, rng.Next(2, 9), rng.Next(1, 4), i % 3 == 0 ? "2c443c" : "223932");
            }
            // A small moonlit pond in the south-west corner.
            c.E(109, 276, 53, 24, "1b302f"); c.E(109, 275, 45, 19, "213e45");
            c.E(111, 273, 36, 15, "294c50");
            for (int i = 0; i < 12; i++) c.R(79 + rng.Next(63), 263 + rng.Next(23), rng.Next(3, 12), 1, "386461");
            for (int i = 0; i < GameModel.Path.Length - 1; i++) c.L(GameModel.Path[i] / 2, GameModel.Path[i + 1] / 2, 17, "1b2d2c");
            for (int i = 0; i < GameModel.Path.Length - 1; i++) c.L(GameModel.Path[i] / 2, GameModel.Path[i + 1] / 2, 14, "635c46");
            for (int i = 0; i < GameModel.Path.Length - 1; i++) c.L(GameModel.Path[i] / 2, GameModel.Path[i + 1] / 2, 11, "827151");
            var model = new GameModel();
            for (int i = 0; i < 460; i++)
            {
                Vector2 p = model.Position((float)rng.NextDouble() * model.PathLength) / 2;
                c.R((int)p.x + rng.Next(-10, 11), (int)p.y + rng.Next(-10, 11), rng.Next(1, 4), 1, i % 3 == 0 ? "9e8961" : "6e6049");
            }
            for (int i = 0; i < 94; i++)
            {
                int x = rng.Next(4, 525), y = rng.Next(18, 316);
                Vector2 p = new Vector2(x * 2, y * 2);
                bool avoid = y > 248 && x < 165 || x > 444 && y > 55 && y < 191;
                foreach (Vector2 site in GameModel.Sites) if (Vector2.Distance(p, site) < 68) avoid = true;
                for (float d = 0; d < model.PathLength; d += 14) if (Vector2.Distance(model.Position(d), p) < 62) avoid = true;
                if (!avoid) Tree(c, x, y, 1);
            }
            // Ruins, gravestones and tiny flowers give landmarks without hiding the road.
            for (int i = 0; i < 16; i++)
            {
                int x = 335 + rng.Next(39), y = 140 + rng.Next(51);
                c.R(x, y, 5, 8, "64716a"); c.R(x + 1, y - 1, 3, 1, "818a79");
                c.R(x + 2, y + 2, 1, 4, "394e49"); c.R(x + 1, y + 3, 3, 1, "394e49");
            }
            for (int i = 0; i < 35; i++) { int x = rng.Next(528), y = rng.Next(320); c.R(x, y, 1, 2, "94a58b"); }
            c.R(14, 47, 3, 19, "594c39"); c.R(9, 48, 23, 10, "b09a66"); c.R(12, 51, 14, 2, "493f32");
            Castle(c, 461, 76, 1);
            return c.Texture("Forest battlefield");
        }
        public static Texture2D Menu()
        {
            var c = new Canvas(480, 300, "101e29"); var rng = new System.Random(106);
            c.R(0, 110, 480, 75, "172a33"); c.R(0, 185, 480, 115, "1d3436");
            for (int i = 0; i < 65; i++) { int x = rng.Next(480), y = rng.Next(10, 125); c.R(x, y, 1, 1, "506369"); }
            c.E(380, 50, 28, 28, "263f48"); c.E(380, 50, 22, 22, "697e7c"); c.E(380, 50, 19, 19, "c3c5a5");
            c.E(374, 43, 4, 3, "a5b39d"); c.E(386, 54, 3, 5, "a5b39d");
            for (int i = 0; i < 14; i++) c.Roof(i * 46 - 10, 95 + rng.Next(25), 120, 80, "1b3039");
            for (int i = 0; i < 19; i++) Tree(c, i * 29, 188 + rng.Next(30), 2, true);
            c.E(359, 283, 162, 57, "293d37"); c.E(359, 281, 137, 43, "374a3d");
            c.L(new Vector2(274, 303), new Vector2(355, 225), 17, "675e47");
            c.L(new Vector2(274, 303), new Vector2(355, 225), 12, "8a7854");
            Castle(c, 294, 117, 2);
            for (int i = 0; i < 200; i++)
            {
                int x = rng.Next(245, 480), y = rng.Next(256, 300); c.R(x, y, 2, 1, i % 4 == 0 ? "9b9c66" : "425643");
            }
            Tree(c, 466, 285, 3); Tree(c, 225, 266, 2, true); Tree(c, 445, 309, 2, true);
            c.R(0, 280, 480, 20, "14282b");
            for (int i = 0; i < 24; i++) c.Roof(i * 24, 268 + rng.Next(18), 26, 32, "13272a");
            return c.Texture("Moonlit fortress");
        }
        public static Texture2D Pad()
        {
            var c = new Canvas(34, 24);
            c.E(17, 15, 16, 8, "142725"); c.E(17, 12, 15, 8, "687164"); c.E(17, 11, 14, 7, "8b9076");
            c.E(17, 11, 11, 5, "3e5144"); c.E(17, 12, 9, 4, "304537");
            for (int i = 0; i < 5; i++) c.R(4 + i * 6, 12 + (i % 2) * 3, 1, 4, "4e5c50");
            return c.Texture("Build site");
        }
        public static Texture2D Tower(TowerKind kind, int level)
        {
            var c = new Canvas(36, 48);
            c.E(18, 41, 17, 6, "142422"); c.R(6, 35, 24, 8, "464f48"); c.R(7, 33, 22, 6, "a29d7d");
            c.R(9, 22, 18, 13, "6b766a"); c.R(9, 22, 7, 13, "92957b");
            for (int y = 24; y < 35; y += 5) { c.R(9, y, 18, 1, "4f6158"); c.R(14 + y % 3 * 3, y - 3, 1, 4, "4f6158"); }
            if (kind == TowerKind.Archer)
            {
                c.R(5, 17, 26, 8, "78563a"); c.R(5, 17, 26, 2, "c4a16a");
                for (int i = 0; i < 5; i++) c.R(6 + i * 5, 13, 3, 7, "b49565");
                c.Roof(18, 2, 29, 14, "355847"); c.Roof(14, 4, 18, 11, "5a805a");
                c.R(15, 17, 6, 7, "23352f"); c.R(17, 18, 2, 2, "f1dab1");
                c.L(new Vector2(24, 16), new Vector2(29, 23), 1, "d1b27b"); c.R(25, 16, 1, 8, "e1d2a3");
            }
            else if (kind == TowerKind.Ember)
            {
                c.R(8, 18, 20, 8, "3b4142"); c.R(7, 17, 22, 3, "ba945a");
                c.E(18, 12, 8, 10, "ab533c"); c.E(18, 11, 5, 8, "ea9851");
                c.R(18, 0, 3, 11, "e8a45b"); c.E(17, 14, 3, 5, "f4d48a");
                c.R(6, 12, 3, 10, "7c7157"); c.R(27, 12, 3, 10, "7c7157");
            }
            else
            {
                c.R(12, 15, 12, 11, "366275"); c.Roof(18, 0, 14, 12, "acdcd8");
                c.R(12, 10, 12, 11, "70bfc7"); c.R(13, 9, 5, 12, "c4eee4");
                c.R(18, 4, 3, 17, "4d9db7"); c.Roof(18, 20, 12, 7, "377789");
                c.R(6, 23, 24, 3, "a6b2a2");
            }
            for (int i = 0; i < level; i++) { c.R(12 + i * 5, 37, 3, 3, "eed082"); }
            return c.Texture(kind + " level " + level);
        }
        public static Texture2D Skeleton(SkeletonKind kind, int frame) => Enemy(EnemyKind.Skeleton, frame, kind);
        public static Texture2D Enemy(EnemyKind kind, int frame, SkeletonKind skeleton = SkeletonKind.Normal)
        {
            var c = new Canvas(24, 29);
            string bone = kind == EnemyKind.Orc ? "86ad70" : "d8d6b7";
            c.E(12, 26, 10, 2, "162724");
            if (skeleton == SkeletonKind.Crawler)
            {
                c.R(3, 17, 10, 8, bone); c.R(4, 19, 3, 2, "213a36"); c.R(9, 19, 3, 2, "213a36");
                c.R(13, 23, 7, 2, bone); c.R(17, 20, 2, 6, bone); c.R(19, 21 + frame, 4, 2, bone);
                return c.Texture("Crawling skeleton " + frame);
            }
            if (skeleton == SkeletonKind.TRex)
            {
                c.L(new Vector2(2, 21), new Vector2(12, 16), 1, bone); c.L(new Vector2(12, 16), new Vector2(15, 8), 2, bone);
                c.R(13, 3, 10, 6, bone); c.R(18, 4, 2, 2, "213a36"); c.R(14, 10, 9, 2, bone);
                c.R(16, 8, 1, 3, bone); c.R(20, 8, 1, 3, bone);
                c.R(10, 14, 7, 7, bone); c.R(11, 15, 4, 1, "445c54"); c.R(11, 18, 4, 1, "445c54");
                c.R(10, 21, 2, 5 - frame, bone); c.R(15, 21, 2, 4 + frame, bone); c.R(15, 25, 5, 2, bone);
                c.R(17, 15, 4, 2, bone); return c.Texture("T-Rex skeleton " + frame);
            }
            if (skeleton == SkeletonKind.Sarcophagus) { c.R(1, 7, 10, 19, "8d733e"); c.R(2, 8, 7, 15, "d2b15e"); c.R(4, 11, 3, 10, "456d76"); }
            int leg = frame == 0 ? 0 : 2;
            c.R(8, 20, 3, 6 - leg, "929f90"); c.R(14, 20, 3, 4 + leg, "c7c9ad");
            c.R(7 - leg, 25 - leg, 5, 2, bone); c.R(14, 24 + leg, 5, 2, bone);
            c.R(7, 11, 11, 9, "84968b"); c.R(9, 11, 7, 9, bone);
            for (int y = 13; y < 19; y += 3) c.R(9, y, 7, 1, "445c54");
            c.R(11, 11, 2, 10, bone); c.R(5, 12, 3, 8, bone); c.R(18, 12, 2, 6, bone);
            c.R(6, 2, 13, 8, "899c91"); c.R(7, 1, 11, 8, bone); c.R(9, 9, 7, 3, bone);
            c.R(8, 4, 4, 3, "213a36"); c.R(14, 4, 3, 3, "213a36");
            c.R(9, 5, 2, 1, "73c6bc"); c.R(14, 5, 2, 1, "73c6bc");
            c.R(12, 7, 1, 2, "465b4f");
            if (kind == EnemyKind.Orc)
            {
                c.R(5, 0, 15, 3, "49776b"); c.R(4, 2, 3, 8, "365e55");
                c.R(6, 10, 13, 3, "578c77"); c.R(2, 12, 4, 8, "39685b");
                c.L(new Vector2(21, 16), new Vector2(23, 10), 1, "bccdb5");
                c.R(7, 3, 11, 7, "83a769"); c.R(8, 5, 3, 2, "20362e"); c.R(14, 5, 3, 2, "20362e");
                c.R(9, 9, 2, 3, "ebe1ba"); c.R(15, 9, 2, 3, "ebe1ba");
            }
            if (kind == EnemyKind.Chaos)
            {
                c.R(5, 0, 15, 3, "74838a"); c.R(4, 2, 4, 7, "52656c"); c.R(17, 2, 4, 7, "52656c");
                c.R(4, 11, 16, 10, "64767b"); c.R(5, 12, 7, 8, "839194"); c.R(12, 12, 2, 8, "b8b59c");
                c.R(1, 14, 6, 10, "354f59"); c.R(2, 15, 4, 8, "809c9a"); c.R(3, 18, 2, 2, "d6be84");
                c.R(21, 8, 2, 16, "755c41"); c.R(18, 7, 6, 5, "bac2b5");
                c.R(6, 1, 12, 5, "604768"); c.R(4, 0, 3, 5, "b59a78"); c.R(18, 0, 3, 5, "b59a78");
                c.R(9, 5, 2, 2, "dc8278"); c.R(14, 5, 2, 2, "dc8278");
            }
            if (skeleton == SkeletonKind.Ninja)
            { c.R(5, 2, 15, 3, "423754"); c.R(6, 7, 13, 4, "423754"); c.R(3, 4, 4, 2, "b47cba"); c.R(7, 13, 11, 6, "514564"); }
            else if (skeleton == SkeletonKind.Tutankhamun)
            { c.R(4, 0, 17, 3, "e2b85a"); c.R(4, 3, 3, 11, "dfb45d"); c.R(18, 3, 3, 11, "dfb45d"); for (int y = 4; y < 14; y += 3) { c.R(4,y,3,1,"3d8291"); c.R(18,y,3,1,"3d8291"); } c.R(22, 7, 1, 20, "d6b25f"); c.E(22,6,1,2,"75c6ce"); }
            else if (skeleton == SkeletonKind.Pirate)
            { c.R(4, 1, 17, 4, "533b37"); c.R(8, 0, 10, 3, "533b37"); c.R(8, 4, 5, 3, "101b22"); c.R(20, 12, 4, 3, "949da0"); c.R(20, 15, 2, 4, "7c5b37"); c.R(1, 15, 4, 8, "688452"); c.R(2, 12, 2, 4, "b99962"); }
            else if (skeleton == SkeletonKind.Samurai)
            { c.R(4, 1, 17, 3, "9e4848"); c.R(5, 0, 3, 5, "c5a56a"); c.R(17, 0, 3, 5, "c5a56a"); c.R(6, 12, 13, 7, "763e41"); c.L(new Vector2(21, 20), new Vector2(23, 4), 1, "e3e4cd"); }
            else if (skeleton == SkeletonKind.Knight)
            { c.R(5, 0, 15, 4, "8eacb6"); c.R(6, 3, 3, 7, "637f8d"); c.R(17, 3, 3, 7, "637f8d"); c.R(6, 12, 14, 9, "7c9ca6"); c.R(8, 13, 4, 7, "b0c8c6"); c.R(1, 13, 5, 12, "55758a"); }
            else if (skeleton == SkeletonKind.Warlock)
            { c.Roof(12, 0, 16, 6, "755590"); c.R(5, 10, 3, 15, "624876"); c.R(17, 10, 3, 15, "624876"); c.R(21, 6, 2, 21, "947849"); c.E(22, 5, 2, 3, "b997e3"); }
            else if (skeleton == SkeletonKind.Mad)
            { c.R(9, 4, 2, 3, "fcad5b"); c.R(14, 4, 2, 3, "fcad5b"); c.R(2, 3, 3, 12, bone); c.R(20, 3, 3, 12, bone); c.R(2, 1 + frame, 3, 3, "f2ae63"); c.R(20, 2 - frame, 3, 3, "f2ae63"); }
            else if (skeleton == SkeletonKind.Boxer)
            { c.R(2, 11, 5, 7, "c95449"); c.R(18, 11, 6, 7, "c95449"); c.R(2, 11, 5, 2, "ef9681"); c.R(18, 11, 5, 2, "ef9681"); c.R(8, 19, 9, 3, "a14f50"); }
            else if (skeleton == SkeletonKind.Boss)
            { c.R(4, 0, 17, 3, "d5ae5f"); c.R(5, 0, 2, 5, "f5d58a"); c.R(11, 0, 2, 5, "f5d58a"); c.R(18, 0, 2, 5, "f5d58a"); c.R(3, 11, 4, 12, "793a4b"); c.R(18, 11, 4, 12, "793a4b"); c.R(8, 20, 10, 2, "ddbc78"); }
            return c.Texture(kind + " " + skeleton + " frame " + frame);
        }
        public static Texture2D Hero(HeroKind kind)
        {
            var c = new Canvas(40, 52);
            c.E(20, 47, 17, 4, "10242a");
            if (kind == HeroKind.Circe)
            {
                c.R(11, 8, 18, 22, "634437"); c.R(14, 7, 12, 16, "deb58b");
                c.R(11, 5, 18, 7, "b98b4e"); c.R(13, 4, 15, 3, "dbc078");
                c.R(15, 6, 11, 2, "f3dc93"); c.R(14, 12, 3, 2, "4d5261"); c.R(22, 12, 3, 2, "4d5261");
                c.R(13, 22, 14, 15, "74759e"); c.Roof(20, 23, 28, 24, "66698f");
                c.R(15, 23, 5, 22, "afb1cf"); c.R(11, 31, 18, 3, "d7b46d");
                c.R(8, 24, 5, 12, "c7a57f"); c.R(27, 25, 5, 10, "c7a57f");
                c.R(33, 14, 2, 34, "b99765"); c.E(34, 12, 4, 4, "f3d27e"); c.R(33, 10, 2, 4, "fff1b4");
                c.R(13, 46, 7, 3, "c4a57b"); c.R(23, 46, 7, 3, "c4a57b");
            }
            else
            {
                c.R(9, 19, 20, 24, "884449"); c.R(13, 11, 14, 13, "d4a27b");
                c.R(11, 8, 18, 7, "be9b62"); c.R(12, 6, 16, 4, "d8bb75");
                c.R(17, 0, 6, 8, "b55848"); c.R(17, 13, 3, 9, "c9a15a");
                c.R(13, 23, 16, 17, "bc9b64"); c.R(14, 24, 6, 13, "e0c082");
                c.R(13, 34, 16, 3, "8b6845"); c.R(12, 39, 7, 10, "b39060"); c.R(23, 39, 7, 10, "b39060");
                c.R(5, 23, 9, 19, "a68550"); c.R(6, 25, 7, 13, "dcc184"); c.R(9, 29, 2, 5, "8a744b");
                c.R(32, 14, 3, 23, "d7ded0"); c.R(31, 12, 4, 3, "f3ead4");
                c.R(28, 35, 11, 3, "dfbc6b"); c.R(32, 38, 3, 8, "775d41");
                c.R(11, 48, 9, 3, "6f5840"); c.R(22, 48, 9, 3, "6f5840");
            }
            return c.Texture(kind + " temporary hero");
        }
        public static Texture2D Deer()
        {
            var c = new Canvas(58, 48);
            c.E(28, 44, 23, 3, "15372e");
            c.E(25, 25, 15, 8, "9ebe98"); c.E(26, 23, 12, 6, "d4dcb3");
            c.L(new Vector2(35, 27), new Vector2(43, 12), 3, "c3d1a6");
            c.E(45, 11, 7, 4, "d4dcb3"); c.R(48, 9, 2, 2, "243c37");
            c.L(new Vector2(17, 29), new Vector2(12, 41), 1, "c3d1a6");
            c.L(new Vector2(23, 31), new Vector2(23, 44), 1, "859a77");
            c.L(new Vector2(34, 29), new Vector2(41, 41), 1, "c3d1a6");
            c.L(new Vector2(30, 29), new Vector2(31, 43), 1, "859a77");
            c.L(new Vector2(44, 9), new Vector2(39, 1), 1, "e5dab0");
            c.L(new Vector2(43, 7), new Vector2(49, 0), 1, "e5dab0");
            c.L(new Vector2(40, 4), new Vector2(35, 3), 1, "e5dab0");
            c.L(new Vector2(46, 4), new Vector2(47, 0), 1, "e5dab0");
            c.R(8, 19, 7, 4, "d4dcb3");
            return c.Texture("Friendly spirit deer");
        }
        public static Texture2D Frog()
        {
            var c = new Canvas(28, 23);
            c.E(14, 19, 12, 3, "142b35");
            c.E(14, 13, 10, 7, "497bb2"); c.E(14, 14, 7, 5, "73add3");
            c.R(4, 5, 7, 6, "74bce0"); c.R(18, 5, 7, 6, "74bce0");
            c.R(7, 6, 3, 3, "1e354b"); c.R(18, 6, 3, 3, "1e354b");
            c.R(3, 16, 7, 4, "6299c3"); c.R(18, 16, 7, 4, "6299c3");
            c.R(10, 15, 8, 1, "355576");
            return c.Texture("Blue frog");
        }
        public static Texture2D Ring()
        {
            var c = new Canvas(128, 128);
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f));
                if (d <= 63) c.P(x, y, d > 61.5f ? new Color(.64f, .82f, .65f, .65f) : new Color(.6f, .85f, .65f, .07f));
            }
            return c.Texture("Tower range");
        }
    }
}
