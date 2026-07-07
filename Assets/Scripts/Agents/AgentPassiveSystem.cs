using UnityEngine;
using FirstGame.Core;
using FirstGame.Combat;
using FirstGame.Player;
using FirstGame.Abilities;
using FirstGame.Progression;

namespace FirstGame.Agents
{
    /// <summary>
    /// Single entry point that activates the chosen agent's passive for the local player.
    /// Added by PlayerRig.Assemble and wired via Init(refs). Reads the agent from
    /// PlayerProfile.Current.agentId, so changing agent in the menu changes the passive next match.
    ///
    ///  - Brasier (Élan Ardent) : sur kill, +1 charge de Décharge Foudre et +12% célérité 3s.
    ///  - Nocturne (Emprise)    : +25% de durée sur mur/fumée/zone (via AbilitySystem.zoneDurationMul).
    ///  - Faille (Onde de Choc) : Pic de Terre / Rafale de Vent révèlent la cible 2s.
    ///  - Rempart (Garde)       : régén 8 PV/s hors combat, Bouclier de Lumière rend 40 PV au lieu de 25.
    /// </summary>
    public class AgentPassiveSystem : MonoBehaviour
    {
        WeaponController _weapon;
        AbilitySystem _abilities;
        PlayerHealth _health;
        FirstPersonController _controller;
        AgentData _agent;

        public void Init(PlayerRig.Refs r)
        {
            _weapon = r.weapon;
            _abilities = r.abilities;
            _health = r.health;
            _controller = r.controller;
            _agent = AgentCatalog.ById(PlayerProfile.Current.agentId);
            if (_agent == null) return;

            switch (_agent.id)
            {
                case "agent_brasier": // Élan Ardent
                    if (_weapon != null) _weapon.OnKill += OnBrasierKill;
                    break;

                case "agent_nocturne": // Emprise (+25% durée des zones/écrans)
                    if (_abilities != null) _abilities.zoneDurationMul = 1.25f;
                    break;

                case "agent_faille": // Onde de Choc (révélation)
                    if (_abilities != null) _abilities.OnAbilityHit += OnFailleHit;
                    break;

                case "agent_rempart": // Garde
                    if (_health != null) _health.EnsureRegenAtLeast(8f);
                    if (_abilities != null) _abilities.shieldReturnHp = 40f;
                    break;
            }
        }

        void OnDestroy()
        {
            if (_weapon != null) _weapon.OnKill -= OnBrasierKill;
            if (_abilities != null) _abilities.OnAbilityHit -= OnFailleHit;
        }

        // Brasier — after each kill: recharge one dash charge and grant a short celerity burst.
        void OnBrasierKill(bool headshot)
        {
            _abilities?.AddCharge("decharge_foudre");
            _controller?.AddSpeedBuff(1.12f, 3f);
        }

        // Faille — reveal the target hit by Pic de Terre or Rafale de Vent.
        void OnFailleHit(int slot, AbilityData a, IDamageable target)
        {
            if (a == null || target == null) return;
            if (a.id != "pic_de_terre" && a.id != "rafale_de_vent") return;
            var mb = target as MonoBehaviour;
            if (mb != null) RevealMarker.Show(mb.transform, 2f);
        }
    }

    /// <summary>Floating amber marker pinned above a revealed target for a few seconds
    /// (same visual language as ReconDrone.Marker). Refreshes instead of stacking.</summary>
    public class RevealMarker : MonoBehaviour
    {
        Transform _target;
        float _die;

        public static void Show(Transform target, float life)
        {
            if (target == null) return;
            var existing = target.GetComponentInChildren<RevealMarker>();
            if (existing != null) { existing._die = Time.time + life; return; }

            var go = Prim.Sphere(null, target.position + Vector3.up * 2.6f, 0.35f,
                                 ArtPalette.Objective, unlit: true, name: "RevealMark");
            var col = go.GetComponent<Collider>(); if (col) Destroy(col);
            go.transform.SetParent(target, worldPositionStays: true);
            var m = go.AddComponent<RevealMarker>();
            m._target = target;
            m._die = Time.time + life;
        }

        void Update()
        {
            if (_target == null || Time.time >= _die) { Destroy(gameObject); return; }
            transform.position = _target.position + Vector3.up * 2.6f;
        }
    }
}
