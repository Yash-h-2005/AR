using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using SurakshaAR.UI.Navigation;

namespace SurakshaAR.AR
{
    /// <summary>
    /// PHASE 3 — Interactive AR Fire Discovery and Identification Controller.
    ///
    /// Responsibilities:
    ///   1. Opens Android rear camera via ARFoundation (real camera dominant view).
    ///   2. Detects real horizontal floor planes using ARPlaneManager.
    ///   3. Places persistent ARAnchor on detected real floor with ARElectricalFireHazard.
    ///   4. Real-time physical worker approach tracking (LOOK, MOVE, OBSERVE, DECIDE).
    ///   5. Proximity-triggered contextual objective: "Identify the type of fire."
    ///   6. Interactive fire classification choices (Electrical, Combustible, Flammable Liquid).
    ///   7. Responsive feedback: educational retry on incorrect, positive validation on correct.
    ///   8. NO permanent step counters or artificial progress bars.
    /// </summary>
    public class ARPhase1FloorScanController : MonoBehaviour
    {
        // ── AR Foundation References ──────────────────────────────────────────
        [Header("AR Foundation Components")]
        [SerializeField] private ARSession arSession;
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private ARAnchorManager anchorManager;
        [SerializeField] private Camera arCamera;

        // ── HUD Text References ───────────────────────────────────────────────
        [Header("HUD UI")]
        [SerializeField] private Text instructionText;
        [SerializeField] private Text statusBadgeText;
        [SerializeField] private GameObject tapHintPanel;
        [SerializeField] private GameObject anchorConfirmedPanel;
        [SerializeField] private Button continueButton;

        // ── Phase 3 Interactive UI Elements ───────────────────────────────────
        private Canvas mainCanvas;
        private GameObject choicePanelRoot;
        private GameObject feedbackPanelRoot;
        private Text feedbackTitleText;
        private Text feedbackDescText;
        private Button optElectricalBtn;
        private Button optCombustibleBtn;
        private Button optFlammableBtn;
        private Button feedbackActionBtn;

        // ── Internal State ────────────────────────────────────────────────────
        private bool groundDetected = false;
        private bool anchorPlaced = false;
        private bool isIdentified = false;
        private bool showingFeedback = false;
        private ARAnchor trainingAnchor;
        private ARElectricalFireHazard spawnedHazard;
        private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

        // Scan pulse animation
        private float pulseTimer = 0f;
        private readonly string[] scanDots = { ".", "..", "..." };

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Awake()
        {
            if (arSession == null) arSession = FindFirstObjectByType<ARSession>();
            if (planeManager == null) planeManager = FindFirstObjectByType<ARPlaneManager>();
            if (raycastManager == null) raycastManager = FindFirstObjectByType<ARRaycastManager>();
            if (anchorManager == null) anchorManager = FindFirstObjectByType<ARAnchorManager>();
            if (arCamera == null) arCamera = Camera.main;

            mainCanvas = FindFirstObjectByType<Canvas>();
        }

        private void OnEnable()
        {
            if (planeManager != null)
            {
                planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
                planeManager.trackablesChanged.AddListener(OnPlanesChanged);
            }

            ARSession.stateChanged += OnARSessionStateChanged;
        }

        private void OnDisable()
        {
            if (planeManager != null)
                planeManager.trackablesChanged.RemoveListener(OnPlanesChanged);

            ARSession.stateChanged -= OnARSessionStateChanged;
        }

        private void Start()
        {
            // Initial state: scanning real floor
            SetInstruction("Scan the ground and move your phone slowly.");
            SetStatusBadge("Scanning floor", new Color(0.96f, 0.62f, 0.04f));  // amber
            SetTapHintVisible(false);
            SetAnchorConfirmedVisible(false);

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinuePressed);
                continueButton.gameObject.SetActive(false);
            }

            // Build dynamic Phase 3 interactive UI overlay
            BuildInteractiveIdentificationUI();

            StartCoroutine(InitializeARSessionRoutine());

            Debug.Log("[ARPhase3] Started. Waiting for real ground plane detection.");
        }

        private IEnumerator InitializeARSessionRoutine()
        {
#if UNITY_ANDROID
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                Debug.Log("[ARPhase3] Requesting Android Camera permission...");
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
                yield return new WaitForSeconds(0.5f);
            }
