using UnityEngine;

namespace FirstGame.Core
{
    /// <summary>Instantiates an imported model, strips its colliders, auto-scales it to a target
    /// size by measured bounds (robust to unknown FBX import scale), and optionally recolours it.</summary>
    public static class ModelUtil
    {
        public static GameObject Spawn(GameObject prefab, Transform parent, float targetSize, bool byHeight, Material material)
        {
            var m = Object.Instantiate(prefab, parent);
            m.transform.localPosition = Vector3.zero;
            m.transform.localRotation = Quaternion.identity;
            m.transform.localScale = Vector3.one;

            foreach (var c in m.GetComponentsInChildren<Collider>()) Object.Destroy(c);

            var rends = m.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                float dim = byHeight ? b.size.y : Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                if (dim > 0.0001f) m.transform.localScale = Vector3.one * (targetSize / dim);
                if (material != null)
                    foreach (var r in rends) r.sharedMaterial = material;
            }
            return m;
        }

        public static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform c in go.transform) SetLayerRecursive(c.gameObject, layer);
        }

        /// <summary>Places a level prop: auto-scaled, grounded (base at pos.y), tinted, with a
        /// BoxCollider covering its bounds so it blocks players/bots/bullets.</summary>
        public static GameObject SpawnProp(GameObject prefab, Transform parent, Vector3 pos, float yRotation,
                                           float targetSize, bool byHeight, Material material)
        {
            var root = new GameObject(prefab.name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;
            root.transform.localRotation = Quaternion.identity;

            var model = Object.Instantiate(prefab, root.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            model.transform.localScale = Vector3.one;

            foreach (var c in model.GetComponentsInChildren<Collider>()) Object.Destroy(c);

            var rends = model.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return root;

            Bounds b = Combined(rends);
            float dim = byHeight ? b.size.y : Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (dim > 0.0001f) model.transform.localScale = Vector3.one * (targetSize / dim);
            if (material != null) foreach (var r in rends) r.sharedMaterial = material;

            // ground: base sits at pos.y
            b = Combined(rends);
            model.transform.localPosition = new Vector3(0f, -(b.min.y - root.transform.position.y), 0f);

            // box collider from final bounds (root unrotated + scale 1 → local == world - pos)
            b = Combined(rends);
            var bc = root.AddComponent<BoxCollider>();
            bc.center = b.center - root.transform.position;
            bc.size = b.size;
            return root;
        }

        static Bounds Combined(Renderer[] rends)
        {
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return b;
        }
    }
}
