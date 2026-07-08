using UnityEngine;

namespace FirstGame.Core
{
    /// <summary>Designed arena generator: a connected maze of rooms (recursive backtracker) linked by
    /// doorways/chokepoints, with cover props for hiding + strategy. Uses the real modular sci-fi
    /// prefabs when available (GameAssets.Prop), else textured primitive fallbacks. Seeded per (mode, map).</summary>
    public static class MapGen
    {
        const float CenterZ = 10f;
        const int N = 5;                 // grid cells per side
        const float Cell = 11f;
        static float MinX => -N * Cell * 0.5f;
        static float MinZ => CenterZ - N * Cell * 0.5f;

        static readonly string[] CoverKeys = { "container_big", "container_small", "crate_hd", "half_wall", "storage_big" };

        public static void Build(Transform arena, int seed)
        {
            var rng = new System.Random(seed * 733 + 97);

            // ---- Carve a connected maze (recursive backtracker) ----
            bool[,] wallE = new bool[N, N]; // wall on the EAST edge of cell (x,z)
            bool[,] wallN = new bool[N, N]; // wall on the NORTH edge of cell (x,z)
            for (int x = 0; x < N; x++) for (int z = 0; z < N; z++) { wallE[x, z] = true; wallN[x, z] = true; }

            bool[,] vis = new bool[N, N];
            var stack = new System.Collections.Generic.Stack<Vector2Int>();
            vis[0, 0] = true; stack.Push(new Vector2Int(0, 0));
            while (stack.Count > 0)
            {
                var c = stack.Peek();
                var nbs = new System.Collections.Generic.List<int>(); // 0=E 1=W 2=N 3=S
                if (c.x + 1 < N && !vis[c.x + 1, c.y]) nbs.Add(0);
                if (c.x - 1 >= 0 && !vis[c.x - 1, c.y]) nbs.Add(1);
                if (c.y + 1 < N && !vis[c.x, c.y + 1]) nbs.Add(2);
                if (c.y - 1 >= 0 && !vis[c.x, c.y - 1]) nbs.Add(3);
                if (nbs.Count == 0) { stack.Pop(); continue; }
                int dir = nbs[rng.Next(nbs.Count)];
                switch (dir)
                {
                    case 0: wallE[c.x, c.y] = false; vis[c.x + 1, c.y] = true; stack.Push(new Vector2Int(c.x + 1, c.y)); break;
                    case 1: wallE[c.x - 1, c.y] = false; vis[c.x - 1, c.y] = true; stack.Push(new Vector2Int(c.x - 1, c.y)); break;
                    case 2: wallN[c.x, c.y] = false; vis[c.x, c.y + 1] = true; stack.Push(new Vector2Int(c.x, c.y + 1)); break;
                    case 3: wallN[c.x, c.y - 1] = false; vis[c.x, c.y - 1] = true; stack.Push(new Vector2Int(c.x, c.y - 1)); break;
                }
            }

            // ---- Braid: open ~40% of remaining interior walls for loops/lanes (FPS flow, not a dead-end maze) ----
            for (int x = 0; x < N; x++)
                for (int z = 0; z < N; z++)
                {
                    if (x < N - 1 && wallE[x, z] && rng.NextDouble() < 0.4) wallE[x, z] = false;
                    if (z < N - 1 && wallN[x, z] && rng.NextDouble() < 0.4) wallN[x, z] = false;
                }

            // ---- Render interior walls as spans with doorways (chokepoints) ----
            for (int x = 0; x < N; x++)
                for (int z = 0; z < N; z++)
                {
                    if (x < N - 1)
                    {
                        var center = new Vector3(MinX + (x + 1) * Cell, 0f, MinZ + (z + 0.5f) * Cell);
                        RenderEdge(arena, wallE[x, z], center, Cell, alongX: false, rng);
                    }
                    if (z < N - 1)
                    {
                        var center = new Vector3(MinX + (x + 0.5f) * Cell, 0f, MinZ + (z + 1) * Cell);
                        RenderEdge(arena, wallN[x, z], center, Cell, alongX: true, rng);
                    }
                }

            // ---- Cover props inside cells (hiding spots + peek angles) ----
            for (int x = 0; x < N; x++)
                for (int z = 0; z < N; z++)
                {
                    var cc = new Vector3(MinX + (x + 0.5f) * Cell, 0f, MinZ + (z + 0.5f) * Cell);
                    if (SpawnZone(cc)) continue;           // keep spawns open
                    bool site = ObjectiveZone(cc);
                    int pieces = site ? 3 : 1 + rng.Next(0, 2); // sites = strategic, heavier cover
                    for (int i = 0; i < pieces; i++)
                    {
                        var off = new Vector3((float)(rng.NextDouble() * 2 - 1) * 3.2f, 0f, (float)(rng.NextDouble() * 2 - 1) * 3.2f);
                        Cover(arena, cc + off, rng);
                    }
                }
        }

