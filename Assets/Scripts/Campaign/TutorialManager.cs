using System;
using System.Collections.Generic;
using UnityEngine;
using FirstGame.Core;
using FirstGame.Player;
using FirstGame.Combat;
using FirstGame.Abilities;
using FirstGame.Enemies;
using FirstGame.UI;
using FirstGame.Progression;

namespace FirstGame.Campaign
{
    /// <summary>
    /// Drives the 8-step playable tutorial from the design pass. Each step has an enter
    /// action, a measurable completion check, and French on-screen text. Grants XP on finish.
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        public FirstPersonController controller;
        public WeaponController weapon;
        public AbilitySystem abilities;
        public PlayerHealth health;
        public TutorialUI ui;
        public TrainingDummy[] staticDummies;
        public TrainingDummy[] movingDummies;

        class Step
        {
            public string instruction;
            public string hint;
            public Action onEnter;
            public Func<bool> isDone;
        }

        readonly List<Step> _steps = new();
        int _index = -1;
        float _stepStartTime;
        bool _finished;
        bool _advancing;

        // event counters
        int _weaponHits, _reloads, _abilityUses, _staticDeaths, _movingDeaths;
        int _bWeaponHits, _bReloads, _bAbilityUses, _bStaticDeaths, _bMovingDeaths, _bJumps;

        GameObject _moveMarker;

        void Start()
        {
            if (weapon != null) { weapon.OnHit += (_, __, ___) => _weaponHits++; weapon.OnReloadEnd += () => _reloads++; }
            if (abilities != null) abilities.OnAbilityUsed += (_, __) => _abilityUses++;
            if (staticDummies != null) foreach (var d in staticDummies) if (d) d.OnDied += _ => _staticDeaths++;
            if (movingDummies != null) foreach (var d in movingDummies) if (d) d.OnDied += _ => _movingDeaths++;

            BuildMoveMarker();
            BuildSteps();
            Next();
        }

        void BuildMoveMarker()
        {
            Vector3 pos = (controller != null ? controller.transform.position : Vector3.zero) + Vector3.forward * 12f;
            pos.y = 0.06f;
            _moveMarker = Prim.Cylinder(null, pos, 1.6f, 0.12f, ArtPalette.Objective, unlit: true, name: "MoveMarker");
            var col = _moveMarker.GetComponent<Collider>(); if (col) Destroy(col);
            var pulse = _moveMarker.AddComponent<Pulse>();
            pulse.color = ArtPalette.Objective; pulse.speed = 3f;
            _moveMarker.SetActive(false);
        }

