using UnityEngine;

namespace FirstGame.Enemies
{
    /// <summary>Marker on a head collider so weapons can detect and reward headshots (x2).</summary>
    public class HeadHitbox : MonoBehaviour
    {
        public TrainingDummy owner;
    }
}
