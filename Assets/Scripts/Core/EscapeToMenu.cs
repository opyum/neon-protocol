using UnityEngine;

namespace FirstGame.Core
{
    /// <summary>Press Échap to return to the main menu from any gameplay scene.</summary>
    public class EscapeToMenu : MonoBehaviour
    {
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                GameManager.LoadScene(SceneNames.MainMenu);
        }
    }
}
