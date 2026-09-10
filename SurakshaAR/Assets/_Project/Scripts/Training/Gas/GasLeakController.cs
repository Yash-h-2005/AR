using System;
using UnityEngine;
using SurakshaAR.Training.Fire;
using SurakshaAR.AR;
using SurakshaAR.UI.Navigation;
using SurakshaAR.UI.Screens;

namespace SurakshaAR.Training.Gas
{
    public class GasLeakController : MonoBehaviour
    {
        public static GasLeakController Instance { get; private set; }

        [Header("State Machine")]
        [SerializeField] private GasLeakStepType currentStep = GasLeakStepType.RecognizeHazard;
        public GasLeakStepType CurrentStep => currentStep;

        [Header("Simulated Gas Status")]
        [SerializeField] private SimulatedGasLevel currentGasLevel = SimulatedGasLevel.Low;
        public SimulatedGasLevel CurrentGasLevel => currentGasLevel;

        [Header("3D AR Scenario Objects")]
        [SerializeField] private TrainingObject gasHazardObject;
        [SerializeField] private TrainingObject alarmButtonObject;
        [SerializeField] private TrainingObject safeExitObject;
        [SerializeField] private TrainingObject blockedExitObject;
        [SerializeField] private TrainingObject ppeStationObject;
        [SerializeField] private TrainingObject gasDetectorObject;
        [SerializeField] private TrainingObject confinedSpaceObject;
        [SerializeField] private TrainingObject buddyPointObject;
        [SerializeField] private TrainingObject assemblyZoneObject;
        [SerializeField] private Transform arCameraTransform;

        [Header("Scoring & Telemetry Metrics")]
        [SerializeField] private int baseScore = 1000;
        [SerializeField] private int totalMistakes = 0;
        [SerializeField] private int totalRetries = 0;
        [SerializeField] private int sequenceErrors = 0;
        [SerializeField] private int totalStepAttempts = 0;
        [SerializeField] private int independentSuccesses = 0;

        public event Action<GasLeakStepType> OnStepChanged;
        public event Action<SimulatedGasLevel> OnGasLevelChanged;
        public event Action<string> OnInstructionUpdated;
        public event Action<string> OnFeedbackTriggered;

        private MistakePatternAnalyzer mistakeAnalyzer;
        private AdaptiveHelpController helpController;
        private AudioHapticManager audioManager;
        private MovementTrackingManager movementTracker;
        private GasTelemetryLogger telemetryLogger;
        private GasDetectorAR detectorComponent;
        private GasHazard gasHazardComponent;

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
            mistakeAnalyzer = GetComponent<MistakePatternAnalyzer>() ?? FindFirstObjectByType<MistakePatternAnalyzer>();
            helpController = GetComponent<AdaptiveHelpController>() ?? FindFirstObjectByType<AdaptiveHelpController>();
            audioManager = GetComponent<AudioHapticManager>() ?? FindFirstObjectByType<AudioHapticManager>();
            movementTracker = GetComponent<MovementTrackingManager>() ?? FindFirstObjectByType<MovementTrackingManager>();
            telemetryLogger = GetComponent<GasTelemetryLogger>() ?? FindFirstObjectByType<GasTelemetryLogger>();

            if (gasDetectorObject != null)
            {
                detectorComponent = gasDetectorObject.GetComponent<GasDetectorAR>() ?? gasDetectorObject.gameObject.AddComponent<GasDetectorAR>();
            }

            if (gasHazardObject != null)
            {
                gasHazardComponent = gasHazardObject.GetComponent<GasHazard>() ?? gasHazardObject.gameObject.AddComponent<GasHazard>();
            }

            SubscribeObjectEvents();
            SetStep(GasLeakStepType.RecognizeHazard);

            if (telemetryLogger != null)
            {
                telemetryLogger.StartSession("WRK_OFFLINE", "BEGINNER");
            }
        }

