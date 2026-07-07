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
        public string weaponId = "wpn_fusil_rempart";          // primary
        public string secondaryWeaponId = "wpn_pistolet_eclat"; // secondary (switch in-game)
        public string ability0 = "trait_de_feu";
        public string ability1 = "decharge_foudre";
        public string ability2 = "bouclier_de_lumiere";
        public string equipmentId = "equip_armure_legere"; // armour slot
        public string utilityId = "equip_fumigene";        // utility slot (key G)
        public string agentId = "agent_brasier";           // chosen agent (fixes the 3 abilities)

        // Ranked (ELO) — updated by the "Classé contre bots" mode, reused later by PvP.
        public int elo = 1000;
        public int rankedGames = 0;
        public int rankedWins = 0;

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
        public void SetSecondaryWeapon(string id) { secondaryWeaponId = id; Save(); }
        public void SetEquipment(string id) { equipmentId = id; Save(); }
        public void SetUtility(string id) { utilityId = id; Save(); }

        /// <summary>Pick a class/character — gives its passive only. Spells are chosen freely
        /// (SetAbility), so the character no longer locks the 3 abilities.</summary>
        public void SetAgent(string id) { agentId = id; Save(); }

        /// <summary>Fill the 3 spell slots from an agent's suggested loadout (a preset button).</summary>
        public void ApplyAgentPreset(string id)
        {
            var a = FirstGame.Agents.AgentCatalog.ById(id);
            if (a != null) { SetAbilityRaw(0, a.abilityIds[0]); SetAbilityRaw(1, a.abilityIds[1]); SetAbilityRaw(2, a.abilityIds[2]); }
            Save();
        }

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

        // Ranked tier from ELO (FR names). This is the real competitive rank.
        public string Rank => RankedTier;

        public string RankedTier
        {
            get
            {
                if (elo >= 2500) return "Champion";
                if (elo >= 2300) return "Maître";
                if (elo >= 2050) return "Diamant";
                if (elo >= 1800) return "Émeraude";
                if (elo >= 1550) return "Platine";
                if (elo >= 1300) return "Or";
                if (elo >= 1050) return "Argent";
                if (elo >= 800) return "Bronze";
                return "Fer";
            }
        }

        /// <summary>ELO update. K=40 during placement (10 games), then 24, then 16 at 2100+.
        /// Returns the point delta (signed).</summary>
        public int ApplyMatchResult(bool win, int opponentRating)
        {
            int k = rankedGames < 10 ? 40 : (elo < 2100 ? 24 : 16);
            float s = win ? 1f : 0f;
            float e = 1f / (1f + Mathf.Pow(10f, (opponentRating - elo) / 400f));
            int delta = Mathf.RoundToInt(k * (s - e));
            elo = Mathf.Max(0, elo + delta);
            rankedGames++;
            if (win) rankedWins++;
            Save();
            return delta;
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
