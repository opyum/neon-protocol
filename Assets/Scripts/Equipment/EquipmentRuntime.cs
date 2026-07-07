using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FirstGame.Core;
using FirstGame.Combat;
using FirstGame.Player;

namespace FirstGame.Equipment
{
    /// <summary>Applies the passive armour effect at spawn.</summary>
    public static class EquipmentEffects
    {
        public static void ApplyArmor(EquipmentData e, PlayerRig.Refs r)
        {
            if (e == null || r.health == null) return;
            switch (e.id)
            {
                case "equip_armure_legere":
                    r.health.AddShield(25f);
                    if (r.controller) r.controller.equipSpeedMul = 1.08f;
                    break;
                case "equip_armure_lourde":
                    r.health.AddShield(75f);
                    if (r.controller) r.controller.equipSpeedMul = 0.92f;
                    break;
                case "equip_regulateur":
                    r.health.AddShield(40f);
                    r.player.AddComponent<ShieldRegen>().Init(r.health, 40f, 12f, 4f);
                    break;
            }
        }
    }

    /// <summary>Regenerates shield out of combat up to a cap.</summary>
    public class ShieldRegen : MonoBehaviour
    {
        PlayerHealth _h;
        float _cap, _rate, _delay, _lastDamage = -999f, _added;

        public void Init(PlayerHealth h, float cap, float rate, float delay)
        {
            _h = h; _cap = cap; _rate = rate; _delay = delay;
            float last = h.Health;
            _h.OnHealthChanged += (cur, max) => { if (cur < last) _lastDamage = Time.time; last = cur; };
        }

        void Update()
        {
            if (_h == null) return;
            if (Time.time - _lastDamage >= _delay && _added < _cap)
            {
                float d = Mathf.Min(_rate * Time.deltaTime, _cap - _added);
                _h.AddShield(d);
                _added += d;
            }
        }
    }

    /// <summary>Consumable utility on key G. Manages charges + recharge and fires the effect.</summary>
    public class UtilityController : MonoBehaviour
    {
        EquipmentData _item;
        PlayerRig.Refs _r;
        int _charges;
        float _rechargeAt;
        AudioSource _audio;

        public bool ControlEnabled = true;
        public event Action<int, int> OnChargesChanged; // (charges, max)
        public int Charges => _charges;
        public EquipmentData Item => _item;

        (int max, float cd) Cfg() => _item == null ? (0, 1f) : _item.id switch
        {
            "equip_fumigene" => (2, 25f),
            "equip_grenade_aveuglante" => (1, 30f),
            "equip_drone" => (1, 35f),
            _ => (1, 30f)
        };

        public void Init(EquipmentData e, PlayerRig.Refs r)
        {
            _item = e; _r = r;
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.spatialBlend = 0f; _audio.playOnAwake = false;
            if (e != null) _charges = Cfg().max;
        }

        void Start() => OnChargesChanged?.Invoke(_charges, Cfg().max);

        void Update()
        {
            if (_item == null) return;
            var (max, cd) = Cfg();
            if (_charges < max && Time.time >= _rechargeAt)
            {
                _charges++;
                if (_charges < max) _rechargeAt = Time.time + cd;
                OnChargesChanged?.Invoke(_charges, max);
            }
            if (ControlEnabled && Input.GetKeyDown(Keybinds.Get(GameAction.Utility)) && _charges > 0)
            {
                _charges--;
                if (_rechargeAt < Time.time) _rechargeAt = Time.time + cd;
                Fire();
                OnChargesChanged?.Invoke(_charges, max);
            }
        }

        Vector3 Aim(float maxDist)
        {
            var c = _r.camera;
            var ray = new Ray(c.transform.position, c.transform.forward);
            return Physics.Raycast(ray, out var h, maxDist, _r.abilities.hitMask, QueryTriggerInteraction.Ignore)
                ? h.point : ray.GetPoint(maxDist);
        }

        void Fire()
        {
            if (_audio && ProceduralAudio.Ability) _audio.PlayOneShot(ProceduralAudio.Ability, 0.6f);
            switch (_item.id)
            {
                case "equip_fumigene": SmokeVolume.Spawn(Aim(15f), 4f, 10f, _item.color); break;
                case "equip_grenade_aveuglante": Flashbang.Throw(_r.camera, _r.health, 8f, 2.5f); break;
                case "equip_drone": ReconDrone.Deploy(Aim(20f), 8f, 20f, _r.abilities.hitMask, _item.color); break;
            }
        }
    }

