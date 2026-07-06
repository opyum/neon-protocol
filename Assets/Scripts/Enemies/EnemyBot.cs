using System;
using System.Collections;
using UnityEngine;
using FirstGame.Combat;
using FirstGame.Core;
using FirstGame.Player;

namespace FirstGame.Enemies
{
    public enum BotTier { Recrue, Soldat, Veteran, Elite }

    /// <summary>
    /// Combat bot (no NavMesh): perceives the player by line-of-sight, closes to a preferred
    /// range while strafing, and fires hitscan with an accuracy roll (damages PlayerHealth).
    /// Hittable (headshot x2 via head collider), dies (IDamageable), optionally respawns.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class EnemyBot : MonoBehaviour, IDamageable
    {
        struct Cfg { public float health, moveSpeed, fireInterval, damage, accuracy, aggroRange, preferredRange, reactionDelay; }

        static Cfg TierCfg(BotTier t) => t switch
        {
            BotTier.Recrue  => new Cfg { health = 80,  moveSpeed = 2.5f, fireInterval = 1.4f,  damage = 8,  accuracy = 0.45f, aggroRange = 22, preferredRange = 12, reactionDelay = 0.6f },
            BotTier.Soldat  => new Cfg { health = 110, moveSpeed = 3.5f, fireInterval = 1.0f,  damage = 12, accuracy = 0.60f, aggroRange = 28, preferredRange = 10, reactionDelay = 0.4f },
            BotTier.Veteran => new Cfg { health = 140, moveSpeed = 4.2f, fireInterval = 0.75f, damage = 16, accuracy = 0.72f, aggroRange = 34, preferredRange = 9,  reactionDelay = 0.25f },
            _               => new Cfg { health = 180, moveSpeed = 4.8f, fireInterval = 0.6f,  damage = 20, accuracy = 0.80f, aggroRange = 40, preferredRange = 8,  reactionDelay = 0.15f },
        };

        public BotTier tier = BotTier.Soldat;
        public bool autoRespawn = false;
        public float respawnDelay = 3f;

        public event Action<EnemyBot> OnDied;
        public bool IsAlive => _health > 0f && gameObject.activeSelf;

        Cfg _cfg;
        float _health;
        CharacterController _cc;
        Transform _target;
        PlayerHealth _targetHealth;
        Renderer _bodyRenderer;
        Transform _eye;
        LayerMask _mask;
        AudioSource _audio;
        CharacterVisual _visual;
        float _nextFire;
        float _armedAt;
        bool _aggro;
        float _vy;
        Vector3 _spawn;

        public static EnemyBot Spawn(Transform parent, Vector3 pos, BotTier tier,
                                     Transform target, PlayerHealth targetHealth,
                                     bool autoRespawn = false, float scale = 1f, string name = "Bot")
        {
            var go = new GameObject(name);
            if (parent) go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.4f; cc.center = new Vector3(0, 0.9f, 0);
            var bot = go.AddComponent<EnemyBot>();
            bot.tier = tier;
            bot.autoRespawn = autoRespawn;
            bot._target = target;
            bot._targetHealth = targetHealth;
            bot.BuildVisual(scale);
            return bot;
        }

        void BuildVisual(float scale)
        {
            _cfg = TierCfg(tier);
            _health = _cfg.health;
            _cc = GetComponent<CharacterController>();
            _spawn = transform.position;
            _mask = Physics.DefaultRaycastLayers; // excludes the player (IgnoreRaycast)
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.spatialBlend = 1f; _audio.maxDistance = 40f; _audio.playOnAwake = false;

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(transform, false);
            body.transform.localPosition = new Vector3(0, 1.0f, 0);
            body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f) * scale;
            _bodyRenderer = body.GetComponent<Renderer>();
            _bodyRenderer.sharedMaterial = ArtPalette.MakeEmissive(ArtPalette.Enemy, 0.4f);

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(transform, false);
            head.transform.localPosition = new Vector3(0, 1.95f * scale, 0);
            head.transform.localScale = Vector3.one * 0.45f * scale;
            head.GetComponent<Renderer>().sharedMaterial = ArtPalette.MakeMaterial(ArtPalette.Signal, 0f, 0.3f);
            head.AddComponent<HeadHitbox>();

            var gun = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gun.name = "Gun";
            Destroy(gun.GetComponent<Collider>());
            gun.transform.SetParent(transform, false);
            gun.transform.localPosition = new Vector3(0.28f, 1.3f, 0.45f);
            gun.transform.localScale = new Vector3(0.12f, 0.12f, 0.5f);
            gun.GetComponent<Renderer>().sharedMaterial = ArtPalette.MakeMaterial(ArtPalette.Metal, 0.8f, 0.6f);

            _eye = new GameObject("Eye").transform;
            _eye.SetParent(transform, false);
            _eye.localPosition = new Vector3(0, 1.6f, 0);

            // Real character model if available (keep the primitive colliders for hit detection).
            var ga = GameAssets.Instance;
            if (ga != null && ga.enemyCharacterPrefab != null)
            {
                Destroy(body.GetComponent<MeshRenderer>());
                Destroy(head.GetComponent<MeshRenderer>());
                Destroy(gun);
                _bodyRenderer = null;
                _visual = CharacterVisual.Attach(transform, ga.enemyCharacterPrefab, 1.8f * scale, ArtPalette.Enemy);
            }
        }

