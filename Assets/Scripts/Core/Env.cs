using UnityEngine;
using UnityEngine.Rendering;

namespace FirstGame.Core
{
    /// <summary>Shared stylized lighting/atmosphere so every scene reads consistently.</summary>
    public static class Env
    {
        public static void SetupStylized(bool fog, float fogStart = 20f, float fogEnd = 95f)
        {
            // Key light (cold, crisp shadows = tactical info). Created first so it can be the sun.
            var key = new GameObject("KeyLight").AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = ArtPalette.SunColor;
            key.intensity = 1.15f;
            key.shadows = LightShadows.Soft;
            key.shadowStrength = 0.75f;
            key.transform.rotation = Quaternion.Euler(48f, -38f, 0f);

            // Cyan rim/fill from the opposite side for silhouette separation
            var rim = new GameObject("RimLight").AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = ArtPalette.NeonCyan;
            rim.intensity = 0.35f;
            rim.transform.rotation = Quaternion.Euler(18f, 150f, 0f);

            // Soft fill to lift shadows a touch
            var fill = new GameObject("FillLight").AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = ArtPalette.Hex("#3A2A4A");
            fill.intensity = 0.12f;
            fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(-15f, 60f, 0f);

            // Procedural gradient skybox (twilight)
            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                var sky = new Material(skyShader);
                sky.SetFloat("_SunSize", 0.04f);
                sky.SetFloat("_AtmosphereThickness", 1.4f);
                sky.SetColor("_SkyTint", ArtPalette.Hex("#1E2742"));
                sky.SetColor("_GroundColor", ArtPalette.Hex("#0A0E18"));
                sky.SetFloat("_Exposure", 0.75f);
                RenderSettings.skybox = sky;
                RenderSettings.sun = key;
                RenderSettings.ambientMode = AmbientMode.Skybox;
            }
            else
            {
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = ArtPalette.Hex("#22314F");
                RenderSettings.ambientEquatorColor = ArtPalette.Hex("#161C2E");
                RenderSettings.ambientGroundColor = ArtPalette.Hex("#0A0E18");
            }
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 0.35f;

            RenderSettings.fog = fog;
            if (fog)
            {
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = ArtPalette.Sky;
                RenderSettings.fogStartDistance = fogStart;
                RenderSettings.fogEndDistance = fogEnd;
            }

            DynamicGI.UpdateEnvironment();

            PostFx.SetupBloom(); // no-op under Built-in
        }
    }
}
