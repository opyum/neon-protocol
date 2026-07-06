# Feuille de route

État actuel : **prototype jouable** — menu d'intro + campagne/tutoriel (8 étapes) + progression.
Tout compile et tourne sous Unity 6.1.

## Phase 1 — Fondations jouables ✅ (fait)
- [x] Menu d'introduction + écran Personnage/Stats
- [x] Contrôleur FPS, tir hitscan + headshot, rechargement
- [x] Système de sorts (3 équipés, charges/cooldown)
- [x] Mannequins d'entraînement (fixes + mobiles)
- [x] HUD, tutoriel 8 étapes, XP/niveaux persistants

## Phase 2 — Contenu solo & ressenti
- [x] **Stand de tir** (Entraînement libre : cibles qui réapparaissent, changement d'arme 1-5).
- [x] **Sons procéduraux** (tir, rechargement, impact, sort) + **screenshake**.
- [x] **Menu de sélection des 3 sorts parmi 10** + **choix de l'arme** (écran Arsenal, sauvegardé).
- [ ] **Passer en URP + Bloom** pour la vraie DA néon (voir `DIRECTION_ARTISTIQUE.md`).
      *La façon la plus rentable d'améliorer le rendu.*
- [ ] Musique de fond + sons de pas.
- [ ] Bots simples avec IA de tir (mouvement + ligne de vue) pour les missions Duel / Round.
- [ ] Missions 2 à 4 (recul déterministe, économie de crédits).
- [ ] Effets de sorts complets : mur physique, fumée occlusive, zone de dégâts persistante.
- [ ] Choix d'équipement (armure, utilitaires) avant match.

## Phase 3 — Multijoueur & compétitif
- [ ] Netcode (Netcode for GameObjects ou Photon Fusion) — commencer par un 1v1.
- [ ] Boucle de round complète (achat 15 s, pose/désamorçage, best-of-13).
- [ ] Matchmaking + **système ELO** (déjà spécifié dans `GAME_DESIGN.md`).
- [ ] Profils serveur, anti-triche de base, classement.

## Dette technique à surveiller
- Les scènes sont volontairement vides (monde bâti par code). Si tu veux éditer visuellement
  dans l'éditeur, tu peux à terme « figer » une scène construite en prefab.
- `WeaponCatalog` / `AbilityCatalog` sont des données en C#. Les migrer vers des
  **ScriptableObjects** facilitera l'équilibrage sans recompiler.
- Le rendu Built-in RP est un choix de fiabilité ; URP est le chemin d'évolution visuelle.
