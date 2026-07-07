using UnityEngine;

namespace FirstGame.Core
{
    /// <summary>Procedural labyrinth-style interior for the combat arena. Seeded, so each map index
    /// is a distinct-but-deterministic layout: scattered wall segments (corridors), pillars and crates,
    /// with the spawn/objective areas kept clear for balance.</summary>
    public static class MapGen
    {
        // Playable footprint (matches the arena floor centred ~ (0, 10)).
        const float CenterZ = 10f;
        const int Cells = 7;
        const float Cell = 8f;

        public static void Build(Transform arena, int seed)
        {
            var rng = new System.Random(seed * 9973 + 12345);
            var mat = Surfaces.Metal;
            float half = Cells * Cell * 0.5f;
            const float wallH = 3.2f, wallT = 0.6f;

            // Vertical wall segments (between columns).
            for (int gx = 1; gx < Cells; gx++)
                for (int gz = 0; gz < Cells; gz++)
                {
                    if (rng.NextDouble() > 0.30) continue;
                    float x = -half + gx * Cell;
                    float z = CenterZ - half + (gz + 0.5f) * Cell;
                    if (Clear(x, z)) continue;
                    Wall(arena, new Vector3(x, wallH * 0.5f, z), new Vector3(wallT, wallH, Cell * 0.9f), mat);
                }

            // Horizontal wall segments (between rows).
            for (int gz = 1; gz < Cells; gz++)
                for (int gx = 0; gx < Cells; gx++)
                {
                    if (rng.NextDouble() > 0.30) continue;
                    float z = CenterZ - half + gz * Cell;
                    float x = -half + (gx + 0.5f) * Cell;
                    if (Clear(x, z)) continue;
                    Wall(arena, new Vector3(x, wallH * 0.5f, z), new Vector3(Cell * 0.9f, wallH, wallT), mat);
                }

            // Pillars at some grid intersections.
            for (int gx = 1; gx < Cells; gx++)
                for (int gz = 1; gz < Cells; gz++)
                {
                    if (rng.NextDouble() > 0.16) continue;
                    float x = -half + gx * Cell, z = CenterZ - half + gz * Cell;
                    if (Clear(x, z)) continue;
                    Wall(arena, new Vector3(x, 1.8f, z), new Vector3(1.2f, 3.6f, 1.2f), mat);
                }

            // Scattered crates (chest-high cover).
            int crates = 10 + rng.Next(0, 8);
            for (int i = 0; i < crates; i++)
            {
                float x = (float)(rng.NextDouble() * 2 - 1) * (half - 3f);
                float z = CenterZ + (float)(rng.NextDouble() * 2 - 1) * (half - 3f);
                if (Clear(x, z)) continue;
                var box = Prim.Box(arena, new Vector3(x, 0.75f, z), new Vector3(1.6f, 1.5f, 1.6f), ArtPalette.Cover, name: "Crate");
                var r = box.GetComponent<Renderer>(); if (r) r.sharedMaterial = mat;
            }
        }

        // Keep the objective, spawns and a central corridor clear so a path always exists.
        static bool Clear(float x, float z)
        {
            if (new Vector2(x, z - 18f).sqrMagnitude < 49f) return true;   // objective zone
            if (new Vector2(x, z + 18f).sqrMagnitude < 36f) return true;   // player spawn
            if (z > 30f) return true;                                      // bot spawn band (far end)
            if (Mathf.Abs(x) < 3.5f) return true;                          // central N-S corridor (guaranteed path)
            if (Mathf.Abs(z - 10f) < 3.5f) return true;                    // central E-W corridor
            return false;
        }

        static void Wall(Transform arena, Vector3 pos, Vector3 size, Material mat)
        {
            var go = Prim.Box(arena, pos, size, ArtPalette.Wall, name: "MazeWall");
            var r = go.GetComponent<Renderer>(); if (r) r.sharedMaterial = mat;
        }
    }
}
