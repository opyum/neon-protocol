# Direction Artistique — « NEON PROTOCOL »

**FPS compétitif procédural, style flat / stylisé, haute lisibilité.**
Principe fondateur : *sans texture, la couleur EST l'information*. Le décor reste neutre à ~90 % ;
la couleur saturée (néons, ennemis, objectifs) est un **langage de gameplay**, pas de la déco.

Mots-clés : flat stylisé · propre et net · contraste compétitif · néon froid sur béton neutre ·
crépuscule cyber · lisibilité avant tout · matériaux mats · accents émissifs saturés.

## Palette (hex utilisés dans le code — `ArtPalette.cs`)

| Rôle | Hex | Usage |
|---|---|---|
| Béton froid (sol) | `#3A4048` | Sol, base neutre |
| Béton clair (murs) | `#5A626C` | Murs, structures verticales |
| Ardoise (couverture) | `#2A2F36` | Caisses, obstacles — silhouette forte |
| **Rouge hostile** | `#FF2D4E` | Ennemis / mannequins — **exclusif aux cibles** |
| **Cyan allié** | `#22E0C8` | Joueur, alliés, viewmodel |
| Cyan néon | `#00F0FF` | Néons, HUD, arêtes tech |
| Magenta néon | `#FF3DD0` | Néons secondaires, sorts |
| **Ambre objectif** | `#FFB627` | Objectifs, éléments interactifs, checkpoints |
| Orange danger | `#FF6A1A` | Zones de dégâts, alerte |
| Indigo crépuscule (ciel) | `#141A2E` | Skybox / fond |
| Encre UI | `#0E1420` | Panneaux d'interface |
| Blanc signal | `#EAF2FF` | Texte, réticule (jamais `#FFFFFF` pur) |

## Règle des 3 plans de valeur
SOL sombre-moyen → MURS plus clairs → COUVERTURES foncées. Le joueur distingue
sol / mur / obstacle d'un coup d'œil. C'est déjà appliqué dans `TutorialScene.BuildArena()`.

## Matériaux (guide)
Tous mats sauf `MAT_Metal_Tech` (`#8A929C`, metallic 0.85) réservé aux détails fonctionnels.
Ennemis et objectifs légèrement **auto-émissifs** pour ne jamais disparaître dans l'ombre
(`ArtPalette.MakeEmissive`).

## UI
Flat, tactique, **tout en français** : `JOUER`, `CAMPAGNE`, `PERSONNAGE`, `PARAMÈTRES`, `QUITTER`.
Fonds encre `#0E1420` à ~85 %, accent unique cyan pour la sélection, ambre pour la confirmation.

## Éclairage (cible)
Directionnelle froide `#C8D4FF`, ombres nettes (info tactique). Ambiance sombre pour faire
ressortir les émissifs. **Cible finale : URP + Bloom** (threshold ~0.9) pour les vrais néons —
le prototype approxime avec des matériaux *unlit* brillants sous Built-in RP.
