using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SurakshaAR.UI.Data;
using SurakshaAR.AR;

namespace SurakshaAR.Training.Fire
{
    /// <summary>
    /// Central state machine for Fire & Explosion Response Learn Mode (TRUE CAMERA AR).
    /// Overlays spatially anchored 3D objects onto the live phone camera feed.
    /// Manages the 8-step safety procedure, PASS camera extinguisher technique,
    /// physical movement tracking, counterfactual hazard feedback, audio/haptics, and offline telemetry.
    /// </summary>
    public class FireLearnController : MonoBehaviour
    {
        public static FireLearnController Instance { get; private set; }

        [Header("State Machine")]
        [SerializeField] private TrainingStepType currentStep = TrainingStepType.IdentifyFire;
        public TrainingStepType CurrentStep => currentStep;

        [Header("Placement Status")]
        public bool IsSetupComplete { get; private set; } = false;

        [Header("3D Scenario Objects")]
        [SerializeField] private TrainingObject fireHazardObject;
        [SerializeField] private TrainingObject alarmButtonObject;
        [SerializeField] private TrainingObject safeExitObject;
        [SerializeField] private TrainingObject blockedExitObject;
        [SerializeField] private TrainingObject waterExtinguisherObject;
        [SerializeField] private TrainingObject co2ExtinguisherObject;
        [SerializeField] private TrainingObject assemblyZoneObject;

        [Header("Extinguisher Mechanics (PASS)")]
        [SerializeField] private bool pinPulled = false;
        public bool PinPulled => pinPulled;
        [SerializeField] private bool isExtinguisherHeld = false;
        public bool IsExtinguisherHeld => isExtinguisherHeld;
        [SerializeField] private float sprayProgress = 0f; // 0 to 1
        public float SprayProgress => sprayProgress;
        [SerializeField] private float sprayRate = 0.35f; // fill per second

        [Header("Evacuation & Camera Setup")]
        [SerializeField] private Transform arCameraTransform;
        [SerializeField] private GameObject smokeLayerObject;
        [SerializeField] private float safeCrouchHeight = 1.4f; // Meters relative to floor

        [Header("Scoring & Metrics")]
        private int currentScore = 100;
        public int CurrentScore => currentScore;

        private int stepMistakes = 0;
        private int stepRetries = 0;
        private int stepHints = 0;
        private int stepExamples = 0;
        private int stepPractice = 0;
        private bool isIndependentSuccess = true;

        // Events for UI HUD binding
        public event Action<TrainingStepType, string, float> OnStepChanged; // step, instruction, progress
        public event Action<float> OnSprayProgressUpdated; // 0 to 1
        public event Action<string, string, Action, Action> OnFeedbackTriggered; // title, body, onRetry, onHelp
        public event Action<int, float, string> OnTrainingCompleted; // finalScore, durationSec, sessionJson

        private FireState fireState = FireState.Active;
        public FireState FireState => fireState;

        private Vector3 groundOriginPosition = Vector3.zero;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (arCameraTransform == null && Camera.main != null)
            {
                arCameraTransform = Camera.main.transform;
            }

            // Initialize Telemetry & Help
            if (FireTelemetryLogger.Instance != null)
            {
                FireTelemetryLogger.Instance.StartNewSession("MOD_FIRE_01", "LEARN");
            }
            if (AdaptiveHelpController.Instance != null)
            {
                AdaptiveHelpController.Instance.ResetSessionHelp();
            }

            RegisterObjectEvents();
            SubscribeToARPlacementAndMovement();

            // Check if AR origin is already set or requires ground scanning tap
            if (ARTrainingPlacementManager.Instance != null && ARTrainingPlacementManager.Instance.IsOriginEstablished)
            {
                OnTrainingOriginPlaced(ARTrainingPlacementManager.Instance.TrainingOriginPosition);
            }
            else
            {
                OnStepChanged?.Invoke(TrainingStepType.IdentifyFire, "Scan the floor — Ensure training area is clear, then tap floor to place objects.", 0.05f);
            }
        }

