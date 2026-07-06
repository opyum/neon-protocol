using System;
using UnityEngine;
using FirstGame.Core;
using FirstGame.Combat;
using FirstGame.Progression;

namespace FirstGame.Abilities
{
    /// <summary>
    /// Manages the 3 equipped abilities (keys E / F / C), their charges and cooldowns,
    /// and executes a best-effort effect for each. Wall/Smoke/Zone are approximated with
    /// placeholder volumes until their dedicated systems arrive.
    /// </summary>
    public class AbilitySystem : MonoBehaviour
    {
        public Camera aimCamera;
        public FirstGame.Player.PlayerHealth playerHealth;
        public CharacterController body;
        public LayerMask hitMask = Physics.DefaultRaycastLayers;

        public readonly AbilityData[] Equipped = new AbilityData[3];
        public readonly KeyCode[] Keys = { KeyCode.E, KeyCode.F, KeyCode.C };

        readonly int[] _charges = new int[3];
        readonly float[] _rechargeAt = new float[3]; // time when the next charge is granted
        readonly float[] _lockUntil = new float[3];  // brief re-use lock after a cast
        float _powerMul = 1f;
        float _cdMul = 1f;

        public bool ControlEnabled = true;

        /// <summary>(slot, ability) fired whenever an ability is successfully cast.</summary>
        public event Action<int, AbilityData> OnAbilityUsed;
        /// <summary>(slot, ability, target) fired when a cast damages something.</summary>
        public event Action<int, AbilityData, IDamageable> OnAbilityHit;

        void Awake()
        {
            _powerMul = PlayerProfile.Current.AbilityPowerMultiplier;
            _cdMul = PlayerProfile.Current.CooldownMultiplier;

            var loadout = AbilityCatalog.ResolveLoadout(PlayerProfile.Current);
            for (int i = 0; i < 3; i++)
            {
                Equipped[i] = i < loadout.Length ? loadout[i] : null;
                _charges[i] = Equipped[i]?.charges ?? 0;
                _rechargeAt[i] = 0f;
            }
        }

        void Update()
        {
            for (int i = 0; i < 3; i++)
            {
                var a = Equipped[i];
                if (a == null) continue;

                // regen charges over time up to max
                if (_charges[i] < a.charges && Time.time >= _rechargeAt[i])
                {
                    _charges[i]++;
                    if (_charges[i] < a.charges) _rechargeAt[i] = Time.time + a.cooldown * _cdMul;
                }

                if (ControlEnabled && Input.GetKeyDown(Keys[i]) &&
                    _charges[i] > 0 && Time.time >= _lockUntil[i])
                {
                    Cast(i);
                }
            }
        }

        // 0 = ready, 1 = just used (for a radial cooldown display)
        public float CooldownFill(int slot)
        {
            var a = Equipped[slot];
            if (a == null || _charges[slot] >= a.charges) return 0f;
            float cd = a.cooldown * _cdMul;
            if (cd <= 0f) return 0f;
            return Mathf.Clamp01((_rechargeAt[slot] - Time.time) / cd);
        }

        public int Charges(int slot) => _charges[slot];
        public AbilityData Ability(int slot) => Equipped[slot];

        void Cast(int slot)
        {
            var a = Equipped[slot];
            bool wasFull = _charges[slot] >= a.charges;
            _charges[slot]--;
            if (wasFull || _rechargeAt[slot] < Time.time)
                _rechargeAt[slot] = Time.time + a.cooldown * _cdMul;
            _lockUntil[slot] = Time.time + 0.35f;

            Execute(slot, a);
            OnAbilityUsed?.Invoke(slot, a);
        }

        void Execute(int slot, AbilityData a)
        {
            var cam = aimCamera != null ? aimCamera : Camera.main;
            float dmg = a.damage * _powerMul;

            switch (a.effect)
            {
                case AbilityEffect.DamageBurst:
                    if (cam != null)
                    {
                        var ray = new Ray(cam.transform.position, cam.transform.forward);
                        if (Physics.Raycast(ray, out var hit, 30f, hitMask, QueryTriggerInteraction.Ignore))
                        {
                            var d = hit.collider.GetComponentInParent<IDamageable>();
                            if (d != null && d.IsAlive && dmg > 0f)
                            {
                                d.TakeDamage(dmg, hit.point, hit.normal);
                                OnAbilityHit?.Invoke(slot, a, d);
                            }
                            Burst(hit.point, a.color);
                        }
                        else Burst(cam.transform.position + cam.transform.forward * 3f, a.color);
                    }
                    break;

                case AbilityEffect.Dash:
                    if (body != null) body.Move(transform.forward * 5f);
                    Burst(transform.position + transform.forward, a.color);
                    break;

                case AbilityEffect.Heal:
                    playerHealth?.Heal(Mathf.Max(25f, dmg));
                    Burst(transform.position, a.color);
                    break;

                case AbilityEffect.Shield:
                    playerHealth?.AddShield(60f);
                    Burst(transform.position, a.color);
                    break;

                case AbilityEffect.Knockback:
                    // Placeholder pulse; a real knockback needs enemy rigidbodies (phase 2).
                    Burst(transform.position + transform.forward * 2f, a.color);
                    break;

                default: // Wall / Smoke / Zone — drop a temporary marker volume in front of the player
                    SpawnPlaceholderVolume(a);
                    break;
            }
        }

        void Burst(Vector3 pos, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var col = go.GetComponent<Collider>(); if (col) Destroy(col);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.7f;
            go.GetComponent<Renderer>().sharedMaterial = ArtPalette.MakeUnlit(color);
            Destroy(go, 0.35f);
        }

        void SpawnPlaceholderVolume(AbilityData a)
        {
            var cam = aimCamera != null ? aimCamera : Camera.main;
            Vector3 origin = transform.position + transform.forward * 4f;
            if (cam != null && Physics.Raycast(new Ray(cam.transform.position, cam.transform.forward),
                    out var hit, 20f, hitMask, QueryTriggerInteraction.Ignore))
                origin = hit.point;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var col = go.GetComponent<Collider>(); if (col) Destroy(col);
            go.name = "AbilityVolume_" + a.id;
            go.transform.position = origin + Vector3.up * 1.5f;
            go.transform.localScale = new Vector3(4f, 3f, 0.4f);
            go.transform.rotation = Quaternion.LookRotation(transform.forward);
            var col2 = a.color; col2.a = 0.5f;
            go.GetComponent<Renderer>().sharedMaterial = ArtPalette.MakeUnlit(col2);
            Destroy(go, 4f);
        }
    }
}
