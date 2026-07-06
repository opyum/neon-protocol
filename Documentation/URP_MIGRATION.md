# Migration URP + Bloom

> **Statut : APPLIQUÉE ✅ (compilation + configuration vérifiées).** Le projet est passé en **URP 17.5.0**
> (fourni avec Unity 6.1, aucun téléchargement). Le paquet, l'asset de pipeline (`Assets/Settings/PC_RPAsset`
> + `PC_Renderer`, HDR activé), l'assignation à *Graphics* + aux **6 niveaux de qualité**, le **Bloom**
> (Volume global dans `Env`), le post-process caméra, et les shaders/émissifs HDR sont tous en place et
> **compilent sans erreur** (vérifié en batch sur une copie isolée).
>
> **Il reste UNE chose que je ne peux pas faire à ta place : la validation VISUELLE.** Rouvre le projet
> et regarde chaque scène. Cherche : (a) des matériaux **roses** (shader non résolu), (b) le **bloom** sur
> les néons. Si tout est correct, c'est bon. Réglages fins (intensité du bloom, exposition) = à l'œil.
>
> ### ⚠️ Important — rouvre le projet
> Comme ton éditeur était ouvert pendant la migration, **ferme et rouvre le projet** dans Unity : au
> chargement, Unity résout URP et applique le pipeline. La première ouverture importe les shaders URP
> (peut prendre 1-2 min).
>
> ### Si un matériau apparaît rose
> Menu **NEON ▸ Configurer URP** (script `Assets/Editor/UrpSetup.cs`) — il (re)crée et (ré)assigne le
> pipeline à Graphics + tous les niveaux de qualité. Puis vérifie *Project Settings → Quality* : chaque
> niveau doit avoir le *Render Pipeline Asset* = `PC_RPAsset`.

---

## Détail de ce qui a été fait (référence)

## Pourquoi
Le Built-in RP n'a pas de bloom natif ; les néons sont approximés par des halos additifs
(`MakeGlow` / `NeonGlowSphere`). URP + Bloom donne le vrai glow « NEON PROTOCOL ».

## Étapes
1. **Paquet** : ajouter `com.unity.render-pipelines.universal` (version 17.x alignée Unity 6.1) dans
   `Packages/manifest.json`. Laisser Unity résoudre au refocus. Vérifier qu'aucune erreur n'apparaît.
2. **URP Asset + Renderer** : Project → Create → Rendering → *URP Asset (with Universal Renderer)*.
   Sur l'asset : **cocher HDR** (obligatoire pour le bloom) et *Post-processing*.
3. **Assignation** : Project Settings → Graphics → *Scriptable Render Pipeline Settings* = ton URP Asset.
   Puis Project Settings → **Quality → pour chaque niveau**, *Render Pipeline Asset* = ton URP Asset
   (sinon certains niveaux rendent en rose).
4. **Shaders runtime** : déjà géré dans `ArtPalette.cs` — il choisit `Universal Render Pipeline/Lit`
   quand `GraphicsSettings.currentRenderPipeline != null`, et mappe `_Smoothness` au lieu de `_Glossiness`.
   Rien à faire, sauf vérifier `MakeGlow` (voir risques).
5. **Post-process caméra** : les caméras sont créées en code (`PlayerRig`, `MainMenuScene`). Ajouter
   après création : `cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;`
   (`using UnityEngine.Rendering.Universal;`). Sans ça, aucun bloom.
6. **Volume + Bloom** (par code, dans `Env.SetupStylized`) :
   ```csharp
   var vGo = new GameObject("[PostFX]");
   var vol = vGo.AddComponent<Volume>(); vol.isGlobal = true;
   var profile = ScriptableObject.CreateInstance<VolumeProfile>(); vol.profile = profile;
   var bloom = profile.Add<Bloom>(true);
   bloom.threshold.Override(0.9f); bloom.intensity.Override(0.9f);
   bloom.scatter.Override(0.65f); bloom.tint.Override(ArtPalette.NeonCyan);
   ```
7. **Néons HDR** : pour qu'une source bloome, sa couleur émissive doit **dépasser 1** (ex `NeonCyan * 3`).
   Monter les intensités dans `MakeEmissive` (2.5–4) et faire briller `MakeUnlit` en HDR.
8. **Nettoyage** : en URP, **supprimer** les halos additifs `NeonGlowSphere` (le vrai bloom les remplace,
   sinon double glow flou).

## Risques (à surveiller à l'œil)
- **Matériaux roses** : un niveau de Quality sans URP Asset, ou un shader Built-in laissé sous URP.
- **Bloom invisible** : HDR non coché, `renderPostProcessing` oublié, ou émissif ≤ 1 (sous le seuil).
- **`MakeGlow`** : la config transparente Built-in ne s'applique pas à URP/Lit — en URP, le plus simple
  est de supprimer `MakeGlow` et de laisser le bloom faire le halo.
- **Brouillard** : rendu différemment en URP (par pixel) — revérifier les distances.
- **Version de paquet** incompatible = échec de résolution du manifest.

Aucun de ces points n'est détectable en compilation : c'est une **passe manuelle** dans l'éditeur.