        private void OnDestroy()
        {
            UnregisterObjectEvents();
            UnsubscribeFromARPlacementAndMovement();
        }

        private void SubscribeToARPlacementAndMovement()
        {
            if (ARTrainingPlacementManager.Instance != null)
            {
                ARTrainingPlacementManager.Instance.OnGroundPlaneDetected += HandleGroundPlaneDetected;
                ARTrainingPlacementManager.Instance.OnTrainingOriginPlaced += OnTrainingOriginPlaced;
            }

            if (MovementTrackingManager.Instance != null)
            {
                MovementTrackingManager.Instance.OnExitReached += HandleExitReachedByMovement;
                MovementTrackingManager.Instance.OnAssemblyReached += HandleAssemblyReachedByMovement;
                MovementTrackingManager.Instance.OnTrackingStateChanged += HandleARTrackingStateChanged;
            }
        }

        private void UnsubscribeFromARPlacementAndMovement()
        {
            if (ARTrainingPlacementManager.Instance != null)
            {
                ARTrainingPlacementManager.Instance.OnGroundPlaneDetected -= HandleGroundPlaneDetected;
                ARTrainingPlacementManager.Instance.OnTrainingOriginPlaced -= OnTrainingOriginPlaced;
            }

            if (MovementTrackingManager.Instance != null)
            {
                MovementTrackingManager.Instance.OnExitReached -= HandleExitReachedByMovement;
                MovementTrackingManager.Instance.OnAssemblyReached -= HandleAssemblyReachedByMovement;
                MovementTrackingManager.Instance.OnTrackingStateChanged -= HandleARTrackingStateChanged;
            }
        }

        private void HandleGroundPlaneDetected()
        {
            if (!IsSetupComplete)
            {
                OnStepChanged?.Invoke(TrainingStepType.IdentifyFire, "Real floor detected! Tap floor to establish training area.", 0.08f);
            }
        }

        private void OnTrainingOriginPlaced(Vector3 originPos)
        {
            groundOriginPosition = originPos;
            IsSetupComplete = true;

            Debug.Log($"[FireLearnController] Anchoring 3D AR prefabs relative to real-world origin: {originPos}");

            // Spatially anchor objects onto real floor
            AnchorObject(fireHazardObject, originPos + new Vector3(0f, 0.4f, 2.5f));
            AnchorObject(alarmButtonObject, originPos + new Vector3(-1.5f, 1.2f, 2.0f));
            AnchorObject(safeExitObject, originPos + new Vector3(1.8f, 1.0f, 3.5f));
            AnchorObject(blockedExitObject, originPos + new Vector3(-1.8f, 1.0f, 3.5f));
            AnchorObject(co2ExtinguisherObject, originPos + new Vector3(-0.6f, 0.25f, 1.5f));
            AnchorObject(waterExtinguisherObject, originPos + new Vector3(0.6f, 0.25f, 1.5f));
            AnchorObject(assemblyZoneObject, originPos + new Vector3(2.5f, 0.05f, 5.0f));

            if (smokeLayerObject != null)
            {
                smokeLayerObject.transform.position = originPos + new Vector3(0f, 2.0f, 3.0f);
            }

            // Set targets for physical movement tracking
            if (MovementTrackingManager.Instance != null && safeExitObject != null && assemblyZoneObject != null)
            {
                MovementTrackingManager.Instance.SetTargets(safeExitObject.transform, assemblyZoneObject.transform, originPos);
            }

            // Start emergency audio alert pulse
            if (AudioHapticManager.Instance != null)
            {
                AudioHapticManager.Instance.StartEmergencyAlert(1);
            }

            SetStep(TrainingStepType.IdentifyFire);
        }

        private void AnchorObject(TrainingObject obj, Vector3 pos)
        {
            if (obj == null) return;
            if (ARTrainingAnchorManager.Instance != null)
            {
                ARTrainingAnchorManager.Instance.AnchorObjectToWorld(obj.gameObject, pos, Quaternion.identity);
            }
            else
            {
                obj.transform.position = pos;
            }
        }

