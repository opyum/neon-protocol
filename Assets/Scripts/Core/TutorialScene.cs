using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using FirstGame.Player;
using FirstGame.Combat;
using FirstGame.Abilities;
using FirstGame.Enemies;
using FirstGame.UI;
using FirstGame.Campaign;

namespace FirstGame.Core
{
    /// <summary>Builds the playable tutorial: arena, player rig, dummies, HUD and the step manager.</summary>
    public static class TutorialScene
    {
        public static void Build()
        {
            ConfigureLighting();
            BuildArena();

            var rig = PlayerRig.Build(new Vector3(0, 0.1f, -6f), Quaternion.identity);
            new GameObject("[PauseMenu]").AddComponent<FirstGame.UI.PauseMenu>();
            var health = rig.health;
            var weapon = rig.weapon;
            var abilities = rig.abilities;
            var controller = rig.controller;

            var statics = new List<TrainingDummy>
            {
                TrainingDummy.Spawn(null, new Vector3(-3, 0, 15), 60f, locked: true, name: "Dummy_Statique_1"),
                TrainingDummy.Spawn(null, new Vector3( 0, 0, 16), 60f, locked: true, name: "Dummy_Statique_2"),
                TrainingDummy.Spawn(null, new Vector3( 3, 0, 15), 60f, locked: true, name: "Dummy_Statique_3"),
            };

            var movers = new List<TrainingDummy>
            {
                TrainingDummy.Spawn(null, new Vector3(-5, 0, 19), 80f, locked: false, patrol: true, patrolRange: 4f, patrolSpeed: 3f, name: "Dummy_Mobile_1"),
                TrainingDummy.Spawn(null, new Vector3( 5, 0, 19), 80f, locked: false, patrol: true, patrolRange: 4f, patrolSpeed: 3f, name: "Dummy_Mobile_2"),
            };
            foreach (var m in movers) m.gameObject.SetActive(false);

            // HUD
            UIFactory.EnsureEventSystem();
            var hudCanvas = UIFactory.CreateCanvas("HUDCanvas", 0);
            var hud = hudCanvas.gameObject.AddComponent<HUD>();
            hud.playerHealth = health;
            hud.weapon = weapon;
            hud.abilities = abilities;

            // Tutorial UI (above HUD)
            var tutCanvas = UIFactory.CreateCanvas("TutorialCanvas", 5);
            var tutUI = tutCanvas.gameObject.AddComponent<TutorialUI>();

            // Manager
            var mgrGo = new GameObject("[TutorialManager]");
            var mgr = mgrGo.AddComponent<TutorialManager>();
            mgr.controller = controller;
            mgr.weapon = weapon;
            mgr.abilities = abilities;
            mgr.health = health;
            mgr.ui = tutUI;
            mgr.staticDummies = statics.ToArray();
            mgr.movingDummies = movers.ToArray();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1f;
        }

        static void BuildArena()
        {
            var arena = new GameObject("Arena").transform;

            // Floor (solid box so it has a reliable collider)
            Prim.Box(arena, new Vector3(0, -0.25f, 6f), new Vector3(50, 0.5f, 50), ArtPalette.Floor, smoothness: 0.12f, name: "Floor");

            // Perimeter walls
            Prim.Box(arena, new Vector3(0, 2, 31f), new Vector3(50, 4, 0.5f), ArtPalette.Wall, name: "Wall_N");
            Prim.Box(arena, new Vector3(0, 2, -19f), new Vector3(50, 4, 0.5f), ArtPalette.Wall, name: "Wall_S");
            Prim.Box(arena, new Vector3(25f, 2, 6f), new Vector3(0.5f, 4, 50), ArtPalette.Wall, name: "Wall_E");
            Prim.Box(arena, new Vector3(-25f, 2, 6f), new Vector3(0.5f, 4, 50), ArtPalette.Wall, name: "Wall_W");

            // Cover boxes
            Prim.Box(arena, new Vector3(-7, 0.75f, 8), new Vector3(2, 1.5f, 2), ArtPalette.Cover, name: "Cover_1");
            Prim.Box(arena, new Vector3(7, 0.75f, 8), new Vector3(2, 1.5f, 2), ArtPalette.Cover, name: "Cover_2");
            Prim.Box(arena, new Vector3(0, 0.5f, 22), new Vector3(6, 1f, 1.5f), ArtPalette.Cover, name: "Cover_3");

            // Low wall to jump over (set dressing for the jump step)
            Prim.Box(arena, new Vector3(9, 0.5f, 12), new Vector3(6, 1f, 0.5f), ArtPalette.Cover, name: "JumpWall");

            // Neon guide strips on the floor
            Prim.NeonStrip(arena, new Vector3(0, 0.02f, 6), new Vector3(0.18f, 0.02f, 44), ArtPalette.NeonCyan, "GuideLine");
            Prim.NeonStrip(arena, new Vector3(-24.5f, 0.6f, 6), new Vector3(0.1f, 0.06f, 48), ArtPalette.NeonCyan, "EdgeW");
            Prim.NeonStrip(arena, new Vector3(24.5f, 0.6f, 6), new Vector3(0.1f, 0.06f, 48), ArtPalette.NeonCyan, "EdgeE");
        }

        static void ConfigureLighting() => Env.SetupStylized(fog: true);
    }
}