        private void Update()
        {
            // Check physical movement during evacuation step
            if (currentStep == GasLeakStepType.AssemblyPoint && assemblyZoneObject != null && arCameraTransform != null)
            {
                float dist = Vector3.Distance(arCameraTransform.position, assemblyZoneObject.transform.position);
                if (dist < 1.8f)
                {
                    CompleteTrainingModule();
                }
            }
        }

        private void SubscribeObjectEvents()
        {
            TrainingObject[] objects = new TrainingObject[]
            {
                gasHazardObject, alarmButtonObject, safeExitObject, blockedExitObject,
                ppeStationObject, gasDetectorObject, confinedSpaceObject, buddyPointObject, assemblyZoneObject
            };

            foreach (var obj in objects)
            {
                if (obj != null)
                {
                    obj.OnObjectSelected += HandleObjectSelected;
                }
            }
        }

        public void HandleObjectSelected(TrainingObject selectedObj)
        {
            if (selectedObj == null) return;
            totalStepAttempts++;

            Debug.Log($"[GasLeakController] Object selected: {selectedObj.ObjectName} on step {currentStep}");

            switch (currentStep)
            {
                case GasLeakStepType.RecognizeHazard:
                    if (selectedObj == gasHazardObject)
                    {
                        RegisterIndependentSuccess("Recognized Gas Hazard");
                        AdvanceStep(GasLeakStepType.RaiseAlarm);
                    }
                    else
                    {
                        RegisterMistake(MistakeCategory.WRONG_HAZARD_IDENTIFICATION, "Selected object is not the gas leak hazard!");
                    }
                    break;

                case GasLeakStepType.RaiseAlarm:
                    if (selectedObj == alarmButtonObject)
                    {
                        RegisterIndependentSuccess("Raised Gas Leak Alarm");
                        AdvanceStep(GasLeakStepType.IdentifySafeExit);
                    }
                    else
                    {
                        RegisterMistake(MistakeCategory.WRONG_ALARM_ACTION, "Must press emergency alarm button to warn work crew!");
                    }
                    break;

                case GasLeakStepType.IdentifySafeExit:
                    if (selectedObj == safeExitObject)
                    {
                        RegisterIndependentSuccess("Identified Upwind Safe Exit");
                        AdvanceStep(GasLeakStepType.SelectPPE);
                    }
                    else if (selectedObj == blockedExitObject)
                    {
                        RegisterMistake(MistakeCategory.WRONG_EXIT_SELECTION, "Selected exit is blocked by gas accumulation! Choose upwind safe exit.");
                    }
                    else
                    {
                        RegisterMistake(MistakeCategory.SEQUENCE_ERROR, "Select safe emergency exit route.");
                    }
                    break;

                case GasLeakStepType.SelectPPE:
                    if (selectedObj == ppeStationObject)
                    {
                        ExecutePPESelectionStep();
                    }
                    else
                    {
                        RegisterMistake(MistakeCategory.SEQUENCE_ERROR, "Select PPE station to equip safety gear.");
                    }
                    break;

                case GasLeakStepType.CheckGasDetector:
                    if (selectedObj == gasDetectorObject)
                    {
                        ExecuteGasDetectorStep();
                    }
                    else
                    {
                        RegisterMistake(MistakeCategory.SEQUENCE_ERROR, "Locate and scan gas detector device.");
                    }
                    break;

                case GasLeakStepType.ConfinedSpaceProtocol:
                    if (selectedObj == confinedSpaceObject)
                    {
                        ExecuteConfinedSpaceStep();
                    }
                    else
                    {
                        RegisterMistake(MistakeCategory.SEQUENCE_ERROR, "Select confined space hazard entry point.");
                    }
                    break;

                case GasLeakStepType.BuddyProcedure:
                    if (selectedObj == buddyPointObject)
                    {
                        ExecuteBuddyProcedureStep();
                    }
                    else
                    {
                        RegisterMistake(MistakeCategory.SEQUENCE_ERROR, "Select buddy system check-in point.");
                    }
                    break;

                case GasLeakStepType.AssemblyPoint:
                    if (selectedObj == assemblyZoneObject)
                    {
                        RegisterIndependentSuccess("Reached Safe Assembly Zone");
                        CompleteTrainingModule();
                    }
                    break;
            }
        }