        private void RegisterObjectEvents()
        {
            TrainingObject[] objects = FindObjectsByType<TrainingObject>(FindObjectsSortMode.None);
            foreach (var obj in objects)
            {
                obj.OnObjectSelected += HandleObjectSelected;
                
                switch (obj.ObjectType)
                {
                    case TrainingObjectType.FireHazard: if (fireHazardObject == null) fireHazardObject = obj; break;
                    case TrainingObjectType.AlarmButton: if (alarmButtonObject == null) alarmButtonObject = obj; break;
                    case TrainingObjectType.SafeExit: if (safeExitObject == null) safeExitObject = obj; break;
                    case TrainingObjectType.BlockedExit: if (blockedExitObject == null) blockedExitObject = obj; break;
                    case TrainingObjectType.WaterExtinguisher: if (waterExtinguisherObject == null) waterExtinguisherObject = obj; break;
                    case TrainingObjectType.CO2Extinguisher: if (co2ExtinguisherObject == null) co2ExtinguisherObject = obj; break;
                    case TrainingObjectType.AssemblyZone: if (assemblyZoneObject == null) assemblyZoneObject = obj; break;
                }
            }
        }

        private void UnregisterObjectEvents()
        {
            TrainingObject[] objects = FindObjectsByType<TrainingObject>(FindObjectsSortMode.None);
            foreach (var obj in objects)
            {
                obj.OnObjectSelected -= HandleObjectSelected;
            }
        }

        private void Update()
        {
            if (!IsSetupComplete)
            {
                // Touch tap raycast to set origin
                if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                {
                    if (ARTrainingPlacementManager.Instance != null)
                    {
                        ARTrainingPlacementManager.Instance.TryPlaceTrainingOriginFromTap(Input.GetTouch(0).position, out _, out _);
                    }
                }
                return;
            }

            // Continuous step checks for physical movement
            if (currentStep == TrainingStepType.Evacuate)
            {
                CheckEvacuationProgress();
            }
            else if (currentStep == TrainingStepType.AssemblyPoint)
            {
                CheckAssemblyPointProgress();
            }
        }

        public void SetStep(TrainingStepType nextStep)
        {
            currentStep = nextStep;
            stepMistakes = 0;
            stepRetries = 0;
            stepHints = 0;
            stepExamples = 0;
            stepPractice = 0;
            isIndependentSuccess = true;

            string instructionText = GetStepInstruction(currentStep);
            float progressVal = (float)((int)currentStep) / 8f;

            Debug.Log($"[FireLearnController] Switched to Step: {currentStep} (Progress: {progressVal * 100:F0}%)");

            // Update 3D Highlights
            ClearAllHighlights();
            HighlightTargetForStep(currentStep);

            OnStepChanged?.Invoke(currentStep, instructionText, progressVal);
        }

        private void ClearAllHighlights()
        {
            TrainingObject[] objects = FindObjectsByType<TrainingObject>(FindObjectsSortMode.None);
            foreach (var obj in objects)
            {
                obj.SetHighlight(false);
            }
        }

        private void HighlightTargetForStep(TrainingStepType step)
        {
            TrainingObject target = GetTargetObjectForStep(step);
            if (target != null)
            {
                target.SetHighlight(true, Color.green);
            }
        }

        public TrainingObject GetTargetObjectForStep(TrainingStepType step)
        {
            return step switch
            {
                TrainingStepType.IdentifyFire => fireHazardObject,
                TrainingStepType.RaiseAlarm => alarmButtonObject,
                TrainingStepType.IdentifyExit => safeExitObject,
                TrainingStepType.SelectExtinguisher => co2ExtinguisherObject,
                TrainingStepType.PickupExtinguisher => co2ExtinguisherObject,
                TrainingStepType.UseExtinguisher => fireHazardObject,
                TrainingStepType.Evacuate => safeExitObject,
                TrainingStepType.AssemblyPoint => assemblyZoneObject,
                _ => null
            };
        }

