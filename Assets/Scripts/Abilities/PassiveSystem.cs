using UnityEngine;
using FirstGame.Core;
using FirstGame.Combat;
using FirstGame.Player;
using FirstGame.Progression;

namespace FirstGame.Abilities
{
    /// <summary>Sums the player's 3 equipped passive skills and applies their modifiers to the rig.</summary>
    public class PassiveSystem : MonoBehaviour
    {
        float _lifesteal;
        PlayerHealth _health;

        public void Init(PlayerRig.Refs r)
        {
            float dmg = 0, hp = 0, spd = 0, cd = 0, ls = 0, regen = 0, reload = 0, dr = 0;
            var p = PlayerProfile.Current;
            for (int i = 0; i < 3; i++)
            {
                var pd = PassiveCatalog.ById(p.GetPassive(i));
                if (pd == null) continue;
                dmg += pd.damagePct; hp += pd.healthFlat; spd += pd.speedPct; cd += pd.cooldownPct;
                ls += pd.lifestealPct; regen += pd.regenFlat; reload += pd.reloadPct; dr += pd.damageReductionPct;
            }

            if (r.controller != null) r.controller.passiveSpeedMul = 1f + spd / 100f;
            if (r.weapon != null) { r.weapon.passiveDamageMul = 1f + dmg / 100f; r.weapon.reloadMul = Mathf.Max(0.2f, 1f - reload / 100f); }
            if (r.abilities != null)
            {
                r.abilities.passiveDamageMul = 1f + dmg / 100f;
                r.abilities.passiveCdMul = Mathf.Max(0.2f, 1f - cd / 100f);
                r.abilities.ReloadLoadout(); // recompute power/cooldown with the passives
            }
            if (r.health != null) r.health.ApplyPassives(hp, regen, Mathf.Clamp01(dr / 100f));

            _lifesteal = ls / 100f;
            _health = r.health;
            if (r.weapon != null && _lifesteal > 0f) r.weapon.OnHit += OnHit;
        }

        void OnHit(IDamageable target, float dmg, bool head)
        {
            if (_health != null && _lifesteal > 0f) _health.Heal(dmg * _lifesteal);
        }
    }
}
