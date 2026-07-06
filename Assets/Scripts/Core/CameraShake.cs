using UnityEngine;

namespace FirstGame.Core
{
    /// <summary>Trauma-based camera shake. Add() bumps trauma; it decays each frame.</summary>
    public class CameraShake : MonoBehaviour
    {
        public float maxAngle = 2.2f;
        public float maxOffset = 0.10f;
        public float decay = 1.6f;

        Vector3 _baseLocalPos;
        float _trauma;

        void Awake() => _baseLocalPos = transform.localPosition;

        public void Add(float amount) => _trauma = Mathf.Clamp01(_trauma + amount);

        void LateUpdate()
        {
            if (_trauma <= 0f)
            {
                transform.localPosition = _baseLocalPos;
                transform.localRotation = Quaternion.identity;
                return;
            }

            float shake = _trauma * _trauma;
            float t = Time.time * 28f;
            float nx = Mathf.PerlinNoise(t, 0f) - 0.5f;
            float ny = Mathf.PerlinNoise(0f, t) - 0.5f;
            float nr = Mathf.PerlinNoise(t, t) - 0.5f;

            transform.localPosition = _baseLocalPos + new Vector3(nx, ny, 0f) * 2f * maxOffset * shake;
            transform.localRotation = Quaternion.Euler(ny * 2f * maxAngle * shake, nx * 2f * maxAngle * shake, nr * 2f * maxAngle * shake);

            _trauma = Mathf.Max(0f, _trauma - decay * Time.deltaTime);
        }
    }
}
