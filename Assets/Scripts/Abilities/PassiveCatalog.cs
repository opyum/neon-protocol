using System;
using System.Collections.Generic;
using UnityEngine;
using FirstGame.Core;

namespace FirstGame.Abilities
{
    /// <summary>A passive skill = a set of additive combat modifiers. Applied by PassiveSystem.</summary>
    public class PassiveData
    {
        public string id, nameFr, descriptionFr;
        public Color color = Color.gray;
        public float damagePct;          // +% weapon & ability damage
        public float healthFlat;         // +max HP
        public float speedPct;           // +% move speed
        public float cooldownPct;        // -% ability cooldown
        public float lifestealPct;       // % of weapon damage healed
        public float regenFlat;          // +HP/s out of combat
        public float reloadPct;          // -% reload time
        public float damageReductionPct; // +% incoming damage reduction
    }

    /// <summary>100 passive skills (20 families × 5 ranks). Pick 3 for your build.</summary>
    public static class PassiveCatalog
    {
        public static readonly List<PassiveData> All = new();
        static readonly string[] Roman = { "I", "II", "III", "IV", "V" };

        static PassiveCatalog() => Generate();

        static void Generate()
        {
            var fams = new (string name, string col, string desc, int per, Action<PassiveData, int> mod)[]
            {
                ("Force",         "#FF5A36", "+{0}% dégâts (armes & sorts)",        3,  (p,t)=>p.damagePct=t*3),
                ("Brutalité",     "#FF2D4E", "+{0}% dégâts",                        5,  (p,t)=>p.damagePct=t*5),
                ("Vitalité",      "#3DDC84", "+{0} PV max",                         15, (p,t)=>p.healthFlat=t*15),
                ("Colosse",       "#2FA060", "+{0} PV max",                         30, (p,t)=>p.healthFlat=t*30),
                ("Célérité",      "#22E0C8", "+{0}% vitesse de déplacement",        2,  (p,t)=>p.speedPct=t*2),
                ("Fulgurance",    "#00F0FF", "+{0}% vitesse de déplacement",        4,  (p,t)=>p.speedPct=t*4),
                ("Focalisation",  "#C24DFF", "-{0}% recharge des sorts",            3,  (p,t)=>p.cooldownPct=t*3),
                ("Méditation",    "#9B4DFF", "-{0}% recharge des sorts",            5,  (p,t)=>p.cooldownPct=t*5),
                ("Vampirisme",    "#C0304E", "{0}% vol de vie sur les tirs",        2,  (p,t)=>p.lifestealPct=t*2),
                ("Sangsue",       "#8A1E38", "{0}% vol de vie sur les tirs",        4,  (p,t)=>p.lifestealPct=t*4),
                ("Régénération",  "#8BE04E", "+{0} PV/s hors combat",               1,  (p,t)=>p.regenFlat=t),
                ("Convalescence", "#6FBF3A", "+{0} PV/s hors combat",               2,  (p,t)=>p.regenFlat=t*2),
                ("Barillet",      "#FFD23F", "-{0}% temps de rechargement",         6,  (p,t)=>p.reloadPct=t*6),
                ("Munitionnaire", "#FFB01F", "-{0}% temps de rechargement",         10, (p,t)=>p.reloadPct=t*10),
                ("Cuirasse",      "#7FA0C0", "+{0}% réduction de dégâts",           3,  (p,t)=>p.damageReductionPct=t*3),
                ("Forteresse",    "#4A6B8C", "+{0}% réduction de dégâts",           5,  (p,t)=>p.damageReductionPct=t*5),
                ("Berserker",     "#FF6A1A", "+{0}% dégâts & +{0}% vitesse",        4,  (p,t)=>{p.damagePct=t*4;p.speedPct=t*4;}),
                ("Sentinelle",    "#5AA0B0", "+{0} PV & +réduction",                12, (p,t)=>{p.healthFlat=t*12;p.damageReductionPct=t*2;}),
                ("Arcaniste",     "#B060FF", "-{0}% recharge & +dégâts sorts",      3,  (p,t)=>{p.cooldownPct=t*3;p.damagePct=t*2;}),
                ("Prédateur",     "#D04060", "+{0}% vol de vie & vitesse",          2,  (p,t)=>{p.lifestealPct=t*2;p.speedPct=t*2;}),
            };

            for (int fi = 0; fi < fams.Length; fi++)
            {
                var f = fams[fi];
                for (int t = 1; t <= 5; t++)
                {
                    var pd = new PassiveData { id = $"pas_{fi}_{t}", nameFr = $"{f.name} {Roman[t - 1]}", color = ArtPalette.Hex(f.col) };
                    f.mod(pd, t);
                    pd.descriptionFr = string.Format(f.desc, f.per * t);
                    All.Add(pd);
                }
            }
        }

        public static PassiveData ById(string id) => string.IsNullOrEmpty(id) ? null : All.Find(p => p.id == id);
    }
}
