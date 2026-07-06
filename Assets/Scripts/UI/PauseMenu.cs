using System;
using UnityEngine;
using UnityEngine.UI;
using FirstGame.Core;
using FirstGame.Player;
using FirstGame.Combat;
using FirstGame.Abilities;
using FirstGame.Equipment;

namespace FirstGame.UI
{
    /// <summary>Escape opens a pause menu (Resume / Options / Main menu / Quit). Freezes the game
    /// and disables player control; Options tune mouse sensitivity + volume (saved).</summary>
    public class PauseMenu : MonoBehaviour
    {
        FirstPersonController _controller;
        WeaponController _weapon;
        AbilitySystem _abilities;

        GameObject _root, _mainPanel, _optionsPanel;
        Text _sensValue, _volValue;
        bool _paused, _wasEnabled;

        void Start()
        {
            _controller = FindAnyObjectByType<FirstPersonController>();
            _weapon = FindAnyObjectByType<WeaponController>();
            _abilities = FindAnyObjectByType<AbilitySystem>();
            Settings.Apply();
            Build();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { if (_paused) Resume(); else Pause(); }
        }

        void Pause()
        {
            _paused = true;
            _wasEnabled = _controller != null && _controller.ControlEnabled;
            SetControl(false);
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _root.SetActive(true);
            _mainPanel.SetActive(true);
            _optionsPanel.SetActive(false);
        }

        void Resume()
        {
            _paused = false;
            SetControl(_wasEnabled);
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _root.SetActive(false);
        }

        void SetControl(bool on)
        {
            if (_controller) _controller.ControlEnabled = on;
            if (_weapon) _weapon.ControlEnabled = on;
            if (_abilities) _abilities.ControlEnabled = on;
            var u = _controller != null ? _controller.GetComponent<UtilityController>() : null;
            if (u) u.ControlEnabled = on;
        }

        void Build()
        {
            UIFactory.EnsureEventSystem();
            var canvas = UIFactory.CreateCanvas("PauseCanvas", 20);
            _root = canvas.gameObject;

            var bg = UIFactory.Panel(_root.transform, new Color(ArtPalette.Sky.r, ArtPalette.Sky.g, ArtPalette.Sky.b, 0.88f));
            UIFactory.Stretch(bg.rectTransform);

            _mainPanel = UIFactory.AddChild(_root.transform, "Main").gameObject;
            UIFactory.Stretch((RectTransform)_mainPanel.transform);
            var title = UIFactory.Label(_mainPanel.transform, "PAUSE", 64, ArtPalette.NeonCyan, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, C, C, new Vector2(0, 190), new Vector2(600, 84));
            MenuBtn(_mainPanel.transform, 70, "REPRENDRE", ArtPalette.NeonCyan, ArtPalette.UiInk, Resume);
            MenuBtn(_mainPanel.transform, -10, "OPTIONS", ArtPalette.Cover, ArtPalette.UiText,
                () => { _mainPanel.SetActive(false); _optionsPanel.SetActive(true); RefreshOptions(); });
            MenuBtn(_mainPanel.transform, -90, "MENU PRINCIPAL", ArtPalette.Cover, ArtPalette.UiText,
                () => { Time.timeScale = 1f; GameManager.LoadScene(SceneNames.MainMenu); });
            MenuBtn(_mainPanel.transform, -170, "QUITTER", ArtPalette.Enemy, ArtPalette.UiInk, Quit);

            BuildOptions();
            _root.SetActive(false);
        }

        void MenuBtn(Transform parent, float y, string label, Color bg, Color fg, Action onClick)
        {
            var slot = UIFactory.AddChild(parent, "Btn_" + label);
            UIFactory.Place(slot, C, C, new Vector2(0, y), new Vector2(380, 64));
            UIFactory.Button(slot, label, bg, fg, onClick, 28);
        }

        void BuildOptions()
        {
            _optionsPanel = UIFactory.AddChild(_root.transform, "Options").gameObject;
            UIFactory.Stretch((RectTransform)_optionsPanel.transform);
            var title = UIFactory.Label(_optionsPanel.transform, "OPTIONS", 48, ArtPalette.NeonCyan, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, C, C, new Vector2(0, 200), new Vector2(600, 60));

            _sensValue = OptionRow(_optionsPanel.transform, 90, "SENSIBILITÉ SOURIS",
                () => { Settings.MouseSensitivity -= 0.2f; RefreshOptions(); },
                () => { Settings.MouseSensitivity += 0.2f; RefreshOptions(); });
            _volValue = OptionRow(_optionsPanel.transform, 10, "VOLUME",
                () => { Settings.MasterVolume -= 0.05f; RefreshOptions(); },
                () => { Settings.MasterVolume += 0.05f; RefreshOptions(); });

            var back = UIFactory.AddChild(_optionsPanel.transform, "Back");
            UIFactory.Place(back, C, C, new Vector2(0, -120), new Vector2(300, 60));
            UIFactory.Button(back, "RETOUR", ArtPalette.NeonCyan, ArtPalette.UiInk,
                () => { _optionsPanel.SetActive(false); _mainPanel.SetActive(true); }, 26);

            _optionsPanel.SetActive(false);
        }

        Text OptionRow(Transform parent, float y, string label, Action minus, Action plus)
        {
            var row = UIFactory.AddChild(parent, "Opt_" + label);
            UIFactory.Place(row, C, C, new Vector2(0, y), new Vector2(640, 56));
            UIFactory.Panel(row, new Color(ArtPalette.UiInk.r, ArtPalette.UiInk.g, ArtPalette.UiInk.b, 0.85f));

            var lbl = UIFactory.Label(row, label, 22, ArtPalette.UiText, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(lbl.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(360, 40));

            var minusBtn = UIFactory.AddChild(row, "Minus");
            UIFactory.Place(minusBtn, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-200, 0), new Vector2(52, 44));
            UIFactory.Button(minusBtn, "−", ArtPalette.Cover, ArtPalette.UiText, minus, 30);

            var val = UIFactory.Label(row, "", 26, ArtPalette.NeonCyan, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Place(val.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-132, 0), new Vector2(120, 44));

            var plusBtn = UIFactory.AddChild(row, "Plus");
            UIFactory.Place(plusBtn, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-62, 0), new Vector2(52, 44));
            UIFactory.Button(plusBtn, "+", ArtPalette.Cover, ArtPalette.UiText, plus, 30);
            return val;
        }

        void RefreshOptions()
        {
            if (_sensValue != null) _sensValue.text = Settings.MouseSensitivity.ToString("0.0");
            if (_volValue != null) _volValue.text = Mathf.RoundToInt(Settings.MasterVolume * 100f) + "%";
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        static readonly Vector2 C = new Vector2(0.5f, 0.5f);
    }
}
