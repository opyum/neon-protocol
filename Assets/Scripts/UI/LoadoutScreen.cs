using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FirstGame.Core;
using FirstGame.Progression;
using FirstGame.Agents;
using FirstGame.Abilities;
using FirstGame.Combat;

namespace FirstGame.UI
{
    /// <summary>Full RPG build composer: 3 active spells (from 100) + 3 passives (from 100) + class +
    /// 2 weapons. Click a slot, then pick from the scrollable list. Applies live.</summary>
    public class LoadoutScreen : MonoBehaviour
    {
        string _agentId, _primaryId, _secondaryId;
        bool _passiveKind;   // false = active slot selected, true = passive slot selected
        int _selSlot;
        Action _onReady;
        GameObject _canvasGo;

        readonly List<(string id, Image img)> _classBtns = new();
        readonly List<(string id, Image img)> _primBtns = new();
        readonly List<(string id, Image img)> _secBtns = new();
        readonly Image[] _activeBorder = new Image[3];
        readonly Text[] _activeLabel = new Text[3];
        readonly Image[] _passiveBorder = new Image[3];
        readonly Text[] _passiveLabel = new Text[3];
        readonly Dictionary<string, Image> _listImgs = new();
        RectTransform _content;

        public void Show(Action onReady)
        {
            var p = PlayerProfile.Current;
            _agentId = p.agentId; _primaryId = p.weaponId; _secondaryId = p.secondaryWeaponId;
            _onReady = onReady;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            Time.timeScale = 0f;
            Build();
            RebuildList();
            Refresh();
        }

        void Build()
        {
            UIFactory.EnsureEventSystem();
            var canvas = UIFactory.CreateCanvas("Loadout", 18);
            _canvasGo = canvas.gameObject;
            var t = canvas.transform;
            var bg = UIFactory.Panel(t, new Color(ArtPalette.Sky.r, ArtPalette.Sky.g, ArtPalette.Sky.b, 0.97f));
            UIFactory.Stretch(bg.rectTransform);

            var title = UIFactory.Label(t, "COMPOSE TON BUILD", 38, ArtPalette.NeonMag, TextAnchor.UpperCenter, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, C1, C1, new Vector2(0, -22), new Vector2(1200, 46));

            // Class row
            var agents = AgentCatalog.All;
            for (int i = 0; i < agents.Count; i++)
            {
                var a = agents[i]; string id = a.id;
                var slot = UIFactory.AddChild(t, "Cl_" + id);
                UIFactory.Place(slot, C1, C1, new Vector2(-435 + i * 292, -80), new Vector2(276, 44));
                var btn = UIFactory.Button(slot, $"{a.nameFr} · {a.role}", ArtPalette.Cover, ArtPalette.UiText, () => { _agentId = id; Refresh(); }, 17);
                _classBtns.Add((id, btn.GetComponent<Image>()));
            }

            // Active + passive slot chips
            SlotChip(t, -140, false, 0, "E"); SlotChip(t, -140, false, 1, "F"); SlotChip(t, -140, false, 2, "C");
            SlotChip(t, -196, true, 0, "P1"); SlotChip(t, -196, true, 1, "P2"); SlotChip(t, -196, true, 2, "P3");

            // Scroll list (fills the middle)
            var scrollGo = UIFactory.AddChild(t, "Scroll");
            UIFactory.Place(scrollGo, C1, C1, new Vector2(0, -238), new Vector2(1780, 420));
            scrollGo.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.25f);
            scrollGo.gameObject.AddComponent<RectMask2D>();
            var sr = scrollGo.gameObject.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true; sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 40f;
            _content = UIFactory.AddChild(scrollGo, "Content");
            _content.anchorMin = new Vector2(0, 1); _content.anchorMax = new Vector2(1, 1); _content.pivot = new Vector2(0.5f, 1);
            _content.anchoredPosition = Vector2.zero; _content.sizeDelta = new Vector2(0, 100);
            sr.content = _content; sr.viewport = scrollGo;

            // Weapons
            Header(t, "ARME PRINCIPALE  (1)", -672);
            WeaponRow(t, -706, _primBtns, id => { _primaryId = id; Refresh(); });
            Header(t, "ARME SECONDAIRE  (2 / molette)", -776);
            WeaponRow(t, -810, _secBtns, id => { _secondaryId = id; Refresh(); });

