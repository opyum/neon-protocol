using UnityEngine;
using UnityEngine.Rendering;

namespace FirstGame.Core
{
    /// <summary>Real PBR surface materials built from CC0 textures (ambientCG), loaded from
    /// Resources. Falls back to flat ArtPalette colours if the textures are absent.</summary>
    public static class Surfaces
    {
        static Material _floor, _metal;

        public static Material Floor => _floor != null ? _floor
            : (_floor = Build("Textures/concrete_color", "Textures/concrete_normal", 0.12f, 0f, new Vector2(18, 18), ArtPalette.Floor));

        public static Material Metal => _metal != null ? _metal
            : (_metal = Build("Textures/metal_color", "Textures/metal_normal", 0.5f, 0.5f, new Vector2(2, 2), ArtPalette.Wall));

        static bool Urp => GraphicsSettings.currentRenderPipeline != null;

        static Material Build(string colorPath, string normalPath, float smoothness, float metallic, Vector2 tiling, Color fallbackTint)
        {
            var color = Resources.Load<Texture2D>(colorPath);
            if (color == null) return ArtPalette.MakeMaterial(fallbackTint, metallic, smoothness);

            var shader = Shader.Find(Urp ? "Universal Render Pipeline/Lit" : "Standard");
            var m = new Material(shader);
            string baseTex = Urp ? "_BaseMap" : "_MainTex";
            m.SetTexture(baseTex, color);
            m.SetTextureScale(baseTex, tiling);

            var normal = Resources.Load<Texture2D>(normalPath);
            if (normal != null)
            {
                m.SetTexture("_BumpMap", normal);
                m.SetTextureScale("_BumpMap", tiling);
                m.EnableKeyword("_NORMALMAP");
            }
            m.SetFloat(Urp ? "_Smoothness" : "_Glossiness", smoothness);
            m.SetFloat("_Metallic", metallic);
            return m;
        }
    }
}
