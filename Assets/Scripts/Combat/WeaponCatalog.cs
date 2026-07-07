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
        public float aoeRadius = 0f;    // explosive splash (rocket)
        public float aoeDamage = 0f;    // splash damage in the radius
        public bool deployable = false; // heavy: wild recoil moving, accurate when stationary
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
            new WeaponData { id="wpn_smg_rafale",      nameFr="Frelon",     category="Mitraillette",         damage=20,  fireRate=15f,  magazineSize=35, reloadSeconds=2.0f,  range=24 },
            new WeaponData { id="wpn_fusil_vanguard",  nameFr="Vanguard",   category="Fusil d'assaut",       damage=34,  fireRate=11f,  magazineSize=30, reloadSeconds=2.4f,  range=48 },
            new WeaponData { id="wpn_dmr_traqueur",    nameFr="Traqueur",   category="Fusil de désignation", damage=55,  fireRate=3.5f, magazineSize=12, reloadSeconds=2.6f,  range=60 },
            new WeaponData { id="wpn_sniper_perceur",  nameFr="Perceur",    category="Fusil de précision",   damage=150, fireRate=0.5f, magazineSize=4,  reloadSeconds=3.9f,  range=80 },
            new WeaponData { id="wpn_mitrailleuse",    nameFr="Faucheuse",  category="Mitrailleuse lourde",  damage=28,  fireRate=9f,   magazineSize=80, reloadSeconds=4.5f,  range=55, deployable=true },
            new WeaponData { id="wpn_lance_flammes",   nameFr="Fournaise",  category="Lance-flammes",        damage=9,   fireRate=14f,  magazineSize=60, reloadSeconds=3.0f,  range=9,  headshotMultiplier=1f },
            new WeaponData { id="wpn_lance_roquette",  nameFr="Fléau",      category="Lance-roquettes",      damage=30,  fireRate=0.8f, magazineSize=1,  reloadSeconds=3.2f,  range=80, aoeRadius=4.5f, aoeDamage=90f },
        };

        public static WeaponData Default => Weapons[0];
        public static WeaponData ById(string id) => Weapons.Find(w => w.id == id) ?? Default;
    }
}
