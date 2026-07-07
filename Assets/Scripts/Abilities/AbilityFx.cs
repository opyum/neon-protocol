using System;
using UnityEngine;
using FirstGame.Core;
using FirstGame.Combat;

namespace FirstGame.Abilities
{
    /// <summary>
    /// Stateless spawners for the ability "volumes" (ice wall, shadow smoke, toxic zone).
    /// Extracted from AbilitySystem so BOTH the player and the bots (ennemis-agents) can cast
    /// the exact same effects. Durations are passed in by the caller (agent passives scale them).
    /// </summary>
    public static class AbilityFx
    {
        /// <summary>Ice wall: solid, opaque slab that blocks bullets AND movement (keeps its BoxCollider).</summary>
        public static GameObject Wall(Vector3 basePos, Vector3 forward, Color color, float life, string id = "")
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            forward.Normalize();

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); // keep BoxCollider
            go.name = "IceWall_" + id;
            go.transform.position = basePos + Vector3.up * 1.5f;
            go.transform.rotation = Quaternion.LookRotation(Vector3.Cross(Vector3.up, forward));
            go.transform.localScale = new Vector3(4f, 3f, 0.3f);
            var c = color; c.a = 1f;
            go.GetComponent<Renderer>().sharedMaterial = ArtPalette.MakeMaterial(c, 0f, 0.6f);
            UnityEngine.Object.Destroy(go, life);
            return go;
        }

        /// <summary>Shadow smoke: opaque sphere that only blocks vision (no collider).</summary>
        public static GameObject Smoke(Vector3 center, Color color, float life, string id = "")
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var col = go.GetComponent<Collider>(); if (col) UnityEngine.Object.Destroy(col);
            go.name = "ShadowSmoke_" + id;
            go.transform.position = center;
            go.transform.localScale = Vector3.one * 5f;
            var c = color; c.a = 1f;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = ArtPalette.MakeUnlit(c);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            UnityEngine.Object.Destroy(go, life);
            return go;
        }

        /// <summary>Toxic zone: trigger-free DoT volume (OverlapSphere per tick) that ignores <paramref name="self"/>.</summary>
        public static ToxicZone Zone(Vector3 center, Color color, float radius, float dps, float tick, float life,
                                     IDamageable self, Action<IDamageable> onHit, string id = "")
        {
            var root = new GameObject("ToxicZone_" + id);
            root.transform.position = center;
            var z = root.AddComponent<ToxicZone>();
            z.radius = radius; z.dps = dps; z.tick = tick; z.life = life;
            z.self = self;
            z.onHit = onHit;
            Prim.Cylinder(root.transform, Vector3.up * 0.3f, radius, 0.6f, color, unlit: true, name: "ZoneFx");
            return z;
        }
    }
}
