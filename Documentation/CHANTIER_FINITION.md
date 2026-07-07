# Chantier finition — 2026-07-07

Intégration du travail de l'escouade (audit PO + 6 agents dev). **Tout compile (0 erreur).**
Ce document dit HONNÊTEMENT ce qui est réellement dans le code (certains items des specs n'ont pas été écrits par les agents ; je les ai complétés ou reversés au backlog).

## ✅ Livré et compilé

### E2 — Passives d'agents & ennemis-agents
- `AgentPassiveSystem.cs` : Brasier (Élan Ardent), Nocturne (Emprise), Faille (Onde de Choc), Rempart (Garde).
- `AbilityFx.cs` : helper mur/fumée/zone partagé joueur ↔ bots ; `EnemyBot` peut lancer des sorts (agentId).

### E5 — Options complètes
- `OptionsPanel.cs` : écran Options réutilisé au **menu principal** et en **pause** (sensibilité, volumes, FOV, plein écran, résolution, qualité).
- `Keybinds.cs` : remappage des touches. `Settings.cs` étendu (FOV, plein écran…). ADS/FOV live via `FirstPersonController.cam`.

### E1 — Feedback de combat (complété par moi)
- `KillFeed.cs` : journal d'éliminations haut-droit (5 lignes, ~5s), solo + réseau.
- `DamageNumbers.cs` : chiffres de dégâts flottants (pool, headshots dorés/plus gros), solo + réseau.

### Divers
- `PracticeDrill.cs` : stand d'entraînement chronométré (touche T au practice).
- 2e layout d'arène (Alpha/Beta) via `MatchConfig`/`LevelBuilder`.
- `MatchConfig.cs` + `CombatMissionManager` : réglages de manche (à valider au test).

## 🟡 Partiel / à finir (voir BACKLOG.md)

- **E1** : indicateur directionnel de dégâts, jauge de rechargement, munitions basses, flash de cast — pas encore.
- **E3** : switch d'arme, rechargement animé, recul/dispersion — WeaponController à peine touché.
- **E4** : effets de sorts (mur/fumée/zone) NON répliqués en réseau.
- **E7** : boucle best-of réseau + achat synchronisé — à confirmer/finir.

## ⬜ Non commencé

- E8 Équilibrage · E9 Anti-triche réseau · E10 Audio · E11 Online/Relay · E12 Polish menus.

## Check-list de test (passe de validation joueur)

### Solo (Missions de combat / Entraînement)
- [ ] Kill feed : éliminer un bot ajoute une ligne haut-droite qui s'efface après ~5s.
- [ ] Damage numbers : chaque impact fait monter un chiffre ; headshots dorés/plus gros.
- [ ] Agent : choisir chaque agent au menu → sa passive se ressent (Brasier vitesse après kill, Nocturne zones + longues, Faille révèle, Rempart régén).
- [ ] Les bots (paliers élevés) lancent parfois un sort (fumée/zone/mur).
- [ ] Practice : touche T lance un drill chronométré.
- [ ] 2e layout d'arène sélectionnable depuis le picker de missions.

### Options
- [ ] Menu principal ▸ OPTIONS s'ouvre ; régler sensibilité/volume/FOV/plein écran/résolution/qualité et vérifier l'effet.
- [ ] Remapper une touche (Keybinds) et vérifier qu'elle est prise en compte en jeu.
- [ ] Les réglages persistent après avoir quitté/relancé.

### Multijoueur
- [ ] Kill feed + damage numbers apparaissent aussi en partie réseau.
- [ ] Tab (scores), Échap (quitter), respawn OK (régressions ?).
- [ ] Se tirer dessus fonctionne toujours.

Remonte-moi tout ce qui cloche, je corrige.
