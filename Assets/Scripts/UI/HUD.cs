using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using FirstGame.Core;
using FirstGame.Player;
using FirstGame.Combat;
using FirstGame.Abilities;

namespace FirstGame.UI
{
    /// <summary>In-game HUD, fully code-built. Assign the references before Start runs.</summary>
    public class HUD : MonoBehaviour
    {
        public PlayerHealth playerHealth;
        public WeaponController weapon;
        public AbilitySystem abilities;

        Image _healthFill, _shieldFill;
        Text _healthText, _ammoText, _weaponName;
        Image _hitmarker;
        readonly Image[] _abilityFills = new Image[3];
        readonly Text[] _abilityCharges = new Text[3];

        void Start()
        {
            Build();
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += OnHealth;
                playerHealth.OnShieldChanged += OnShield;
                OnHealth(playerHealth.Health, playerHealth.MaxHealth);
                OnShield(playerHealth.Shield);
            }
            if (weapon != null)
            {
                weapon.OnAmmoChanged += OnAmmo;
                weapon.OnHit += OnWeaponHit;
                weapon.OnWeaponChanged += OnWeaponChanged;
                OnAmmo(weapon.Ammo, weapon.weapon.magazineSize);
                OnWeaponChanged();
            }
        }

        void OnDestroy()
        {
            if (playerHealth != null) { playerHealth.OnHealthChanged -= OnHealth; playerHealth.OnShieldChanged -= OnShield; }
            if (weapon != null) { weapon.OnAmmoChanged -= OnAmmo; weapon.OnHit -= OnWeaponHit; weapon.OnWeaponChanged -= OnWeaponChanged; }
        }

        void OnWeaponChanged()
        {
            if (_weaponName != null && weapon != null) _weaponName.text = weapon.weapon.nameFr.ToUpper();
        }

        void Update()
        {
            if (abilities == null) return;
            for (int i = 0; i < 3; i++)
            {
                if (_abilityFills[i] != null) _abilityFills[i].fillAmount = abilities.CooldownFill(i);
                if (_abilityCharges[i] != null) _abilityCharges[i].text = abilities.Charges(i).ToString();
            }
        }

        void Build()
        {
            var canvas = GetComponent<Canvas>();
            var root = canvas != null ? canvas.transform : transform;

            // Crosshair (4 ticks + centre dot)
            var cross = UIFactory.AddChild(root, "Crosshair");
            UIFactory.Place(cross, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24, 24));
            Tick(cross, new Vector2(0, 8), new Vector2(2, 8));
            Tick(cross, new Vector2(0, -8), new Vector2(2, 8));
            Tick(cross, new Vector2(8, 0), new Vector2(8, 2));
            Tick(cross, new Vector2(-8, 0), new Vector2(8, 2));

