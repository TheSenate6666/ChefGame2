using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChefGame.UI
{
    /// <summary>Controls the main menu's panels, scene transitions, and quit action.</summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuManager : MonoBehaviour
    {
        [Header("Gameplay Scenes")]
        [SerializeField] private string controllerOnlyScene = "Final";
        [SerializeField] private string mixedInputScene = "Prototype";
        [SerializeField] private string keyboardOnlyScene = string.Empty;

        [Header("Main Navigation Buttons")]
        [SerializeField] private Button playGameButton;
        [SerializeField] private Button howToPlayButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Button creditsButton;

        [Header("Panels")]
        [SerializeField] private GameObject playGamePanel;
        [SerializeField] private GameObject howToPlayPanel;
        [SerializeField] private GameObject creditsPanel;

        private void Awake()
        {
            ClosePanels();
        }

        public void OpenPlayGamePanel() => ShowOnly(playGamePanel);
        public void OpenHowToPlayPanel() => ShowOnly(howToPlayPanel);
        public void OpenCreditsPanel() => ShowOnly(creditsPanel);

        public void ClosePanels()
        {
            SetPanelActive(playGamePanel, false);
            SetPanelActive(howToPlayPanel, false);
            SetPanelActive(creditsPanel, false);
        }

        public void LoadControllerOnlyScene() => LoadScene(controllerOnlyScene, "Controller Only");
        public void LoadMixedInputScene() => LoadScene(mixedInputScene, "Mixed");
        public void LoadKeyboardOnlyScene() => LoadScene(keyboardOnlyScene, "Keyboard Only");

        public void QuitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void ShowOnly(GameObject selectedPanel)
        {
            if (selectedPanel == null)
            {
                Debug.LogWarning("The requested main menu panel is not assigned.", this);
                return;
            }

            SetPanelActive(playGamePanel, selectedPanel == playGamePanel);
            SetPanelActive(howToPlayPanel, selectedPanel == howToPlayPanel);
            SetPanelActive(creditsPanel, selectedPanel == creditsPanel);
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null && panel.activeSelf != active)
                panel.SetActive(active);
        }

        private void LoadScene(string sceneName, string modeLabel)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning($"The {modeLabel} scene is not assigned on {nameof(MainMenuManager)}.", this);
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"Scene '{sceneName}' is not in Build Settings. Add it to the Scenes In Build list before using the {modeLabel} button.", this);
                return;
            }

            SceneManager.LoadScene(sceneName);
        }
    }
}
