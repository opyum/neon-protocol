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
    }
}