            // Hitmarker (hidden until a hit)
            var hm = UIFactory.AddChild(root, "Hitmarker");
            UIFactory.Place(hm, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18, 18));
            _hitmarker = hm.gameObject.AddComponent<Image>();
            _hitmarker.color = new Color(1, 1, 1, 0);

            // Health / shield block (bottom-left)
            var block = UIFactory.AddChild(root, "HealthBlock");
            UIFactory.Place(block, Vector2.zero, Vector2.zero, new Vector2(40, 48), new Vector2(360, 74));
            UIFactory.Panel(block, new Color(ArtPalette.UiInk.r, ArtPalette.UiInk.g, ArtPalette.UiInk.b, 0.55f));

            var hpBg = UIFactory.AddChild(block, "HpBg");
            UIFactory.Place(hpBg, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -12), new Vector2(336, 22));
            hpBg.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);
            _healthFill = MakeBar(hpBg, ArtPalette.Player);

            var shBg = UIFactory.AddChild(block, "ShieldBg");
            UIFactory.Place(shBg, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -40), new Vector2(336, 12));
            shBg.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);
            _shieldFill = MakeBar(shBg, ArtPalette.NeonCyan);

            _healthText = UIFactory.Label(block, "100", 22, ArtPalette.UiText, TextAnchor.MiddleRight);
            UIFactory.Place(_healthText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -12), new Vector2(330, 22));

            // Ammo (bottom-right)
            var ammoBlock = UIFactory.AddChild(root, "AmmoBlock");
            UIFactory.Place(ammoBlock, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 48), new Vector2(260, 90));
            _weaponName = UIFactory.Label(ammoBlock, "ÉCLAT", 20, ArtPalette.UiDim, TextAnchor.LowerRight);
            UIFactory.Place(_weaponName.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-260, 60), new Vector2(260, 26));
            _ammoText = UIFactory.Label(ammoBlock, "13 / 13", 40, ArtPalette.UiText, TextAnchor.LowerRight, FontStyle.Bold);
            UIFactory.Place(_ammoText.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-260, 6), new Vector2(260, 52));

            // Ability slots (bottom-centre): E / F / C
            string[] keys = { "E", "F", "C" };
            for (int i = 0; i < 3; i++)
            {
                var slot = UIFactory.AddChild(root, "Ability_" + keys[i]);
                UIFactory.Place(slot, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2((i - 1) * 96, 60), new Vector2(84, 84));
                slot.gameObject.AddComponent<Image>().color = new Color(ArtPalette.UiInk.r, ArtPalette.UiInk.g, ArtPalette.UiInk.b, 0.7f);

                var ab = abilities != null ? abilities.Ability(i) : null;
                var iconColor = ab != null ? ab.color : ArtPalette.UiDim;
                var icon = UIFactory.AddChild(slot, "Icon");
                UIFactory.Stretch(icon, 10);
                var iconImg = icon.gameObject.AddComponent<Image>();
                iconImg.color = iconColor;

                // Cooldown overlay (radial fill)
                var cd = UIFactory.AddChild(slot, "Cooldown");
                UIFactory.Stretch(cd, 10);
                var cdImg = cd.gameObject.AddComponent<Image>();
                cdImg.color = new Color(0, 0, 0, 0.7f);
                cdImg.type = Image.Type.Filled;
                cdImg.fillMethod = Image.FillMethod.Radial360;
                cdImg.fillOrigin = (int)Image.Origin360.Top;
                cdImg.fillClockwise = false;
                cdImg.fillAmount = 0f;
                _abilityFills[i] = cdImg;

                var keyLabel = UIFactory.Label(slot, keys[i], 20, ArtPalette.UiText, TextAnchor.UpperLeft, FontStyle.Bold);
                UIFactory.Place(keyLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(6, -4), new Vector2(30, 26));

                _abilityCharges[i] = UIFactory.Label(slot, "1", 20, ArtPalette.Objective, TextAnchor.LowerRight, FontStyle.Bold);
                UIFactory.Place(_abilityCharges[i].rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-6, 4), new Vector2(30, 26));
            }
        }

        Image MakeBar(RectTransform bg, Color color)
        {
            var fillRt = UIFactory.AddChild(bg, "Fill");
            UIFactory.Stretch(fillRt);
            var img = fillRt.gameObject.AddComponent<Image>();
            img.color = color;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
            img.fillAmount = 1f;
            return img;
        }

        void Tick(Transform parent, Vector2 pos, Vector2 size)
        {
            var rt = UIFactory.AddChild(parent, "Tick");
            UIFactory.Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
            rt.gameObject.AddComponent<Image>().color = ArtPalette.Signal;
        }

        void OnHealth(float h, float max)
        {
            if (_healthFill != null) _healthFill.fillAmount = max > 0 ? h / max : 0;
            if (_healthText != null) _healthText.text = Mathf.CeilToInt(h).ToString();
        }

        void OnShield(float s)
        {
            if (_shieldFill != null) _shieldFill.fillAmount = Mathf.Clamp01(s / 50f);
        }

        void OnAmmo(int ammo, int mag)
        {
            if (_ammoText != null) _ammoText.text = $"{ammo} / {mag}";
        }

        void OnWeaponHit(IDamageable target, float dmg, bool headshot)
        {
            StopAllCoroutines();
            StartCoroutine(HitmarkerRoutine(headshot));
        }

        IEnumerator HitmarkerRoutine(bool headshot)
        {
            if (_hitmarker == null) yield break;
            _hitmarker.color = headshot ? ArtPalette.Objective : ArtPalette.Signal;
            float t = 0f;
            while (t < 0.18f)
            {
                t += Time.deltaTime;
                var c = _hitmarker.color; c.a = Mathf.Lerp(1f, 0f, t / 0.18f);
                _hitmarker.color = c;
                yield return null;
            }
            var c2 = _hitmarker.color; c2.a = 0; _hitmarker.color = c2;
        }
    }
}
