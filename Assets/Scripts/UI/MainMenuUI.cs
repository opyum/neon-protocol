using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FirstGame.Core;
using FirstGame.Progression;
using FirstGame.Abilities;
using FirstGame.Combat;

namespace FirstGame.UI
{
    /// <summary>Intro menu + character/stats screen. Built entirely in code on its own canvas.</summary>
    public class MainMenuUI : MonoBehaviour
    {
        GameObject _characterPanel;
        Text _levelChip;
        readonly Dictionary<string, Text> _statValues = new();
        Text _pointsText, _xpText;

        GameObject _loadoutPanel;
        readonly List<(string id, Image img)> _weaponButtons = new();
        readonly Text[] _equipNames = new Text[3];
        readonly Image[] _equipSwatches = new Image[3];

        static readonly (string id, string name, string effect)[] Stats =
        {
            ("vitalite",     "VITALITÉ",      "+6 PV par point"),
            ("celerite",     "CÉLÉRITÉ",      "+1,5% vitesse par point"),
            ("controle",     "CONTRÔLE",      "-2,5% dispersion par point"),
            ("focalisation", "FOCALISATION",  "-2% recharge sorts par point"),
            ("amplification","AMPLIFICATION", "+3% puissance des sorts par point"),
            ("regeneration", "RÉGÉNÉRATION",  "+0,5 PV/s régén. hors combat"),
        };

        void Start() => BuildMenu();

        void BuildMenu()
        {
            var root = transform;

            // --- Framing: left dark fade (keeps text readable over the 3D backdrop) + vignette ---
            var fade = UIFactory.AddChild(root, "LeftFade");
            UIFactory.Anchor(fade, Vector2.zero, new Vector2(0.60f, 1f), Vector2.zero, Vector2.zero);
            var fadeImg = fade.gameObject.AddComponent<Image>();
            fadeImg.sprite = Tex.HorizontalGradient(
                new Color(ArtPalette.UiInk.r, ArtPalette.UiInk.g, ArtPalette.UiInk.b, 0.96f),
                new Color(ArtPalette.UiInk.r, ArtPalette.UiInk.g, ArtPalette.UiInk.b, 0f));
            fadeImg.raycastTarget = false;

            var vig = UIFactory.AddChild(root, "Vignette");
            UIFactory.Stretch(vig);
            var vigImg = vig.gameObject.AddComponent<Image>();
            vigImg.sprite = Tex.Vignette;
            vigImg.raycastTarget = false;

            // --- Title with a soft neon glow ---
            var glow = UIFactory.Label(root, "NEON PROTOCOL", 86,
                new Color(ArtPalette.NeonCyan.r, ArtPalette.NeonCyan.g, ArtPalette.NeonCyan.b, 0.30f),
                TextAnchor.LowerLeft, FontStyle.Bold);
            UIFactory.Place(glow.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(92, -218), new Vector2(1100, 100));
            var title = UIFactory.Label(root, "NEON PROTOCOL", 82, ArtPalette.Signal, TextAnchor.LowerLeft, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(90, -220), new Vector2(1100, 96));
            var accent = UIFactory.AddChild(root, "TitleAccent");
            UIFactory.Place(accent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(94, -232), new Vector2(420, 6));
            accent.gameObject.AddComponent<Image>().color = ArtPalette.NeonCyan;
            var sub = UIFactory.Label(root, "FPS TACTIQUE  •  ENTRAÎNEMENT & CAMPAGNE", 26, ArtPalette.NeonCyan, TextAnchor.UpperLeft);
            UIFactory.Place(sub.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(96, -268), new Vector2(1000, 34));

            // --- Menu buttons (large, single-line, with subtitle) ---
            (string title, string sub, Color accent)[] items =
            {
                ("CAMPAGNE",           "Tutoriel jouable — apprends les bases", ArtPalette.NeonCyan),
                ("ENTRAÎNEMENT LIBRE", "Stand de tir : règle ta visée",         ArtPalette.NeonCyan),
                ("ARSENAL",            "Choisis ton arme et tes 3 sorts",       ArtPalette.Objective),
                ("PERSONNAGE",         "Niveaux & points de statistiques",      ArtPalette.Player),
                ("QUITTER",            "Fermer le jeu",                          ArtPalette.Enemy),
            };
            for (int i = 0; i < items.Length; i++)
            {
                int idx = i;
                MenuButton(root, -340 - i * 100, items[i].accent, items[i].title, items[i].sub, () => OnMenu(idx));
            }

            // --- Level chip (top-right) ---
            var chip = UIFactory.AddChild(root, "LevelChip");
            UIFactory.Place(chip, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-44, -44), new Vector2(340, 68));
            UIFactory.Panel(chip, new Color(ArtPalette.UiInk.r, ArtPalette.UiInk.g, ArtPalette.UiInk.b, 0.85f));
            var chipBar = UIFactory.AddChild(chip, "Bar");
            UIFactory.Place(chipBar, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(6, 68));
            chipBar.gameObject.AddComponent<Image>().color = ArtPalette.NeonCyan;
            _levelChip = UIFactory.Label(chip, "", 24, ArtPalette.UiText, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(_levelChip.rectTransform, 12);
            RefreshLevelChip();

            // --- Footer ---
            var footer = UIFactory.Label(root, "v0.2  •  Prototype jouable  •  Unity 6.1  •  ZQSD + Souris  •  Échap : menu",
                16, ArtPalette.UiDim, TextAnchor.LowerLeft);
            UIFactory.Place(footer.rectTransform, Vector2.zero, Vector2.zero, new Vector2(44, 26), new Vector2(1100, 24));

            BuildCharacterPanel(root);
            BuildLoadoutPanel(root);
        }

