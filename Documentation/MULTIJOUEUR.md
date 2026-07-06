# Multijoueur + ELO (plan)

> **Statut : conçu, pas implémenté.** Le netcode est un chantier lourd qui exige un test à
> **2 clients minimum** (impossible à valider en aveugle / en compilation). Voici la marche à suivre
> réaliste, en s'appuyant sur ce qui existe déjà (loadout, sorts, bots, arène, PlayerHealth).

## Ordre recommandé (du plus sûr au plus ambitieux)
1. **Choisir la techno** :
   - *Netcode for GameObjects* (officiel Unity, gratuit) — bon pour apprendre.
   - *Photon Fusion / PUN* — hébergement relais inclus, plus rapide à mettre en ligne.
   Recommandation débutant : **Netcode for GameObjects** + *Unity Transport*, en **1v1 d'abord**.
2. **Rendre le joueur "réseau"** : convertir `PlayerRig`/`FirstPersonController` pour n'autoriser les
   entrées que sur le joueur local (`IsOwner`), synchroniser position/rotation, et répliquer les tirs
   (hitscan) via des RPC serveur-autoritaires (le serveur valide les dégâts, pas le client).
3. **Répliquer le combat** : `PlayerHealth` devient une variable réseau ; les dégâts passent par le
   serveur. Les sorts (`AbilitySystem`) et utilitaires déclenchent des effets répliqués (spawn réseau).
4. **Boucle de round** : phase d'achat 15 s (économie de crédits), pose/désamorçage d'un objectif,
   best-of-13 (premier à 7). Réutiliser l'arène de `CombatArenaScene`.
5. **Matchmaking + lobby** : file d'attente, création/join de partie, sélection d'équipe.

## Système ELO (déjà spécifié — voir `GAME_DESIGN.md`)
- Rating de départ **1000**, K adaptatif (40 placement → 24 → 16).
- Score attendu : `E_A = 1 / (1 + 10^((R_B − R_A)/400))`, variation `Δ = K × (S − E_A)`.
- En 5v5 : comparer la moyenne de rating des deux équipes, appliquer le même `Δ` à chaque joueur.
- Paliers : Fer < 800 · Bronze · Argent · Or · Platine · Émeraude · Diamant · Maître · Champion > 2500.
- L'ELO peut être calculé **côté serveur** à la fin du match et persisté (base de données / service).

## Ce que le solo actuel apporte déjà
- Les **bots** (`EnemyBot`) sont une excellente cible d'entraînement et une base pour des **bots de
  remplacement** si un joueur quitte.
- Le **loadout**, les **sorts**, l'**équipement** et l'**arène** sont réutilisables tels quels ;
  il "suffit" de les rendre réseau-autoritaires.

## Conseil
Ne saute pas à un 5v5 classé. Un **1v1 fonctionnel en réseau local** est déjà une grande étape et
valide toute l'architecture. Le reste (5v5, matchmaking, ELO) s'ajoute ensuite.
