# NEON PROTOCOL — Backlog (audit Product Owner)

> Escouade multi-agents (1 PO + 6 devs), chantier « finition » du 2026-07-07.
> Statut par épic : ✅ fait · 🟡 partiel · ⬜ à faire. (Vérifié contre le code réel.)

## Vision

NEON PROTOCOL est aujourd'hui un socle FPS tactique solide et jouable (tir hitscan, 10 sorts, 4 agents, missions bots, progression/ELO, LAN 1v1) mais qui reste au stade "prototype fonctionnel" : il lui manque la couche de LISIBILITÉ/feedback qui fait qu'un affrontement se lit instantanément (kill feed, damage numbers, direction des dégâts), l'IDENTITÉ réelle des agents (les passives ne sont que du texte, non codées), la profondeur d'ARMEMENT (pas de switch d'arme ni de recul en combat), des OPTIONS dignes d'un vrai jeu (pas d'écran options au menu principal, pas de keybinds/plein écran/résolution), et surtout un MULTI qui n'est encore qu'une démo de déplacement (effets de sorts non répliqués, dégâts de zone/knockback inopérants contre un joueur réseau, aucune boucle de manches ni achat synchronisé, LAN uniquement). Le cap "100% fonctionnel" = rendre chaque système déjà présent complet, lisible et équilibré en solo, puis hisser le multi au rang de vraie partie compétitive (manches + achat + online).

## Épics

### E1 — Feedback & lisibilité de combat (solo)  ·  priorité 1  ·  🟡 PARTIEL

*Le combat fonctionne mais ne se LIT pas : hitmarker + popup 'ÉLIMINÉ' existent, mais aucun kill feed, aucun chiffre de dégâts, aucune indication de la direction d'où l'on se fait tirer dessus, et le rechargement n'a aucun retour visuel. C'est le chantier n°1 pour passer de prototype à jeu.*

**État :** Kill feed + damage numbers FAITS. Reste : indicateur directionnel de dégâts, jauge de rechargement, munitions basses en rouge, flash de cast.

- **Kill feed FR (journal d'éliminations)** _(effort M)_ — Nouvelle classe FirstGame.UI.KillFeed (canvas coin haut-droit, construite via UIFactory). S'abonner à WeaponController.OnKill et à EnemyBot.OnDied (déjà exposés) ainsi qu'à NetDamageRelay.Kills en réseau. Afficher des lignes 'VOUS ▸ [arme] ▸ Bot Vétéran', couleur selon tête/corps, disparaissant après ~5s, 5 lignes max empilées.
  - ✅ *Acceptation :* Éliminer un bot ajoute une ligne nommant tueur, arme et victime ; la ligne s'efface après ~5s ; fonctionne en CombatArena ET en partie réseau ; jamais plus de 5 lignes.
- **Damage numbers flottants** _(effort M)_ — FirstGame.UI.DamageNumbers en world-space, branché sur WeaponController.OnHit(target,dmg,headshot) et AbilitySystem.OnAbilityHit. Spawn d'un Text montant à hit.point ; têtes en doré + plus gros. Pooler les objets pour éviter le GC par tir.
  - ✅ *Acceptation :* Chaque impact fait monter un chiffre à la position monde touchée ; les headshots sont dorés/plus grands ; le chiffre disparaît en ~0,8s ; aucune saccade due aux allocations en tir soutenu.
- **Indicateur directionnel de dégâts** _(effort M)_ — Étendre la vignette rouge du HUD : ajouter des arcs autour du réticule pointant vers l'attaquant. PlayerHealth.TakeDamage transmet déjà hitPoint ; passer aussi la position de la source (Fire du EnemyBot appelle TakeDamage) pour calculer l'angle relatif à la caméra et afficher un arc ~1s.
  - ✅ *Acceptation :* Se faire toucher par un bot affiche un arc rouge dans sa direction pendant ~1s, correct à ±45°.
- **Feedback de rechargement & munitions basses** _(effort S)_ — HUD : sur WeaponController.OnReloadStart afficher une jauge circulaire 'RECHARGEMENT' calée sur weapon.reloadSeconds ; passer le texte de munitions en rouge sous 25% ; afficher un prompt '[R]' quand le chargeur est vide.
  - ✅ *Acceptation :* Recharger montre un indicateur qui se remplit sur la durée réelle ; le compteur de munitions vire au rouge sous le seuil ; chargeur vide affiche le prompt [R].
- **Feedback de cast de sort** _(effort S)_ — Sur AbilitySystem.OnAbilityUsed : flasher le slot HUD concerné, jouer un SFX (ProceduralAudio.Ability) et un léger kick caméra via CameraShake. Ajouter un 'blip' de refus quand on presse E/F/C sans charge.
  - ✅ *Acceptation :* Lancer E/F/C flashe son slot et joue un son ; tenter sans charge produit un refus audible/visuel sans consommer de cast.

### E2 — Passives d'agents & ennemis-agents  ·  priorité 1  ·  ✅ FAIT

*Les 4 agents ont un passiveFr rédigé (Élan Ardent, Emprise, Onde de Choc, Garde) mais AUCUN n'est codé (grep : seules des chaînes existent). C'est le cœur de l'identité 'type Valorant' et c'est actuellement du vent. En parallèle, les bots ne lancent jamais de sorts : les rendre 'agents' crédibilise le solo.*

**État :** 4 passives d'agents + ennemis-agents lanceurs de sorts.

- **Système de passif d'agent** _(effort M)_ — Nouveau FirstGame.Agents.AgentPassiveSystem (MonoBehaviour) ajouté dans PlayerRig.Assemble, lisant AgentCatalog.ById(PlayerProfile.Current.agentId) et activant la logique correspondante. Point d'entrée unique pour brancher les 4 passives.
  - ✅ *Acceptation :* Le bon comportement de passif est actif selon l'agent choisi ; changer d'agent dans le menu change la passive à la partie suivante.
- **Brasier — Élan Ardent** _(effort S)_ — Sur WeaponController.OnKill : recharger +1 charge du sort Décharge Foudre (slot correspondant de AbilitySystem) et appliquer +12% de célérité 3s via un multiplicateur sur FirstPersonController.
  - ✅ *Acceptation :* Après un kill en Brasier, la charge de dash augmente de 1 (plafonnée) et la vitesse +12% pendant 3s puis revient à la normale.
- **Nocturne — Emprise (+25% durée des zones)** _(effort S)_ — Paramétrer les Destroy(go, 8f/12f) codés en dur dans AbilitySystem.SpawnWall/SpawnSmoke et ToxicZone.life ; multiplier par 1,25 quand l'agent est Nocturne.
  - ✅ *Acceptation :* En Nocturne, mur de glace 10s (au lieu de 8), fumée 15s, zone toxique 7,5s ; durées inchangées pour les autres agents.
- **Faille — Onde de Choc (révélation)** _(effort S)_ — Quand Pic de Terre ou Rafale de Vent touche un ennemi (AbilitySystem.OnAbilityHit), attacher un marqueur de révélation 2s au-dessus de la cible (réutiliser le style ReconDrone.Marker).
  - ✅ *Acceptation :* Toucher un bot avec ces deux sorts en Faille affiche un marqueur au-dessus de lui pendant 2s.
- **Rempart — Garde (regen HC + bouclier renforcé)** _(effort S)_ — Régen 8 PV/s hors combat (réutiliser le chemin de régen de PlayerHealth avec override de _regenPerSecond) et Bouclier de Lumière rend 40 PV au lieu de 25 (aujourd'hui AddShield(60) fixe dans Execute).
  - ✅ *Acceptation :* En Rempart, les PV remontent de 8/s après 5s sans dégâts ; le sort bouclier octroie la valeur renforcée.
- **Ennemis-agents (bots lanceurs de sorts)** _(effort L)_ — Refactorer les spawns de volumes de AbilitySystem dans un helper statique partagé (AbilityFx), puis donner à EnemyBot un agentId optionnel et une IA simple qui cast périodiquement fumée/mur/zone vers le joueur ; l'Elite déclenche un ultime.
  - ✅ *Acceptation :* Un bot marqué 'agent' lance au moins un sort non-hitscan avec effet visible pendant un combat, sans erreur quand le joueur se met à couvert.

### E3 — Armement in-game : switch, reload animé, recul  ·  priorité 2  ·  🟡 PARTIEL

*En combat réel le joueur est bloqué sur UNE arme (le switch 1-5 n'existe que dans PracticeRange via WeaponSwitcher), le rechargement n'est qu'un timer sans animation, et il n'y a ni recul ni dispersion — du coup la stat 'controle' du profil n'a aucun effet sur la visée. Il manque la profondeur d'armement de base d'un FPS.*

**État :** ADS/FOV live faits. Reste : switch d'arme (1/2/molette), animation de rechargement, recul/dispersion pilotés par 'controle'.

- **Deux armes équipées + switch (1/2 & molette)** _(effort M)_ — Ajouter PlayerProfile.secondaryWeaponId ; PlayerRig gère une arme primaire + secondaire ; attacher WeaponSwitcher au rig de combat (pas seulement PracticeRange) avec touches 1/2 et molette souris ; munitions suivies par arme.
  - ✅ *Acceptation :* En CombatArena on alterne entre deux armes du loadout via 1/2 et molette ; le nom d'arme du HUD se met à jour ; les munitions sont mémorisées par arme.
- **Animation de rechargement (viewmodel + perso)** _(effort M)_ — Animer le viewmodel (plongée/rotation) sur weapon.reloadSeconds dans GameFeel ou un nouveau ViewmodelAnimator ; ajouter CharacterVisual.Reload() pour la 3e personne (bots/adversaire).
  - ✅ *Acceptation :* Recharger anime visiblement le viewmodel sur toute la durée puis revient en idle ; les personnages distants jouent un mouvement de rechargement.
- **Recul & dispersion (spread) branchés sur 'controle'** _(effort M)_ — WeaponController applique un cône de dispersion par tir combinant l'arme et PlayerProfile.SpreadMultiplier (stat controle, aujourd'hui inutilisée à la visée) + un kick caméra ; récupération au relâchement.
  - ✅ *Acceptation :* Le tir soutenu élargit visiblement le cône et fait monter la caméra ; investir 'controle' resserre mesurablement le cône ; le tir au coup par coup reste précis.
- **Switch d'arme répliqué en réseau** _(effort S)_ — Répliquer l'index d'arme courant via NetworkVariable pour que le viewmodel/personnage distant corresponde.
  - ✅ *Acceptation :* En partie réseau, l'arme tenue par l'adversaire reflète ses changements.

### E4 — Répliquer les effets de sorts en réseau  ·  priorité 2  ·  ⬜ À FAIRE

*Limite connue majeure : mur de glace, fumigène et zone toxique sont des GameObject.CreatePrimitive LOCAUX (jamais répliqués). Pire, DoKnockback et ToxicZone ne ciblent que TrainingDummy/EnemyBot : en 1v1 réseau ils n'affectent PAS l'adversaire (NetDamageRelay/IDamageable). Le kit tactique est donc inopérant en multi.*

**État :** Répliquer mur/fumée/zone + knockback/zone contre joueurs réseau (non fait).

- **Volumes de sorts en NetworkObject (mur/fumée/zone)** _(effort L)_ — Extraire les spawns de AbilitySystem.SpawnWall/SpawnSmoke/SpawnZone (et SmokeVolume/ReconDrone d'équipement) vers un spawner partagé, en versions prefab réseau instanciées via ServerRpc et enregistrées dans NetworkManager ; conserver le chemin local pour le solo.
  - ✅ *Acceptation :* En 1v1 réseau, quand un joueur pose mur/fumée/zone, l'autre voit le même volume à la même position ; comportement solo inchangé.
- **Dégâts de zone & knockback contre joueurs réseau** _(effort M)_ — Faire que DoKnockback et ToxicZone ciblent aussi IDamageable (NetDamageRelay), pas seulement TrainingDummy/EnemyBot ; router le knockback via RPC vers le propriétaire cible.
  - ✅ *Acceptation :* La zone toxique inflige des dégâts périodiques à l'adversaire réseau ; la rafale de vent le repousse ; les kills passent bien par NetDamageRelay.
- **Soin/bouclier & VFX de cast visibles à distance** _(effort M)_ — Répliquer le VFX de cast via ClientRpc pour que l'adversaire voie vos sorts, et refléter le gain de bouclier/soin sur l'affichage réseau (NetHp déjà présent, ajouter NetShield).
  - ✅ *Acceptation :* L'adversaire voit le VFX de cast et la barre de bouclier/PV réseau reflète soins et boucliers.

### E5 — Options & UX complètes (keybinds, plein écran, graphismes)  ·  priorité 2  ·  ✅ FAIT

*Le MENU PRINCIPAL n'a AUCUN écran d'options (grep confirmé : seules deux options — sensibilité, volume — existent, et uniquement dans le PauseMenu). Aucune remappe de touches (KeyCodes en dur partout), aucun réglage plein écran/résolution/qualité/FOV. C'est un manque bloquant pour un 'vrai jeu'.*

**État :** Écran Options (menu + pause), remappage des touches, plein écran/résolution/qualité/FOV, volumes.

- **Écran Options au menu principal** _(effort M)_ — Extraire les OptionRow du PauseMenu vers un builder partagé OptionsPanel ; ajouter un bouton OPTIONS dans MainMenuUI qui l'ouvre (sensibilité + volume + réglages ci-dessous).
  - ✅ *Acceptation :* Le menu principal a un bouton OPTIONS ouvrant un panneau avec sensibilité, volume et les nouveaux réglages ; les valeurs persistent.
- **Remappage des touches (keybinds)** _(effort L)_ — Nouvelle classe FirstGame.Core.Keybinds (PlayerPrefs) pour déplacement/saut/tir/reload/E-F-C/G ; FirstPersonController, WeaponController, AbilitySystem et UtilityController lisent depuis Keybinds au lieu des KeyCode en dur ; UI de rebind qui capture la prochaine touche.
  - ✅ *Acceptation :* Rebinder par ex. le rechargement prend effet immédiatement et persiste ; E/F/C et G sont remappables ; pas de conflit silencieux.
- **Graphismes, plein écran & résolution** _(effort M)_ — Options plein écran (Screen.fullScreenMode), liste de résolutions (Screen.resolutions), qualité (QualitySettings), curseur FOV (caméra), VSync ; persistance via Settings.
  - ✅ *Acceptation :* Basculer en plein écran et choisir une résolution s'applique immédiatement et survit au redémarrage ; le curseur FOV modifie la caméra en jeu.
- **Sensibilité avancée & réinitialisation** _(effort S)_ — Ajouter un multiplicateur de sensibilité ADS, une inversion de l'axe Y et un bouton 'Réinitialiser les réglages'.
  - ✅ *Acceptation :* L'inversion Y inverse la visée verticale ; le reset restaure les valeurs par défaut.

### E6 — Contenu : maps & modes de jeu  ·  priorité 3  ·  🟡 PARTIEL

*Une seule arène (LevelBuilder) et trois missions (Duel/Round/Examen) + Classé. Peu de rejouabilité. Ajouter des cartes et un mode 'pose/désamorçage' style Valorant densifie fortement le solo.*

**État :** 2e layout d'arène (Alpha/Beta) + stand d'entraînement chronométré (touche T). Reste : nouvelles maps/modes.

- **Deuxième arène** _(effort L)_ — LevelBuilder ne construit qu'une arène ; ajouter une variante de layout sélectionnable par mission, en réutilisant le kit Sci-Fi Modular.
  - ✅ *Acceptation :* Au moins un layout d'arène supplémentaire distinct est jouable en mission ; les deux se chargent sans erreur.
- **Mode Pose / Désamorçage (spike)** _(effort L)_ — Nouveau type de mission dans CombatMissionManager : poser sur un site, timer, désamorçage ; réutiliser la barre de capture existante pour l'état/timer.
  - ✅ *Acceptation :* Une manche pose/désamorçage se gagne par pose+timer ou par désamorçage/éliminations ; l'UI montre le timer et l'état.
- **Sélecteur de difficulté & modificateurs** _(effort S)_ — Réglage de difficulté modulant tier/précision des bots ; exposé dans le sélecteur de CombatArena.
  - ✅ *Acceptation :* Choisir une difficulté change mesurablement les stats des bots ; enregistré comme préférence.
- **Practice Range : épreuve chronométrée** _(effort M)_ — Ajouter un mode 'drill' de précision chronométré à PracticeRangeScene avec score + meilleur temps sauvegardé.
  - ✅ *Acceptation :* Le joueur lance une épreuve, obtient un score et voit son meilleur score sauvegardé.

### E7 — Boucle de manches multi + achat réseau  ·  priorité 3  ·  🟡 À VÉRIFIER

*Le multi est aujourd'hui un 1v1 'à mort' sans structure : pas de manches, pas de score de partie, pas de phase d'achat synchronisée (BuyMenuUI n'est utilisé qu'en solo). Sans boucle de manches, ce n'est pas 'une partie'.*

**État :** MatchConfig + réglages de manche ajoutés. Boucle best-of réseau + achat synchronisé : à confirmer au test / finir.

- **Boucle de manches réseau (best-of-N, serveur-auth)** _(effort L)_ — Nouveau FirstGame.Net.NetRoundManager (autorité serveur) comptant les manches gagnées (ex. jusqu'à 7) ; gel/réinitialisation des joueurs entre manches ; écran de fin de match. Le NetScoreboard reflète les manches.
  - ✅ *Acceptation :* Une partie réseau enchaîne plusieurs manches, réinitialise positions/PV entre elles, se termine quand un joueur atteint la cible avec un écran de résultat.
- **Phase d'achat synchronisée** _(effort L)_ — Réutiliser BuyMenuUI en début de manche sur le réseau ; chaque joueur achète avec une économie synchronisée (crédits en NetworkVariable, récompenses de kill/manche).
  - ✅ *Acceptation :* Les deux joueurs ont une phase d'achat avant chaque manche ; les achats s'appliquent à leur propre rig ; les crédits évoluent selon kills/résultat.
- **Économie de manche** _(effort M)_ — Economy : crédits par manche, bonus de défaite, récompense de kill ; persistance au fil des manches d'une même partie.
  - ✅ *Acceptation :* Les crédits se reportent entre manches et varient selon les résultats.

### E8 — Équilibrage  ·  priorité 3  ·  ⬜ À FAIRE

*Les valeurs d'armes/sorts/bots/ELO viennent d'une passe de design mais n'ont pas été validées en jeu (limite connue : 'équilibrage non passé'). Un jeu fonctionnel a besoin d'un TTK cohérent et de sorts qui ne surclassent pas le tir.*

**État :** Équilibrage.

- **Passe TTK armes vs paliers de bots** _(effort M)_ — Tabuler le nombre de balles pour tuer chaque arme de WeaponCatalog contre 100 PV+bouclier et contre les PV des bots ; ajuster damage/fireRate/dispersion vers un TTK cible ; documenter.
  - ✅ *Acceptation :* Une table d'équilibrage existe et les valeurs sont ajustées : le sniper one-shot tête seulement, le pompe uniquement à courte portée, aucune arme ne domine trivialement — vérifié en jeu.
- **Équilibrage des sorts** _(effort M)_ — Revoir cooldowns/dégâts/charges de AbilityCatalog ; s'assurer que les ultimes sont à la hauteur et que les basiques ne surpassent pas un chargeur d'arme.
  - ✅ *Acceptation :* Passe de tuning documentée ; aucun sort n'inflige gratuitement plus qu'un chargeur complet ; cadence d'ultime raisonnable.
- **Courbe ELO & appariement bots** _(effort S)_ — Valider les K-factors de PlayerProfile.ApplyMatchResult et les ratings/tiers d'adversaire de RunRanked ; vérifier le ressenti du placement.
  - ✅ *Acceptation :* Des séquences simulées de victoires/défaites font évoluer l'ELO de façon cohérente à travers les paliers, sans sauts dégénérés.
- **Effet réel des 6 stats** _(effort S)_ — La stat 'controle' n'a aucun effet à la visée tant que le système de dispersion (E3) n'existe pas ; la brancher et vérifier que les 6 stats ont un effet mesurable et plafonné.
  - ✅ *Acceptation :* Chacune des 6 stats produit un effet mesurable en jeu ; les plafonds sont respectés.

### E9 — Robustesse réseau & anti-triche  ·  priorité 3  ·  ⬜ À FAIRE

*NetDamageRelay.SubmitDamageServerRpc rediffuse simplement le dégât sans validation (pas d'autorité serveur réelle, dégâts appliqués de façon optimiste côté tireur) : un client peut mentir. Aucune gestion propre de déconnexion. Nécessaire pour un multi crédible.*

**État :** Robustesse réseau & anti-triche.

- **Autorité serveur sur les dégâts** _(effort L)_ — Déplacer l'autorité des PV sur le serveur : SubmitDamageServerRpc valide (tireur vivant, portée d'arme plausible) puis applique côté serveur et réplique, au lieu de rediffuser aveuglément.
  - ✅ *Acceptation :* Un client ne peut pas infliger de dégâts alors qu'il est mort ou hors portée d'arme ; les variations de PV proviennent du serveur.
- **Spawn/respawn serveur-auth** _(effort M)_ — Points de spawn et respawn gérés par NetRoundManager plutôt que par un téléport côté owner ; corriger les chevauchements de spawn.
  - ✅ *Acceptation :* Les joueurs réapparaissent toujours à des points valides et non superposés, contrôlés par le serveur.
- **Déconnexion propre / hôte parti** _(effort M)_ — Gérer la déconnexion d'un client / le départ de l'hôte : message FR clair et retour au menu sans crash.
  - ✅ *Acceptation :* Si un pair se déconnecte, l'autre voit un message FR clair et revient au menu sans plantage.

### E10 — Audio & musique  ·  priorité 4  ·  ⬜ À FAIRE

*L'audio procédural couvre tir/reload/hit/kill/pas/musique mais reste générique : mêmes sons pour tous les sorts, pas d'annonceur, pas d'ambiance, pas de mix séparé musique/SFX. L'audio porte énormément le 'game feel'.*

**État :** Audio & musique.

- **SFX de sorts par élément** _(effort M)_ — ProceduralAudio : signatures sonores distinctes par AbilityEffect/élément ; les brancher dans AbilitySystem.Execute.
  - ✅ *Acceptation :* Chaque type de sort a un son reconnaissable au cast et à l'impact.
- **Annonceur FR** _(effort M)_ — Répliques d'annonceur (voix synthétique ou courts clips) pour début/victoire/défaite de manche, 'PREMIÈRE ÉLIMINATION', multi-kills.
  - ✅ *Acceptation :* Les événements clés déclenchent une réplique FR sans se chevaucher en spam.
- **Ambiance & mix séparé** _(effort S)_ — Nappe d'ambiance d'arène ; volumes indépendants Musique/SFX reliés à MasterVolume.
  - ✅ *Acceptation :* Musique et SFX ont des curseurs indépendants ; une boucle d'ambiance joue dans les arènes.

### E12 — Onboarding, progression & polish menus  ·  priorité 4  ·  ⬜ À FAIRE

*Une fois les passives réelles (E2), il faut les montrer ; l'écran de fin de match est minimal (titre + XP) ; la sauvegarde gère mal les migrations. Ces finitions rendent la progression lisible et durable.*

**État :** Onboarding & polish menus.

- **Fiches d'agents & prévisualisation** _(effort M)_ — Menu AGENTS : afficher la passive (désormais réelle), les 3 sorts, le rôle et le stat bias avec un aperçu 3D ; la sélection persiste.
  - ✅ *Acceptation :* Sélectionner un agent montre sa passive/ses sorts réels et met à jour le loadout.
- **Écran de fin de match détaillé** _(effort M)_ — Statistiques post-match : kills/morts/dégâts/% headshot, détail de l'XP ; réutilisable en solo et en multi.
  - ✅ *Acceptation :* L'écran de fin affiche les stats de combat de la partie et l'XP gagnée.
- **Sauvegarde robuste & reset profil** _(effort S)_ — PlayerProfile : sauvegarde versionnée/migration, export/reset depuis les Options, garde-fous contre les saves corrompues (déjà partiel via try/catch).
  - ✅ *Acceptation :* Les sauvegardes corrompues ou anciennes se chargent sans perte quand c'est possible ; l'utilisateur peut réinitialiser son profil depuis les Options.

### E11 — Matchmaking / Lobby / Online (Relay)  ·  priorité 5  ·  ⬜ À FAIRE

*Le multi est LAN uniquement (127.0.0.1, IP manuelle). Pour jouer à distance il faut Unity Relay + Authentication, et un lobby/matchmaking pour trouver des adversaires. C'est l'extension online de fond, mais lourde — donc secondaire par rapport à rendre le 1v1 complet d'abord.*

**État :** Matchmaking/Lobby/Online (Relay).

- **Online via Unity Relay (code de partie)** _(effort L)_ — Intégrer Unity Relay + Authentication ; NetSession propose 'Créer une partie' (allocation → code) et 'Rejoindre' (saisie du code) en plus de l'IP LAN.
  - ✅ *Acceptation :* Deux machines sur des réseaux différents se connectent via un code de partie et jouent un 1v1.
- **Lobby & file d'attente** _(effort L)_ — Service Unity Lobby : créer/lister/rejoindre des lobbies ; matchmaking simple par tranche d'ELO.
  - ✅ *Acceptation :* Un joueur peut créer ou rejoindre un lobby et être placé dans une partie.
- **Profil/compte en ligne léger** _(effort M)_ — Utiliser les IDs anonymes d'Authentication pour transporter ELO/agent au-delà du PlayerPrefs local.
  - ✅ *Acceptation :* ELO/agent suivent le compte entre sessions lorsqu'on est connecté.

