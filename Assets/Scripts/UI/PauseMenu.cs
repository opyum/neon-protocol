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
                () => { _mainPanel.SetActive(false); _optionsPanel.SetActive(true); });
            MenuBtn(_mainPanel.transform, -90, "MENU PRINCIPAL", ArtPalette.Cover, ArtPalette.UiText,
                () =>
                {
                    Time.timeScale = 1f;
                    var nm = Unity.Netcode.NetworkManager.Singleton;
                    if (nm != null && nm.IsListening) nm.Shutdown();
                    GameManager.LoadScene(SceneNames.MainMenu);
                });
            MenuBtn(_mainPanel.transform, -170, "QUITTER", ArtPalette.Enemy, ArtPalette.UiInk, Quit);

            // Shared options screen. Camera.main is the live gameplay camera so FOV updates instantly.
            var op = OptionsPanel.Create(_root.transform,
                () => { _optionsPanel.SetActive(false); _mainPanel.SetActive(true); },
                Camera.main);
            _optionsPanel = op.gameObject;
            _optionsPanel.SetActive(false);

            _root.SetActive(false);
        }

        void MenuBtn(Transform parent, float y, string label, Color bg, Color fg, Action onClick)
        {
            var slot = UIFactory.AddChild(parent, "Btn_" + label);
            UIFactory.Place(slot, C, C, new Vector2(0, y), new Vector2(380, 64));
            UIFactory.Button(slot, label, bg, fg, onClick, 28);
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
