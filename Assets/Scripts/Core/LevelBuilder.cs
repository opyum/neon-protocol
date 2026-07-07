using UnityEngine;

namespace FirstGame.Core
{
    /// <summary>Builds the combat arena's cover/structures. Prefers the HD sci-fi pack props
    /// (real textured models); falls back to Kenney prototype props, then primitives.</summary>
    public static class LevelBuilder
    {
        public static bool BuildCombatCover(Transform arena)
        {
            var ga = GameAssets.Instance;
            if (ga == null) return false;
            bool beta = MatchConfig.ArenaLayout == 1;
            if (ga.Prop("container_big") != null) { if (beta) BuildSciFiBeta(arena); else BuildSciFi(arena); return true; }
            if (ga.Prop("crate") != null) { if (beta) BuildKenneyBeta(arena); else BuildKenney(arena); return true; }
            return false;
        }

        // ---- HD sci-fi arena (keeps the packs' own textured materials) ----
        static void BuildSciFi(Transform arena)
        {
            // Centrepiece at the objective
            P("shield_core", arena, 0, 18, 0, 3.2f, true);

            // Big containers = solid cover
            P("container_big", arena, -8, 6, 0, 2.6f, false);
            P("container_big", arena, 8, 6, 0, 2.6f, false);
            P("storage_big", arena, 0, 0, 0, 2.6f, false);
            P("container_big", arena, -7, 26, 90, 2.6f, false);
            P("container_big", arena, 7, 26, 90, 2.6f, false);

            // Small containers / crates
            P("container_small", arena, -4, -12, 0, 1.6f, false);
            P("container_small", arena, 4, -12, 0, 1.6f, false);
            P("crate_hd", arena, -12, 14, 0, 1.5f, false);
            P("crate_hd", arena, 12, 14, 0, 1.5f, false);
            P("crate_hd", arena, -10, 20, 0, 1.5f, false);
            P("crate_hd", arena, 10, 20, 0, 1.5f, false);

            // Pillars
            P("pillar", arena, -14, 8, 0, 4.2f, true);
            P("pillar", arena, 14, 8, 0, 4.2f, true);
            P("pillar", arena, -14, 30, 0, 4.2f, true);
            P("pillar", arena, 14, 30, 0, 4.2f, true);

            // Machines as detail/cover (often emissive → fits the neon theme)
            P("generator", arena, -17, 18, 0, 2.4f, true);
            P("capacitor", arena, 17, 18, 0, 2.4f, true);
            P("battery", arena, 0, 33, 0, 2.2f, true);

            // Half walls = chest-high cover around the site
            P("half_wall", arena, 0, 11, 0, 1.4f, true);
            P("half_wall", arena, -6, 18, 90, 1.4f, true);
            P("half_wall", arena, 6, 18, 90, 1.4f, true);

            // Tall walls = lane sightline blockers
            P("wall_tall", arena, -13, 12, 90, 3.4f, true);
            P("wall_tall", arena, 13, 12, 90, 3.4f, true);

            // Detail pipes along the side walls
            P("pipes", arena, -24, 18, 0, 4f, true);
            P("pipes", arena, 24, 18, 0, 4f, true);
        }

        // ---- HD sci-fi arena BETA layout ("Réacteur"): open centre, flank bunkers, X of pillars ----
        static void BuildSciFiBeta(Transform arena)
        {
            // Reactor core pushed to the back so the plant site (0,18) stays walkable
            P("shield_core", arena, 0, 33, 0, 3.4f, true);

            // Two flank bunkers of stacked containers
            P("container_big", arena, -12, 10, 0, 2.6f, false);
            P("container_big", arena, -12, 16, 0, 2.6f, false);
            P("container_big", arena, 12, 10, 0, 2.6f, false);
            P("container_big", arena, 12, 16, 0, 2.6f, false);

            // Storage blocks near the spawns
            P("storage_big", arena, -8, -8, 0, 2.4f, false);
            P("storage_big", arena, 8, -8, 0, 2.4f, false);

            // Scattered crates around the site
            P("crate_hd", arena, -4, 14, 0, 1.5f, false);
            P("crate_hd", arena, 5, 10, 0, 1.5f, false);
            P("crate_hd", arena, -5, 24, 0, 1.5f, false);
            P("crate_hd", arena, 4, 22, 0, 1.5f, false);

            // Pillars in an X around the site
            P("pillar", arena, -8, 8, 0, 4.2f, true);
            P("pillar", arena, 8, 8, 0, 4.2f, true);
            P("pillar", arena, -8, 28, 0, 4.2f, true);
            P("pillar", arena, 8, 28, 0, 4.2f, true);

            // Half-walls ringing the site (chest-high cover)
            P("half_wall", arena, -4, 18, 0, 1.4f, true);
            P("half_wall", arena, 4, 18, 0, 1.4f, true);
            P("half_wall", arena, 0, 22, 90, 1.4f, true);

            // Tall walls splitting the flank lanes
            P("wall_tall", arena, -16, 18, 0, 3.4f, true);
            P("wall_tall", arena, 16, 18, 0, 3.4f, true);

            // Machines / detail
            P("generator", arena, -20, 30, 0, 2.4f, true);
            P("capacitor", arena, 20, 30, 0, 2.4f, true);
            P("pipes", arena, -24, 12, 0, 4f, true);
            P("pipes", arena, 24, 12, 0, 4f, true);
        }

