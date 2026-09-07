using UnityEngine;

namespace SkeletonDefender
{
    public sealed partial class SkeletonGame
    {
        private Texture2D gothicPortraitArch, gothicOrnament, gothicLight, gothicBattlefieldEmission;
        private void DrawGothicPortraitAlcove(Rect rect)
        {
            if (gothicPortraitArch == null) gothicPortraitArch = Resources.Load<Texture2D>("NeonGothic/portrait_arch");
            if (gothicPortraitArch != null) Texture(rect, gothicPortraitArch);
            else { Fill(rect, Ink); Outline(rect, Edge); }
        }
        private void DrawGothicOrnament(Rect rect)
        {
            if (gothicOrnament == null) gothicOrnament = Resources.Load<Texture2D>("NeonGothic/rune_ornament");
            if (gothicOrnament != null) Texture(rect, gothicOrnament);
        }
        // Centers and radii are authored in the 624 x 384 Aseprite menu canvas.
        private static readonly Vector4[] MenuLights = {
            new Vector4(241,92,25,60), new Vector4(298,66,25,60), new Vector4(357,40,23,60),
            new Vector4(414,84,25,52), new Vector4(481,84,23,48), new Vector4(241,233,25,49),
            new Vector4(286,235,37,46), new Vector4(433,220,36,48), new Vector4(505,262,32,44)
        };
        private void DrawGothicMenuLights()
        {
            if (gothicLight == null) gothicLight = Resources.Load<Texture2D>("NeonGothic/pixel_light");
            if (gothicLight == null) return;
            float t = Mathf.Floor(Time.unscaledTime * 5) / 5;
            for (int i = 0; i < MenuLights.Length; i++)
            {
                Vector4 p = MenuLights[i];
                Color color = i < 3 ? PixelArt.C("7df5ff") : i < 6 ? PixelArt.C("ff84e1")
                    : i < 9 ? PixelArt.C("ffd084") : PixelArt.C("63fff1");
                float strength = i < 6 ? .20f : i < 9 ? .36f : .32f;
                color.a = strength * (.74f + .19f * Mathf.Sin(t * Mathf.PI / 1.6f + (i + 1) * .7f)
                    + .06f * Mathf.Sin(t * Mathf.PI * 2.5f + i + 1));
                Texture(new Rect((p.x-p.z)*1440/624, (p.y-p.w)*900/384,
                    p.z*2*1440/624, p.w*2*900/384), gothicLight, color);
            }
        }

        // Source coordinates are native 528x320 pixels; this runs inside mapRect
        // immediately after the background, before towers and actors.
        private static readonly Rect[] BattlefieldLampCores = {
            new Rect(25,21,7,7), new Rect(349,34,10,12), new Rect(448,66,6,6),
            new Rect(520,124,8,12), new Rect(0,147,5,12), new Rect(31,223,11,11),
            new Rect(390,270,10,12)
        };
        private static readonly Vector2[] BattlefieldLampCenters = {
            new Vector2(28,24), new Vector2(354,40), new Vector2(451,69),
            new Vector2(525,130), new Vector2(1,153), new Vector2(37,229),
            new Vector2(395,276)
        };

        private void DrawGothicBattlefieldLights()
        {
            if (gothicLight == null) gothicLight = Resources.Load<Texture2D>("NeonGothic/pixel_light");
            if (gothicBattlefieldEmission == null)
                gothicBattlefieldEmission = Resources.Load<Texture2D>("NeonGothic/battlefield_emission");
            if (gothicLight == null || game == null) return;

            // Simulation time freezes when the game pauses. No wall-clock flicker.
            float t = game.Elapsed;
            Color amber = PixelArt.C("ffb663");
            Color innerAmber = PixelArt.C("ffd28d");
            for (int i = 0; i < BattlefieldLampCenters.Length; i++)
            {
                Vector2 center = BattlefieldLampCenters[i];
                float phase = i * 1.73f;
                float flame = .83f + .11f * Mathf.Sin(t * 4.2f + phase)
                    + .06f * Mathf.Sin(t * 7.7f + phase * .61f);
                DrawBattlefieldGlow(center, new Vector2(20, 24), amber, .48f * flame);
                DrawBattlefieldGlow(center, new Vector2(7, 10), innerAmber, .23f * flame);
                DrawBattlefieldCore(BattlefieldLampCores[i], .36f + .20f * flame);
            }

            Vector2 glass = new Vector2(509, 47);
            float breathing = .84f + .12f * Mathf.Sin(t * 1.25f + .6f)
                + .04f * Mathf.Sin(t * 2.7f + 1.4f);
            DrawBattlefieldGlow(glass, new Vector2(21, 34), PixelArt.C("e25eaf"), .38f * breathing);
            DrawBattlefieldGlow(glass, new Vector2(10, 22), PixelArt.C("fc8bce"), .19f * breathing);
            // Only the pink panes brighten; the dark tracery and stone stay intact.
            DrawBattlefieldCore(new Rect(500,28,18,38), .35f + .22f * breathing);
        }

        private void DrawBattlefieldGlow(Vector2 center, Vector2 radius, Color color, float alpha)
        {
            color.a = alpha;
            Texture(new Rect((center.x - radius.x) * 2, (center.y - radius.y) * 2,
                radius.x * 4, radius.y * 4), gothicLight, color);
        }

        private void DrawBattlefieldCore(Rect sourcePixels, float alpha)
        {
            if (gothicBattlefieldEmission == null) return;
            Rect uv = new Rect(sourcePixels.x / 528f, 1 - sourcePixels.yMax / 320f,
                sourcePixels.width / 528f, sourcePixels.height / 320f);
            Rect target = new Rect(sourcePixels.x * 2, sourcePixels.y * 2,
                sourcePixels.width * 2, sourcePixels.height * 2);
            Color previous = GUI.color;
            GUI.color = new Color(1, 1, 1, alpha);
            GUI.DrawTextureWithTexCoords(target, gothicBattlefieldEmission, uv, true);
            GUI.color = previous;
        }
    }
}
