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
    /// <summary>Pre-round "build" composer (MMORPG-style, no shop): pick your agent (→ 3 spells) and
    /// your 2 weapons, then start. Applies live so it also works when re-opened mid-match.</summary>
    public class LoadoutScreen : MonoBehaviour
    {
        string _agentId, _primaryId, _secondaryId;
        Action _onReady;
        GameObject _canvasGo;
        readonly List<(string id, Image img)> _agentBtns = new();
        readonly List<(string id, Image img)> _primBtns = new();
        readonly List<(string id, Image img)> _secBtns = new();
        Text _spellPreview;

        public void Show(Action onReady)
        {
            var p = PlayerProfile.Current;
            _agentId = p.agentId; _primaryId = p.weaponId; _secondaryId = p.secondaryWeaponId;
            _onReady = onReady;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
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

            var title = UIFactory.Label(t, "COMPOSE TON BUILD", 46, ArtPalette.NeonMag, TextAnchor.UpperCenter, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -56), new Vector2(1200, 54));
            var sub = UIFactory.Label(t, "Choisis ton agent (3 sorts) et tes 2 armes. Rééquipable en pause (mi-temps).", 20, ArtPalette.UiDim, TextAnchor.UpperCenter);
            UIFactory.Place(sub.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -112), new Vector2(1200, 28));

            Header(t, "AGENT", -166);
            var agents = AgentCatalog.All;
            for (int i = 0; i < agents.Count; i++)
            {
                var a = agents[i]; string id = a.id;
                var slot = UIFactory.AddChild(t, "Ag_" + id);
                UIFactory.Place(slot, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-450 + i * 300, -206), new Vector2(280, 62));
                var btn = UIFactory.Button(slot, $"{a.nameFr}  ·  {a.role}", ArtPalette.Cover, ArtPalette.UiText, () => { _agentId = id; Refresh(); }, 20);
                _agentBtns.Add((id, btn.GetComponent<Image>()));
            }
            _spellPreview = UIFactory.Label(t, "", 18, ArtPalette.UiText, TextAnchor.UpperCenter, FontStyle.Bold);
            UIFactory.Place(_spellPreview.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -280), new Vector2(1300, 30));

            Header(t, "ARME PRINCIPALE  (touche 1)", -338);
            BuildWeaponRow(t, -378, _primBtns, id => { _primaryId = id; Refresh(); });
            Header(t, "ARME SECONDAIRE  (touche 2 / molette)", -466);
            BuildWeaponRow(t, -506, _secBtns, id => { _secondaryId = id; Refresh(); });

            var ready = UIFactory.AddChild(t, "Ready");
            UIFactory.Place(ready, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 72), new Vector2(380, 68));
            UIFactory.Button(ready, "PRÊT — LANCER", ArtPalette.NeonCyan, ArtPalette.UiInk, Ready, 26);
            var back = UIFactory.AddChild(t, "Back");
            UIFactory.Place(back, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-70, 72), new Vector2(260, 56));
            UIFactory.Button(back, "RETOUR AU MENU", ArtPalette.Cover, ArtPalette.UiText,
                () => { Time.timeScale = 1f; GameManager.LoadScene(SceneNames.MainMenu); }, 20);
        }

        void Header(Transform t, string s, float y)
        {
            var h = UIFactory.Label(t, s, 22, ArtPalette.NeonCyan, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(h.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-600, y), new Vector2(700, 26));
        }

        void BuildWeaponRow(Transform t, float y, List<(string id, Image img)> store, Action<string> onPick)
        {
            var ws = WeaponCatalog.Weapons;
            for (int i = 0; i < ws.Count; i++)
            {
                var w = ws[i]; string id = w.id;
                var slot = UIFactory.AddChild(t, "W_" + y + "_" + id);
                UIFactory.Place(slot, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-600 + i * 252, y), new Vector2(240, 66));
                var btn = UIFactory.Button(slot, $"{w.nameFr}\n{w.damage:0} dgt · {w.category}", ArtPalette.Cover, ArtPalette.UiText, () => onPick(id), 16);
                store.Add((id, btn.GetComponent<Image>()));
            }
        }

        void Refresh()
        {
            foreach (var (id, img) in _agentBtns) if (img) img.color = id == _agentId ? ArtPalette.NeonMag : ArtPalette.Cover;
            foreach (var (id, img) in _primBtns) if (img) img.color = id == _primaryId ? ArtPalette.NeonCyan : ArtPalette.Cover;
            foreach (var (id, img) in _secBtns) if (img) img.color = id == _secondaryId ? ArtPalette.Objective : ArtPalette.Cover;

            var a = AgentCatalog.ById(_agentId);
            if (a != null && _spellPreview != null)
            {
                string s = "";
                for (int i = 0; i < 3; i++)
                {
                    var ab = AbilityCatalog.ById(a.abilityIds[i]);
                    s += (i > 0 ? "      ·      " : "") + $"[{"EFC"[i]}] " + (ab != null ? ab.nameFr : a.abilityIds[i]);
                }
                _spellPreview.text = s;
            }
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
    }
}
