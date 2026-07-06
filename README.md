# NEON PROTOCOL — FPS tactique (prototype Unity 6.1)

Ton premier jeu : un FPS 3D compétitif inspiré de Valorant. Ce dépôt contient un
**prototype jouable de A à Z** du **menu d'introduction** et du **mode campagne / tutoriel**.

> Fait à la main dans Unity 6.1 (6000.5.2f1). **Le projet compile proprement** (vérifié en
> compilation batch, 0 erreur). Tout le monde de jeu est construit **par code au lancement** —
> il n'y a donc rien à câbler manuellement dans l'éditeur, ça marche dès l'ouverture.

---

## ✅ Prérequis

- **Unity 6000.5.2f1** (Unity 6.1), installé via **Unity Hub**.
  Une version proche de Unity 6.x devrait aussi fonctionner (Unity proposera une mise à niveau).
- **Git** pour cloner (ou télécharger le ZIP depuis GitHub).
- Aucun paquet payant, aucune texture externe : tout est procédural.

## ⬇️ Cloner le projet

```bash
git clone https://github.com/opyum/neon-protocol.git
```

> Ou sur GitHub : bouton vert **Code** → **Download ZIP**, puis décompresse.

## ▶ Ouvrir et lancer dans Unity

1. Ouvre **Unity Hub** → **Add** → **Add project from disk** → sélectionne le dossier cloné
   (celui qui contient les dossiers `Assets`, `Packages`, `ProjectSettings`).
2. Ouvre le projet avec **Unity 6000.5.2f1**. La première ouverture importe les paquets et
   régénère le dossier `Library/` (1–3 min), c'est normal.
3. Dans la fenêtre **Project**, ouvre `Assets/Scenes/MainMenu.unity`.
4. Appuie sur **▶ Play**. Le menu s'affiche → clique **CAMPAGNE**.

> Tu peux aussi ouvrir directement `Assets/Scenes/Tutorial.unity` ou `Assets/Scenes/PracticeRange.unity`
> et faire Play pour arriver tout de suite en jeu.

> ℹ️ Le dossier `Library/` n'est **pas** dans le dépôt (il est régénéré par Unity à l'ouverture) :
> c'est normal et voulu, c'est la bonne pratique pour un projet Unity partagé.

---

## 🎮 Contrôles

| Action | Touche |
|---|---|
| Se déplacer | **ZQSD** (aussi WASD / flèches) |
| Regarder | **Souris** |
| Sauter | **Espace** |
| Sprint | **Shift gauche** |
| Tirer | **Clic gauche** |
| Recharger | **R** |
| Sorts | **E**, **F**, **C** |
| Utilitaire (fumigène / flash / drone) | **G** |
| Changer d'arme (stand de tir) | **1** à **5** |
| Retour au menu | **Échap** |

---

## 📦 Ce qui est construit

- **Menu d'introduction** : titre à halo néon, fond 3D animé (grille Tron + formes flottantes),
  ciel en dégradé, puce de niveau/palier, et 6 entrées **toutes fonctionnelles**.
- **Arsenal** : choisis ton **arme parmi 5**, tes **3 sorts parmi les 10** (E/F/C), et ton
  **équipement** (armure passive + utilitaire touche G). Sauvegardé et utilisé en jeu.
- **Écran Personnage** : montée de niveau, distribution des **6 stats**, respec gratuit.
- **Mode Campagne (tutoriel jouable en 8 étapes)** : déplacement, visée, saut, tir, rechargement,
  sorts, mannequins fixes puis mobiles.
- **Missions de combat** : arène avec **bots qui ripostent** (4 paliers de difficulté). 3 missions —
  **Le Duel** (4 manches 1v1), **Le Round** (capture de zone), **Examen** (4 vagues + chef d'élite),
  avec système de vies et écran victoire/défaite.
- **Stand de tir (Entraînement libre)** : loadout complet, armes aux touches 1-5, cibles qui réapparaissent.
- **Sorts complets** : Mur de glace (bloque balles+déplacement), Voile d'ombre (coupe la vue),
  Nuage toxique (dégâts/s), Rafale de vent (repousse les ennemis), plus dégâts/dash/soin/bouclier.
- **Équipement** : armures (bouclier + vitesse, régén), et utilitaires — fumigène, grenade
  aveuglante (flash plein écran), drone de reconnaissance.
- **Game feel** : **musique synthwave + SFX + sons de pas** générés par code, **tremblement de caméra**,
  et **skybox en dégradé** + éclairage stylisé (lumière de contour, brouillard de profondeur).
- **Systèmes de jeu réels** :
  - Contrôleur FPS (CharacterController) — AZERTY/QWERTY.
  - Tir hitscan avec **headshot ×2** (viser la sphère blanche), munitions, rechargement, traceur.
  - Système de **sorts** (3 équipés, charges + cooldown, effets : dégâts, dash, soin, bouclier…).
  - **Mannequins d'entraînement** (feedback de coup, mort, reset, patrouille).
  - **HUD** codé : réticule, barre de vie + bouclier, munitions, icônes de sorts avec cooldown radial, hitmarker.
  - **Progression persistante** (PlayerPrefs) : XP, niveaux (cap 30), points de stats.
- **Direction artistique "NEON PROTOCOL"** appliquée : palette béton froid + néons cyan/magenta,
  ennemis rouges, objectifs ambre — voir `Documentation/DIRECTION_ARTISTIQUE.md`.

> **Assets** : le décor est procédural (primitives), mais les **personnages** (ennemis/mannequins) et
> les **armes** utilisent désormais de **vrais modèles 3D** (packs **Kenney**, CC0, inclus dans le dépôt).
> Voir `Documentation/ASSETS_IMPORT.md` (dont comment ajouter des animations Mixamo). Rendu en
> **URP 17.5 + Bloom**. ⚠️ **À la première ouverture, passe une fois dans chaque scène pour valider le
> rendu à l'œil** (pas de matériaux roses, bloom sur les néons) — voir `Documentation/URP_MIGRATION.md`.

---

## 🗂 Structure

```
Assets/
  Scenes/            MainMenu.unity, Tutorial.unity  (scènes vides : le monde est bâti par code)
  Scripts/
    Core/            Palette, UIFactory, primitives, GameManager (bootstrap), scene builders
    Player/          FirstPersonController, PlayerHealth
    Combat/          IDamageable, WeaponController, WeaponCatalog
    Abilities/       AbilitySystem, AbilityCatalog (10 sorts)
    Enemies/         TrainingDummy, HeadHitbox
    UI/              MainMenuUI, HUD, TutorialUI
    Campaign/        TutorialManager (les 8 étapes)
    Progression/     PlayerProfile (niveaux, stats, sauvegarde)
Documentation/       GDD, direction artistique, sorts, armes, roadmap
```

**Architecture clé** : les scènes ne contiennent aucun GameObject. Au lancement,
`GameManager` (via `[RuntimeInitializeOnLoadMethod]`) détecte la scène active et construit
tout le contenu par code. C'est ce qui rend le projet robuste et immédiatement fonctionnel.

---

## 🧭 Suite du plan

Voir `Documentation/ROADMAP.md` pour la feuille de route complète (stand de tir, missions de
combat, économie, multijoueur, ELO). Bon dev, agent !
