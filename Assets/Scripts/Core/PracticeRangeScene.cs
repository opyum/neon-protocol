using UnityEngine;
using UnityEngine.Rendering;
using FirstGame.Combat;
using FirstGame.Enemies;
using FirstGame.UI;

namespace FirstGame.Core
{
    /// <summary>Free-play shooting range: your saved loadout, weapon switching (1-5), respawning targets.</summary>
    public static class PracticeRangeScene
    {
        public static void Build()
        {
            ConfigureLighting();
            BuildArena();

            var rig = PlayerRig.Build(new Vector3(0, 0.1f, -8f), Quaternion.identity);
            var switcher = rig.player.AddComponent<WeaponSwitcher>();
            switcher.weapon = rig.weapon;
            new GameObject("[PauseMenu]").AddComponent<FirstGame.UI.PauseMenu>();

            BuildTargets();

            // HUD
            UIFactory.EnsureEventSystem();
            var hudCanvas = UIFactory.CreateCanvas("HUDCanvas", 0);
            var hud = hudCanvas.gameObject.AddComponent<HUD>();
            hud.playerHealth = rig.health;
            hud.weapon = rig.weapon;
            hud.abilities = rig.abilities;

            // Instructions banner
            var infoCanvas = UIFactory.CreateCanvas("InfoCanvas", 5);
            var title = UIFactory.Label(infoCanvas.transform, "STAND DE TIR", 26, ArtPalette.NeonCyan, TextAnchor.UpperCenter, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -16), new Vector2(700, 34));
            var help = UIFactory.Label(infoCanvas.transform,
                "1-5 : changer d'arme   •   Clic gauche : tirer   •   E / F / C : sorts   •   R : recharger   •   Échap : menu",
                18, ArtPalette.UiText, TextAnchor.UpperCenter);
            UIFactory.Place(help.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -52), new Vector2(1200, 28));

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1f;
        }

        static void BuildTargets()
        {
            // Rows of respawning static targets at increasing distance
            float[] distances = { 12f, 18f, 24f, 30f };
            float[] xs = { -6f, -2f, 2f, 6f };
            for (int row = 0; row < distances.Length; row++)
                foreach (var x in xs)
                    TrainingDummy.Spawn(null, new Vector3(x, 0, distances[row]), 40f,
                        autoRespawn: true, respawnDelay: 1.5f, name: "Cible");

            // A couple of moving targets
            TrainingDummy.Spawn(null, new Vector3(-8, 0, 20), 60f, patrol: true, patrolRange: 5f, patrolSpeed: 4f,
                autoRespawn: true, respawnDelay: 1.5f, name: "Cible_Mobile_1");
            TrainingDummy.Spawn(null, new Vector3(8, 0, 26), 60f, patrol: true, patrolRange: 5f, patrolSpeed: 4f,
                autoRespawn: true, respawnDelay: 1.5f, name: "Cible_Mobile_2");
        }

        static void BuildArena()
        {
            var arena = new GameObject("RangeArena").transform;

            Prim.Box(arena, new Vector3(0, -0.25f, 12f), new Vector3(60, 0.5f, 60), ArtPalette.Floor, smoothness: 0.12f, name: "Floor");

            Prim.Box(arena, new Vector3(0, 2, 42f), new Vector3(60, 4, 0.5f), ArtPalette.Wall, name: "Wall_N");
            Prim.Box(arena, new Vector3(0, 2, -18f), new Vector3(60, 4, 0.5f), ArtPalette.Wall, name: "Wall_S");
            Prim.Box(arena, new Vector3(30f, 2, 12f), new Vector3(0.5f, 4, 60), ArtPalette.Wall, name: "Wall_E");
            Prim.Box(arena, new Vector3(-30f, 2, 12f), new Vector3(0.5f, 4, 60), ArtPalette.Wall, name: "Wall_W");

            // Distance markers (neon lines across the floor)
            float[] lines = { 12f, 18f, 24f, 30f };
            foreach (var z in lines)
                Prim.NeonStrip(arena, new Vector3(0, 0.02f, z), new Vector3(20, 0.02f, 0.12f), ArtPalette.NeonCyan, "Line_" + z);

            // Side cover
            Prim.Box(arena, new Vector3(-10, 0.75f, 6), new Vector3(2, 1.5f, 2), ArtPalette.Cover, name: "Cover_L");
            Prim.Box(arena, new Vector3(10, 0.75f, 6), new Vector3(2, 1.5f, 2), ArtPalette.Cover, name: "Cover_R");
        }

        static void ConfigureLighting() => Env.SetupStylized(fog: true);
    }
}
