# NEON PROTOCOL — Expansion RPG

FPS tactique type Valorant **+ couche RPG** : build 100% custom, stats à niveaux infinis, arsenal étendu, modes de jeu et maps procédurales. Tout est construit par code (pas de scène à éditer).

## 1. Stats & progression (`PlayerProfile`)
- **Niveaux infinis** (courbe XP `80·1,12^(niv-1)`, plafonnée pour éviter l'overflow). +3 points/niveau.
- **5 stats**, sans plafond :
  - **Force** → +2% dégâts (armes & sorts) / pt
  - **Endurance** → +8 PV max & régén / pt
  - **Défense** → réduction de dégâts (soft cap : 50% à 20 pts)
  - **Intelligence** → −1,5% recharge des sorts & +puissance / pt
  - **Vitesse** → +1,2% vitesse de déplacement / pt
- Menu : **MON BUILD → PERSONNAGE** (distribuer les points, respec gratuit).

## 2. Build : 3 actifs + 3 passifs
- **100 sorts actifs** (`AbilityCatalog`) : 10 signatures + génération (12 éléments × 8 familles d'effets : Trait, Ruée, Zone, Mur, Égide, Vague, Souffle, Voile).
- **100 passifs** (`PassiveCatalog`) : 20 familles × 5 rangs (Force, Vitalité, Célérité, Focalisation, Vampirisme, Cuirasse, Berserker…). Modificateurs : dégâts, PV, vitesse, recharge, vol de vie, régén, rechargement, réduction.
- **Système** : `PassiveSystem` somme les 3 passifs et applique les modifs (réappliqué à chaud si on change de build en pause).
- **Écran de build** (`LoadoutScreen`, au début du round ou en pause → MODIFIER LE BUILD) : clique un emplacement (E/F/C ou P1/P2/P3) puis pioche dans la liste défilable. + arme principale + secondaire.
- Menu : **MON BUILD → SORTS & ARMES**.

## 3. Arsenal (`WeaponCatalog`, 12 armes)
Pistolet, SMG (Guêpe, Frelon), fusils (Rempart, Vanguard), pompe, DMR (Traqueur), snipers (Longue-Vue, Perceur), **mitrailleuse lourde** (Faucheuse — *à déployer : précise à l'arrêt, folle en mouvement*), **lance-flammes** (Fournaise — courte portée, cadence élevée), **lance-roquettes** (Fléau — *splash AoE*).
- **Switch en jeu** : touches **1 / 2 / molette** (2 armes portées).

## 4. Modes de jeu (`CombatMissionManager`) — vs bots
Duel · Round · Examen · **Classé (ELO)** · Spike · **Match à Mort (TDM, 20 kills)** · **Chacun pour soi (FFA, 25 kills)** · **Capture Attaque** · **Capture Défense (tenir 90s)**.
- Sélection : **JOUER → MISSIONS DE COMBAT** (grille 2 colonnes).

## 5. Maps procédurales (`MapGen`)
- **10 cartes** labyrinthes, seedées et déterministes : murs en corridors, piliers, caisses de couverture ; zones de spawn/objectif dégagées pour l'équilibre.
- Sélecteur **CARTE** + **STYLE** + **DIFFICULTÉ** dans le picker.

## Limites connues / à polir
- Les modes d'équipe (TDM/FFA) sont *solo vs bots* (pas encore de bots alliés ni de vrai PvP en équipe).
- Maps procédurales non ajustées à la main (peuvent avoir des recoins) — équilibrage à affiner au test.
- Multijoueur réseau : voir `MULTIJOUEUR_PLAN.md` (répliquer les sorts-volumes, boucle de manches, online).

## Fichiers clés
`PlayerProfile`, `AbilityCatalog`, `PassiveCatalog`, `PassiveSystem`, `WeaponCatalog`, `WeaponController`, `CombatMissionManager`, `MapGen`, `MatchConfig`, `LoadoutScreen`, `MainMenuUI`.
