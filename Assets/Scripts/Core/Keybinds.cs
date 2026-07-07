using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstGame.Core
{
    /// <summary>Every remappable gameplay action.</summary>
    public enum GameAction
    {
        Forward, Back, Left, Right, Jump, Sprint,
        Fire, Aim, Reload,
        AbilityE, AbilityF, AbilityC, Utility
    }

    /// <summary>
    /// Rebindable key map, persisted in PlayerPrefs. ALL gameplay input reads through this so
    /// remaps take effect immediately and survive restarts. Defaults are AZERTY (ZQSD) to match
    /// the project's French UI; arrow keys stay hard-wired as a movement fallback.
    /// </summary>
    public static class Keybinds
    {
        public static readonly GameAction[] All = (GameAction[])Enum.GetValues(typeof(GameAction));

        static readonly Dictionary<GameAction, KeyCode> Defaults = new()
        {
            { GameAction.Forward,  KeyCode.Z },
            { GameAction.Back,     KeyCode.S },
            { GameAction.Left,     KeyCode.Q },
            { GameAction.Right,    KeyCode.D },
            { GameAction.Jump,     KeyCode.Space },
            { GameAction.Sprint,   KeyCode.LeftShift },
            { GameAction.Fire,     KeyCode.Mouse0 },
            { GameAction.Aim,      KeyCode.Mouse1 },
            { GameAction.Reload,   KeyCode.R },
            { GameAction.AbilityE, KeyCode.E },
            { GameAction.AbilityF, KeyCode.F },
            { GameAction.AbilityC, KeyCode.C },
            { GameAction.Utility,  KeyCode.G },
        };

        static readonly Dictionary<GameAction, string> FrLabels = new()
        {
            { GameAction.Forward,  "AVANCER" },
            { GameAction.Back,     "RECULER" },
            { GameAction.Left,     "PAS À GAUCHE" },
            { GameAction.Right,    "PAS À DROITE" },
            { GameAction.Jump,     "SAUTER" },
            { GameAction.Sprint,   "COURIR" },
            { GameAction.Fire,     "TIRER" },
            { GameAction.Aim,      "VISER (ADS)" },
            { GameAction.Reload,   "RECHARGER" },
            { GameAction.AbilityE, "SORT 1" },
            { GameAction.AbilityF, "SORT 2" },
            { GameAction.AbilityC, "SORT 3" },
            { GameAction.Utility,  "UTILITAIRE" },
        };

        static Dictionary<GameAction, KeyCode> _cache;
        static Dictionary<GameAction, KeyCode> Cache
        {
            get
            {
                if (_cache == null)
                {
                    _cache = new Dictionary<GameAction, KeyCode>();
                    foreach (var a in All)
                        _cache[a] = (KeyCode)PlayerPrefs.GetInt(PrefKey(a), (int)Defaults[a]);
                }
                return _cache;
            }
        }

        static string PrefKey(GameAction a) => "key." + a;

        public static KeyCode Get(GameAction a) => Cache[a];
        public static bool Held(GameAction a) => Input.GetKey(Cache[a]);
        public static bool Pressed(GameAction a) => Input.GetKeyDown(Cache[a]);
        public static string ActionLabel(GameAction a) => FrLabels.TryGetValue(a, out var s) ? s : a.ToString();

        /// <summary>
        /// Rebinds an action. If <paramref name="k"/> is already used by another action, the two
        /// swap keys — guaranteeing no action is ever left unbound and no two actions silently
        /// share a key (acceptance: "pas de conflit silencieux").
        /// </summary>
        public static void Set(GameAction a, KeyCode k)
        {
            var old = Cache[a];
            foreach (var other in All)
                if (other != a && Cache[other] == k) Store(other, old);
            Store(a, k);
            PlayerPrefs.Save();
        }

        static void Store(GameAction a, KeyCode k)
        {
            Cache[a] = k;
            PlayerPrefs.SetInt(PrefKey(a), (int)k);
        }

        public static void ResetAll()
        {
            foreach (var a in All) Store(a, Defaults[a]);
            PlayerPrefs.Save();
        }

        /// <summary>French-friendly display name for a key.</summary>
        public static string KeyName(KeyCode k) => k switch
        {
            KeyCode.Mouse0 => "Clic G.",
            KeyCode.Mouse1 => "Clic D.",
            KeyCode.Mouse2 => "Clic M.",
            KeyCode.Space => "Espace",
            KeyCode.LeftShift => "Maj G.",
            KeyCode.RightShift => "Maj D.",
            KeyCode.LeftControl => "Ctrl G.",
            KeyCode.RightControl => "Ctrl D.",
            KeyCode.LeftAlt => "Alt G.",
            KeyCode.RightAlt => "Alt D.",
            KeyCode.Return => "Entrée",
            KeyCode.Tab => "Tab",
            KeyCode.UpArrow => "Haut",
            KeyCode.DownArrow => "Bas",
            KeyCode.LeftArrow => "Gauche",
            KeyCode.RightArrow => "Droite",
            KeyCode.None => "—",
            _ => k.ToString()
        };

        // KeyCodes eligible for capture (excludes None, Escape reserved for cancel, and joystick codes).
        static KeyCode[] _capturable;
        public static KeyCode[] Capturable
        {
            get
            {
                if (_capturable == null)
                {
                    var list = new List<KeyCode>();
                    foreach (KeyCode k in Enum.GetValues(typeof(KeyCode)))
                    {
                        if (k == KeyCode.None || k == KeyCode.Escape) continue;
                        var n = k.ToString();
                        if (n.StartsWith("Joystick")) continue;
                        list.Add(k);
                    }
                    _capturable = list.ToArray();
                }
                return _capturable;
            }
        }
    }
}
