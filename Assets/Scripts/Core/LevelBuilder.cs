using UnityEngine;

namespace FirstGame.Core
{
    /// <summary>Builds a real modular level (Kenney Prototype Kit props) for the combat arena.
    /// Returns false if the props aren't available, so the caller can fall back to primitives.</summary>
    public static class LevelBuilder
    {
        public static bool BuildCombatCover(Transform arena)
        {
            var ga = GameAssets.Instance;
            if (ga == null || ga.Prop("crate") == null) return false;

            var wallMat = ArtPalette.MakeMaterial(ArtPalette.Wall, 0f, 0.2f);
            var coverMat = ArtPalette.MakeMaterial(ArtPalette.Cover, 0f, 0.25f);
            var metalMat = ArtPalette.MakeMaterial(ArtPalette.Metal, 0.6f, 0.5f);

            // Columns — site corners, side pillars
            foreach (var (x, z) in new[] { (-5f, 13f), (5f, 13f), (-5f, 23f), (5f, 23f), (-12f, 18f), (12f, 18f) })
                Prop("column", arena, x, z, 0f, 3.5f, true, wallMat);
            // Rounded pillars — mid & far sightline breakers
            foreach (var (x, z) in new[] { (0f, 6f), (0f, 32f) })
                Prop("column-rounded", arena, x, z, 0f, 3.6f, true, metalMat);

            // Crates — cover
            foreach (var (x, z) in new[] { (-8f, 4f), (8f, 4f), (-4f, -12f), (4f, -12f), (-15f, 22f), (15f, 22f) })
                Prop("crate", arena, x, z, 0f, 1.4f, false, coverMat);
            foreach (var (x, z) in new[] { (0f, 0f), (-9f, 10f), (9f, 10f) })
                Prop("crate-color", arena, x, z, 0f, 1.4f, false, coverMat);

            // Low walls — chest-high cover around the site
            Prop("wall-low", arena, 0f, 12f, 0f, 1.2f, true, coverMat);
            Prop("wall-low", arena, -5f, 18f, 90f, 1.2f, true, coverMat);
            Prop("wall-low", arena, 5f, 18f, 90f, 1.2f, true, coverMat);
            Prop("wall-low", arena, -6f, 28f, 0f, 1.2f, true, coverMat);
            Prop("wall-low", arena, 6f, 28f, 0f, 1.2f, true, coverMat);

            // Tall walls — lane sightline blockers
            Prop("wall", arena, -13f, 12f, 90f, 3f, true, wallMat);
            Prop("wall", arena, 13f, 12f, 90f, 3f, true, wallMat);

            return true;
        }

        static void Prop(string key, Transform arena, float x, float z, float yRot, float size, bool byHeight, Material mat)
        {
            var prefab = GameAssets.Instance.Prop(key);
            if (prefab == null) return;
            ModelUtil.SpawnProp(prefab, arena, new Vector3(x, 0f, z), yRot, size, byHeight, mat);
        }
    }
}