        void Start()
        {
            if (_target == null)
            {
                var ph = FindAnyObjectByType<PlayerHealth>();
                if (ph != null) { _targetHealth = ph; _target = ph.transform; }
            }
        }

        Vector3 Eye => transform.position + Vector3.up * 1.6f;
        Vector3 AimPoint => _target.position + Vector3.up * 1.1f;

        void Update()
        {
            if (!IsAlive || _target == null || _targetHealth == null || !_targetHealth.IsAlive) return;

            Vector3 toPlayer = AimPoint - Eye;
            float dist = toPlayer.magnitude;
            Vector3 dir = toPlayer / Mathf.Max(dist, 0.001f);
            bool los = HasLineOfSight(dir, dist);

            if (!_aggro)
            {
                if (dist <= _cfg.aggroRange && los) { _aggro = true; _armedAt = Time.time + _cfg.reactionDelay; }
            }

            Vector3 move = Vector3.zero;
            if (_aggro)
            {
                Vector3 flat = new Vector3(dir.x, 0, dir.z).normalized;
                if (flat.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), 8f * Time.deltaTime);

                if (dist > _cfg.preferredRange + 1f) move = flat;
                else if (dist < _cfg.preferredRange - 1f) move = -flat;
                else move = transform.right * Mathf.Sign(Mathf.Sin(Time.time * 1.6f));

                if (los && dist <= _cfg.aggroRange && Time.time >= _armedAt && Time.time >= _nextFire)
                    Fire(dist);
            }

            if (_visual != null) _visual.SetSpeed(_aggro && move.sqrMagnitude > 0.01f ? 1f : 0f);

            _vy = _cc.isGrounded ? -1f : _vy - 20f * Time.deltaTime;
            _cc.Move((move.normalized * _cfg.moveSpeed + Vector3.up * _vy) * Time.deltaTime);
        }

        bool HasLineOfSight(Vector3 dir, float dist)
        {
            if (Physics.Raycast(Eye, dir, out var hit, dist, _mask, QueryTriggerInteraction.Ignore))
                return hit.distance >= dist - 1.0f; // player is on IgnoreRaycast, so a nearer hit = blocker
            return true;
        }

        void Fire(float dist)
        {
            _nextFire = Time.time + _cfg.fireInterval;
            _visual?.Shoot();
            Tracer(Eye, AimPoint);
            if (_audio && ProceduralAudio.Shot) _audio.PlayOneShot(ProceduralAudio.Shot, 0.4f);
            float p = _cfg.accuracy * Mathf.Clamp01(1.25f - dist / _cfg.aggroRange);
            if (UnityEngine.Random.value <= p) _targetHealth.TakeDamage(_cfg.damage, AimPoint, Vector3.zero);
        }

        void Tracer(Vector3 a, Vector3 b)
        {
            var go = new GameObject("BotTracer");
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = ArtPalette.MakeUnlit(ArtPalette.Enemy);
            lr.startWidth = 0.03f; lr.endWidth = 0.01f; lr.positionCount = 2;
            lr.SetPosition(0, a); lr.SetPosition(1, b);
            Destroy(go, 0.05f);
        }

        public float TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (!IsAlive) return 0f;
            _health -= amount;
            _aggro = true; // shooting it wakes it up
            StopAllCoroutines();
            StartCoroutine(Flash());
            if (_health <= 0f) Die();
            return amount;
        }

        public void Knockback(Vector3 dir, float distance, float duration)
        {
            StartCoroutine(KnockCo(dir.normalized * distance, duration));
        }

        IEnumerator KnockCo(Vector3 delta, float dur)
        {
            float t = 0f;
            while (t < dur && IsAlive)
            {
                t += Time.deltaTime;
                _cc.Move(delta * (Time.deltaTime / dur));
                yield return null;
            }
        }

        IEnumerator Flash()
        {
            if (_bodyRenderer) _bodyRenderer.material.color = ArtPalette.Signal;
            yield return new WaitForSeconds(0.05f);
            if (_bodyRenderer) _bodyRenderer.material.color = ArtPalette.Enemy;
        }

        void Die()
        {
            _visual?.Die();
            OnDied?.Invoke(this);
            if (autoRespawn) { SetVisible(false); StartCoroutine(Respawn()); }
            else gameObject.SetActive(false);
        }

        IEnumerator Respawn()
        {
            yield return new WaitForSeconds(respawnDelay);
            _health = _cfg.health; _aggro = false;
            _cc.enabled = false; transform.position = _spawn; _cc.enabled = true;
            gameObject.SetActive(true); SetVisible(true);
            if (_bodyRenderer) _bodyRenderer.material.color = ArtPalette.Enemy;
        }

        void SetVisible(bool v)
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true)) if (r) r.enabled = v;
            foreach (var c in GetComponentsInChildren<Collider>(true)) if (c) c.enabled = v;
        }
    }
}
