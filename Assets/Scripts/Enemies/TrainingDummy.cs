using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FirstGame.Combat;
using FirstGame.Core;

namespace FirstGame.Enemies
{
    /// <summary>
    /// Procedural training dummy (capsule body + sphere head). Flashes on hit,
    /// deactivates on death, can be reset. Optionally patrols laterally (moving target).
    /// </summary>
    public class TrainingDummy : MonoBehaviour, IDamageable
    {
        public float maxHealth = 60f;
        public bool locked = false; // if true, absorbs hits but never dies (early tutorial steps)
        public bool autoRespawn = false;
        public float respawnDelay = 2f;

        float _health;
        public bool IsAlive => _health > 0f && gameObject.activeSelf;

        readonly List<Renderer> _renderers = new();
        Color _baseColor = ArtPalette.Enemy;
        Color _hitColor = ArtPalette.Signal;
        Vector3 _spawn;
        bool _patrol;
        float _patrolRange;
        float _patrolSpeed;
        Coroutine _flash;

        public event Action<TrainingDummy> OnDied;
        public event Action<TrainingDummy, float, bool> OnDamaged; // (self, amount, headshot)

        public static TrainingDummy Spawn(Transform parent, Vector3 pos, float health = 60f,
                                          bool locked = false, bool patrol = false,
                                          float patrolRange = 4f, float patrolSpeed = 3f,
                                          bool autoRespawn = false, float respawnDelay = 2f, string name = "Dummy")
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            var dummy = root.AddComponent<TrainingDummy>();
            dummy.maxHealth = health;
            dummy.locked = locked;
            dummy.autoRespawn = autoRespawn;
            dummy.respawnDelay = respawnDelay;
            dummy._patrol = patrol;
            dummy._patrolRange = patrolRange;
            dummy._patrolSpeed = patrolSpeed;
            dummy.BuildVisual();
            return dummy;
        }

        void BuildVisual()
        {
            // Body
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(transform, false);
            body.transform.localPosition = new Vector3(0, 1.0f, 0);
            body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            body.GetComponent<Renderer>().sharedMaterial = ArtPalette.MakeEmissive(ArtPalette.Enemy, 0.4f);
            _renderers.Add(body.GetComponent<Renderer>());

            // Head (headshot zone, near-white)
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(transform, false);
            head.transform.localPosition = new Vector3(0, 1.95f, 0);
            head.transform.localScale = Vector3.one * 0.45f;
            head.GetComponent<Renderer>().sharedMaterial = ArtPalette.MakeMaterial(ArtPalette.Signal, 0f, 0.3f);
            var hh = head.AddComponent<HeadHitbox>();
            hh.owner = this;
            _renderers.Add(head.GetComponent<Renderer>());

            _baseColor = ArtPalette.Enemy;
            _hitColor = ArtPalette.Signal;
            _spawn = transform.position;
            _health = maxHealth;
        }

        void Update()
        {
            if (_patrol && IsAlive)
            {
                float x = Mathf.Sin(Time.time * _patrolSpeed / Mathf.Max(0.1f, _patrolRange)) * _patrolRange;
                var p = _spawn; p.x += x;
                transform.position = p;
            }
        }

        public float TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (!IsAlive) return 0f;
            bool headshot = hitPoint.y - transform.position.y > 1.6f;
            if (!locked) _health -= amount;

            OnDamaged?.Invoke(this, amount, headshot);
            if (_flash != null) StopCoroutine(_flash);
            _flash = StartCoroutine(Flash());

            if (_health <= 0f) Die();
            return amount;
        }

        IEnumerator Flash()
        {
            SetColor(_hitColor);
            yield return new WaitForSeconds(0.06f);
            SetColor(_baseColor);
            _flash = null;
        }

        void SetColor(Color c)
        {
            // Body only (index 0); leave the head white.
            if (_renderers.Count > 0 && _renderers[0] != null) _renderers[0].material.color = c;
        }

        void Die()
        {
            OnDied?.Invoke(this);
            if (autoRespawn)
            {
                // Stay active (so the coroutine can run) but hide + disable colliders, then respawn.
                SetVisible(false);
                StartCoroutine(RespawnAfterDelay());
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(respawnDelay);
            ResetDummy();
        }

        void SetVisible(bool visible)
        {
            foreach (var r in _renderers) if (r) r.enabled = visible;
            foreach (var c in GetComponentsInChildren<Collider>(true)) if (c) c.enabled = visible;
        }

        public void ResetDummy()
        {
            _health = maxHealth;
            transform.position = _spawn;
            gameObject.SetActive(true);
            SetVisible(true);
            SetColor(_baseColor);
        }

        /// <summary>Pushes the dummy (moves its patrol centre too, so the shove sticks).</summary>
        public void Knockback(Vector3 dir, float distance, float duration)
        {
            StartCoroutine(KnockCo(dir.normalized * distance, duration));
        }

        IEnumerator KnockCo(Vector3 delta, float dur)
        {
            Vector3 start = _spawn, end = _spawn + delta;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                _spawn = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t / dur));
                if (!_patrol) transform.position = _spawn;
                yield return null;
            }
            _spawn = end;
        }
    }
}
