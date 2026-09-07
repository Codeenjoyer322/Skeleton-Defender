using UnityEngine;

namespace SkeletonDefender
{
    public sealed partial class SkeletonGame
    {
        private Texture2D gothicPortraitArch, gothicOrnament, gothicLight;
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
    }
}
