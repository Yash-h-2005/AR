#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SurakshaAR.Localization;
using SurakshaAR.UI.Data;
using SurakshaAR.UI.Navigation;
using SurakshaAR.UI.Components;
using SurakshaAR.UI.Screens;
using SurakshaAR.Training.Fire;
using SurakshaAR.Training.Gas;
using SurakshaAR.Assessment;
using SurakshaAR.Certification;
using SurakshaAR.Data;
using SurakshaAR.AR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

namespace SurakshaAR.Editor
{
    public static class SceneBuilderScript
    {
        private static Font defaultFont;

        // Clean Industrial Color Palette
        private static readonly Color BgColor = new Color(0.92f, 0.94f, 0.97f, 1f);          // #EBF0F5 Soft Cool Slate Canvas
        private static readonly Color CardBgColor = new Color(1f, 1f, 1f, 1f);              // #FFFFFF Pure White Card
        private static readonly Color BorderColor = new Color(0.80f, 0.84f, 0.90f, 1f);       // #CCD6E0 Defined Shadow Border
        private static readonly Color DarkNavyColor = new Color(0.06f, 0.09f, 0.16f, 1f);      // #0F172A Dark Navy Text
        private static readonly Color MediumGrayColor = new Color(0.39f, 0.45f, 0.55f, 1f);    // #64748B Slate Subtitle
        private static readonly Color PrimaryOrangeColor = new Color(0.97f, 0.45f, 0.09f, 1f);  // #F97316 Safety Orange
        private static readonly Color SecondarySlateColor = new Color(0.2f, 0.25f, 0.33f, 1f);  // #334155 Secondary Action
        private static readonly Color SuccessGreenColor = new Color(0.06f, 0.73f, 0.51f, 1f);   // #10B981 Success Green
        private static readonly Color WarningAmberColor = new Color(0.96f, 0.62f, 0.04f, 1f);   // #F59E0B Warning Amber

        private static Sprite roundedCardSprite;
        private static Sprite roundedPillSprite;
        private static Sprite roundedSquareSprite;
        private static Sprite circleSprite;

        private static void InitializeSprites()
        {
            if (roundedCardSprite == null)
            {
                roundedCardSprite = CreateRoundedSprite(128, 128, 24, new Vector4(24, 24, 24, 24));
            }
            if (roundedPillSprite == null)
            {
                roundedPillSprite = CreateRoundedSprite(64, 64, 31, new Vector4(31, 31, 31, 31));
            }
            if (roundedSquareSprite == null)
            {
                roundedSquareSprite = CreateRoundedSprite(64, 64, 18, new Vector4(18, 18, 18, 18));
            }
            if (circleSprite == null)
            {
                circleSprite = CreateRoundedSprite(64, 64, 31, Vector4.zero);
            }
        }

        private static Sprite CreateRoundedSprite(int width, int height, int radius, Vector4 border)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            Color[] colors = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = 0;
                    if (x < radius) dx = radius - x;
                    else if (x >= width - radius) dx = x - (width - radius - 1);

                    float dy = 0;
                    if (y < radius) dy = radius - y;
                    else if (y >= height - radius) dy = y - (height - radius - 1);

                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = 1f;
                    if (dx > 0 && dy > 0)
                    {
                        alpha = Mathf.Clamp01(radius - dist + 0.5f);
                    }

                    colors[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(colors);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        [MenuItem("SurakshaAR/Build Redesigned Phase 3 UI Scenes")]
        public static void BuildAllScenes()
        {
            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
            {
                defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 16);
            }

            InitializeSprites();
            EnsureDirectories();

            CreateSplashScene();
            CreateLoginScene();
            CreateLanguageSelectionScene();
            CreateCameraAccessScene();
            CreateTrainingNoticeScene();
            CreateHomeScene();
            CreateTrainingModulesScene();
            CreateFireModuleIntroScene();
            CreateGasModuleIntroScene();
            CreateARPhase1Scene();
            CreateARLearnPlaceholderScene();
            CreateGasARLearnPlaceholderScene();
            CreateARSimulationPlaceholderScene();
            CreateResultScene();
            CreatePerformanceSummaryScene();
            CreateCertificatePreviewScene();
            CreateQRVerificationScene();
            CreateProfileScene();
            CreateSettingsScene();

            Debug.Log("[SceneBuilder] All 18 Redesigned Unity Scenes Built Successfully with Rounded UI!");
        }

        private static void EnsureDirectories()
        {
            string[] dirs = new string[]
            {
                "Assets/_Project/Scenes/Bootstrap",
                "Assets/_Project/Scenes/MainMenu",
                "Assets/_Project/Scenes/Fire",
                "Assets/_Project/Scenes/Gas"
            };

            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
        }

        private static GameObject CreateCanvasWithEventSystem(UnityEngine.SceneManagement.Scene scene, string name = "UI Canvas")
        {
            GameObject canvasGO = new GameObject(name);
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // Event System
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<StandaloneInputModule>();
            }

            // Global Managers GameObject
            GameObject managersGO = new GameObject("AppManagers");
            if (Object.FindFirstObjectByType<LocalizationManager>() == null)
                managersGO.AddComponent<LocalizationManager>();
            if (Object.FindFirstObjectByType<UserSession>() == null)
                managersGO.AddComponent<UserSession>();
            if (Object.FindFirstObjectByType<SceneNavigator>() == null)
                managersGO.AddComponent<SceneNavigator>();
            if (Object.FindFirstObjectByType<WorkerProfile>() == null)
                managersGO.AddComponent<WorkerProfile>();
            if (Object.FindFirstObjectByType<AssessmentManager>() == null)
                managersGO.AddComponent<AssessmentManager>();
            if (Object.FindFirstObjectByType<CertificateManager>() == null)
                managersGO.AddComponent<CertificateManager>();
            if (Object.FindFirstObjectByType<FirebaseSyncManager>() == null)
                managersGO.AddComponent<FirebaseSyncManager>();

            return canvasGO;
        }

