using UnityEngine;

namespace FirstGame.Core
{
    /// <summary>Procedural geometry helpers — the whole world is built from primitives.</summary>
    public static class Prim
    {
        public static GameObject Box(Transform parent, Vector3 pos, Vector3 size, Color color,
                                     float metallic = 0f, float smoothness = 0.15f, bool collider = true, string name = "Box")
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (!collider) Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = ArtPalette.MakeMaterial(color, metallic, smoothness);
            return go;
        }

        public static GameObject NeonStrip(Transform parent, Vector3 pos, Vector3 size, Color color, string name = "Neon")
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = ArtPalette.MakeUnlit(color);
            return go;
        }

        public static GameObject Cylinder(Transform parent, Vector3 pos, float radius, float height, Color color,
                                          bool unlit = false, string name = "Cyl")
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = unlit ? ArtPalette.MakeUnlit(color) : ArtPalette.MakeMaterial(color);
            return go;
        }

        public static GameObject Sphere(Transform parent, Vector3 pos, float diameter, Color color, bool unlit = false, string name = "Sphere")
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = Vector3.one * diameter;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = unlit ? ArtPalette.MakeUnlit(color) : ArtPalette.MakeMaterial(color);
            return go;
        }

        /// <summary>Bright core + additive halo = cheap bloom-like neon glow.</summary>
        public static GameObject NeonGlowSphere(Transform parent, Vector3 pos, float diameter, Color color, string name = "NeonGlow")
        {
            var core = Sphere(parent, pos, diameter, color, unlit: true, name: name);
            Object.Destroy(core.GetComponent<Collider>());
            var halo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            halo.name = "Halo";
            Object.Destroy(halo.GetComponent<Collider>());
            halo.transform.SetParent(core.transform, false);
            halo.transform.localScale = Vector3.one * 2.4f;
            halo.GetComponent<Renderer>().sharedMaterial = ArtPalette.MakeGlow(color);
            return core;
        }

        /// <summary>A neon grid on the XZ plane (Tron-style floor) built from thin strips.</summary>
        public static GameObject Grid(Transform parent, Vector3 center, float size, int cells, Color color, string name = "Grid")
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = center;
            float half = size * 0.5f;
            float step = size / cells;
            for (int i = 0; i <= cells; i++)
            {
                float o = -half + i * step;
                NeonStrip(root.transform, new Vector3(o, 0, 0), new Vector3(0.05f, 0.02f, size), color, "L");
                NeonStrip(root.transform, new Vector3(0, 0, o), new Vector3(size, 0.02f, 0.05f), color, "L");
            }
            return root;
        }
    }

    /// <summary>Rotates a transform slowly (menu backdrop flair).</summary>
    public class SlowSpin : MonoBehaviour
    {
        public Vector3 degreesPerSecond = new Vector3(0, 12, 0);
        void Update() => transform.Rotate(degreesPerSecond * Time.deltaTime, Space.World);
    }

    /// <summary>Gentle vertical bobbing (floating menu shapes). Phase seeded by X for variety.</summary>
    public class Bob : MonoBehaviour
    {
        public float amplitude = 0.35f;
        public float speed = 1f;
        Vector3 _base;
        float _phase;
        void Awake() { _base = transform.localPosition; _phase = transform.position.x * 1.3f; }
        void Update()
        {
            var p = _base;
            p.y += Mathf.Sin(Time.time * speed + _phase) * amplitude;
            transform.localPosition = p;
        }
    }

    /// <summary>Pulses an unlit material's brightness (objective markers).</summary>
    public class Pulse : MonoBehaviour
    {
        public float speed = 2f;
        public Color color = Color.yellow;
        Renderer _r;
        void Awake() { _r = GetComponent<Renderer>(); }
        void Update()
        {
            if (_r == null) return;
            float t = 0.55f + 0.45f * Mathf.Sin(Time.time * speed);
            _r.material.color = color * t;
        }
    }
}
