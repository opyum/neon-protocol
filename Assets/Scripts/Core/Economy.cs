namespace FirstGame.Core
{
    /// <summary>Buy-phase economy: per-mission credit budget and item prices (Valorant-style).</summary>
    public static class Economy
    {
        public const int StartBudget = 4500;

        public static readonly (string id, int price)[] Weapons =
        {
            ("wpn_pistolet_eclat",   0),
            ("wpn_smg_guepe",        1000),
            ("wpn_pompe_broyeur",    1200),
            ("wpn_fusil_rempart",    2400),
            ("wpn_sniper_longuevue", 3800),
        };

        public static readonly (string id, int price)[] Armors =
        {
            ("equip_armure_legere", 400),
            ("equip_regulateur",    700),
            ("equip_armure_lourde", 1000),
        };

        public static int WeaponPrice(string id)
        {
            foreach (var w in Weapons) if (w.id == id) return w.price;
            return 0;
        }

        public static int ArmorPrice(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;
            foreach (var a in Armors) if (a.id == id) return a.price;
            return 0;
        }

        public static float ArmorShield(string id) => id switch
        {
            "equip_armure_legere" => 25f,
            "equip_regulateur"    => 40f,
            "equip_armure_lourde" => 75f,
            _ => 0f,
        };
    }
}
