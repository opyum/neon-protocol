using UnityEngine;
using UnityEngine.SceneManagement;

namespace FirstGame.Core
{
    /// <summary>
    /// Persistent bootstrap. Scenes ship EMPTY (just settings) — the whole world is
    /// constructed here at runtime, keyed off the active scene name. This removes all
    /// fragile hand-authored scene YAML / GUID references: the project just works on open.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        static bool _bootstrapped;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (_bootstrapped) return;
            _bootstrapped = true;

            var go = new GameObject("[GameManager]");
            Instance = go.AddComponent<GameManager>();
            DontDestroyOnLoad(go);

            new GameObject("[Music]").AddComponent<MusicPlayer>();

            SceneManager.sceneLoaded += Instance.OnSceneLoaded;
            Instance.BuildForScene(SceneManager.GetActiveScene().name);
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode) => BuildForScene(scene.name);

        void BuildForScene(string sceneName)
        {
            switch (sceneName)
            {
                case SceneNames.Tutorial:
                    TutorialScene.Build();
                    break;
                case SceneNames.PracticeRange:
                    PracticeRangeScene.Build();
                    break;
                case SceneNames.CombatArena:
                    CombatArenaScene.Build();
                    break;
                case SceneNames.MainMenu:
                default:
                    MainMenuScene.Build();
                    break;
            }
        }

        public static void LoadScene(string name)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(name);
        }
    }
}
