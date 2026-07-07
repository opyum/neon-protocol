using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FirstGame.Core;
using FirstGame.Combat;
using FirstGame.Enemies;
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
        static readonly GameAction[] SlotActions = { GameAction.AbilityE, GameAction.AbilityF, GameAction.AbilityC };

        readonly int[] _charges = new int[3];
        readonly float[] _rechargeAt = new float[3]; // time when the next charge is granted
        readonly float[] _lockUntil = new float[3];  // brief re-use lock after a cast
        float _powerMul = 1f;
        float _cdMul = 1f;

        // ---- Agent passive hooks (set by AgentPassiveSystem before first cast) ----
        /// <summary>Nocturne (Emprise): multiplies the lifetime of wall/smoke/zone volumes.</summary>
        public float zoneDurationMul = 1f;
        /// <summary>Bouclier de Lumière: HP returned if the shield still holds after 5s
        /// (25 by default, 40 for Rempart — Garde).</summary>
        public float shieldReturnHp = 25f;

        public bool ControlEnabled = true;

        /// <summary>(slot, ability) fired whenever an ability is successfully cast.</summary>
        public event Action<int, AbilityData> OnAbilityUsed;
        /// <summary>(slot, ability, target) fired when a cast damages something.</summary>
        public event Action<int, AbilityData, IDamageable> OnAbilityHit;

        void Awake() => ReloadLoadout();

        /// <summary>Re-resolves the equipped abilities from the profile (agent). Call after the player
        /// changes their build mid-scene (loadout screen).</summary>
        public void ReloadLoadout()
        {
            _powerMul = PlayerProfile.Current.AbilityPowerMultiplier;
            _cdMul = PlayerProfile.Current.CooldownMultiplier;

            var loadout = AbilityCatalog.ResolveLoadout(PlayerProfile.Current);
            for (int i = 0; i < 3; i++)
            {
                Equipped[i] = i < loadout.Length ? loadout[i] : null;
                _charges[i] = Equipped[i]?.charges ?? 0;
                _rechargeAt[i] = 0f;
                Keys[i] = Keybinds.Get(SlotActions[i]); // keep display in sync with remaps
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

                if (ControlEnabled && Input.GetKeyDown(Keybinds.Get(SlotActions[i])) &&
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

        /// <summary>Grant one extra charge to the equipped ability with this id (capped at its max).
        /// Used by Brasier's passive (Élan Ardent) to refund a Décharge Foudre charge on kill.</summary>
        public void AddCharge(string abilityId)
        {
            for (int i = 0; i < 3; i++)
                if (Equipped[i] != null && Equipped[i].id == abilityId) { AddCharge(i); return; }
        }

        public void AddCharge(int slot)
        {
            var a = Equipped[slot];
            if (a == null || _charges[slot] >= a.charges) return;
            _charges[slot]++;
            if (_charges[slot] >= a.charges) _rechargeAt[slot] = 0f; // full: stop the regen timer
        }

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
                            Vfx.Explosion(hit.point, a.color);
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
                    if (playerHealth != null && shieldReturnHp > 0f) StartCoroutine(ShieldReturn());
                    Burst(transform.position, a.color);
                    break;

                case AbilityEffect.Knockback:
                    DoKnockback(slot, a);
                    break;

                case AbilityEffect.Wall:
                    SpawnWall(a);
                    break;

                case AbilityEffect.Smoke:
                    SpawnSmoke(a);
                    break;

                case AbilityEffect.Zone:
                    SpawnZone(slot, a);
                    break;

                default:
                    SpawnPlaceholderVolume(a);
                    break;
            }
        }

        Vector3 AimedGroundPoint(float maxDist, Vector3 fallback)
        {
            var cam = aimCamera != null ? aimCamera : Camera.main;
            if (cam != null && Physics.Raycast(new Ray(cam.transform.position, cam.transform.forward),
                    out var hit, maxDist, hitMask, QueryTriggerInteraction.Ignore))
                return hit.point;
            return fallback;
        }

        // HP returned by Bouclier de Lumière if the shield still holds after 5s.
        IEnumerator ShieldReturn()
        {
            yield return new WaitForSeconds(5f);
            if (playerHealth != null && playerHealth.Shield > 0f) playerHealth.Heal(shieldReturnHp);
        }

        // Ice wall: solid, opaque slab that blocks bullets AND movement (keep the collider).
        void SpawnWall(AbilityData a)
        {
            var cam = aimCamera != null ? aimCamera : Camera.main;
            Vector3 fwd = cam != null ? cam.transform.forward : transform.forward;
            fwd.y = 0; fwd.Normalize();
            Vector3 basePos = AimedGroundPoint(20f, transform.position + fwd * 4f);
            AbilityFx.Wall(basePos, fwd, a.color, 8f * zoneDurationMul, a.id);
        }

        // Shadow smoke: opaque sphere that only blocks vision (no collider).
        void SpawnSmoke(AbilityData a)
        {
            Vector3 center = AimedGroundPoint(20f, transform.position + transform.forward * 8f) + Vector3.up * 1f;
            AbilityFx.Smoke(center, a.color, 12f * zoneDurationMul, a.id);
        }

        // Toxic zone: trigger DoT volume that damages IDamageables inside (not the caster).
        void SpawnZone(int slot, AbilityData a)
        {
            Vector3 center = AimedGroundPoint(20f, transform.position + transform.forward * 4f);
            AbilityFx.Zone(center, a.color, 3f, a.damage * _powerMul, 0.5f, 6f * zoneDurationMul,
                           playerHealth, d => OnAbilityHit?.Invoke(slot, a, d), a.id);
        }

        // Wind blast: push enemies within a forward cone.
        void DoKnockback(int slot, AbilityData a)
        {
            Vector3 origin = transform.position;
            Vector3 fwd = transform.forward; fwd.y = 0; fwd.Normalize();
            var seenD = new HashSet<TrainingDummy>();
            var seenB = new HashSet<EnemyBot>();
            foreach (var col in Physics.OverlapSphere(origin, 8f, hitMask, QueryTriggerInteraction.Collide))
            {
                Vector3 to = col.transform.position - origin; to.y = 0;
                if (to.sqrMagnitude < 0.01f || Vector3.Angle(fwd, to.normalized) > 30f) continue;

                var dm = col.GetComponentInParent<TrainingDummy>();
                if (dm != null && dm.IsAlive && seenD.Add(dm))
                {
                    dm.Knockback(to.normalized, 5f, 0.25f);
                    OnAbilityHit?.Invoke(slot, a, dm);
                    continue;
                }
                var bot = col.GetComponentInParent<EnemyBot>();
                if (bot != null && bot.IsAlive && seenB.Add(bot))
                {
                    bot.Knockback(to.normalized, 5f, 0.25f);
                    OnAbilityHit?.Invoke(slot, a, bot);
                }
            }
            Burst(origin + fwd * 2f, a.color);
        }

        void Burst(Vector3 pos, Color color) => Vfx.Burst(pos, color);

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
