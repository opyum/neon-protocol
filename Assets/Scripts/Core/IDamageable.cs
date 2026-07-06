using UnityEngine;

namespace FirstGame.Combat
{
    public interface IDamageable
    {
        /// <summary>Apply damage. Returns the damage actually dealt (after multipliers).</summary>
        float TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal);
        bool IsAlive { get; }
    }
}
