using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FirstGame.Core;
using FirstGame.Combat;

namespace FirstGame.UI
{
    /// <summary>Top-right elimination feed (max 5 lines, each ~5s). Wire via Init() to a weapon.</summary>
    public class KillFeed : MonoBehaviour
    {
        class Line : MonoBehaviour { public float born; }

        readonly List<RectTransform> _lines = new();
        Transform _root;
        WeaponController _weapon;

        public static void Init(WeaponController weapon)
        {
            var canvas = UIFactory.CreateCanvas("KillFeed", 7);
            var kf = canvas.gameObject.AddComponent<KillFeed>();
            kf._root = canvas.transform;
            kf._weapon = weapon;
            if (weapon != null) weapon.OnKill += kf.OnKill;
        }

        void OnKill(bool head)
        {
            string wpn = _weapon != null && _weapon.weapon != null ? _weapon.weapon.nameFr : "arme";
            Add($"VOUS  ▸  {wpn}  ▸  {(head ? "ÉLIMINATION — TÊTE" : "ÉLIMINATION")}",
                head ? ArtPalette.Objective : ArtPalette.NeonCyan);
        }

        public void Add(string text, Color color)
        {
            var line = UIFactory.AddChild(_root, "Line");
            UIFactory.Place(line, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -220), new Vector2(460, 34));
            UIFactory.Panel(line, new Color(ArtPalette.UiInk.r, ArtPalette.UiInk.g, ArtPalette.UiInk.b, 0.75f));
            var t = UIFactory.Label(line, text, 18, color, TextAnchor.MiddleRight, FontStyle.Bold);
            UIFactory.Stretch(t.rectTransform, 10);
            line.gameObject.AddComponent<Line>().born = Time.time;
            _lines.Add(line);
            while (_lines.Count > 5) { Destroy(_lines[0].gameObject); _lines.RemoveAt(0); }
            Reposition();
        }

        void Update()
        {
            bool changed = false;
            for (int i = _lines.Count - 1; i >= 0; i--)
            {
                var l = _lines[i] != null ? _lines[i].GetComponent<Line>() : null;
                if (l == null || Time.time - l.born > 5f) { if (_lines[i]) Destroy(_lines[i].gameObject); _lines.RemoveAt(i); changed = true; }
            }
            if (changed) Reposition();
        }

        void Reposition()
        {
            for (int i = 0; i < _lines.Count; i++)
                if (_lines[i] != null) _lines[i].anchoredPosition = new Vector2(-40, -220 - i * 40);
        }
    }
}
