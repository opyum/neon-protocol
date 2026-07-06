using System.Collections.Generic;

namespace FirstGame.Combat
{
    public class WeaponData
    {
        public string id;
        public string nameFr;
        public string category;
        public float damage;       // body damage per shot
        public float fireRate;     // shots per second
        public int magazineSize;
        public float reloadSeconds;
        public float range;        // metres
        public bool isHitscan = true;
        public float headshotMultiplier = 2f;
    }

    /// <summary>Starting arsenal (balanced Valorant-style values from the combat design pass).</summary>
    public static class WeaponCatalog
    {
        public static readonly List<WeaponData> Weapons = new()
        {
            new WeaponData { id="wpn_pistolet_eclat", nameFr="Éclat",      category="Pistolet",             damage=26,  fireRate=6.5f, magazineSize=13, reloadSeconds=1.75f, range=40, isHitscan=true },
            new WeaponData { id="wpn_smg_guepe",      nameFr="Guêpe",      category="Mitraillette",         damage=23,  fireRate=13f,  magazineSize=30, reloadSeconds=2.2f,  range=22, isHitscan=true },
            new WeaponData { id="wpn_fusil_rempart",  nameFr="Rempart",    category="Fusil d'assaut",       damage=39,  fireRate=9.5f, magazineSize=25, reloadSeconds=2.5f,  range=50, isHitscan=true },
            new WeaponData { id="wpn_pompe_broyeur",  nameFr="Broyeur",    category="Fusil à pompe",        damage=66,  fireRate=1.1f, magazineSize=5,  reloadSeconds=2.6f,  range=9,  isHitscan=true },
            new WeaponData { id="wpn_sniper_longuevue",nameFr="Longue-Vue",category="Fusil de précision",   damage=120, fireRate=0.7f, magazineSize=5,  reloadSeconds=3.7f,  range=65, isHitscan=true },
        };

        public static WeaponData Default => Weapons[0];
        public static WeaponData ById(string id) => Weapons.Find(w => w.id == id) ?? Default;
    }
}
