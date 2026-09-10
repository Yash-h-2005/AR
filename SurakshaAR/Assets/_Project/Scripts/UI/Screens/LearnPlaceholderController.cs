using System;
using UnityEngine;
using UnityEngine.UI;
using SurakshaAR.UI.Navigation;
using SurakshaAR.UI.Components;
using SurakshaAR.Training.Fire;

namespace SurakshaAR.UI.Screens
{
    /// <summary>
    /// UI Controller for AR Learn Mode HUD (<15% screen coverage, high contrast, mobile-friendly).
    /// Binds UI HUD overlays to FireLearnController state machine and AdaptiveHelpController.
    /// </summary>
    public class LearnPlaceholderController : MonoBehaviour
    {
        [Header("HUD Top Bar Components")]
        [SerializeField] private Text stepCounterText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Text hazardStatusText;

        [Header("HUD Instruction Banner")]
        [SerializeField] private Text instructionText;

        [Header("PASS Extinguisher Controls")]
        [SerializeField] private Button sprayButton;
        [SerializeField] private GameObject sprayControlsContainer;
        [SerializeField] private Slider sprayProgressBar;

        [Header("Action Buttons")]
        [SerializeField] private Button needHelpButton;
        [SerializeField] private Button backButton;

        [Header("Contextual Feedback Card")]
        [SerializeField] private FeedbackCard feedbackCard;

        [Header("Help Options Modal/Overlay (Optional)")]
        [SerializeField] private GameObject helpModalOverlay;
        [SerializeField] private Button hintOptionButton;
        [SerializeField] private Button exampleOptionButton;
        [SerializeField] private Button practiceOptionButton;
        [SerializeField] private Button closeHelpModalButton;

        private bool isSpraying = false;

        private void Start()
        {
            WireButtons();
            SubscribeToControllerEvents();

            if (helpModalOverlay != null)
            {
                helpModalOverlay.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromControllerEvents();
        }

        private void SubscribeToControllerEvents()
        {
            if (FireLearnController.Instance != null)
            {
                FireLearnController.Instance.OnStepChanged += HandleStepChanged;
                FireLearnController.Instance.OnSprayProgressUpdated += HandleSprayProgressUpdated;
                FireLearnController.Instance.OnFeedbackTriggered += HandleFeedbackTriggered;
                FireLearnController.Instance.OnTrainingCompleted += HandleTrainingCompleted;
            }

            if (AdaptiveHelpController.Instance != null)
            {
                AdaptiveHelpController.Instance.OnHelpTriggered += HandleHelpTriggered;
            }
        }

        private void UnsubscribeFromControllerEvents()
        {
            if (FireLearnController.Instance != null)
            {
                FireLearnController.Instance.OnStepChanged -= HandleStepChanged;
                FireLearnController.Instance.OnSprayProgressUpdated -= HandleSprayProgressUpdated;
                FireLearnController.Instance.OnFeedbackTriggered -= HandleFeedbackTriggered;
                FireLearnController.Instance.OnTrainingCompleted -= HandleTrainingCompleted;
            }

            if (AdaptiveHelpController.Instance != null)
            {
                AdaptiveHelpController.Instance.OnHelpTriggered -= HandleHelpTriggered;
            }
        }

        private void WireButtons()
        {
            if (sprayButton != null)
            {
                // Both click and hold supported
                sprayButton.onClick.AddListener(OnSprayClicked);
            }

            if (needHelpButton != null)
                needHelpButton.onClick.AddListener(OnNeedHelpClicked);

            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);

            if (hintOptionButton != null)
                hintOptionButton.onClick.AddListener(() => SelectHelpOption(HelpType.Hint));

            if (exampleOptionButton != null)
                exampleOptionButton.onClick.AddListener(() => SelectHelpOption(HelpType.Example));

            if (practiceOptionButton != null)
                practiceOptionButton.onClick.AddListener(() => SelectHelpOption(HelpType.Practice));

            if (closeHelpModalButton != null)
                closeHelpModalButton.onClick.AddListener(() => { if (helpModalOverlay != null) helpModalOverlay.SetActive(false); });
        }

