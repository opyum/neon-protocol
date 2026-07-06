using UnityEngine;

namespace FirstGame.Core
{
    /// <summary>Procedural combat VFX (no external assets): expanding glow, light flash, debris,
    /// impact sparks and muzzle flashes. Built from URP-safe glow/unlit materials.</summary>
    public static class Vfx
    {
        public static void Explosion(Vector3 pos, Color color)
        {
            GlowSphere(pos, color, 0.4f, 4.5f, 0.4f);
            Flash(pos, color, 9f, 14f, 0.22f);
            Debris(pos, color, 10, 0.28f);
        }

        public static void Burst(Vector3 pos, Color color)
        {
            GlowSphere(pos, color, 0.25f, 2.2f, 0.28f);
            Flash(pos, color, 5f, 8f, 0.16f);
            Debris(pos, color, 5, 0.18f);
        }

        public static void Impact(Vector3 pos, Color color)
        {
            GlowSphere(pos, color, 0.05f, 0.6f, 0.12f);
            Debris(pos, color, 4, 0.1f);
        }

        public static void Muzzle(Vector3 pos, Vector3 forward, Color color)
        {
            GlowSphere(pos + forward * 0.2f, color, 0.15f, 0.45f, 0.06f);
            Flash(pos, color, 3f, 5f, 0.05f);
        }

        // ---- primitives ----
        static void GlowSphere(Vector3 pos, Color color, float from, float to, float dur)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var col = s.GetComponent<Collider>(); if (col) Object.Destroy(col);
            s.transform.position = pos;
            s.GetComponent<Renderer>().sharedMaterial = ArtPalette.MakeGlow(color, 3f);
            var e = s.AddComponent<ExpandFade>();
            e.duration = dur; e.startScale = from; e.endScale = to; e.color = color;
        }

        static void Flash(Vector3 pos, Color color, float intensity, float range, float dur)
        {
            var go = new GameObject("Flash");
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.color = color; l.range = range; l.intensity = intensity;
            var lf = go.AddComponent<LightFade>();
            lf.duration = dur; lf.startIntensity = intensity;
        }

        static void Debris(Vector3 pos, Color color, int count, float size)
        {
            var mat = ArtPalette.MakeUnlit(color);
            for (int i = 0; i < count; i++)
            {
                var d = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var col = d.GetComponent<Collider>(); if (col) Object.Destroy(col);
                d.transform.position = pos;
                d.transform.localScale = Vector3.one * (size * Random.Range(0.5f, 1.2f));
                d.GetComponent<Renderer>().sharedMaterial = mat;
                var db = d.AddComponent<DebrisPiece>();
                db.velocity = Random.onUnitSphere * Random.Range(4f, 9f) + Vector3.up * 3f;
            }
        }
    }

    public class ExpandFade : MonoBehaviour
    {
        public float duration = 0.3f, startScale = 0.2f, endScale = 3f;
        public Color color = Color.white;
        Renderer _r; float _t;
        void Awake() { _r = GetComponent<Renderer>(); transform.localScale = Vector3.one * startScale; }
        void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / duration);
            transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, k);
            if (_r != null)
            {
                _r.material.SetColor("_EmissionColor", color * ((1f - k) * 3f));
                _r.material.color = new Color(color.r, color.g, color.b, (1f - k) * 0.25f);
            }
            if (_t >= duration) Destroy(gameObject);
        }
    }

    public class LightFade : MonoBehaviour
    {
        public float duration = 0.2f, startIntensity = 8f;
        Light _l; float _t;
        void Awake() { _l = GetComponent<Light>(); }
        void Update()
        {
            _t += Time.deltaTime;
            if (_l != null) _l.intensity = Mathf.Lerp(startIntensity, 0f, _t / duration);
            if (_t >= duration) Destroy(gameObject);
        }
    }

    public class DebrisPiece : MonoBehaviour
    {
        public Vector3 velocity;
        float _t;
        void Update()
        {
            velocity += Vector3.down * 15f * Time.deltaTime;
            transform.position += velocity * Time.deltaTime;
            transform.Rotate(new Vector3(220f, 300f, 140f) * Time.deltaTime);
            _t += Time.deltaTime;
            if (_t > 0.6f) Destroy(gameObject);
        }
    }
}
