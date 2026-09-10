using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SurakshaAR.Training.Fire
{
    /// <summary>
    /// Phase 1 Mine Simulator HUD.
    /// Minimal overlay: tracking state badge, one-time fade instruction, tracking-loss warning.
    /// NO joystick. NO step numbers. NO progress bar.
    /// </summary>
    public class MineSimulatorHUD : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("HUD References (set by SceneBuilder or Inspector)")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text trackingBadgeText;
        [SerializeField] private Image trackingBadgeDot;
        [SerializeField] private Text instructionText;
        [SerializeField] private CanvasGroup instructionGroup;
        [SerializeField] private GameObject trackingLostOverlay;
        [SerializeField] private Text trackingLostText;

        [Header("Movement Controller")]
        [SerializeField] private MineMovementController movementController;

        [Header("Phase 2A — Fire & Alarm")]
        [SerializeField] private FireHazardController fireHazardController;
        [SerializeField] private EmergencyAlarmController emergencyAlarmController;
        [SerializeField] private GameObject fireAlertBanner;
        [SerializeField] private Text fireAlertText;

        [Header("Phase 2B — Fire Assessment & Resources")]
        [SerializeField] private FireResponseResourceManager resourceManager;
        [SerializeField] private Text hpText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private GameObject objectivePanel;

        [Header("Phase 2B-2 — Fire Response Evaluator")]
        [SerializeField] private FireResponseEvaluator responseEvaluator;

        [Header("Settings")]
        [Tooltip("Seconds before the instruction text begins to fade out.")]
        [SerializeField] private float instructionDisplayTime = 4.5f;
        [SerializeField] private float instructionFadeTime = 1.5f;

        // ── Colors ────────────────────────────────────────────────────────────
        private static readonly Color TrackingGreen  = new Color(0.06f, 0.85f, 0.50f, 1f);
        private static readonly Color TrackingAmber  = new Color(0.97f, 0.72f, 0.04f, 1f);
        private static readonly Color TrackingRed    = new Color(0.95f, 0.25f, 0.18f, 1f);

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Start()
        {
            if (movementController != null)
                movementController.OnTrackingStateChanged += HandleTrackingStateChanged;

            // Phase 2A: subscribe to fire and alarm events
            if (fireHazardController != null)
            {
                fireHazardController.OnFireTriggered    += HandleFireTriggered;
                fireHazardController.OnFireFullyGrown   += HandleFireFullyGrown;
                fireHazardController.OnFireExtinguished += HandleFireExtinguished;
            }
            if (emergencyAlarmController != null)
            {
                emergencyAlarmController.OnAlarmActivated += HandleAlarmActivated;
            }

            // Phase 2B: subscribe to resource selection
            if (resourceManager != null)
            {
                resourceManager.OnResourceSelected += HandleResourceSelected;
            }

            // Phase 2B-2: subscribe to evaluator events
            if (responseEvaluator != null)
            {
                responseEvaluator.OnFireControlled += HandleFireControlled;
                responseEvaluator.OnHPChanged      += HandleHPChanged;
            }

            SetTrackingUI(TrackingState.Initializing);

            if (trackingLostOverlay != null)
                trackingLostOverlay.SetActive(false);

            if (fireAlertBanner != null)
                fireAlertBanner.SetActive(false);

            if (debugHUDPanel != null)
                debugHUDPanel.SetActive(false);

            // Initialize Phase 2B HUD elements
            if (hpText != null)
                hpText.text = "HP: 100";

            SetObjective("Enter the underground mine.");

            StartCoroutine(FadeInstructionOut());
        }

        private void OnDestroy()
        {
            if (movementController != null)
                movementController.OnTrackingStateChanged -= HandleTrackingStateChanged;

            if (fireHazardController != null)
            {
                fireHazardController.OnFireTriggered    -= HandleFireTriggered;
                fireHazardController.OnFireFullyGrown   -= HandleFireFullyGrown;
                fireHazardController.OnFireExtinguished -= HandleFireExtinguished;
            }
            if (emergencyAlarmController != null)
            {
                emergencyAlarmController.OnAlarmActivated -= HandleAlarmActivated;
            }
            if (resourceManager != null)
            {
                resourceManager.OnResourceSelected -= HandleResourceSelected;
            }
            if (responseEvaluator != null)
            {
                responseEvaluator.OnFireControlled -= HandleFireControlled;
                responseEvaluator.OnHPChanged      -= HandleHPChanged;
            }
        }

        // ── Instruction Fade ──────────────────────────────────────────────────

        private IEnumerator FadeInstructionOut()
        {
            if (instructionGroup == null) yield break;

            instructionGroup.alpha = 1f;
            yield return new WaitForSeconds(instructionDisplayTime);

            float elapsed = 0f;
            while (elapsed < instructionFadeTime)
            {
                elapsed += Time.deltaTime;
                instructionGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / instructionFadeTime);
                yield return null;
            }
            instructionGroup.alpha = 0f;
        }

        // ── Tracking State UI ─────────────────────────────────────────────────

        private void HandleTrackingStateChanged(TrackingState state)
        {
            SetTrackingUI(state);
        }

        private void SetTrackingUI(TrackingState state)
        {
            switch (state)
            {
                case TrackingState.Initializing:
                    SetBadge("Initializing", TrackingAmber);
                    if (instructionText != null) instructionText.text = "Initializing tracking...";
                    if (instructionGroup != null) instructionGroup.alpha = 1f;
                    if (trackingLostOverlay != null) trackingLostOverlay.SetActive(false);
                    break;

                case TrackingState.Calibrating:
                    SetBadge("● Calibrating", TrackingAmber);
                    if (instructionText != null) instructionText.text = "Face the direction you want to start walking.";
                    if (instructionGroup != null) instructionGroup.alpha = 1f;
                    if (trackingLostOverlay != null) trackingLostOverlay.SetActive(false);
                    break;

                case TrackingState.Tracking:
                    SetBadge("● Tracking", TrackingGreen);
                    if (instructionText != null) instructionText.text = "Walk physically to move in the phone facing direction.";
                    if (trackingLostOverlay != null) trackingLostOverlay.SetActive(false);
                    StopAllCoroutines();
                    StartCoroutine(FadeInstructionOut());
                    break;

                case TrackingState.Lost:
                    SetBadge("⚠ Lost", TrackingRed);
                    if (trackingLostText != null) trackingLostText.text = "Tracking lost — pause and reorient your phone.";
                    if (trackingLostOverlay != null) trackingLostOverlay.SetActive(true);
                    break;
            }
        }

        public void SetBadge(string label, Color color)
        {
            if (trackingBadgeText != null)
            {
                trackingBadgeText.text = label;
                trackingBadgeText.color = color;
            }
            if (trackingBadgeDot != null)
                trackingBadgeDot.color = color;
        }

        [Header("Debug HUD Overlay")]
        [SerializeField] private GameObject debugHUDPanel;
        [SerializeField] private Text debugHUDText;

        // ── Objective progression state ───────────────────────────────────────
        private bool hasEnteredMine = false;

        private void Update()
        {
            // ── Z-based objective transition: outdoor → mine interior ────────────
            if (!hasEnteredMine && movementController != null)
            {
                if (movementController.VirtualPosition.z >= 0f)
                {
                    hasEnteredMine = true;
                    SetObjective("Explore the virtual mine corridor.");
                    Debug.Log("[HUD] Player entered mine — objective updated to explore corridor.");
                }
            }

            // Debug telemetry panel is disabled for production HUD cleanliness
            if (debugHUDPanel != null && debugHUDPanel.activeSelf && debugHUDText != null && movementController != null)
            {
                float yaw = movementController.PhoneYaw;
                float pitch = movementController.PhonePitch;
                string motion = movementController.MotionState;
                string dir = movementController.WalkDirection;
                float dist = movementController.EstimatedWalkDistance;
                Vector3 vPos = movementController.VirtualPosition;

                string motionColor = (motion == "WALKING") ? "#10B981" : "#94A3B8";
                string dirColor = (dir == "FORWARD") ? "#38BDF8" : ((dir == "BACKWARD") ? "#F59E0B" : "#94A3B8");

                debugHUDText.text =
                    $"<b>PHONE YAW:</b> {yaw:F0}°\n" +
                    $"<b>PHONE PITCH:</b> {pitch:F0}°\n" +
                    $"<b>MOTION STATE:</b> <color={motionColor}><b>{motion}</b></color>\n" +
                    $"<b>WALK DIRECTION:</b> <color={dirColor}><b>{dir}</b></color>\n" +
                    $"<b>ESTIMATED WALK DISTANCE:</b> {dist:F2} m\n" +
                    $"<b>VIRTUAL POSITION:</b> X {vPos.x:F2}  Z {vPos.z:F2}";
            }
        }

        // ── Public API for SceneBuilder wiring ────────────────────────────────

        public void SetReferences(Text title, Text badge, Image badgeDot,
            Text instruction, CanvasGroup instrGroup,
            GameObject lostOverlay, Text lostText,
            MineMovementController controller)
        {
            titleText            = title;
            trackingBadgeText    = badge;
            trackingBadgeDot     = badgeDot;
            instructionText      = instruction;
            instructionGroup     = instrGroup;
            trackingLostOverlay  = lostOverlay;
            trackingLostText     = lostText;
            movementController   = controller;
        }

        public void SetDebugHUD(GameObject panel, Text text)
        {
            debugHUDPanel = panel;
            debugHUDText  = text;
            if (debugHUDPanel != null) debugHUDPanel.SetActive(false);
        }

        // ── Phase 2A Event Handlers ───────────────────────────────────────────

        private void HandleFireTriggered()
        {
            // Show fire alert banner
            if (fireAlertBanner != null)
                fireAlertBanner.SetActive(true);

            // Update instruction text to guide worker to the alarm
            if (instructionText != null)
                instructionText.text = "Fire detected! Locate the Emergency Alarm.";
            if (instructionGroup != null)
            {
                StopAllCoroutines();
                instructionGroup.alpha = 1f;
                StartCoroutine(FadeInstructionOut());
            }

            Debug.Log("[HUD] Fire triggered — showing alert banner.");
        }

        private void HandleFireFullyGrown()
        {
            if (instructionText != null)
                instructionText.text = "Approach the Emergency Alarm and TAP to activate it.";
            if (instructionGroup != null)
            {
                StopAllCoroutines();
                instructionGroup.alpha = 1f;
                StartCoroutine(FadeInstructionOut());
            }
        }

        private void HandleAlarmActivated()
        {
            // Hide fire alert banner — alarm is now active
            if (fireAlertBanner != null)
                fireAlertBanner.SetActive(false);

            // Update tracking badge to show alarm state
            SetBadge("🚨 ALARM ACTIVE", new Color(1f, 0.2f, 0.1f));

            // Phase 2B-1 Objective: Assess the fire and identify a safe response
            SetObjective("Assess the fire and identify a safe response.");

            // Activate assessment mode on resource manager
            if (resourceManager != null)
                resourceManager.ActivateAssessment();

            Debug.Log("[HUD] Alarm activated — Objective set to fire assessment.");
        }

        private void HandleResourceSelected(ResponseResourceType resourceType)
        {
            // Phase 2B-1 Objective: Use the selected response on the fire
            SetObjective("Use the selected response on the fire.");

            Debug.Log("[HUD] Resource selected (" + resourceType + ") — Objective updated.");
        }

        // ── Phase 2B-2 Event Handlers ─────────────────────────────────────────

        private void HandleFireControlled()
        {
            if (fireAlertBanner != null)
                fireAlertBanner.SetActive(false);

            SetObjective("Proceed to the emergency exit.");
            SetBadge("Evacuation Active", new Color(0.20f, 0.80f, 1.0f));
            Debug.Log("[HUD] Fire controlled — Phase 2C Evacuation active: Proceed to emergency exit.");
        }

        private void HandleFireExtinguished()
        {
            if (fireAlertBanner != null)
                fireAlertBanner.SetActive(false);
            Debug.Log("[HUD] Fire extinguished — alert banner hidden.");
        }

        private void HandleHPChanged(int newHP)
        {
            if (hpText != null)
                hpText.text = "HP: " + newHP;
            Debug.Log("[HUD] HP updated to: " + newHP);
        }

        public void SetObjective(string text)
        {
            if (objectiveText != null)
                objectiveText.text = "<b>OBJECTIVE:</b> " + text;

            if (objectivePanel != null)
                objectivePanel.SetActive(true);
        }

        // ── Public API for Phase 2A & 2B wiring ───────────────────────────────

        public void SetPhase2AReferences(
            FireHazardController fireCtrl,
            EmergencyAlarmController alarmCtrl,
            GameObject alertBanner,
            Text alertText)
        {
            fireHazardController      = fireCtrl;
            emergencyAlarmController  = alarmCtrl;
            fireAlertBanner           = alertBanner;
            fireAlertText             = alertText;
        }

        public void SetPhase2BReferences(
            FireResponseResourceManager resMgr,
            Text hpTxt,
            Text objTxt,
            GameObject objPanel)
        {
            resourceManager = resMgr;
            hpText          = hpTxt;
            objectiveText   = objTxt;
            objectivePanel  = objPanel;
        }

        [Header("Phase 2C Status Toast")]
        [SerializeField] private Text statusToastText;
        [SerializeField] private CanvasGroup statusToastCanvasGroup;

        public void SetToastReferences(Text tText, CanvasGroup tGroup)
        {
            statusToastText = tText;
            statusToastCanvasGroup = tGroup;
        }

        public void ShowStatusToast(string message, Color color, float duration = 4.0f)
        {
            if (statusToastText != null)
            {
                statusToastText.text = message;
                statusToastText.color = color;
            }
            if (statusToastCanvasGroup != null)
            {
                StartCoroutine(FadeToastRoutine(duration));
            }
        }

        private IEnumerator FadeToastRoutine(float duration)
        {
            if (statusToastCanvasGroup != null) statusToastCanvasGroup.alpha = 1f;
            yield return new WaitForSeconds(duration);
            float elapsed = 0f;
            while (elapsed < 1.2f)
            {
                elapsed += Time.deltaTime;
                if (statusToastCanvasGroup != null)
                    statusToastCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / 1.2f);
                yield return null;
            }
            if (statusToastCanvasGroup != null) statusToastCanvasGroup.alpha = 0f;
        }

        public void SetPhase2B2References(FireResponseEvaluator evaluator)
        {
            responseEvaluator = evaluator;
            if (responseEvaluator != null)
            {
                responseEvaluator.OnFireControlled += HandleFireControlled;
                responseEvaluator.OnHPChanged      += HandleHPChanged;
            }
        }
    }
}