        void OnMenu(int index)
        {
            switch (index)
            {
                case 0: GameManager.LoadScene(SceneNames.Tutorial); break;
                case 1: GameManager.LoadScene(SceneNames.PracticeRange); break;
                case 2: _loadoutPanel.SetActive(true); RefreshLoadout(); break;
                case 3: _characterPanel.SetActive(true); RefreshCharacter(); break;
                case 4: Quit(); break;
                default: break;
            }
        }

        void RefreshLevelChip()
        {
            var p = PlayerProfile.Current;
            _levelChip.text = $"NIVEAU {p.level}  •  {p.Rank.ToUpper()}";
        }

        void MenuButton(Transform parent, float y, Color accent, string title, string subtitle, System.Action onClick)
        {
            const float w = 600f, h = 88f;
            var slot = UIFactory.AddChild(parent, "MenuBtn_" + title);
            UIFactory.Place(slot, new Vector2(0, 1), new Vector2(0, 1), new Vector2(96, y), new Vector2(w, h));

            var bg = slot.gameObject.AddComponent<Image>();
            bg.color = new Color(ArtPalette.UiInk.r, ArtPalette.UiInk.g, ArtPalette.UiInk.b, 0.82f);
            var btn = slot.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            var cb = btn.colors;
            cb.normalColor = new Color(0.82f, 0.87f, 0.94f, 1f);
            cb.highlightedColor = Color.white;
            cb.pressedColor = new Color(0.6f, 0.75f, 0.85f, 1f);
            cb.selectedColor = new Color(0.82f, 0.87f, 0.94f, 1f);
            cb.fadeDuration = 0.1f;
            btn.colors = cb;
            btn.onClick.AddListener(() => onClick());

            var bar = UIFactory.AddChild(slot, "Accent");
            UIFactory.Place(bar, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(4, 0), new Vector2(8, h - 16));
            var barImg = bar.gameObject.AddComponent<Image>(); barImg.color = accent; barImg.raycastTarget = false;

            var icon = UIFactory.AddChild(slot, "Icon");
            UIFactory.Place(icon, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(46, 0), new Vector2(30, 30));
            var iconImg = icon.gameObject.AddComponent<Image>(); iconImg.color = accent; iconImg.raycastTarget = false;

            var t = UIFactory.Label(slot, title, 34, ArtPalette.UiText, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(t.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(84, -14), new Vector2(w - 100, 40));

            var s = UIFactory.Label(slot, subtitle, 17, ArtPalette.UiDim, TextAnchor.MiddleLeft);
            UIFactory.Place(s.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(86, -56), new Vector2(w - 100, 26));
        }

        void BuildCharacterPanel(Transform root)
        {
            _characterPanel = UIFactory.AddChild(root, "CharacterPanel").gameObject;
            var prt = (RectTransform)_characterPanel.transform;
            UIFactory.Stretch(prt);
            _characterPanel.AddComponent<Image>().color = new Color(ArtPalette.Sky.r, ArtPalette.Sky.g, ArtPalette.Sky.b, 0.95f);

            var t = _characterPanel.transform;
            var head = UIFactory.Label(t, "PERSONNAGE & STATISTIQUES", 44, ArtPalette.NeonCyan, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(head.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(90, -70), new Vector2(1000, 54));

            _xpText = UIFactory.Label(t, "", 22, ArtPalette.UiDim, TextAnchor.UpperLeft);
            UIFactory.Place(_xpText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(92, -128), new Vector2(1000, 28));

            _pointsText = UIFactory.Label(t, "", 26, ArtPalette.Objective, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(_pointsText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(92, -162), new Vector2(1000, 30));

            // Stat rows
            for (int i = 0; i < Stats.Length; i++)
            {
                var s = Stats[i];
                var rowY = -220 - i * 72;

                var row = UIFactory.AddChild(t, "Stat_" + s.id);
                UIFactory.Place(row, new Vector2(0, 1), new Vector2(0, 1), new Vector2(90, rowY), new Vector2(820, 60));
                UIFactory.Panel(row, new Color(ArtPalette.UiInk.r, ArtPalette.UiInk.g, ArtPalette.UiInk.b, 0.7f));

                var name = UIFactory.Label(row, s.name, 24, ArtPalette.UiText, TextAnchor.MiddleLeft, FontStyle.Bold);
                UIFactory.Place(name.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, 8), new Vector2(300, 30));
                var eff = UIFactory.Label(row, s.effect, 15, ArtPalette.UiDim, TextAnchor.MiddleLeft);
                UIFactory.Place(eff.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, -14), new Vector2(360, 22));

                var val = UIFactory.Label(row, "0", 30, ArtPalette.NeonCyan, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIFactory.Place(val.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-150, 0), new Vector2(90, 44));
                _statValues[s.id] = val;

                var plus = UIFactory.AddChild(row, "Plus");
                UIFactory.Place(plus, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-56, 0), new Vector2(56, 44));
                string statId = s.id;
                UIFactory.Button(plus, "+", ArtPalette.NeonCyan, ArtPalette.UiInk, () => { PlayerProfile.Current.Spend(statId); RefreshCharacter(); }, 34);
            }

            // Loadout preview (default 3 abilities)
            var loadoutTitle = UIFactory.Label(t, "SORTS ÉQUIPÉS (E / F / C)", 22, ArtPalette.UiText, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(loadoutTitle.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(960, -220), new Vector2(500, 30));
            var loadout = AbilityCatalog.DefaultLoadout;
            for (int i = 0; i < loadout.Length; i++)
            {
                var a = loadout[i];
                var card = UIFactory.AddChild(t, "Ability_" + i);
                UIFactory.Place(card, new Vector2(0, 1), new Vector2(0, 1), new Vector2(960, -262 - i * 96), new Vector2(500, 84));
                UIFactory.Panel(card, new Color(ArtPalette.UiInk.r, ArtPalette.UiInk.g, ArtPalette.UiInk.b, 0.7f));
                var swatch = UIFactory.AddChild(card, "Swatch");
                UIFactory.Place(swatch, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(56, 56));
                swatch.gameObject.AddComponent<Image>().color = a.color;
                var an = UIFactory.Label(card, $"[{"EFC"[i]}]  {a.nameFr}", 20, ArtPalette.UiText, TextAnchor.UpperLeft, FontStyle.Bold);
                UIFactory.Place(an.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(84, -8), new Vector2(400, 26));
                var ad = UIFactory.Label(card, a.descriptionFr, 14, ArtPalette.UiDim, TextAnchor.UpperLeft);
                UIFactory.Place(ad.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(84, -34), new Vector2(400, 44));
            }

            // Respec + close
            var respec = UIFactory.AddChild(t, "Respec");
            UIFactory.Place(respec, Vector2.zero, Vector2.zero, new Vector2(90, 50), new Vector2(280, 60));
            UIFactory.Button(respec, "RÉINITIALISER (GRATUIT)", ArtPalette.Cover, ArtPalette.UiText, () => { PlayerProfile.Current.Respec(); RefreshCharacter(); }, 20);

            var close = UIFactory.AddChild(t, "Close");
            UIFactory.Place(close, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-90, 50), new Vector2(240, 60));
            UIFactory.Button(close, "RETOUR", ArtPalette.NeonCyan, ArtPalette.UiInk, () => { _characterPanel.SetActive(false); RefreshLevelChip(); }, 26);

            _characterPanel.SetActive(false);
        }

