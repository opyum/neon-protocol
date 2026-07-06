using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Creates a URP pipeline asset + renderer and assigns them to Graphics + all Quality levels.
/// Run once from the menu "NEON ▸ Configurer URP" (or batch via -executeMethod UrpSetup.Run).
/// </summary>
public static class UrpSetup
{
    const string Dir = "Assets/Settings";
    const string RendererPath = Dir + "/PC_Renderer.asset";
    const string PipelinePath = Dir + "/PC_RPAsset.asset";

    [MenuItem("NEON/Configurer URP")]
    public static void Run()
    {
        if (!AssetDatabase.IsValidFolder(Dir))
            AssetDatabase.CreateFolder("Assets", "Settings");

        var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (rendererData == null)
        {
            rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(rendererData, RendererPath);
        }

        var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        if (urp == null)
        {
            urp = UniversalRenderPipelineAsset.Create(rendererData);
            AssetDatabase.CreateAsset(urp, PipelinePath);
        }
        urp.supportsHDR = true;

        GraphicsSettings.defaultRenderPipeline = urp;

        int levels = QualitySettings.names.Length;
        int current = QualitySettings.GetQualityLevel();
        for (int i = 0; i < levels; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = urp;
        }
        QualitySettings.SetQualityLevel(current, false);

        EditorUtility.SetDirty(urp);
        EditorUtility.SetDirty(rendererData);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[UrpSetup] URP configuré: pipeline assigné à Graphics + {levels} niveaux de qualité.");
    }
}
