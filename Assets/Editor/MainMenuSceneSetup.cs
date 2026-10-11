using ChefGame.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChefGame.Editor
{
    /// <summary>Creates the authored uGUI hierarchy and persistent button events in MainMenu.unity.</summary>
    [InitializeOnLoad]
    public static class MainMenuSceneSetup
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

        private static readonly Color BackgroundColor = new(0.055f, 0.075f, 0.07f, 1f);
        private static readonly Color CardColor = new(0.11f, 0.15f, 0.13f, 0.98f);
        private static readonly Color ButtonColor = new(0.24f, 0.38f, 0.29f, 1f);
        private static readonly Color AccentColor = new(0.91f, 0.69f, 0.32f, 1f);
        private static readonly Color TextColor = new(0.96f, 0.94f, 0.87f, 1f);

        static MainMenuSceneSetup()
        {
            EditorApplication.delayCall += BuildIfMissing;
        }

        [MenuItem("Tools/Chef Game/Build Main Menu UI")]
        public static void Build()
        {
            BuildIntoScene(forceRebuild: true);
        }

        private static void BuildIfMissing()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += BuildIfMissing;
                return;
            }

            BuildIntoScene(forceRebuild: false);
        }

        private static void BuildIntoScene(bool forceRebuild)
        {
            Scene originalActiveScene = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(MainMenuScenePath);
            bool openedAdditively = !scene.IsValid() || !scene.isLoaded;
            if (openedAdditively)
                scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Additive);

            GameObject[] roots = scene.GetRootGameObjects();
            MainMenuManager manager = null;
            GameObject existingCanvas = null;
            foreach (GameObject root in roots)
            {
                if (root.name == "MainMenuCanvas")
                    existingCanvas = root;

                if (manager == null)
                    manager = root.GetComponentInChildren<MainMenuManager>(true);
            }

            if (manager == null)
            {
                Debug.LogError($"No {nameof(MainMenuManager)} component exists in {MainMenuScenePath}.");
                if (openedAdditively)
                    EditorSceneManager.CloseScene(scene, true);
                return;
            }

            if (!forceRebuild && existingCanvas != null)
            {
                if (openedAdditively)
                    EditorSceneManager.CloseScene(scene, true);
                return;
            }

            if (!EditorSceneManager.SetActiveScene(scene))
            {
                Debug.LogError($"Could not make {MainMenuScenePath} the active scene during UI setup.");
                if (openedAdditively)
                    EditorSceneManager.CloseScene(scene, true);
                return;
            }

            EnsureEventSystem(scene);
            BuildCanvas(manager, existingCanvas);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Authored the main menu Canvas and saved it to {MainMenuScenePath}.");

            if (originalActiveScene.IsValid() && originalActiveScene.isLoaded)
                EditorSceneManager.SetActiveScene(originalActiveScene);
            if (openedAdditively)
                EditorSceneManager.CloseScene(scene, true);
        }

        private static void BuildCanvas(MainMenuManager manager, GameObject existingCanvas)
        {
            if (existingCanvas != null)
                Object.DestroyImmediate(existingCanvas);

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            if (font == null)
            {
                Debug.LogError("TextMesh Pro has no default font asset. Import the TMP Essentials before creating the menu.");
                return;
            }

            GameObject canvasObject = new("MainMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            GameObject backdrop = CreateImage("MenuBackdrop", canvasRect, BackgroundColor,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            CreateText("GameTitle", backdrop.transform, "LOCAL CO-OP KITCHEN", 52f, AccentColor,
                new Vector2(0.08f, 0.71f), new Vector2(0.92f, 0.86f), TextAlignmentOptions.Center);
            CreateText("GameSubtitle", backdrop.transform, "Cook together. Serve together.", 26f, TextColor,
                new Vector2(0.15f, 0.64f), new Vector2(0.85f, 0.72f), TextAlignmentOptions.Center);

            GameObject playPanel = CreatePanel(backdrop.transform, "PlayGamePanel", "PLAY GAME");
            Button controllerOnlyButton = CreateButton(playPanel.transform, "ControllerOnlyButton", "CONTROLLER ONLY",
                new Vector2(0f, 88f), new Vector2(390f, 62f));
            Button mixedButton = CreateButton(playPanel.transform, "MixedInputButton", "MIXED CONTROLS",
                new Vector2(0f, 12f), new Vector2(390f, 62f));
            Button keyboardOnlyButton = CreateButton(playPanel.transform, "KeyboardOnlyButton", "KEYBOARD ONLY",
                new Vector2(0f, -64f), new Vector2(390f, 62f));
            Button playBackButton = CreateBackButton(playPanel.transform, new Vector2(0f, -158f));

            GameObject howToPlayPanel = CreatePanel(backdrop.transform, "HowToPlayPanel", "HOW TO PLAY");
            CreateText("HowToPlayBody", howToPlayPanel.transform,
                "Move your hand with your assigned controls. Press and hold Interact to pick up an ingredient or tool, then release to drop it.\n\nWork together to prepare ingredients and complete orders.",
                25f, TextColor, new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.72f), TextAlignmentOptions.Center);
            Button howToPlayBackButton = CreateBackButton(howToPlayPanel.transform, new Vector2(0f, -194f));

            GameObject creditsPanel = CreatePanel(backdrop.transform, "CreditsPanel", "CREDITS");
            CreateText("CreditsBody", creditsPanel.transform,
                "Chef Game Jam\nCreated by: [Add team credits]\nThanks for playing!",
                26f, TextColor, new Vector2(0.1f, 0.32f), new Vector2(0.9f, 0.7f), TextAlignmentOptions.Center);
            Button creditsBackButton = CreateBackButton(creditsPanel.transform, new Vector2(0f, -194f));

            RectTransform navigation = CreateRect("MainNavigation", backdrop.transform,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-32f, 28f), new Vector2(286f, 282f));
            Button playNavButton = CreateNavigationButton(navigation, "PlayGameButton", "PLAY GAME", 0);
            Button howToPlayNavButton = CreateNavigationButton(navigation, "HowToPlayButton", "HOW TO PLAY", 1);
            Button exitButton = CreateNavigationButton(navigation, "ExitButton", "EXIT", 2);
            Button creditsNavButton = CreateNavigationButton(navigation, "CreditsButton", "CREDITS", 3);

            Wire(playNavButton, manager.OpenPlayGamePanel);
            Wire(howToPlayNavButton, manager.OpenHowToPlayPanel);
            Wire(exitButton, manager.QuitGame);
            Wire(creditsNavButton, manager.OpenCreditsPanel);
            Wire(controllerOnlyButton, manager.LoadControllerOnlyScene);
            Wire(mixedButton, manager.LoadMixedInputScene);
            Wire(keyboardOnlyButton, manager.LoadKeyboardOnlyScene);
            Wire(playBackButton, manager.ClosePanels);
            Wire(howToPlayBackButton, manager.ClosePanels);
            Wire(creditsBackButton, manager.ClosePanels);

            playPanel.SetActive(false);
            howToPlayPanel.SetActive(false);
            creditsPanel.SetActive(false);

            SerializedObject serializedManager = new(manager);
            serializedManager.FindProperty("playGameButton").objectReferenceValue = playNavButton;
            serializedManager.FindProperty("howToPlayButton").objectReferenceValue = howToPlayNavButton;
            serializedManager.FindProperty("exitButton").objectReferenceValue = exitButton;
            serializedManager.FindProperty("creditsButton").objectReferenceValue = creditsNavButton;
            serializedManager.FindProperty("playGamePanel").objectReferenceValue = playPanel;
            serializedManager.FindProperty("howToPlayPanel").objectReferenceValue = howToPlayPanel;
            serializedManager.FindProperty("creditsPanel").objectReferenceValue = creditsPanel;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreatePanel(Transform parent, string panelName, string title)
        {
            GameObject overlay = CreateImage(panelName, parent, new Color(0f, 0f, 0f, 0.58f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            GameObject card = CreateImage("PanelCard", overlay.transform, CardColor,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 560f));
            CreateImage("PanelAccent", card.transform, AccentColor,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -8f), new Vector2(0f, 8f));
            CreateText("PanelTitle", card.transform, title, 38f, AccentColor,
                new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.94f), TextAlignmentOptions.Center);
            return overlay;
        }

        private static Button CreateNavigationButton(Transform parent, string objectName, string label, int index)
        {
            return CreateButton(parent, objectName, label, new Vector2(0f, -28f - index * 64f), new Vector2(250f, 54f));
        }

        private static Button CreateBackButton(Transform parent, Vector2 position)
        {
            return CreateButton(parent, "BackButton", "BACK", position, new Vector2(220f, 56f));
        }

        private static Button CreateButton(Transform parent, string objectName, string label, Vector2 position, Vector2 size)
        {
            GameObject buttonObject = CreateImage(objectName, parent, ButtonColor,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();

            ColorBlock colors = button.colors;
            colors.normalColor = ButtonColor;
            colors.highlightedColor = new Color(0.34f, 0.5f, 0.37f, 1f);
            colors.pressedColor = new Color(0.18f, 0.29f, 0.22f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            button.colors = colors;

            CreateText("Label", buttonObject.transform, label, 23f, TextColor,
                Vector2.zero, Vector2.one, TextAlignmentOptions.Center);
            return button;
        }

        private static GameObject CreateImage(string objectName, Transform parent, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject imageObject = new(objectName, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return imageObject;
        }

        private static RectTransform CreateRect(string objectName, Transform parent, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject rectObject = new(objectName, typeof(RectTransform));
            rectObject.transform.SetParent(parent, false);
            RectTransform rect = rectObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return rect;
        }

        private static void CreateText(string objectName, Transform parent, string text, float fontSize,
            Color color, Vector2 anchorMin, Vector2 anchorMax, TextAlignmentOptions alignment)
        {
            GameObject textObject = new(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = alignment;
            label.enableWordWrapping = true;
            label.raycastTarget = false;
        }

        private static void Wire(Button button, UnityEngine.Events.UnityAction callback)
        {
            UnityEventTools.AddPersistentListener(button.onClick, callback);
        }

        private static void EnsureEventSystem(Scene scene)
        {
            EventSystem eventSystem = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                eventSystem = root.GetComponentInChildren<EventSystem>(true);
                if (eventSystem != null)
                    break;
            }

            if (eventSystem == null)
            {
                GameObject eventSystemObject = new("EventSystem");
                SceneManager.MoveGameObjectToScene(eventSystemObject, scene);
                eventSystem = eventSystemObject.AddComponent<EventSystem>();
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                InputSystemUIInputModule inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                inputModule.AssignDefaultActions();
            }
        }
    }
}
