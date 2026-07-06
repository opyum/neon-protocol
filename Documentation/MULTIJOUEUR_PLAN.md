# NEON PROTOCOL — Multijoueur & Classement (plan)

Ce document est le **plan d'architecture réseau**. La partie **ELO / Classé contre bots est déjà implémentée et jouable** (voir plus bas). Le **vrai netcode** est décrit ici comme feuille de route : il exige un test à 2 clients et n'a donc pas été codé « à l'aveugle ».

## Recommandation techno : Netcode for GameObjects (NGO)

- **Officiel Unity 6.1**, gratuit, intégré, énormément de docs/tutos. Colle au style « tout par code » du projet.
- API proche des events C# déjà utilisés (`NetworkBehaviour`, `[Rpc]`/`ServerRpc`, `NetworkVariable<T>`, `NetworkTransform`).
- **Multiplayer Play Mode (MPPM)** : lance 2-4 instances de l'éditeur sur **une seule machine** pour tester « 2 joueurs » sans 2e PC. Décisif en solo.
- **Unity Relay + Lobby** (quota gratuit) pour passer du LAN à l'online plus tard, sans ouvrir de ports.

**Pas Photon pour l'instant** : PUN2 est client-autoritaire (triche triviale sur un FPS). Fusion 2 est supérieur pour le compétitif (prédiction + rollback + lag comp) mais trop abstrait comme premier réseau. Fusion = chemin d'upgrade éventuel, pas le point de départ.

> Honnêteté : NGO n'a pas de lag compensation intégrée. Pour un 1v1 LAN entre amis, non bloquant. Pour de l'online public compétitif, il faudra coder une lag comp ou migrer vers Fusion — **pas avant** d'avoir un 1v1 LAN qui tourne.

## Architecture (mouvement owner-autoritaire, combat serveur-autoritaire)

Règle d'or : **tout ce qui décide qui meurt** (raycast dégâts, PV, charges, respawn, score) = **serveur**. Tout ce qui est cosmétique (tracer, muzzle, shake, VFX, audio) = local/ClientRpc. Le HUD lit des `NetworkVariable`, il ne calcule rien.

- **PlayerHealth** → `NetworkBehaviour`. `Health/Shield` deviennent `NetworkVariable<float>` (écriture serveur). `TakeDamage/Heal/AddShield` uniquement si `IsServer`. Les events actuels se rebranchent sur `OnValueChanged` → **le HUD marche sans changement**.
- **WeaponController** → tir local pour le feel, mais **raycast rejoué côté serveur** : `FireServerRpc(origin, dir)`, le serveur refait le raycast contre les positions serveur, valide le headshot, applique les dégâts. Ne jamais envoyer « j'ai touché X pour Y ».
- **AbilitySystem** → autorité serveur sur charges/cooldowns : `CastServerRpc(slot, aim…)`. Les volumes (mur de glace, fumigène, zone) deviennent des `NetworkObject` spawnés par le serveur.
- **FirstPersonController** → `NetworkObject` + `NetworkTransform` owner-authoritative. **Garde `IsOwner`** sur caméra/audio/input dans `OnNetworkSpawn` (sinon bug classique : toutes les caméras bougent ensemble).
- **Loadout** → un `MatchManager` (host) envoie à chaque client son loadout résolu par **id** (les catalogues sont déjà des lookups par id, parfaits pour le réseau). Ne jamais lire le `PlayerProfile` de l'adversaire côté serveur.

## Plan par phases

- **Phase 0 — OFFLINE (FAIT ✅)** : ELO dans `PlayerProfile` + mode **Classé contre bots**. Testable seul, valide la formule ELO avant tout réseau.
- **Phase 1 — NGO 1v1 LAN** : installer `com.unity.netcode.gameobjects` + `com.unity.multiplayer.playmode`. `NetworkManager` (UnityTransport). 2 `PlayerRig` réseau, garde `IsOwner`, `NetworkTransform`. Objectif : **voir l'autre bouger**. Aucun combat.
- **Phase 2 — Combat serveur-autoritaire** : `PlayerHealth` en `NetworkBehaviour`, `WeaponController.FireServerRpc` (re-raycast serveur), hit-marker via `ClientRpc`. Objectif : se tirer dessus, PV synchro, mort/respawn serveur.
- **Phase 3 — Sorts + manches** : `AbilitySystem.CastServerRpc`, volumes `NetworkObject`, `MatchManager` de manche (score, best-of). En fin de match → **le même `ApplyMatchResult(win, eloAdverse)`** que la Phase 0 → l'ELO devient PvP sans réécrire la formule.
- **Phase 4 — ONLINE** : Unity Relay + Lobby (hors LAN). Puis, si besoin compétitif, lag compensation ou évaluation d'une migration Fusion 2.

## Ce qui exige un test à 2 clients (non codé à l'aveugle)

Réplication du mouvement (interpolation, ownership), round-trip des RPC et latence perçue, synchro des `NetworkVariable`, enregistrement des tirs sous latence (décision lag comp), spawn réseau des volumes de sorts, anti-triche (le client ne peut pas s'auto-soigner), fin de match PvP appliquant l'ELO sur chaque profil local. **Outil : Multiplayer Play Mode** (jusqu'à 4 instances) évite le 2e PC pour la majorité.

## Risques principaux

1. **Scope** : tout convertir d'un coup casserait le solo qui marche. → Garder le solo/classé intact, brancher le réseau derrière un flag, ne convertir qu'après un 1v1 LAN « je vois l'autre bouger ».
2. **Pas de lag comp NGO** : tirs « ratés » sur cible rapide en online. → Rester LAN au début.
3. **Stats serveur** : `PlayerProfile` est local (PlayerPrefs). → Le serveur ne fait autorité que sur la résolution par id ; stats via payload borné au spawn.
4. **Caméra/Input non gardés par `IsOwner`** : bug quasi-certain au 1er essai. → Traiter dès la Phase 1.
5. **Volumes de sorts non répliqués** : mur/zone d'un seul côté. → Tout volume qui affecte le combat = `NetworkObject` serveur.

---

## Classement ELO — IMPLÉMENTÉ ✅

- `PlayerProfile.elo` (départ **1000**), `rankedGames`, `rankedWins`.
- `RankedTier` : Fer < 800, Bronze, Argent, Or, Platine, Émeraude, Diamant, Maître, Champion ≥ 2500.
- `ApplyMatchResult(win, opponentRating)` : K adaptatif (**40** placement < 10 parties, **24**, **16** à 2100+), `E = 1/(1+10^((Radv−R)/400))`, `Δ = round(K·(S−E))`.
- **Mode Classé contre bots** (Missions de combat ▸ CLASSÉ) : duel 1v1, l'adversaire = tier de bot le plus proche de ton ELO (Recrue 700 / Soldat 1000 / Vétéran 1400 / Élite 1900). Victoire → +ELO, défaite → −ELO, delta et palier affichés. La **même** méthode servira au PvP en Phase 3.
