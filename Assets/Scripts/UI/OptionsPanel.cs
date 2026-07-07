using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FirstGame.Core;

namespace FirstGame.UI
{
    /// <summary>
    /// Shared, scrollable Options screen built entirely in code. Used by BOTH the main menu and the
    /// pause menu. Covers sensitivity / ADS multiplier / invert-Y, volume, FOV, fullscreen mode,
    /// resolution, quality, VSync, full key rebinding, and a reset-to-defaults button. All values
    /// persist through <see cref="Settings"/> and <see cref="Keybinds"/>.
    /// </summary>
    public class OptionsPanel : MonoBehaviour
    {
        Action _onClose;
        Camera _liveCam;                 // optional in-game camera to apply FOV live (pause menu)
        RectTransform _content;
        readonly List<Action> _refreshers = new();
        readonly Dictionary<GameAction, Text> _bindTexts = new();

        // capture state for rebinding
        GameAction? _capturing;
        int _captureFrame;

        // cycle indices
        List<Vector2Int> _resolutions;
        int _resIndex, _fsIndex, _qualityIndex;

        static readonly (FullScreenMode mode, string fr)[] FsModes =
        {
            (FullScreenMode.FullScreenWindow,    "PLEIN ÉCRAN"),
            (FullScreenMode.Windowed,            "FENÊTRÉ"),
            (FullScreenMode.ExclusiveFullScreen, "EXCLUSIF"),
        };

        static readonly Color RowBg   = new(ArtPalette.UiInk.r, ArtPalette.UiInk.g, ArtPalette.UiInk.b, 0.85f);
        static readonly Color OnColor = new(0.12f, 0.40f, 0.36f, 1f);

        /// <summary>Builds a full-screen options panel under <paramref name="parent"/>.</summary>
        public static OptionsPanel Create(Transform parent, Action onClose, Camera liveCam = null)
        {
            var go = UIFactory.AddChild(parent, "OptionsPanel").gameObject;
            var op = go.AddComponent<OptionsPanel>();
            op._onClose = onClose;
            op._liveCam = liveCam;
            op.Build();
            return op;
        }

        void Build()
        {
            var rt = (RectTransform)transform;
            UIFactory.Stretch(rt);
            var bg = gameObject.AddComponent<Image>();
            bg.color = new Color(ArtPalette.Sky.r, ArtPalette.Sky.g, ArtPalette.Sky.b, 0.97f);

            var title = UIFactory.Label(transform, "OPTIONS", 46, ArtPalette.NeonCyan, TextAnchor.UpperCenter, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -34), new Vector2(700, 60));

            BuildScroll();
            BuildResolutions();
            SyncIndices();
            BuildRows();

            // Footer buttons (fixed, outside the scroll area)
            var reset = UIFactory.AddChild(transform, "Reset");
            UIFactory.Place(reset, new Vector2(0, 0), new Vector2(0, 0), new Vector2(120, 44), new Vector2(330, 58));
            UIFactory.Button(reset, "RÉINITIALISER LES RÉGLAGES", ArtPalette.Enemy, ArtPalette.UiInk, () =>
            {
                Settings.ResetAll();
                BuildResolutions();
                SyncIndices();
                ApplyLiveFov();
                RefreshAll();
            }, 20);

            var back = UIFactory.AddChild(transform, "Back");
            UIFactory.Place(back, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-120, 44), new Vector2(260, 58));
            UIFactory.Button(back, "RETOUR", ArtPalette.NeonCyan, ArtPalette.UiInk, () =>
            {
                _capturing = null;
                _onClose?.Invoke();
            }, 26);

