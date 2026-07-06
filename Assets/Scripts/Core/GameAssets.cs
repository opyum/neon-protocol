using System;
using UnityEngine;

namespace FirstGame.Core
{
    [Serializable]
    public class WeaponVisual
    {
        public string weaponId;   // ex: "wpn_pistolet_eclat" (voir WeaponCatalog)
        public GameObject prefab; // modèle d'arme (Kenney/Quaternius)
    }

    [Serializable]
    public class NamedProp
    {
        public string key;        // ex: "crate", "column", "wall" (voir Prototype Kit)
        public GameObject prefab;
    }

    /// <summary>
    /// Config d'assets importés. Glisse tes prefabs ici (Mixamo, Kenney...).
    /// Tout est optionnel : si un slot est vide, le jeu retombe sur les primitives.
    /// Le fichier vit dans Assets/Resources/GameAssets.asset (chargé par code).
    /// </summary>
    [CreateAssetMenu(fileName = "GameAssets", menuName = "NEON/Game Assets")]
    public class GameAssets : ScriptableObject
    {
        [Header("Personnages (Mixamo — modèle riggé + Animator)")]
        public GameObject enemyCharacterPrefab;      // ennemis + mannequins
        public GameObject playerCharacterPrefab;     // optionnel (peu visible en FPS)
        public Vector3 characterScale = Vector3.one; // Mixamo importe parfois à une autre échelle

        [Header("Armes — viewmodel 1re personne (Kenney/Quaternius)")]
        public WeaponVisual[] weaponViewmodels;

        [Header("Décor (optionnel)")]
        public GameObject coverPrefab;

        [Header("Décor modulaire (Prototype Kit)")]
        public NamedProp[] props;

        public GameObject Prop(string key)
        {
            if (props == null) return null;
            foreach (var p in props)
                if (p != null && p.key == key && p.prefab != null) return p.prefab;
            return null;
        }

        static GameAssets _instance;
        static bool _loaded;
        public static GameAssets Instance
        {
            get
            {
                if (!_loaded) { _instance = Resources.Load<GameAssets>("GameAssets"); _loaded = true; }
                return _instance;
            }
        }

        public GameObject WeaponFor(string id)
        {
            if (weaponViewmodels == null) return null;
            foreach (var w in weaponViewmodels)
                if (w != null && w.weaponId == id && w.prefab != null) return w.prefab;
            return null;
        }

        public Vector3 SafeScale => characterScale == Vector3.zero ? Vector3.one : characterScale;
    }
}
