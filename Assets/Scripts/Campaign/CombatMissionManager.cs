using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FirstGame.Core;
using FirstGame.Player;
using FirstGame.Combat;
using FirstGame.Abilities;
using FirstGame.Enemies;
using FirstGame.Equipment;
using FirstGame.UI;
using FirstGame.Progression;

namespace FirstGame.Campaign
{
    /// <summary>Runs the 3 combat missions (Duel / Round / Examen) with lives and win/lose.</summary>
    public class CombatMissionManager : MonoBehaviour
    {
        public FirstPersonController controller;
        public WeaponController weapon;
        public AbilitySystem abilities;
        public PlayerHealth health;
        public TutorialUI ui;
        public Transform arena;
        public Vector3 zoneCenter = new Vector3(0, 0, 18);

        readonly List<EnemyBot> _bots = new();
        int _lives, _deaths;
        bool _over;
        bool _ranked;
        int _rankedOpp;

        Text _livesLabel, _enemiesLabel, _captureLabel;
        Image _captureFill;
        GameObject _captureBar;

        static readonly Vector3[] SpawnPoints =
        {
            new Vector3(0, 0, 34), new Vector3(12, 0, 32), new Vector3(-12, 0, 32),
            new Vector3(20, 0, 18), new Vector3(-20, 0, 18), new Vector3(0, 0, 20),
        };

        public void StartMission(int index)
        {
            BeginWithLoadout(() => StartMissionInternal(index));
        }

        // ---------- Loadout phase (compose your build — agent + 2 weapons, no shop) ----------
        void BeginWithLoadout(System.Action start)
        {
            new GameObject("[Loadout]").AddComponent<LoadoutScreen>().Show(start);
        }