        public void ExecutePPESelectionStep()
        {
            if (PPESelectionManager.Instance == null)
            {
                AdvanceStep(GasLeakStepType.CheckGasDetector);
                return;
            }

            // Simulate selecting SCBA, Helmet, Goggles, Gloves
            PPESelectionManager.Instance.SelectItem(PPEType.SCBARespirator);
            PPESelectionManager.Instance.SelectItem(PPEType.SafetyHelmet);
            PPESelectionManager.Instance.SelectItem(PPEType.SafetyGoggles);
            PPESelectionManager.Instance.SelectItem(PPEType.ProtectiveGloves);

            if (PPESelectionManager.Instance.ValidatePPESelection(out string feedback))
            {
                RegisterIndependentSuccess("Equipped SCBA & Full PPE");
                OnFeedbackTriggered?.Invoke(feedback);
                AdvanceStep(GasLeakStepType.CheckGasDetector);
            }
            else
            {
                RegisterMistake(MistakeCategory.SEQUENCE_ERROR, feedback);
            }
        }

        public void ExecuteGasDetectorStep()
        {
            if (detectorComponent != null)
            {
                detectorComponent.PerformScan(currentGasLevel, () =>
                {
                    RegisterIndependentSuccess("Scanned Gas Detector");
                    AdvanceStep(GasLeakStepType.ConfinedSpaceProtocol);
                });
            }
            else
            {
                AdvanceStep(GasLeakStepType.ConfinedSpaceProtocol);
            }
        }

        public void ExecuteConfinedSpaceStep()
        {
            bool ppeValid = PPESelectionManager.Instance != null && PPESelectionManager.Instance.ValidatePPESelection(out _);
            bool detectorScanned = detectorComponent == null || detectorComponent.HasScanned;
            bool buddyVerified = true; // Will check in step 7

            if (ConfinedSpaceManager.Instance != null)
            {
                if (ConfinedSpaceManager.Instance.AttemptConfinedSpaceEntry(ppeValid, detectorScanned, buddyVerified, out string msg))
                {
                    RegisterIndependentSuccess("Confined Space Protocol Verified");
                    OnFeedbackTriggered?.Invoke(msg);
                    AdvanceStep(GasLeakStepType.BuddyProcedure);
                }
                else
                {
                    RegisterMistake(MistakeCategory.SEQUENCE_ERROR, msg);
                }
            }
            else
            {
                AdvanceStep(GasLeakStepType.BuddyProcedure);
            }
        }

        public void ExecuteBuddyProcedureStep()
        {
            if (BuddyProcedureManager.Instance != null)
            {
                BuddyProcedureManager.Instance.ConfirmBuddyPresent();
                BuddyProcedureManager.Instance.ConfirmCommChecked();

                if (BuddyProcedureManager.Instance.ConfirmEntryProcedure(out string msg))
                {
                    RegisterIndependentSuccess("Buddy System Verified");
                    OnFeedbackTriggered?.Invoke(msg);
                    UpdateGasLevel(SimulatedGasLevel.Safe);
                    AdvanceStep(GasLeakStepType.AssemblyPoint);
                }
                else
                {
                    RegisterMistake(MistakeCategory.SEQUENCE_ERROR, msg);
                }
            }
            else
            {
                UpdateGasLevel(SimulatedGasLevel.Safe);
                AdvanceStep(GasLeakStepType.AssemblyPoint);
            }
        }

        private void RegisterIndependentSuccess(string actionName)
        {
            independentSuccesses++;
            baseScore += 100;
            Debug.Log($"[GasLeakController] Independent success: {actionName}");
        }

