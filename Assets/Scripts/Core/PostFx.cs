using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FirstGame.Core
{
    /// <summary>URP post-processing setup. Inert under Built-in (guards on the active pipeline).</summary>
    public static class PostFx
    {
        public static bool UrpActive => GraphicsSettings.currentRenderPipeline != null;

        /// <summary>Creates a global Volume with Bloom + neutral tonemapping (once per scene).</summary>
        public static void SetupBloom()
        {
            if (!UrpActive) return;

            var go = new GameObject("[PostFX]");
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 0f;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            vol.profile = profile;

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(0.9f);
            bloom.scatter.Override(0.65f);
            bloom.tint.Override(ArtPalette.NeonCyan);

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);
        }

        /// <summary>Enables post-processing on a camera (needed for Bloom to appear).</summary>
        public static void EnablePostProcessing(Camera cam)
        {
            if (!UrpActive || cam == null) return;
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null)
            {
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            }
        }
    }
}
