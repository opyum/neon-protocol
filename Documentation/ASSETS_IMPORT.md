# Assets 3D — vrais modèles (Kenney, Mixamo)

> **Statut : premiers vrais assets en place.** Fini les capsules : les ennemis/mannequins utilisent
> un **personnage humanoïde** (Kenney Blocky Characters) et le joueur tient un **vrai modèle d'arme**
> (Kenney Blaster Kit). Tout est **CC0** (libre, sans attribution obligatoire) et inclus dans le dépôt.

## Comment ça marche (architecture « asset-driven »)
- `Assets/Resources/GameAssets.asset` = la config. Elle référence les prefabs :
  `enemyCharacterPrefab` (personnage) et `weaponViewmodels` (une arme par id d'arme).
- Le code (`PlayerRig`, `EnemyBot`, `TrainingDummy`) **instancie ces prefabs s'ils sont assignés**,
  sinon il retombe sur les primitives. Tu peux donc remplacer n'importe quel modèle sans toucher au code.
- `ModelUtil` **auto-dimensionne** chaque modèle par ses bounds (perso → 1,8 m ; arme → 0,5 m), donc
  peu importe l'échelle d'import du FBX. Les matériaux sont **re-teintés** par `ArtPalette` (ennemis en
  rouge pour la lisibilité, arme en métal) — d'où un rendu « couleur unie » voulu, pas la texture Kenney.

## Ce qui est fait / ce qui reste
- ✅ Modèles de **personnages** (ennemis, mannequins) et d'**armes** (viewmodel 1re personne).
- ⏳ **Animations** : les persos Kenney sont riggés mais sans Animator Controller ici → ils sont **statiques**
  pour l'instant. Pour les animer : voir *Mixamo* ci-dessous. Le code appelle déjà `SetSpeed/Shoot/Die`
  (paramètres d'Animator « Speed » float, « Shoot »/« Die » triggers) — il suffit de fournir le Controller.
- ⏳ **Level design** : le pack **Kenney Prototype Kit** (145 pièces modulaires) est téléchargé et prêt à
  être intégré pour une vraie map (prochaine étape).
- 🎨 Le viewmodel de l'arme peut nécessiter un ajustement de **position/rotation** (réglé dans `PlayerRig`,
  bloc « Viewmodel ») — dis-moi ce que tu vois et je cale les valeurs.

## Ajouter / changer des modèles
1. Dépose ton FBX dans `Assets/Art/Characters/` ou `Assets/Art/Weapons/`.
2. Menu **NEON ▸ Configurer les assets** (script `Assets/Editor/AssetSetup.cs`) — il (ré)assigne
   `character-a.fbx` comme ennemi et `blaster-a..e` aux 5 armes. Édite ce script pour pointer d'autres fichiers,
   OU ouvre `Assets/Resources/GameAssets.asset` dans l'Inspector et glisse tes prefabs dans les slots.

## Animer les personnages avec Mixamo (gratuit)
1. Va sur **mixamo.com** (compte Adobe gratuit — c'est la seule étape que je ne peux pas automatiser).
2. Upload un perso (ou prends-en un du catalogue), télécharge les animations **Idle, Walk/Run, Shoot, Death**
   en **FBX for Unity**.
3. Dans Unity : sur le FBX, onglet *Rig* → Animation Type **Humanoid**. Crée un **Animator Controller** avec
   un paramètre float `Speed` (blend Idle↔Run) et des triggers `Shoot` / `Die`, assigne-le au prefab, et
   mets ce prefab dans `GameAssets.enemyCharacterPrefab`.
4. Dis-moi quand c'est importé — je câble le blend tree et le retargeting proprement.

## Sources gratuites (CC0)
- **Kenney.nl** — armes, persos, kits de niveau (utilisés ici).
- **Quaternius.com** — persos/armes/nature low-poly.
- **Mixamo (Adobe)** — persos humanoïdes **riggés + animés** (nécessite un compte).

Licences CC0 incluses dans `Assets/Art/*/License.txt`.
