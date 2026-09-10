using UnityEngine;
using UnityEngine.UI;
using SurakshaAR.UI.Navigation;
using SurakshaAR.Training.Gas;
using SurakshaAR.UI.Components;

namespace SurakshaAR.UI.Screens
{
    public class GasLearnPlaceholderController : MonoBehaviour
    {
        [Header("Top HUD Overlay (<15% screen space)")]
        [SerializeField] private Text stepCounterText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Text gasStatusText;
        [SerializeField] private Text instructionText;

        [Header("Bottom Bar Actions (<15% screen space)")]
        [SerializeField] private Button scanActionButton;
        [SerializeField] private Button needHelpButton;
        [SerializeField] private Button backButton;

        [Header("Contextual Feedback Overlay")]
        [SerializeField] private FeedbackCard feedbackCard;

        private GasLeakController gasController;

        private void Start()
        {
            gasController = GasLeakController.Instance ?? FindFirstObjectByType<GasLeakController>();

            if (gasController != null)
            {
                gasController.OnStepChanged += HandleStepChanged;
                gasController.OnGasLevelChanged += HandleGasLevelChanged;
                gasController.OnInstructionUpdated += HandleInstructionUpdated;
                gasController.OnFeedbackTriggered += HandleFeedbackTriggered;
            }

            if (scanActionButton != null)
            {
                scanActionButton.onClick.AddListener(OnScanActionClicked);
            }

            if (needHelpButton != null)
            {
                needHelpButton.onClick.AddListener(OnNeedHelpClicked);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
            }

            if (feedbackCard != null)
            {
                feedbackCard.gameObject.SetActive(false);
            }

            UpdateUIState();
        }

        private void OnDestroy()
        {
            if (gasController != null)
            {
                gasController.OnStepChanged -= HandleStepChanged;
                gasController.OnGasLevelChanged -= HandleGasLevelChanged;
                gasController.OnInstructionUpdated -= HandleInstructionUpdated;
                gasController.OnFeedbackTriggered -= HandleFeedbackTriggered;
            }
        }

        private void HandleStepChanged(GasLeakStepType newStep)
        {
            int stepNum = (int)newStep;
            if (stepCounterText != null)
            {
                stepCounterText.text = $"Step {stepNum} of 8";
            }

            if (progressBar != null)
            {
                progressBar.value = (float)stepNum / 8f;
            }
        }

        private void HandleGasLevelChanged(SimulatedGasLevel newLevel)
        {
            if (gasStatusText != null)
            {
                gasStatusText.text = $"SIMULATED GAS: {newLevel.ToString().ToUpper()}";
                gasStatusText.color = (newLevel == SimulatedGasLevel.Safe) ? Color.green :
                                      (newLevel == SimulatedGasLevel.Low) ? new Color(0.95f, 0.6f, 0.05f) : Color.red;
            }
        }

        private void HandleInstructionUpdated(string text)
        {
            if (instructionText != null)
            {
                instructionText.text = text;
            }
        }

        private void HandleFeedbackTriggered(string feedbackMsg)
        {
            if (feedbackCard != null)
            {
                feedbackCard.ShowFeedback("⚠ SAFETY NOTICE", feedbackMsg, () => {
                    feedbackCard.Hide();
                });
            }
        }

        private void OnScanActionClicked()
        {
            if (gasController != null)
            {
                // Trigger context action based on step
                switch (gasController.CurrentStep)
                {
                    case GasLeakStepType.SelectPPE:
                        gasController.ExecutePPESelectionStep();
                        break;
                    case GasLeakStepType.CheckGasDetector:
                        gasController.ExecuteGasDetectorStep();
                        break;
                    case GasLeakStepType.ConfinedSpaceProtocol:
                        gasController.ExecuteConfinedSpaceStep();
                        break;
                    case GasLeakStepType.BuddyProcedure:
                        gasController.ExecuteBuddyProcedureStep();
                        break;
                    default:
                        Debug.Log("[GasLearnPlaceholderController] Tap spatial AR object in camera view to proceed.");
                        break;
                }
            }
        }

        private void OnNeedHelpClicked()
        {
            if (feedbackCard != null)
            {
                feedbackCard.ShowFeedback("💡 ADAPTIVE GUIDE", "Follow directional hints to complete safety step.", () => {
                    feedbackCard.Hide();
                });
            }
        }

        private void OnBackClicked()
        {
            if (SceneNavigator.Instance != null)
            {
                SceneNavigator.Instance.GoBack();
            }
        }

        private void UpdateUIState()
        {
            if (gasController != null)
            {
                HandleStepChanged(gasController.CurrentStep);
                HandleGasLevelChanged(gasController.CurrentGasLevel);
            }
        }
    }
}