        private void Update()
        {
            // Support continuous spray button holding in Unity Editor / Mobile Touch
            if (isSpraying && FireLearnController.Instance != null)
            {
                FireLearnController.Instance.ExecutePASSNozzleSpray();
            }
        }

        private void HandleStepChanged(TrainingStepType step, string instruction, float progress)
        {
            int stepNum = (int)step;
            if (stepCounterText != null) stepCounterText.text = $"Step {stepNum} of 8";
            if (progressBar != null) progressBar.value = progress;
            if (instructionText != null) instructionText.text = instruction;

            // Status label update
            if (hazardStatusText != null)
            {
                hazardStatusText.text = step switch
                {
                    TrainingStepType.IdentifyFire => "🔥 FIRE DETECTED",
                    TrainingStepType.RaiseAlarm => "🚨 ALARM REQUIRED",
                    TrainingStepType.IdentifyExit => "🚪 EVACUATION ROUTE",
                    TrainingStepType.SelectExtinguisher => "🧯 SELECT EXTINGUISHER",
                    TrainingStepType.PickupExtinguisher => "🧯 PULL SAFETY PIN",
                    TrainingStepType.UseExtinguisher => "💨 SPRAY AT FIRE BASE",
                    TrainingStepType.Evacuate => "🌫️ CROUCH & EVACUATE",
                    TrainingStepType.AssemblyPoint => "🚩 ASSEMBLY POINT",
                    TrainingStepType.Complete => "✅ TRAINED & SAFE",
                    _ => "⚠️ AR SAFETY"
                };
            }

            // Show/hide SPRAY controls
            bool isSprayStep = (step == TrainingStepType.UseExtinguisher || step == TrainingStepType.PickupExtinguisher);
            if (sprayControlsContainer != null) sprayControlsContainer.SetActive(isSprayStep);
            if (sprayButton != null) sprayButton.gameObject.SetActive(isSprayStep);
        }

        private void HandleSprayProgressUpdated(float progress)
        {
            if (sprayProgressBar != null)
            {
                sprayProgressBar.value = progress;
            }
        }

        private void HandleFeedbackTriggered(string title, string body, Action onRetry, Action onHelp)
        {
            if (feedbackCard != null)
            {
                feedbackCard.ShowFeedback(title, body, onRetry, onHelp);
            }
        }

        private void HandleHelpTriggered(string tipText, HelpType helpType)
        {
            if (feedbackCard != null)
            {
                feedbackCard.ShowFeedback($"💡 HELP ({helpType})", tipText, null, null);
            }
        }

        private void HandleTrainingCompleted(int score, float durationSec, string telemetryJson)
        {
            if (instructionText != null)
            {
                instructionText.text = $"🎉 Training Complete! Score: {score}/100";
            }
        }

        private void OnSprayClicked()
        {
            Debug.Log("[LearnPlaceholderController] SPRAY Pressed.");
            if (FireLearnController.Instance != null)
            {
                FireLearnController.Instance.ExecutePASSNozzleSpray();
            }
        }

        private void OnNeedHelpClicked()
        {
            Debug.Log("[LearnPlaceholderController] NEED HELP Pressed.");
            if (helpModalOverlay != null)
            {
                helpModalOverlay.SetActive(true);
            }
            else if (FireLearnController.Instance != null)
            {
                // Default to Tier 1 Hint
                FireLearnController.Instance.RequestHelp(HelpType.Hint);
            }
        }

        private void SelectHelpOption(HelpType helpType)
        {
            if (helpModalOverlay != null) helpModalOverlay.SetActive(false);
            if (FireLearnController.Instance != null)
            {
                FireLearnController.Instance.RequestHelp(helpType);
            }
        }

        private void OnBackClicked()
        {
            if (SceneNavigator.Instance != null)
            {
                SceneNavigator.Instance.GoBack();
            }
        }
    }
}
