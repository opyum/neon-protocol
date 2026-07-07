using UnityEngine;
using FirstGame.Enemies;

namespace FirstGame.Core
{
    /// <summary>
    /// Per-match / preference settings shared between the mission picker and the runtime:
    /// which arena layout to build, and the bot difficulty. Both are remembered in PlayerPrefs.
    /// Difficulty is pushed into EnemyBot's global modifiers via <see cref="ApplyDifficulty"/>.
    /// </summary>
    public static class MatchConfig
    {
        // ---------- Arena layout (remembered) ----------
        public static readonly string[] ArenaNames = { "ALPHA — Entrepôt", "BETA — Réacteur" };

        public static int ArenaLayout
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt("match.arena", 0), 0, ArenaNames.Length - 1);
            set { PlayerPrefs.SetInt("match.arena", Mathf.Clamp(value, 0, ArenaNames.Length - 1)); PlayerPrefs.Save(); }
        }

        public static string ArenaName => ArenaNames[ArenaLayout];

        // ---------- Procedural maps (10 seeded labyrinth layouts PER game mode) ----------
        public const int MapCount = 10;

        /// <summary>Selected game mode (0-based, matches the picker order). Drives a distinct map set.</summary>
        public static int ModeId = 0;

        public static int MapIndex
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt("match.map", 0), 0, MapCount - 1);
            set { PlayerPrefs.SetInt("match.map", ((value % MapCount) + MapCount) % MapCount); PlayerPrefs.Save(); }
        }

        /// <summary>Seed unique per (mode, map) so every mode has its own 10 distinct maps.</summary>
        public static int MapSeed => ModeId * 101 + MapIndex + 1;

        public static string MapName => $"CARTE {MapIndex + 1} / {MapCount}";

        // ---------- Difficulty (remembered preference) ----------
        public static readonly string[] DifficultyNames = { "FACILE", "NORMAL", "DIFFICILE", "CAUCHEMAR" };

        public static int Difficulty
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt("match.diff", 1), 0, DifficultyNames.Length - 1);
            set { PlayerPrefs.SetInt("match.diff", Mathf.Clamp(value, 0, DifficultyNames.Length - 1)); PlayerPrefs.Save(); }
        }

        public static string DifficultyName => DifficultyNames[Difficulty];

        // Per-difficulty bot modifiers: (accuracy, damage, fireInterval, reactionDelay).
        // fireInterval/reaction < 1 = faster & sharper bots; accuracy/damage > 1 = deadlier.
        static readonly (float acc, float dmg, float fire, float react)[] Mods =
        {
            (0.65f, 0.80f, 1.30f, 1.60f), // FACILE
            (1.00f, 1.00f, 1.00f, 1.00f), // NORMAL (reference stats from EnemyBot.TierCfg)
            (1.20f, 1.15f, 0.80f, 0.55f), // DIFFICILE
            (1.40f, 1.30f, 0.65f, 0.30f), // CAUCHEMAR
        };

        /// <summary>Pushes the current difficulty into EnemyBot's global modifiers. Call before spawning bots.</summary>
        public static void ApplyDifficulty()
        {
            var m = Mods[Difficulty];
            EnemyBot.AccuracyScale = m.acc;
            EnemyBot.DamageScale = m.dmg;
            EnemyBot.FireIntervalScale = m.fire;
            EnemyBot.ReactionScale = m.react;
        }

        /// <summary>Restores neutral (1x) bot modifiers — used by ranked so ELO stays fair.</summary>
        public static void ResetBotModifiers()
        {
            EnemyBot.AccuracyScale = 1f;
            EnemyBot.DamageScale = 1f;
            EnemyBot.FireIntervalScale = 1f;
            EnemyBot.ReactionScale = 1f;
        }
    }
}
