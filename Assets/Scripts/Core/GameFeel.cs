using UnityEngine;
using FirstGame.Combat;
using FirstGame.Abilities;
using FirstGame.Player;

namespace FirstGame.Core
{
    /// <summary>Central "juice": plays procedural SFX and drives camera shake from gameplay events.</summary>
    public class GameFeel : MonoBehaviour
    {
        public WeaponController weapon;
        public AbilitySystem abilities;
        public PlayerHealth health;
        public CameraShake shake;

        AudioSource _src;
        float _lastHealth = -1f;

        void Start()
        {
            _src = gameObject.AddComponent<AudioSource>();
            _src.spatialBlend = 0f;
            _src.playOnAwake = false;

            if (weapon != null)
            {
                weapon.OnFired += OnFired;
                weapon.OnReloadStart += () => Play(ProceduralAudio.Reload, 0.7f);
                weapon.OnHit += (t, d, head) => Play(head ? ProceduralAudio.HitHead : ProceduralAudio.Hit, 0.8f);
            }
            if (abilities != null)
                abilities.OnAbilityUsed += (s, a) => Play(ProceduralAudio.Ability, 0.7f);
            if (health != null)
                health.OnHealthChanged += OnHealth;
        }

        void OnFired()
        {
            Play(ProceduralAudio.Shot, 0.55f);
            if (shake != null) shake.Add(0.16f);
        }

        void OnHealth(float h, float max)
        {
            if (_lastHealth >= 0f && h < _lastHealth && shake != null) shake.Add(0.45f);
            _lastHealth = h;
        }

        void Play(AudioClip clip, float volume)
        {
            if (clip != null && _src != null) _src.PlayOneShot(clip, volume);
        }
    }
}
