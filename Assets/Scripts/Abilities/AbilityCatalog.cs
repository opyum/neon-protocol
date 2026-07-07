using System.Collections.Generic;
using UnityEngine;
using FirstGame.Core;
using FirstGame.Progression;

namespace FirstGame.Abilities
{
    /// <summary>How an ability resolves in the current playable slice. Some (Wall/Smoke/Zone)
    /// are approximated with placeholder FX until their full systems land in later phases.</summary>
    public enum AbilityEffect { DamageBurst, Dash, Heal, Shield, Knockback, Wall, Smoke, Zone }

    public class AbilityData
    {
        public string id;
        public string nameFr;
        public string type;        // basique | signature | ultime
        public string element;
        public float cooldown;     // seconds
        public int charges = 1;
        public float damage;
        public string descriptionFr;
        public Color color = Color.cyan;
        public AbilityEffect effect = AbilityEffect.DamageBurst;
    }

    /// <summary>The ~10 abilities. Player equips 3 per match (default loadout used in the tutorial).</summary>
    public static class AbilityCatalog
    {
        public static readonly List<AbilityData> All = new()
        {
            new AbilityData{ id="trait_de_feu",      nameFr="Trait de Feu",       type="basique",   element="Feu",     cooldown=8,   charges=2, damage=45,  effect=AbilityEffect.DamageBurst, color=Hex("#FF5A36"), descriptionFr="Lance un projectile enflammé. 45 dégâts à l'impact + brûlure. Vise la tête pour +30%." },
            new AbilityData{ id="mur_de_glace",      nameFr="Mur de Glace",       type="basique",   element="Glace",   cooldown=14,  charges=1, damage=0,   effect=AbilityEffect.Wall,        color=Hex("#7FD4F0"), descriptionFr="Érige un mur de glace opaque 8s. Bloque la vue et les balles." },
            new AbilityData{ id="decharge_foudre",   nameFr="Décharge Foudre",    type="basique",   element="Foudre",  cooldown=10,  charges=2, damage=30,  effect=AbilityEffect.Dash,        color=Hex("#FFD23F"), descriptionFr="Dash électrique de 6m qui traverse les ennemis. 30 dégâts + ralentit." },
            new AbilityData{ id="nuage_toxique",     nameFr="Nuage Toxique",      type="basique",   element="Poison",  cooldown=12,  charges=1, damage=10,  effect=AbilityEffect.Zone,        color=Hex("#8BE04E"), descriptionFr="Zone toxique 6s. 10 dégâts/s et réduit les soins ennemis de 50%." },
            new AbilityData{ id="rafale_de_vent",    nameFr="Rafale de Vent",     type="basique",   element="Vent",    cooldown=9,   charges=2, damage=0,   effect=AbilityEffect.Knockback,   color=Hex("#B8F0E6"), descriptionFr="Repousse les ennemis de 5m et interrompt leurs canalisations." },
            new AbilityData{ id="bouclier_de_lumiere",nameFr="Bouclier de Lumière",type="signature",element="Lumière", cooldown=20,  charges=1, damage=0,   effect=AbilityEffect.Shield,      color=Hex("#F5D76E"), descriptionFr="Bouclier de 60 PV pendant 5s. Rend 25 PV s'il tient." },
            new AbilityData{ id="voile_d_ombre",     nameFr="Voile d'Ombre",      type="signature", element="Ombre",   cooldown=18,  charges=2, damage=0,   effect=AbilityEffect.Smoke,       color=Hex("#6B4FA0"), descriptionFr="Nuage d'ombre (5m) qui bloque la vision 12s. Placement à distance." },
            new AbilityData{ id="pic_de_terre",      nameFr="Pic de Terre",       type="signature", element="Terre",   cooldown=16,  charges=1, damage=20,  effect=AbilityEffect.DamageBurst, color=Hex("#B07A4B"), descriptionFr="Pics rocheux au sol. 20 dégâts et immobilise 1,5s." },
            new AbilityData{ id="tempete_arcanique", nameFr="Tempête Arcanique",  type="ultime",    element="Arcane",  cooldown=120, charges=1, damage=120, effect=AbilityEffect.DamageBurst, color=Hex("#C24DFF"), descriptionFr="ULTIME. Vortex (7m) 4s qui attire les ennemis et inflige 120 dégâts." },
            new AbilityData{ id="jugement_solaire",  nameFr="Jugement Solaire",   type="ultime",    element="Solaire", cooldown=150, charges=1, damage=150, effect=AbilityEffect.DamageBurst, color=Hex("#FFB01F"), descriptionFr="ULTIME. Rayon solaire vertical. 150 dégâts et aveugle 2s." },
        };

        static Color Hex(string h) => ArtPalette.Hex(h);

        /// <summary>Default 3-ability loadout for the tutorial: a damage ability on E, a dash, and a heal/shield.</summary>
        public static AbilityData[] DefaultLoadout => new[]
        {
            ById("trait_de_feu"),
            ById("decharge_foudre"),
            ById("bouclier_de_lumiere"),
        };

        public static AbilityData ById(string id) => All.Find(a => a.id == id);

        /// <summary>Resolve the player's 3 freely-chosen spells (falls back to defaults).</summary>
        public static AbilityData[] ResolveLoadout(PlayerProfile p)
        {
            var def = DefaultLoadout;
            return new[]
            {
                ById(p.GetAbility(0)) ?? def[0],
                ById(p.GetAbility(1)) ?? def[1],
                ById(p.GetAbility(2)) ?? def[2],
            };
        }
    }
}
