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
    /// <summary>Pre-round build composer (no shop). Fully custom: pick a class (passive), your 3 spells
    /// freely from the list (E/F/C), and 2 weapons. Applies live so it also works re-opened mid-match.</summary>
    public class LoadoutScreen : MonoBehaviour
    {
        string _agentId, _primaryId, _secondaryId;
        int _activeSlot;
        Action _onReady;
        GameObject _canvasGo;

        readonly List<(string id, Image img)> _classBtns = new();
        readonly List<(string id, Image img)> _primBtns = new();
        readonly List<(string id, Image img)> _secBtns = new();
        readonly Image[] _slotBorder = new Image[3];
        readonly Text[] _slotLabel = new Text[3];
        readonly List<(string id, Image img)> _abilityCards = new();

        public void Show(Action onReady)
        {
            var p = PlayerProfile.Current;
            _agentId = p.agentId; _primaryId = p.weaponId; _secondaryId = p.secondaryWeaponId; _activeSlot = 0;
            _onReady = onReady;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            Time.timeScale = 0f;
            Build();
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

            var title = UIFactory.Label(t, "COMPOSE TON BUILD", 42, ArtPalette.NeonMag, TextAnchor.UpperCenter, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, C1, C1, new Vector2(0, -34), new Vector2(1200, 50));

            // --- Class (passive) ---
            Header(t, "CLASSE  (passive)", -100);
            var agents = AgentCatalog.All;
            for (int i = 0; i < agents.Count; i++)
            {
                var a = agents[i]; string id = a.id;
                var slot = UIFactory.AddChild(t, "Cl_" + id);
                UIFactory.Place(slot, C1, C1, new Vector2(-435 + i * 290, -140), new Vector2(272, 52));
                var btn = UIFactory.Button(slot, $"{a.nameFr} · {a.role}", ArtPalette.Cover, ArtPalette.UiText, () => { _agentId = id; Refresh(); }, 18);
                _classBtns.Add((id, btn.GetComponent<Image>()));
            }

            // --- Spell slots (click to select, then pick a spell below) ---
            Header(t, "SORTS ÉQUIPÉS  (clique un emplacement puis un sort)", -212);
            for (int i = 0; i < 3; i++)
            {
                int slotIdx = i;
                var chip = UIFactory.AddChild(t, "Slot_" + i);
                UIFactory.Place(chip, C1, C1, new Vector2(-360 + i * 360, -252), new Vector2(344, 54));
                _slotBorder[i] = chip.gameObject.AddComponent<Image>();
                var inner = UIFactory.AddChild(chip, "In");
                UIFactory.Place(inner, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(336, 46));
                var ib = inner.gameObject.AddComponent<Image>();
                ib.color = ArtPalette.UiInk;
                var b = inner.gameObject.AddComponent<Button>(); b.targetGraphic = ib;
                b.onClick.AddListener(() => { _activeSlot = slotIdx; Refresh(); });
                _slotLabel[i] = UIFactory.Label(inner, "", 18, ArtPalette.UiText, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIFactory.Stretch(_slotLabel[i].rectTransform, 8);
            }

            // --- Spell picker (all 10) ---
            var all = AbilityCatalog.All;
            for (int i = 0; i < all.Count; i++)
            {
                var a = all[i]; string id = a.id;
                int col = i % 5, row = i / 5;
                var card = UIFactory.AddChild(t, "Ab_" + id);
                UIFactory.Place(card, C1, C1, new Vector2(-540 + col * 272, -320 - row * 68), new Vector2(260, 60));
                var img = card.gameObject.AddComponent<Image>(); img.color = ArtPalette.Cover;
                var btn = card.gameObject.AddComponent<Button>(); btn.targetGraphic = img;
                btn.onClick.AddListener(() => AssignSpell(id));
                var sw = UIFactory.AddChild(card, "Sw");
                UIFactory.Place(sw, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(20, 40));
                sw.gameObject.AddComponent<Image>().color = a.color;
                var nm = UIFactory.Label(card, a.nameFr, 15, ArtPalette.UiText, TextAnchor.MiddleLeft, FontStyle.Bold);
                UIFactory.Place(nm.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(40, 0), new Vector2(-8, 0));
                _abilityCards.Add((id, img));
            }

            // --- Weapons ---
            Header(t, "ARME PRINCIPALE  (1)", -520);
            WeaponRow(t, -556, _primBtns, id => { _primaryId = id; Refresh(); });
            Header(t, "ARME SECONDAIRE  (2 / molette)", -638);
            WeaponRow(t, -674, _secBtns, id => { _secondaryId = id; Refresh(); });

            var ready = UIFactory.AddChild(t, "Ready");
            UIFactory.Place(ready, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 66), new Vector2(360, 62));
            UIFactory.Button(ready, "PRÊT — LANCER", ArtPalette.NeonCyan, ArtPalette.UiInk, Ready, 24);
            var back = UIFactory.AddChild(t, "Back");
            UIFactory.Place(back, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-60, 66), new Vector2(250, 52));
            UIFactory.Button(back, "RETOUR AU MENU", ArtPalette.Cover, ArtPalette.UiText,
                () => { Time.timeScale = 1f; GameManager.LoadScene(SceneNames.MainMenu); }, 18);
        }

        void AssignSpell(string id)
        {
            PlayerProfile.Current.SetAbility(_activeSlot, id); // swaps to keep 3 distinct, saves
            _activeSlot = (_activeSlot + 1) % 3;
            Refresh();
        }

        void WeaponRow(Transform t, float y, List<(string id, Image img)> store, Action<string> onPick)
        {
            var ws = WeaponCatalog.Weapons;
            for (int i = 0; i < ws.Count; i++)
            {
                var w = ws[i]; string id = w.id;
                var slot = UIFactory.AddChild(t, "W_" + y + "_" + id);
                UIFactory.Place(slot, C1, C1, new Vector2(-560 + i * 236, y), new Vector2(224, 60));
                var btn = UIFactory.Button(slot, $"{w.nameFr}\n{w.damage:0} dgt · {w.category}", ArtPalette.Cover, ArtPalette.UiText, () => onPick(id), 15);
                store.Add((id, btn.GetComponent<Image>()));
            }
        }

        void Header(Transform t, string s, float y)
        {
            var h = UIFactory.Label(t, s, 20, ArtPalette.NeonCyan, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(h.rectTransform, C1, C1, new Vector2(-600, y), new Vector2(900, 24));
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
                if (_slotLabel[i] != null) { _slotLabel[i].text = $"[{"EFC"[i]}]  {(a != null ? a.nameFr : "—")}"; _slotLabel[i].color = a != null ? a.color : ArtPalette.UiDim; }
                if (_slotBorder[i] != null) _slotBorder[i].color = i == _activeSlot ? ArtPalette.NeonCyan : new Color(1, 1, 1, 0.12f);
            }

            var equipped = new HashSet<string> { p.GetAbility(0), p.GetAbility(1), p.GetAbility(2) };
            foreach (var (id, img) in _abilityCards)
                if (img) img.color = equipped.Contains(id) ? new Color(0.10f, 0.34f, 0.40f, 1f) : ArtPalette.Cover;
        }

        void Ready()
        {
            Time.timeScale = 1f;
            var p = PlayerProfile.Current;
            p.SetAgent(_agentId);
            p.SetWeapon(_primaryId);
            p.SetSecondaryWeapon(_secondaryId);

            var abilities = FindAnyObjectByType<AbilitySystem>();
            if (abilities != null) abilities.ReloadLoadout();
            var weapon = FindAnyObjectByType<WeaponController>();
            if (weapon != null) weapon.SetLoadout(WeaponCatalog.ById(_primaryId), WeaponCatalog.ById(_secondaryId));

            if (_canvasGo != null) Destroy(_canvasGo);
            _onReady?.Invoke();
            Destroy(gameObject);
        }

        static readonly Vector2 C1 = new Vector2(0.5f, 1f);
    }
}
