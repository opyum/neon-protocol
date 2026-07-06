using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FirstGame.Core;
using FirstGame.Combat;
using FirstGame.Equipment;

namespace FirstGame.UI
{
    /// <summary>Pre-mission buy phase: spend a credit budget on a weapon and armour.
    /// Returns the chosen (weaponId, armorId) and remaining credits. Freezes the game while open.</summary>
    public class BuyMenuUI : MonoBehaviour
    {
        class Card { public string id; public Image bg; public Text price; public int cost; }

        int _credits;
        string _weaponId = "wpn_pistolet_eclat"; // pistol is free, always the default
        string _armorId;                          // none by default
        Action<string, string, int> _onDone;
        float _timeLeft = 25f;
        bool _closed;

        GameObject _root;
        Text _creditsLabel, _timerLabel;
        readonly List<Card> _weaponCards = new();
        readonly List<Card> _armorCards = new();

        static readonly Color Sel = new Color(0.10f, 0.34f, 0.40f, 1f);
        static Color Normal => new Color(ArtPalette.UiInk.r, ArtPalette.UiInk.g, ArtPalette.UiInk.b, 0.92f);
        static Color Grey => new Color(ArtPalette.UiInk.r, ArtPalette.UiInk.g, ArtPalette.UiInk.b, 0.4f);

        public void Show(int budget, string subtitle, Action<string, string, int> onDone)
        {
            _credits = budget;
            _onDone = onDone;
            UIFactory.EnsureEventSystem();
            Build(subtitle);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;
            Refresh();
        }

        void Update()
        {
            if (_closed) return;
            _timeLeft -= Time.unscaledDeltaTime;
            if (_timerLabel != null) _timerLabel.text = "0:" + Mathf.Max(0, Mathf.CeilToInt(_timeLeft)).ToString("00");
            if (_timeLeft <= 0f) Validate();
        }

        void Build(string subtitle)
        {
            var canvas = UIFactory.CreateCanvas("BuyMenu", 20);
            _root = canvas.gameObject;
            var bg = UIFactory.Panel(_root.transform, new Color(ArtPalette.UiInk.r, ArtPalette.UiInk.g, ArtPalette.UiInk.b, 0.94f));
            UIFactory.Stretch(bg.rectTransform);
            var t = _root.transform;

            var title = UIFactory.Label(t, "PHASE D'ACHAT", 40, ArtPalette.NeonCyan, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(90, -60), new Vector2(700, 50));
            var sub = UIFactory.Label(t, subtitle, 20, ArtPalette.UiDim, TextAnchor.UpperLeft);
            UIFactory.Place(sub.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(92, -108), new Vector2(900, 26));

            _creditsLabel = UIFactory.Label(t, "", 30, ArtPalette.Objective, TextAnchor.UpperRight, FontStyle.Bold);
            UIFactory.Place(_creditsLabel.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-260, -60), new Vector2(400, 40));
            _timerLabel = UIFactory.Label(t, "", 26, ArtPalette.UiText, TextAnchor.UpperRight, FontStyle.Bold);
            UIFactory.Place(_timerLabel.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-90, -60), new Vector2(150, 40));

