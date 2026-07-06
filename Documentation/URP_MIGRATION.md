# Migration URP + Bloom (guide)

> **Statut : préparé, pas activé.** Le projet tourne en **Built-in Render Pipeline** (fiable, vérifié).
> Le code est **prêt pour URP** (`ArtPalette` choisit déjà le bon shader selon le pipeline actif).
> Passer réellement en URP demande une **validation visuelle dans l'éditeur** — impossible à vérifier
> en compilation seule (une erreur URP se voit à l'écran : matériaux roses, pas de bloom). Fais-le
> quand tu peux ouvrir chaque scène et regarder.

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