    /// <summary>Opaque vision-blocking smoke sphere (no collider).</summary>
    public static class SmokeVolume
    {
        public static void Spawn(Vector3 center, float radius, float life, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var col = go.GetComponent<Collider>(); if (col) UnityEngine.Object.Destroy(col);
            go.name = "SmokeVolume";
            go.transform.position = center + Vector3.up * 1f;
            go.transform.localScale = Vector3.one * radius * 2f;
            var c = color; c.a = 1f;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = ArtPalette.MakeUnlit(c);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            UnityEngine.Object.Destroy(go, life);
        }
    }

    /// <summary>Thrown flashbang: after a fuse, blinds the player if in range and facing it.</summary>
    public class Flashbang : MonoBehaviour
    {
        Camera _cam;
        PlayerHealth _health;
        float _radius, _blind, _detonateAt;
        bool _done;

        public static void Throw(Camera cam, PlayerHealth health, float radius, float blind)
        {
            var go = Prim.Sphere(null, cam.transform.position + cam.transform.forward * 1.5f, 0.25f, ArtPalette.Signal, unlit: true, name: "Flashbang");
            var rb = go.AddComponent<Rigidbody>();
            rb.linearVelocity = cam.transform.forward * 16f + Vector3.up * 2f;
            var f = go.AddComponent<Flashbang>();
            f._cam = cam; f._health = health; f._radius = radius; f._blind = blind;
            f._detonateAt = Time.time + 1.2f;
        }

        void Update()
        {
            if (_done || Time.time < _detonateAt) return;
            _done = true;
            if (_health != null && _cam != null)
            {
                Vector3 toBlast = transform.position - _cam.transform.position;
                if (toBlast.magnitude <= _radius && Vector3.Dot(_cam.transform.forward, toBlast.normalized) > 0.1f)
                    ScreenFlash.Do(_blind);
            }
            Destroy(gameObject);
        }
    }

    /// <summary>Full-screen white flash that fades out.</summary>
    public static class ScreenFlash
    {
        public static void Do(float duration)
        {
            var go = new GameObject("[Flash]");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            var imgGo = new GameObject("F", typeof(RectTransform), typeof(Image));
            imgGo.transform.SetParent(go.transform, false);
            var rt = (RectTransform)imgGo.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var img = imgGo.GetComponent<Image>();
            img.color = Color.white; img.raycastTarget = false;
            go.AddComponent<FlashFade>().Init(img, duration);
        }
    }

    public class FlashFade : MonoBehaviour
    {
        Image _img; float _dur, _t;
        public void Init(Image img, float dur) { _img = img; _dur = dur; }
        void Update()
        {
            _t += Time.deltaTime;
            if (_img != null) { var c = _img.color; c.a = Mathf.Lerp(1f, 0f, _t / _dur); _img.color = c; }
            if (_t >= _dur) Destroy(gameObject);
        }
    }

    /// <summary>Stationary recon drone: highlights nearby enemies every second.</summary>
    public class ReconDrone : MonoBehaviour
    {
        float _life, _scanRadius, _age, _nextScan;

        public static void Deploy(Vector3 pos, float life, float scanRadius, LayerMask mask, Color color)
        {
            var core = Prim.NeonGlowSphere(null, pos + Vector3.up * 1.5f, 0.4f, color);
            var d = core.AddComponent<ReconDrone>();
            d._life = life; d._scanRadius = scanRadius;
        }

        void Update()
        {
            _age += Time.deltaTime;
            if (_age >= _life) { Destroy(gameObject); return; }
            if (Time.time < _nextScan) return;
            _nextScan = Time.time + 1f;

            var seen = new HashSet<IDamageable>();
            foreach (var col in Physics.OverlapSphere(transform.position, _scanRadius, ~0, QueryTriggerInteraction.Ignore))
            {
                var d = col.GetComponentInParent<IDamageable>();
                if (d == null || !d.IsAlive || d is PlayerHealth || !seen.Add(d)) continue;
                var mb = d as MonoBehaviour;
                if (mb != null) Marker(mb.transform.position);
            }
        }

        void Marker(Vector3 pos)
        {
            var m = Prim.Sphere(null, pos + Vector3.up * 2.5f, 0.3f, ArtPalette.Objective, unlit: true, name: "Mark");
            var col = m.GetComponent<Collider>(); if (col) Destroy(col);
            Destroy(m, 1f);
        }
    }
}
