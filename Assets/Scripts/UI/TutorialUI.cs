using System;
using UnityEngine;
using UnityEngine.UI;
using FirstGame.Core;

namespace FirstGame.UI
{
    /// <summary>Objective banner (top), hint line, progress, and end-of-tutorial overlay.</summary>
    public class TutorialUI : MonoBehaviour
    {
        Text _objective, _hint, _progress, _toast;
        GameObject _completePanel;
        float _toastUntil;

        void Awake() => Build();

        void Build()
        {
            var root = transform;

            // Top banner
            var banner = UIFactory.AddChild(root, "Banner");
            UIFactory.Place(banner, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -20), new Vector2(1100, 120));
            UIFactory.Panel(banner, new Color(ArtPalette.UiInk.r, ArtPalette.UiInk.g, ArtPalette.UiInk.b, 0.8f));

            var accent = UIFactory.AddChild(banner, "Accent");
            UIFactory.Place(accent, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(6, 0), new Vector2(5, 96));
            accent.gameObject.AddComponent<Image>().color = ArtPalette.NeonCyan;

            _progress = UIFactory.Label(banner, "OBJECTIF 1 / 8", 18, ArtPalette.NeonCyan, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(_progress.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -10), new Vector2(1040, 24));

            _objective = UIFactory.Label(banner, "", 30, ArtPalette.UiText, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(_objective.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -36), new Vector2(1040, 44));

            _hint = UIFactory.Label(banner, "", 18, ArtPalette.UiDim, TextAnchor.MiddleLeft);
            UIFactory.Place(_hint.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -82), new Vector2(1040, 30));

            // Centre toast (step complete!)
            _toast = UIFactory.Label(root, "", 44, ArtPalette.Objective, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Place(_toast.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 220), new Vector2(900, 80));
            _toast.gameObject.SetActive(false);

            // Completion overlay (hidden)
            _completePanel = UIFactory.AddChild(root, "Complete").gameObject;
            UIFactory.Stretch((RectTransform)_completePanel.transform);
            _completePanel.AddComponent<Image>().color = new Color(ArtPalette.Sky.r, ArtPalette.Sky.g, ArtPalette.Sky.b, 0.92f);
            _completePanel.SetActive(false);
        }

        public void ShowStep(int index, int total, string instruction, string hint)
        {
            if (_progress != null) _progress.text = $"OBJECTIF {index} / {total}";
            if (_objective != null) _objective.text = instruction;
            if (_hint != null) _hint.text = "Astuce : " + hint;
        }

        public void Toast(string message)
        {
            if (_toast == null) return;
            _toast.text = message;
            _toast.gameObject.SetActive(true);
            _toastUntil = Time.time + 1.4f;
        }

        void Update()
        {
            if (_toast != null && _toast.gameObject.activeSelf && Time.time >= _toastUntil)
                _toast.gameObject.SetActive(false);
        }

        public void ShowComplete(int level, int xpGained, Action onReplay, Action onMenu)
        {
            _completePanel.SetActive(true);
            var t = _completePanel.transform;

            var title = UIFactory.Label(t, "CAMP D'ENTRAÎNEMENT TERMINÉ", 52, ArtPalette.NeonCyan, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 180), new Vector2(1200, 70));

            var sub = UIFactory.Label(t, $"+{xpGained} XP   •   Niveau {level}   •   Bien joué, agent !", 30, ArtPalette.UiText, TextAnchor.MiddleCenter);
            UIFactory.Place(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 100), new Vector2(1200, 44));

            var info = UIFactory.Label(t,
                "Tu maîtrises maintenant : déplacement, visée, saut, tir, rechargement et sorts.\nProchaine étape du plan de jeu : le stand de tir libre, puis les missions de combat.",
                20, ArtPalette.UiDim, TextAnchor.MiddleCenter);
            UIFactory.Place(info.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(1000, 80));

            var replay = UIFactory.AddChild(t, "Replay");
            UIFactory.Place(replay, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-170, -90), new Vector2(300, 64));
            UIFactory.Button(replay, "RECOMMENCER", ArtPalette.Cover, ArtPalette.UiText, onReplay);

            var menu = UIFactory.AddChild(t, "Menu");
            UIFactory.Place(menu, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(170, -90), new Vector2(300, 64));
            UIFactory.Button(menu, "MENU PRINCIPAL", ArtPalette.NeonCyan, ArtPalette.UiInk, onMenu);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