        private static GameObject CreatePanel(GameObject parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Sprite sprite = null)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent.transform, false);

            Image img = panel.AddComponent<Image>();
            img.color = color;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = (sprite.border != Vector4.zero) ? Image.Type.Sliced : Image.Type.Simple;
            }

            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return panel;
        }

        private static GameObject CreateCardPanel(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            InitializeSprites();
            // Outer Border / Shadow Panel with Rounded Sprite
            GameObject outer = CreatePanel(parent, name, BorderColor, anchorMin, anchorMax, roundedCardSprite);
            Shadow shadow = outer.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.06f, 0.09f, 0.16f, 0.08f);
            shadow.effectDistance = new Vector2(0, -4);

            // Inner Card Body with Rounded Sprite
            GameObject inner = CreatePanel(outer, "Body", CardBgColor, Vector2.zero, Vector2.one, roundedCardSprite);
            RectTransform innerRt = inner.GetComponent<RectTransform>();
            innerRt.offsetMin = new Vector2(2, 2);
            innerRt.offsetMax = new Vector2(-2, -2);

            return inner;
        }

        private static Text CreateText(GameObject parent, string name, string textContent, int fontSize, Color color, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta, FontStyle style = FontStyle.Normal)
        {
            GameObject textGO = new GameObject(name);
            textGO.transform.SetParent(parent.transform, false);

            Text text = textGO.AddComponent<Text>();
            text.font = defaultFont;
            text.text = textContent;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = style;

            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            RectTransform rt = textGO.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            return text;
        }

        private static Button CreateButton(GameObject parent, string name, string buttonText, Color btnColor, Color textColor, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta, bool isPill = false)
        {
            InitializeSprites();
            GameObject btnGO = new GameObject(name);
            btnGO.transform.SetParent(parent.transform, false);

            Image img = btnGO.AddComponent<Image>();
            img.sprite = isPill ? roundedPillSprite : roundedCardSprite;
            img.type = Image.Type.Sliced;
            img.color = btnColor;

            if (btnColor == PrimaryOrangeColor)
            {
                Shadow sh = btnGO.AddComponent<Shadow>();
                sh.effectColor = new Color(0.97f, 0.45f, 0.09f, 0.28f);
                sh.effectDistance = new Vector2(0, -4);
            }

            Button btn = btnGO.AddComponent<Button>();

            RectTransform rt = btnGO.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            CreateText(btnGO, "Text", buttonText, 32, textColor, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FontStyle.Bold);

            return btn;
        }

        private static InputField CreateInputField(GameObject parent, string name, string placeholderText, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            InitializeSprites();
            GameObject outer = CreatePanel(parent, name, BorderColor, anchorMin, anchorMax, roundedCardSprite);
            RectTransform outerRt = outer.GetComponent<RectTransform>();
            outerRt.anchoredPosition = anchoredPos;
            outerRt.sizeDelta = sizeDelta;

            GameObject inner = CreatePanel(outer, "Body", CardBgColor, Vector2.zero, Vector2.one, roundedCardSprite);
            RectTransform innerRt = inner.GetComponent<RectTransform>();
            innerRt.offsetMin = new Vector2(2, 2);
            innerRt.offsetMax = new Vector2(-2, -2);

            InputField inputField = outer.AddComponent<InputField>();

            Text text = CreateText(inner, "Text", "", 32, DarkNavyColor, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(24, 0), new Vector2(-48, 0));
            Text placeholder = CreateText(inner, "Placeholder", placeholderText, 32, MediumGrayColor, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(24, 0), new Vector2(-48, 0));

            inputField.textComponent = text;
            inputField.placeholder = placeholder;

            return inputField;
        }

        private static Slider CreateProgressBar(GameObject parent, string name, float initialValue, Vector2 anchorMin, Vector2 anchorMax)
        {
            InitializeSprites();
            GameObject sliderGO = new GameObject(name);
            sliderGO.transform.SetParent(parent.transform, false);

            Slider slider = sliderGO.AddComponent<Slider>();

            RectTransform rt = sliderGO.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Background with Rounded Pill Sprite
            GameObject bg = CreatePanel(sliderGO, "Background", new Color(0.88f, 0.91f, 0.95f, 1f), Vector2.zero, Vector2.one, roundedPillSprite);
            
            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderGO.transform, false);
            RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.offsetMin = Vector2.zero;
            fillAreaRt.offsetMax = Vector2.zero;

            GameObject fill = CreatePanel(fillArea, "Fill", PrimaryOrangeColor, Vector2.zero, Vector2.one, roundedPillSprite);

            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.value = initialValue;

            return slider;
        }

        // ==================== 13 SCENE BUILDERS ====================

        private static void CreateSplashScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject canvas = CreateCanvasWithEventSystem(scene, "SplashCanvas");

            CreatePanel(canvas, "Background", BgColor, Vector2.zero, Vector2.one);
            CreateText(canvas, "Logo", "SurakshaAR", 72, PrimaryOrangeColor, TextAnchor.MiddleCenter, new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.7f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            CreateText(canvas, "Subtitle", "Industrial Safety Training", 36, DarkNavyColor, TextAnchor.MiddleCenter, new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.55f), Vector2.zero, Vector2.zero);

            GameObject controllerGO = new GameObject("SplashController");
            controllerGO.AddComponent<SplashScreenController>();

            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/Bootstrap/Splash.unity");
        }

        private static void CreateLoginScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject canvas = CreateCanvasWithEventSystem(scene, "LoginCanvas");

            CreatePanel(canvas, "Background", BgColor, Vector2.zero, Vector2.one);

            Button backBtn = CreateButton(canvas, "BackButton", "←", BgColor, DarkNavyColor, new Vector2(0.04f, 0.92f), new Vector2(0.18f, 0.97f), Vector2.zero, Vector2.zero);

            Text titleText = CreateText(canvas, "Title", "LOGIN", 54, DarkNavyColor, TextAnchor.MiddleCenter, new Vector2(0.1f, 0.78f), new Vector2(0.9f, 0.86f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text subtitleText = CreateText(canvas, "Subtitle", "Enter your Worker details", 30, MediumGrayColor, TextAnchor.MiddleCenter, new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.77f), Vector2.zero, Vector2.zero);

            // Field 1: Worker Name
            Text nameLabel = CreateText(canvas, "WorkerNameLabel", "Worker Name", 26, DarkNavyColor, TextAnchor.MiddleLeft, new Vector2(0.08f, 0.64f), new Vector2(0.92f, 0.69f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            InputField nameInput = CreateInputField(canvas, "WorkerNameInput", "Enter your name", new Vector2(0.08f, 0.55f), new Vector2(0.92f, 0.63f), Vector2.zero, Vector2.zero);

            // Field 2: Worker ID
            Text idLabel = CreateText(canvas, "WorkerIDLabel", "Worker ID", 26, DarkNavyColor, TextAnchor.MiddleLeft, new Vector2(0.08f, 0.47f), new Vector2(0.92f, 0.52f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            InputField idInput = CreateInputField(canvas, "WorkerIDInput", "Enter Worker ID", new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.46f), Vector2.zero, Vector2.zero);

            Text errText = CreateText(canvas, "ErrorBanner", "Worker Name and Worker ID are required!", 26, Color.red, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.36f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            errText.gameObject.SetActive(false);

            Button loginBtn = CreateButton(canvas, "LoginButton", "LOGIN", PrimaryOrangeColor, Color.white, new Vector2(0.08f, 0.19f), new Vector2(0.92f, 0.28f), Vector2.zero, Vector2.zero);
            Text loginBtnText = loginBtn.GetComponentInChildren<Text>();

            // Bottom Help Footer
            Text helpText = CreateText(canvas, "HelpText", "Need help? Contact Admin", 28, new Color(0.85f, 0.4f, 0.25f, 1f), TextAnchor.MiddleCenter, new Vector2(0.1f, 0.04f), new Vector2(0.9f, 0.10f), Vector2.zero, Vector2.zero);

            GameObject controllerGO = new GameObject("LoginController");
            LoginScreenController ctrl = controllerGO.AddComponent<LoginScreenController>();

            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("workerNameInput").objectReferenceValue = nameInput;
            so.FindProperty("workerIdInput").objectReferenceValue = idInput;
            so.FindProperty("loginButton").objectReferenceValue = loginBtn;
            so.FindProperty("backButton").objectReferenceValue = backBtn;
            so.FindProperty("titleText").objectReferenceValue = titleText;
            so.FindProperty("subtitleText").objectReferenceValue = subtitleText;
            so.FindProperty("workerNameLabel").objectReferenceValue = nameLabel;
            so.FindProperty("workerIdLabel").objectReferenceValue = idLabel;
            so.FindProperty("loginButtonText").objectReferenceValue = loginBtnText;
            so.FindProperty("errorBannerText").objectReferenceValue = errText;
            so.FindProperty("helpFooterText").objectReferenceValue = helpText;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/MainMenu/Login.unity");
        }

        private static void CreateLanguageSelectionScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject canvas = CreateCanvasWithEventSystem(scene, "LanguageCanvas");

            CreatePanel(canvas, "Background", BgColor, Vector2.zero, Vector2.one);
            Button backBtn = CreateButton(canvas, "BackButton", "←", BgColor, DarkNavyColor, new Vector2(0.04f, 0.92f), new Vector2(0.18f, 0.97f), Vector2.zero, Vector2.zero);

            Text titleText = CreateText(canvas, "Title", "SELECT LANGUAGE", 52, DarkNavyColor, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.80f), new Vector2(0.92f, 0.88f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text subtitleText = CreateText(canvas, "Subtitle", "Choose your preferred training language", 30, MediumGrayColor, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.74f), new Vector2(0.92f, 0.79f), Vector2.zero, Vector2.zero);

            // Card 1: English
            GameObject enCard = CreateCardPanel(canvas, "EnglishCard", new Vector2(0.08f, 0.56f), new Vector2(0.92f, 0.68f));
            Outline enOutline = enCard.GetComponent<Outline>();
            CreateText(enCard, "EnTitle", "ENGLISH", 36, DarkNavyColor, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.5f), new Vector2(0.8f, 0.9f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            CreateText(enCard, "EnSub", "English", 28, MediumGrayColor, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.1f), new Vector2(0.8f, 0.48f), Vector2.zero, Vector2.zero);
            Text enCheck = CreateText(enCard, "Checkmark", "✓", 36, PrimaryOrangeColor, TextAnchor.MiddleCenter, new Vector2(0.82f, 0.2f), new Vector2(0.96f, 0.8f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Button enBtn = enCard.AddComponent<Button>();

            // Card 2: Hindi
            GameObject hiCard = CreateCardPanel(canvas, "HindiCard", new Vector2(0.08f, 0.41f), new Vector2(0.92f, 0.53f));
            Outline hiOutline = hiCard.GetComponent<Outline>();
            CreateText(hiCard, "HiTitle", "HINDI", 36, DarkNavyColor, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.5f), new Vector2(0.8f, 0.9f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            CreateText(hiCard, "HiSub", "हिन्दी", 28, MediumGrayColor, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.1f), new Vector2(0.8f, 0.48f), Vector2.zero, Vector2.zero);
            Text hiCheck = CreateText(hiCard, "Checkmark", "✓", 36, PrimaryOrangeColor, TextAnchor.MiddleCenter, new Vector2(0.82f, 0.2f), new Vector2(0.96f, 0.8f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Button hiBtn = hiCard.AddComponent<Button>();

            // Card 3: Santali
            GameObject satCard = CreateCardPanel(canvas, "SantaliCard", new Vector2(0.08f, 0.26f), new Vector2(0.92f, 0.38f));
            Outline satOutline = satCard.GetComponent<Outline>();
            CreateText(satCard, "SatTitle", "SANTALI", 36, DarkNavyColor, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.5f), new Vector2(0.8f, 0.9f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            CreateText(satCard, "SatSub", "Santali (Ol Chiki)", 28, MediumGrayColor, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.1f), new Vector2(0.8f, 0.48f), Vector2.zero, Vector2.zero);
            Text satCheck = CreateText(satCard, "Checkmark", "✓", 36, PrimaryOrangeColor, TextAnchor.MiddleCenter, new Vector2(0.82f, 0.2f), new Vector2(0.96f, 0.8f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Button satBtn = satCard.AddComponent<Button>();

            // Large Orange Continue Button
            Button continueBtn = CreateButton(canvas, "ContinueButton", "CONTINUE", PrimaryOrangeColor, Color.white, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.17f), Vector2.zero, Vector2.zero);
            Text continueBtnText = continueBtn.GetComponentInChildren<Text>();

            GameObject controllerGO = new GameObject("LanguageController");
            LanguageSelectionController ctrl = controllerGO.AddComponent<LanguageSelectionController>();

            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("backButton").objectReferenceValue = backBtn;
            so.FindProperty("englishButton").objectReferenceValue = enBtn;
            so.FindProperty("hindiButton").objectReferenceValue = hiBtn;
            so.FindProperty("santaliButton").objectReferenceValue = satBtn;
            so.FindProperty("continueButton").objectReferenceValue = continueBtn;
            so.FindProperty("titleText").objectReferenceValue = titleText;
            so.FindProperty("subtitleText").objectReferenceValue = subtitleText;
            so.FindProperty("continueButtonText").objectReferenceValue = continueBtnText;
            so.FindProperty("englishOutline").objectReferenceValue = enOutline;
            so.FindProperty("hindiOutline").objectReferenceValue = hiOutline;
            so.FindProperty("santaliOutline").objectReferenceValue = satOutline;
            so.FindProperty("englishCheckmark").objectReferenceValue = enCheck.gameObject;
            so.FindProperty("hindiCheckmark").objectReferenceValue = hiCheck.gameObject;
            so.FindProperty("santaliCheckmark").objectReferenceValue = satCheck.gameObject;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/MainMenu/LanguageSelection.unity");
        }

        private static void CreateCameraAccessScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject canvas = CreateCanvasWithEventSystem(scene, "CameraAccessCanvas");

            CreatePanel(canvas, "Background", BgColor, Vector2.zero, Vector2.one);
            Button backBtn = CreateButton(canvas, "BackButton", "←", BgColor, DarkNavyColor, new Vector2(0.04f, 0.92f), new Vector2(0.18f, 0.97f), Vector2.zero, Vector2.zero);

            // Center Camera Illustration Card
            GameObject illustCard = CreateCardPanel(canvas, "IllustrationCard", new Vector2(0.10f, 0.48f), new Vector2(0.90f, 0.88f));

            // Try load texture sprite (CameraIllustration.png with transparency, fallback to JPG)
            Sprite illustSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Textures/CameraIllustration.png");
            if (illustSprite == null)
            {
                illustSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Textures/CameraAccessIllustration.jpg");
            }
            if (illustSprite == null)
            {
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Textures/CameraIllustration.png");
                if (tex == null) tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Textures/CameraAccessIllustration.jpg");
                if (tex != null)
                {
                    illustSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }

            if (illustSprite != null)
            {
                GameObject imgGO = new GameObject("IllustrationImage");
                imgGO.transform.SetParent(illustCard.transform, false);
                Image img = imgGO.AddComponent<Image>();
                img.sprite = illustSprite;
                img.preserveAspect = true;
                RectTransform imgRt = imgGO.GetComponent<RectTransform>();
                imgRt.anchorMin = new Vector2(0.03f, 0.03f);
                imgRt.anchorMax = new Vector2(0.97f, 0.97f);
                imgRt.offsetMin = Vector2.zero;
                imgRt.offsetMax = Vector2.zero;
            }
            else
            {
                CreateText(illustCard, "CameraIcon", "📷", 96, PrimaryOrangeColor, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            // Title & Description
            Text titleTxt = CreateText(canvas, "Title", "Camera Access", 52, DarkNavyColor, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.44f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text descTxt = CreateText(canvas, "Description", "SURAKSHAAR uses your camera to provide AR-based training experiences.", 30, MediumGrayColor, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.24f), new Vector2(0.92f, 0.35f), Vector2.zero, Vector2.zero);

            // ONLY BUTTON: ALLOW CAMERA ACCESS (No "Not now" button)
            Button allowBtn = CreateButton(canvas, "AllowCameraButton", "ALLOW CAMERA ACCESS", PrimaryOrangeColor, Color.white, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.21f), Vector2.zero, Vector2.zero);
            Text allowBtnText = allowBtn.GetComponentInChildren<Text>();

            GameObject controllerGO = new GameObject("CameraAccessController");
            CameraAccessController ctrl = controllerGO.AddComponent<CameraAccessController>();

            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("backButton").objectReferenceValue = backBtn;
            so.FindProperty("allowCameraButton").objectReferenceValue = allowBtn;
            so.FindProperty("titleText").objectReferenceValue = titleTxt;
            so.FindProperty("descText").objectReferenceValue = descTxt;
            so.FindProperty("allowButtonText").objectReferenceValue = allowBtnText;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/MainMenu/CameraAccess.unity");
        }

        private static void CreateTrainingNoticeScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject canvas = CreateCanvasWithEventSystem(scene, "TrainingNoticeCanvas");

            CreatePanel(canvas, "Background", BgColor, Vector2.zero, Vector2.one);
            Button backBtn = CreateButton(canvas, "BackButton", "←", BgColor, DarkNavyColor, new Vector2(0.04f, 0.92f), new Vector2(0.18f, 0.97f), Vector2.zero, Vector2.zero);

            // Warning Icon Triangle
            CreateText(canvas, "WarningIcon", "⚠️", 96, PrimaryOrangeColor, TextAnchor.MiddleCenter, new Vector2(0.1f, 0.58f), new Vector2(0.9f, 0.75f), Vector2.zero, Vector2.zero);

            // Title & Legal Description
            CreateText(canvas, "Title", "Training Simulation", 54, DarkNavyColor, TextAnchor.MiddleCenter, new Vector2(0.1f, 0.48f), new Vector2(0.9f, 0.56f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            CreateText(canvas, "Description", "This application is intended for safety training purposes only and does not replace workplace safety procedures.", 32, MediumGrayColor, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.32f), new Vector2(0.92f, 0.46f), Vector2.zero, Vector2.zero);

            // Checkbox UI Panel
            GameObject checkPanel = new GameObject("CheckboxPanel");
            checkPanel.transform.SetParent(canvas.transform, false);
            RectTransform checkRt = checkPanel.AddComponent<RectTransform>();
            checkRt.anchorMin = new Vector2(0.1f, 0.22f);
            checkRt.anchorMax = new Vector2(0.9f, 0.29f);
            checkRt.offsetMin = Vector2.zero;
            checkRt.offsetMax = Vector2.zero;

            Toggle toggle = checkPanel.AddComponent<Toggle>();

            GameObject checkBg = CreatePanel(checkPanel, "Background", CardBgColor, new Vector2(0.05f, 0.2f), new Vector2(0.15f, 0.8f));
            Outline checkOutline = checkBg.AddComponent<Outline>();
            checkOutline.effectColor = BorderColor;
            GameObject checkMark = CreatePanel(checkBg, "Checkmark", PrimaryOrangeColor, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));

            toggle.targetGraphic = checkBg.GetComponent<Image>();
            toggle.graphic = checkMark.GetComponent<Image>();
            toggle.isOn = false;

            CreateText(checkPanel, "Label", "I understand", 34, DarkNavyColor, TextAnchor.MiddleLeft, new Vector2(0.2f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, FontStyle.Bold);

            // Continue Button (disabled gray by default)
            Button continueBtn = CreateButton(canvas, "ContinueButton", "CONTINUE", MediumGrayColor, Color.white, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.17f), Vector2.zero, Vector2.zero);
            Image btnImg = continueBtn.GetComponent<Image>();

            GameObject controllerGO = new GameObject("TrainingNoticeController");
            TrainingNoticeController ctrl = controllerGO.AddComponent<TrainingNoticeController>();

            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("backButton").objectReferenceValue = backBtn;
            so.FindProperty("understandToggle").objectReferenceValue = toggle;
            so.FindProperty("continueButton").objectReferenceValue = continueBtn;
            so.FindProperty("continueButtonImage").objectReferenceValue = btnImg;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/MainMenu/TrainingNotice.unity");
        }

        private static GameObject AddBottomNavigation(GameObject canvas, ScreenState activeScreen)
        {
            GameObject bottomNavGO = new GameObject("BottomNavigation");
            bottomNavGO.transform.SetParent(canvas.transform, false);

            RectTransform navRt = bottomNavGO.AddComponent<RectTransform>();
            navRt.anchorMin = new Vector2(0f, 0f);
            navRt.anchorMax = new Vector2(1f, 0.10f);
            navRt.offsetMin = Vector2.zero;
            navRt.offsetMax = Vector2.zero;

            Color lightNavBg = new Color(1f, 1f, 1f, 1f); // Pure White #FFFFFF
            GameObject navPanel = CreatePanel(bottomNavGO, "NavPanel", lightNavBg, Vector2.zero, Vector2.one);
            Outline outline = navPanel.AddComponent<Outline>();
            outline.effectColor = BorderColor;

            Color activeTabColor = PrimaryOrangeColor;
            Color inactiveTabColor = MediumGrayColor;

            // 4 Clean Tabs with Vertical Icon + Text
            Button homeBtn = CreateButton(navPanel, "HomeBtn", "⌂\nHome", lightNavBg, activeScreen == ScreenState.Home ? activeTabColor : inactiveTabColor, new Vector2(0f, 0.05f), new Vector2(0.25f, 0.95f), Vector2.zero, Vector2.zero);
            Button modulesBtn = CreateButton(navPanel, "ModulesBtn", "▶\nTraining", lightNavBg, activeScreen == ScreenState.TrainingModules ? activeTabColor : inactiveTabColor, new Vector2(0.25f, 0.05f), new Vector2(0.5f, 0.95f), Vector2.zero, Vector2.zero);
            Button certBtn = CreateButton(navPanel, "CertBtn", "★\nCertificates", lightNavBg, activeScreen == ScreenState.CertificatePreview ? activeTabColor : inactiveTabColor, new Vector2(0.5f, 0.05f), new Vector2(0.75f, 0.95f), Vector2.zero, Vector2.zero);
            Button profileBtn = CreateButton(navPanel, "ProfileBtn", "●\nProfile", lightNavBg, activeScreen == ScreenState.Profile ? activeTabColor : inactiveTabColor, new Vector2(0.75f, 0.05f), new Vector2(1f, 0.95f), Vector2.zero, Vector2.zero);

            BottomNavigation botNav = bottomNavGO.AddComponent<BottomNavigation>();

            SerializedObject so = new SerializedObject(botNav);
            so.FindProperty("homeButton").objectReferenceValue = homeBtn;
            so.FindProperty("modulesButton").objectReferenceValue = modulesBtn;
            so.FindProperty("certificateButton").objectReferenceValue = certBtn;
            so.FindProperty("profileButton").objectReferenceValue = profileBtn;
            so.ApplyModifiedProperties();

            return bottomNavGO;
        }

        private static void CreateHomeScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject canvas = CreateCanvasWithEventSystem(scene, "HomeCanvas");

            // Soft Cool Slate Background (#EBF0F5)
            CreatePanel(canvas, "Background", BgColor, Vector2.zero, Vector2.one);

            // Dedicated Top Header Bar (Hamburger Button & Notification Button)
            GameObject hamburgerBox = CreatePanel(canvas, "HamburgerBox", BorderColor, new Vector2(0.06f, 0.92f), new Vector2(0.18f, 0.975f), roundedSquareSprite);
            Shadow hamShadow = hamburgerBox.AddComponent<Shadow>();
            hamShadow.effectColor = new Color(0.06f, 0.09f, 0.16f, 0.06f);
            hamShadow.effectDistance = new Vector2(0, -3);

            GameObject hamInner = CreatePanel(hamburgerBox, "Inner", CardBgColor, Vector2.zero, Vector2.one, roundedSquareSprite);
            RectTransform hiRt = hamInner.GetComponent<RectTransform>();
            hiRt.offsetMin = new Vector2(2, 2);
            hiRt.offsetMax = new Vector2(-2, -2);
            Button hamburgerBtn = hamburgerBox.AddComponent<Button>();
            CreateText(hamInner, "Icon", "☰", 44, DarkNavyColor, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FontStyle.Bold);

            GameObject notifBox = CreatePanel(canvas, "NotifBox", BorderColor, new Vector2(0.82f, 0.92f), new Vector2(0.94f, 0.975f), roundedSquareSprite);
            Shadow notifShadow = notifBox.AddComponent<Shadow>();
            notifShadow.effectColor = new Color(0.06f, 0.09f, 0.16f, 0.06f);
            notifShadow.effectDistance = new Vector2(0, -3);

            GameObject notifInner = CreatePanel(notifBox, "Inner", CardBgColor, Vector2.zero, Vector2.one, roundedSquareSprite);
            RectTransform niRt = notifInner.GetComponent<RectTransform>();
            niRt.offsetMin = new Vector2(2, 2);
            niRt.offsetMax = new Vector2(-2, -2);
            Button notifBtn = notifBox.AddComponent<Button>();
            CreateText(notifInner, "Icon", "🔔", 38, PrimaryOrangeColor, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FontStyle.Bold);

            // Greeting & Worker ID
            Text greetingText = CreateText(canvas, "Greeting", "Hello, Worker 👋", 46, DarkNavyColor, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.845f), new Vector2(0.94f, 0.905f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text workerIdText = CreateText(canvas, "WorkerID", "Worker ID: ---", 28, MediumGrayColor, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.80f), new Vector2(0.94f, 0.84f), Vector2.zero, Vector2.zero);

            // Training Progress Card (Rounded Card with left Orange Accent Bar)
            GameObject progCard = CreateCardPanel(canvas, "ProgressCard", new Vector2(0.06f, 0.66f), new Vector2(0.94f, 0.79f));
            CreatePanel(progCard, "Accent", PrimaryOrangeColor, new Vector2(0f, 0f), new Vector2(0.015f, 1f));

            Text progCardTitle = CreateText(progCard, "CardTitle", "TRAINING PROGRESS", 26, MediumGrayColor, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.72f), new Vector2(0.6f, 0.92f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text percentText = CreateText(progCard, "PercentText", "0%", 64, DarkNavyColor, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.32f), new Vector2(0.45f, 0.72f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text subText = CreateText(progCard, "Subtext", "0 of 3 modules completed", 26, MediumGrayColor, TextAnchor.MiddleRight, new Vector2(0.45f, 0.36f), new Vector2(0.94f, 0.65f), Vector2.zero, Vector2.zero);

            Slider pBar = CreateProgressBar(progCard, "ProgressBar", 0f, new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.28f));

            // Section Header: Current Training
            Text secTitle = CreateText(canvas, "SectionTitle", "Current Training", 36, DarkNavyColor, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.60f), new Vector2(0.94f, 0.65f), Vector2.zero, Vector2.zero, FontStyle.Bold);

            // Card 1: Fire & Explosion Response
            GameObject fireCard = CreateCardPanel(canvas, "FireCard", new Vector2(0.06f, 0.48f), new Vector2(0.94f, 0.58f));
            CreatePanel(fireCard, "Accent", SuccessGreenColor, new Vector2(0f, 0f), new Vector2(0.015f, 1f));

            GameObject fireIconBox = CreatePanel(fireCard, "IconBox", new Color(0.86f, 0.99f, 0.90f, 1f), new Vector2(0.04f, 0.18f), new Vector2(0.15f, 0.82f), roundedSquareSprite);
            CreateText(fireIconBox, "Icon", "⚡", 38, SuccessGreenColor, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FontStyle.Bold);

            Text fireTitle = CreateText(fireCard, "Title", "Fire & Explosion Response", 32, DarkNavyColor, TextAnchor.MiddleLeft, new Vector2(0.18f, 0.5f), new Vector2(0.70f, 0.88f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text fireDesc = CreateText(fireCard, "Progress", "Module 1", 26, MediumGrayColor, TextAnchor.MiddleLeft, new Vector2(0.18f, 0.12f), new Vector2(0.70f, 0.48f), Vector2.zero, Vector2.zero);
            
            GameObject firePill = CreatePanel(fireCard, "StatusPill", PrimaryOrangeColor, new Vector2(0.68f, 0.24f), new Vector2(0.96f, 0.76f), roundedPillSprite);
            Image fireBadgeImg = firePill.GetComponent<Image>();
            Text fireStatusTxt = CreateText(firePill, "Text", "Start", 24, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FontStyle.Bold);
            Button fireCardBtn = fireCard.transform.parent.gameObject.AddComponent<Button>();

            // Card 2: Gas Leak & Confined Space
            GameObject gasCard = CreateCardPanel(canvas, "GasCard", new Vector2(0.06f, 0.36f), new Vector2(0.94f, 0.46f));
            CreatePanel(gasCard, "Accent", PrimaryOrangeColor, new Vector2(0f, 0f), new Vector2(0.015f, 1f));

            GameObject gasIconBox = CreatePanel(gasCard, "IconBox", new Color(1f, 0.93f, 0.83f, 1f), new Vector2(0.04f, 0.18f), new Vector2(0.15f, 0.82f), roundedSquareSprite);
            CreateText(gasIconBox, "Icon", "♨", 38, PrimaryOrangeColor, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FontStyle.Bold);

            Text gasTitle = CreateText(gasCard, "Title", "Gas Leak & Confined Space", 32, DarkNavyColor, TextAnchor.MiddleLeft, new Vector2(0.18f, 0.5f), new Vector2(0.70f, 0.88f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text gasDesc = CreateText(gasCard, "Progress", "Module 2", 26, MediumGrayColor, TextAnchor.MiddleLeft, new Vector2(0.18f, 0.12f), new Vector2(0.70f, 0.48f), Vector2.zero, Vector2.zero);
            
            GameObject gasPill = CreatePanel(gasCard, "StatusPill", BorderColor, new Vector2(0.68f, 0.24f), new Vector2(0.96f, 0.76f), roundedPillSprite);
            Text gasStatusTxt = CreateText(gasPill, "Text", "Not Started", 24, MediumGrayColor, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FontStyle.Bold);
            Button gasCardBtn = gasCard.transform.parent.gameObject.AddComponent<Button>();

            // Card 3: Machinery Safety (Coming Soon)
            GameObject machCard = CreateCardPanel(canvas, "MachineryCard", new Vector2(0.06f, 0.24f), new Vector2(0.94f, 0.34f));
            CreatePanel(machCard, "Accent", MediumGrayColor, new Vector2(0f, 0f), new Vector2(0.015f, 1f));

            GameObject machIconBox = CreatePanel(machCard, "IconBox", new Color(0.94f, 0.96f, 0.98f, 1f), new Vector2(0.04f, 0.18f), new Vector2(0.15f, 0.82f), roundedSquareSprite);
            CreateText(machIconBox, "Icon", "■", 38, MediumGrayColor, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FontStyle.Bold);

            Text machTitle = CreateText(machCard, "Title", "Machinery Safety", 32, MediumGrayColor, TextAnchor.MiddleLeft, new Vector2(0.18f, 0.5f), new Vector2(0.70f, 0.88f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text machDesc = CreateText(machCard, "Progress", "Coming Soon", 26, MediumGrayColor, TextAnchor.MiddleLeft, new Vector2(0.18f, 0.12f), new Vector2(0.70f, 0.48f), Vector2.zero, Vector2.zero);
            
            GameObject machPill = CreatePanel(machCard, "StatusPill", BorderColor, new Vector2(0.68f, 0.24f), new Vector2(0.96f, 0.76f), roundedPillSprite);
            Text machStatusTxt = CreateText(machPill, "Text", "Locked", 24, MediumGrayColor, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FontStyle.Bold);
            Button machCardBtn = machCard.transform.parent.gameObject.AddComponent<Button>();

            // My Certificates Rounded Orange Banner
            GameObject certBanner = CreatePanel(canvas, "MyCertificatesBanner", PrimaryOrangeColor, new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.21f), roundedCardSprite);
            Shadow certShadow = certBanner.AddComponent<Shadow>();
            certShadow.effectColor = new Color(0.97f, 0.45f, 0.09f, 0.35f);
            certShadow.effectDistance = new Vector2(0, -5);

            CreateText(certBanner, "Icon", "★", 44, Color.white, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.2f), new Vector2(0.18f, 0.8f), Vector2.zero, Vector2.zero);
            Text certBtnText = CreateText(certBanner, "Title", "My Certificates", 36, Color.white, TextAnchor.MiddleLeft, new Vector2(0.20f, 0.2f), new Vector2(0.80f, 0.8f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            CreateText(certBanner, "Arrow", "›", 52, Color.white, TextAnchor.MiddleCenter, new Vector2(0.82f, 0.2f), new Vector2(0.96f, 0.8f), Vector2.zero, Vector2.zero, FontStyle.Bold);

            Button myCertBtn = certBanner.AddComponent<Button>();

            // Fixed Bottom Navigation Bar
            AddBottomNavigation(canvas, ScreenState.Home);

            // ==================== SIDE DRAWER SETUP ====================
            GameObject drawerContainer = new GameObject("SideDrawerContainer");
            drawerContainer.transform.SetParent(canvas.transform, false);

            RectTransform dcRt = drawerContainer.AddComponent<RectTransform>();
            dcRt.anchorMin = Vector2.zero;
            dcRt.anchorMax = Vector2.one;
            dcRt.offsetMin = Vector2.zero;
            dcRt.offsetMax = Vector2.zero;

            // Dimmed Backdrop Overlay
            GameObject backdropPanel = CreatePanel(drawerContainer, "Backdrop", new Color(0f, 0f, 0f, 0.6f), Vector2.zero, Vector2.one);
            Button backdropBtn = backdropPanel.AddComponent<Button>();

            // Left Drawer Panel (75% Width)
            GameObject drawerPanel = CreatePanel(drawerContainer, "DrawerPanel", CardBgColor, new Vector2(0f, 0f), new Vector2(0.75f, 1f));

            // Orange Header Profile Panel
            GameObject drawerHeader = CreatePanel(drawerPanel, "DrawerHeader", PrimaryOrangeColor, new Vector2(0f, 0.76f), new Vector2(1f, 1f));

            // Circular Avatar Placeholder
            GameObject avatarCircle = CreatePanel(drawerHeader, "AvatarCircle", Color.white, new Vector2(0.08f, 0.30f), new Vector2(0.28f, 0.78f), circleSprite);
            Text avatarInitial = CreateText(avatarCircle, "UserIcon", "W", 48, PrimaryOrangeColor, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FontStyle.Bold);

            Text drawerName = CreateText(drawerHeader, "Name", "Worker", 38, Color.white, TextAnchor.MiddleLeft, new Vector2(0.34f, 0.48f), new Vector2(0.95f, 0.85f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text drawerWorkerId = CreateText(drawerHeader, "WorkerID", "---", 26, new Color(1f, 0.9f, 0.8f, 1f), TextAnchor.MiddleLeft, new Vector2(0.34f, 0.15f), new Vector2(0.95f, 0.45f), Vector2.zero, Vector2.zero);

            // Drawer Links
            Button dHomeBtn = CreateButton(drawerPanel, "DHomeBtn", "  ⌂   Home", CardBgColor, DarkNavyColor, new Vector2(0.05f, 0.68f), new Vector2(0.95f, 0.75f), Vector2.zero, Vector2.zero);
            Button dTrainBtn = CreateButton(drawerPanel, "DTrainBtn", "  ▶   Training", CardBgColor, DarkNavyColor, new Vector2(0.05f, 0.60f), new Vector2(0.95f, 0.67f), Vector2.zero, Vector2.zero);
            Button dCertBtn = CreateButton(drawerPanel, "DCertBtn", "  ★   Certificates", CardBgColor, DarkNavyColor, new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.59f), Vector2.zero, Vector2.zero);
            Button dProfBtn = CreateButton(drawerPanel, "DProfBtn", "  ●   Profile", CardBgColor, DarkNavyColor, new Vector2(0.05f, 0.44f), new Vector2(0.95f, 0.51f), Vector2.zero, Vector2.zero);

            // Horizontal Divider
            CreatePanel(drawerPanel, "Divider", BorderColor, new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.425f));

            Button dOfflineBtn = CreateButton(drawerPanel, "DOfflineBtn", "  ▼   Offline Training", CardBgColor, DarkNavyColor, new Vector2(0.05f, 0.33f), new Vector2(0.95f, 0.40f), Vector2.zero, Vector2.zero);
            Button dAchieveBtn = CreateButton(drawerPanel, "DAchieveBtn", "  ✦   Achievements", CardBgColor, DarkNavyColor, new Vector2(0.05f, 0.25f), new Vector2(0.95f, 0.32f), Vector2.zero, Vector2.zero);
            Button dSettingsBtn = CreateButton(drawerPanel, "DSettingsBtn", "  ⚙   Settings", CardBgColor, DarkNavyColor, new Vector2(0.05f, 0.17f), new Vector2(0.95f, 0.24f), Vector2.zero, Vector2.zero);

            // Footer Version
            CreateText(drawerPanel, "VersionText", "SURAKSHAAR v1.0.0", 24, MediumGrayColor, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.03f), new Vector2(0.95f, 0.08f), Vector2.zero, Vector2.zero);

            SideDrawerController sideDrawerCtrl = drawerContainer.AddComponent<SideDrawerController>();
            SerializedObject sdSo = new SerializedObject(sideDrawerCtrl);
            sdSo.FindProperty("drawerContainer").objectReferenceValue = drawerContainer;
            sdSo.FindProperty("drawerPanel").objectReferenceValue = drawerPanel.GetComponent<RectTransform>();
            sdSo.FindProperty("backdropButton").objectReferenceValue = backdropBtn;
            sdSo.FindProperty("avatarInitialText").objectReferenceValue = avatarInitial;
            sdSo.FindProperty("profileNameText").objectReferenceValue = drawerName;
            sdSo.FindProperty("workerIdText").objectReferenceValue = drawerWorkerId;
            sdSo.FindProperty("homeButton").objectReferenceValue = dHomeBtn;
            sdSo.FindProperty("trainingButton").objectReferenceValue = dTrainBtn;
            sdSo.FindProperty("certificatesButton").objectReferenceValue = dCertBtn;
            sdSo.FindProperty("profileButton").objectReferenceValue = dProfBtn;
            sdSo.FindProperty("offlineTrainingButton").objectReferenceValue = dOfflineBtn;
            sdSo.FindProperty("achievementsButton").objectReferenceValue = dAchieveBtn;
            sdSo.FindProperty("settingsButton").objectReferenceValue = dSettingsBtn;
            sdSo.ApplyModifiedProperties();

            // ==================== NOTIFICATION POPUP OVERLAY ====================
            GameObject notifPopup = new GameObject("NotificationPopup");
            notifPopup.transform.SetParent(canvas.transform, false);
            RectTransform npRt = notifPopup.AddComponent<RectTransform>();
            npRt.anchorMin = Vector2.zero;
            npRt.anchorMax = Vector2.one;
            npRt.offsetMin = Vector2.zero;
            npRt.offsetMax = Vector2.zero;

            // Dimmed Backdrop Overlay
            GameObject npBackdrop = CreatePanel(notifPopup, "Backdrop", new Color(0f, 0f, 0f, 0.55f), Vector2.zero, Vector2.one);
            Button npBackdropBtn = npBackdrop.AddComponent<Button>();

            // Center Dialog Card
            GameObject npCard = CreatePanel(notifPopup, "DialogCard", CardBgColor, new Vector2(0.08f, 0.32f), new Vector2(0.92f, 0.68f), roundedCardSprite);
            Shadow npCardShadow = npCard.AddComponent<Shadow>();
            npCardShadow.effectColor = new Color(0.06f, 0.09f, 0.16f, 0.15f);
            npCardShadow.effectDistance = new Vector2(0, -6);

            // Dialog Header: Title & Close Button
            Text npTitle = CreateText(npCard, "Title", "Notifications", 36, DarkNavyColor, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.82f), new Vector2(0.75f, 0.95f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Button npCloseBtn = CreateButton(npCard, "CloseBtn", "✕", CardBgColor, DarkNavyColor, new Vector2(0.80f, 0.82f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero);

            // Subtle Divider
            CreatePanel(npCard, "Divider", BorderColor, new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.805f));

            // Content Items
            Text npItems = CreateText(npCard, "ItemsText", "• Welcome to SurakshaAR! Begin with Fire & Explosion Response.", 26, MediumGrayColor, TextAnchor.UpperLeft, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.76f), Vector2.zero, Vector2.zero);

            notifPopup.SetActive(false);

            // Home Controller Wireup
            GameObject controllerGO = new GameObject("HomeController");
            HomeScreenController ctrl = controllerGO.AddComponent<HomeScreenController>();

            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("hamburgerButton").objectReferenceValue = hamburgerBtn;
            so.FindProperty("notificationButton").objectReferenceValue = notifBtn;
            so.FindProperty("greetingText").objectReferenceValue = greetingText;
            so.FindProperty("workerIdText").objectReferenceValue = workerIdText;
            so.FindProperty("trainingProgressTitleText").objectReferenceValue = progCardTitle;
            so.FindProperty("progressPercentText").objectReferenceValue = percentText;
            so.FindProperty("progressSubtext").objectReferenceValue = subText;
            so.FindProperty("progressBar").objectReferenceValue = pBar;
            so.FindProperty("trainingModulesTitleText").objectReferenceValue = secTitle;
            so.FindProperty("fireModuleCardButton").objectReferenceValue = fireCardBtn;
            so.FindProperty("fireCardTitleText").objectReferenceValue = fireTitle;
            so.FindProperty("fireCardDescText").objectReferenceValue = fireDesc;
            so.FindProperty("fireStatusText").objectReferenceValue = fireStatusTxt;
            so.FindProperty("fireStatusBadge").objectReferenceValue = fireBadgeImg;
            so.FindProperty("gasModuleCardButton").objectReferenceValue = gasCardBtn;
            so.FindProperty("gasCardTitleText").objectReferenceValue = gasTitle;
            so.FindProperty("gasCardDescText").objectReferenceValue = gasDesc;
            so.FindProperty("gasStatusText").objectReferenceValue = gasStatusTxt;
            so.FindProperty("machineryModuleCardButton").objectReferenceValue = machCardBtn;
            so.FindProperty("machineryCardTitleText").objectReferenceValue = machTitle;
            so.FindProperty("machineryCardDescText").objectReferenceValue = machDesc;
            so.FindProperty("machineryStatusText").objectReferenceValue = machStatusTxt;
            so.FindProperty("myCertificatesButton").objectReferenceValue = myCertBtn;
            so.FindProperty("myCertificatesButtonText").objectReferenceValue = certBtnText;
            so.FindProperty("sideDrawer").objectReferenceValue = sideDrawerCtrl;
            so.FindProperty("notificationPopup").objectReferenceValue = notifPopup;
            so.FindProperty("notificationCloseButton").objectReferenceValue = npCloseBtn;
            so.FindProperty("notificationBackdropButton").objectReferenceValue = npBackdropBtn;
            so.FindProperty("notificationTitleText").objectReferenceValue = npTitle;
            so.FindProperty("notificationItemsText").objectReferenceValue = npItems;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/MainMenu/Home.unity");
        }

        private static TrainingModulesController.TopModuleCardUI CreateTopLevelModuleCard(
            GameObject parent, string modId, string defaultTitle, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject card = CreateCardPanel(parent, $"TopCard_{modId}", anchorMin, anchorMax);

            // Title
            Text titleTxt = CreateText(card, "Title", defaultTitle, 30, DarkNavyColor, TextAnchor.MiddleLeft,
                new Vector2(0.05f, 0.65f), new Vector2(0.66f, 0.95f), Vector2.zero, Vector2.zero, FontStyle.Bold);

            // Status Badge Pill
            GameObject badgePanel = CreatePanel(card, "StatusBadge", new Color(0.40f, 0.45f, 0.55f, 1f),
                new Vector2(0.67f, 0.70f), new Vector2(0.96f, 0.92f), roundedPillSprite);
            Image badgeImg = badgePanel.GetComponent<Image>();
            Text statusTxt = CreateText(badgePanel, "StatusText", "NOT STARTED", 16, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FontStyle.Bold);

            // Sub-modules summary
            Text progressTxt = CreateText(card, "ProgressSummary", "0 of 3 Sub-modules Complete", 22, MediumGrayColor, TextAnchor.MiddleLeft,
                new Vector2(0.05f, 0.35f), new Vector2(0.95f, 0.60f), Vector2.zero, Vector2.zero);

            // Action Button
            Button openBtn = CreateButton(card, "OpenBtn", "OPEN MODULE →", PrimaryOrangeColor, Color.white,
                new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.30f), Vector2.zero, Vector2.zero, isPill: true);
            Text openBtnTxt = openBtn.GetComponentInChildren<Text>();

            return new TrainingModulesController.TopModuleCardUI
            {
                moduleId = modId,
                titleText = titleTxt,
                statusText = statusTxt,
                statusBadge = badgeImg,
                progressSummaryText = progressTxt,
                openButton = openBtn,
                openButtonText = openBtnTxt
            };
        }

        private static void CreateTrainingModulesScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject canvas = CreateCanvasWithEventSystem(scene, "ModulesCanvas");

            CreatePanel(canvas, "Background", BgColor, Vector2.zero, Vector2.one);

            // =====================================================================
            // LEVEL 1: TOP-LEVEL MODULE LIST PANEL (Default Active)
            // =====================================================================
            GameObject listPanel = CreatePanel(canvas, "ModuleListPanel", Color.clear, new Vector2(0f, 0.09f), new Vector2(1f, 1f));

            // Header Bar: Back Button & Title
            Button headerBackBtn = CreateButton(listPanel, "HeaderBackButton", "←", BgColor, DarkNavyColor, new Vector2(0.04f, 0.92f), new Vector2(0.16f, 0.98f), Vector2.zero, Vector2.zero);
            Text headerTitle = CreateText(listPanel, "HeaderTitle", "TRAINING MODULES", 38, DarkNavyColor, TextAnchor.MiddleLeft, new Vector2(0.18f, 0.92f), new Vector2(0.94f, 0.98f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text headerSubhead = CreateText(listPanel, "HeaderSubhead", "Select a module to view training sub-modules & assessment.", 22, MediumGrayColor, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.87f), new Vector2(0.94f, 0.92f), Vector2.zero, Vector2.zero);

            // 3 Clean, spacious Module Cards
            var cardM1 = CreateTopLevelModuleCard(listPanel, "M1", "🔥 Fire & Explosion Response", new Vector2(0.05f, 0.60f), new Vector2(0.95f, 0.85f));
            var cardM2 = CreateTopLevelModuleCard(listPanel, "M2", "☣️ Gas Leak & Confined Space", new Vector2(0.05f, 0.33f), new Vector2(0.95f, 0.58f));
            var cardM3 = CreateTopLevelModuleCard(listPanel, "M3", "⚙️ Machinery Safety", new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.31f));

            // =====================================================================
            // LEVEL 2: DEDICATED MODULE DETAIL PANEL (Initially Inactive)
            // =====================================================================
            GameObject detailPanel = CreatePanel(canvas, "ModuleDetailPanel", Color.clear, new Vector2(0f, 0.09f), new Vector2(1f, 1f));

            // Detail Header Bar
            Button detailBackBtn = CreateButton(detailPanel, "DetailBackButton", "← BACK", SecondarySlateColor, Color.white, new Vector2(0.04f, 0.92f), new Vector2(0.24f, 0.98f), Vector2.zero, Vector2.zero, isPill: true);
            Text detailTitle = CreateText(detailPanel, "DetailTitle", "🔥 Fire & Explosion Response", 30, DarkNavyColor, TextAnchor.MiddleLeft, new Vector2(0.26f, 0.92f), new Vector2(0.72f, 0.98f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            
            GameObject detailBadge = CreatePanel(detailPanel, "DetailBadge", new Color(0.96f, 0.62f, 0.04f), new Vector2(0.74f, 0.92f), new Vector2(0.96f, 0.98f), roundedPillSprite);
            Image detailBadgeImg = detailBadge.GetComponent<Image>();
            Text detailStatus = CreateText(detailBadge, "DetailStatus", "IN PROGRESS", 16, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FontStyle.Bold);

            // =====================================================================
            // =====================================================================
            // 4 COMPACT SINGLE CARDS (Sub 1, Sub 2, Sub 3, and Assessment Simulation)
            // Sized compact like Home page cards (0.125 height with sleek horizontal buttons)
            // =====================================================================

            // Card 1: Sub-module 1 Card
            GameObject card1 = CreateCardPanel(detailPanel, "Card_Sub1", new Vector2(0.05f, 0.725f), new Vector2(0.95f, 0.850f));
            Text s1Title = CreateText(card1, "S1Title", "1. Fire & Extinguisher for electric fire", 24, DarkNavyColor, TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0.48f), new Vector2(0.68f, 0.92f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text s1Status = CreateText(card1, "S1Status", "Available", 18, MediumGrayColor, TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0.10f), new Vector2(0.68f, 0.48f), Vector2.zero, Vector2.zero);
            Button s1Btn = CreateButton(card1, "S1Btn", "START →", PrimaryOrangeColor, Color.white,
                new Vector2(0.70f, 0.20f), new Vector2(0.96f, 0.80f), Vector2.zero, Vector2.zero, isPill: false);
            Text s1BtnTxt = s1Btn.GetComponentInChildren<Text>();
            if (s1BtnTxt != null) s1BtnTxt.fontSize = 20;

            // Card 2: Sub-module 2 Card
            GameObject card2 = CreateCardPanel(detailPanel, "Card_Sub2", new Vector2(0.05f, 0.580f), new Vector2(0.95f, 0.705f));
            Text s2Title = CreateText(card2, "S2Title", "2. Fire & mud for petroleum fire", 24, DarkNavyColor, TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0.48f), new Vector2(0.68f, 0.92f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text s2Status = CreateText(card2, "S2Status", "Locked", 18, MediumGrayColor, TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0.10f), new Vector2(0.68f, 0.48f), Vector2.zero, Vector2.zero);
            Button s2Btn = CreateButton(card2, "S2Btn", "Locked", new Color(0.60f, 0.65f, 0.72f), Color.white,
                new Vector2(0.70f, 0.20f), new Vector2(0.96f, 0.80f), Vector2.zero, Vector2.zero, isPill: false);
            Text s2BtnTxt = s2Btn.GetComponentInChildren<Text>();
            if (s2BtnTxt != null) s2BtnTxt.fontSize = 20;

            // Card 3: Sub-module 3 Card
            GameObject card3 = CreateCardPanel(detailPanel, "Card_Sub3", new Vector2(0.05f, 0.435f), new Vector2(0.95f, 0.560f));
            Text s3Title = CreateText(card3, "S3Title", "3. Explosion & collision", 24, DarkNavyColor, TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0.48f), new Vector2(0.68f, 0.92f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text s3Status = CreateText(card3, "S3Status", "Locked", 18, MediumGrayColor, TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0.10f), new Vector2(0.68f, 0.48f), Vector2.zero, Vector2.zero);
            Button s3Btn = CreateButton(card3, "S3Btn", "Locked", new Color(0.60f, 0.65f, 0.72f), Color.white,
                new Vector2(0.70f, 0.20f), new Vector2(0.96f, 0.80f), Vector2.zero, Vector2.zero, isPill: false);
            Text s3BtnTxt = s3Btn.GetComponentInChildren<Text>();
            if (s3BtnTxt != null) s3BtnTxt.fontSize = 20;

            // Card 4: Assessment Simulation Card (ALWAYS Assessment, never Certificate)
            GameObject cardAssess = CreateCardPanel(detailPanel, "Card_Assessment", new Vector2(0.05f, 0.290f), new Vector2(0.95f, 0.415f));
            Text assessTitle = CreateText(cardAssess, "AssessTitle", "4. Assessment Simulation", 24, DarkNavyColor, TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0.48f), new Vector2(0.68f, 0.92f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text assessStatus = CreateText(cardAssess, "AssessStatus", "Locked (Complete all 3 sub-modules)", 18, MediumGrayColor, TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0.10f), new Vector2(0.68f, 0.48f), Vector2.zero, Vector2.zero);
            Button assessBtn = CreateButton(cardAssess, "AssessBtn", "Locked", SecondarySlateColor, Color.white,
                new Vector2(0.70f, 0.20f), new Vector2(0.96f, 0.80f), Vector2.zero, Vector2.zero, isPill: false);
            Text assessBtnTxt = assessBtn.GetComponentInChildren<Text>();
            if (assessBtnTxt != null) assessBtnTxt.fontSize = 20;

            // Hidden dummy cert row for binding compatibility
            GameObject certRow = new GameObject("CertRowDummy");
            certRow.transform.SetParent(cardAssess.transform, false);
            Text certStatus = certRow.AddComponent<Text>();
            Button viewCertBtn = certRow.AddComponent<Button>();
            Text viewCertBtnTxt = certRow.AddComponent<Text>();
            certRow.SetActive(false);

            detailPanel.SetActive(false);

            AddBottomNavigation(canvas, ScreenState.TrainingModules);

            GameObject controllerGO = new GameObject("ModulesController");
            TrainingModulesController ctrl = controllerGO.AddComponent<TrainingModulesController>();
            ctrl.SetupBindings(
                listPanel, headerBackBtn, headerTitle, headerSubhead,
                cardM1, cardM2, cardM3,
                detailPanel, detailBackBtn, detailTitle, detailStatus, detailBadgeImg,
                s1Title, s1Status, s1Btn, s1BtnTxt,
                s2Title, s2Status, s2Btn, s2BtnTxt,
                s3Title, s3Status, s3Btn, s3BtnTxt,
                assessTitle, assessStatus, assessBtn, assessBtnTxt,
                certRow, certStatus, viewCertBtn, viewCertBtnTxt
            );

            EditorUtility.SetDirty(ctrl);
            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/MainMenu/TrainingModules.unity");
        }

        private static void CreateFireModuleIntroScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject canvas = CreateCanvasWithEventSystem(scene, "FireIntroCanvas");

            CreatePanel(canvas, "Background", BgColor, Vector2.zero, Vector2.one);
            CreateText(canvas, "Title", "🔥 Fire & Explosion Response", 48, PrimaryOrangeColor, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.96f), Vector2.zero, Vector2.zero, FontStyle.Bold);

            string desc = "Learn how to identify fire hazards, raise an alarm, use an extinguisher and safely evacuate.";
            CreateText(canvas, "Desc", desc, 32, DarkNavyColor, TextAnchor.UpperLeft, new Vector2(0.08f, 0.77f), new Vector2(0.92f, 0.86f), Vector2.zero, Vector2.zero);

            GameObject card = CreateCardPanel(canvas, "LearnCard", new Vector2(0.08f, 0.3f), new Vector2(0.92f, 0.74f));
            CreateText(card, "Header", "What You'll Learn", 36, DarkNavyColor, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.84f), new Vector2(0.94f, 0.94f), Vector2.zero, Vector2.zero, FontStyle.Bold);

            string checklist = "✓ Identify fire hazards\n✓ Raise alarm\n✓ Find safe exit\n✓ Select and use extinguisher\n✓ Evacuate safely\n✓ Reach assembly point";
            CreateText(card, "Checklist", checklist, 30, MediumGrayColor, TextAnchor.UpperLeft, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.8f), Vector2.zero, Vector2.zero);

            Button learnBtn = CreateButton(canvas, "LearnBtn", "LEARN MODE →", PrimaryOrangeColor, Color.white, new Vector2(0.08f, 0.17f), new Vector2(0.92f, 0.27f), Vector2.zero, Vector2.zero);
            Button simBtn = CreateButton(canvas, "SimBtn", "SIMULATION →", SecondarySlateColor, Color.white, new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.15f), Vector2.zero, Vector2.zero);

            GameObject controllerGO = new GameObject("FireIntroController");
            FireModuleIntroController ctrl = controllerGO.AddComponent<FireModuleIntroController>();

            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("learnModeButton").objectReferenceValue = learnBtn;
            so.FindProperty("simulationButton").objectReferenceValue = simBtn;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/Fire/FireModuleIntro.unity");
        }

        /// <summary>
        /// PHASE 1 — Real Camera AR Foundation Scene.
        /// Opens the rear camera, detects real floor planes, lets user tap to set a persistent anchor.
        /// NO virtual environment, no cubes, no primitives.
        /// </summary>
        [MenuItem("SurakshaAR/Build Phase 1 AR Foundation Scene Only")]
        public static void CreateARPhase1Scene()
        {
            defaultFont = defaultFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 16);
            InitializeSprites();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── 1. AR Session (ARCore real camera) ────────────────────────────────
            // ARSession is the top-level AR lifecycle manager (unchanged in AF6)
            GameObject arSessionGO = new GameObject("AR Session");
            arSessionGO.AddComponent<ARSession>();
            // Note: ARInputManager was removed in AR Foundation 5+. No replacement needed.

            // ── 2. XR Origin + AR Camera ─────────────────────────────────────────
            // In AR Foundation 5+, XROrigin manages the camera hierarchy and session-space origin.
            GameObject arOriginGO = new GameObject("XR Origin");
            XROrigin xrOrigin = arOriginGO.AddComponent<XROrigin>();

            // Camera Offset child (standard AR Foundation 6 hierarchy)
            GameObject cameraOffsetGO = new GameObject("Camera Offset");
            cameraOffsetGO.transform.SetParent(arOriginGO.transform);

            // AR Camera under Camera Offset
            GameObject arCameraGO = new GameObject("AR Camera");
            arCameraGO.transform.SetParent(cameraOffsetGO.transform);
            Camera arCam = arCameraGO.AddComponent<Camera>();
            arCam.clearFlags = CameraClearFlags.SolidColor;
            arCam.backgroundColor = Color.black;
            arCam.nearClipPlane = 0.1f;
            arCameraGO.tag = "MainCamera";
            arCameraGO.AddComponent<ARCameraManager>();
            arCameraGO.AddComponent<ARCameraBackground>();
            arCameraGO.AddComponent<TrackedPoseDriver>();

            xrOrigin.Camera = arCam;
            xrOrigin.Origin = arOriginGO;
            xrOrigin.CameraFloorOffsetObject = cameraOffsetGO;

            // ── 3. AR Trackable Managers (on XR Origin) ───────────────────────────
            // In AR Foundation 6, all trackable managers live on the XR Origin object
            ARPlaneManager planeManager = arOriginGO.AddComponent<ARPlaneManager>();
            planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            ARRaycastManager raycastManager = arOriginGO.AddComponent<ARRaycastManager>();
            ARAnchorManager anchorManager = arOriginGO.AddComponent<ARAnchorManager>();


            // ── 4. Phase 1 Controller ──────────────────────────────────────────────
            GameObject controllerGO = new GameObject("ARPhase1_Controller");
            ARPhase1FloorScanController phase1Ctrl = controllerGO.AddComponent<ARPhase1FloorScanController>();

            // ── 5. Minimal HUD Canvas — LIVE CAMERA BACKGROUND ────────────────────
            // Canvas is Screen Space Overlay — sits on top of the real AR camera feed
            GameObject canvasGO = CreateCanvasWithEventSystem(scene, "Phase1_HUD_Canvas");

            // ── Top Status Bar (5% height) ─────────────────────────────────────────
            GameObject topBar = CreatePanel(canvasGO, "TopStatusBar",
                new Color(0f, 0f, 0f, 0.55f),
                new Vector2(0f, 0.94f), new Vector2(1f, 1f));

            // App name left
            CreateText(topBar, "AppName", "SurakshaAR",
                28, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0.03f, 0.1f), new Vector2(0.5f, 0.9f),
                Vector2.zero, Vector2.zero, FontStyle.Bold);

            // Status badge text right
            Text statusText = CreateText(topBar, "StatusBadge", "Scanning floor.",
                24, WarningAmberColor, TextAnchor.MiddleRight,
                new Vector2(0.5f, 0.1f), new Vector2(0.97f, 0.9f),
                Vector2.zero, Vector2.zero, FontStyle.Bold);

            // ── Centre Instruction Card (visible during scan, transparent bg) ────
            GameObject instructCard = CreatePanel(canvasGO, "InstructionCard",
                new Color(0f, 0f, 0f, 0.60f),
                new Vector2(0.05f, 0.36f), new Vector2(0.95f, 0.52f));

            Text instrText = CreateText(instructCard, "InstructionText",
                "Scan the ground and move your phone slowly.",
                32, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f),
                Vector2.zero, Vector2.zero, FontStyle.Normal);

            // ── Tap Hint Panel (hidden until floor detected) ─────────────────────
            GameObject tapHintPanel = CreatePanel(canvasGO, "TapHintPanel",
                new Color(0.06f, 0.73f, 0.51f, 0.85f),
                new Vector2(0.15f, 0.14f), new Vector2(0.85f, 0.23f));
            CreateText(tapHintPanel, "TapHintText",
                "TAP THE FLOOR to place your training anchor",
                28, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.03f, 0.1f), new Vector2(0.97f, 0.9f),
                Vector2.zero, Vector2.zero, FontStyle.Bold);
            tapHintPanel.SetActive(false);

            // ── Anchor Confirmed Panel (hidden until anchor placed) ───────────────
            GameObject anchorConfPanel = CreatePanel(canvasGO, "AnchorConfirmedPanel",
                new Color(0.06f, 0.73f, 0.51f, 0.92f),
                new Vector2(0.05f, 0.36f), new Vector2(0.95f, 0.56f));
            CreateText(anchorConfPanel, "ConfirmedIcon", "✓",
                72, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.02f, 0.55f), new Vector2(0.98f, 0.98f),
                Vector2.zero, Vector2.zero, FontStyle.Bold);
            CreateText(anchorConfPanel, "ConfirmedText",
                "Training position locked!\nYour anchor is stable in the real world.",
                28, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.54f),
                Vector2.zero, Vector2.zero, FontStyle.Normal);
            anchorConfPanel.SetActive(false);

            // ── Continue Button (shown after anchor placed) ───────────────────────
            Button continueBtn = CreateButton(canvasGO, "ContinueBtn",
                "CONTINUE TO TRAINING  →",
                PrimaryOrangeColor, Color.white,
                new Vector2(0.1f, 0.04f), new Vector2(0.9f, 0.14f),
                Vector2.zero, Vector2.zero);
            continueBtn.gameObject.SetActive(false);

            // ── Wire Phase1 Controller Fields ─────────────────────────────────────
            SerializedObject ctrlSO = new SerializedObject(phase1Ctrl);
            ctrlSO.FindProperty("arSession").objectReferenceValue = arSessionGO.GetComponent<ARSession>();
            ctrlSO.FindProperty("planeManager").objectReferenceValue = planeManager;
            ctrlSO.FindProperty("raycastManager").objectReferenceValue = raycastManager;
            ctrlSO.FindProperty("anchorManager").objectReferenceValue = anchorManager;
            ctrlSO.FindProperty("arCamera").objectReferenceValue = arCam;
            ctrlSO.FindProperty("instructionText").objectReferenceValue = instrText;
            ctrlSO.FindProperty("statusBadgeText").objectReferenceValue = statusText;
            ctrlSO.FindProperty("tapHintPanel").objectReferenceValue = tapHintPanel;
            ctrlSO.FindProperty("anchorConfirmedPanel").objectReferenceValue = anchorConfPanel;
            ctrlSO.FindProperty("continueButton").objectReferenceValue = continueBtn;
            ctrlSO.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/Fire/ARPhase1_FloorScan.unity");
            Debug.Log("[SceneBuilder] Phase 1 AR Foundation Scene built: ARPhase1_FloorScan.unity");
        }

        private static void CreateARLearnPlaceholderScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera & Manager Setup
            GameObject cameraGO = new GameObject("Main Camera");
            Camera cameraComp = cameraGO.AddComponent<Camera>();
            cameraGO.tag = "MainCamera";
            cameraGO.transform.position = new Vector3(0, 1.6f, 0); // 1.6m eye level standing height
            cameraGO.transform.rotation = Quaternion.identity;

            GameObject managersGO = new GameObject("AR_Training_Managers");
            FireLearnController learnController = managersGO.AddComponent<FireLearnController>();
            ARTrainingPlacementManager placementManager = managersGO.AddComponent<ARTrainingPlacementManager>();
            ARTrainingAnchorManager anchorManager = managersGO.AddComponent<ARTrainingAnchorManager>();
            MovementTrackingManager trackingManager = managersGO.AddComponent<MovementTrackingManager>();
            AudioHapticManager audioManager = managersGO.AddComponent<AudioHapticManager>();
            AdaptiveHelpController helpController = managersGO.AddComponent<AdaptiveHelpController>();
            FireTelemetryLogger telemetryLogger = managersGO.AddComponent<FireTelemetryLogger>();
            ARSessionController arSessionCtrl = managersGO.AddComponent<ARSessionController>();
            ARPlaneController arPlaneCtrl = managersGO.AddComponent<ARPlaneController>();
            MistakePatternAnalyzer mistakeAnalyzer = managersGO.AddComponent<MistakePatternAnalyzer>();
            AdaptiveScenarioManager scenarioManager = managersGO.AddComponent<AdaptiveScenarioManager>();
            AdaptiveTrainingManager adaptiveTrainingManager = managersGO.AddComponent<AdaptiveTrainingManager>();

            // 2. 3D Spatial AR Scenario Objects Creation
            GameObject scenarioGroup = new GameObject("3D_AR_Scenario");

            // Fire Hazard (Red Cube/Sphere at (0, 0.5f, 3f))
            GameObject fireGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fireGO.name = "3D_FireHazard";
            fireGO.transform.parent = scenarioGroup.transform;
            fireGO.transform.position = new Vector3(0f, 0.5f, 3.0f);
            fireGO.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            fireGO.GetComponent<Renderer>().material.color = Color.red;
            TrainingObject fireObject = fireGO.AddComponent<TrainingObject>();
            SerializedObject fireSo = new SerializedObject(fireObject);
            fireSo.FindProperty("objectType").enumValueIndex = (int)TrainingObjectType.FireHazard;
            fireSo.FindProperty("objectName").stringValue = "Electrical Fire Hazard";
            fireSo.ApplyModifiedProperties();

            // Alarm Button (Amber Box at (-1.8f, 1.4f, 2.5f))
            GameObject alarmGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            alarmGO.name = "3D_AlarmButton";
            alarmGO.transform.parent = scenarioGroup.transform;
            alarmGO.transform.position = new Vector3(-1.8f, 1.4f, 2.5f);
            alarmGO.transform.localScale = new Vector3(0.3f, 0.4f, 0.2f);
            alarmGO.GetComponent<Renderer>().material.color = WarningAmberColor;
            TrainingObject alarmObject = alarmGO.AddComponent<TrainingObject>();
            SerializedObject alarmSo = new SerializedObject(alarmObject);
            alarmSo.FindProperty("objectType").enumValueIndex = (int)TrainingObjectType.AlarmButton;
            alarmSo.FindProperty("objectName").stringValue = "Emergency Call Point";
            alarmSo.ApplyModifiedProperties();

            // Safe Exit Door (Green Frame at (2.0f, 1.0f, 4.0f))
            GameObject safeExitGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            safeExitGO.name = "3D_SafeExitDoor";
            safeExitGO.transform.parent = scenarioGroup.transform;
            safeExitGO.transform.position = new Vector3(2.0f, 1.0f, 4.0f);
            safeExitGO.transform.localScale = new Vector3(1.2f, 2.2f, 0.1f);
            safeExitGO.GetComponent<Renderer>().material.color = SuccessGreenColor;
            TrainingObject safeExitObject = safeExitGO.AddComponent<TrainingObject>();
            SerializedObject safeExitSo = new SerializedObject(safeExitObject);
            safeExitSo.FindProperty("objectType").enumValueIndex = (int)TrainingObjectType.SafeExit;
            safeExitSo.FindProperty("objectName").stringValue = "Safe Exit Door A";
            safeExitSo.ApplyModifiedProperties();

            // Blocked Exit Door (Smoke Filled Door at (-2.0f, 1.0f, 4.0f))
            GameObject blockedExitGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blockedExitGO.name = "3D_BlockedExitDoor";
            blockedExitGO.transform.parent = scenarioGroup.transform;
            blockedExitGO.transform.position = new Vector3(-2.0f, 1.0f, 4.0f);
            blockedExitGO.transform.localScale = new Vector3(1.2f, 2.2f, 0.1f);
            blockedExitGO.GetComponent<Renderer>().material.color = DarkNavyColor;
            TrainingObject blockedExitObject = blockedExitGO.AddComponent<TrainingObject>();
            SerializedObject blockedExitSo = new SerializedObject(blockedExitObject);
            blockedExitSo.FindProperty("objectType").enumValueIndex = (int)TrainingObjectType.BlockedExit;
            blockedExitSo.FindProperty("objectName").stringValue = "Smoke Blocked Exit B";
            blockedExitSo.ApplyModifiedProperties();

            // CO2 Extinguisher (Orange/Red Cylinder at (-0.8f, 0.4f, 1.8f))
            GameObject co2ExtGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            co2ExtGO.name = "3D_CO2Extinguisher";
            co2ExtGO.transform.parent = scenarioGroup.transform;
            co2ExtGO.transform.position = new Vector3(-0.8f, 0.4f, 1.8f);
            co2ExtGO.transform.localScale = new Vector3(0.25f, 0.5f, 0.25f);
            co2ExtGO.GetComponent<Renderer>().material.color = PrimaryOrangeColor;
            TrainingObject co2ExtObject = co2ExtGO.AddComponent<TrainingObject>();
            SerializedObject co2So = new SerializedObject(co2ExtObject);
            co2So.FindProperty("objectType").enumValueIndex = (int)TrainingObjectType.CO2Extinguisher;
            co2So.FindProperty("objectName").stringValue = "CO2 Extinguisher (Electrical)";
            co2So.ApplyModifiedProperties();

            // Water Extinguisher (Blue Cylinder at (0.8f, 0.4f, 1.8f))
            GameObject waterExtGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            waterExtGO.name = "3D_WaterExtinguisher";
            waterExtGO.transform.parent = scenarioGroup.transform;
            waterExtGO.transform.position = new Vector3(0.8f, 0.4f, 1.8f);
            waterExtGO.transform.localScale = new Vector3(0.25f, 0.5f, 0.25f);
            waterExtGO.GetComponent<Renderer>().material.color = Color.blue;
            TrainingObject waterExtObject = waterExtGO.AddComponent<TrainingObject>();
            SerializedObject waterSo = new SerializedObject(waterExtObject);
            waterSo.FindProperty("objectType").enumValueIndex = (int)TrainingObjectType.WaterExtinguisher;
            waterSo.FindProperty("objectName").stringValue = "Water Extinguisher";
            waterSo.ApplyModifiedProperties();

            // Assembly Point Beacon (Green Cylinder at (3.5f, 0.1f, 6.0f))
            GameObject assemblyGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            assemblyGO.name = "3D_AssemblyZone";
            assemblyGO.transform.parent = scenarioGroup.transform;
            assemblyGO.transform.position = new Vector3(3.5f, 0.1f, 6.0f);
            assemblyGO.transform.localScale = new Vector3(1.5f, 0.1f, 1.5f);
            assemblyGO.GetComponent<Renderer>().material.color = SuccessGreenColor;
            TrainingObject assemblyObject = assemblyGO.AddComponent<TrainingObject>();
            SerializedObject assemblySo = new SerializedObject(assemblyObject);
            assemblySo.FindProperty("objectType").enumValueIndex = (int)TrainingObjectType.AssemblyZone;
            assemblySo.FindProperty("objectName").stringValue = "Emergency Assembly Point";
            assemblySo.ApplyModifiedProperties();

            // Overhead Smoke Layer Quad (Grey at (0f, 2.2f, 3.0f))
            GameObject smokeGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
            smokeGO.name = "3D_SmokeLayer";
            smokeGO.transform.parent = scenarioGroup.transform;
            smokeGO.transform.position = new Vector3(0f, 2.2f, 3.0f);
            smokeGO.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            smokeGO.transform.localScale = new Vector3(10f, 10f, 1f);
            smokeGO.GetComponent<Renderer>().material.color = new Color(0.3f, 0.3f, 0.3f, 0.7f);
            smokeGO.SetActive(false); // Hidden until evacuation step

            // 3. Wire FireLearnController Serialized Fields
            SerializedObject controllerSO = new SerializedObject(learnController);
            controllerSO.FindProperty("fireHazardObject").objectReferenceValue = fireObject;
            controllerSO.FindProperty("alarmButtonObject").objectReferenceValue = alarmObject;
            controllerSO.FindProperty("safeExitObject").objectReferenceValue = safeExitObject;
            controllerSO.FindProperty("blockedExitObject").objectReferenceValue = blockedExitObject;
            controllerSO.FindProperty("waterExtinguisherObject").objectReferenceValue = waterExtObject;
            controllerSO.FindProperty("co2ExtinguisherObject").objectReferenceValue = co2ExtObject;
            controllerSO.FindProperty("assemblyZoneObject").objectReferenceValue = assemblyObject;
            controllerSO.FindProperty("smokeLayerObject").objectReferenceValue = smokeGO;
            controllerSO.FindProperty("arCameraTransform").objectReferenceValue = cameraGO.transform;
            controllerSO.ApplyModifiedProperties();

            // 4. UI Canvas & HUD Overlay (<15% screen space)
            GameObject canvas = CreateCanvasWithEventSystem(scene, "ARLearnCanvas");

            // Top HUD Overlay
            GameObject topHud = CreateCardPanel(canvas, "TopHUD", new Vector2(0.04f, 0.85f), new Vector2(0.96f, 0.98f));
            Text stepText = CreateText(topHud, "StepText", "Step 1 of 8", 28, DarkNavyColor, TextAnchor.MiddleLeft, new Vector2(0.05f, 0.6f), new Vector2(0.5f, 0.9f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Slider pBar = CreateProgressBar(topHud, "ProgressBar", 1f / 8f, new Vector2(0.52f, 0.65f), new Vector2(0.95f, 0.85f));

            Text hazardText = CreateText(topHud, "HazardText", "🔥 FIRE DETECTED", 28, Color.red, TextAnchor.MiddleLeft, new Vector2(0.05f, 0.15f), new Vector2(0.45f, 0.55f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text instText = CreateText(topHud, "InstText", "Locate and select the active electrical fire hazard.", 26, DarkNavyColor, TextAnchor.MiddleLeft, new Vector2(0.48f, 0.15f), new Vector2(0.95f, 0.55f), Vector2.zero, Vector2.zero);

            // Contextual Feedback Overlay Card (Hidden by default)
            GameObject feedbackCardGO = CreateCardPanel(canvas, "FeedbackCard", new Vector2(0.1f, 0.40f), new Vector2(0.9f, 0.68f));
            CreateText(feedbackCardGO, "Header", "⚠ Try Again", 34, Color.red, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.70f), new Vector2(0.95f, 0.92f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            CreateText(feedbackCardGO, "Explanation", "Follow instructions to complete step.", 26, DarkNavyColor, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.35f), new Vector2(0.95f, 0.68f), Vector2.zero, Vector2.zero);
            Button tryAgainBtn = CreateButton(feedbackCardGO, "TryAgainBtn", "TRY AGAIN", PrimaryOrangeColor, Color.white, new Vector2(0.1f, 0.06f), new Vector2(0.48f, 0.30f), Vector2.zero, Vector2.zero);
            Button cardHelpBtn = CreateButton(feedbackCardGO, "CardHelpBtn", "NEED HELP?", SecondarySlateColor, Color.white, new Vector2(0.52f, 0.06f), new Vector2(0.90f, 0.30f), Vector2.zero, Vector2.zero);

            FeedbackCard fbCardComp = feedbackCardGO.AddComponent<FeedbackCard>();
            SerializedObject fbSo = new SerializedObject(fbCardComp);
            fbSo.FindProperty("tryAgainButton").objectReferenceValue = tryAgainBtn;
            fbSo.FindProperty("needHelpButton").objectReferenceValue = cardHelpBtn;
            fbSo.ApplyModifiedProperties();
            feedbackCardGO.SetActive(false);

            // Bottom Action Bar (<15% screen space)
            GameObject botHud = CreatePanel(canvas, "BottomBar", new Color(0, 0, 0, 0.35f), new Vector2(0, 0), new Vector2(1, 0.15f));
            Button backBtn = CreateButton(botHud, "BackBtn", "← BACK", SecondarySlateColor, Color.white, new Vector2(0.04f, 0.25f), new Vector2(0.24f, 0.75f), Vector2.zero, Vector2.zero);
            Button sprayBtn = CreateButton(botHud, "SprayBtn", "💨 SPRAY", PrimaryOrangeColor, Color.white, new Vector2(0.28f, 0.25f), new Vector2(0.72f, 0.75f), Vector2.zero, Vector2.zero);
            Button helpBtn = CreateButton(botHud, "HelpBtn", "NEED HELP?", WarningAmberColor, Color.white, new Vector2(0.76f, 0.25f), new Vector2(0.96f, 0.75f), Vector2.zero, Vector2.zero);

            GameObject controllerGO = new GameObject("LearnPlaceholderController");
            LearnPlaceholderController ctrl = controllerGO.AddComponent<LearnPlaceholderController>();

            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("stepCounterText").objectReferenceValue = stepText;
            so.FindProperty("progressBar").objectReferenceValue = pBar;
            so.FindProperty("hazardStatusText").objectReferenceValue = hazardText;
            so.FindProperty("instructionText").objectReferenceValue = instText;
            so.FindProperty("sprayButton").objectReferenceValue = sprayBtn;
            so.FindProperty("needHelpButton").objectReferenceValue = helpBtn;
            so.FindProperty("backButton").objectReferenceValue = backBtn;
            so.FindProperty("feedbackCard").objectReferenceValue = fbCardComp;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/Fire/ARLearnPlaceholder.unity");
        }

        private static void CreateARSimulationPlaceholderScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject canvas = CreateCanvasWithEventSystem(scene, "ARSimCanvas");

            GameObject topHud = CreateCardPanel(canvas, "TopHUD", new Vector2(0.04f, 0.85f), new Vector2(0.96f, 0.98f));
            Text statusText = CreateText(topHud, "StatusText", "FIRE RESPONSE — SIMULATION\nComplete safety procedure using AR environment.", 28, DarkNavyColor, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero, FontStyle.Bold);

            Button exitBtn = CreateButton(canvas, "ExitBtn", "EXIT SIMULATION", SecondarySlateColor, Color.white, new Vector2(0.2f, 0.04f), new Vector2(0.8f, 0.12f), Vector2.zero, Vector2.zero);

            GameObject controllerGO = new GameObject("SimPlaceholderController");
            SimulationPlaceholderController ctrl = controllerGO.AddComponent<SimulationPlaceholderController>();

            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("exitSimulationButton").objectReferenceValue = exitBtn;
            so.FindProperty("statusText").objectReferenceValue = statusText;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/Fire/ARSimulationPlaceholder.unity");
        }

        private static void CreateResultScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject canvas = CreateCanvasWithEventSystem(scene, "ResultCanvas");

            CreatePanel(canvas, "Background", BgColor, Vector2.zero, Vector2.one);
            Text titleText = CreateText(canvas, "Title", "Training Completed 🎉", 52, SuccessGreenColor, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text scoreText = CreateText(canvas, "ScoreText", "85%", 80, PrimaryOrangeColor, TextAnchor.MiddleCenter, new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.84f), Vector2.zero, Vector2.zero, FontStyle.Bold);

            GameObject perfCard = CreateCardPanel(canvas, "PerfCard", new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.70f));
            Text metricsText = CreateText(perfCard, "MetricsText", "Performance:\n• Correct Actions: 7/8\n• Mistakes: 2\n• Retries: 1\n• Time: 03:42", 30, DarkNavyColor, TextAnchor.UpperLeft, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f), Vector2.zero, Vector2.zero);

            GameObject impCard = CreateCardPanel(canvas, "ImpCard", new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.39f));
            Text areasText = CreateText(impCard, "AreasText", "Areas to Improve:\n• Fire extinguisher technique", 30, DarkNavyColor, TextAnchor.UpperLeft, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f), Vector2.zero, Vector2.zero);

            Button practiceBtn = CreateButton(canvas, "PracticeBtn", "PRACTICE AGAIN", SecondarySlateColor, Color.white, new Vector2(0.08f, 0.12f), new Vector2(0.48f, 0.21f), Vector2.zero, Vector2.zero);
            Button continueBtn = CreateButton(canvas, "ContinueBtn", "CONTINUE →", PrimaryOrangeColor, Color.white, new Vector2(0.52f, 0.12f), new Vector2(0.92f, 0.21f), Vector2.zero, Vector2.zero);

            GameObject controllerGO = new GameObject("ResultController");
            ResultScreenController ctrl = controllerGO.AddComponent<ResultScreenController>();

            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("headerTitleText").objectReferenceValue = titleText;
            so.FindProperty("scoreText").objectReferenceValue = scoreText;
            so.FindProperty("metricsText").objectReferenceValue = metricsText;
            so.FindProperty("areasToImproveText").objectReferenceValue = areasText;
            so.FindProperty("practiceAgainButton").objectReferenceValue = practiceBtn;
            so.FindProperty("continueButton").objectReferenceValue = continueBtn;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/MainMenu/Result.unity");
        }

        private static void CreatePerformanceSummaryScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject canvas = CreateCanvasWithEventSystem(scene, "PerfCanvas");

            CreatePanel(canvas, "Background", BgColor, Vector2.zero, Vector2.one);
            CreateText(canvas, "Title", "Performance Summary", 50, DarkNavyColor, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero, FontStyle.Bold);

            GameObject card = CreateCardPanel(canvas, "PerfCard", new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.82f));
            string info = "Strengths:\n• Quick hazard identification\n\nAreas to Improve:\n• Extinguisher pin pulling sequence\n\nRecommended Retraining:\n• Fire Safety Refresher Module";
            CreateText(card, "Info", info, 32, DarkNavyColor, TextAnchor.UpperLeft, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f), Vector2.zero, Vector2.zero);

            Button homeBtn = CreateButton(canvas, "HomeBtn", "BACK TO HOME", PrimaryOrangeColor, Color.white, new Vector2(0.15f, 0.08f), new Vector2(0.85f, 0.18f), Vector2.zero, Vector2.zero);

            GameObject controllerGO = new GameObject("PerfController");
            PerformanceSummaryController ctrl = controllerGO.AddComponent<PerformanceSummaryController>();

            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("backToHomeButton").objectReferenceValue = homeBtn;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/MainMenu/PerformanceSummary.unity");
        }

        private static CertificateScreenController.CertCardUI CreateCertCard(
            GameObject parent, string modId, string defaultTitle, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject card = CreateCardPanel(parent, $"CertCard_{modId}", anchorMin, anchorMax);

            Text title = CreateText(card, "Title", defaultTitle, 25, DarkNavyColor, TextAnchor.MiddleLeft,
                new Vector2(0.05f, 0.52f), new Vector2(0.68f, 0.90f), Vector2.zero, Vector2.zero, FontStyle.Bold);

            Text status = CreateText(card, "Status", "🔒 Locked", 18, SecondarySlateColor, TextAnchor.MiddleLeft,
                new Vector2(0.05f, 0.12f), new Vector2(0.68f, 0.48f), Vector2.zero, Vector2.zero);

            Button viewBtn = CreateButton(card, "ViewBtn", "Locked", SecondarySlateColor, Color.white,
                new Vector2(0.70f, 0.22f), new Vector2(0.95f, 0.78f), Vector2.zero, Vector2.zero, isPill: false);

            return new CertificateScreenController.CertCardUI
            {
                moduleId = modId,
                cardPanel = card,
                titleText = title,
                statusText = status,
                viewButton = viewBtn,
                viewButtonText = viewBtn.GetComponentInChildren<Text>(),
                viewButtonImage = viewBtn.GetComponent<Image>()
            };
        }

        private static void CreateCertificatePreviewScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject canvas = CreateCanvasWithEventSystem(scene, "CertCanvas");

            CreatePanel(canvas, "Background", BgColor, Vector2.zero, Vector2.one);

            // Header Bar
            Button backBtn = CreateButton(canvas, "BackBtn", "←", SecondarySlateColor, Color.white, new Vector2(0.05f, 0.90f), new Vector2(0.16f, 0.96f), Vector2.zero, Vector2.zero, isPill: true);
            Text headerTitle = CreateText(canvas, "Title", "CERTIFICATES", 36, DarkNavyColor, TextAnchor.MiddleLeft, new Vector2(0.18f, 0.90f), new Vector2(0.95f, 0.96f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text headerSubtitle = CreateText(canvas, "Subtitle", "Official Mining Safety Certificates • Issued upon passing module assessments.", 19, MediumGrayColor, TextAnchor.MiddleLeft, new Vector2(0.05f, 0.845f), new Vector2(0.95f, 0.895f), Vector2.zero, Vector2.zero);

            // 4 Separate Certificate Cards
            CertificateScreenController.CertCardUI cardM1 = CreateCertCard(canvas, "M1", "🔥 Module 1: Fire & Explosion Response", new Vector2(0.05f, 0.680f), new Vector2(0.95f, 0.825f));
            CertificateScreenController.CertCardUI cardM2 = CreateCertCard(canvas, "M2", "☣ Module 2: Gas Leak & Confined Space", new Vector2(0.05f, 0.515f), new Vector2(0.95f, 0.660f));
            CertificateScreenController.CertCardUI cardM3 = CreateCertCard(canvas, "M3", "⚙ Module 3: Machinery Safety", new Vector2(0.05f, 0.350f), new Vector2(0.95f, 0.495f));
            CertificateScreenController.CertCardUI cardTotal = CreateCertCard(canvas, "TOTAL", "🏆 Total Mining Safety Certification", new Vector2(0.05f, 0.185f), new Vector2(0.95f, 0.330f));

            // Bottom Actions
            Button verifyBtn = CreateButton(canvas, "VerifyBtn", "VERIFY CERTIFICATE", SecondarySlateColor, Color.white, new Vector2(0.08f, 0.095f), new Vector2(0.92f, 0.155f), Vector2.zero, Vector2.zero);

            // Bottom Navigation
            AddBottomNavigation(canvas, ScreenState.CertificatePreview);

            // Certificate Detail Modal (Overlay)
            GameObject modalOverlay = CreatePanel(canvas, "CertDetailModal", new Color(0.05f, 0.08f, 0.15f, 0.75f), Vector2.zero, Vector2.one);
            GameObject modalCard = CreateCardPanel(modalOverlay, "ModalCard", new Vector2(0.06f, 0.09f), new Vector2(0.94f, 0.93f));

            Button modalCloseBtn = CreateButton(modalCard, "CloseBtn", "✕ CLOSE", SecondarySlateColor, Color.white, new Vector2(0.68f, 0.92f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero, isPill: true);
            Text modalOrg = CreateText(modalCard, "OrgHeader", "SURAKSHAAR INDUSTRIAL SAFETY TRAINING", 22, PrimaryOrangeColor, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.84f), new Vector2(0.96f, 0.90f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text modalTitle = CreateText(modalCard, "CertTitle", "Fire & Explosion Response Training Certificate", 26, DarkNavyColor, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.74f), new Vector2(0.96f, 0.83f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text modalName = CreateText(modalCard, "WorkerName", "Worker Name", 30, DarkNavyColor, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.65f), new Vector2(0.96f, 0.73f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text modalId = CreateText(modalCard, "WorkerId", "Worker ID: ---", 22, MediumGrayColor, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.58f), new Vector2(0.96f, 0.64f), Vector2.zero, Vector2.zero);
            Text modalCId = CreateText(modalCard, "CertId", "Certificate ID: ---", 20, DarkNavyColor, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.51f), new Vector2(0.96f, 0.57f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text modalScore = CreateText(modalCard, "Score", "Assessment Score: 95%", 22, PrimaryOrangeColor, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.44f), new Vector2(0.96f, 0.50f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text modalDate = CreateText(modalCard, "Date", "Issued: --", 20, MediumGrayColor, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.38f), new Vector2(0.96f, 0.43f), Vector2.zero, Vector2.zero);
            Text modalBadge = CreateText(modalCard, "Badge", "✓ VERIFIED & VALID CERTIFICATE", 22, SuccessGreenColor, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.31f), new Vector2(0.96f, 0.37f), Vector2.zero, Vector2.zero, FontStyle.Bold);

            // QR Code RawImage
            GameObject qrGO = new GameObject("QRCodeRawImage");
            qrGO.transform.SetParent(modalCard.transform, false);
            RawImage modalQr = qrGO.AddComponent<RawImage>();
            RectTransform qrRt = qrGO.GetComponent<RectTransform>();
            qrRt.anchorMin = new Vector2(0.36f, 0.10f);
            qrRt.anchorMax = new Vector2(0.64f, 0.30f);
            qrRt.offsetMin = Vector2.zero;
            qrRt.offsetMax = Vector2.zero;

            Button modalVerifyBtn = CreateButton(modalCard, "VerifyQrBtn", "VERIFY QR CODE", PrimaryOrangeColor, Color.white, new Vector2(0.15f, 0.02f), new Vector2(0.85f, 0.08f), Vector2.zero, Vector2.zero);

            modalOverlay.SetActive(false);

            GameObject controllerGO = new GameObject("CertificateScreenController");
            CertificateScreenController ctrl = controllerGO.AddComponent<CertificateScreenController>();

            ctrl.SetupBindings(
                backBtn, headerTitle, headerSubtitle,
                cardM1, cardM2, cardM3, cardTotal,
                verifyBtn,
                modalOverlay, modalCloseBtn, modalOrg, modalTitle,
                modalName, modalId, modalCId, modalScore, modalDate, modalBadge,
                modalQr, modalVerifyBtn
            );

            EditorUtility.SetDirty(ctrl);
            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/MainMenu/CertificatePreview.unity");
        }

        private static void CreateQRVerificationScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject canvas = CreateCanvasWithEventSystem(scene, "QRVerifyCanvas");

            CreatePanel(canvas, "Background", BgColor, Vector2.zero, Vector2.one);
            CreateText(canvas, "Title", "QR Certificate Verification", 48, DarkNavyColor, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.96f), Vector2.zero, Vector2.zero, FontStyle.Bold);

            InputField inputField = CreateInputField(canvas, "CertIdInput", "Enter Certificate ID (SAR-2026-XXXXXX)", new Vector2(0.08f, 0.74f), new Vector2(0.92f, 0.82f), Vector2.zero, Vector2.zero);
            Button verifySearchBtn = CreateButton(canvas, "VerifySearchBtn", "VERIFY CERTIFICATE →", PrimaryOrangeColor, Color.white, new Vector2(0.08f, 0.63f), new Vector2(0.92f, 0.71f), Vector2.zero, Vector2.zero);

            // Result Overlay Card
            GameObject resCard = CreateCardPanel(canvas, "ResultCardPanel", new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.60f));
            Text statusText = CreateText(resCard, "ResultStatus", "✓ CERTIFICATE VERIFIED", 36, SuccessGreenColor, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.76f), new Vector2(0.95f, 0.94f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text detailsText = CreateText(resCard, "CertDetails", "Worker: ---\nID: ---\nModule: Fire Response\nScore: --%", 28, DarkNavyColor, TextAnchor.UpperLeft, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.72f), Vector2.zero, Vector2.zero);

            Button backBtn = CreateButton(canvas, "BackBtn", "BACK", SecondarySlateColor, Color.white, new Vector2(0.15f, 0.1f), new Vector2(0.85f, 0.18f), Vector2.zero, Vector2.zero);

            GameObject controllerGO = new GameObject("QRVerificationController");
            QRVerificationController ctrl = controllerGO.AddComponent<QRVerificationController>();

            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("certIdInputField").objectReferenceValue = inputField;
            so.FindProperty("verifySearchButton").objectReferenceValue = verifySearchBtn;
            so.FindProperty("backButton").objectReferenceValue = backBtn;
            so.FindProperty("resultCardPanel").objectReferenceValue = resCard;
            so.FindProperty("resultStatusText").objectReferenceValue = statusText;
            so.FindProperty("certDetailsText").objectReferenceValue = detailsText;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/MainMenu/QRVerification.unity");
        }

        private static void CreateProfileScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject canvas = CreateCanvasWithEventSystem(scene, "ProfileCanvas");

            CreatePanel(canvas, "Background", BgColor, Vector2.zero, Vector2.one);
            CreateText(canvas, "Title", "Worker Profile", 48, DarkNavyColor, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.96f), Vector2.zero, Vector2.zero, FontStyle.Bold);

            // Profile Card
            GameObject pCard = CreateCardPanel(canvas, "ProfileCard", new Vector2(0.06f, 0.52f), new Vector2(0.94f, 0.84f));
            Text nameText = CreateText(pCard, "NameText", "Worker", 40, DarkNavyColor, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.92f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text idText = CreateText(pCard, "IdText", "ID: ---", 28, MediumGrayColor, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.52f), new Vector2(0.94f, 0.70f), Vector2.zero, Vector2.zero);
            Text compText = CreateText(pCard, "CompText", "Completed Modules: 0 / 3", 30, DarkNavyColor, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.28f), new Vector2(0.94f, 0.48f), Vector2.zero, Vector2.zero);
            Text perfText = CreateText(pCard, "PerfText", "Overall Performance: 0%", 30, SuccessGreenColor, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.26f), Vector2.zero, Vector2.zero, FontStyle.Bold);

            // Language Options Card
            GameObject lCard = CreateCardPanel(canvas, "LangCard", new Vector2(0.06f, 0.24f), new Vector2(0.94f, 0.48f));
            CreateText(lCard, "LangHeader", "Language", 32, DarkNavyColor, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.65f), new Vector2(0.94f, 0.92f), Vector2.zero, Vector2.zero, FontStyle.Bold);

            Button enBtn = CreateButton(lCard, "EnBtn", "English", CardBgColor, DarkNavyColor, new Vector2(0.06f, 0.15f), new Vector2(0.32f, 0.55f), Vector2.zero, Vector2.zero);
            Button hiBtn = CreateButton(lCard, "HiBtn", "हिन्दी", CardBgColor, DarkNavyColor, new Vector2(0.36f, 0.15f), new Vector2(0.64f, 0.55f), Vector2.zero, Vector2.zero);
            Button satBtn = CreateButton(lCard, "SatBtn", "Santali", CardBgColor, DarkNavyColor, new Vector2(0.68f, 0.15f), new Vector2(0.94f, 0.55f), Vector2.zero, Vector2.zero);

            Button settingsBtn = CreateButton(canvas, "SettingsBtn", "SETTINGS ⚙", SecondarySlateColor, Color.white, new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.21f), Vector2.zero, Vector2.zero);

            AddBottomNavigation(canvas, ScreenState.Profile);

            GameObject controllerGO = new GameObject("ProfileController");
            ProfileScreenController ctrl = controllerGO.AddComponent<ProfileScreenController>();

            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("workerNameText").objectReferenceValue = nameText;
            so.FindProperty("workerIdText").objectReferenceValue = idText;
            so.FindProperty("completedModulesText").objectReferenceValue = compText;
            so.FindProperty("overallPerformanceText").objectReferenceValue = perfText;
            so.FindProperty("englishButton").objectReferenceValue = enBtn;
            so.FindProperty("hindiButton").objectReferenceValue = hiBtn;
            so.FindProperty("santaliButton").objectReferenceValue = satBtn;
            so.FindProperty("settingsButton").objectReferenceValue = settingsBtn;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/MainMenu/Profile.unity");
        }

        private static void CreateSettingsScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject canvas = CreateCanvasWithEventSystem(scene, "SettingsCanvas");

            CreatePanel(canvas, "Background", BgColor, Vector2.zero, Vector2.one);
            CreateText(canvas, "Title", "Settings", 52, DarkNavyColor, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.96f), Vector2.zero, Vector2.zero, FontStyle.Bold);

            CreateText(canvas, "LangHeader", "Language Options:", 34, DarkNavyColor, TextAnchor.MiddleLeft, new Vector2(0.1f, 0.78f), new Vector2(0.9f, 0.84f), Vector2.zero, Vector2.zero);

            Button enBtn = CreateButton(canvas, "EnBtn", "English", CardBgColor, DarkNavyColor, new Vector2(0.1f, 0.69f), new Vector2(0.34f, 0.77f), Vector2.zero, Vector2.zero);
            Button hiBtn = CreateButton(canvas, "HiBtn", "हिन्दी", CardBgColor, DarkNavyColor, new Vector2(0.38f, 0.69f), new Vector2(0.62f, 0.77f), Vector2.zero, Vector2.zero);
            Button satBtn = CreateButton(canvas, "SatBtn", "Santali", CardBgColor, DarkNavyColor, new Vector2(0.66f, 0.69f), new Vector2(0.9f, 0.77f), Vector2.zero, Vector2.zero);

            string about = "About SurakshaAR:\nAndroid AR Safety Simulator for Mining & Manufacturing sectors in Jharkhand.\nVersion 1.0.0 (Phase 3 UI Redesign)";
            CreateText(canvas, "AboutText", about, 28, MediumGrayColor, TextAnchor.UpperLeft, new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.62f), Vector2.zero, Vector2.zero);

            Button backBtn = CreateButton(canvas, "BackBtn", "BACK", SecondarySlateColor, Color.white, new Vector2(0.15f, 0.1f), new Vector2(0.85f, 0.2f), Vector2.zero, Vector2.zero);

            GameObject controllerGO = new GameObject("SettingsController");
            SettingsController ctrl = controllerGO.AddComponent<SettingsController>();

            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("englishLangButton").objectReferenceValue = enBtn;
            so.FindProperty("hindiLangButton").objectReferenceValue = hiBtn;
            so.FindProperty("santaliLangButton").objectReferenceValue = satBtn;
            so.FindProperty("backButton").objectReferenceValue = backBtn;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/MainMenu/Settings.unity");
        }

        private static void CreateGasModuleIntroScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject canvas = CreateCanvasWithEventSystem(scene, "GasIntroCanvas");

            CreatePanel(canvas, "Background", BgColor, Vector2.zero, Vector2.one);
            CreateText(canvas, "Title", "☣ Gas Leak & Confined Space", 48, WarningAmberColor, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.96f), Vector2.zero, Vector2.zero, FontStyle.Bold);

            string desc = "Learn how to recognize gas hazards, select SCBA PPE, perform multi-gas detector checks, verify confined space entry permits, and carry out two-person buddy procedures.";
            CreateText(canvas, "Desc", desc, 32, DarkNavyColor, TextAnchor.UpperLeft, new Vector2(0.08f, 0.75f), new Vector2(0.92f, 0.86f), Vector2.zero, Vector2.zero);

            GameObject card = CreateCardPanel(canvas, "LearnCard", new Vector2(0.08f, 0.3f), new Vector2(0.92f, 0.72f));
            CreateText(card, "Header", "8-Step Safety Procedure", 36, DarkNavyColor, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.84f), new Vector2(0.94f, 0.94f), Vector2.zero, Vector2.zero, FontStyle.Bold);

            string checklist = "1. Recognize Gas Hazard\n2. Raise Alarm / Alert Crew\n3. Identify Safe Upwind Exit\n4. Select Required SCBA PPE\n5. Check Multi-Gas Detector\n6. Verify Confined-Space Permit\n7. Stand-by Buddy Check\n8. Reach Assembly Point";
            CreateText(card, "Checklist", checklist, 28, MediumGrayColor, TextAnchor.UpperLeft, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.8f), Vector2.zero, Vector2.zero);

            Button learnBtn = CreateButton(canvas, "LearnBtn", "LEARN MODE →", PrimaryOrangeColor, Color.white, new Vector2(0.08f, 0.17f), new Vector2(0.92f, 0.27f), Vector2.zero, Vector2.zero);
            Button simBtn = CreateButton(canvas, "SimBtn", "SIMULATION →", SecondarySlateColor, Color.white, new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.15f), Vector2.zero, Vector2.zero);

            GameObject controllerGO = new GameObject("GasIntroController");
            GasModuleIntroController ctrl = controllerGO.AddComponent<GasModuleIntroController>();

            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("learnModeButton").objectReferenceValue = learnBtn;
            so.FindProperty("simulationButton").objectReferenceValue = simBtn;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/Gas/GasModuleIntro.unity");
        }

        private static void CreateGasARLearnPlaceholderScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera & Manager Setup
            GameObject cameraGO = new GameObject("Main Camera");
            Camera cameraComp = cameraGO.AddComponent<Camera>();
            cameraGO.tag = "MainCamera";
            cameraGO.transform.position = new Vector3(0, 1.6f, 0);
            cameraGO.transform.rotation = Quaternion.identity;

            GameObject managersGO = new GameObject("AR_Gas_Managers");
            GasLeakController gasController = managersGO.AddComponent<GasLeakController>();
            ARTrainingPlacementManager placementManager = managersGO.AddComponent<ARTrainingPlacementManager>();
            ARTrainingAnchorManager anchorManager = managersGO.AddComponent<ARTrainingAnchorManager>();
            MovementTrackingManager trackingManager = managersGO.AddComponent<MovementTrackingManager>();
            AudioHapticManager audioManager = managersGO.AddComponent<AudioHapticManager>();
            AdaptiveHelpController helpController = managersGO.AddComponent<AdaptiveHelpController>();
            GasTelemetryLogger telemetryLogger = managersGO.AddComponent<GasTelemetryLogger>();
            ARSessionController arSessionCtrl = managersGO.AddComponent<ARSessionController>();
            ARPlaneController arPlaneCtrl = managersGO.AddComponent<ARPlaneController>();
            MistakePatternAnalyzer mistakeAnalyzer = managersGO.AddComponent<MistakePatternAnalyzer>();
            GasLeakScenarioManager gasScenarioManager = managersGO.AddComponent<GasLeakScenarioManager>();
            AdaptiveTrainingManager adaptiveTrainingManager = managersGO.AddComponent<AdaptiveTrainingManager>();
            PPESelectionManager ppeManager = managersGO.AddComponent<PPESelectionManager>();
            ConfinedSpaceManager confinedManager = managersGO.AddComponent<ConfinedSpaceManager>();
            BuddyProcedureManager buddyManager = managersGO.AddComponent<BuddyProcedureManager>();

            // 2. 3D Spatial AR Objects
            GameObject scenarioGroup = new GameObject("3D_AR_Gas_Scenario");

            // Gas Hazard Cloud (Amber Sphere at (0, 0.5f, 3.0f))
            GameObject gasHazardGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gasHazardGO.name = "3D_GasHazardSource";
            gasHazardGO.transform.parent = scenarioGroup.transform;
            gasHazardGO.transform.position = new Vector3(0f, 0.5f, 3.0f);
            gasHazardGO.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            gasHazardGO.GetComponent<Renderer>().material.color = new Color(0.95f, 0.6f, 0.05f, 0.4f);
            TrainingObject gasHazardObject = gasHazardGO.AddComponent<TrainingObject>();
            GasHazard ghComp = gasHazardGO.AddComponent<GasHazard>();

            // Alarm Button (Amber Box at (-1.8f, 1.4f, 2.5f))
            GameObject alarmGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            alarmGO.name = "3D_GasAlarmButton";
            alarmGO.transform.parent = scenarioGroup.transform;
            alarmGO.transform.position = new Vector3(-1.8f, 1.4f, 2.5f);
            alarmGO.transform.localScale = new Vector3(0.3f, 0.4f, 0.2f);
            alarmGO.GetComponent<Renderer>().material.color = WarningAmberColor;
            TrainingObject alarmObject = alarmGO.AddComponent<TrainingObject>();

            // Safe Exit (Green Frame at (2.0f, 1.0f, 4.0f))
            GameObject safeExitGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            safeExitGO.name = "3D_GasSafeExit";
            safeExitGO.transform.parent = scenarioGroup.transform;
            safeExitGO.transform.position = new Vector3(2.0f, 1.0f, 4.0f);
            safeExitGO.transform.localScale = new Vector3(1.2f, 2.2f, 0.1f);
            safeExitGO.GetComponent<Renderer>().material.color = SuccessGreenColor;
            TrainingObject safeExitObject = safeExitGO.AddComponent<TrainingObject>();

            // Blocked Exit (Dark Navy Frame at (-2.0f, 1.0f, 4.0f))
            GameObject blockedExitGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blockedExitGO.name = "3D_GasBlockedExit";
            blockedExitGO.transform.parent = scenarioGroup.transform;
            blockedExitGO.transform.position = new Vector3(-2.0f, 1.0f, 4.0f);
            blockedExitGO.transform.localScale = new Vector3(1.2f, 2.2f, 0.1f);
            blockedExitGO.GetComponent<Renderer>().material.color = DarkNavyColor;
            TrainingObject blockedExitObject = blockedExitGO.AddComponent<TrainingObject>();

            // PPE Station (Blue Box at (-0.8f, 0.8f, 1.8f))
            GameObject ppeGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ppeGO.name = "3D_PPESelectionStation";
            ppeGO.transform.parent = scenarioGroup.transform;
            ppeGO.transform.position = new Vector3(-0.8f, 0.8f, 1.8f);
            ppeGO.transform.localScale = new Vector3(0.6f, 0.8f, 0.4f);
            ppeGO.GetComponent<Renderer>().material.color = Color.blue;
            TrainingObject ppeObject = ppeGO.AddComponent<TrainingObject>();

            // Gas Detector (Yellow Box at (0.8f, 0.6f, 1.8f))
            GameObject detectorGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            detectorGO.name = "3D_GasDetectorDevice";
            detectorGO.transform.parent = scenarioGroup.transform;
            detectorGO.transform.position = new Vector3(0.8f, 0.6f, 1.8f);
            detectorGO.transform.localScale = new Vector3(0.3f, 0.4f, 0.2f);
            detectorGO.GetComponent<Renderer>().material.color = Color.yellow;
            TrainingObject detectorObject = detectorGO.AddComponent<TrainingObject>();
            GasDetectorAR detectorComp = detectorGO.AddComponent<GasDetectorAR>();

            // Confined Space Marker (Red Arch at (0f, 1.0f, 4.5f))
            GameObject csGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            csGO.name = "3D_ConfinedSpaceMarker";
            csGO.transform.parent = scenarioGroup.transform;
            csGO.transform.position = new Vector3(0f, 1.0f, 4.5f);
            csGO.transform.localScale = new Vector3(1.5f, 2.0f, 0.2f);
            csGO.GetComponent<Renderer>().material.color = Color.red;
            TrainingObject csObject = csGO.AddComponent<TrainingObject>();

            // Buddy Check Point (Green Cylinder at (1.5f, 0.5f, 2.2f))
            GameObject buddyGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            buddyGO.name = "3D_BuddyCheckPoint";
            buddyGO.transform.parent = scenarioGroup.transform;
            buddyGO.transform.position = new Vector3(1.5f, 0.5f, 2.2f);
            buddyGO.transform.localScale = new Vector3(0.4f, 0.8f, 0.4f);
            buddyGO.GetComponent<Renderer>().material.color = SuccessGreenColor;
            TrainingObject buddyObject = buddyGO.AddComponent<TrainingObject>();

            // Assembly Zone (Green Cylinder at (3.5f, 0.1f, 6.0f))
            GameObject assemblyGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            assemblyGO.name = "3D_GasAssemblyZone";
            assemblyGO.transform.parent = scenarioGroup.transform;
            assemblyGO.transform.position = new Vector3(3.5f, 0.1f, 6.0f);
            assemblyGO.transform.localScale = new Vector3(1.5f, 0.1f, 1.5f);
            assemblyGO.GetComponent<Renderer>().material.color = SuccessGreenColor;
            TrainingObject assemblyObject = assemblyGO.AddComponent<TrainingObject>();

            // 3. Wire GasLeakController
            SerializedObject controllerSO = new SerializedObject(gasController);
            controllerSO.FindProperty("gasHazardObject").objectReferenceValue = gasHazardObject;
            controllerSO.FindProperty("alarmButtonObject").objectReferenceValue = alarmObject;
            controllerSO.FindProperty("safeExitObject").objectReferenceValue = safeExitObject;
            controllerSO.FindProperty("blockedExitObject").objectReferenceValue = blockedExitObject;
            controllerSO.FindProperty("ppeStationObject").objectReferenceValue = ppeObject;
            controllerSO.FindProperty("gasDetectorObject").objectReferenceValue = detectorObject;
            controllerSO.FindProperty("confinedSpaceObject").objectReferenceValue = csObject;
            controllerSO.FindProperty("buddyPointObject").objectReferenceValue = buddyObject;
            controllerSO.FindProperty("assemblyZoneObject").objectReferenceValue = assemblyObject;
            controllerSO.FindProperty("arCameraTransform").objectReferenceValue = cameraGO.transform;
            controllerSO.ApplyModifiedProperties();

            // 4. UI Canvas & HUD Overlay
            GameObject canvas = CreateCanvasWithEventSystem(scene, "GasARLearnCanvas");

            GameObject topHud = CreateCardPanel(canvas, "TopHUD", new Vector2(0.04f, 0.85f), new Vector2(0.96f, 0.98f));
            Text stepText = CreateText(topHud, "StepText", "Step 1 of 8", 28, DarkNavyColor, TextAnchor.MiddleLeft, new Vector2(0.05f, 0.6f), new Vector2(0.5f, 0.9f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Slider pBar = CreateProgressBar(topHud, "ProgressBar", 1f / 8f, new Vector2(0.52f, 0.65f), new Vector2(0.95f, 0.85f));

            Text statusText = CreateText(topHud, "StatusText", "SIMULATED GAS: LOW", 28, WarningAmberColor, TextAnchor.MiddleLeft, new Vector2(0.05f, 0.15f), new Vector2(0.45f, 0.55f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text instText = CreateText(topHud, "InstText", "Recognize gas hazard & tap leak source.", 26, DarkNavyColor, TextAnchor.MiddleLeft, new Vector2(0.48f, 0.15f), new Vector2(0.95f, 0.55f), Vector2.zero, Vector2.zero);

            GameObject feedbackCardGO = CreateCardPanel(canvas, "FeedbackCard", new Vector2(0.1f, 0.40f), new Vector2(0.9f, 0.68f));
            CreateText(feedbackCardGO, "Header", "⚠ Safety Notice", 34, WarningAmberColor, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.70f), new Vector2(0.95f, 0.92f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            CreateText(feedbackCardGO, "Explanation", "Follow instructions to complete step.", 26, DarkNavyColor, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.35f), new Vector2(0.95f, 0.68f), Vector2.zero, Vector2.zero);
            Button tryAgainBtn = CreateButton(feedbackCardGO, "TryAgainBtn", "TRY AGAIN", PrimaryOrangeColor, Color.white, new Vector2(0.1f, 0.06f), new Vector2(0.48f, 0.30f), Vector2.zero, Vector2.zero);
            Button cardHelpBtn = CreateButton(feedbackCardGO, "CardHelpBtn", "NEED HELP?", SecondarySlateColor, Color.white, new Vector2(0.52f, 0.06f), new Vector2(0.90f, 0.30f), Vector2.zero, Vector2.zero);

            FeedbackCard fbCardComp = feedbackCardGO.AddComponent<FeedbackCard>();
            SerializedObject fbSo = new SerializedObject(fbCardComp);
            fbSo.FindProperty("tryAgainButton").objectReferenceValue = tryAgainBtn;
            fbSo.FindProperty("needHelpButton").objectReferenceValue = cardHelpBtn;
            fbSo.ApplyModifiedProperties();
            feedbackCardGO.SetActive(false);

            GameObject botHud = CreatePanel(canvas, "BottomBar", new Color(0, 0, 0, 0.35f), new Vector2(0, 0), new Vector2(1, 0.15f));
            Button backBtn = CreateButton(botHud, "BackBtn", "← BACK", SecondarySlateColor, Color.white, new Vector2(0.04f, 0.25f), new Vector2(0.24f, 0.75f), Vector2.zero, Vector2.zero);
            Button scanBtn = CreateButton(botHud, "ScanBtn", "🔍 ACTION", PrimaryOrangeColor, Color.white, new Vector2(0.28f, 0.25f), new Vector2(0.72f, 0.75f), Vector2.zero, Vector2.zero);
            Button helpBtn = CreateButton(botHud, "HelpBtn", "NEED HELP?", WarningAmberColor, Color.white, new Vector2(0.76f, 0.25f), new Vector2(0.96f, 0.75f), Vector2.zero, Vector2.zero);

            GameObject controllerGO = new GameObject("GasLearnPlaceholderController");
            GasLearnPlaceholderController ctrl = controllerGO.AddComponent<GasLearnPlaceholderController>();

            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("stepCounterText").objectReferenceValue = stepText;
            so.FindProperty("progressBar").objectReferenceValue = pBar;
            so.FindProperty("gasStatusText").objectReferenceValue = statusText;
            so.FindProperty("instructionText").objectReferenceValue = instText;
            so.FindProperty("scanActionButton").objectReferenceValue = scanBtn;
            so.FindProperty("needHelpButton").objectReferenceValue = helpBtn;
            so.FindProperty("backButton").objectReferenceValue = backBtn;
            so.FindProperty("feedbackCard").objectReferenceValue = fbCardComp;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/Gas/GasARLearnPlaceholder.unity");
        }

        // =====================================================================
        // FIRE MODULE — PHASE 1: Spatial-Tracking Virtual Mine Simulator
        // =====================================================================

        /// <summary>
        /// Builds the FireMineSimulator.unity scene.
        /// Scene contains: ARSession (for VIO positional tracking), XROrigin + AR Camera (tracking source,
        /// NOT rendered to screen), MineOrigin + VirtualCamera (renders the virtual mine),
        /// MineEnvironmentBuilder (procedural corridor), MineMovementController (tracking bridge),
        /// MineSimulatorHUD (minimal overlay), and EventSystem.
        /// </summary>
        [MenuItem("SurakshaAR/Build Fire Mine Simulator Scene")]
        public static void CreateFireMineSimulatorScene()
        {
            BuildMineSimulatorScene("Assets/_Project/Scenes/Fire/FireMineSimulator.unity", FireScenarioType.ElectricalEquipment);
        }

        [MenuItem("SurakshaAR/Build Petroleum Fire Simulator Scene")]
        public static void CreatePetroleumFireSimulatorScene()
        {
            BuildMineSimulatorScene("Assets/_Project/Scenes/Fire/PetroleumFireSimulator.unity", FireScenarioType.LiquidFuel);
        }

        [MenuItem("SurakshaAR/Build Explosion Mine Simulator Scene")]
        public static void CreateExplosionMineSimulatorScene()
        {
            BuildExplosionMineSimulatorScene();
        }

        [MenuItem("SurakshaAR/Build Fire Assessment Simulator Scene")]
        public static void CreateFireAssessmentSimulatorScene()
        {
            BuildFireAssessmentSimulatorScene();
        }

        public static void BuildMineSimulatorScene(string scenePath, FireScenarioType scenarioType)
        {
            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
                defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 16);

            InitializeSprites();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── ARSession ─────────────────────────────────────────────────────
            GameObject arSessionGO = new GameObject("ARSession");
            arSessionGO.AddComponent<ARSession>();
            arSessionGO.AddComponent<ARInputManager>();

            // ── XROrigin (ARCore positional tracking source) ───────────────────
            GameObject arOriginGO = new GameObject("XR Origin");
            XROrigin xrOrigin = arOriginGO.AddComponent<XROrigin>();

            GameObject cameraOffsetGO = new GameObject("Camera Offset");
            cameraOffsetGO.transform.SetParent(arOriginGO.transform, false);

            GameObject arCamGO = new GameObject("AR Camera");
            arCamGO.transform.SetParent(cameraOffsetGO.transform, false);
            Camera arCam = arCamGO.AddComponent<Camera>();
            arCam.cullingMask = 1;            // Camera enabled and active for ARCore background render pass
            arCam.clearFlags = CameraClearFlags.SolidColor;
            arCam.backgroundColor = Color.black;
            arCam.nearClipPlane = 0.1f;
            arCam.farClipPlane = 20f;
            arCam.depth = 0;                  // Depth 0: renders background texture first, covered by virtualCam at depth 1
            arCamGO.tag = "MainCamera";       // Required by ARCore Vulkan backend in Built-in pipeline
            arCamGO.AddComponent<ARCameraManager>();
            arCamGO.AddComponent<ARCameraBackground>();

            var trackedPoseDriver = arCamGO.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
            trackedPoseDriver.trackingType = UnityEngine.InputSystem.XR.TrackedPoseDriver.TrackingType.RotationAndPosition;
            trackedPoseDriver.updateType = UnityEngine.InputSystem.XR.TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            trackedPoseDriver.ignoreTrackingState = true;

            var posAction = new UnityEngine.InputSystem.InputAction("Position", binding: "<XRHMD>/centerEyePosition", expectedControlType: "Vector3");
            posAction.AddBinding("<HandheldARInputDevice>/devicePosition");
            posAction.Enable();

            var rotAction = new UnityEngine.InputSystem.InputAction("Rotation", binding: "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion");
            rotAction.AddBinding("<HandheldARInputDevice>/deviceRotation");
            rotAction.Enable();

            trackedPoseDriver.positionInput = new UnityEngine.InputSystem.InputActionProperty(posAction);
            trackedPoseDriver.rotationInput = new UnityEngine.InputSystem.InputActionProperty(rotAction);

            xrOrigin.Camera = arCam;
            xrOrigin.Origin = arOriginGO;
            xrOrigin.CameraFloorOffsetObject = cameraOffsetGO;

            // AR Plane Manager on XR Origin: activates horizontal plane detection, forcing ARCore into full 6-DoF VIO SLAM
            ARPlaneManager planeManager = arOriginGO.AddComponent<ARPlaneManager>();
            planeManager.requestedDetectionMode = UnityEngine.XR.ARSubsystems.PlaneDetectionMode.Horizontal;
            arOriginGO.AddComponent<ARRaycastManager>();

            // ── Mine Root & Virtual Camera ─────────────────────────────────────
            // MineOrigin is the virtual world anchor. VirtualCamera is child, renders the mine.
            GameObject mineOriginGO = new GameObject("MineOrigin");
            mineOriginGO.transform.position = Vector3.zero;

            GameObject virtualCamGO = new GameObject("VirtualCamera");
            virtualCamGO.transform.SetParent(mineOriginGO.transform, false);
            virtualCamGO.transform.localPosition = new Vector3(0, 1.65f, -14.0f); // outdoor start — player walks into mine

            Camera virtualCam = virtualCamGO.AddComponent<Camera>();
            virtualCamGO.AddComponent<AudioListener>();
            virtualCam.clearFlags = CameraClearFlags.SolidColor;
            virtualCam.backgroundColor = new Color(0.40f, 0.62f, 0.85f, 1f); // sky blue — outdoors start
            virtualCam.fieldOfView = 75f;
            virtualCam.nearClipPlane = 0.05f;
            virtualCam.farClipPlane = 100f;
            virtualCam.depth = 1;           // main rendered camera (draws over arCam)
            virtualCam.cullingMask = -1;    // renders everything

            // ── Mine Environment Builder ───────────────────────────────────────
            var envBuilder = mineOriginGO.AddComponent<SurakshaAR.Training.Fire.MineEnvironmentBuilder>();
            SerializedObject envSO = new SerializedObject(envBuilder);
            envSO.FindProperty("mineRoot").objectReferenceValue = mineOriginGO.transform;
            envSO.ApplyModifiedProperties();

            // ── Mine Movement Controller ───────────────────────────────────────
            GameObject movCtrlGO = new GameObject("MineSimulatorController");
            var movCtrl = movCtrlGO.AddComponent<SurakshaAR.Training.Fire.MineMovementController>();
            SerializedObject movSO = new SerializedObject(movCtrl);
            movSO.FindProperty("virtualCamera").objectReferenceValue = virtualCam;
            movSO.FindProperty("mineOrigin").objectReferenceValue = mineOriginGO.transform;
            movSO.FindProperty("startVirtualPosition").vector3Value = new Vector3(0f, 1.65f, -14.0f);
            movSO.FindProperty("arCamera").objectReferenceValue = arCam;
            movSO.FindProperty("arSession").objectReferenceValue = arSessionGO.GetComponent<ARSession>();
            movSO.FindProperty("stepThreshold").floatValue = 0.14f;
            movSO.FindProperty("minStepInterval").floatValue = 0.28f;
            movSO.FindProperty("stationaryTimeout").floatValue = 0.55f;
            movSO.FindProperty("baseWalkSpeed").floatValue = 1.20f;
            movSO.FindProperty("physicalToVirtualScale").floatValue = 1.0f;
            movSO.FindProperty("smoothTime").floatValue = 0.06f;
            movSO.FindProperty("rotationSmoothing").floatValue = 0.05f;
            movSO.FindProperty("clampToCorridor").boolValue = true;
            movSO.FindProperty("corridorMinX").floatValue = -1.20f;
            movSO.FindProperty("corridorMaxX").floatValue =  1.20f;
            movSO.FindProperty("corridorMinY").floatValue =  1.10f;
            movSO.FindProperty("corridorMaxY").floatValue =  1.95f;
            movSO.FindProperty("corridorMinZ").floatValue = -15.00f;  // outdoor start is Z=-14m
            movSO.FindProperty("corridorMaxZ").floatValue = 110.00f;
            movSO.ApplyModifiedProperties();

            // ── HUD Canvas ─────────────────────────────────────────────────────
            GameObject canvasGO = new GameObject("HUD_Canvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();

            // Top bar (dark semi-transparent)
            GameObject topBar = CreatePanel(canvasGO, "TopBar",
                new Color(0f, 0f, 0f, 0.70f),
                new Vector2(0f, 0.92f), new Vector2(1f, 1f));

            // Title "SurakshaAR | Mine Training"
            string topTitle = (scenarioType == FireScenarioType.LiquidFuel)
                ? "SurakshaAR  |  Petroleum Fire"
                : "SurakshaAR  |  Mine Training";
            Text titleText = CreateText(topBar, "TitleText", topTitle,
                22, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0.02f, 0.1f), new Vector2(0.44f, 0.9f),
                Vector2.zero, Vector2.zero, FontStyle.Bold);

            // Phase 2B: HP Display
            Text hpText = CreateText(topBar, "HPText", "HP: 100",
                22, new Color(0.15f, 0.92f, 0.45f, 1f), TextAnchor.MiddleCenter,
                new Vector2(0.44f, 0.1f), new Vector2(0.62f, 0.9f),
                Vector2.zero, Vector2.zero, FontStyle.Bold);

            // Tracking dot indicator (reuse Image component)
            GameObject dotGO = new GameObject("TrackingDot");
            dotGO.transform.SetParent(topBar.transform, false);
            Image dotImg = dotGO.AddComponent<Image>();
            dotImg.color = new Color(0.97f, 0.72f, 0.04f, 1f);
            RectTransform dotRT = dotImg.rectTransform;
            dotRT.anchorMin = new Vector2(0.64f, 0.25f);
            dotRT.anchorMax = new Vector2(0.67f, 0.75f);
            dotRT.offsetMin = dotRT.offsetMax = Vector2.zero;

            // Tracking badge
            Text badgeText = CreateText(topBar, "TrackingBadge", "Initializing",
                20, new Color(0.97f, 0.72f, 0.04f, 1f), TextAnchor.MiddleRight,
                new Vector2(0.68f, 0.1f), new Vector2(0.98f, 0.9f),
                Vector2.zero, Vector2.zero, FontStyle.Bold);

            // ── Phase 2B: Objective Banner (directly below top bar) ────────────
            GameObject objectivePanel = CreatePanel(canvasGO, "ObjectivePanel",
                new Color(0.03f, 0.07f, 0.14f, 0.92f),
                new Vector2(0.0f, 0.865f), new Vector2(1.0f, 0.92f));

            Text objectiveText = CreateText(objectivePanel, "ObjectiveText",
                "<b>OBJECTIVE:</b> Enter the underground mine.",
                21, new Color(1.0f, 0.88f, 0.25f, 1f), TextAnchor.MiddleCenter,
                new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.95f),
                Vector2.zero, Vector2.zero, FontStyle.Normal);

            // ── Temporary Debug HUD Panel (Live 8-Field Display) ───────────────
            GameObject debugPanel = CreatePanel(canvasGO, "DebugHUDPanel",
                new Color(0.04f, 0.06f, 0.10f, 0.88f),
                new Vector2(0.03f, 0.46f), new Vector2(0.97f, 0.80f));

            Text debugText = CreateText(debugPanel, "DebugHUDText",
                "TRACKING STATE: Initializing\nRAW AR POS: X: 0.000 Y: 0.000 Z: 0.000\nRAW AR ROT: P: 0.0° Y: 0.0° R: 0.0°\nPREV AR POS: X: 0.000 Y: 0.000 Z: 0.000\nPOS DELTA: dX: 0.000 dY: 0.000 dZ: 0.000\nACCUM DISP: Dist: 0.000m (0.00, 0.00, 0.00)\nVIRTUAL POS: X: 0.000 Y: 1.600 Z: 0.500\nVIRTUAL ROT: P: 0.0° Y: 0.0° R: 0.0°",
                21, Color.white, TextAnchor.UpperLeft,
                new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.97f),
                Vector2.zero, Vector2.zero, FontStyle.Normal);
            debugPanel.SetActive(false);

            // ── Instruction Fade Panel ─────────────────────────────────────────
            GameObject instrPanel = CreatePanel(canvasGO, "InstructionPanel",
                new Color(0f, 0f, 0f, 0.55f),
                new Vector2(0.1f, 0.42f), new Vector2(0.9f, 0.55f));

            Text instrText = CreateText(instrPanel, "InstructionText",
                "Move naturally to explore the mine.",
                34, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f),
                Vector2.zero, Vector2.zero, FontStyle.Normal);

            CanvasGroup instrGroup = instrPanel.AddComponent<CanvasGroup>();
            instrGroup.alpha = 1f;

            // ── Tracking Lost Overlay ──────────────────────────────────────────
            GameObject lostOverlay = CreatePanel(canvasGO, "TrackingLostOverlay",
                new Color(0.80f, 0.40f, 0.00f, 0.82f),
                new Vector2(0.0f, 0.06f), new Vector2(1.0f, 0.14f));

            Text lostText = CreateText(lostOverlay, "TrackingLostText",
                "Tracking lost — pause and reorient the phone.",
                28, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.03f, 0.08f), new Vector2(0.97f, 0.92f),
                Vector2.zero, Vector2.zero, FontStyle.Bold);

            lostOverlay.SetActive(false);

            // ── MineSimulatorHUD component ─────────────────────────────────────
            GameObject hudCtrlGO = new GameObject("HUD_Controller");
            var hud = hudCtrlGO.AddComponent<SurakshaAR.Training.Fire.MineSimulatorHUD>();
            SerializedObject hudSO = new SerializedObject(hud);
            hudSO.FindProperty("titleText").objectReferenceValue = titleText;
            hudSO.FindProperty("trackingBadgeText").objectReferenceValue = badgeText;
            hudSO.FindProperty("trackingBadgeDot").objectReferenceValue = dotImg;
            hudSO.FindProperty("instructionText").objectReferenceValue = instrText;
            hudSO.FindProperty("instructionGroup").objectReferenceValue = instrGroup;
            hudSO.FindProperty("trackingLostOverlay").objectReferenceValue = lostOverlay;
            hudSO.FindProperty("trackingLostText").objectReferenceValue = lostText;
            hudSO.FindProperty("movementController").objectReferenceValue = movCtrl;
            hudSO.FindProperty("debugHUDPanel").objectReferenceValue = debugPanel;
            hudSO.FindProperty("debugHUDText").objectReferenceValue = debugText;
            hudSO.ApplyModifiedProperties();

            // ── PHASE 2A: Fire Incident HUD Panels ────────────────────────────

            // Fire Alert Banner (appears when fire triggers)
            GameObject fireAlertBanner = CreatePanel(canvasGO, "FireAlertBanner",
                new Color(0.85f, 0.08f, 0.04f, 0.92f),
                new Vector2(0.0f, 0.805f), new Vector2(1f, 0.865f));
            Text fireAlertText = CreateText(fireAlertBanner, "FireAlertText",
                "🔥 FIRE DETECTED — TAKE ACTION IMMEDIATELY",
                28, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.95f),
                Vector2.zero, Vector2.zero, FontStyle.Bold);
            fireAlertBanner.SetActive(false);

            // Alarm Interaction Prompt (clean floating text — NO dark rectangular card)
            GameObject alarmPromptPanel = CreatePanel(canvasGO, "AlarmPromptPanel",
                Color.clear,
                new Vector2(0.1f, 0.08f), new Vector2(0.9f, 0.22f));
            var promptImg = alarmPromptPanel.GetComponent<Image>();
            if (promptImg != null) promptImg.enabled = false;

            Text alarmPromptText = CreateText(alarmPromptPanel, "AlarmPromptText",
                "🔔  Emergency Alarm\nTAP TO ACTIVATE",
                32, new Color(1f, 0.9f, 0.2f), TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f),
                Vector2.zero, Vector2.zero, FontStyle.Bold);
            var promptOutline = alarmPromptText.gameObject.AddComponent<Outline>();
            promptOutline.effectColor = new Color(0f, 0f, 0f, 0.90f);
            promptOutline.effectDistance = new Vector2(2f, -2f);

            CanvasGroup alarmPromptGroup = alarmPromptPanel.AddComponent<CanvasGroup>();
            alarmPromptGroup.alpha = 0f;
            alarmPromptGroup.interactable = false;

            // Alarm Status Panel (bottom center — clean text without dark card background)
            GameObject alarmStatusPanel = CreatePanel(canvasGO, "AlarmStatusPanel",
                Color.clear,
                new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.11f));
            var statusImg = alarmStatusPanel.GetComponent<Image>();
            if (statusImg != null) statusImg.enabled = false;

            Text alarmStatusText = CreateText(alarmStatusPanel, "AlarmStatusText",
                "",
                24, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.95f),
                Vector2.zero, Vector2.zero, FontStyle.Bold);
            var statusOutline = alarmStatusText.gameObject.AddComponent<Outline>();
            statusOutline.effectColor = new Color(0f, 0f, 0f, 0.90f);
            statusOutline.effectDistance = new Vector2(2f, -2f);

            CanvasGroup alarmStatusGroup = alarmStatusPanel.AddComponent<CanvasGroup>();
            alarmStatusGroup.alpha = 0f;

            // ── PHASE 2A: Fire Hazard Controller ─────────────────────────────
            GameObject fireAnchorGO = new GameObject("FireAnchor");
            fireAnchorGO.transform.SetParent(mineOriginGO.transform, false);
            if (scenarioType == FireScenarioType.LiquidFuel)
            {
                BuildPetroleumHazardProps(mineOriginGO, new Vector3(0.85f, 0f, 10f));
                fireAnchorGO.transform.localPosition = new Vector3(0.85f, 0.25f, 10f); // Over fuel spill/drums
            }
            else
            {
                fireAnchorGO.transform.localPosition = new Vector3(1.05f, 0.40f, 10f); // On right wall
            }

            GameObject fireCtrlGO = new GameObject("FireHazardController");
            var fireCtrl = fireCtrlGO.AddComponent<SurakshaAR.Training.Fire.FireHazardController>();
            SerializedObject fireSO = new SerializedObject(fireCtrl);
            fireSO.FindProperty("movementController").objectReferenceValue = movCtrl;
            fireSO.FindProperty("triggerDistance").floatValue = 24f;
            fireSO.FindProperty("fireAnchor").objectReferenceValue = fireAnchorGO.transform;
            var fireScenProp = fireSO.FindProperty("scenarioType");
            if (fireScenProp != null) fireScenProp.enumValueIndex = (int)scenarioType;
            fireSO.ApplyModifiedProperties();
            fireCtrl.ScenarioType = scenarioType;

            // ── PHASE 2A: Emergency Alarm Controller ──────────────────────────
            // Alarm placed near extinguisher in response zone (Z = 13)
            GameObject alarmCtrlGO = new GameObject("EmergencyAlarmController");
            var alarmCtrl = alarmCtrlGO.AddComponent<SurakshaAR.Training.Fire.EmergencyAlarmController>();
            SerializedObject alarmSO = new SerializedObject(alarmCtrl);
            alarmSO.FindProperty("movementController").objectReferenceValue = movCtrl;
            alarmSO.FindProperty("virtualCamera").objectReferenceValue = virtualCam;
            var offsetProp = alarmSO.FindProperty("alarmOffsetFromFire");
            if (offsetProp != null) offsetProp.floatValue = 2.0f;
            var startPosProp = alarmSO.FindProperty("startingStationPos");
            if (startPosProp != null) startPosProp.vector3Value = new Vector3(-1.08f, 1.35f, 12.0f);
            alarmSO.FindProperty("proximityDistance").floatValue = 2.0f;
            alarmSO.FindProperty("alarmPromptText").objectReferenceValue = alarmPromptText;
            alarmSO.FindProperty("alarmPromptGroup").objectReferenceValue = alarmPromptGroup;
            alarmSO.FindProperty("alarmStatusText").objectReferenceValue = alarmStatusText;
            alarmSO.FindProperty("alarmStatusGroup").objectReferenceValue = alarmStatusGroup;
            alarmSO.ApplyModifiedProperties();

            // ── PHASE 2A: HUD Phase2A Wiring ──────────────────────────────────
            SerializedObject hudSO2 = new SerializedObject(hud);
            hudSO2.FindProperty("fireHazardController").objectReferenceValue = fireCtrl;
            hudSO2.FindProperty("emergencyAlarmController").objectReferenceValue = alarmCtrl;
            hudSO2.FindProperty("fireAlertBanner").objectReferenceValue = fireAlertBanner;
            hudSO2.FindProperty("fireAlertText").objectReferenceValue = fireAlertText;
            hudSO2.ApplyModifiedProperties();

            // ── PHASE 2A: Orchestrator (bridges fire → alarm placement) ──────────
            GameObject orchestratorGO = new GameObject("Phase2AOrchestrator");
            var orchestrator = orchestratorGO.AddComponent<SurakshaAR.Training.Fire.Phase2AOrchestrator>();
            SerializedObject orchSO = new SerializedObject(orchestrator);
            orchSO.FindProperty("fireHazardController").objectReferenceValue = fireCtrl;
            orchSO.FindProperty("emergencyAlarmController").objectReferenceValue = alarmCtrl;
            orchSO.ApplyModifiedProperties();

            // ── PHASE 2B: Response Resource Prompts & Feedback Toast ───────────

            // Resource Interaction Prompt (clean floating text — NO dark card)
            GameObject resPromptPanel = CreatePanel(canvasGO, "ResourcePromptPanel",
                Color.clear,
                new Vector2(0.10f, 0.15f), new Vector2(0.90f, 0.28f));
            var resImg = resPromptPanel.GetComponent<Image>();
            if (resImg != null) resImg.enabled = false;

            Text resPromptTitle = CreateText(resPromptPanel, "ResourcePromptTitle",
                "Fire Extinguisher",
                28, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.48f), new Vector2(0.96f, 0.95f),
                Vector2.zero, Vector2.zero, FontStyle.Bold);
            var titleOutline = resPromptTitle.gameObject.AddComponent<Outline>();
            titleOutline.effectColor = new Color(0f, 0f, 0f, 0.90f);
            titleOutline.effectDistance = new Vector2(2f, -2f);

            Text resPromptAction = CreateText(resPromptPanel, "ResourcePromptAction",
                "TAP TO PICK UP",
                24, new Color(0.2f, 0.92f, 0.5f), TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.48f),
                Vector2.zero, Vector2.zero, FontStyle.Bold);
            var actionOutline = resPromptAction.gameObject.AddComponent<Outline>();
            actionOutline.effectColor = new Color(0f, 0f, 0f, 0.90f);
            actionOutline.effectDistance = new Vector2(2f, -2f);

            CanvasGroup resPromptGroup = resPromptPanel.AddComponent<CanvasGroup>();
            resPromptGroup.alpha = 0f;
            resPromptGroup.interactable = false;

            // Resource Status Toast (clean floating text — NO dark card)
            GameObject resToastPanel = CreatePanel(canvasGO, "ResourceToastPanel",
                Color.clear,
                new Vector2(0.08f, 0.29f), new Vector2(0.92f, 0.42f));
            var toastImg = resToastPanel.GetComponent<Image>();
            if (toastImg != null) toastImg.enabled = false;

            Text resToastText = CreateText(resToastPanel, "ResourceToastText",
                "",
                28, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f),
                Vector2.zero, Vector2.zero, FontStyle.Bold);
            var toastOutline = resToastText.gameObject.AddComponent<Outline>();
            toastOutline.effectColor = new Color(0f, 0f, 0f, 0.90f);
            toastOutline.effectDistance = new Vector2(2f, -2f);

            CanvasGroup resToastGroup = resToastPanel.AddComponent<CanvasGroup>();
            resToastGroup.alpha = 0f;

            // ── PHASE 2B: Response Resource Manager ───────────────────────────
            GameObject resMgrGO = new GameObject("FireResponseResourceManager");
            var resMgr = resMgrGO.AddComponent<SurakshaAR.Training.Fire.FireResponseResourceManager>();
            SerializedObject resSO = new SerializedObject(resMgr);
            resSO.FindProperty("movementController").objectReferenceValue = movCtrl;
            resSO.FindProperty("virtualCamera").objectReferenceValue = virtualCam;
            resSO.FindProperty("mineOrigin").objectReferenceValue = mineOriginGO.transform;
            resSO.FindProperty("proximityDistance").floatValue = 2.0f;
            resSO.FindProperty("promptTitleText").objectReferenceValue = resPromptTitle;
            resSO.FindProperty("promptActionText").objectReferenceValue = resPromptAction;
            resSO.FindProperty("promptCanvasGroup").objectReferenceValue = resPromptGroup;
            resSO.FindProperty("toastText").objectReferenceValue = resToastText;
            resSO.FindProperty("toastCanvasGroup").objectReferenceValue = resToastGroup;
            resSO.ApplyModifiedProperties();

            // Wire Phase 2B references into HUD
            SerializedObject hudSO3 = new SerializedObject(hud);
            hudSO3.FindProperty("resourceManager").objectReferenceValue = resMgr;
            hudSO3.FindProperty("hpText").objectReferenceValue = hpText;
            hudSO3.FindProperty("objectiveText").objectReferenceValue = objectiveText;
            hudSO3.FindProperty("objectivePanel").objectReferenceValue = objectivePanel;
            hudSO3.ApplyModifiedProperties();

            // ── PHASE 2B-2: Fire Response Evaluator UI panels ─────────────────

            // "Use on Fire" prompt — shown when worker returns to fire with resource
            GameObject useOnFirePanel = CreatePanel(canvasGO, "UseOnFirePromptPanel",
                new Color(0.75f, 0.12f, 0.04f, 0.92f),
                new Vector2(0.10f, 0.15f), new Vector2(0.90f, 0.28f));

            Text usePromptTitle = CreateText(useOnFirePanel, "UseOnFireTitle",
                "🔥 Fire Extinguisher Ready",
                28, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.48f), new Vector2(0.96f, 0.95f),
                Vector2.zero, Vector2.zero, FontStyle.Bold);

            Text usePromptAction = CreateText(useOnFirePanel, "UseOnFireAction",
                "TAP TO USE ON FIRE",
                26, new Color(1f, 0.85f, 0.20f), TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.48f),
                Vector2.zero, Vector2.zero, FontStyle.Bold);

            CanvasGroup usePromptGroup = useOnFirePanel.AddComponent<CanvasGroup>();
            usePromptGroup.alpha = 0f;
            usePromptGroup.interactable = false;

            // Outcome toast — shows extinguishing / flare-up / success messages (clean floating text — NO dark card)
            GameObject outcomePanel = CreatePanel(canvasGO, "OutcomeToastPanel",
                Color.clear,
                new Vector2(0.06f, 0.40f), new Vector2(0.94f, 0.56f));
            var outcomeImg = outcomePanel.GetComponent<Image>();
            if (outcomeImg != null) outcomeImg.enabled = false;

            Text outcomeText = CreateText(outcomePanel, "OutcomeText",
                "",
                30, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f),
                Vector2.zero, Vector2.zero, FontStyle.Bold);
            var outcomeOutline = outcomeText.gameObject.AddComponent<Outline>();
            outcomeOutline.effectColor = new Color(0f, 0f, 0f, 0.90f);
            outcomeOutline.effectDistance = new Vector2(2f, -2f);

            CanvasGroup outcomeGroup = outcomePanel.AddComponent<CanvasGroup>();
            outcomeGroup.alpha = 0f;

            // ── PHASE 2B-2: FireResponseEvaluator component ───────────────────
            GameObject evalGO = new GameObject("FireResponseEvaluator");
            var evaluator = evalGO.AddComponent<SurakshaAR.Training.Fire.FireResponseEvaluator>();
            SerializedObject evalSO = new SerializedObject(evaluator);
            evalSO.FindProperty("fireHazardController").objectReferenceValue = fireCtrl;
            evalSO.FindProperty("resourceManager").objectReferenceValue = resMgr;
            evalSO.FindProperty("virtualCamera").objectReferenceValue = virtualCam;
            evalSO.FindProperty("usePromptTitleText").objectReferenceValue = usePromptTitle;
            evalSO.FindProperty("usePromptActionText").objectReferenceValue = usePromptAction;
            evalSO.FindProperty("usePromptCanvasGroup").objectReferenceValue = usePromptGroup;
            evalSO.FindProperty("outcomeToastText").objectReferenceValue = outcomeText;
            evalSO.FindProperty("outcomeToastCanvasGroup").objectReferenceValue = outcomeGroup;
            evalSO.FindProperty("hpText").objectReferenceValue = hpText;
            evalSO.ApplyModifiedProperties();
            evaluator.ScenarioType = scenarioType;

            // Wire evaluator into HUD (Phase 2B-2)
            SerializedObject hudSO4 = new SerializedObject(hud);
            hudSO4.FindProperty("responseEvaluator").objectReferenceValue = evaluator;
            hudSO4.ApplyModifiedProperties();

            // Wire toast references for Phase 2C assembly and evacuation alerts
            hud.SetToastReferences(outcomeText, outcomeGroup);

            // ── PHASE 2C: AssemblyZoneController ──────────────────────────────
            GameObject assemblyZoneGO = new GameObject("AssemblyZoneController");
            var assemblyZone = assemblyZoneGO.AddComponent<SurakshaAR.Training.Fire.AssemblyZoneController>();
            SerializedObject azSO = new SerializedObject(assemblyZone);
            azSO.FindProperty("virtualCamera").objectReferenceValue = virtualCam;
            azSO.FindProperty("hud").objectReferenceValue = hud;
            azSO.FindProperty("assemblyCenter").vector3Value = new Vector3(-8.5f, 1.6f, 94.0f);
            azSO.FindProperty("zoneRadius").floatValue = 6.0f;
            azSO.FindProperty("dwellTimeRequired").floatValue = 2.0f;
            azSO.ApplyModifiedProperties();

            // ── SUB-MODULE COMPLETION SCREEN OVERLAY ──────────────────────────
            CreateCompletionScreenOverlay(canvasGO, assemblyZone);

            // ── EventSystem ────────────────────────────────────────────────────
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // ── Save Scene ─────────────────────────────────────────────────────
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[SceneBuilder] Simulator scene saved ({scenarioType}): {scenePath}");
        }

        private static SubModuleCompletionScreen CreateCompletionScreenOverlay(GameObject canvasGO, AssemblyZoneController azCtrl)
        {
            // Dark translucent overlay panel covering the screen
            GameObject overlay = CreatePanel(canvasGO, "SubModuleCompletionOverlay",
                new Color(0.04f, 0.07f, 0.12f, 0.94f),
                Vector2.zero, Vector2.one);

            // Center dialog card
            GameObject card = CreatePanel(overlay, "CompletionCard",
                new Color(0.10f, 0.14f, 0.22f, 0.98f),
                new Vector2(0.08f, 0.26f), new Vector2(0.92f, 0.74f), roundedCardSprite);

            var cardOutline = card.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0.18f, 0.55f, 0.95f, 0.50f);
            cardOutline.effectDistance = new Vector2(2f, -2f);

            // Success badge/icon
            Text iconText = CreateText(card, "SuccessIcon", "✅", 64, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.94f), Vector2.zero, Vector2.zero);

            // Title: Successfully Completed
            Text titleText = CreateText(card, "CompletionTitle", "Successfully Completed", 36, new Color(0.15f, 0.95f, 0.45f), TextAnchor.MiddleCenter,
                new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.72f), Vector2.zero, Vector2.zero, FontStyle.Bold);

            // Description text
            Text descText = CreateText(card, "CompletionDesc", "You have successfully completed\nthis training simulation.", 26, new Color(0.88f, 0.92f, 0.98f), TextAnchor.MiddleCenter,
                new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.52f), Vector2.zero, Vector2.zero);

            // NEXT & CONFIRM button
            Button confirmBtn = CreateButton(card, "NextConfirmButton", "NEXT & CONFIRM", PrimaryOrangeColor, Color.white,
                new Vector2(0.10f, 0.06f), new Vector2(0.90f, 0.22f), Vector2.zero, Vector2.zero, true);
            Text btnText = confirmBtn.GetComponentInChildren<Text>();

            // Controller script on canvasGO (stays active so events are received)
            var compScreen = canvasGO.AddComponent<SubModuleCompletionScreen>();
            compScreen.SetReferences(overlay, titleText, descText, confirmBtn, btnText, azCtrl);

            // Overlay starts hidden
            overlay.SetActive(false);

            return compScreen;
        }

        private static void BuildPetroleumHazardProps(GameObject parent, Vector3 basePos)
        {
            var root = new GameObject("PetroleumHazardProps");
            root.transform.SetParent(parent.transform, false);
            root.transform.localPosition = basePos;

            var stdShader = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse");

            // 1. Spilled Fuel Oil Slick / Puddle on Floor (dark, shiny, semi-gloss)
            var slick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            slick.name = "FuelSpillSlick";
            slick.transform.SetParent(root.transform, false);
            slick.transform.localPosition = new Vector3(0.05f, 0.015f, 0f);
            slick.transform.localScale = new Vector3(1.8f, 0.01f, 1.6f);
            Object.DestroyImmediate(slick.GetComponent<Collider>());
            var slickMat = new Material(stdShader);
            slickMat.color = new Color(0.05f, 0.04f, 0.03f, 0.95f);
            slickMat.SetFloat("_Glossiness", 0.90f);
            slickMat.SetFloat("_Metallic", 0.35f);
            slick.GetComponent<Renderer>().material = slickMat;

            // 2. Large Industrial Petroleum Drum 1 (Standing upright, Safety Red with Ribs)
            var drum1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            drum1.name = "OilDrum_Upright_Red";
            drum1.transform.SetParent(root.transform, false);
            drum1.transform.localPosition = new Vector3(0.35f, 0.45f, 0.25f);
            drum1.transform.localScale = new Vector3(0.55f, 0.45f, 0.55f);
            Object.DestroyImmediate(drum1.GetComponent<Collider>());
            var drumRedMat = new Material(stdShader);
            drumRedMat.color = new Color(0.78f, 0.12f, 0.08f);
            drumRedMat.SetFloat("_Glossiness", 0.60f);
            drumRedMat.SetFloat("_Metallic", 0.45f);
            drum1.GetComponent<Renderer>().material = drumRedMat;

            // Drum 1 Rib Ring
            var rib1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rib1.name = "DrumRib_Upper";
            rib1.transform.SetParent(drum1.transform, false);
            rib1.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            rib1.transform.localScale = new Vector3(1.04f, 0.05f, 1.04f);
            Object.DestroyImmediate(rib1.GetComponent<Collider>());
            var blackBandMat = new Material(stdShader);
            blackBandMat.color = new Color(0.12f, 0.12f, 0.12f);
            rib1.GetComponent<Renderer>().material = blackBandMat;

            // 3. Second Industrial Fuel Drum (Slightly rusted yellow/diesel drum)
            var drum2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            drum2.name = "OilDrum_Yellow";
            drum2.transform.SetParent(root.transform, false);
            drum2.transform.localPosition = new Vector3(-0.35f, 0.42f, 0.10f);
            drum2.transform.localScale = new Vector3(0.52f, 0.42f, 0.52f);
            Object.DestroyImmediate(drum2.GetComponent<Collider>());
            var drumYellowMat = new Material(stdShader);
            drumYellowMat.color = new Color(0.85f, 0.65f, 0.08f);
            drumYellowMat.SetFloat("_Glossiness", 0.45f);
            drum2.GetComponent<Renderer>().material = drumYellowMat;

            // 4. Third Drum: Tipped Over / Leaking Fuel Canister
            var drumTipped = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            drumTipped.name = "OilDrum_TippedLeaking";
            drumTipped.transform.SetParent(root.transform, false);
            drumTipped.transform.localPosition = new Vector3(0.0f, 0.25f, -0.25f);
            drumTipped.transform.localRotation = Quaternion.Euler(0f, 25f, 90f);
            drumTipped.transform.localScale = new Vector3(0.48f, 0.42f, 0.48f);
            Object.DestroyImmediate(drumTipped.GetComponent<Collider>());
            drumTipped.GetComponent<Renderer>().material = drumRedMat;

            // 5. Metal Jerry Cans (Heavy duty fuel transport cans)
            for (int i = 0; i < 2; i++)
            {
                var jerryCan = GameObject.CreatePrimitive(PrimitiveType.Cube);
                jerryCan.name = $"JerryCan_{i + 1}";
                jerryCan.transform.SetParent(root.transform, false);
                float xOff = (i == 0) ? -0.15f : 0.60f;
                float zOff = (i == 0) ? -0.45f : -0.20f;
                jerryCan.transform.localPosition = new Vector3(xOff, 0.20f, zOff);
                jerryCan.transform.localRotation = Quaternion.Euler(0f, (i == 0) ? 15f : -30f, 0f);
                jerryCan.transform.localScale = new Vector3(0.20f, 0.40f, 0.32f);
                Object.DestroyImmediate(jerryCan.GetComponent<Collider>());
                var jerryMat = new Material(stdShader);
                jerryMat.color = (i == 0) ? new Color(0.68f, 0.15f, 0.08f) : new Color(0.18f, 0.32f, 0.16f); // red or olive
                jerryCan.GetComponent<Renderer>().material = jerryMat;

                // Spout / handle on top
                var handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                handle.name = "Handle";
                handle.transform.SetParent(jerryCan.transform, false);
                handle.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                handle.transform.localScale = new Vector3(0.15f, 0.18f, 0.70f);
                Object.DestroyImmediate(handle.GetComponent<Collider>());
                handle.GetComponent<Renderer>().material = blackBandMat;
            }

            // 6. Wall-Mounted Hazard Diamond Warning Sign (Class 3 Flammable Liquid)
            var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "FlammableLiquidSign";
            sign.transform.SetParent(parent.transform, false);
            sign.transform.localPosition = new Vector3(1.22f, 1.45f, 10f); // on right wall above drums
            sign.transform.localRotation = Quaternion.Euler(0f, -90f, 45f); // diamond orientation
            sign.transform.localScale = new Vector3(0.02f, 0.32f, 0.32f);
            Object.DestroyImmediate(sign.GetComponent<Collider>());
            var signMat = new Material(stdShader);
            signMat.color = new Color(0.92f, 0.15f, 0.08f); // Red hazard diamond
            sign.GetComponent<Renderer>().material = signMat;
        }

        public static void BuildExplosionMineSimulatorScene()
        {
            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
                defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 16);

            InitializeSprites();

            string scenePath = "Assets/_Project/Scenes/Fire/ExplosionMineSimulator.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── ARSession ─────────────────────────────────────────────────────
            GameObject arSessionGO = new GameObject("ARSession");
            arSessionGO.AddComponent<ARSession>();
            arSessionGO.AddComponent<ARInputManager>();

            // ── XROrigin ──────────────────────────────────────────────────────
            GameObject arOriginGO = new GameObject("XR Origin");
            XROrigin xrOrigin = arOriginGO.AddComponent<XROrigin>();

            GameObject cameraOffsetGO = new GameObject("Camera Offset");
            cameraOffsetGO.transform.SetParent(arOriginGO.transform, false);

            GameObject arCamGO = new GameObject("AR Camera");
            arCamGO.transform.SetParent(cameraOffsetGO.transform, false);
            Camera arCam = arCamGO.AddComponent<Camera>();
            arCam.cullingMask = 1;            // Camera enabled and active for ARCore background render pass
            arCam.clearFlags = CameraClearFlags.SolidColor;
            arCam.backgroundColor = Color.black;
            arCam.nearClipPlane = 0.1f;
            arCam.farClipPlane = 20f;
            arCam.depth = 0;                  // Depth 0: renders background texture first, covered by virtualCam at depth 1
            arCamGO.tag = "MainCamera";       // Required by ARCore Vulkan backend in Built-in pipeline
            arCamGO.AddComponent<ARCameraManager>();
            arCamGO.AddComponent<ARCameraBackground>();

            var trackedPoseDriver = arCamGO.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
            trackedPoseDriver.trackingType = UnityEngine.InputSystem.XR.TrackedPoseDriver.TrackingType.RotationAndPosition;
            trackedPoseDriver.updateType = UnityEngine.InputSystem.XR.TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            trackedPoseDriver.ignoreTrackingState = true;

            var posAction = new UnityEngine.InputSystem.InputAction("Position", binding: "<XRHMD>/centerEyePosition", expectedControlType: "Vector3");
            posAction.AddBinding("<HandheldARInputDevice>/devicePosition");
            posAction.Enable();

            var rotAction = new UnityEngine.InputSystem.InputAction("Rotation", binding: "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion");
            rotAction.AddBinding("<HandheldARInputDevice>/deviceRotation");
            rotAction.Enable();

            trackedPoseDriver.positionInput = new UnityEngine.InputSystem.InputActionProperty(posAction);
            trackedPoseDriver.rotationInput = new UnityEngine.InputSystem.InputActionProperty(rotAction);

            xrOrigin.Camera = arCam;
            xrOrigin.Origin = arOriginGO;
            xrOrigin.CameraFloorOffsetObject = cameraOffsetGO;

            // AR Plane Manager on XR Origin: activates horizontal plane detection, forcing ARCore into full 6-DoF VIO SLAM
            ARPlaneManager planeManager = arOriginGO.AddComponent<ARPlaneManager>();
            planeManager.requestedDetectionMode = UnityEngine.XR.ARSubsystems.PlaneDetectionMode.Horizontal;
            arOriginGO.AddComponent<ARRaycastManager>();

            // ── Virtual Mine World ────────────────────────────────────────────
            GameObject mineOriginGO = new GameObject("MineOrigin");
            mineOriginGO.transform.position = Vector3.zero;

            GameObject virtualCamGO = new GameObject("VirtualCamera");
            virtualCamGO.transform.SetParent(mineOriginGO.transform, false);
            virtualCamGO.transform.localPosition = new Vector3(0, 1.65f, -14.0f);

            Camera virtualCam = virtualCamGO.AddComponent<Camera>();
            virtualCamGO.AddComponent<AudioListener>();
            virtualCam.clearFlags = CameraClearFlags.SolidColor;
            virtualCam.backgroundColor = new Color(0.40f, 0.62f, 0.85f, 1f);
            virtualCam.fieldOfView = 75f;
            virtualCam.nearClipPlane = 0.05f;
            virtualCam.farClipPlane = 100f;
            virtualCam.depth = 1;
            virtualCam.cullingMask = -1;

            // ── Mine Environment Builder ───────────────────────────────────────
            var envBuilder = mineOriginGO.AddComponent<SurakshaAR.Training.Fire.MineEnvironmentBuilder>();
            SerializedObject envSO = new SerializedObject(envBuilder);
            envSO.FindProperty("mineRoot").objectReferenceValue = mineOriginGO.transform;
            envSO.ApplyModifiedProperties();

            // ── Mine Movement Controller ───────────────────────────────────────
            GameObject movCtrlGO = new GameObject("MineSimulatorController");
            var movCtrl = movCtrlGO.AddComponent<SurakshaAR.Training.Fire.MineMovementController>();
            SerializedObject movSO = new SerializedObject(movCtrl);
            movSO.FindProperty("virtualCamera").objectReferenceValue = virtualCam;
            movSO.FindProperty("mineOrigin").objectReferenceValue = mineOriginGO.transform;
            movSO.FindProperty("startVirtualPosition").vector3Value = new Vector3(0f, 1.65f, -14.0f);
            movSO.FindProperty("arCamera").objectReferenceValue = arCam;
            movSO.FindProperty("arSession").objectReferenceValue = arSessionGO.GetComponent<ARSession>();
            movSO.FindProperty("stepThreshold").floatValue = 0.14f;
            movSO.FindProperty("minStepInterval").floatValue = 0.28f;
            movSO.FindProperty("stationaryTimeout").floatValue = 0.55f;
            movSO.FindProperty("baseWalkSpeed").floatValue = 1.20f;
            movSO.FindProperty("physicalToVirtualScale").floatValue = 1.0f;
            movSO.FindProperty("smoothTime").floatValue = 0.06f;
            movSO.FindProperty("rotationSmoothing").floatValue = 0.05f;
            movSO.FindProperty("clampToCorridor").boolValue = true;
            movSO.FindProperty("corridorMinX").floatValue = -1.20f;
            movSO.FindProperty("corridorMaxX").floatValue =  1.20f;
            movSO.FindProperty("corridorMinY").floatValue =  1.10f;
            movSO.FindProperty("corridorMaxY").floatValue =  1.95f;
            movSO.FindProperty("corridorMinZ").floatValue = -15.00f;
            movSO.FindProperty("corridorMaxZ").floatValue = 110.00f;
            movSO.ApplyModifiedProperties();

            // ── HUD Canvas ─────────────────────────────────────────────────────
            GameObject canvasGO = new GameObject("HUD_Canvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();

            // Top bar
            GameObject topBar = CreatePanel(canvasGO, "TopBar",
                new Color(0f, 0f, 0f, 0.70f),
                new Vector2(0f, 0.92f), new Vector2(1f, 1f));

            Text titleText = CreateText(topBar, "TitleText", "SurakshaAR  |  Explosion & Collision",
                22, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0.02f, 0.1f), new Vector2(0.55f, 0.9f),
                Vector2.zero, Vector2.zero, FontStyle.Bold);

            // Tracking badge
            Text badgeText = CreateText(topBar, "TrackingBadge", "Initializing",
                20, new Color(0.97f, 0.72f, 0.04f, 1f), TextAnchor.MiddleRight,
                new Vector2(0.68f, 0.1f), new Vector2(0.98f, 0.9f),
                Vector2.zero, Vector2.zero, FontStyle.Bold);

            // Objective panel
            GameObject objectivePanel = CreatePanel(canvasGO, "ObjectivePanel",
                new Color(0.03f, 0.07f, 0.14f, 0.92f),
                new Vector2(0.0f, 0.865f), new Vector2(1.0f, 0.92f));

            Text objectiveText = CreateText(objectivePanel, "ObjectiveText",
                "<b>OBJECTIVE:</b> Inspect the mine corridor and equipment.",
                21, new Color(1.0f, 0.88f, 0.25f, 1f), TextAnchor.MiddleCenter,
                new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.95f),
                Vector2.zero, Vector2.zero, FontStyle.Normal);

            // Outcome / Alert Toast panel
            GameObject outcomePanel = CreatePanel(canvasGO, "OutcomeToastPanel",
                Color.clear,
                new Vector2(0.06f, 0.40f), new Vector2(0.94f, 0.56f));
            var outcomeImg = outcomePanel.GetComponent<Image>();
            if (outcomeImg != null) outcomeImg.enabled = false;

            Text outcomeText = CreateText(outcomePanel, "OutcomeText",
                "",
                30, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f),
                Vector2.zero, Vector2.zero, FontStyle.Bold);
            var outcomeOutline = outcomeText.gameObject.AddComponent<Outline>();
            outcomeOutline.effectColor = new Color(0f, 0f, 0f, 0.90f);
            outcomeOutline.effectDistance = new Vector2(2f, -2f);

            CanvasGroup outcomeGroup = outcomePanel.AddComponent<CanvasGroup>();
            outcomeGroup.alpha = 0f;

            // HUD controller
            GameObject hudGO = new GameObject("MineSimulatorHUD");
            var hud = hudGO.AddComponent<SurakshaAR.Training.Fire.MineSimulatorHUD>();
            SerializedObject hudSO = new SerializedObject(hud);
            hudSO.FindProperty("trackingBadgeText").objectReferenceValue = badgeText;
            hudSO.FindProperty("movementController").objectReferenceValue = movCtrl;
            hudSO.FindProperty("objectiveText").objectReferenceValue = objectiveText;
            hudSO.FindProperty("objectivePanel").objectReferenceValue = objectivePanel;
            hudSO.ApplyModifiedProperties();
            hud.SetToastReferences(outcomeText, outcomeGroup);

            // ── Emergency Alarm Controller ────────────────────────────────────
            GameObject alarmCtrlGO = new GameObject("EmergencyAlarmController");
            var alarmCtrl = alarmCtrlGO.AddComponent<SurakshaAR.Training.Fire.EmergencyAlarmController>();
            SerializedObject alarmSO = new SerializedObject(alarmCtrl);
            alarmSO.FindProperty("movementController").objectReferenceValue = movCtrl;
            alarmSO.FindProperty("virtualCamera").objectReferenceValue = virtualCam;
            alarmSO.FindProperty("startingStationPos").vector3Value = new Vector3(-1.08f, 1.35f, 12.0f);
            alarmSO.ApplyModifiedProperties();

            // ── Mine Collapse Controller ──────────────────────────────────────
            GameObject collapseCtrlGO = new GameObject("MineCollapseController");
            var collapseCtrl = collapseCtrlGO.AddComponent<SurakshaAR.Training.Fire.MineCollapseController>();
            SerializedObject collapseSO = new SerializedObject(collapseCtrl);
            collapseSO.FindProperty("movementController").objectReferenceValue = movCtrl;
            collapseSO.FindProperty("virtualCamera").objectReferenceValue = virtualCam;
            collapseSO.FindProperty("hud").objectReferenceValue = hud;
            collapseSO.ApplyModifiedProperties();

            // Failure UI Overlay
            CreateFailureOverlay(canvasGO, collapseCtrl);

            // ── Explosion Hazard Controller ───────────────────────────────────
            GameObject explosionCtrlGO = new GameObject("ExplosionHazardController");
            var explosionCtrl = explosionCtrlGO.AddComponent<SurakshaAR.Training.Fire.ExplosionHazardController>();
            SerializedObject expSO = new SerializedObject(explosionCtrl);
            expSO.FindProperty("movementController").objectReferenceValue = movCtrl;
            expSO.FindProperty("virtualCamera").objectReferenceValue = virtualCam;
            expSO.FindProperty("emergencyAlarmController").objectReferenceValue = alarmCtrl;
            expSO.FindProperty("hud").objectReferenceValue = hud;
            expSO.FindProperty("collapseController").objectReferenceValue = collapseCtrl;
            expSO.FindProperty("electricBoxPosition").vector3Value = new Vector3(1.05f, 1.25f, 10.0f);
            expSO.FindProperty("triggerRadius").floatValue = 2.6f;
            expSO.ApplyModifiedProperties();

            // ── Assembly Zone Controller ──────────────────────────────────────
            GameObject assemblyZoneGO = new GameObject("AssemblyZoneController");
            var assemblyZone = assemblyZoneGO.AddComponent<SurakshaAR.Training.Fire.AssemblyZoneController>();
            SerializedObject azSO = new SerializedObject(assemblyZone);
            azSO.FindProperty("virtualCamera").objectReferenceValue = virtualCam;
            azSO.FindProperty("hud").objectReferenceValue = hud;
            azSO.FindProperty("assemblyCenter").vector3Value = new Vector3(-8.5f, 1.6f, 94.0f);
            azSO.FindProperty("zoneRadius").floatValue = 6.0f;
            azSO.FindProperty("dwellTimeRequired").floatValue = 2.0f;
            azSO.ApplyModifiedProperties();

            // ── Sub-Module Completion Confirmation Screen ─────────────────────
            CreateCompletionScreenOverlay(canvasGO, assemblyZone);

            // ── EventSystem ───────────────────────────────────────────────────
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // ── Save Scene ────────────────────────────────────────────────────
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[SceneBuilder] ExplosionMineSimulator scene saved: {scenePath}");
        }

        private static void CreateFailureOverlay(GameObject canvasGO, MineCollapseController collapseCtrl)
        {
            // Dark crimson translucent overlay
            GameObject overlay = CreatePanel(canvasGO, "FailureOverlayPanel",
                new Color(0.12f, 0.02f, 0.02f, 0.95f),
                Vector2.zero, Vector2.one);

            // Failure card
            GameObject card = CreatePanel(overlay, "FailureCard",
                new Color(0.18f, 0.06f, 0.06f, 0.98f),
                new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.72f), roundedCardSprite);

            var outline = card.AddComponent<Outline>();
            outline.effectColor = new Color(0.85f, 0.15f, 0.10f, 0.60f);
            outline.effectDistance = new Vector2(2f, -2f);

            Text iconText = CreateText(card, "FailIcon", "⚠️", 60, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.94f), Vector2.zero, Vector2.zero);

            Text titleText = CreateText(card, "FailTitle", "Evacuation Failed", 34, new Color(1.0f, 0.30f, 0.25f), TextAnchor.MiddleCenter,
                new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.72f), Vector2.zero, Vector2.zero, FontStyle.Bold);

            Text descText = CreateText(card, "FailDesc", "The mine tunnel collapsed behind you.\nYou must move quickly toward the emergency exit when the alarm sounds.", 24, new Color(0.95f, 0.88f, 0.88f), TextAnchor.MiddleCenter,
                new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.52f), Vector2.zero, Vector2.zero);

            Button retryBtn = CreateButton(card, "RetryButton", "RETRY EVACUATION", PrimaryOrangeColor, Color.white,
                new Vector2(0.10f, 0.06f), new Vector2(0.90f, 0.22f), Vector2.zero, Vector2.zero, true);

            collapseCtrl.SetFailureUI(overlay, titleText, descText, retryBtn);
            overlay.SetActive(false);
        }

        public static void BuildFireAssessmentSimulatorScene()
        {
            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
                defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 16);

            InitializeSprites();

            string scenePath = "Assets/_Project/Scenes/Fire/FireAssessmentSimulator.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── ARSession ─────────────────────────────────────────────────────
            GameObject arSessionGO = new GameObject("ARSession");
            arSessionGO.AddComponent<ARSession>();
            arSessionGO.AddComponent<ARInputManager>();

            // ── XROrigin ──────────────────────────────────────────────────────
            GameObject arOriginGO = new GameObject("XR Origin");
            XROrigin xrOrigin = arOriginGO.AddComponent<XROrigin>();

            GameObject cameraOffsetGO = new GameObject("Camera Offset");
            cameraOffsetGO.transform.SetParent(arOriginGO.transform, false);

            GameObject arCamGO = new GameObject("AR Camera");
            arCamGO.transform.SetParent(cameraOffsetGO.transform, false);
            Camera arCam = arCamGO.AddComponent<Camera>();
            arCam.cullingMask = 1;
            arCam.clearFlags = CameraClearFlags.SolidColor;
            arCam.backgroundColor = Color.black;
            arCam.nearClipPlane = 0.1f;
            arCam.farClipPlane = 20f;
            arCam.depth = 0;
            arCamGO.tag = "MainCamera";
            arCamGO.AddComponent<ARCameraManager>();
            arCamGO.AddComponent<ARCameraBackground>();

            var trackedPoseDriver = arCamGO.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
            trackedPoseDriver.trackingType = UnityEngine.InputSystem.XR.TrackedPoseDriver.TrackingType.RotationAndPosition;
            trackedPoseDriver.updateType = UnityEngine.InputSystem.XR.TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            trackedPoseDriver.ignoreTrackingState = true;

            var posAction = new UnityEngine.InputSystem.InputAction("Position", binding: "<XRHMD>/centerEyePosition", expectedControlType: "Vector3");
            posAction.AddBinding("<HandheldARInputDevice>/devicePosition");
            posAction.Enable();

            var rotAction = new UnityEngine.InputSystem.InputAction("Rotation", binding: "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion");
            rotAction.AddBinding("<HandheldARInputDevice>/deviceRotation");
            rotAction.Enable();

            trackedPoseDriver.positionInput = new UnityEngine.InputSystem.InputActionProperty(posAction);
            trackedPoseDriver.rotationInput = new UnityEngine.InputSystem.InputActionProperty(rotAction);

            xrOrigin.Camera = arCam;
            xrOrigin.Origin = arOriginGO;
            xrOrigin.CameraFloorOffsetObject = cameraOffsetGO;

            ARPlaneManager planeManager = arOriginGO.AddComponent<ARPlaneManager>();
            planeManager.requestedDetectionMode = UnityEngine.XR.ARSubsystems.PlaneDetectionMode.Horizontal;
            arOriginGO.AddComponent<ARRaycastManager>();

            // ── Virtual Mine World ────────────────────────────────────────────
            GameObject mineOriginGO = new GameObject("MineOrigin");
            mineOriginGO.transform.position = Vector3.zero;

            GameObject virtualCamGO = new GameObject("VirtualCamera");
            virtualCamGO.transform.SetParent(mineOriginGO.transform, false);
            virtualCamGO.transform.localPosition = new Vector3(0, 1.65f, -14.0f);

            Camera virtualCam = virtualCamGO.AddComponent<Camera>();
            virtualCamGO.AddComponent<AudioListener>();
            virtualCam.clearFlags = CameraClearFlags.SolidColor;
            virtualCam.backgroundColor = new Color(0.40f, 0.62f, 0.85f, 1f);
            virtualCam.fieldOfView = 75f;
            virtualCam.nearClipPlane = 0.05f;
            virtualCam.farClipPlane = 100f;
            virtualCam.depth = 1;
            virtualCam.cullingMask = -1;

            // ── Mine Environment Builder (Route B is Emergency Exit) ──────────
            var envBuilder = mineOriginGO.AddComponent<SurakshaAR.Training.Fire.MineEnvironmentBuilder>();
            SerializedObject envSO = new SerializedObject(envBuilder);
            envSO.FindProperty("mineRoot").objectReferenceValue = mineOriginGO.transform;
            envSO.FindProperty("isRouteBEmergencyExit").boolValue = true;
            envSO.ApplyModifiedProperties();
            envBuilder.IsRouteBEmergencyExit = true;

            // ── Mine Movement Controller ───────────────────────────────────────
            GameObject movCtrlGO = new GameObject("MineSimulatorController");
            var movCtrl = movCtrlGO.AddComponent<SurakshaAR.Training.Fire.MineMovementController>();
            SerializedObject movSO = new SerializedObject(movCtrl);
            movSO.FindProperty("virtualCamera").objectReferenceValue = virtualCam;
            movSO.FindProperty("mineOrigin").objectReferenceValue = mineOriginGO.transform;
            movSO.FindProperty("startVirtualPosition").vector3Value = new Vector3(0f, 1.65f, -14.0f);
            movSO.FindProperty("arCamera").objectReferenceValue = arCam;
            movSO.FindProperty("arSession").objectReferenceValue = arSessionGO.GetComponent<ARSession>();
            movSO.FindProperty("stepThreshold").floatValue = 0.14f;
            movSO.FindProperty("minStepInterval").floatValue = 0.28f;
            movSO.FindProperty("stationaryTimeout").floatValue = 0.55f;
            movSO.FindProperty("baseWalkSpeed").floatValue = 1.20f;
            movSO.FindProperty("physicalToVirtualScale").floatValue = 1.0f;
            movSO.FindProperty("smoothTime").floatValue = 0.06f;
            movSO.FindProperty("rotationSmoothing").floatValue = 0.05f;
            movSO.FindProperty("clampToCorridor").boolValue = true;
            movSO.FindProperty("corridorMinX").floatValue = -1.20f;
            movSO.FindProperty("corridorMaxX").floatValue =  1.20f;
            movSO.FindProperty("corridorMinY").floatValue =  1.10f;
            movSO.FindProperty("corridorMaxY").floatValue =  1.95f;
            movSO.FindProperty("corridorMinZ").floatValue = -15.00f;
            movSO.FindProperty("corridorMaxZ").floatValue = 110.00f;
            movSO.FindProperty("routeBEmergencyExit").boolValue = true;
            movSO.ApplyModifiedProperties();
            movCtrl.RouteBEmergencyExit = true;

            // ── HUD Canvas ─────────────────────────────────────────────────────
            GameObject canvasGO = new GameObject("HUD_Canvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();

            // Top bar
            GameObject topBar = CreatePanel(canvasGO, "TopBar",
                new Color(0f, 0f, 0f, 0.70f),
                new Vector2(0f, 0.92f), new Vector2(1f, 1f));

            Text titleText = CreateText(topBar, "TitleText", "SurakshaAR  |  Final Assessment",
                22, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0.02f, 0.1f), new Vector2(0.55f, 0.9f),
                Vector2.zero, Vector2.zero, FontStyle.Bold);

            Text badgeText = CreateText(topBar, "TrackingBadge", "Initializing",
                20, new Color(0.97f, 0.72f, 0.04f, 1f), TextAnchor.MiddleRight,
                new Vector2(0.68f, 0.1f), new Vector2(0.98f, 0.9f),
                Vector2.zero, Vector2.zero, FontStyle.Bold);

            Text hpText = CreateText(topBar, "WorkerHPText", "HP 100%",
                18, new Color(0.2f, 0.92f, 0.45f, 1f), TextAnchor.MiddleCenter,
                new Vector2(0.48f, 0.1f), new Vector2(0.68f, 0.9f),
                Vector2.zero, Vector2.zero, FontStyle.Bold);

            // Objective panel
            GameObject objectivePanel = CreatePanel(canvasGO, "ObjectivePanel",
                new Color(0.03f, 0.07f, 0.14f, 0.92f),
                new Vector2(0.0f, 0.865f), new Vector2(1.0f, 0.92f));

            Text objectiveText = CreateText(objectivePanel, "ObjectiveText",
                "<b>OBJECTIVE:</b> Extinguish electrical equipment fire.",
                21, new Color(1.0f, 0.88f, 0.25f, 1f), TextAnchor.MiddleCenter,
                new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.95f),
                Vector2.zero, Vector2.zero, FontStyle.Normal);

            // Outcome / Alert Toast panel
            GameObject outcomePanel = CreatePanel(canvasGO, "OutcomeToastPanel",
                Color.clear,
                new Vector2(0.06f, 0.40f), new Vector2(0.94f, 0.56f));
            var outcomeImg = outcomePanel.GetComponent<Image>();
            if (outcomeImg != null) outcomeImg.enabled = false;

            Text outcomeText = CreateText(outcomePanel, "OutcomeText",
                "",
                30, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f),
                Vector2.zero, Vector2.zero, FontStyle.Bold);
            var outcomeOutline = outcomeText.gameObject.AddComponent<Outline>();
            outcomeOutline.effectColor = new Color(0f, 0f, 0f, 0.90f);
            outcomeOutline.effectDistance = new Vector2(2f, -2f);

            CanvasGroup outcomeGroup = outcomePanel.AddComponent<CanvasGroup>();
            outcomeGroup.alpha = 0f;

            // Resource Interaction Prompt Panel
            GameObject resPromptPanel = CreatePanel(canvasGO, "ResourcePromptPanel",
                Color.clear,
                new Vector2(0.10f, 0.15f), new Vector2(0.90f, 0.28f));
            var resImg = resPromptPanel.GetComponent<Image>();
            if (resImg != null) resImg.enabled = false;

            Text resPromptTitle = CreateText(resPromptPanel, "ResourcePromptTitle",
                "Fire Extinguisher",
                28, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.48f), new Vector2(0.96f, 0.95f),
                Vector2.zero, Vector2.zero, FontStyle.Bold);
            var titleOutline = resPromptTitle.gameObject.AddComponent<Outline>();
            titleOutline.effectColor = new Color(0f, 0f, 0f, 0.90f);
            titleOutline.effectDistance = new Vector2(2f, -2f);

            Text resPromptAction = CreateText(resPromptPanel, "ResourcePromptAction",
                "TAP TO PICK UP",
                24, new Color(0.2f, 0.92f, 0.5f), TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.48f),
                Vector2.zero, Vector2.zero, FontStyle.Bold);
            var actionOutline = resPromptAction.gameObject.AddComponent<Outline>();
            actionOutline.effectColor = new Color(0f, 0f, 0f, 0.90f);
            actionOutline.effectDistance = new Vector2(2f, -2f);

            CanvasGroup resPromptGroup = resPromptPanel.AddComponent<CanvasGroup>();
            resPromptGroup.alpha = 0f;
            resPromptGroup.interactable = false;

            // Resource Status Toast
            GameObject resToastPanel = CreatePanel(canvasGO, "ResourceToastPanel",
                Color.clear,
                new Vector2(0.08f, 0.29f), new Vector2(0.92f, 0.42f));
            var toastImg = resToastPanel.GetComponent<Image>();
            if (toastImg != null) toastImg.enabled = false;

            Text resToastText = CreateText(resToastPanel, "ResourceToastText",
                "",
                28, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f),
                Vector2.zero, Vector2.zero, FontStyle.Bold);
            var toastOutline = resToastText.gameObject.AddComponent<Outline>();
            toastOutline.effectColor = new Color(0f, 0f, 0f, 0.90f);
            toastOutline.effectDistance = new Vector2(2f, -2f);

            CanvasGroup resToastGroup = resToastPanel.AddComponent<CanvasGroup>();
            resToastGroup.alpha = 0f;

            // "Use on Fire" prompt
            GameObject useOnFirePanel = CreatePanel(canvasGO, "UseOnFirePromptPanel",
                new Color(0.75f, 0.12f, 0.04f, 0.92f),
                new Vector2(0.10f, 0.15f), new Vector2(0.90f, 0.28f));

            Text usePromptTitle = CreateText(useOnFirePanel, "UseOnFireTitle",
                "🔥 Fire Extinguisher Ready",
                28, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.48f), new Vector2(0.96f, 0.95f),
                Vector2.zero, Vector2.zero, FontStyle.Bold);

            Text usePromptAction = CreateText(useOnFirePanel, "UseOnFireAction",
                "TAP TO USE ON FIRE",
                26, new Color(1f, 0.85f, 0.20f), TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.48f),
                Vector2.zero, Vector2.zero, FontStyle.Bold);

            CanvasGroup usePromptGroup = useOnFirePanel.AddComponent<CanvasGroup>();
            usePromptGroup.alpha = 0f;
            usePromptGroup.interactable = false;

            // HUD controller
            GameObject hudGO = new GameObject("MineSimulatorHUD");
            var hud = hudGO.AddComponent<SurakshaAR.Training.Fire.MineSimulatorHUD>();
            SerializedObject hudSO = new SerializedObject(hud);
            hudSO.FindProperty("trackingBadgeText").objectReferenceValue = badgeText;
            hudSO.FindProperty("movementController").objectReferenceValue = movCtrl;
            hudSO.FindProperty("objectiveText").objectReferenceValue = objectiveText;
            hudSO.FindProperty("objectivePanel").objectReferenceValue = objectivePanel;
            hudSO.FindProperty("hpText").objectReferenceValue = hpText;
            hudSO.ApplyModifiedProperties();
            hud.SetToastReferences(outcomeText, outcomeGroup);

            // ── Electrical Fire Hazard ─────────────────────────────────────────
            GameObject fireAnchorGO = new GameObject("FireAnchor");
            fireAnchorGO.transform.SetParent(mineOriginGO.transform, false);
            fireAnchorGO.transform.localPosition = new Vector3(1.05f, 0.40f, 10f);

            GameObject fireCtrlGO = new GameObject("FireHazardController");
            var fireCtrl = fireCtrlGO.AddComponent<SurakshaAR.Training.Fire.FireHazardController>();
            SerializedObject fireSO = new SerializedObject(fireCtrl);
            fireSO.FindProperty("movementController").objectReferenceValue = movCtrl;
            fireSO.FindProperty("triggerDistance").floatValue = 24f;
            fireSO.FindProperty("fireAnchor").objectReferenceValue = fireAnchorGO.transform;
            var fireScenProp = fireSO.FindProperty("scenarioType");
            if (fireScenProp != null) fireScenProp.enumValueIndex = (int)FireScenarioType.ElectricalEquipment;
            fireSO.ApplyModifiedProperties();
            fireCtrl.ScenarioType = FireScenarioType.ElectricalEquipment;

            // ── Resource Manager ──────────────────────────────────────────────
            GameObject resMgrGO = new GameObject("FireResponseResourceManager");
            var resMgr = resMgrGO.AddComponent<SurakshaAR.Training.Fire.FireResponseResourceManager>();
            SerializedObject resSO = new SerializedObject(resMgr);
            resSO.FindProperty("movementController").objectReferenceValue = movCtrl;
            resSO.FindProperty("virtualCamera").objectReferenceValue = virtualCam;
            resSO.FindProperty("mineOrigin").objectReferenceValue = mineOriginGO.transform;
            resSO.FindProperty("proximityDistance").floatValue = 2.0f;
            resSO.FindProperty("promptTitleText").objectReferenceValue = resPromptTitle;
            resSO.FindProperty("promptActionText").objectReferenceValue = resPromptAction;
            resSO.FindProperty("promptCanvasGroup").objectReferenceValue = resPromptGroup;
            resSO.FindProperty("toastText").objectReferenceValue = resToastText;
            resSO.FindProperty("toastCanvasGroup").objectReferenceValue = resToastGroup;
            resSO.ApplyModifiedProperties();

            // ── Fire Response Evaluator ───────────────────────────────────────
            GameObject evalGO = new GameObject("FireResponseEvaluator");
            var evaluator = evalGO.AddComponent<SurakshaAR.Training.Fire.FireResponseEvaluator>();
            SerializedObject evalSO = new SerializedObject(evaluator);
            evalSO.FindProperty("fireHazardController").objectReferenceValue = fireCtrl;
            evalSO.FindProperty("resourceManager").objectReferenceValue = resMgr;
            evalSO.FindProperty("virtualCamera").objectReferenceValue = virtualCam;
            evalSO.FindProperty("usePromptTitleText").objectReferenceValue = usePromptTitle;
            evalSO.FindProperty("usePromptActionText").objectReferenceValue = usePromptAction;
            evalSO.FindProperty("usePromptCanvasGroup").objectReferenceValue = usePromptGroup;
            evalSO.FindProperty("outcomeToastText").objectReferenceValue = outcomeText;
            evalSO.FindProperty("outcomeToastCanvasGroup").objectReferenceValue = outcomeGroup;
            evalSO.FindProperty("hpText").objectReferenceValue = hpText;
            evalSO.ApplyModifiedProperties();
            evaluator.ScenarioType = FireScenarioType.ElectricalEquipment;

            // Wire HUD with fire references
            SerializedObject hudSO2 = new SerializedObject(hud);
            hudSO2.FindProperty("fireHazardController").objectReferenceValue = fireCtrl;
            hudSO2.FindProperty("resourceManager").objectReferenceValue = resMgr;
            hudSO2.FindProperty("responseEvaluator").objectReferenceValue = evaluator;
            hudSO2.ApplyModifiedProperties();

            // ── Emergency Alarm Controller ────────────────────────────────────
            GameObject alarmCtrlGO = new GameObject("EmergencyAlarmController");
            var alarmCtrl = alarmCtrlGO.AddComponent<SurakshaAR.Training.Fire.EmergencyAlarmController>();
            SerializedObject alarmSO = new SerializedObject(alarmCtrl);
            alarmSO.FindProperty("movementController").objectReferenceValue = movCtrl;
            alarmSO.FindProperty("virtualCamera").objectReferenceValue = virtualCam;
            alarmSO.FindProperty("startingStationPos").vector3Value = new Vector3(-1.08f, 1.35f, 12.0f);
            alarmSO.ApplyModifiedProperties();

            // ── Mine Collapse Controller (Route B emergency exit) ─────────────
            GameObject collapseCtrlGO = new GameObject("MineCollapseController");
            var collapseCtrl = collapseCtrlGO.AddComponent<SurakshaAR.Training.Fire.MineCollapseController>();
            SerializedObject collapseSO = new SerializedObject(collapseCtrl);
            collapseSO.FindProperty("movementController").objectReferenceValue = movCtrl;
            collapseSO.FindProperty("virtualCamera").objectReferenceValue = virtualCam;
            collapseSO.FindProperty("hud").objectReferenceValue = hud;
            collapseSO.FindProperty("isRouteBEmergencyExit").boolValue = true;
            collapseSO.ApplyModifiedProperties();
            collapseCtrl.IsRouteBEmergencyExit = true;

            // ── Explosion Hazard Controller (Strict Post-Fire Trigger) ────────
            GameObject explosionCtrlGO = new GameObject("ExplosionHazardController");
            var explosionCtrl = explosionCtrlGO.AddComponent<SurakshaAR.Training.Fire.ExplosionHazardController>();
            SerializedObject expSO = new SerializedObject(explosionCtrl);
            expSO.FindProperty("movementController").objectReferenceValue = movCtrl;
            expSO.FindProperty("virtualCamera").objectReferenceValue = virtualCam;
            expSO.FindProperty("emergencyAlarmController").objectReferenceValue = alarmCtrl;
            expSO.FindProperty("hud").objectReferenceValue = hud;
            expSO.FindProperty("collapseController").objectReferenceValue = collapseCtrl;
            expSO.FindProperty("electricBoxPosition").vector3Value = new Vector3(1.05f, 1.25f, 10.0f);
            expSO.FindProperty("triggerRadius").floatValue = 2.6f;
            expSO.FindProperty("autoTriggerOnProximity").boolValue = false;
            expSO.ApplyModifiedProperties();
            explosionCtrl.AutoTriggerOnProximity = false;

            // ── Assembly Zone Controller (Route B Center at X = +8.5m) ────────
            GameObject assemblyZoneGO = new GameObject("AssemblyZoneController");
            var assemblyZone = assemblyZoneGO.AddComponent<SurakshaAR.Training.Fire.AssemblyZoneController>();
            SerializedObject azSO = new SerializedObject(assemblyZone);
            azSO.FindProperty("virtualCamera").objectReferenceValue = virtualCam;
            azSO.FindProperty("hud").objectReferenceValue = hud;
            azSO.FindProperty("assemblyCenter").vector3Value = new Vector3(8.5f, 1.6f, 94.0f);
            azSO.FindProperty("zoneRadius").floatValue = 6.0f;
            azSO.FindProperty("dwellTimeRequired").floatValue = 2.0f;
            azSO.ApplyModifiedProperties();
            assemblyZone.AssemblyCenter = new Vector3(8.5f, 1.6f, 94.0f);

            // ── Assessment Result Screen Overlay ──────────────────────────────
            var resultScreen = CreateAssessmentResultOverlay(canvasGO);

            // ── Master Fire Assessment Controller ─────────────────────────────
            GameObject assessmentMgrGO = new GameObject("FireAssessmentController");
            var assessmentCtrl = assessmentMgrGO.AddComponent<SurakshaAR.Training.Fire.FireAssessmentController>();
            assessmentCtrl.SetReferences(
                evaluator,
                explosionCtrl,
                alarmCtrl,
                collapseCtrl,
                assemblyZone,
                resultScreen,
                hud,
                virtualCam
            );

            // ── EventSystem ───────────────────────────────────────────────────
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // ── Save Scene ────────────────────────────────────────────────────
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[SceneBuilder] FireAssessmentSimulator scene created & saved: {scenePath}");
        }

        private static AssessmentResultScreen CreateAssessmentResultOverlay(GameObject canvasGO)
        {
            // Dark translucent overlay panel covering the screen
            GameObject overlay = CreatePanel(canvasGO, "AssessmentResultOverlay",
                new Color(0.04f, 0.06f, 0.10f, 0.96f),
                Vector2.zero, Vector2.one);

            // Center dialog card
            GameObject card = CreatePanel(overlay, "AssessmentCard",
                new Color(0.09f, 0.12f, 0.18f, 0.98f),
                new Vector2(0.06f, 0.14f), new Vector2(0.94f, 0.86f), roundedCardSprite);

            var cardOutline = card.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0.20f, 0.60f, 0.95f, 0.55f);
            cardOutline.effectDistance = new Vector2(2f, -2f);

            // Header badge: MODULE 1: FINAL ASSESSMENT
            Text headerBadge = CreateText(card, "HeaderBadge",
                "MODULE 1: FIRE & EXPLOSION RESPONSE",
                20, new Color(0.85f, 0.70f, 0.20f), TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.90f), new Vector2(0.96f, 0.97f), Vector2.zero, Vector2.zero, FontStyle.Bold);

            // Status Title: ASSESSMENT PASSED / NOT PASSED
            Text statusTitle = CreateText(card, "StatusTitle",
                "ASSESSMENT EVALUATION",
                32, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.80f), new Vector2(0.96f, 0.90f), Vector2.zero, Vector2.zero, FontStyle.Bold);

            // Big Score percent text
            Text scorePercent = CreateText(card, "ScorePercent",
                "0%",
                54, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.10f, 0.64f), new Vector2(0.90f, 0.80f), Vector2.zero, Vector2.zero, FontStyle.Bold);

            // Subtitle: (Passing score: >= 70%)
            Text scoreSubtitle = CreateText(card, "ScoreSubtitle",
                "Passing Score: 70%",
                20, new Color(0.75f, 0.82f, 0.90f), TextAnchor.MiddleCenter,
                new Vector2(0.05f, 0.58f), new Vector2(0.95f, 0.64f), Vector2.zero, Vector2.zero);

            // Breakdown card container
            GameObject breakdownContainer = CreatePanel(card, "BreakdownBox",
                new Color(0.05f, 0.08f, 0.13f, 0.90f),
                new Vector2(0.05f, 0.33f), new Vector2(0.95f, 0.57f));

            Text fireBreakdown = CreateText(breakdownContainer, "FireBreakdown",
                "• Electric Fire Response:  0 / 40 pts",
                21, new Color(0.90f, 0.92f, 0.96f), TextAnchor.MiddleLeft,
                new Vector2(0.05f, 0.66f), new Vector2(0.95f, 0.96f), Vector2.zero, Vector2.zero, FontStyle.Bold);

            Text routeBreakdown = CreateText(breakdownContainer, "RouteBreakdown",
                "• Route B Navigation:      0 / 30 pts",
                21, new Color(0.90f, 0.92f, 0.96f), TextAnchor.MiddleLeft,
                new Vector2(0.05f, 0.33f), new Vector2(0.95f, 0.66f), Vector2.zero, Vector2.zero, FontStyle.Bold);

            Text evacBreakdown = CreateText(breakdownContainer, "EvacBreakdown",
                "• Evacuation & Survival:   0 / 30 pts",
                21, new Color(0.90f, 0.92f, 0.96f), TextAnchor.MiddleLeft,
                new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.33f), Vector2.zero, Vector2.zero, FontStyle.Bold);

            // Feedback summary text
            Text feedbackSummary = CreateText(card, "FeedbackSummary",
                "Evaluation in progress...",
                20, new Color(0.80f, 0.88f, 0.95f), TextAnchor.MiddleCenter,
                new Vector2(0.05f, 0.18f), new Vector2(0.95f, 0.32f), Vector2.zero, Vector2.zero);

            // Action button (NEXT & CONFIRM / RETRY ASSESSMENT)
            Button actionBtn = CreateButton(card, "ActionButton", "CONFIRM",
                PrimaryOrangeColor, Color.white,
                new Vector2(0.10f, 0.04f), new Vector2(0.90f, 0.16f), Vector2.zero, Vector2.zero, true);
            Text btnText = actionBtn.GetComponentInChildren<Text>();

            var resultScreen = canvasGO.AddComponent<AssessmentResultScreen>();
            resultScreen.SetReferences(
                overlay,
                headerBadge,
                statusTitle,
                scorePercent,
                scoreSubtitle,
                fireBreakdown,
                routeBreakdown,
                evacBreakdown,
                feedbackSummary,
                actionBtn,
                btnText
            );

            overlay.SetActive(false);
            return resultScreen;
        }
    }
}
#endif
