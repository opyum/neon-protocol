using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using FirstGame.Core;

/// <summary>
/// Wires the Mixamo character (X Bot) + animations into an animated enemy:
/// sets Humanoid rigs, builds an Animator Controller (Speed blend + Shoot/Die triggers),
/// creates a prefab, and assigns it to GameAssets. Run via "NEON ▸ Configurer l'animation".
/// </summary>
public static class AnimSetup
{
    const string Dir = "Assets/Art/Mixamo";
    const string ControllerPath = Dir + "/EnemyAnimator.controller";
    const string PrefabPath = Dir + "/EnemyMixamo.prefab";

    [MenuItem("NEON/Configurer l'animation")]
    public static void Run()
    {
        // 1) Humanoid rigs + loop flags
        MakeHumanoid($"{Dir}/X_Bot.fbx", null);
        MakeHumanoid($"{Dir}/RifleIdle.fbx", true);
        MakeHumanoid($"{Dir}/RifleRun.fbx", true);
        MakeHumanoid($"{Dir}/FiringRifle.fbx", false);
        MakeHumanoid($"{Dir}/Dying.fbx", false);

        var idle = LoadClip($"{Dir}/RifleIdle.fbx");
        var run = LoadClip($"{Dir}/RifleRun.fbx");
        var shoot = LoadClip($"{Dir}/FiringRifle.fbx");
        var die = LoadClip($"{Dir}/Dying.fbx");
        if (idle == null || run == null) { Debug.LogError("[AnimSetup] clips introuvables"); return; }

        // 2) Animator Controller
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        var sm = ctrl.layers[0].stateMachine;

        var loco = ctrl.CreateBlendTreeInController("Locomotion", out var tree);
        tree.blendType = BlendTreeType.Simple1D;
        tree.blendParameter = "Speed";
        tree.AddChild(idle, 0f);
        tree.AddChild(run, 1f);
        sm.defaultState = loco;

        if (shoot != null)
        {
            var shootState = sm.AddState("Shoot");
            shootState.motion = shoot;
            var toShoot = sm.AddAnyStateTransition(shootState);
            toShoot.AddCondition(AnimatorConditionMode.If, 0, "Shoot");
            toShoot.duration = 0.08f; toShoot.canTransitionToSelf = false;
            var back = shootState.AddTransition(loco);
            back.hasExitTime = true; back.exitTime = 0.7f; back.duration = 0.2f;
        }
        if (die != null)
        {
            var dieState = sm.AddState("Die");
            dieState.motion = die;
            var toDie = sm.AddAnyStateTransition(dieState);
            toDie.AddCondition(AnimatorConditionMode.If, 0, "Die");
            toDie.duration = 0.08f; toDie.canTransitionToSelf = false;
        }
        EditorUtility.SetDirty(ctrl);

        // 3) Prefab: X Bot mesh + Animator (controller, avatar, no root motion)
        var xbotPath = $"{Dir}/X_Bot.fbx";
        var xbot = AssetDatabase.LoadAssetAtPath<GameObject>(xbotPath);
        var avatar = AssetDatabase.LoadAllAssetsAtPath(xbotPath).OfType<Avatar>().FirstOrDefault();
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(xbot);
        var anim = inst.GetComponent<Animator>();
        if (anim == null) anim = inst.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;
        if (avatar != null) anim.avatar = avatar;
        anim.applyRootMotion = false;
        var prefab = PrefabUtility.SaveAsPrefabAsset(inst, PrefabPath);
        Object.DestroyImmediate(inst);

        // 4) Assign to GameAssets
        var ga = AssetDatabase.LoadAssetAtPath<GameAssets>("Assets/Resources/GameAssets.asset");
        if (ga != null && prefab != null)
        {
            ga.enemyCharacterPrefab = prefab;
            EditorUtility.SetDirty(ga);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[AnimSetup] OK — controller + prefab créés, ennemi = {(prefab != null ? prefab.name : "null")}");
    }

    static void MakeHumanoid(string path, bool? loop)
    {
        var imp = AssetImporter.GetAtPath(path) as ModelImporter;
        if (imp == null) { Debug.LogWarning($"[AnimSetup] pas un modèle: {path}"); return; }
        imp.animationType = ModelImporterAnimationType.Human;
        imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        if (loop.HasValue)
        {
            var clips = imp.defaultClipAnimations;
            for (int i = 0; i < clips.Length; i++) clips[i].loopTime = loop.Value;
            if (clips.Length > 0) imp.clipAnimations = clips;
        }
        imp.SaveAndReimport();
    }

    static AnimationClip LoadClip(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__"));
    }
}
