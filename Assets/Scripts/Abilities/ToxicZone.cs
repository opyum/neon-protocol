using System;
using System.Collections.Generic;
using UnityEngine;
using FirstGame.Combat;

namespace FirstGame.Abilities
{
    /// <summary>Persistent damage-over-time volume. Uses OverlapSphere per tick (robust with
    /// static targets), so it needs no trigger events. Ignores the caster.</summary>
    public class ToxicZone : MonoBehaviour
    {
        public float radius = 3f;
        public float dps = 10f;
        public float tick = 0.5f;
        public float life = 6f;
        public IDamageable self;
        public Action<IDamageable> onHit;

        float _acc;
        float _age;

        void Update()
        {
            _age += Time.deltaTime;
            if (_age >= life) { Destroy(gameObject); return; }

            _acc += Time.deltaTime;
            if (_acc < tick) return;
            _acc -= tick;

            float dmg = dps * tick;
            var seen = new HashSet<IDamageable>();
            var hits = Physics.OverlapSphere(transform.position + Vector3.up * 1.2f, radius, ~0, QueryTriggerInteraction.Collide);
            foreach (var col in hits)
            {
                var d = col.GetComponentInParent<IDamageable>();
                if (d == null || d == self || !d.IsAlive || !seen.Add(d)) continue;
                d.TakeDamage(dmg, transform.position + Vector3.up, Vector3.up);
                onHit?.Invoke(d);
            }
        }
    }
}
