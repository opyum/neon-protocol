using UnityEngine;
using UnityEngine.UI;
using FirstGame.UI;
using FirstGame.Campaign;

namespace FirstGame.Core
{
    /// <summary>Combat arena: player rig vs bots, with an in-scene mission picker (Duel / Round / Examen).</summary>
    public static class CombatArenaScene
    {
        public static void Build()
        {
            Env.SetupStylized(fog: true);
            BuildArena();

            var rig = PlayerRig.Build(new Vector3(0, 0.1f, -18f), Quaternion.identity);
            rig.player.AddComponent<EscapeToMenu>();

            UIFactory.EnsureEventSystem();
            var hudCanvas = UIFactory.CreateCanvas("HUDCanvas", 0);
            var hud = hudCanvas.gameObject.AddComponent<HUD>();
            hud.playerHealth = rig.health;
            hud.weapon = rig.weapon;
            hud.abilities = rig.abilities;

            var tutCanvas = UIFactory.CreateCanvas("CombatBanner", 5);
            var ui = tutCanvas.gameObject.AddComponent<TutorialUI>();

            var mgrGo = new GameObject("[CombatMissionManager]");
            var mgr = mgrGo.AddComponent<CombatMissionManager>();
            mgr.controller = rig.controller;
            mgr.weapon = rig.weapon;
            mgr.abilities = rig.abilities;
            mgr.health = rig.health;
            mgr.ui = ui;
            mgr.arena = null;

            // Disable control until a mission is picked
            rig.controller.ControlEnabled = false;
            rig.weapon.ControlEnabled = false;
            rig.abilities.ControlEnabled = false;

            BuildMissionPicker(mgr, rig);

            Time.timeScale = 1f;
        }

        static void BuildMissionPicker(CombatMissionManager mgr, PlayerRig.Refs rig)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            var canvas = UIFactory.CreateCanvas("MissionPicker", 10);
            var root = canvas.transform;
            var panel = UIFactory.Panel(root, new Color(ArtPalette.Sky.r, ArtPalette.Sky.g, ArtPalette.Sky.b, 0.94f));
            UIFactory.Stretch(panel.rectTransform);

            var title = UIFactory.Label(root, "MISSIONS DE COMBAT", 54, ArtPalette.NeonCyan, TextAnchor.UpperCenter, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -120), new Vector2(1200, 64));
            var sub = UIFactory.Label(root, "Choisis ta mission. Ton loadout (arme, 3 sorts, équipement) s'applique.", 22, ArtPalette.UiDim, TextAnchor.UpperCenter);
            UIFactory.Place(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -186), new Vector2(1200, 30));

            (string t, string d)[] missions =
            {
                ("LE DUEL", "4 duels 1v1 de difficulté croissante. 1 vie. +150 XP."),
                ("LE ROUND", "Capture et sécurise la zone contre 4 défenseurs. 2 vies. +250 XP."),
                ("EXAMEN", "4 vagues d'assaut jusqu'au chef d'élite. 3 vies. +400 XP (bonus sans mort)."),
            };
            for (int i = 0; i < missions.Length; i++)
            {
                int idx = i;
                var card = UIFactory.AddChild(root, "Mission_" + i);
                UIFactory.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 120 - i * 130), new Vector2(760, 110));
                UIFactory.Panel(card, new Color(ArtPalette.UiInk.r, ArtPalette.UiInk.g, ArtPalette.UiInk.b, 0.85f));

                var accent = UIFactory.AddChild(card, "Accent");
                UIFactory.Place(accent, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(4, 0), new Vector2(8, 94));
                accent.gameObject.AddComponent<Image>().color = ArtPalette.AccentFor(i);

                var name = UIFactory.Label(card, missions[i].t, 32, ArtPalette.UiText, TextAnchor.UpperLeft, FontStyle.Bold);
                UIFactory.Place(name.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -14), new Vector2(500, 38));
                var desc = UIFactory.Label(card, missions[i].d, 18, ArtPalette.UiDim, TextAnchor.UpperLeft);
                UIFactory.Place(desc.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(32, -56), new Vector2(560, 44));

                var play = UIFactory.AddChild(card, "Play");
                UIFactory.Place(play, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(150, 60));
                UIFactory.Button(play, "JOUER", ArtPalette.NeonCyan, ArtPalette.UiInk, () =>
                {
                    canvas.gameObject.SetActive(false);
                    mgr.StartMission(idx);
                }, 24);
            }

            var back = UIFactory.AddChild(root, "Back");
            UIFactory.Place(back, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 70), new Vector2(280, 56));
            UIFactory.Button(back, "RETOUR AU MENU", ArtPalette.Cover, ArtPalette.UiText, () => GameManager.LoadScene(SceneNames.MainMenu), 22);
        }

        static void BuildArena()
        {
            var arena = new GameObject("CombatArena").transform;

            var floor = Prim.Box(arena, new Vector3(0, -0.25f, 10f), new Vector3(70, 0.5f, 70), ArtPalette.Floor, smoothness: 0.12f, name: "Floor");
            floor.GetComponent<Renderer>().sharedMaterial = Surfaces.Floor;

            var wallMat = Surfaces.Metal;
            SetMat(Prim.Box(arena, new Vector3(0, 2, 45f), new Vector3(70, 4, 0.5f), ArtPalette.Wall, name: "Wall_N"), wallMat);
            SetMat(Prim.Box(arena, new Vector3(0, 2, -25f), new Vector3(70, 4, 0.5f), ArtPalette.Wall, name: "Wall_S"), wallMat);
            SetMat(Prim.Box(arena, new Vector3(35f, 2, 10f), new Vector3(0.5f, 4, 70), ArtPalette.Wall, name: "Wall_E"), wallMat);
            SetMat(Prim.Box(arena, new Vector3(-35f, 2, 10f), new Vector3(0.5f, 4, 70), ArtPalette.Wall, name: "Wall_W"), wallMat);

            // Cover: real modular props (Prototype Kit) if available, else primitive boxes
            if (!LevelBuilder.BuildCombatCover(arena))
            {
                Vector3[] covers = { new(-8, 0, 0), new(8, 0, 0), new(-5, 0, 12), new(5, 0, 12), new(-14, 0, 20), new(14, 0, 20) };
                foreach (var c in covers)
                    Prim.Box(arena, c + new Vector3(0, 0.75f, 0), new Vector3(2, 1.5f, 2), ArtPalette.Cover, name: "Cover");
                Prim.Box(arena, new Vector3(0, 1f, 24), new Vector3(6, 2f, 1.5f), ArtPalette.Cover, name: "Cover_Big");
            }

            // Control zone marker (amber disc + pulse)
            var disc = Prim.Cylinder(arena, new Vector3(0, 0.06f, 18), 4f, 0.12f, ArtPalette.Objective, unlit: true, name: "ControlZone");
            var col = disc.GetComponent<Collider>(); if (col) Object.Destroy(col);
            var pulse = disc.AddComponent<Pulse>(); pulse.color = ArtPalette.Objective; pulse.speed = 2.5f;

            // Subtle neon edge strips along the side walls (not a big central line)
            Prim.NeonStrip(arena, new Vector3(-34.4f, 0.3f, 10), new Vector3(0.1f, 0.05f, 68), ArtPalette.NeonCyan, "EdgeW");
            Prim.NeonStrip(arena, new Vector3(34.4f, 0.3f, 10), new Vector3(0.1f, 0.05f, 68), ArtPalette.NeonCyan, "EdgeE");
        }

        static void SetMat(GameObject go, Material m)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = m;
        }
    }
}
