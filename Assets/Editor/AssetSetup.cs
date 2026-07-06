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

        ga.enemyCharacterPrefab = LoadModel("Assets/Art/Characters/character-a.fbx");
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

        EditorUtility.SetDirty(ga);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[AssetSetup] GameAssets: perso={(ga.enemyCharacterPrefab != null)}, armes={list.Count}");
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