        private void HandleObjectSelected(TrainingObject obj)
        {
            if (!IsSetupComplete) return;

            Debug.Log($"[FireLearnController] Step {currentStep} received selection: {obj.ObjectName} ({obj.ObjectType})");

            switch (currentStep)
            {
                case TrainingStepType.IdentifyFire:
                    if (obj.ObjectType == TrainingObjectType.FireHazard)
                    {
                        AdvanceStep(TrainingStepType.RaiseAlarm);
                    }
                    else
                    {
                        HandleMistake(obj, "Incorrect Object Selected", "Identify the active electrical fire hazard first.");
                    }
                    break;

                case TrainingStepType.RaiseAlarm:
                    if (obj.ObjectType == TrainingObjectType.AlarmButton)
                    {
                        if (AudioHapticManager.Instance != null) AudioHapticManager.Instance.StartEmergencyAlert(2);
                        AdvanceStep(TrainingStepType.IdentifyExit);
                    }
                    else
                    {
                        HandleMistake(obj, "Alarm Not Activated", "Locate and tap the red manual call point alarm button.");
                    }
                    break;

                case TrainingStepType.IdentifyExit:
                    if (obj.ObjectType == TrainingObjectType.SafeExit)
                    {
                        AdvanceStep(TrainingStepType.SelectExtinguisher);
                    }
                    else if (obj.ObjectType == TrainingObjectType.BlockedExit)
                    {
                        HandleMistake(obj, "⚠ DANGEROUS EXIT - SMOKE HAZARD", "Exit B is blocked by toxic carbon monoxide smoke. Select clear Exit A!");
                    }
                    else
                    {
                        HandleMistake(obj, "Invalid Selection", "Select the safe unblocked emergency exit door.");
                    }
                    break;

                case TrainingStepType.SelectExtinguisher:
                    if (obj.ObjectType == TrainingObjectType.CO2Extinguisher)
                    {
                        AdvanceStep(TrainingStepType.PickupExtinguisher);
                    }
                    else if (obj.ObjectType == TrainingObjectType.WaterExtinguisher)
                    {
                        HandleMistake(obj, "⚡ ELECTROCUTION HAZARD!", "NEVER use Water on electrical fires! Water conducts electricity. Select CO2!");
                    }
                    else
                    {
                        HandleMistake(obj, "Wrong Tool", "Choose the CO2 Extinguisher suitable for electrical hazards.");
                    }
                    break;

                case TrainingStepType.PickupExtinguisher:
                    if (obj.ObjectType == TrainingObjectType.CO2Extinguisher)
                    {
                        // Pickup & Attach to AR Camera as HELD
                        isExtinguisherHeld = true;
                        pinPulled = true;

                        if (co2ExtinguisherObject != null && arCameraTransform != null)
                        {
                            if (ARTrainingAnchorManager.Instance != null)
                            {
                                ARTrainingAnchorManager.Instance.RemoveAnchor(co2ExtinguisherObject.gameObject);
                            }
                            co2ExtinguisherObject.transform.parent = arCameraTransform;
                            co2ExtinguisherObject.transform.localPosition = new Vector3(0.25f, -0.3f, 0.6f);
                            co2ExtinguisherObject.transform.localRotation = Quaternion.Euler(0f, -45f, 0f);
                        }

                        Debug.Log("[FireLearnController] PASS: Extinguisher HELD and Pin Pulled!");
                        AdvanceStep(TrainingStepType.UseExtinguisher);
                    }
                    else
                    {
                        HandleMistake(obj, "Extinguisher Sealed", "Tap the CO2 extinguisher handle to pick up and pull the safety pin.");
                    }
                    break;

                case TrainingStepType.UseExtinguisher:
                    if (obj.ObjectType == TrainingObjectType.FireHazard)
                    {
                        ExecutePASSNozzleSpray();
                    }
                    break;
            }
        }

