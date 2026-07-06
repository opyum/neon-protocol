using UnityEngine;
using UnityEngine.Rendering;

namespace FirstGame.Core
{
    /// <summary>Shared stylized lighting/atmosphere so every scene reads consistently.</summary>
    public static class Env
    {
        public static void SetupStylized(bool fog, float fogStart = 20f, float fogEnd = 95f)
        {
            // Gradient ambient — richer shading than flat, keeps the twilight mood.
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ArtPalette.Hex("#22314F");
            RenderSettings.ambientEquatorColor = ArtPalette.Hex("#161C2E");
            RenderSettings.ambientGroundColor = ArtPalette.Hex("#0A0E18");

            RenderSettings.fog = fog;
            if (fog)
            {
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = ArtPalette.Sky;
                RenderSettings.fogStartDistance = fogStart;
                RenderSettings.fogEndDistance = fogEnd;
            }

            // Key light (warm-cold, crisp shadows = tactical info)
            var key = new GameObject("KeyLight").AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = ArtPalette.SunColor;
            key.intensity = 1.15f;
            key.shadows = LightShadows.Soft;
            key.transform.rotation = Quaternion.Euler(48f, -38f, 0f);

            // Cyan rim/fill from the opposite side for edge separation
            var rim = new GameObject("RimLight").AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = ArtPalette.NeonCyan;
            rim.intensity = 0.28f;
            rim.transform.rotation = Quaternion.Euler(18f, 150f, 0f);
        }
    }
}
