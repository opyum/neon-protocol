# Document de conception — NEON PROTOCOL

## Piliers
1. **Précision reine** — TTK court (0,3–0,6 s), headshot ×2, recul déterministe et apprenable.
2. **Lisibilité procédurale** — la couleur est l'information (ennemi rouge, allié cyan, objectif ambre).
3. **Décision avant action** — 3 sorts choisis parmi ~10 + équipement + stats : chaque engagement se prépare.
4. **Progression = options, pas puissance brute** — stats plafonnées, respec gratuit, zéro pay-to-win.
5. **La campagne enseigne, le PvP éprouve** — chaque système est appris isolé en solo avant le PvP.

## Boucle de jeu
- **Macro** : Menu → préparation (stats + 3 sorts + équipement) → match → XP/ELO → montée de niveau → retour prépa.
- **Micro (round 30–100 s)** : achat 15 s → déploiement → prise d'info → engagement (TTK court) → sorts (cooldown/charges) → objectif.

## Campagne (7 missions — la 1re est le tutoriel construit ici)
1. **Contact** — se déplacer, viser, tirer, recharger. *(implémenté : tutoriel 8 étapes)*
2. **Ligne de mire** — recul et headshots.
3. **L'Arsenal** — choisir et utiliser 3 sorts.
4. **Économie** — gérer les crédits sur 3 vagues.
5. **Le Duel** — 1v1 puis 1v3 contre des bots.
6. **Le Round** — round complet attaque/défense.
7. **Examen** — 5v5 bots, débloque le Compétitif classé.

## Progression
- Niveau max **30**. Courbe : `XP(n→n+1) = round(100 × 1,15^(n-1), 10)`. *(implémenté dans `PlayerProfile`)*
- **2 points de stats par niveau**, plafond 20 par stat, **respec gratuit**.

| Stat | Effet | Par point |
|---|---|---|
| Vitalité | PV (base 100) | +6 PV |
| Célérité | Vitesse (base 6 m/s) | +1,5 % (cap +25 %) |
| Contrôle | Dispersion/recul | −2,5 % (cap −50 %) |
| Focalisation | Recharge des sorts | −2 % (cap −40 %) |
| Amplification | Puissance des sorts | +3 % (cap +60 %) |
| Régénération | Soin hors combat | +0,5 PV/s |

## Modes de jeu
Campagne (PvE) · Stand de tir · Match à mort FFA · Match rapide 5v5 · **Compétitif 5v5 (ELO)** · Événements.

## Système d'ELO (phase 2, spécifié)
MMR type Elo, départ 1000, K adaptatif (40 placement → 24 → 16).
`E_A = 1 / (1 + 10^((R_B − R_A)/400))`, `Δ = K × (S − E_A)`.
Paliers : Fer < 800 · Bronze · Argent · Or · Platine · Émeraude · Diamant · Maître · Champion > 2500.