        void StartMissionInternal(int index)
        {
            _over = false; _deaths = 0;
            MatchConfig.ApplyDifficulty();
            BuildMiniHud();
            health.OnDied += OnPlayerDied;
            SetControls(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            switch (index)
            {
                case 0: _lives = 1; StartCoroutine(RunDuel()); break;
                case 1: _lives = 2; StartCoroutine(RunRound()); break;
                default: _lives = 3; StartCoroutine(RunExamen()); break;
            }
            UpdateLives();
        }

        // ---------- Ranked (ELO vs bots) ----------
        public void StartRanked()
        {
            BeginWithLoadout(StartRankedInternal);
        }

        void StartRankedInternal()
        {
            _over = false; _deaths = 0; _ranked = true;
            MatchConfig.ResetBotModifiers();
            BuildMiniHud();
            health.OnDied += OnPlayerDied;
            SetControls(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _lives = 1;
            UpdateLives();
            StartCoroutine(RunRanked());
        }

        IEnumerator RunRanked()
        {
            var p = PlayerProfile.Current;
            int elo = p.elo;
            BotTier tier;
            if (elo < 900) { tier = BotTier.Recrue; _rankedOpp = 700; }
            else if (elo < 1250) { tier = BotTier.Soldat; _rankedOpp = 1000; }
            else if (elo < 1650) { tier = BotTier.Veteran; _rankedOpp = 1400; }
            else { tier = BotTier.Elite; _rankedOpp = 1900; }

            ui.ShowStep(1, 1, $"CLASSÉ — {p.RankedTier} ({elo}) : élimine l'adversaire.",
                $"Adversaire estimé ~{_rankedOpp}. 1 vie. Gagne pour monter, perds pour descendre.");
            health.Heal(999f);
            bool botDead = false;
            var bot = SpawnBot(new Vector3(0, 0, 0), tier);
            bot.OnDied += _ => botDead = true;
            UpdateEnemies();
            while (!botDead && !_over) { UpdateEnemies(); yield return null; }
            if (_over) yield break;
            RankedEnd(true);
        }

        void RankedEnd(bool win)
        {
            if (_over) return;
            _over = true;
            SetControls(false);
            DespawnBots();
            var p = PlayerProfile.Current;
            string oldTier = p.RankedTier;
            int delta = p.ApplyMatchResult(win, _rankedOpp);
            string tierLine = oldTier != p.RankedTier ? $"{oldTier} → {p.RankedTier}" : p.RankedTier;
            string sign = delta >= 0 ? "+" : "";
            ui.ShowResult(
                win ? "VICTOIRE CLASSÉE" : "DÉFAITE CLASSÉE",
                $"{sign}{delta} ELO   •   {p.elo}  ({tierLine})",
                win ? "Bien joué. Rejoue pour continuer à grimper." : "Réessaie et remonte au prochain duel.",
                win ? ArtPalette.NeonCyan : ArtPalette.Enemy,
                "REJOUER", () => GameManager.LoadScene(SceneNames.CombatArena),
                () => GameManager.LoadScene(SceneNames.MainMenu));
        }

        // ---------- Missions ----------
        IEnumerator RunDuel()
        {
            BotTier[] tiers = { BotTier.Recrue, BotTier.Soldat, BotTier.Veteran, BotTier.Elite };
            for (int i = 0; i < 4; i++)
            {
                if (_over) yield break;
                ui.ShowStep(i + 1, 4, $"DUEL — Manche {i + 1}/4 : élimine l'adversaire.", "Vise la tête (x2 dégâts). Utilise le couvert entre deux tirs.");
                health.Heal(999f);
                bool dead = false;
                var bot = SpawnBot(new Vector3(0, 0, 0), tiers[i]);
                bot.OnDied += _ => dead = true;
                UpdateEnemies();
                while (!dead && !_over) yield return null;
                if (_over) yield break;
                ui.Toast($"Manche {i + 1} gagnée !");
                yield return new WaitForSeconds(2f);
            }
            Win(150, "DUEL REMPORTÉ");
        }

        IEnumerator RunRound()
        {
            ui.ShowStep(1, 1, "LE ROUND — Capture et sécurise la zone ambre.", "Reste dans la zone, sans ennemis dedans, jusqu'à 100%.");
            ShowCaptureBar(true);
            SpawnBot(new Vector3(-6, 0, 22), BotTier.Soldat);
            SpawnBot(new Vector3(6, 0, 22), BotTier.Soldat);
            SpawnBot(new Vector3(-3, 0, 26), BotTier.Veteran);
            SpawnBot(new Vector3(3, 0, 26), BotTier.Veteran);

            float capture = 0f;
            while (capture < 100f && !_over)
            {
                int inZone = 0;
                foreach (var b in _bots) if (b != null && b.IsAlive && Flat(b.transform.position, zoneCenter) < 4f) inZone++;
                bool playerIn = Flat(controller.transform.position, zoneCenter) < 4f;

                if (playerIn && inZone == 0) capture += 12f * Time.deltaTime;
                else if (inZone > 0) capture -= 8f * Time.deltaTime;
                capture = Mathf.Clamp(capture, 0f, 100f);

                UpdateCapture(capture);
                UpdateEnemies();
                yield return null;
            }
            if (!_over) Win(250, "ZONE SÉCURISÉE");
        }

        IEnumerator RunExamen()
        {
            (BotTier tier, int count, float scale)[][] waves =
            {
                new[] { (BotTier.Recrue, 3, 1f) },
                new[] { (BotTier.Soldat, 3, 1f) },
                new[] { (BotTier.Soldat, 2, 1f), (BotTier.Veteran, 2, 1f) },
                new[] { (BotTier.Veteran, 3, 1f), (BotTier.Elite, 1, 1.3f) },
            };

            for (int w = 0; w < waves.Length; w++)
            {
                if (_over) yield break;
                ui.ShowStep(w + 1, 4, $"EXAMEN — Vague {w + 1}/4 : survis et nettoie.", "Enchaîne tir et sorts. Le chef d'élite arrive à la dernière vague.");
                ui.Toast($"Vague {w + 1} / 4");
                int sp = 0;
                foreach (var group in waves[w])
                    for (int k = 0; k < group.count; k++)
                        SpawnBot(SpawnPoints[sp++ % SpawnPoints.Length], group.tier, group.scale);
                UpdateEnemies();

                while (AliveCount() > 0 && !_over) { UpdateEnemies(); yield return null; }
                if (_over) yield break;
                yield return new WaitForSeconds(3f);
            }
            int xp = _deaths == 0 ? 550 : 400;
            Win(xp, _deaths == 0 ? "EXAMEN RÉUSSI — SANS FAUTE !" : "EXAMEN RÉUSSI");
        }

        // ---------- Mode Pose / Désamorçage (spike) ----------
        public void StartSpike()
        {
            BeginWithLoadout(StartSpikeInternal);
        }

        void StartSpikeInternal()
        {
            _over = false; _deaths = 0;
            MatchConfig.ApplyDifficulty();
            BuildMiniHud();
            health.OnDied += OnPlayerDied;
            SetControls(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _lives = 2;
            UpdateLives();
            StartCoroutine(RunSpike());
        }

        IEnumerator RunSpike()
        {
            Vector3 site = zoneCenter; // amber disc at (0,0,18)
            const float PlantTime = 4f, FuseTime = 30f, DefuseTime = 6f;

            ui.ShowStep(1, 1, "POSE / DÉSAMORÇAGE — Attaque : arme la charge sur le site.",
                "Reste sur le disque ambre pour armer. Ensuite, défends la charge jusqu'à la détonation.");
            ShowCaptureBar(true);

            SpawnBot(new Vector3(-5, 0, 26), BotTier.Soldat);
            SpawnBot(new Vector3(5, 0, 26), BotTier.Soldat);
            SpawnBot(new Vector3(0, 0, 30), BotTier.Veteran);
            UpdateEnemies();

            // Phase 1 — plant, OR win outright by wiping the defenders.
            float plant = 0f;
            bool planted = false;
            while (!planted && !_over)
            {
                if (AliveCount() == 0) { Win(250, "DÉFENSEURS ÉLIMINÉS"); yield break; }
                bool playerIn = health.IsAlive && Flat(controller.transform.position, site) < 4f;
                plant += (playerIn ? 1f : -0.5f) * Time.deltaTime;
                plant = Mathf.Clamp(plant, 0f, PlantTime);
                SetCaptureColor(ArtPalette.Objective);
                UpdateCapture(plant / PlantTime * 100f);
                SetStateText(playerIn ? "POSE EN COURS…" : "REJOINS LE SITE POUR ARMER");
                if (plant >= PlantTime) planted = true;
                UpdateEnemies();
                yield return null;
            }
            if (_over) yield break;
            ui.Toast("CHARGE ARMÉE !");

            // Phase 2 — fuse counts down; a defender standing on the site defuses it.
            float fuse = FuseTime, defuse = 0f;
            while (fuse > 0f && !_over)
            {
                fuse -= Time.deltaTime;
                int botsOnSite = 0;
                foreach (var b in _bots)
                    if (b != null && b.IsAlive && Flat(b.transform.position, site) < 4f) botsOnSite++;

                defuse += (botsOnSite > 0 ? 1f : -1.5f) * Time.deltaTime;
                defuse = Mathf.Clamp(defuse, 0f, DefuseTime);

                if (defuse > 0f)
                {
                    SetCaptureColor(ArtPalette.NeonCyan);
                    UpdateCapture(defuse / DefuseTime * 100f);
                    SetStateText($"DÉSAMORÇAGE… défends le site ! ({Mathf.CeilToInt(DefuseTime - defuse)}s)");
                }
                else
                {
                    SetCaptureColor(ArtPalette.Enemy);
                    UpdateCapture(fuse / FuseTime * 100f);
                    SetStateText($"DÉTONATION DANS {Mathf.CeilToInt(fuse)}s");
                }

                if (defuse >= DefuseTime) { SpikeLose("La charge a été désamorcée."); yield break; }
                UpdateEnemies();
                yield return null;
            }
            if (_over) yield break;
            Win(300, "SITE DÉTRUIT — VICTOIRE");
        }

        void SpikeLose(string reason)
        {
            if (_over) return;
            _over = true;
            SetControls(false);
            DespawnBots();
            ui.ShowResult("MANCHE PERDUE", reason,
                "Empêche les défenseurs d'atteindre le site après la pose.", ArtPalette.Enemy,
                "RÉESSAYER", () => GameManager.LoadScene(SceneNames.CombatArena),
                () => GameManager.LoadScene(SceneNames.MainMenu));
        }

        void SetStateText(string s) { if (_captureLabel) _captureLabel.text = s; }
        void SetCaptureColor(Color c) { if (_captureFill) _captureFill.color = c; }

        // ---------- Helpers ----------
        EnemyBot SpawnBot(Vector3 pos, BotTier tier, float scale = 1f, string agentId = null)
        {
            agentId ??= AutoAgentFor(tier);
            var b = EnemyBot.Spawn(arena, pos, tier, controller.transform, health,
                                   autoRespawn: false, scale: scale, name: "Bot_" + tier, agentId: agentId);
            _bots.Add(b);
            return b;
        }

        // Strong bots become "ennemis-agents" (spell casters) to credibilise le solo.
        static string AutoAgentFor(BotTier tier)
        {
            if (tier == BotTier.Elite) return "agent_nocturne";
            if (tier == BotTier.Veteran && UnityEngine.Random.value < 0.5f) return "agent_faille";
            return null;
        }

        int AliveCount()
        {
            int n = 0;
            foreach (var b in _bots) if (b != null && b.IsAlive) n++;
            return n;
        }

        static float Flat(Vector3 a, Vector3 b) { a.y = 0; b.y = 0; return Vector3.Distance(a, b); }

        void OnPlayerDied()
        {
            if (_over) return;
            _deaths++; _lives--;
            UpdateLives();
            if (_ranked) { if (_lives <= 0) RankedEnd(false); return; }
            if (_lives <= 0) { ui?.Toast("Éliminé !"); Lose(); }
            else ui?.Toast($"Éliminé ! Vies restantes : {_lives}");
        }

        void SetControls(bool on)
        {
            if (controller) controller.ControlEnabled = on;
            if (weapon) weapon.ControlEnabled = on;
            if (abilities) abilities.ControlEnabled = on;
            var util = controller ? controller.GetComponent<UtilityController>() : null;
            if (util) util.ControlEnabled = on;
        }

        void DespawnBots()
        {
            foreach (var b in _bots) if (b != null) Destroy(b.gameObject);
            _bots.Clear();
        }

        void Win(int xp, string title)
        {
            if (_over) return;
            _over = true;
            PlayerProfile.Current.AddXp(xp);
            SetControls(false);
            DespawnBots();
            ui.ShowResult(title, $"+{xp} XP   •   Niveau {PlayerProfile.Current.level}",
                "Bravo, agent. Rejoue une mission ou retourne au menu.", ArtPalette.NeonCyan,
                "REJOUER", () => GameManager.LoadScene(SceneNames.CombatArena), () => GameManager.LoadScene(SceneNames.MainMenu));
        }

        void Lose()
        {
            if (_over) return;
            _over = true;
            SetControls(false);
            DespawnBots();
            ui.ShowResult("MISSION ÉCHOUÉE", "Tu as été éliminé.",
                "Réessaie — ajuste ton loadout dans l'Arsenal si besoin.", ArtPalette.Enemy,
                "RÉESSAYER", () => GameManager.LoadScene(SceneNames.CombatArena), () => GameManager.LoadScene(SceneNames.MainMenu));
        }

        // ---------- Mini HUD ----------
        void BuildMiniHud()
        {
            var canvas = UIFactory.CreateCanvas("CombatHud", 6);
            var block = UIFactory.AddChild(canvas.transform, "MissionInfo");
            UIFactory.Place(block, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -120), new Vector2(300, 84));
            UIFactory.Panel(block, new Color(ArtPalette.UiInk.r, ArtPalette.UiInk.g, ArtPalette.UiInk.b, 0.8f));
            _livesLabel = UIFactory.Label(block, "", 24, ArtPalette.Enemy, TextAnchor.UpperCenter, FontStyle.Bold);
            UIFactory.Place(_livesLabel.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -10), new Vector2(0, 30));
            UIFactory.Anchor(_livesLabel.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -40), new Vector2(-8, -8));
            _enemiesLabel = UIFactory.Label(block, "", 22, ArtPalette.UiText, TextAnchor.LowerCenter, FontStyle.Bold);
            UIFactory.Anchor(_enemiesLabel.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(8, 8), new Vector2(-8, 40));

            _captureBar = UIFactory.AddChild(canvas.transform, "CaptureBar").gameObject;
            var cbRt = (RectTransform)_captureBar.transform;
            UIFactory.Place(cbRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(500, 28));
            _captureBar.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);
            var fill = UIFactory.AddChild(_captureBar.transform, "Fill");
            UIFactory.Stretch(fill, 3);
            _captureFill = fill.gameObject.AddComponent<Image>();
            _captureFill.color = ArtPalette.Objective;
            _captureFill.type = Image.Type.Filled;
            _captureFill.fillMethod = Image.FillMethod.Horizontal;
            _captureFill.fillAmount = 0f;

            _captureLabel = UIFactory.Label(_captureBar.transform, "", 18, ArtPalette.UiText, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(_captureLabel.rectTransform);

            _captureBar.SetActive(false);
        }

        void UpdateLives() { if (_livesLabel) _livesLabel.text = $"VIES : {Mathf.Max(_lives, 0)}"; }
        void UpdateEnemies() { if (_enemiesLabel) _enemiesLabel.text = $"ENNEMIS : {AliveCount()}"; }
        void ShowCaptureBar(bool on) { if (_captureBar) _captureBar.SetActive(on); }
        void UpdateCapture(float pct) { if (_captureFill) _captureFill.fillAmount = pct / 100f; }
    }
}
