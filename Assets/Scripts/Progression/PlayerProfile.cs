using System;
using UnityEngine;

namespace FirstGame.Progression
{
    /// <summary>
    /// Persistent RPG character: infinite levels, 5 stats, and a build of 3 active + 3 passive skills.
    /// Saved to PlayerPrefs as JSON.
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        public const int PointsPerLevel = 3;

        public int level = 1;
        public int xp = 0;
        public int unspentPoints = 0;

        // ---- Loadout ----
        public string weaponId = "wpn_fusil_rempart";          // primary
        public string secondaryWeaponId = "wpn_pistolet_eclat"; // secondary (switch in-game)
        public string ability0 = "trait_de_feu";   // active skill slots (E / F / C)
        public string ability1 = "decharge_foudre";
        public string ability2 = "bouclier_de_lumiere";
        public string passive0 = "";                // passive skill slots
        public string passive1 = "";
        public string passive2 = "";
        public string equipmentId = "equip_armure_legere";
        public string utilityId = "equip_fumigene";
        public string agentId = "agent_brasier";    // class (passive identity)

        // Ranked (ELO)
        public int elo = 1000;
        public int rankedGames = 0;
        public int rankedWins = 0;

        // ---- Stats (RPG, no cap — invest freely) ----
        public int force = 0;        // +2% weapon & ability damage / pt
        public int endurance = 0;    // +8 max HP / pt (+ out-of-combat regen)
        public int defense = 0;      // damage reduction (soft-capped)
        public int intelligence = 0; // -1.5% ability cooldown / pt + ability power
        public int vitesse = 0;      // +1.2% move speed / pt

        // ---- XP curve (infinite levels; capped growth to avoid overflow) ----
        public int XpForNext => Mathf.Min(250000, Mathf.RoundToInt(80f * Mathf.Pow(1.12f, level - 1) / 10f) * 10);

        public void AddXp(int amount)
        {
            if (amount <= 0) return;
            xp += amount;
            while (xp >= XpForNext)
            {
                xp -= XpForNext;
                level++;
                unspentPoints += PointsPerLevel;
            }
            Save();
        }

        public bool Spend(string stat)
        {
            if (unspentPoints <= 0) return false;
            switch (stat)
            {
                case "force": force++; break;
                case "endurance": endurance++; break;
                case "defense": defense++; break;
                case "intelligence": intelligence++; break;
                case "vitesse": vitesse++; break;
                default: return false;
            }
            unspentPoints--;
            Save();
            return true;
        }

        public int StatValue(string stat) => stat switch
        {
            "force" => force,
            "endurance" => endurance,
            "defense" => defense,
            "intelligence" => intelligence,
            "vitesse" => vitesse,
            _ => 0
        };

        public void Respec()
        {
            unspentPoints += force + endurance + defense + intelligence + vitesse;
            force = endurance = defense = intelligence = vitesse = 0;
            Save();
        }

        // ---- Loadout accessors ----
        public string GetAbility(int slot) => slot switch { 0 => ability0, 1 => ability1, 2 => ability2, _ => ability0 };
        void SetAbilityRaw(int slot, string id) { if (slot == 0) ability0 = id; else if (slot == 1) ability1 = id; else ability2 = id; }

        public void SetAbility(int slot, string id)
        {
            for (int i = 0; i < 3; i++)
                if (i != slot && GetAbility(i) == id) SetAbilityRaw(i, GetAbility(slot));
            SetAbilityRaw(slot, id);
            Save();
        }

        public string GetPassive(int slot) => slot switch { 0 => passive0, 1 => passive1, 2 => passive2, _ => passive0 };
        void SetPassiveRaw(int slot, string id) { if (slot == 0) passive0 = id; else if (slot == 1) passive1 = id; else passive2 = id; }

        public void SetPassive(int slot, string id)
        {
            for (int i = 0; i < 3; i++)
                if (i != slot && GetPassive(i) == id && !string.IsNullOrEmpty(id)) SetPassiveRaw(i, GetPassive(slot));
            SetPassiveRaw(slot, id);
            Save();
        }

        public void SetWeapon(string id) { weaponId = id; Save(); }
        public void SetSecondaryWeapon(string id) { secondaryWeaponId = id; Save(); }
        public void SetEquipment(string id) { equipmentId = id; Save(); }
        public void SetUtility(string id) { utilityId = id; Save(); }
        public void SetAgent(string id) { agentId = id; Save(); }

        public void ApplyAgentPreset(string id)
        {
            var a = FirstGame.Agents.AgentCatalog.ById(id);
            if (a != null) { SetAbilityRaw(0, a.abilityIds[0]); SetAbilityRaw(1, a.abilityIds[1]); SetAbilityRaw(2, a.abilityIds[2]); }
            Save();
        }

        // ---- Derived combat values (names kept stable for the rest of the codebase) ----
        public float DamageMultiplier => 1f + force * 0.02f;
        public float MaxHealth => 100f + endurance * 8f;
        public float RegenPerSecond => endurance * 0.35f;
        public float DamageReduction => (defense * 5f) / (100f + defense * 5f); // soft cap: 50% at 20 pts
        public float CooldownMultiplier => Mathf.Max(0.35f, 1f - intelligence * 0.015f);
        public float AbilityPowerMultiplier => 1f + intelligence * 0.02f + force * 0.01f;
        public float MoveSpeedMultiplier => 1f + vitesse * 0.012f;
        public float SpreadMultiplier => 1f; // spread/recoil now handled per weapon

        // ---- Rank (ELO) ----
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
        const string Key = "firstgame.profile.v2";
        static PlayerProfile _current;
        public static PlayerProfile Current => _current ??= Load();

        public static PlayerProfile Load()
        {
            if (PlayerPrefs.HasKey(Key))
            {
                try { var p = JsonUtility.FromJson<PlayerProfile>(PlayerPrefs.GetString(Key)); if (p != null) return p; }
                catch { /* corrupt -> fresh */ }
            }
            return new PlayerProfile();
        }

        public void Save() { PlayerPrefs.SetString(Key, JsonUtility.ToJson(this)); PlayerPrefs.Save(); }

        public static void ResetProfile() { _current = new PlayerProfile(); _current.Save(); }
    }
}
