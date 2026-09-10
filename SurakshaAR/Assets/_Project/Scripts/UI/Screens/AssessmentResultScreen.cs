using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using SurakshaAR.UI.Navigation;
using SurakshaAR.UI.Data;
using SurakshaAR.UI.Screens;

namespace SurakshaAR.Training.Fire
{
    /// <summary>
    /// Full-screen result dialog for Module 1 Final Assessment.
    /// Displays dynamically calculated score, breakdown for both fire response and evacuation,
    /// and gatekeeps official module completion.
    /// If Passed (>= 70%): "NEXT & CONFIRM" officially certifies Module 1, marks Card 4 as PASSED, and updates overall progress to 33%.
    /// If Failed (< 70% or caught in collapse): "RETRY ASSESSMENT" restarts the simulation without certifying.
    /// </summary>
    public class AssessmentResultScreen : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject overlayPanel;
        [SerializeField] private Text headerBadgeText;
        [SerializeField] private Text statusTitleText;
        [SerializeField] private Text scorePercentText;
        [SerializeField] private Text scoreSubtitleText;
        [SerializeField] private Text breakdownFireText;
        [SerializeField] private Text breakdownRouteText;
        [SerializeField] private Text breakdownEvacText;
        [SerializeField] private Text feedbackSummaryText;
        [SerializeField] private Button actionButton;
        [SerializeField] private Text actionButtonText;

        // ── State ─────────────────────────────────────────────────────────────
        private bool isPassed = false;
        private float finalScore = 0f;
        private bool hasConfirmed = false;

        private void Start()
        {
            if (overlayPanel != null)
                overlayPanel.SetActive(false);

            if (actionButton != null)
                actionButton.onClick.AddListener(OnActionButtonClicked);
        }

        /// <summary>
        /// Displays the assessment outcome with dynamic score breakdown.
        /// </summary>
        public void ShowAssessmentResult(float score, float fireScore, float routeScore, float evacScore, bool passed, string customFeedback = null)
        {
            if (hasConfirmed) return;

            finalScore = Mathf.Clamp(score, 0f, 100f);
            isPassed = passed;

            if (overlayPanel != null)
                overlayPanel.SetActive(true);

            if (headerBadgeText != null)
                headerBadgeText.text = "MODULE 1: FIRE & EXPLOSION RESPONSE";

            if (scorePercentText != null)
                scorePercentText.text = $"{Mathf.RoundToInt(finalScore)}%";

            if (scoreSubtitleText != null)
                scoreSubtitleText.text = isPassed ? "Assessment Passed (Requirement: ≥70%)" : "Requirement Not Met (Passing Score: ≥70%)";

            if (statusTitleText != null)
            {
                if (isPassed)
                {
                    statusTitleText.text = "✅ ASSESSMENT PASSED";
                    statusTitleText.color = new Color(0.15f, 0.95f, 0.45f);
                }
                else
                {
                    statusTitleText.text = "⚠️ ASSESSMENT NOT PASSED";
                    statusTitleText.color = new Color(1.0f, 0.32f, 0.25f);
                }
            }

            if (breakdownFireText != null)
                breakdownFireText.text = $"• Electric Fire Response:  {Mathf.RoundToInt(fireScore)} / 40 pts";

            if (breakdownRouteText != null)
                breakdownRouteText.text = $"• Route B Navigation:      {Mathf.RoundToInt(routeScore)} / 30 pts";

            if (breakdownEvacText != null)
                breakdownEvacText.text = $"• Evacuation & Survival:   {Mathf.RoundToInt(evacScore)} / 30 pts";

            if (feedbackSummaryText != null)
            {
                if (!string.IsNullOrEmpty(customFeedback))
                {
                    feedbackSummaryText.text = customFeedback;
                }
                else if (isPassed)
                {
                    feedbackSummaryText.text = "Outstanding performance! You extinguished the electrical fire correctly and evacuated swiftly along Route B to the safe assembly point.";
                }
                else
                {
                    feedbackSummaryText.text = "Evacuation safety criteria were not fully met. Follow green emergency signage into Route B and avoid entering blocked haulage drifts.";
                }
            }

            if (actionButtonText != null)
            {
                actionButtonText.text = isPassed ? "NEXT & CONFIRM" : "RETRY ASSESSMENT";
            }

            if (actionButton != null)
            {
                var colors = actionButton.colors;
                colors.normalColor = isPassed ? new Color(0.12f, 0.75f, 0.38f) : new Color(0.92f, 0.42f, 0.15f);
                actionButton.colors = colors;
            }

            Debug.Log($"[AssessmentResultScreen] Rendered result: Passed={isPassed}, Score={finalScore:F1}% (Fire={fireScore:F1}, Route={routeScore:F1}, Evac={evacScore:F1})");
        }

        private void OnActionButtonClicked()
        {
            if (hasConfirmed) return;

            if (isPassed)
            {
                hasConfirmed = true;
                Debug.Log($"[AssessmentResultScreen] Worker confirmed assessment pass. Score: {finalScore:F0}%");

                if (UserSession.Instance != null)
                {
                    UserSession.Instance.RecordAssessmentResult("M1", true, finalScore);
                }

                TrainingModulesController.TargetModuleToOpen = "M1";

                if (SceneNavigator.Instance != null)
                {
                    SceneNavigator.Instance.NavigateTo(ScreenState.TrainingModules);
                }
                else
                {
                    SceneManager.LoadScene("TrainingModules");
                }
            }
            else
            {
                Debug.Log("[AssessmentResultScreen] Worker tapped Retry Assessment — reloading simulation.");
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        public void SetReferences(
            GameObject panel,
            Text headerBadge,
            Text statusTitle,
            Text scorePercent,
            Text scoreSubtitle,
            Text breakdownFire,
            Text breakdownRoute,
            Text breakdownEvac,
            Text feedbackSummary,
            Button btn,
            Text btnText)
        {
            overlayPanel = panel;
            headerBadgeText = headerBadge;
            statusTitleText = statusTitle;
            scorePercentText = scorePercent;
            scoreSubtitleText = scoreSubtitle;
            breakdownFireText = breakdownFire;
            breakdownRouteText = breakdownRoute;
            breakdownEvacText = breakdownEvac;
            feedbackSummaryText = feedbackSummary;
            actionButton = btn;
            actionButtonText = btnText;

            if (overlayPanel != null)
                overlayPanel.SetActive(false);
        }
    }
}
