# Sorts & Armes (données de `AbilityCatalog.cs` et `WeaponCatalog.cs`)

## Les 10 sorts (on en équipe 3 par partie — E / F / C)

| # | Nom | Type | Élément | CD | Charges | Dégâts | Effet |
|---|---|---|---|---|---|---|---|
| 1 | Trait de Feu | basique | Feu | 8 s | 2 | 45 | Projectile + brûlure *(DamageBurst)* |
| 2 | Mur de Glace | basique | Glace | 14 s | 1 | — | Bloque vue/balles *(Wall — placeholder)* |
| 3 | Décharge Foudre | basique | Foudre | 10 s | 2 | 30 | Dash traversant + ralenti *(Dash)* |
| 4 | Nuage Toxique | basique | Poison | 12 s | 1 | 10/s | Zone de dégâts *(Zone — placeholder)* |
| 5 | Rafale de Vent | basique | Vent | 9 s | 2 | — | Repousse + interrompt *(Knockback)* |
| 6 | Bouclier de Lumière | signature | Lumière | 20 s | 1 | — | +60 PV de bouclier *(Shield)* |
| 7 | Voile d'Ombre | signature | Ombre | 18 s | 2 | — | Nuage bloque-vision *(Smoke — placeholder)* |
| 8 | Pic de Terre | signature | Terre | 16 s | 1 | 20 | Immobilise *(DamageBurst)* |
| 9 | Tempête Arcanique | **ultime** | Arcane | 120 s | 1 | 120 | Vortex de zone |
| 10 | Jugement Solaire | **ultime** | Solaire | 150 s | 1 | 150 | Rayon vertical |

**Loadout par défaut (tutoriel)** : Trait de Feu (E) · Décharge Foudre (F) · Bouclier de Lumière (C).

> *Placeholder* = l'effet complet (mur physique, fumée occlusive, zone persistante) demande des
> systèmes dédiés ; le prototype affiche un volume/marqueur temporaire à la place. Dégâts, dash,
> soin et bouclier sont **pleinement fonctionnels**.

## Armes de départ

| Nom | Catégorie | Dégâts | Cadence (t/s) | Chargeur | Recharge | Portée |
|---|---|---|---|---|---|---|
| Éclat | Pistolet | 26 | 6,5 | 13 | 1,75 s | 40 m |
| Guêpe | Mitraillette | 23 | 13 | 30 | 2,2 s | 22 m |
| Rempart | Fusil d'assaut | 39 | 9,5 | 25 | 2,5 s | 50 m |
| Broyeur | Fusil à pompe | 66 | 1,1 | 5 | 2,6 s | 9 m |
| Longue-Vue | Fusil de précision | 120 | 0,7 | 5 | 3,7 s | 65 m |

Arme de départ du tutoriel : **Éclat**. Headshot = **×2** dégâts sur toutes les armes.

## Équipement (phase 2)
Plastron léger (+25 bouclier) · Plastron lourd (+50, −8 % vitesse) · Fumigène · Grenade aveuglante ·
Drone de reconnaissance.
