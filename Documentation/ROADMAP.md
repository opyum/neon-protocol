# Feuille de route

État actuel : **prototype jouable** — menu d'intro + campagne/tutoriel (8 étapes) + progression.
Tout compile et tourne sous Unity 6.1.

## Phase 1 — Fondations jouables ✅ (fait)
- [x] Menu d'introduction + écran Personnage/Stats
- [x] Contrôleur FPS, tir hitscan + headshot, rechargement
- [x] Système de sorts (3 équipés, charges/cooldown)
- [x] Mannequins d'entraînement (fixes + mobiles)
- [x] HUD, tutoriel 8 étapes, XP/niveaux persistants

## Phase 2 — Contenu solo & ressenti ✅
- [x] Stand de tir (cibles qui réapparaissent, armes 1-5).
- [x] Sons procéduraux + screenshake.
- [x] Sélection des 3 sorts parmi 10 + choix de l'arme (Arsenal).

## Phase 3 — Combat, effets & ambiance ✅ (fait)
- [x] **Bots IA** qui ripostent (perception ligne de vue, poursuite, tir hitscan, 4 paliers).
- [x] **Missions de combat** (Le Duel, Le Round, Examen) avec vies + victoire/défaite.
- [x] **Effets de sorts complets** : Mur, Fumée, Zone de dégâts, Rafale (knockback réel).
- [x] **Équipement** : armures (bouclier/vitesse/régén) + utilitaires (fumigène, flash, drone) touche G.
- [x] **Musique** synthwave + **sons de pas** + sons d'UI (survol/clic).
- [x] **Skybox en dégradé** + peaufinage d'éclairage (contour, reflets, brouillard).
- [x] **ArtPalette prête pour URP** (sélection de shader selon le pipeline).

## Phase 4 — À faire (les deux morceaux lourds)
- [ ] **URP + Bloom** — pour les vrais néons. Étapes précises dans `URP_MIGRATION.md`.
      ⚠️ Nécessite une passe de validation VISUELLE dans l'éditeur (non vérifiable en compilation).
- [ ] **Multijoueur + ELO** — voir `MULTIJOUEUR.md`. Commencer par un 1v1 en réseau.
- [ ] Économie de crédits / phase d'achat, recul d'arme déterministe.
- [ ] Sélection d'arme/équipement par catégorie, plus d'armes et de maps.

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
