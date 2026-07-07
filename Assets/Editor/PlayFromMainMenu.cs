using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>Makes the Editor's Play button always boot from the Main Menu (like a real build),
/// whatever scene is currently open. Toggle via NEON ▸ menu.</summary>
[InitializeOnLoad]
public static class PlayFromMainMenu
{
    const string ScenePath = "Assets/Scenes/MainMenu.unity";
    const string PrefKey = "neon.playFromMenu";
    const string MenuPath = "NEON/Démarrer le Play sur le Menu principal";

    static PlayFromMainMenu() => Apply();

    static void Apply()
    {
        bool on = EditorPrefs.GetBool(PrefKey, true);
        EditorSceneManager.playModeStartScene = on
            ? AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)
            : null;
    }

    [MenuItem(MenuPath)]
    static void Toggle()
    {
        EditorPrefs.SetBool(PrefKey, !EditorPrefs.GetBool(PrefKey, true));
        Apply();
    }

    [MenuItem(MenuPath, true)]
    static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, EditorPrefs.GetBool(PrefKey, true));
        return true;
    }
}