        private void RegisterMistake(MistakeCategory category, string feedback)
        {
            totalMistakes++;
            baseScore = Mathf.Max(0, baseScore - 50);

            if (category == MistakeCategory.SEQUENCE_ERROR) sequenceErrors++;

            OnFeedbackTriggered?.Invoke(feedback);

            if (mistakeAnalyzer != null)
            {
                mistakeAnalyzer.RegisterMistake(TrainingStepType.IdentifyFire, category);
            }

            if (audioManager != null)
            {
                audioManager.StartEmergencyAlert(severityLevel: 1);
            }
        }

        public void AdvanceStep(GasLeakStepType nextStep)
        {
            SetStep(nextStep);
        }

        private void SetStep(GasLeakStepType step)
        {
            currentStep = step;
            OnStepChanged?.Invoke(currentStep);

            string inst = GetStepInstruction(currentStep);
            OnInstructionUpdated?.Invoke(inst);

            UpdateAudioHaptics();
        }

        public void UpdateGasLevel(SimulatedGasLevel level)
        {
            currentGasLevel = level;
            OnGasLevelChanged?.Invoke(currentGasLevel);

            if (gasHazardComponent != null)
            {
                gasHazardComponent.SetGasLevel(currentGasLevel);
            }

            UpdateAudioHaptics();
        }

        private void UpdateAudioHaptics()
        {
            if (audioManager == null) return;

            if (currentGasLevel == SimulatedGasLevel.Safe)
            {
                audioManager.StopEmergencyAlert();
            }
            else
            {
                int severity = (currentGasLevel == SimulatedGasLevel.High || currentGasLevel == SimulatedGasLevel.Critical) ? 1 : 2;
                audioManager.StartEmergencyAlert(severityLevel: severity);
            }
        }

        private string GetStepInstruction(GasLeakStepType step)
        {
            switch (step)
            {
                case GasLeakStepType.RecognizeHazard:
                    return "STEP 1/8: Scan ground and tap the AR Gas Leak Hazard source.";
                case GasLeakStepType.RaiseAlarm:
                    return "STEP 2/8: Activate the emergency call point alarm button.";
                case GasLeakStepType.IdentifySafeExit:
                    return "STEP 3/8: Select the safe unblocked upwind exit route.";
                case GasLeakStepType.SelectPPE:
                    return "STEP 4/8: Tap PPE station to equip SCBA Respirator, Helmet, Goggles & Gloves.";
                case GasLeakStepType.CheckGasDetector:
                    return "STEP 5/8: Locate and scan the multi-gas detector device.";
                case GasLeakStepType.ConfinedSpaceProtocol:
                    return "STEP 6/8: Tap confined space entry point to verify safety permit conditions.";
                case GasLeakStepType.BuddyProcedure:
                    return "STEP 7/8: Confirm stand-by buddy presence & radio communication.";
                case GasLeakStepType.AssemblyPoint:
                    return "STEP 8/8: Walk physically to the outdoor Safe Assembly Zone.";
                default:
                    return "Gas Training Complete.";
            }
        }

        private void CompleteTrainingModule()
        {
            currentStep = GasLeakStepType.Complete;
            UpdateGasLevel(SimulatedGasLevel.Safe);

            float independentAcc = (totalStepAttempts > 0) ? ((float)independentSuccesses / totalStepAttempts) * 100f : 100f;
            float assistedAcc = Mathf.Clamp(independentAcc + 15f, 0f, 100f);

            if (telemetryLogger != null)
            {
                telemetryLogger.EndSession(baseScore, independentAcc, assistedAcc, totalMistakes, totalRetries, sequenceErrors);
            }

            Debug.Log($"[GasLeakController] Module Complete! Final Score: {baseScore}, Acc: {independentAcc:F1}%");

            if (SceneNavigator.Instance != null)
            {
                SceneNavigator.Instance.NavigateTo(ScreenState.Result);
            }
        }
    }
}