#endif

            if (ARSession.state == ARSessionState.None || ARSession.state == ARSessionState.CheckingAvailability)
            {
                Debug.Log("[ARPhase3] Checking ARCore availability...");
                yield return ARSession.CheckAvailability();
            }

            if (ARSession.state == ARSessionState.NeedsInstall)
            {
                Debug.Log("[ARPhase3] Installing ARCore runtime...");
                yield return ARSession.Install();
            }

            Debug.Log($"[ARPhase3] ARCore State: {ARSession.state}");
        }

        private void Update()
        {
            if (anchorPlaced)
            {
                // Real-time worker movement & proximity tracking for Phase 3
                if (trainingAnchor != null)
                {
                    Transform camTransform = arCamera != null ? arCamera.transform : (Camera.main != null ? Camera.main.transform : null);
                    if (camTransform != null)
                    {
                        float distance = Vector3.Distance(camTransform.position, trainingAnchor.transform.position);

                        if (!isIdentified)
                        {
                            if (distance > 2.4f)
                            {
                                SetInstruction($"Investigate the fire.\n\n<size=22><color=#FFA500>Distance: {distance:F1}m — Walk forward toward fire</color></size>");
                                SetStatusBadge($"Fire Active ({distance:F1}m)", new Color(1f, 0.5f, 0.1f));
                                SetChoicePanelVisible(false);
                            }
                            else if (distance > 0.7f)
                            {
                                SetInstruction($"Identify the type of fire.\n\n<size=22><color=#00E5FF>Distance: {distance:F1}m — Observe arcing, panel & conduits</color></size>");
                                SetStatusBadge($"Observing Hazard ({distance:F1}m)", new Color(0.0f, 0.9f, 1f));
                                if (!showingFeedback)
                                    SetChoicePanelVisible(true);
                            }
                            else
                            {
                                SetInstruction($"⚠ High Heat Area\n\n<size=22><color=#FF4444>Distance: {distance:F1}m — Maintain safe observation distance</color></size>");
                                SetStatusBadge($"High Heat ({distance:F1}m)", Color.red);
                                if (!showingFeedback)
                                    SetChoicePanelVisible(true);
                            }
                        }
                        else
                        {
                            // Identified success state
                            SetInstruction($"✔ Electrical Fire (Class C) Identified\n\n<size=22><color=#00FF88>Distance: {distance:F1}m — Ready for safety response</color></size>");
                            SetStatusBadge($"✔ Class C Identified", new Color(0.06f, 0.73f, 0.51f));
                        }
                    }
                }
                return;
            }

            // Animate scan dots
            if (!groundDetected)
            {
                pulseTimer += Time.deltaTime;
                if (pulseTimer >= 0.6f)
                {
                    pulseTimer = 0f;
                    int idx = Mathf.FloorToInt(Time.time / 0.6f) % scanDots.Length;
                    SetInstruction("Scan the ground and move your phone slowly" + scanDots[idx]);
                }
            }

            // Handle tap input
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    TryPlaceAnchor(touch.position);
                }
            }

#if UNITY_EDITOR
            // Editor mouse click fallback for testing
            if (Input.GetMouseButtonDown(0))
            {
                TryPlaceAnchor(Input.mousePosition);
            }
