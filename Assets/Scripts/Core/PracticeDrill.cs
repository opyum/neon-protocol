using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using FirstGame.Combat;
using FirstGame.Enemies;
using FirstGame.Progression;

namespace FirstGame.Core
{
    /// <summary>
    /// Timed precision drill for the Practice Range. Press T to launch: a sequence of targets
    /// appears one after another; the run is timed and scored (speed × accuracy). The best score
    /// and its time are saved to PlayerPrefs and shown on the panel. Awards a little XP.
    /// FPS-friendly: driven by the T key so the cursor can stay locked.
    /// </summary>
    public class PracticeDrill : MonoBehaviour
    {
        const int TargetCount = 12;
        const string KeyScore = "practice.drill.bestScore";
        const string KeyTime = "practice.drill.bestTime";

        public WeaponController weapon;

        Text _title, _mid, _best;
        bool _running;
        int _remaining, _shots, _hits;
        float _startTime;
        TrainingDummy _current;

        public static PracticeDrill Attach(WeaponController weapon)
        {
            var go = new GameObject("[PracticeDrill]");
            var d = go.AddComponent<PracticeDrill>();
            d.weapon = weapon;
            return d;
        }

        void Start()
        {
            BuildUI();
            Idle();
        }

        void BuildUI()
        {
            var canvas = UIFactory.CreateCanvas("DrillHud", 6);
            var block = UIFactory.AddChild(canvas.transform, "Drill");
            UIFactory.Place(block, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -120), new Vector2(440, 150));
            UIFactory.Panel(block, new Color(ArtPalette.UiInk.r, ArtPalette.UiInk.g, ArtPalette.UiInk.b, 0.82f));

            _title = UIFactory.Label(block, "", 24, ArtPalette.NeonCyan, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Anchor(_title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -44), new Vector2(-16, -8));

            _mid = UIFactory.Label(block, "", 20, ArtPalette.UiText, TextAnchor.UpperLeft);
            UIFactory.Anchor(_mid.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -92), new Vector2(-16, -48));

            _best = UIFactory.Label(block, "", 18, ArtPalette.UiDim, TextAnchor.UpperLeft);
            UIFactory.Anchor(_best.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -142), new Vector2(-16, -96));
        }

        void Idle()
        {
            _running = false;
            _title.text = "ÉPREUVE CHRONO";
            _mid.text = "Appuie sur T pour lancer.";
            _best.text = BestLine();
        }

        string BestLine()
        {
            int bs = PlayerPrefs.GetInt(KeyScore, 0);
            if (bs <= 0) return "Aucun record enregistré.";
            float bt = PlayerPrefs.GetFloat(KeyTime, 0f);
            return $"Record : {bs} pts  •  {bt:0.0}s";
        }

        void Update()
        {
            if (!_running)
            {
                if (Input.GetKeyDown(KeyCode.T)) StartDrill();
                return;
            }
            _mid.text = $"Cibles : {TargetCount - _remaining + 1}/{TargetCount}   •   {Time.time - _startTime:0.0}s";
        }

        void StartDrill()
        {
            _running = true;
            _remaining = TargetCount;
            _shots = 0; _hits = 0;
            _startTime = Time.time;
            if (weapon != null) { weapon.OnFired += OnShot; weapon.OnHit += OnHitAny; }
            _title.text = "ÉPREUVE EN COURS";
            _best.text = "Détruis chaque cible le plus vite possible.";
            SpawnNext();
        }

        void OnShot() => _shots++;
        void OnHitAny(FirstGame.Combat.IDamageable t, float dmg, bool head) => _hits++;

        void SpawnNext()
        {
            float x = Random.Range(-8f, 8f);
            float z = Random.Range(12f, 30f);
            _current = TrainingDummy.Spawn(null, new Vector3(x, 0f, z), 30f, name: "CibleDrill");
            _current.OnDied += OnTargetDead;
        }

        void OnTargetDead(TrainingDummy d)
        {
            _remaining--;
            if (_remaining <= 0) Finish();
            else SpawnNext();
        }

        void Finish()
        {
            if (weapon != null) { weapon.OnFired -= OnShot; weapon.OnHit -= OnHitAny; }
            float elapsed = Time.time - _startTime;
            float acc = _shots > 0 ? Mathf.Clamp01((float)_hits / _shots) : 0f;
            int score = Mathf.RoundToInt(10000f / (1f + elapsed) * (0.5f + 0.5f * acc));

            int best = PlayerPrefs.GetInt(KeyScore, 0);
            bool record = score > best;
            if (record)
            {
                PlayerPrefs.SetInt(KeyScore, score);
                PlayerPrefs.SetFloat(KeyTime, elapsed);
                PlayerPrefs.Save();
            }

            _title.text = record ? "NOUVEAU RECORD !" : "ÉPREUVE TERMINÉE";
            _mid.text = $"{score} pts  •  {elapsed:0.0}s  •  précision {Mathf.RoundToInt(acc * 100f)}%";
            _best.text = BestLine();

            PlayerProfile.Current.AddXp(Mathf.Clamp(score / 100, 5, 60));
            StartCoroutine(BackToIdle());
        }

        IEnumerator BackToIdle()
        {
            yield return new WaitForSeconds(4f);
            if (!_running) Idle();
        }
    }
}
