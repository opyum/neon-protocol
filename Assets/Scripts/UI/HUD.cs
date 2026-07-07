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
        Text _killPopup;
        Image _damageVignette;
        float _flashAlpha;
        float _lastHp = -1f;
        Coroutine _hitmarkerCo, _killCo;
        readonly Image[] _abilityFills = new Image[3];
        readonly Text[] _abilityCharges = new Text[3];

        static readonly Color HpGreen = new Color(0.24f, 0.86f, 0.52f);
        static readonly Color HpYellow = new Color(0.96f, 0.78f, 0.22f);
        static readonly Color HpRed = new Color(0.92f, 0.26f, 0.30f);

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
                weapon.OnKill += OnKill;
                weapon.OnWeaponChanged += OnWeaponChanged;
                OnAmmo(weapon.Ammo, weapon.weapon.magazineSize);
                OnWeaponChanged();
            }
        }

        void OnDestroy()
        {
            if (playerHealth != null) { playerHealth.OnHealthChanged -= OnHealth; playerHealth.OnShieldChanged -= OnShield; }
            if (weapon != null) { weapon.OnAmmoChanged -= OnAmmo; weapon.OnHit -= OnWeaponHit; weapon.OnKill -= OnKill; weapon.OnWeaponChanged -= OnWeaponChanged; }
        }

        void OnWeaponChanged()
        {
            if (_weaponName != null && weapon != null) _weaponName.text = weapon.weapon.nameFr.ToUpper();
        }

        void Update()
        {
            // Damage flash fade + low-health red pulse
            if (_damageVignette != null)
            {
                float pulse = 0f;
                if (playerHealth != null && playerHealth.IsAlive && playerHealth.MaxHealth > 0f
                    && playerHealth.Health / playerHealth.MaxHealth < 0.3f)
                    pulse = 0.16f + 0.12f * Mathf.Sin(Time.time * 5f);
                _flashAlpha = Mathf.Max(_flashAlpha - Time.deltaTime * 2.8f, pulse);
                var c = _damageVignette.color; c.a = _flashAlpha; _damageVignette.color = c;
            }

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

            // Damage vignette (red flash on hit; pulses when critical). Behind the rest of the HUD.
            var dv = UIFactory.AddChild(root, "DamageVignette");
            UIFactory.Stretch(dv);
            _damageVignette = dv.gameObject.AddComponent<Image>();
            _damageVignette.sprite = Tex.Vignette;
            _damageVignette.type = Image.Type.Simple;
            _damageVignette.color = new Color(0.92f, 0.08f, 0.08f, 0f);
            _damageVignette.raycastTarget = false;

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

            // Kill confirmation popup (hidden until a kill)
            _killPopup = UIFactory.Label(root, "", 46, ArtPalette.Enemy, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Place(_killPopup.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 130), new Vector2(760, 70));
            var kc = _killPopup.color; kc.a = 0f; _killPopup.color = kc;

            // Health / shield block (bottom-left, large & readable)
            var block = UIFactory.AddChild(root, "HealthBlock");
            UIFactory.Place(block, Vector2.zero, Vector2.zero, new Vector2(40, 40), new Vector2(480, 122));
            UIFactory.Panel(block, new Color(ArtPalette.UiInk.r, ArtPalette.UiInk.g, ArtPalette.UiInk.b, 0.7f));
            var accent = UIFactory.AddChild(block, "Accent");
            UIFactory.Place(accent, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(6, 122));
            accent.gameObject.AddComponent<Image>().color = HpGreen;

            // Big HP number
            _healthText = UIFactory.Label(block, "100", 62, HpGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(_healthText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -6), new Vector2(180, 78));
            var pv = UIFactory.Label(block, "PV", 20, ArtPalette.UiDim, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(pv.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(22, -84), new Vector2(80, 26));

            // Wide health bar (colour-coded)
            var hpBg = UIFactory.AddChild(block, "HpBg");
            UIFactory.Place(hpBg, new Vector2(0, 1), new Vector2(0, 1), new Vector2(190, -20), new Vector2(270, 34));
            hpBg.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.75f);
            _healthFill = MakeBar(hpBg, HpGreen);

            // Shield bar (thin, cyan)
            var shBg = UIFactory.AddChild(block, "ShieldBg");
            UIFactory.Place(shBg, new Vector2(0, 1), new Vector2(0, 1), new Vector2(190, -64), new Vector2(270, 14));
            shBg.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.75f);
            _shieldFill = MakeBar(shBg, ArtPalette.NeonCyan);
            var shLabel = UIFactory.Label(block, "BOUCLIER", 13, ArtPalette.UiDim, TextAnchor.UpperLeft);
            UIFactory.Place(shLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(190, -82), new Vector2(160, 18));

            // Ammo (bottom-right)
            var ammoBlock = UIFactory.AddChild(root, "AmmoBlock");
            UIFactory.Place(ammoBlock, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 48), new Vector2(260, 90));
            _weaponName = UIFactory.Label(ammoBlock, "ÉCLAT", 20, ArtPalette.UiDim, TextAnchor.LowerRight);
            UIFactory.Place(_weaponName.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-260, 60), new Vector2(260, 26));
            _ammoText = UIFactory.Label(ammoBlock, "13 / 13", 40, ArtPalette.UiText, TextAnchor.LowerRight, FontStyle.Bold);
            UIFactory.Place(_ammoText.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-260, 6), new Vector2(260, 52));

            // Ability slots (bottom-centre): labels follow the current keybinds.
            string[] keys =
            {
                Keybinds.KeyName(Keybinds.Get(GameAction.AbilityE)),
                Keybinds.KeyName(Keybinds.Get(GameAction.AbilityF)),
                Keybinds.KeyName(Keybinds.Get(GameAction.AbilityC)),
            };
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
            float ratio = max > 0 ? h / max : 0f;
            var color = ratio > 0.5f ? HpGreen : (ratio > 0.25f ? HpYellow : HpRed);
            if (_healthFill != null) { _healthFill.fillAmount = ratio; _healthFill.color = color; }
            if (_healthText != null) { _healthText.text = Mathf.CeilToInt(h).ToString(); _healthText.color = color; }

            if (_lastHp >= 0f && h < _lastHp) _flashAlpha = 0.8f; // took damage -> red screen flash
            _lastHp = h;
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
            if (_hitmarkerCo != null) StopCoroutine(_hitmarkerCo);
            _hitmarkerCo = StartCoroutine(HitmarkerRoutine(headshot));
        }

        void OnKill(bool headshot)
        {
            if (_killPopup == null) return;
            _killPopup.text = headshot ? "ÉLIMINÉ — TÊTE !" : "ÉLIMINÉ !";
            _killPopup.color = headshot ? ArtPalette.Objective : ArtPalette.Enemy;
            if (_killCo != null) StopCoroutine(_killCo);
            _killCo = StartCoroutine(KillPopupRoutine());
        }

        IEnumerator KillPopupRoutine()
        {
            float t = 0f;
            while (t < 1.1f)
            {
                t += Time.deltaTime;
                float a = t < 0.15f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.15f) / 0.95f);
                var c = _killPopup.color; c.a = a; _killPopup.color = c;
                yield return null;
            }
            var c2 = _killPopup.color; c2.a = 0f; _killPopup.color = c2;
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