            RefreshAll();
        }

        void BuildScroll()
        {
            var scroll = UIFactory.AddChild(transform, "Scroll");
            scroll.anchorMin = new Vector2(0.5f, 0);
            scroll.anchorMax = new Vector2(0.5f, 1);
            scroll.pivot = new Vector2(0.5f, 0.5f);
            scroll.sizeDelta = new Vector2(820, -240); // width 820, height = parent - 240
            scroll.anchoredPosition = new Vector2(0, -8);

            var sr = scroll.gameObject.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 28f;

            var viewport = UIFactory.AddChild(scroll, "Viewport");
            UIFactory.Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            sr.viewport = viewport;

            _content = UIFactory.AddChild(viewport, "Content");
            _content.anchorMin = new Vector2(0, 1);
            _content.anchorMax = new Vector2(1, 1);
            _content.pivot = new Vector2(0.5f, 1);
            _content.anchoredPosition = Vector2.zero;
            _content.sizeDelta = Vector2.zero;
            var vlg = _content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;  vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            vlg.spacing = 10;
            vlg.padding = new RectOffset(12, 12, 12, 12);
            var fitter = _content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = _content;
        }

        void BuildRows()
        {
            Header("VISÉE & SOURIS");
            Choice("SENSIBILITÉ SOURIS",
                () => Settings.MouseSensitivity -= 0.2f,
                () => Settings.MouseSensitivity += 0.2f,
                () => Settings.MouseSensitivity.ToString("0.0"));
            Choice("SENSIBILITÉ ADS (×)",
                () => Settings.AdsSensMultiplier -= 0.05f,
                () => Settings.AdsSensMultiplier += 0.05f,
                () => Settings.AdsSensMultiplier.ToString("0.00"));
            Toggle("INVERSER L'AXE Y", () => Settings.InvertY, v => Settings.InvertY = v);

            Header("AUDIO");
            Choice("VOLUME",
                () => Settings.MasterVolume -= 0.05f,
                () => Settings.MasterVolume += 0.05f,
                () => Mathf.RoundToInt(Settings.MasterVolume * 100f) + "%");

            Header("GRAPHISMES");
            Choice("CHAMP DE VISION (FOV)",
                () => { Settings.FieldOfView -= 5f; ApplyLiveFov(); },
                () => { Settings.FieldOfView += 5f; ApplyLiveFov(); },
                () => Mathf.RoundToInt(Settings.FieldOfView) + "°");
            Choice("MODE D'AFFICHAGE",
                () => CycleFs(-1), () => CycleFs(1),
                () => FsModes[_fsIndex].fr);
            Choice("RÉSOLUTION",
                () => CycleRes(-1), () => CycleRes(1),
                () => $"{_resolutions[_resIndex].x} × {_resolutions[_resIndex].y}");
            Choice("QUALITÉ",
                () => CycleQuality(-1), () => CycleQuality(1),
                () => QualitySettings.names.Length > 0 ? QualitySettings.names[_qualityIndex] : "—");
            Toggle("SYNCHRO VERTICALE (VSync)", () => Settings.VSync, v => Settings.VSync = v);

            Header("COMMANDES (clique pour remapper)");
            foreach (var a in Keybinds.All) Keybind(a);
        }

        // ---------- cycle helpers ----------
        void CycleFs(int dir)
        {
            _fsIndex = (_fsIndex + dir + FsModes.Length) % FsModes.Length;
            Settings.FullscreenMode = (int)FsModes[_fsIndex].mode;
        }

        void CycleRes(int dir)
        {
            if (_resolutions.Count == 0) return;
            _resIndex = (_resIndex + dir + _resolutions.Count) % _resolutions.Count;
            var r = _resolutions[_resIndex];
            Settings.SetResolution(r.x, r.y);
        }

        void CycleQuality(int dir)
        {
            int n = QualitySettings.names.Length;
            if (n == 0) return;
            _qualityIndex = (_qualityIndex + dir + n) % n;
            Settings.QualityLevel = _qualityIndex;
        }

        void BuildResolutions()
        {
            _resolutions = new List<Vector2Int>();
            foreach (var r in Screen.resolutions)
            {
                var v = new Vector2Int(r.width, r.height);
                if (!_resolutions.Contains(v)) _resolutions.Add(v);
            }
            if (_resolutions.Count == 0) _resolutions.Add(new Vector2Int(Screen.width, Screen.height));
        }

        void SyncIndices()
        {
            _fsIndex = Array.FindIndex(FsModes, m => (int)m.mode == Settings.FullscreenMode);
            if (_fsIndex < 0) _fsIndex = 0;
            _qualityIndex = Mathf.Clamp(Settings.QualityLevel, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            _resIndex = _resolutions.FindIndex(v => v.x == Settings.ResWidth && v.y == Settings.ResHeight);
            if (_resIndex < 0) _resIndex = _resolutions.Count - 1;
        }

        void ApplyLiveFov() { if (_liveCam != null) _liveCam.fieldOfView = Settings.FieldOfView; }

        // ---------- row builders ----------
        RectTransform Row(float height)
        {
            var outer = UIFactory.AddChild(_content, "Row");
            var le = outer.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height; le.minHeight = height;
            var inner = UIFactory.AddChild(outer, "Inner");
            UIFactory.Stretch(inner); // stable anchors for our absolutely-placed controls
            return inner;
        }

        void Header(string text)
        {
            var inner = Row(40);
            var lbl = UIFactory.Label(inner, text, 20, ArtPalette.NeonCyan, TextAnchor.LowerLeft, FontStyle.Bold);
            UIFactory.Place(lbl.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(6, 0), new Vector2(700, 34));
        }

        void Choice(string label, Action left, Action right, Func<string> read)
        {
            var inner = Row(56);
            UIFactory.Panel(inner, RowBg);

            var lbl = UIFactory.Label(inner, label, 22, ArtPalette.UiText, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(lbl.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(430, 40));

            var minus = UIFactory.AddChild(inner, "Minus");
            UIFactory.Place(minus, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-206, 0), new Vector2(52, 44));
            UIFactory.Button(minus, "−", ArtPalette.Cover, ArtPalette.UiText, () => { left(); RefreshAll(); }, 30);

            var val = UIFactory.Label(inner, "", 24, ArtPalette.NeonCyan, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Place(val.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-128, 0), new Vector2(140, 44));

            var plus = UIFactory.AddChild(inner, "Plus");
            UIFactory.Place(plus, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-50, 0), new Vector2(52, 44));
            UIFactory.Button(plus, "+", ArtPalette.Cover, ArtPalette.UiText, () => { right(); RefreshAll(); }, 30);

            _refreshers.Add(() => val.text = read());
        }

        void Toggle(string label, Func<bool> read, Action<bool> write)
        {
            var inner = Row(56);
            UIFactory.Panel(inner, RowBg);

            var lbl = UIFactory.Label(inner, label, 22, ArtPalette.UiText, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(lbl.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(430, 40));

            var slot = UIFactory.AddChild(inner, "Toggle");
            UIFactory.Place(slot, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-90, 0), new Vector2(150, 44));
            var btn = UIFactory.Button(slot, "", ArtPalette.Cover, ArtPalette.UiInk, () => { write(!read()); RefreshAll(); }, 22);
            var img = btn.GetComponent<Image>();
            var txt = btn.GetComponentInChildren<Text>();

            _refreshers.Add(() =>
            {
                bool on = read();
                txt.text = on ? "OUI" : "NON";
                img.color = on ? OnColor : ArtPalette.Cover;
            });
        }

        void Keybind(GameAction a)
        {
            var inner = Row(48);
            UIFactory.Panel(inner, RowBg);

            var lbl = UIFactory.Label(inner, Keybinds.ActionLabel(a), 20, ArtPalette.UiText, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(lbl.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(430, 36));

            var slot = UIFactory.AddChild(inner, "Bind");
            UIFactory.Place(slot, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-110, 0), new Vector2(190, 40));
            var btn = UIFactory.Button(slot, "", ArtPalette.Cover, ArtPalette.UiText, () => BeginCapture(a), 20);
            _bindTexts[a] = btn.GetComponentInChildren<Text>();

            _refreshers.Add(() =>
            {
                if (_bindTexts.TryGetValue(a, out var t) && t != null)
                    t.text = (_capturing == a) ? "APPUYEZ…" : Keybinds.KeyName(Keybinds.Get(a));
            });
        }

        void BeginCapture(GameAction a)
        {
            _capturing = a;
            _captureFrame = Time.frameCount; // ignore the click that started capture
            RefreshAll();
        }

        void RefreshAll()
        {
            foreach (var r in _refreshers) r();
        }

        void Update()
        {
            if (_capturing == null) return;
            if (Time.frameCount <= _captureFrame) return; // skip the frame capture started

            if (Input.GetKeyDown(KeyCode.Escape)) { _capturing = null; RefreshAll(); return; }

            foreach (var k in Keybinds.Capturable)
            {
                if (!Input.GetKeyDown(k)) continue;
                Keybinds.Set(_capturing.Value, k);
                _capturing = null;
                RefreshAll();
                return;
            }
        }
    }
}