#endif
        }

        // ── AR Event Handlers ─────────────────────────────────────────────────

        private void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
        {
            if (!groundDetected && args.added.Count > 0)
            {
                groundDetected = true;
                Debug.Log("[ARPhase3] Real ground plane detected!");

                SetInstruction("Floor detected!\nTap the floor to place the fire hazard.");
                SetStatusBadge("Floor detected — Tap to place", new Color(0.06f, 0.73f, 0.51f)); // green
                SetTapHintVisible(true);
            }
        }

        private void OnARSessionStateChanged(ARSessionStateChangedEventArgs args)
        {
            if (args.state == ARSessionState.Unsupported)
            {
                SetInstruction("ARCore is not supported on this device.");
                SetStatusBadge("AR Unsupported", Color.red);
                Debug.LogError("[ARPhase3] ARCore unsupported on this device.");
            }
            else if (args.state == ARSessionState.SessionTracking)
            {
                Debug.Log("[ARPhase3] AR Session tracking confirmed.");
            }
        }

        // ── Anchor Placement & Realistic Electrical Fire Spawning ─────────────

        private void TryPlaceAnchor(Vector2 screenPosition)
        {
            if (raycastManager != null && raycastManager.Raycast(screenPosition, hits, TrackableType.AllTypes))
            {
                Pose hitPose = hits[0].pose;
                ARPlane hitPlane = planeManager != null ? planeManager.GetPlane(hits[0].trackableId) : null;
                PlaceTrainingOriginAnchor(hitPose, hitPlane);
                return;
            }

            // Fallback: Place anchor 1.6m in front of camera on estimated floor level
            Transform cam = arCamera != null ? arCamera.transform : (Camera.main != null ? Camera.main.transform : null);
            if (cam != null)
            {
                Vector3 forwardFlat = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
                if (forwardFlat.sqrMagnitude < 0.01f) forwardFlat = cam.forward;
                Vector3 estimatedPos = cam.position + (forwardFlat * 1.6f);
                estimatedPos.y = cam.position.y - 1.1f; // ~floor level below phone
                Pose fallbackPose = new Pose(estimatedPos, Quaternion.LookRotation(forwardFlat, Vector3.up));
                PlaceTrainingOriginAnchor(fallbackPose, null);
            }
        }

        private void PlaceTrainingOriginAnchor(Pose pose, ARPlane plane)
        {
            // Remove previous anchor if retapping
            if (trainingAnchor != null)
            {
                Destroy(trainingAnchor.gameObject);
                trainingAnchor = null;
            }

            // 1. Create persistent AR Anchor at tapped real floor coordinates
            GameObject anchorGO = new GameObject("TRAINING_ORIGIN_Anchor");
            anchorGO.transform.SetPositionAndRotation(pose.position, pose.rotation);
            trainingAnchor = anchorGO.AddComponent<ARAnchor>();

            // 2. Attach Phase 3 Realistic AR Electrical Fire Hazard onto anchor
            spawnedHazard = anchorGO.AddComponent<ARElectricalFireHazard>();

            anchorPlaced = true;
            isIdentified = false;
            showingFeedback = false;

            Debug.Log($"[ARPhase3] Realistic AR Electrical Fire anchored at real-world position: {pose.position}");

            // Hide plane visualisers — keep the camera feed clean
            SetPlaneVisualizersVisible(false);

            // Update HUD for Phase 3: "Investigate the fire."
            SetInstruction("Investigate the fire.\n\n<size=22><color=#FFA500>Walk towards the fire to inspect</color></size>");
            SetStatusBadge("Fire Active", new Color(1f, 0.5f, 0.1f));
            SetTapHintVisible(false);
            SetAnchorConfirmedVisible(false);

            if (continueButton != null)
                continueButton.gameObject.SetActive(false); // Only enable after successful identification
        }

        // ── Interactive UI Construction (Phase 3) ─────────────────────────────

        private void BuildInteractiveIdentificationUI()
        {
            if (mainCanvas == null) return;

            // ── 1. Choice Selection Panel (Bottom Area) ─────────────────────────
            choicePanelRoot = new GameObject("Phase3_ChoicePanel");
            choicePanelRoot.transform.SetParent(mainCanvas.transform, false);

            RectTransform choiceRect = choicePanelRoot.AddComponent<RectTransform>();
            choiceRect.anchorMin = new Vector2(0.05f, 0.05f);
            choiceRect.anchorMax = new Vector2(0.95f, 0.38f);
            choiceRect.offsetMin = Vector2.zero;
            choiceRect.offsetMax = Vector2.zero;

            Image choiceBg = choicePanelRoot.AddComponent<Image>();
            choiceBg.color = new Color(0.05f, 0.08f, 0.14f, 0.90f);

            VerticalLayoutGroup choiceVLayout = choicePanelRoot.AddComponent<VerticalLayoutGroup>();
            choiceVLayout.padding = new RectOffset(20, 20, 16, 16);
            choiceVLayout.spacing = 10;
            choiceVLayout.childControlHeight = true;
            choiceVLayout.childControlWidth = true;
            choiceVLayout.childForceExpandHeight = false;

            // Title
            GameObject titleGO = new GameObject("ChoiceTitle");
            titleGO.transform.SetParent(choicePanelRoot.transform, false);
            Text titleTxt = titleGO.AddComponent<Text>();
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (titleTxt.font == null) titleTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleTxt.text = "IDENTIFY THE FIRE TYPE";
            titleTxt.fontSize = 24;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(1f, 0.75f, 0.1f);
            var titleLE = titleGO.AddComponent<LayoutElement>();
            titleLE.minHeight = 32;

            // Option 1: Electrical Fire (Class C / E)
            optElectricalBtn = CreateChoiceButton(choicePanelRoot.transform, "⚡  Electrical Fire (Class C / E)", new Color(0.08f, 0.18f, 0.32f), () => OnOptionSelected(FireType.Electrical));

            // Option 2: Ordinary Combustible (Class A)
            optCombustibleBtn = CreateChoiceButton(choicePanelRoot.transform, "🪵  Ordinary Combustible (Class A - Wood/Paper)", new Color(0.14f, 0.16f, 0.20f), () => OnOptionSelected(FireType.Combustible));

            // Option 3: Flammable Liquid (Class B)
            optFlammableBtn = CreateChoiceButton(choicePanelRoot.transform, "🛢️  Flammable Liquid (Class B - Oil/Fuel)", new Color(0.14f, 0.16f, 0.20f), () => OnOptionSelected(FireType.FlammableLiquid));

            choicePanelRoot.SetActive(false);

            // ── 2. Responsive Feedback Panel ───────────────────────────────────
            feedbackPanelRoot = new GameObject("Phase3_FeedbackPanel");
            feedbackPanelRoot.transform.SetParent(mainCanvas.transform, false);

            RectTransform fbRect = feedbackPanelRoot.AddComponent<RectTransform>();
            fbRect.anchorMin = new Vector2(0.05f, 0.05f);
            fbRect.anchorMax = new Vector2(0.95f, 0.40f);
            fbRect.offsetMin = Vector2.zero;
            fbRect.offsetMax = Vector2.zero;

            Image fbBg = feedbackPanelRoot.AddComponent<Image>();
            fbBg.color = new Color(0.06f, 0.09f, 0.16f, 0.95f);

            VerticalLayoutGroup fbVLayout = feedbackPanelRoot.AddComponent<VerticalLayoutGroup>();
            fbVLayout.padding = new RectOffset(24, 24, 20, 20);
            fbVLayout.spacing = 12;
            fbVLayout.childControlHeight = true;
            fbVLayout.childControlWidth = true;
            fbVLayout.childForceExpandHeight = false;

            // Feedback Title
            GameObject fbTitleGO = new GameObject("FeedbackTitle");
            fbTitleGO.transform.SetParent(feedbackPanelRoot.transform, false);
            feedbackTitleText = fbTitleGO.AddComponent<Text>();
            feedbackTitleText.font = titleTxt.font;
            feedbackTitleText.fontSize = 24;
            feedbackTitleText.fontStyle = FontStyle.Bold;
            feedbackTitleText.alignment = TextAnchor.MiddleCenter;
            feedbackTitleText.color = Color.white;
            var fbTitleLE = fbTitleGO.AddComponent<LayoutElement>();
            fbTitleLE.minHeight = 34;

            // Feedback Description
            GameObject fbDescGO = new GameObject("FeedbackDesc");
            fbDescGO.transform.SetParent(feedbackPanelRoot.transform, false);
            feedbackDescText = fbDescGO.AddComponent<Text>();
            feedbackDescText.font = titleTxt.font;
            feedbackDescText.fontSize = 20;
            feedbackDescText.alignment = TextAnchor.MiddleLeft;
            feedbackDescText.color = new Color(0.85f, 0.9f, 0.95f);
            var fbDescLE = fbDescGO.AddComponent<LayoutElement>();
            fbDescLE.minHeight = 90;

            // Feedback Action Button
            GameObject fbBtnGO = new GameObject("FeedbackActionBtn");
            fbBtnGO.transform.SetParent(feedbackPanelRoot.transform, false);
            feedbackActionBtn = fbBtnGO.AddComponent<Button>();
            Image fbBtnImg = fbBtnGO.AddComponent<Image>();
            fbBtnImg.color = new Color(0.96f, 0.45f, 0.04f);
            var fbBtnLE = fbBtnGO.AddComponent<LayoutElement>();
            fbBtnLE.minHeight = 55;

            GameObject fbBtnTxtGO = new GameObject("BtnText");
            fbBtnTxtGO.transform.SetParent(fbBtnGO.transform, false);
            Text fbBtnTxt = fbBtnTxtGO.AddComponent<Text>();
            fbBtnTxt.font = titleTxt.font;
            fbBtnTxt.text = "Look Again & Re-evaluate";
            fbBtnTxt.fontSize = 20;
            fbBtnTxt.fontStyle = FontStyle.Bold;
            fbBtnTxt.alignment = TextAnchor.MiddleCenter;
            fbBtnTxt.color = Color.white;
            RectTransform btnTxtRect = fbBtnTxtGO.GetComponent<RectTransform>();
            btnTxtRect.anchorMin = Vector2.zero;
            btnTxtRect.anchorMax = Vector2.one;
            btnTxtRect.offsetMin = Vector2.zero;
            btnTxtRect.offsetMax = Vector2.zero;

            feedbackPanelRoot.SetActive(false);
        }

        private Button CreateChoiceButton(Transform parent, string label, Color bgColor, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnGO = new GameObject("Btn_" + label.Substring(0, Mathf.Min(10, label.Length)));
            btnGO.transform.SetParent(parent, false);

            Button btn = btnGO.AddComponent<Button>();
            Image img = btnGO.AddComponent<Image>();
            img.color = bgColor;

            var le = btnGO.AddComponent<LayoutElement>();
            le.minHeight = 52;

            GameObject txtGO = new GameObject("Text");
            txtGO.transform.SetParent(btnGO.transform, false);
            Text txt = txtGO.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null) txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = label;
            txt.fontSize = 19;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            RectTransform txtRect = txtGO.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = new Vector2(10, 0);
            txtRect.offsetMax = new Vector2(-10, 0);

            btn.onClick.AddListener(onClick);
            return btn;
        }

        private enum FireType
        {
            Electrical,
            Combustible,
            FlammableLiquid
        }

        private void OnOptionSelected(FireType selectedType)
        {
            SetChoicePanelVisible(false);
            showingFeedback = true;

            if (selectedType == FireType.Electrical)
            {
                // ── Correct Answer ─────────────────────────────────────────────
                isIdentified = true;
                feedbackTitleText.text = "✔  CORRECT IDENTIFICATION";
                feedbackTitleText.color = new Color(0.1f, 0.9f, 0.4f);

                feedbackDescText.text = "You correctly identified a <b>Class C Electrical Fire</b>.\n\n" +
                                       "• Energized 440V distribution panel\n" +
                                       "• Visible high-frequency electric arcing\n" +
                                       "• Acrid burning PVC conduit insulation\n\n" +
                                       "<color=#FFA500>Never use water on an energized electrical fire!</color>";

                feedbackActionBtn.GetComponentInChildren<Text>().text = "Continue to Safety Isolation →";
                feedbackActionBtn.GetComponent<Image>().color = new Color(0.06f, 0.73f, 0.51f); // Green
                feedbackActionBtn.onClick.RemoveAllListeners();
                feedbackActionBtn.onClick.AddListener(OnContinuePressed);

                feedbackPanelRoot.SetActive(true);

                SetStatusBadge("✔ Fire Identified: Class C (Electrical)", new Color(0.06f, 0.73f, 0.51f));
                SetInstruction("✔ Electrical Fire (Class C) Identified.\n\n<size=22><color=#00FF88>Proceed to next response step.</color></size>");

                if (continueButton != null)
                    continueButton.gameObject.SetActive(true);
            }
            else if (selectedType == FireType.Combustible)
            {
                // ── Incorrect (Combustible) ────────────────────────────────────
                feedbackTitleText.text = "⚠  INCORRECT IDENTIFICATION";
                feedbackTitleText.color = new Color(1.0f, 0.6f, 0.1f);

                feedbackDescText.text = "<b>Class A (Ordinary Combustible)</b> involves wood, paper, or textiles.\n\n" +
                                       "• Notice the blue electrical arcing and sparks snapping from the panel.\n" +
                                       "• Look at the 440V High Voltage warning sign and industrial cables.\n\n" +
                                       "Observe the equipment closely and try again.";

                feedbackActionBtn.GetComponentInChildren<Text>().text = "Inspect Clues & Try Again";
                feedbackActionBtn.GetComponent<Image>().color = new Color(0.96f, 0.45f, 0.04f);
                feedbackActionBtn.onClick.RemoveAllListeners();
                feedbackActionBtn.onClick.AddListener(() =>
                {
                    feedbackPanelRoot.SetActive(false);
                    showingFeedback = false;
                    SetChoicePanelVisible(true);
                });

                feedbackPanelRoot.SetActive(true);
            }
            else if (selectedType == FireType.FlammableLiquid)
            {
                // ── Incorrect (Flammable Liquid) ───────────────────────────────
                feedbackTitleText.text = "⚠  INCORRECT IDENTIFICATION";
                feedbackTitleText.color = new Color(1.0f, 0.6f, 0.1f);

                feedbackDescText.text = "<b>Class B (Flammable Liquid)</b> involves oil, fuels, or chemical spills.\n\n" +
                                       "• There is no pooling liquid or fuel spill on the floor.\n" +
                                       "• The flames and smoke are erupting from an energized metal cabinet with electrical arcing.\n\n" +
                                       "Look at the hazard markings and re-evaluate.";

                feedbackActionBtn.GetComponentInChildren<Text>().text = "Inspect Clues & Try Again";
                feedbackActionBtn.GetComponent<Image>().color = new Color(0.96f, 0.45f, 0.04f);
                feedbackActionBtn.onClick.RemoveAllListeners();
                feedbackActionBtn.onClick.AddListener(() =>
                {
                    feedbackPanelRoot.SetActive(false);
                    showingFeedback = false;
                    SetChoicePanelVisible(true);
                });

                feedbackPanelRoot.SetActive(true);
            }
        }

        private void SetChoicePanelVisible(bool visible)
        {
            if (choicePanelRoot != null && choicePanelRoot.activeSelf != visible)
                choicePanelRoot.SetActive(visible);
        }

        // ── Plane Visualizer Toggle ───────────────────────────────────────────

        private void SetPlaneVisualizersVisible(bool visible)
        {
            if (planeManager == null) return;
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(visible);
            }
        }

        // ── UI Helpers ────────────────────────────────────────────────────────

        private void SetInstruction(string text)
        {
            if (instructionText != null)
                instructionText.text = text;
        }

        private void SetStatusBadge(string text, Color color)
        {
            if (statusBadgeText != null)
            {
                statusBadgeText.text = text;
                statusBadgeText.color = color;
            }
        }

        private void SetTapHintVisible(bool visible)
        {
            if (tapHintPanel != null)
                tapHintPanel.SetActive(visible);
        }

        private void SetAnchorConfirmedVisible(bool visible)
        {
            if (anchorConfirmedPanel != null)
                anchorConfirmedPanel.SetActive(visible);
        }

        // ── Navigation ────────────────────────────────────────────────────────

        private void OnContinuePressed()
        {
            if (trainingAnchor != null)
            {
                PlayerPrefs.SetFloat("TRAINING_ORIGIN_X", trainingAnchor.transform.position.x);
                PlayerPrefs.SetFloat("TRAINING_ORIGIN_Y", trainingAnchor.transform.position.y);
                PlayerPrefs.SetFloat("TRAINING_ORIGIN_Z", trainingAnchor.transform.position.z);
                PlayerPrefs.SetString("FIRE_TYPE_IDENTIFIED", "ClassC_Electrical");
                PlayerPrefs.Save();
                Debug.Log($"[ARPhase3] Identified Class C Fire. Saved coordinates.");
            }

            if (SceneNavigator.Instance != null)
            {
                SceneNavigator.Instance.NavigateTo(ScreenState.ARLearnPlaceholder);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("ARLearnPlaceholder");
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        public Transform GetTrainingOriginTransform()
        {
            return trainingAnchor != null ? trainingAnchor.transform : null;
        }

        public bool IsAnchorPlaced => anchorPlaced;
    }
}
