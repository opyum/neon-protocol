using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FirstGame.Core;

namespace FirstGame.Equipment
{
    public class EquipmentData
    {
        public string id;
        public string nameFr;
        public string slot;      // "armure" | "utilitaire"
        public string effectFr;
        public Color color = Color.cyan;
    }

    public static class EquipmentCatalog
    {
        public static readonly List<EquipmentData> All = new()
        {
            new EquipmentData { id="equip_armure_legere",       nameFr="Armure Légère",           slot="armure",     effectFr="+25 bouclier, +8% vitesse",              color=ArtPalette.Hex("#7FD4F0") },
            new EquipmentData { id="equip_armure_lourde",       nameFr="Armure Lourde",           slot="armure",     effectFr="+75 bouclier, -8% vitesse",              color=ArtPalette.Hex("#B07A4B") },
            new EquipmentData { id="equip_regulateur",          nameFr="Régulateur de Boucliers", slot="armure",     effectFr="+40 bouclier + 12/s régén hors combat",  color=ArtPalette.Hex("#8BE04E") },
            new EquipmentData { id="equip_fumigene",            nameFr="Fumigène",                slot="utilitaire", effectFr="Fumée 4m / 10s (portée 15m). 2 ch",      color=ArtPalette.Hex("#B8C0C8") },
            new EquipmentData { id="equip_grenade_aveuglante",  nameFr="Grenade Aveuglante",      slot="utilitaire", effectFr="Flash 8m / 2,5s. 1 ch",                  color=ArtPalette.Hex("#F5F5F5") },
            new EquipmentData { id="equip_drone",               nameFr="Drone de Reconnaissance", slot="utilitaire", effectFr="Marque les ennemis (20m) 8s. 1 ch",      color=ArtPalette.Hex("#FFD23F") },
        };

        public static EquipmentData ById(string id) => All.Find(e => e.id == id) ?? All[0];
        public static List<EquipmentData> BySlot(string slot) => All.Where(e => e.slot == slot).ToList();
    }
}
