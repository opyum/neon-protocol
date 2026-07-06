using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>One-click Windows build. Menu "NEON ▸ Build Windows (.exe)" → Builds/NEON_PROTOCOL/.</summary>
public static class BuildGame
{
    const string OutDir = "Builds/NEON_PROTOCOL";
    const string ExePath = OutDir + "/NEON_PROTOCOL.exe";

    [MenuItem("NEON/Build Windows (.exe)")]
    public static void Build()
    {
        PlayerSettings.productName = "NEON PROTOCOL";
        PlayerSettings.companyName = "opyum";
        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, "com.opyum.neonprotocol");

        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0) { Debug.LogError("[BuildGame] Aucune scène dans les Build Settings."); return; }

        Directory.CreateDirectory(OutDir);
        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = ExePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };

        var report = BuildPipeline.BuildPlayer(options);
        var s = report.summary;
        if (s.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildGame] ✅ Build réussi : {ExePath}  ({s.totalSize / (1024 * 1024)} Mo)");
            EditorUtility.RevealInFinder(Path.GetFullPath(ExePath));
        }
        else
        {
            Debug.LogError($"[BuildGame] ❌ Build échoué : {s.result} ({s.totalErrors} erreurs)");
        }
    }
}