        // Keep the imported HD materials (null = don't override).
        static void P(string key, Transform arena, float x, float z, float yRot, float size, bool byHeight)
        {
            var prefab = GameAssets.Instance.Prop(key);
            if (prefab == null) return;
            ModelUtil.SpawnProp(prefab, arena, new Vector3(x, 0f, z), yRot, size, byHeight, null);
        }

        // ---- Kenney prototype fallback (flat metal-textured) ----
        static void BuildKenney(Transform arena)
        {
            var mat = Surfaces.Metal;
            foreach (var (x, z) in new[] { (-5f, 13f), (5f, 13f), (-5f, 23f), (5f, 23f), (-12f, 18f), (12f, 18f) })
                K("column", arena, x, z, 0f, 3.5f, true, mat);
            foreach (var (x, z) in new[] { (0f, 6f), (0f, 32f), (-18f, 6f), (18f, 6f) })
                K("column-rounded", arena, x, z, 0f, 3.6f, true, mat);
            foreach (var (x, z) in new[] { (-8f, 4f), (8f, 4f), (-4f, -12f), (4f, -12f), (-15f, 22f), (15f, 22f) })
                K("crate", arena, x, z, 0f, 1.4f, false, mat);
            foreach (var (x, z) in new[] { (0f, 0f), (-9f, 10f), (9f, 10f), (-16f, 12f), (16f, 12f) })
                K("crate-color", arena, x, z, 0f, 1.4f, false, mat);
            K("wall-low", arena, 0f, 12f, 0f, 1.2f, true, mat);
            K("wall-low", arena, -5f, 18f, 90f, 1.2f, true, mat);
            K("wall-low", arena, 5f, 18f, 90f, 1.2f, true, mat);
            K("wall-low", arena, -6f, 28f, 0f, 1.2f, true, mat);
            K("wall-low", arena, 6f, 28f, 0f, 1.2f, true, mat);
        }

        static void K(string key, Transform arena, float x, float z, float yRot, float size, bool byHeight, Material mat)
        {
            var prefab = GameAssets.Instance.Prop(key);
            if (prefab == null) return;
            ModelUtil.SpawnProp(prefab, arena, new Vector3(x, 0f, z), yRot, size, byHeight, mat);
        }

        // ---- Kenney prototype BETA layout ----
        static void BuildKenneyBeta(Transform arena)
        {
            var mat = Surfaces.Metal;
            foreach (var (x, z) in new[] { (-8f, 8f), (8f, 8f), (-8f, 28f), (8f, 28f) })
                K("column", arena, x, z, 0f, 3.5f, true, mat);
            foreach (var (x, z) in new[] { (-12f, 13f), (12f, 13f), (0f, 33f) })
                K("column-rounded", arena, x, z, 0f, 3.6f, true, mat);
            foreach (var (x, z) in new[] { (-12f, 10f), (-12f, 16f), (12f, 10f), (12f, 16f), (-4f, 14f), (4f, 22f) })
                K("crate", arena, x, z, 0f, 1.4f, false, mat);
            foreach (var (x, z) in new[] { (5f, 10f), (-5f, 24f), (-8f, -8f), (8f, -8f) })
                K("crate-color", arena, x, z, 0f, 1.4f, false, mat);
            K("wall-low", arena, -4f, 18f, 0f, 1.2f, true, mat);
            K("wall-low", arena, 4f, 18f, 0f, 1.2f, true, mat);
            K("wall-low", arena, 0f, 22f, 90f, 1.2f, true, mat);
            K("wall-low", arena, -16f, 18f, 0f, 1.2f, true, mat);
            K("wall-low", arena, 16f, 18f, 0f, 1.2f, true, mat);
        }
    }
}
