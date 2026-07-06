using UnityEngine;
using UnityEngine.Rendering;

namespace FirstGame.Core
{
    /// <summary>
    /// Single source of truth for colours and materials.
    /// Palette = "NEON PROTOCOL" art direction (flat/stylised, high readability).
    /// Colour IS information: red = hostile, cyan = ally/player, amber = objective.
    /// </summary>
    public static class ArtPalette
    {
        public static Color Hex(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out var c)) return c;
            return Color.magenta;
        }

        // Environment (neutral, desaturated ~90% of the scene)
        public static readonly Color Floor   = Hex("#3A4048"); // cold concrete
        public static readonly Color Wall     = Hex("#5A626C"); // lighter concrete
        public static readonly Color Cover    = Hex("#2A2F36"); // dark slate cover
        public static readonly Color Sky      = Hex("#141A2E"); // twilight indigo
        public static readonly Color SkyHigh  = Hex("#1E2742");
        public static readonly Color Metal    = Hex("#8A929C"); // the only shiny material

        // Gameplay language (saturated, reserved)
        public static readonly Color Enemy    = Hex("#FF2D4E"); // hostile red
        public static readonly Color Player   = Hex("#22E0C8"); // ally teal-cyan
        public static readonly Color NeonCyan = Hex("#00F0FF"); // primary accent
        public static readonly Color NeonMag  = Hex("#FF3DD0"); // secondary accent
        public static readonly Color Objective = Hex("#FFB627"); // amber = interactive
        public static readonly Color Danger   = Hex("#FF6A1A"); // orange = hazard
        public static readonly Color Signal   = Hex("#EAF2FF"); // near-white text/reticle

        // UI
        public static readonly Color UiInk    = Hex("#0E1420"); // panel background
        public static readonly Color UiText   = Hex("#EAF2FF");
        public static readonly Color UiDim    = Hex("#8B97A7");

        public static readonly Color SunColor = Hex("#C8D4FF"); // cold directional light

        static readonly Color[] Accents = { NeonCyan, Objective, Enemy, NeonMag, Player };
        public static Color AccentFor(int i) => Accents[((i % Accents.Length) + Accents.Length) % Accents.Length];

        // Pipeline-aware shader selection (URP-ready; uses Built-in shaders while URP is off).
        static bool Urp => GraphicsSettings.currentRenderPipeline != null;
        static Shader _lit, _unlit;
        static Shader Lit => _lit != null ? _lit : (_lit = Shader.Find(Urp ? "Universal Render Pipeline/Lit" : "Standard"));
        static Shader UnlitShader => _unlit != null ? _unlit : (_unlit = Shader.Find(Urp ? "Universal Render Pipeline/Unlit" : "Unlit/Color"));

        public static Material MakeMaterial(Color color, float metallic = 0f, float smoothness = 0.15f)
        {
            var m = new Material(Lit) { color = color }; // _Color/_BaseColor are both [MainColor]
            m.SetFloat("_Metallic", metallic);
            m.SetFloat(Urp ? "_Smoothness" : "_Glossiness", smoothness);
            return m;
        }

        /// <summary>Bright, always-lit colour (used for neon strips, tracers, ability FX).</summary>
        public static Material MakeUnlit(Color color)
        {
            if (UnlitShader == null) return MakeMaterial(color);
            return new Material(UnlitShader) { color = color };
        }

        /// <summary>Emissive Standard material (soft glow so units never vanish in shadow).</summary>
        public static Material MakeEmissive(Color color, float intensity = 1.5f)
        {
            var m = new Material(Lit) { color = color * 0.25f };
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", color * intensity);
            return m;
        }

        /// <summary>Additive, transparent "bloom-fake" material for glow halos around neon.</summary>
        public static Material MakeGlow(Color color, float intensity = 2f)
        {
            var m = new Material(Lit);
            var c = color; c.a = 0.2f;
            m.SetFloat("_Mode", 3f); // Transparent
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // additive
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", color * intensity);
            m.color = c;
            m.renderQueue = 3000;
            return m;
        }
    }
}
