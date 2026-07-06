using UnityEngine;

namespace FirstGame.Core
{
    /// <summary>Procedurally-generated sprites (no image files): gradients + vignette for the UI.</summary>
    public static class Tex
    {
        static Sprite _vignette;
        public static Sprite Vignette => _vignette != null ? _vignette : (_vignette = BuildVignette());

        static Sprite BuildVignette()
        {
            int s = 256;
            var tex = new Texture2D(s, s, TextureFormat.ARGB32, false) { wrapMode = TextureWrapMode.Clamp };
            var c = new Vector2(s / 2f, s / 2f);
            float maxD = c.magnitude;
            var px = new Color[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c) / maxD; // 0 centre .. 1 corner
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 0.42f) / 0.58f)) * 0.85f;
                    px[y * s + x] = new Color(0f, 0f, 0f, a);
                }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>1×N vertical gradient. Stretched on an Image it fills the screen top→bottom.</summary>
        public static Sprite VerticalGradient(Color bottom, Color top)
        {
            int h = 256;
            var tex = new Texture2D(1, h, TextureFormat.ARGB32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[h];
            for (int y = 0; y < h; y++) px[y] = Color.Lerp(bottom, top, (float)y / (h - 1));
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, h), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>N×1 horizontal gradient (left→right).</summary>
        public static Sprite HorizontalGradient(Color left, Color right)
        {
            int w = 256;
            var tex = new Texture2D(w, 1, TextureFormat.ARGB32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[w];
            for (int x = 0; x < w; x++) px[x] = Color.Lerp(left, right, (float)x / (w - 1));
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, 1), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
