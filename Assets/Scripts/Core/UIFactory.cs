using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace FirstGame.Core
{
    /// <summary>Helpers to build legacy uGUI in code (no scene wiring, no TMP import step).</summary>
    public static class UIFactory
    {
        static Font _font;
        public static Font Font
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (_font == null)
                        _font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Liberation Sans", "Segoe UI" }, 16);
                }
                return _font;
            }
        }

        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        public static Canvas CreateCanvas(string name = "Canvas", int sortOrder = 0)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform AddChild(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        public static Image Panel(Transform parent, Color color)
        {
            var rt = AddChild(parent, "Panel");
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color,
                                 TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var rt = AddChild(parent, "Text");
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button Button(Transform parent, string label, Color bg, Color fg, Action onClick, int fontSize = 30)
        {
            var rt = AddChild(parent, "Button_" + label);
            Stretch(rt); // fill the parent slot so the background matches the intended button size
            var img = rt.gameObject.AddComponent<Image>();
            img.color = bg;
            var btn = rt.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.75f, 1f, 1f, 1f); // cyan-ish hover
            colors.pressedColor = new Color(0.6f, 0.9f, 0.9f, 1f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => { UiAudio.I.Click(); onClick?.Invoke(); });

            var trigger = rt.gameObject.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => UiAudio.I.Hover());
            trigger.triggers.Add(enter);

            var txt = Label(rt, label, fontSize, fg);
            Stretch(txt.rectTransform, 8);
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap; // Wrap lets best-fit constrain width
            txt.resizeTextForBestFit = true;                  // shrink to fit so labels never truncate
            txt.resizeTextMinSize = 8;
            txt.resizeTextMaxSize = fontSize;
            return btn;
        }

        public static void Anchor(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
                                  Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        }

        public static void Stretch(RectTransform rt, float padding = 0f)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        /// <summary>Positions a rect by centre anchor with an explicit pixel size.</summary>
        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = anchor; rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }
    }
}