        /// <summary>
        /// PASS Technique: Executed when holding/pressing SPRAY button while aiming phone camera at fire base.
        /// </summary>
        public void ExecutePASSNozzleSpray()
        {
            if (currentStep != TrainingStepType.UseExtinguisher) return;

            if (!pinPulled)
            {
                HandleMistake(co2ExtinguisherObject, "Pin Sealed!", "You must pull the safety pin before spraying!");
                return;
            }

            // Aim direction check
            if (arCameraTransform != null && fireHazardObject != null)
            {
                Vector3 camToFire = (fireHazardObject.transform.position - arCameraTransform.position).normalized;
                float aimDot = Vector3.Dot(arCameraTransform.forward, camToFire);
                if (aimDot < 0.5f)
                {
                    HandleMistake(fireHazardObject, "Aim at Fire Base", "Point your phone camera directly toward the base of the AR Fire!");
                    return;
                }
            }

            fireState = FireState.Suppressing;
            sprayProgress += sprayRate * Time.deltaTime;
            sprayProgress = Mathf.Clamp01(sprayProgress);

            OnSprayProgressUpdated?.Invoke(sprayProgress);

            // Scale down fire hazard visual
            if (fireHazardObject != null)
            {
                float scale = Mathf.Lerp(1.0f, 0.1f, sprayProgress);
                fireHazardObject.transform.localScale = Vector3.one * scale;
            }

            if (sprayProgress >= 1.0f)
            {
                fireState = FireState.Controlled;
                if (fireHazardObject != null)
                {
                    fireHazardObject.gameObject.SetActive(false);
                }

                Debug.Log("[FireLearnController] Fire Extinguished!");

                if (smokeLayerObject != null)
                {
                    smokeLayerObject.SetActive(true);
                }

                AdvanceStep(TrainingStepType.Evacuate);
            }
        }

        private void CheckEvacuationProgress()
        {
            if (arCameraTransform == null) return;

            // Crouch height check
            float currentCamHeight = arCameraTransform.position.y - groundOriginPosition.y;
            if (currentCamHeight > safeCrouchHeight)
            {
                OnFeedbackTriggered?.Invoke(
                    "⚠ SMOKE INHALATION RISK!",
                    "Crouch low below the toxic smoke layer (keep camera below 1.4m) while walking toward the exit!",
                    null,
                    () => RequestHelp(HelpType.Hint)
                );
            }

            if (safeExitObject != null)
            {
                float distToExit = Vector3.Distance(arCameraTransform.position, safeExitObject.transform.position);
                if (distToExit < 1.5f || Input.GetKeyDown(KeyCode.E))
                {
                    AdvanceStep(TrainingStepType.AssemblyPoint);
                }
            }
        }

        private void CheckAssemblyPointProgress()
        {
            if (assemblyZoneObject != null && arCameraTransform != null)
            {
                float distToAssembly = Vector3.Distance(arCameraTransform.position, assemblyZoneObject.transform.position);
                if (distToAssembly < 2.0f || Input.GetKeyDown(KeyCode.A))
                {
                    CompleteTrainingSession();
                }
            }
        }

        private void HandleExitReachedByMovement()
        {
            if (currentStep == TrainingStepType.Evacuate)
            {
                Debug.Log("[FireLearnController] MovementTrackingManager: Physical Exit Reached!");
                AdvanceStep(TrainingStepType.AssemblyPoint);
            }
        }

        private void HandleAssemblyReachedByMovement()
        {
            if (currentStep == TrainingStepType.AssemblyPoint)
            {
                Debug.Log("[FireLearnController] MovementTrackingManager: Physical Assembly Reached!");
                CompleteTrainingSession();
            }
        }

        private void HandleARTrackingStateChanged(bool isTrackingActive)
        {
            if (!isTrackingActive)
            {
                OnFeedbackTriggered?.Invoke(
                    "⚠ AR TRACKING LOST",
                    "Point your camera toward the ground or training area to resume tracking.",
                    null,
                    null
                );
            }
        }

