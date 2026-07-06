using System;
using UnityEngine;

namespace FirstGame.Progression
{
    /// <summary>
    /// Persistent player character: level, XP and the 6 stats from the GDD.
    /// Design: "progression = options, not raw power" — stats are capped, respec is free.
    /// Saved to PlayerPrefs as JSON.
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        public const int MaxLevel = 30;
        public const int PointsPerLevel = 2;
        public const int StatCap = 20; // max points investable in a single stat

        public int level = 1;
        public int xp = 0;
        public int unspentPoints = 0;

        // Chosen loadout (weapon id + 3 ability ids). Resolved via the catalogs.
        public string weaponId = "wpn_pistolet_eclat";
        public string ability0 = "trait_de_feu";
        public string ability1 = "decharge_foudre";
        public string ability2 = "bouclier_de_lumiere";

        // Invested stat points (see GDD)
        public int vitalite = 0;      // +6 HP / pt
        public int celerite = 0;      // +1.5% move speed / pt (cap +25%)
        public int controle = 0;      // -2.5% weapon spread / pt (recoil system: phase 2)
        public int focalisation = 0;  // -2% ability cooldown / pt (cap -40%)
        public int amplification = 0; // +3% ability power / pt (cap +60%)
        public int regeneration = 0;  // +0.5 HP/s out-of-combat regen / pt

        // ---- XP curve: XP(n->n+1) = round(100 * 1.15^(n-1), nearest 10) ----
        public int XpForNext => level >= MaxLevel ? int.MaxValue
            : Mathf.RoundToInt(100f * Mathf.Pow(1.15f, level - 1) / 10f) * 10;

        public void AddXp(int amount)
        {
            if (amount <= 0) return;
            xp += amount;
            while (level < MaxLevel && xp >= XpForNext)
            {
                xp -= XpForNext;
                level++;
                unspentPoints += PointsPerLevel;
            }
            if (level >= MaxLevel) xp = 0;
            Save();
        }

        public bool Spend(string stat)
        {
            if (unspentPoints <= 0) return false;
            if (StatValue(stat) >= StatCap) return false;
            switch (stat)
            {
                case "vitalite": vitalite++; break;
                case "celerite": celerite++; break;
                case "controle": controle++; break;
                case "focalisation": focalisation++; break;
                case "amplification": amplification++; break;
                case "regeneration": regeneration++; break;
                default: return false;
            }
            unspentPoints--;
            Save();
            return true;
        }

        public int StatValue(string stat) => stat switch
        {
            "vitalite" => vitalite,
            "celerite" => celerite,
            "controle" => controle,
            "focalisation" => focalisation,
            "amplification" => amplification,
            "regeneration" => regeneration,
            _ => 0
        };

        // ---- Loadout ----
        public string GetAbility(int slot) => slot switch { 0 => ability0, 1 => ability1, 2 => ability2, _ => ability0 };

        void SetAbilityRaw(int slot, string id)
        {
            if (slot == 0) ability0 = id;
            else if (slot == 1) ability1 = id;
            else ability2 = id;
        }

        /// <summary>Equip an ability in a slot, swapping it out of any other slot to keep 3 distinct.</summary>
        public void SetAbility(int slot, string id)
        {
            for (int i = 0; i < 3; i++)
                if (i != slot && GetAbility(i) == id) SetAbilityRaw(i, GetAbility(slot));
            SetAbilityRaw(slot, id);
            Save();
        }

        public void SetWeapon(string id) { weaponId = id; Save(); }

        /// <summary>Refund every invested point (free respec between matches).</summary>
        public void Respec()
        {
            unspentPoints += vitalite + celerite + controle + focalisation + amplification + regeneration;
            vitalite = celerite = controle = focalisation = amplification = regeneration = 0;
            Save();
        }

        // ---- Derived combat values ----
        public float MaxHealth => 100f + vitalite * 6f;
        public float MoveSpeedMultiplier => 1f + Mathf.Min(celerite, StatCap) * 0.015f; // cap +25% approx (@~17pts)
        public float SpreadMultiplier => Mathf.Max(0.5f, 1f - controle * 0.025f);
        public float CooldownMultiplier => Mathf.Max(0.6f, 1f - focalisation * 0.02f);     // cap -40%
        public float AbilityPowerMultiplier => 1f + Mathf.Min(amplification, StatCap) * 0.03f; // cap +60%
        public float RegenPerSecond => regeneration * 0.5f;

        public string Rank
        {
            get
            {
                // Purely cosmetic preview until ELO/PvP (phase 2). Based on level for now.
                if (level >= 27) return "Champion";
                if (level >= 22) return "Diamant";
                if (level >= 16) return "Platine";
                if (level >= 10) return "Or";
                if (level >= 5) return "Argent";
                return "Bronze";
            }
        }

        // ---- Persistence ----
        const string Key = "firstgame.profile.v1";
        static PlayerProfile _current;
        public static PlayerProfile Current => _current ??= Load();

        public static PlayerProfile Load()
        {
            if (PlayerPrefs.HasKey(Key))
            {
                try
                {
                    var p = JsonUtility.FromJson<PlayerProfile>(PlayerPrefs.GetString(Key));
                    if (p != null) return p;
                }
                catch { /* corrupt save -> fresh profile */ }
            }
            return new PlayerProfile();
        }

        public void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }

        public static void ResetProfile()
        {
            _current = new PlayerProfile();
            _current.Save();
        }
    }
}
