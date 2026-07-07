using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using FirstGame.Core;
using FirstGame.UI;

namespace FirstGame.Net
{
    /// <summary>Phase-1 multiplayer bootstrap: builds a NetworkManager + a Host/Join lobby over the
    /// (already built) arena. Players spawn networked and see each other move. No combat yet.</summary>
    public static class NetSession
    {
        public static bool Pending;
        const ushort Port = 7777;

        public static void Start()
        {
            // Temporary camera so the arena is visible behind the lobby UI (removed once you spawn).
            var camGo = new GameObject("[TempNetCam]");
            camGo.transform.SetPositionAndRotation(new Vector3(0, 6f, -16f), Quaternion.Euler(16f, 0f, 0f));
            camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();

            var prefab = Resources.Load<GameObject>("NetPlayer");

            var nmGo = new GameObject("NetworkManager");
            var nm = nmGo.AddComponent<NetworkManager>();
            var utp = nmGo.AddComponent<UnityTransport>();
            nm.NetworkConfig = new NetworkConfig { NetworkTransport = utp, PlayerPrefab = prefab };
            utp.SetConnectionData("127.0.0.1", Port);

            BuildLobby(nm, utp, prefab != null);
        }

        static void BuildLobby(NetworkManager nm, UnityTransport utp, bool prefabOk)
        {
            UIFactory.EnsureEventSystem();
            var canvas = UIFactory.CreateCanvas("NetLobby", 12);
            var root = canvas.transform;
            var bg = UIFactory.Panel(root, new Color(ArtPalette.Sky.r, ArtPalette.Sky.g, ArtPalette.Sky.b, 0.8f));
            UIFactory.Stretch(bg.rectTransform);

            var title = UIFactory.Label(root, "MULTIJOUEUR — 1v1 (réseau local)", 44, ArtPalette.NeonMag, TextAnchor.UpperCenter, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(1300, 60));
            var sub = UIFactory.Label(root, "Phase 1 : voir l'autre joueur bouger. Un joueur HÉBERGE, l'autre REJOINT (même PC : 127.0.0.1).", 20, ArtPalette.UiDim, TextAnchor.UpperCenter);
            UIFactory.Place(sub.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -176), new Vector2(1300, 30));

            if (!prefabOk)
            {
                var warn = UIFactory.Label(root, "⚠ Prefab réseau manquant — lance d'abord le menu  NEON ▸ Créer le prefab réseau", 22, ArtPalette.Enemy, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIFactory.Place(warn.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(1300, 40));
            }

            var ipLabel = UIFactory.Label(root, "IP de l'hôte :", 22, ArtPalette.UiText, TextAnchor.MiddleRight);
            UIFactory.Place(ipLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-260, 96), new Vector2(220, 40));
            var ip = MakeInput(root, "127.0.0.1", new Vector2(0, 96), new Vector2(300, 48));

            var host = UIFactory.AddChild(root, "Host");
            UIFactory.Place(host, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-190, 0), new Vector2(340, 72));
            UIFactory.Button(host, "HÉBERGER", ArtPalette.NeonCyan, ArtPalette.UiInk,
                () => { if (nm.StartHost()) canvas.gameObject.SetActive(false); }, 28);

            var join = UIFactory.AddChild(root, "Join");
            UIFactory.Place(join, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(190, 0), new Vector2(340, 72));
            UIFactory.Button(join, "REJOINDRE", ArtPalette.Objective, ArtPalette.UiInk,
                () => { utp.SetConnectionData(ip.text.Trim(), Port); if (nm.StartClient()) canvas.gameObject.SetActive(false); }, 28);

            var back = UIFactory.AddChild(root, "Back");
            UIFactory.Place(back, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(300, 56));
            UIFactory.Button(back, "RETOUR AU MENU", ArtPalette.Cover, ArtPalette.UiText, () =>
            {
                if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
                GameManager.LoadScene(SceneNames.MainMenu);
            }, 22);
        }

        static InputField MakeInput(Transform parent, string def, Vector2 pos, Vector2 size)
        {
            var go = UIFactory.AddChild(parent, "Input");
            UIFactory.Place(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
            go.gameObject.AddComponent<Image>().color = ArtPalette.UiInk;
            var input = go.gameObject.AddComponent<InputField>();
            var txt = UIFactory.Label(go, def, 22, ArtPalette.UiText, TextAnchor.MiddleLeft);
            UIFactory.Stretch(txt.rectTransform, 12);
            txt.supportRichText = false;
            input.textComponent = txt;
            input.text = def;
            return input;
        }
    }
}