        void BuildSteps()
        {
            // 1 — Look around
            _steps.Add(new Step
            {
                instruction = "Bouge la SOURIS pour regarder autour de toi.",
                hint = "Regarde à gauche/droite, puis lève et baisse les yeux.",
                onEnter = () => controller?.ResetLookAccumulators(),
                isDone = () => controller != null && controller.AccumYaw >= 360f && controller.AccumPitch >= 60f
            });

            // 2 — Move to the marker
            _steps.Add(new Step
            {
                instruction = "Déplace-toi avec ZQSD et rejoins le cercle ambre au sol.",
                hint = "Maintiens Z pour avancer tout droit jusqu'au cercle.",
                onEnter = () => { if (_moveMarker) _moveMarker.SetActive(true); },
                isDone = () =>
                {
                    if (_moveMarker == null || controller == null) return false;
                    var a = controller.transform.position; a.y = 0;
                    var b = _moveMarker.transform.position; b.y = 0;
                    bool done = Vector3.Distance(a, b) < 2f;
                    if (done) _moveMarker.SetActive(false);
                    return done;
                }
            });

            // 3 — Jump
            _steps.Add(new Step
            {
                instruction = "Appuie sur ESPACE pour sauter.",
                hint = "Un seul saut à la fois — pas de double-saut.",
                onEnter = () => { _bJumps = controller != null ? controller.JumpCount : 0; },
                isDone = () => controller != null && controller.JumpCount - _bJumps >= 1
            });

            // 4 — Shoot (dummies locked: they can't die yet)
            _steps.Add(new Step
            {
                instruction = "Vise un mannequin et fais CLIC GAUCHE pour tirer (3 touches).",
                hint = "Place le réticule central sur la cible avant de cliquer.",
                onEnter = () => { _bWeaponHits = _weaponHits; SetDummiesLocked(true); },
                isDone = () => _weaponHits - _bWeaponHits >= 3
            });

            // 5 — Reload
            _steps.Add(new Step
            {
                instruction = "Chargeur presque vide : appuie sur R pour recharger.",
                hint = "Le compteur de munitions remonte au maximum après 1,75 s.",
                onEnter = () => { _bReloads = _reloads; weapon?.SetAmmo(2); },
                isDone = () => _reloads - _bReloads >= 1
            });

            // 6 — Cast an ability
            _steps.Add(new Step
            {
                instruction = "Appuie sur E pour lancer ton SORT sur un mannequin.",
                hint = "Le sort du slot E est offensif. Il se recharge après usage.",
                onEnter = () => { _bAbilityUses = _abilityUses; },
                isDone = () => _abilityUses - _bAbilityUses >= 1
            });

            // 7 — Defeat 3 static dummies
            _steps.Add(new Step
            {
                instruction = "Élimine les 3 mannequins d'entraînement !",
                hint = "Vise la tête (sphère blanche) pour des dégâts doublés. Recharge avec R.",
                onEnter = () => { _bStaticDeaths = _staticDeaths; SetDummiesLocked(false); ResetStaticDummies(); },
                isDone = () => _staticDeaths - _bStaticDeaths >= 3
            });

            // 8 — Final: 2 moving dummies, 45s reset, unlimited retries
            _steps.Add(new Step
            {
                instruction = "ÉPREUVE FINALE : vaincs les 2 mannequins mobiles (tir + sort E).",
                hint = "Anticipe leur trajectoire. Tu peux réessayer autant que tu veux.",
                onEnter = () => { _bMovingDeaths = _movingDeaths; EnableMovingDummies(); },
                isDone = () =>
                {
                    if (Time.time - _stepStartTime > 45f)
                    {
                        // reset the attempt (no penalty)
                        _bMovingDeaths = _movingDeaths;
                        EnableMovingDummies();
                        _stepStartTime = Time.time;
                        ui?.Toast("Nouvelle tentative !");
                    }
                    return _movingDeaths - _bMovingDeaths >= 2;
                }
            });
        }

        void Update()
        {
            if (_finished || _advancing || _index < 0 || _index >= _steps.Count) return;
            if (_steps[_index].isDone != null && _steps[_index].isDone())
                CompleteCurrent();
        }

        void CompleteCurrent()
        {
            _advancing = true;
            ui?.Toast("Objectif accompli !");
            Invoke(nameof(Next), 1.1f);
        }

        void Next()
        {
            _advancing = false;
            _index++;
            if (_index >= _steps.Count) { Finish(); return; }

            _stepStartTime = Time.time;
            var step = _steps[_index];
            step.onEnter?.Invoke();
            ui?.ShowStep(_index + 1, _steps.Count, step.instruction, step.hint);
        }

        void Finish()
        {
            _finished = true;
            var profile = PlayerProfile.Current;
            int before = profile.level;
            profile.AddXp(400); // "mission campagne 150-400 XP"

            if (controller) controller.ControlEnabled = false;
            if (weapon) weapon.ControlEnabled = false;
            if (abilities) abilities.ControlEnabled = false;

            ui?.ShowComplete(profile.level, 400,
                onReplay: () => GameManager.LoadScene(SceneNames.Tutorial),
                onMenu: () => GameManager.LoadScene(SceneNames.MainMenu));
        }

        // ---- helpers ----
        void SetDummiesLocked(bool locked)
        {
            if (staticDummies == null) return;
            foreach (var d in staticDummies) if (d) d.locked = locked;
        }

        void ResetStaticDummies()
        {
            if (staticDummies == null) return;
            foreach (var d in staticDummies) if (d) d.ResetDummy();
        }

        void EnableMovingDummies()
        {
            if (movingDummies == null) return;
            foreach (var d in movingDummies) if (d) d.ResetDummy();
        }
    }
}
