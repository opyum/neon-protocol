using UnityEngine;
using FirstGame.UI;

namespace FirstGame.Core
{
    /// <summary>Builds the intro menu scene: camera, stylized light, animated backdrop, and the menu UI.</summary>
    public static class MainMenuScene
    {
        public static void Build()
        {
            Env.SetupStylized(fog: true, fogStart: 14f, fogEnd: 55f);

            // Camera
            var camGo = new GameObject("MenuCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.backgroundColor = ArtPalette.Sky;
            cam.fieldOfView = 52f;
            camGo.AddComponent<AudioListener>();
            camGo.tag = "MainCamera";
            PostFx.EnablePostProcessing(cam);
            camGo.transform.position = new Vector3(4.5f, 2.4f, -8.5f);
            camGo.transform.rotation = Quaternion.Euler(7f, -20f, 0f);

            // Rotating backdrop
            var deco = new GameObject("Backdrop");
            deco.transform.position = new Vector3(-1f, 0f, 7f);
            BuildBackdrop(deco.transform);
            deco.AddComponent<SlowSpin>().degreesPerSecond = new Vector3(0, 5f, 0);

            // UI
            UIFactory.EnsureEventSystem();
            var canvas = UIFactory.CreateCanvas("MenuCanvas");
            canvas.gameObject.AddComponent<MainMenuUI>();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1f;
        }

        static void BuildBackdrop(Transform parent)
        {
            // Tron-style neon grid floor
            Prim.Box(parent, new Vector3(0, -1.5f, 0), new Vector3(30, 0.3f, 30), ArtPalette.Floor, collider: false);
            Prim.Grid(parent, new Vector3(0, -1.34f, 0), 28f, 14, ArtPalette.NeonCyan);

            // A little architecture
            Prim.Box(parent, new Vector3(-4, -0.2f, 3), new Vector3(1.6f, 2.6f, 1.6f), ArtPalette.Cover, collider: false);
            Prim.Box(parent, new Vector3(3.2f, -0.6f, 1.5f), new Vector3(2f, 1.8f, 2f), ArtPalette.Wall, collider: false);
            Prim.Box(parent, new Vector3(0.5f, -1.0f, -2.5f), new Vector3(1.2f, 1f, 1.2f), ArtPalette.Cover, collider: false);
            Prim.Cylinder(parent, new Vector3(-1.8f, -0.4f, -1.8f), 0.4f, 2.2f, ArtPalette.Metal, name: "Pillar");

            // Neon edge strips on the cover blocks
            Prim.NeonStrip(parent, new Vector3(-4, 1.05f, 3), new Vector3(1.7f, 0.06f, 0.06f), ArtPalette.NeonCyan, "Edge");
            Prim.NeonStrip(parent, new Vector3(3.2f, 0.25f, 1.5f), new Vector3(0.06f, 0.06f, 2.1f), ArtPalette.NeonMag, "Edge");

            // Floating glowing shapes (bloom-fake halos) that bob gently
            var s1 = Prim.NeonGlowSphere(parent, new Vector3(-3.5f, 2.2f, 2f), 0.5f, ArtPalette.NeonCyan);
            s1.AddComponent<Bob>().amplitude = 0.35f;
            var s2 = Prim.NeonGlowSphere(parent, new Vector3(3.6f, 1.6f, -0.5f), 0.35f, ArtPalette.Objective);
            var b2 = s2.AddComponent<Bob>(); b2.amplitude = 0.5f; b2.speed = 0.8f;
            var s3 = Prim.NeonGlowSphere(parent, new Vector3(0.2f, 2.8f, 4f), 0.28f, ArtPalette.NeonMag);
            var b3 = s3.AddComponent<Bob>(); b3.amplitude = 0.4f; b3.speed = 1.3f;
        }
    }
}