        void RefreshCharacter()
        {
            var p = PlayerProfile.Current;
            foreach (var s in Stats)
                if (_statValues.TryGetValue(s.id, out var lbl)) lbl.text = p.StatValue(s.id).ToString();
            _pointsText.text = p.unspentPoints > 0
                ? $"POINTS À DISTRIBUER : {p.unspentPoints}"
                : "Aucun point disponible — gagne des niveaux en jouant.";
            _xpText.text = $"Niveau {p.level} / {PlayerProfile.MaxLevel}   —   XP {p.xp} / {(p.XpForNext == int.MaxValue ? "MAX" : p.XpForNext.ToString())}   —   Palier {p.Rank}";
            RefreshLevelChip();
        }

        void BuildLoadoutPanel(Transform root)
        {
            _loadoutPanel = UIFactory.AddChild(root, "LoadoutPanel").gameObject;
            UIFactory.Stretch((RectTransform)_loadoutPanel.transform);
            _loadoutPanel.AddComponent<Image>().color = new Color(ArtPalette.Sky.r, ArtPalette.Sky.g, ArtPalette.Sky.b, 0.96f);
            var t = _loadoutPanel.transform;

            var head = UIFactory.Label(t, "ARSENAL — ARMES & SORTS", 44, ArtPalette.NeonCyan, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(head.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(90, -70), new Vector2(1100, 54));

            // --- Weapon row ---
            var wLabel = UIFactory.Label(t, "ARME", 24, ArtPalette.UiText, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(wLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(92, -130), new Vector2(400, 30));

            var weapons = WeaponCatalog.Weapons;
            for (int i = 0; i < weapons.Count; i++)
            {
                var w = weapons[i];
                var slot = UIFactory.AddChild(t, "Wpn_" + w.id);
                UIFactory.Place(slot, new Vector2(0, 1), new Vector2(0, 1), new Vector2(92 + i * 246, -170), new Vector2(232, 78));
                string id = w.id;
                var btn = UIFactory.Button(slot, $"{w.nameFr}\n{w.category}  •  {w.damage} dgt", ArtPalette.Cover, ArtPalette.UiText,
                    () => { PlayerProfile.Current.SetWeapon(id); RefreshLoadout(); }, 18);
                _weaponButtons.Add((w.id, btn.GetComponent<Image>()));
            }

            // --- Equipped chips (E / F / C) ---
            var eqLabel = UIFactory.Label(t, "SORTS ÉQUIPÉS", 24, ArtPalette.UiText, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(eqLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(92, -268), new Vector2(400, 30));

            string[] keys = { "E", "F", "C" };
            for (int i = 0; i < 3; i++)
            {
                var chip = UIFactory.AddChild(t, "Equip_" + keys[i]);
                UIFactory.Place(chip, new Vector2(0, 1), new Vector2(0, 1), new Vector2(92 + i * 300, -308), new Vector2(286, 56));
                UIFactory.Panel(chip, new Color(ArtPalette.UiInk.r, ArtPalette.UiInk.g, ArtPalette.UiInk.b, 0.8f));
                var sw = UIFactory.AddChild(chip, "Sw");
                UIFactory.Place(sw, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(36, 36));
                _equipSwatches[i] = sw.gameObject.AddComponent<Image>();
                var key = UIFactory.Label(chip, keys[i], 22, ArtPalette.Objective, TextAnchor.MiddleLeft, FontStyle.Bold);
                UIFactory.Place(key.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(58, 0), new Vector2(30, 40));
                _equipNames[i] = UIFactory.Label(chip, "", 18, ArtPalette.UiText, TextAnchor.MiddleLeft);
                UIFactory.Place(_equipNames[i].rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(90, 0), new Vector2(190, 40));
            }

            // --- Ability grid (10 cards, 2 columns) with E/F/C assign buttons ---
            var all = AbilityCatalog.All;
            for (int i = 0; i < all.Count; i++)
            {
                var a = all[i];
                int col = i / 5;
                int rowIdx = i % 5;
                var card = UIFactory.AddChild(t, "Ab_" + a.id);
                UIFactory.Place(card, new Vector2(0, 1), new Vector2(0, 1), new Vector2(92 + col * 760, -380 - rowIdx * 82), new Vector2(736, 74));
                UIFactory.Panel(card, new Color(ArtPalette.UiInk.r, ArtPalette.UiInk.g, ArtPalette.UiInk.b, 0.7f));

                var sw = UIFactory.AddChild(card, "Sw");
                UIFactory.Place(sw, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(46, 46));
                sw.gameObject.AddComponent<Image>().color = a.color;

                var nm = UIFactory.Label(card, $"{a.nameFr}  ({a.type})", 18, ArtPalette.UiText, TextAnchor.UpperLeft, FontStyle.Bold);
                UIFactory.Place(nm.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(70, -8), new Vector2(420, 24));
                var ds = UIFactory.Label(card, a.descriptionFr, 13, ArtPalette.UiDim, TextAnchor.UpperLeft);
                UIFactory.Place(ds.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(70, -32), new Vector2(430, 40));

                for (int s = 0; s < 3; s++)
                {
                    int slotIdx = s;
                    string id = a.id;
                    var b = UIFactory.AddChild(card, "Assign_" + keys[s]);
                    UIFactory.Place(b, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16 - (2 - s) * 62, 0), new Vector2(56, 46));
                    UIFactory.Button(b, keys[s], ArtPalette.Cover, ArtPalette.UiText,
                        () => { PlayerProfile.Current.SetAbility(slotIdx, id); RefreshLoadout(); }, 22);
                }
            }

            var close = UIFactory.AddChild(t, "CloseLoadout");
            UIFactory.Place(close, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-90, 50), new Vector2(240, 60));
            UIFactory.Button(close, "RETOUR", ArtPalette.NeonCyan, ArtPalette.UiInk, () => _loadoutPanel.SetActive(false), 26);

            _loadoutPanel.SetActive(false);
        }

        void RefreshLoadout()
        {
            var p = PlayerProfile.Current;
            foreach (var (id, img) in _weaponButtons)
                if (img != null) img.color = id == p.weaponId ? ArtPalette.NeonCyan : ArtPalette.Cover;

            for (int i = 0; i < 3; i++)
            {
                var a = AbilityCatalog.ById(p.GetAbility(i));
                if (a == null) continue;
                if (_equipNames[i] != null) _equipNames[i].text = a.nameFr;
                if (_equipSwatches[i] != null) _equipSwatches[i].color = a.color;
            }
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