        static bool SpawnZone(Vector3 p) =>
            new Vector2(p.x, p.z + 18f).sqrMagnitude < 40f || p.z > 30f; // player spawn + bot spawn band

        static bool ObjectiveZone(Vector3 p) => new Vector2(p.x, p.z - 18f).sqrMagnitude < 64f;

        // ---- Rendering ----
        static void RenderEdge(Transform arena, bool hasWall, Vector3 center, float len, bool alongX, System.Random rng)
        {
            if (hasWall) { PlaceSpan(arena, center, len, alongX); return; }
            const float gap = 4.4f; // doorway / chokepoint
            if (len <= gap + 1.5f) return;
            float side = (len - gap) * 0.5f;
            var dir = alongX ? Vector3.right : Vector3.forward;
            PlaceSpan(arena, center - dir * (gap * 0.5f + side * 0.5f), side, alongX);
            PlaceSpan(arena, center + dir * (gap * 0.5f + side * 0.5f), side, alongX);
        }

        static void PlaceSpan(Transform arena, Vector3 groundCenter, float len, bool alongX)
        {
            if (len < 0.6f) return;
            var ga = GameAssets.Instance;
            var prefab = ga != null ? ga.Prop("wall_tall") : null;
            if (prefab != null)
            {
                int segs = Mathf.Max(1, Mathf.RoundToInt(len / 3.2f));
                var dir = alongX ? Vector3.right : Vector3.forward;
                for (int i = 0; i < segs; i++)
                {
                    float t = (i + 0.5f) / segs - 0.5f;
                    var seg = ModelUtil.SpawnProp(prefab, arena, groundCenter + dir * (len * t), alongX ? 0f : 90f, 3.4f, byHeight: true, material: null);
                    var oc = seg.GetComponent<Collider>(); if (oc) Object.Destroy(oc); // visual only — a tight collider is added below
                }
                AddWallCollider(arena, groundCenter, len, alongX); // one solid, tight collider (reliable LOS + movement)
            }
            else
            {
                var size = alongX ? new Vector3(len, 3.2f, 0.6f) : new Vector3(0.6f, 3.2f, len);
                var go = Prim.Box(arena, groundCenter + Vector3.up * 1.6f, size, ArtPalette.Wall, name: "Wall");
                var r = go.GetComponent<Renderer>(); if (r) r.sharedMaterial = Surfaces.Metal;
            }
        }

        static void AddWallCollider(Transform arena, Vector3 groundCenter, float len, bool alongX)
        {
            var go = new GameObject("WallCollider");
            go.transform.SetParent(arena, false);
            go.transform.localPosition = groundCenter + Vector3.up * 1.6f;
            var bc = go.AddComponent<BoxCollider>();
            bc.size = alongX ? new Vector3(len, 3.2f, 0.7f) : new Vector3(0.7f, 3.2f, len);
        }

        static void Cover(Transform arena, Vector3 pos, System.Random rng)
        {
            var ga = GameAssets.Instance;
            string key = CoverKeys[rng.Next(CoverKeys.Length)];
            var prefab = ga != null ? ga.Prop(key) : null;
            if (prefab != null)
            {
                float size = key == "half_wall" ? 1.3f : key == "crate_hd" ? 1.5f : 2.4f;
                bool byH = key == "half_wall";
                ModelUtil.SpawnProp(prefab, arena, pos, rng.Next(0, 4) * 90f, size, byHeight: byH, material: null);
            }
            else
            {
                bool low = rng.Next(2) == 0;
                var size = low ? new Vector3(2.2f, 1.2f, 0.7f) : new Vector3(1.7f, 1.6f, 1.7f);
                var go = Prim.Box(arena, pos + Vector3.up * size.y * 0.5f, size, ArtPalette.Cover, name: "Cover");
                var r = go.GetComponent<Renderer>(); if (r) r.sharedMaterial = Surfaces.Metal;
            }
        }
    }
}