        private void HandleMistake(TrainingObject target, string title, string message)
        {
            stepMistakes++;
            stepRetries++;
            isIndependentSuccess = false;
            currentScore = Mathf.Max(10, currentScore - 10);

            Debug.LogWarning($"[FireLearnController] Mistake made at {currentStep}. Score: {currentScore}. Title: {title}");

            TrainingObjectType objType = target != null ? target.ObjectType : TrainingObjectType.FireHazard;
            if (MistakePatternAnalyzer.Instance != null)
            {
                MistakeCategory cat = MistakePatternAnalyzer.Instance.ClassifyMistake(currentStep, objType);
                MistakePatternAnalyzer.Instance.RegisterMistake(currentStep, cat);
            }

            if (AdaptiveHelpController.Instance != null)
            {
                AdaptiveHelpController.Instance.RegisterMistake(currentStep, target);
            }

            OnFeedbackTriggered?.Invoke(
                title,
                message,
                () => Debug.Log($"[FireLearnController] Retrying step {currentStep}"),
                () => RequestHelp(HelpType.Hint)
            );
        }

        public void RequestHelp(HelpType helpType)
        {
            switch (helpType)
            {
                case HelpType.Hint: stepHints++; break;
                case HelpType.Example: stepExamples++; break;
                case HelpType.Practice: stepPractice++; isIndependentSuccess = false; break;
            }

            TrainingObject target = GetTargetObjectForStep(currentStep);
            if (AdaptiveHelpController.Instance != null)
            {
                AdaptiveHelpController.Instance.RequestHelp(currentStep, helpType, target);
            }
        }

        private void AdvanceStep(TrainingStepType nextStep)
        {
            if (FireTelemetryLogger.Instance != null)
            {
                FireTelemetryLogger.Instance.RecordStepCompletion(
                    currentStep,
                    stepMistakes,
                    stepRetries,
                    stepHints,
                    stepExamples,
                    stepPractice,
                    isIndependentSuccess
                );
            }

            SetStep(nextStep);
        }

        private void CompleteTrainingSession()
        {
            currentStep = TrainingStepType.Complete;
            if (AudioHapticManager.Instance != null)
            {
                AudioHapticManager.Instance.StopEmergencyAlert();
            }

            if (FireTelemetryLogger.Instance != null)
            {
                FireTelemetryLogger.Instance.RecordStepCompletion(
                    TrainingStepType.AssemblyPoint,
                    stepMistakes, stepRetries, stepHints, stepExamples, stepPractice, isIndependentSuccess
                );
                FireTelemetryLogger.Instance.FinalizeSession(currentScore);
            }

            // Analyze session performance via AdaptiveTrainingManager
            if (AdaptiveTrainingManager.Instance != null && FireTelemetryLogger.Instance != null)
            {
                // Retrieve current session data
                SessionTelemetryData sessionData = new SessionTelemetryData
                {
                    overallScore = currentScore,
                    isCompleted = true
                };
                AdaptiveTrainingManager.Instance.AnalyzeSession(sessionData);
            }

            string lastJson = PlayerPrefs.GetString("SurakshaAR_LastSessionJson", "{}");
            OnTrainingCompleted?.Invoke(currentScore, Time.time, lastJson);

            OnFeedbackTriggered?.Invoke(
                "🎉 TRAINING COMPLETE!",
                $"You have successfully mastered Fire & Explosion Response!\n\nFinal Score: {currentScore}/100\nStatus: PASSED",
                null,
                null
            );
        }

        private string GetStepInstruction(TrainingStepType step)
        {
            return step switch
            {
                TrainingStepType.IdentifyFire => "Locate and select the active electrical fire hazard on the floor.",
                TrainingStepType.RaiseAlarm => "Physically move/turn camera to locate and activate red call point.",
                TrainingStepType.IdentifyExit => "Evaluate exits and select clear Emergency Exit A.",
                TrainingStepType.SelectExtinguisher => "Select CO2 Extinguisher suitable for electrical fire.",
                TrainingStepType.PickupExtinguisher => "Tap CO2 Extinguisher to pick up & pull yellow safety pin.",
                TrainingStepType.UseExtinguisher => "Point phone camera at base of fire and HOLD SPRAY button.",
                TrainingStepType.Evacuate => "Crouch low under smoke layer (<1.4m) and physically walk to Exit A.",
                TrainingStepType.AssemblyPoint => "Physically walk outside to green Assembly Point beacon.",
                TrainingStepType.Complete => "Training completed successfully!",
                _ => "Follow safety training instructions."
            };
        }
    }
}