            var ready = UIFactory.AddChild(t, "Ready");
            UIFactory.Place(ready, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 46), new Vector2(340, 56));
            UIFactory.Button(ready, "PRÊT — LANCER", ArtPalette.NeonCyan, ArtPalette.UiInk, Ready, 24);
            var back = UIFactory.AddChild(t, "Back");
            UIFactory.Place(back, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-60, 46), new Vector2(250, 50));
            UIFactory.Button(back, "RETOUR AU MENU", ArtPalette.Cover, ArtPalette.UiText,
                () => { Time.timeScale = 1f; GameManager.LoadScene(SceneNames.MainMenu); }, 18);
        }

        void SlotChip(Transform t, float y, bool passive, int slot, string key)
        {
            float x = (passive ? -360 : -360) + slot * 360;
            var chip = UIFactory.AddChild(t, (passive ? "P_" : "A_") + slot);
            UIFactory.Place(chip, C1, C1, new Vector2(x, y), new Vector2(344, 48));
            var border = chip.gameObject.AddComponent<Image>();
            var inner = UIFactory.AddChild(chip, "In");
            UIFactory.Place(inner, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(336, 40));
            var ib = inner.gameObject.AddComponent<Image>(); ib.color = ArtPalette.UiInk;
            var b = inner.gameObject.AddComponent<Button>(); b.targetGraphic = ib;
            b.onClick.AddListener(() => { _passiveKind = passive; _selSlot = slot; RebuildList(); Refresh(); });
            var lbl = UIFactory.Label(inner, "", 15, ArtPalette.UiText, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(lbl.rectTransform, 8);
            if (passive) { _passiveBorder[slot] = border; _passiveLabel[slot] = lbl; }
            else { _activeBorder[slot] = border; _activeLabel[slot] = lbl; }
        }

        void RebuildList()
        {
            _listImgs.Clear();
            foreach (Transform c in _content) Destroy(c.gameObject);

            const int cols = 3; const float w = 570f, h = 42f, gx = 12f, gy = 6f;
            int count;
            if (_passiveKind)
            {
                var all = PassiveCatalog.All; count = all.Count;
                for (int i = 0; i < count; i++) AddItem(i, cols, w, h, gx, gy, all[i].id, all[i].nameFr, all[i].descriptionFr, all[i].color);
            }
            else
            {
                var all = AbilityCatalog.All; count = all.Count;
                for (int i = 0; i < count; i++) AddItem(i, cols, w, h, gx, gy, all[i].id, all[i].nameFr, all[i].descriptionFr, all[i].color);
            }
            int rows = Mathf.CeilToInt(count / (float)cols);
            _content.sizeDelta = new Vector2(0, rows * (h + gy) + 14);
            HighlightList();
        }

        void AddItem(int i, int cols, float w, float h, float gx, float gy, string id, string name, string desc, Color color)
        {
            int col = i % cols, row = i / cols;
            var card = UIFactory.AddChild(_content, "It_" + id);
            UIFactory.Place(card, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14 + col * (w + gx), -8 - row * (h + gy)), new Vector2(w, h));
            var img = card.gameObject.AddComponent<Image>(); img.color = ArtPalette.Cover;
            var btn = card.gameObject.AddComponent<Button>(); btn.targetGraphic = img;
            btn.onClick.AddListener(() => Assign(id));
            var sw = UIFactory.AddChild(card, "sw");
            UIFactory.Place(sw, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(16, 28));
            sw.gameObject.AddComponent<Image>().color = color;
            var nm = UIFactory.Label(card, name, 15, ArtPalette.UiText, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(nm.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(32, 8), new Vector2(w - 40, 20));
            var ds = UIFactory.Label(card, desc, 11, ArtPalette.UiDim, TextAnchor.MiddleLeft);
            UIFactory.Place(ds.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(32, -9), new Vector2(w - 40, 16));
            _listImgs[id] = img;
        }

        void Assign(string id)
        {
            if (_passiveKind) PlayerProfile.Current.SetPassive(_selSlot, id);
            else PlayerProfile.Current.SetAbility(_selSlot, id);
            _selSlot = (_selSlot + 1) % 3;
            Refresh();
            HighlightList();
        }

        void WeaponRow(Transform t, float y, List<(string id, Image img)> store, Action<string> onPick)
        {
            var ws = WeaponCatalog.Weapons;
            for (int i = 0; i < ws.Count; i++)
            {
                var w = ws[i]; string id = w.id;
                var slot = UIFactory.AddChild(t, "W_" + y + "_" + id);
                UIFactory.Place(slot, C1, C1, new Vector2(-600 + i * 236, y), new Vector2(224, 58));
                var btn = UIFactory.Button(slot, $"{w.nameFr}\n{w.damage:0} dgt · {w.category}", ArtPalette.Cover, ArtPalette.UiText, () => onPick(id), 15);
                store.Add((id, btn.GetComponent<Image>()));
            }
        }

        void Header(Transform t, string s, float y)
        {
            var h = UIFactory.Label(t, s, 20, ArtPalette.NeonCyan, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(h.rectTransform, C1, C1, new Vector2(-600, y), new Vector2(900, 24));
        }

        void HighlightList()
        {
            var p = PlayerProfile.Current;
            var eq = new HashSet<string>();
            if (_passiveKind) { for (int i = 0; i < 3; i++) eq.Add(p.GetPassive(i)); }
            else { for (int i = 0; i < 3; i++) eq.Add(p.GetAbility(i)); }
            foreach (var kv in _listImgs)
                if (kv.Value) kv.Value.color = eq.Contains(kv.Key) ? new Color(0.10f, 0.34f, 0.40f, 1f) : ArtPalette.Cover;
        }

        void Refresh()
        {
            var p = PlayerProfile.Current;
            foreach (var (id, img) in _classBtns) if (img) img.color = id == _agentId ? ArtPalette.NeonMag : ArtPalette.Cover;
            foreach (var (id, img) in _primBtns) if (img) img.color = id == _primaryId ? ArtPalette.NeonCyan : ArtPalette.Cover;
            foreach (var (id, img) in _secBtns) if (img) img.color = id == _secondaryId ? ArtPalette.Objective : ArtPalette.Cover;

            for (int i = 0; i < 3; i++)
            {
                var a = AbilityCatalog.ById(p.GetAbility(i));
                if (_activeLabel[i]) { _activeLabel[i].text = $"[{"EFC"[i]}] {(a != null ? a.nameFr : "—")}"; _activeLabel[i].color = a != null ? a.color : ArtPalette.UiDim; }
                if (_activeBorder[i]) _activeBorder[i].color = (!_passiveKind && i == _selSlot) ? ArtPalette.NeonCyan : new Color(1, 1, 1, 0.12f);

                var pas = PassiveCatalog.ById(p.GetPassive(i));
                if (_passiveLabel[i]) { _passiveLabel[i].text = pas != null ? pas.nameFr : $"Passif {i + 1} — vide"; _passiveLabel[i].color = pas != null ? pas.color : ArtPalette.UiDim; }
                if (_passiveBorder[i]) _passiveBorder[i].color = (_passiveKind && i == _selSlot) ? ArtPalette.NeonMag : new Color(1, 1, 1, 0.12f);
            }
        }

        void Ready()
        {
            Time.timeScale = 1f;
            var p = PlayerProfile.Current;
            p.SetAgent(_agentId); p.SetWeapon(_primaryId); p.SetSecondaryWeapon(_secondaryId);

            var abilities = FindAnyObjectByType<AbilitySystem>();
            if (abilities != null) abilities.ReloadLoadout();
            var weapon = FindAnyObjectByType<WeaponController>();
            if (weapon != null) weapon.SetLoadout(WeaponCatalog.ById(_primaryId), WeaponCatalog.ById(_secondaryId));
            var passives = FindAnyObjectByType<PassiveSystem>();
            if (passives != null) passives.Reapply(); // apply the chosen passives live

            if (_canvasGo != null) Destroy(_canvasGo);
            _onReady?.Invoke();
            Destroy(gameObject);
        }

        static readonly Vector2 C1 = new Vector2(0.5f, 1f);
    }
}
