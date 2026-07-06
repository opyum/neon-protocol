using System.Collections.Generic;
using UnityEngine;
using FirstGame.Core;

namespace FirstGame.Agents
{
    /// <summary>A playable agent: a fixed 3-ability loadout, a role, a passive and a signature colour.</summary>
    public class AgentData
    {
        public string id;
        public string nameFr;
        public string role;        // Duelliste | Contrôleur | Initiateur | Sentinelle
        public string theme;
        public string passiveFr;
        public string statBias;    // recommended stat (id)
        public Color color;
        public string[] abilityIds; // exactly 3 (E / F / C)
    }

    public static class AgentCatalog
    {
        public static readonly List<AgentData> All = new()
        {
            new AgentData {
                id = "agent_brasier", nameFr = "Brasier", role = "Duelliste",
                theme = "Feu — engagement agressif, mobilité et burst à courte/moyenne portée.",
                abilityIds = new[] { "trait_de_feu", "decharge_foudre", "tempete_arcanique" },
                passiveFr = "Élan Ardent : après chaque élimination, recharge 1 charge de Décharge Foudre et +12% de célérité pendant 3s.",
                statBias = "celerite", color = ArtPalette.Hex("#FF5A36"),
            },
            new AgentData {
                id = "agent_nocturne", nameFr = "Nocturne", role = "Contrôleur",
                theme = "Ombre/Glace — dénie l'espace, coupe les lignes de vue et bloque les angles.",
                abilityIds = new[] { "voile_d_ombre", "mur_de_glace", "nuage_toxique" },
                passiveFr = "Emprise : toutes ses zones/écrans durent 25% plus longtemps. Kit 100% tactique.",
                statBias = "focalisation", color = ArtPalette.Hex("#6B4FA0"),
            },
            new AgentData {
                id = "agent_faille", nameFr = "Faille", role = "Initiateur",
                theme = "Vent/Terre — ouvre les sites, immobilise et désorganise pour que l'équipe suive.",
                abilityIds = new[] { "pic_de_terre", "rafale_de_vent", "jugement_solaire" },
                passiveFr = "Onde de Choc : les ennemis touchés par Pic de Terre ou Rafale de Vent sont révélés 2s.",
                statBias = "controle", color = ArtPalette.Hex("#46C0A0"),
            },
            new AgentData {
                id = "agent_rempart", nameFr = "Rempart", role = "Sentinelle",
                theme = "Lumière/Terre — ancre un site, tient un angle et pose des pièges de zone.",
                abilityIds = new[] { "bouclier_de_lumiere", "mur_de_glace", "pic_de_terre" },
                passiveFr = "Garde : régénère 8 PV/s hors combat et le Bouclier de Lumière rend 40 PV s'il tient.",
                statBias = "vitalite", color = ArtPalette.Hex("#F5D76E"),
            },
        };

        public static AgentData ById(string id) => All.Find(a => a.id == id);
        public static AgentData Default => ById("agent_brasier");
    }
}