            // Weapons (left)
            var wl = UIFactory.Label(t, "ARMES", 24, ArtPalette.UiText, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(wl.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(92, -164), new Vector2(400, 30));
            for (int i = 0; i < Economy.Weapons.Length; i++)
            {
                var (id, price) = Economy.Weapons[i];
                var w = WeaponCatalog.ById(id);
                string wid = id; int cost = price;
                var (cardBg, pr) = BuyCard(t, new Vector2(92, -204 - i * 92), new Vector2(640, 82),
                    w.nameFr, $"{w.category}  •  {w.damage:0} dgt  •  {w.fireRate:0.#} t/s  •  chargeur {w.magazineSize}",
                    price, () => SelectWeapon(wid, cost));
                _weaponCards.Add(new Card { id = id, bg = cardBg, price = pr, cost = cost });
            }

            // Armor (right)
            var al = UIFactory.Label(t, "ARMURE", 24, ArtPalette.UiText, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(al.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(792, -164), new Vector2(400, 30));
            for (int i = 0; i < Economy.Armors.Length; i++)
            {
                var (id, price) = Economy.Armors[i];
                var e = EquipmentCatalog.ById(id);
                string aid = id; int cost = price;
                var (cardBg, pr) = BuyCard(t, new Vector2(792, -204 - i * 92), new Vector2(640, 82),
                    e.nameFr, e.effectFr, price, () => SelectArmor(aid, cost));
                _armorCards.Add(new Card { id = id, bg = cardBg, price = pr, cost = cost });
            }
            var noArmor = UIFactory.AddChild(t, "NoArmor");
            UIFactory.Place(noArmor, new Vector2(0, 1), new Vector2(0, 1), new Vector2(792, -204 - Economy.Armors.Length * 92), new Vector2(300, 54));
            UIFactory.Button(noArmor, "SANS ARMURE", ArtPalette.Cover, ArtPalette.UiText, () => { RefundArmor(); _armorId = null; Refresh(); }, 20);

            // Validate
            var validate = UIFactory.AddChild(t, "Validate");
            UIFactory.Place(validate, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(360, 66));
            UIFactory.Button(validate, "VALIDER — LANCER", ArtPalette.NeonCyan, ArtPalette.UiInk, Validate, 26);
        }

        (Image, Text) BuyCard(Transform parent, Vector2 pos, Vector2 size, string title, string subtitle, int price, Action onClick)
        {
            var card = UIFactory.AddChild(parent, "Card_" + title);
            UIFactory.Place(card, new Vector2(0, 1), new Vector2(0, 1), pos, size);
            var bg = card.gameObject.AddComponent<Image>();
            bg.color = Normal;
            var btn = card.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() => onClick());

            var tl = UIFactory.Label(card, title, 22, ArtPalette.UiText, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(tl.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -10), new Vector2(size.x - 130, 28));
            var sl = UIFactory.Label(card, subtitle, 13, ArtPalette.UiDim, TextAnchor.UpperLeft);
            UIFactory.Place(sl.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -42), new Vector2(size.x - 130, 30));
            var pr = UIFactory.Label(card, "", 20, ArtPalette.Objective, TextAnchor.MiddleRight, FontStyle.Bold);
            UIFactory.Place(pr.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(120, 40));
            return (bg, pr);
        }

        void SelectWeapon(string id, int cost)
        {
            if (_closed || id == _weaponId) return;
            int refund = Economy.WeaponPrice(_weaponId);
            if (_credits + refund < cost) return; // can't afford
            _credits += refund - cost;
            _weaponId = id;
            Refresh();
        }

        void SelectArmor(string id, int cost)
        {
            if (_closed || id == _armorId) return;
            int refund = Economy.ArmorPrice(_armorId);
            if (_credits + refund < cost) return;
            _credits += refund - cost;
            _armorId = id;
            Refresh();
        }

        void RefundArmor()
        {
            if (_armorId != null) _credits += Economy.ArmorPrice(_armorId);
        }

        void Refresh()
        {
            if (_creditsLabel != null) _creditsLabel.text = $"CRÉDITS : {_credits}";
            foreach (var c in _weaponCards)
            {
                bool sel = c.id == _weaponId;
                bool afford = _credits + Economy.WeaponPrice(_weaponId) - c.cost >= 0;
                c.bg.color = sel ? Sel : (afford ? Normal : Grey);
                c.price.text = sel ? "ÉQUIPÉE" : (c.cost == 0 ? "GRATUIT" : $"{c.cost} CR");
                c.price.color = sel ? ArtPalette.NeonCyan : (afford ? ArtPalette.Objective : ArtPalette.Enemy);
            }
            foreach (var c in _armorCards)
            {
                bool sel = c.id == _armorId;
                bool afford = _credits + Economy.ArmorPrice(_armorId) - c.cost >= 0;
                c.bg.color = sel ? Sel : (afford ? Normal : Grey);
                c.price.text = sel ? "ÉQUIPÉE" : $"{c.cost} CR";
                c.price.color = sel ? ArtPalette.NeonCyan : (afford ? ArtPalette.Objective : ArtPalette.Enemy);
            }
        }

        void Validate()
        {
            if (_closed) return;
            _closed = true;
            Time.timeScale = 1f;
            if (_root != null) Destroy(_root);
            _onDone?.Invoke(_weaponId, _armorId, _credits);
            Destroy(gameObject);
        }
    }
}
