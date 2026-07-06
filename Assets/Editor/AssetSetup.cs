using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using FirstGame.Core;

/// <summary>
/// Creates Assets/Resources/GameAssets.asset and assigns the imported Kenney models
/// (character + weapons). Run from menu "NEON ▸ Configurer les assets" or batch
/// (-executeMethod AssetSetup.Run). Materials are overridden at runtime by ArtPalette.
/// </summary>
public static class AssetSetup
{
    const string ResDir = "Assets/Resources";
    const string AssetPath = ResDir + "/GameAssets.asset";

    [MenuItem("NEON/Configurer les assets")]
    public static void Run()
    {
        if (!AssetDatabase.IsValidFolder(ResDir))
            AssetDatabase.CreateFolder("Assets", "Resources");

        var ga = AssetDatabase.LoadAssetAtPath<GameAssets>(AssetPath);
        if (ga == null)
        {
            ga = ScriptableObject.CreateInstance<GameAssets>();
            AssetDatabase.CreateAsset(ga, AssetPath);
        }

        ga.enemyCharacterPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Mixamo/EnemyMixamo.prefab")   // animé (AnimSetup)
            ?? LoadModel("Assets/Synty/SidekickCharacters/Characters/HumanSpecies/HumanSpecies_01/HumanSpecies_01.prefab")
            ?? LoadModel("Assets/Art/Characters/character-a.fbx");
        ga.characterScale = Vector3.one;

        var map = new (string id, string file)[]
        {
            ("wpn_pistolet_eclat",  "blaster-a"),
            ("wpn_smg_guepe",       "blaster-b"),
            ("wpn_fusil_rempart",   "blaster-c"),
            ("wpn_pompe_broyeur",   "blaster-d"),
            ("wpn_sniper_longuevue","blaster-e"),
        };
        var list = new List<WeaponVisual>();
        foreach (var m in map)
        {
            var prefab = LoadModel($"Assets/Art/Weapons/{m.file}.fbx");
            if (prefab != null) list.Add(new WeaponVisual { weaponId = m.id, prefab = prefab });
        }
        ga.weaponViewmodels = list.ToArray();

        var propKeys = new[] { "crate", "crate-color", "column", "column-rounded", "column-low", "wall", "wall-low", "wall-corner", "stairs-small", "floor-thick" };
        var props = new List<NamedProp>();
        foreach (var k in propKeys)
        {
            var prefab = LoadModel($"Assets/Art/Environment/{k}.fbx");
            if (prefab != null) props.Add(new NamedProp { key = k, prefab = prefab });
        }
        // Sci-Fi pack prefabs (URP-converted) — real HD cover/structures for the combat arena.
        var packProps = new (string key, string path)[]
        {
            ("container_big",   "Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/container_big.prefab"),
            ("container_small", "Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/container_small.prefab"),
            ("storage_big",     "Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/storage_container_big.prefab"),
            ("generator",       "Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/generator.prefab"),
            ("capacitor",       "Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/Capacitor.prefab"),
            ("shield_core",     "Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/Shield Core.prefab"),
            ("battery",         "Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/Battery_big.prefab"),
            ("half_wall",       "Assets/Sci-Fi Styled Modular Pack/Prefabs/Walls/Half walls/decorative_half_wall_1_LOD.prefab"),
            ("wall_tall",       "Assets/Sci-Fi Styled Modular Pack/Prefabs/Walls/decorative_wall_1.prefab"),
            ("crate_hd",        "Assets/Creepy_Cat/3D Scifi Kit Starter Kit_HD/Prefabs/Props/Crate_01.prefab"),
            ("pillar",          "Assets/Creepy_Cat/3D Scifi Kit Starter Kit_HD/Prefabs/Walls/Column_01_Big.prefab"),
            ("pipes",           "Assets/Creepy_Cat/3D Scifi Kit Starter Kit_HD/Prefabs/Stuff/Pipes_01.prefab"),
        };
        foreach (var (key, path) in packProps)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) props.Add(new NamedProp { key = key, prefab = prefab });
        }
        ga.props = props.ToArray();

        // Mark the normal maps so Unity decodes them correctly.
        SetNormalMap("Assets/Resources/Textures/concrete_normal.jpg");
        SetNormalMap("Assets/Resources/Textures/metal_normal.jpg");

        EditorUtility.SetDirty(ga);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[AssetSetup] GameAssets: perso={(ga.enemyCharacterPrefab != null)}, armes={list.Count}");
    }

    static void SetNormalMap(string path)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti != null && ti.textureType != TextureImporterType.NormalMap)
        {
            ti.textureType = TextureImporterType.NormalMap;
            ti.SaveAndReimport();
        }
    }

    static GameObject LoadModel(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer != null)
        {
            importer.globalScale = 1f;
            importer.materialImportMode = ModelImporterMaterialImportMode.None; // colours applied at runtime
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }
}
