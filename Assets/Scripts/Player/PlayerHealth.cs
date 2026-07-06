using System;
using UnityEngine;
using FirstGame.Combat;
using FirstGame.Progression;

namespace FirstGame.Player
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        public float MaxHealth { get; private set; }
        public float Health { get; private set; }
        public float Shield { get; private set; }
        public bool IsAlive => Health > 0f;

        public event Action<float, float> OnHealthChanged; // (health, maxHealth)
        public event Action<float> OnShieldChanged;
        public event Action OnDied;

        public Vector3 SpawnPoint;
        float _regenPerSecond;
        float _lastDamageTime = -999f;
        const float RegenDelay = 5f;

        void Awake()
        {
            MaxHealth = PlayerProfile.Current.MaxHealth;
            Health = MaxHealth;
            _regenPerSecond = PlayerProfile.Current.RegenPerSecond;
            SpawnPoint = transform.position;
        }

        void Start()
        {
            OnHealthChanged?.Invoke(Health, MaxHealth);
            OnShieldChanged?.Invoke(Shield);
        }

        void Update()
        {
            if (IsAlive && _regenPerSecond > 0f && Time.time - _lastDamageTime >= RegenDelay && Health < MaxHealth)
            {
                Health = Mathf.Min(MaxHealth, Health + _regenPerSecond * Time.deltaTime);
                OnHealthChanged?.Invoke(Health, MaxHealth);
            }
        }

        public float TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (!IsAlive) return 0f;
            _lastDamageTime = Time.time;

            float remaining = amount;
            if (Shield > 0f)
            {
                float absorbed = Mathf.Min(Shield, remaining);
                Shield -= absorbed;
                remaining -= absorbed;
                OnShieldChanged?.Invoke(Shield);
            }

            Health = Mathf.Max(0f, Health - remaining);
            OnHealthChanged?.Invoke(Health, MaxHealth);
            if (Health <= 0f) { OnDied?.Invoke(); Respawn(); }
            return amount;
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;
            Health = Mathf.Min(MaxHealth, Health + amount);
            OnHealthChanged?.Invoke(Health, MaxHealth);
        }

        public void AddShield(float amount)
        {
            Shield += amount;
            OnShieldChanged?.Invoke(Shield);
        }

        void Respawn()
        {
            Health = MaxHealth;
            Shield = 0f;
            var cc = GetComponent<CharacterController>();
            if (cc) cc.enabled = false;
            transform.position = SpawnPoint;
            if (cc) cc.enabled = true;
            OnHealthChanged?.Invoke(Health, MaxHealth);
            OnShieldChanged?.Invoke(Shield);
        }
    }
}
