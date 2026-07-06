using UnityEngine;

namespace FirstGame.Core
{
    /// <summary>Wraps an imported character model: spawns + tints it, and drives its Animator
    /// (if it has one with matching params — guarded, so a rig-only model is fine too).</summary>
    public class CharacterVisual : MonoBehaviour
    {
        Animator _animator;
        static readonly int SpeedHash = Animator.StringToHash("Speed");
        static readonly int ShootHash = Animator.StringToHash("Shoot");
        static readonly int DieHash = Animator.StringToHash("Die");

        public static CharacterVisual Attach(Transform parent, GameObject prefab, float targetHeight, Color tint)
        {
            // Keep the model's real skin (textures). Only tint if it has no material (e.g. Kenney
            // imported without materials) — that both avoids pink under URP and keeps readability.
            var model = ModelUtil.Spawn(prefab, parent, targetHeight, byHeight: true, material: null);
            var rends = model.GetComponentsInChildren<Renderer>();
            bool hasSkin = rends.Length > 0 && rends[0].sharedMaterial != null;
            if (!hasSkin)
            {
                var mat = ArtPalette.MakeEmissive(tint, 0.35f);
                foreach (var r in rends) r.sharedMaterial = mat;
            }

            var cv = model.AddComponent<CharacterVisual>();
            cv._animator = model.GetComponentInChildren<Animator>();
            if (cv._animator != null) cv._animator.applyRootMotion = false; // bot controller drives movement
            return cv;
        }

        public void SetSpeed(float v) { if (Has(SpeedHash, AnimatorControllerParameterType.Float)) _animator.SetFloat(SpeedHash, v); }
        public void Shoot() { if (Has(ShootHash, AnimatorControllerParameterType.Trigger)) _animator.SetTrigger(ShootHash); }
        public void Die() { if (Has(DieHash, AnimatorControllerParameterType.Trigger)) _animator.SetTrigger(DieHash); }

        bool Has(int hash, AnimatorControllerParameterType type)
        {
            if (_animator == null || _animator.runtimeAnimatorController == null) return false;
            foreach (var p in _animator.parameters) if (p.nameHash == hash && p.type == type) return true;
            return false;
        }
    }
}
