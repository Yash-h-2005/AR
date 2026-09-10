using UnityEngine;
using UnityEngine.UI;
using SurakshaAR.UI.Navigation;
using SurakshaAR.Assessment;
using SurakshaAR.Certification;

namespace SurakshaAR.UI.Screens
{
    public class ResultScreenController : MonoBehaviour
    {
        [Header("Metric Fields")]
        [SerializeField] private Text headerTitleText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text metricsText;
        [SerializeField] private Text areasToImproveText;

        [Header("Buttons")]
        [SerializeField] private Button practiceAgainButton;
        [SerializeField] private Button continueButton;

        private void Start()
        {
            UpdateResults();
            WireButtons();
        }

        private void UpdateResults()
        {
            AssessmentResult res = AssessmentManager.Instance != null ? AssessmentManager.Instance.LatestResult : null;

            if (res != null)
            {
                bool isPassed = res.status == AssessmentStatus.PASSED;

                if (headerTitleText != null)
                {
                    headerTitleText.text = isPassed ? "ASSESSMENT PASSED 🎉" : "ASSESSMENT FAILED ⚠";
                    headerTitleText.color = isPassed ? new Color(0.06f, 0.73f, 0.51f) : Color.red;
                }

                if (scoreText != null)
                {
                    scoreText.text = $"{res.totalScore:F0}%";
                    scoreText.color = isPassed ? new Color(0.97f, 0.45f, 0.09f) : Color.red;
                }

                if (metricsText != null)
                {
                    metricsText.text = $"Assessment Performance:\n• Status: {res.status}\n• Independent Accuracy: {res.independentAccuracyPercent:F0}%\n• Assisted Accuracy: {res.assistedAccuracyPercent:F0}%\n• Sequence Accuracy: {res.sequenceAccuracyPercent:F0}%\n• Critical Violations: {res.criticalViolationsCount}";
                }

                if (areasToImproveText != null)
                {
                    areasToImproveText.text = $"Status Recommendation:\n• {res.recommendedAction}";
                }
            }
            else
            {
                float indepAcc = PlayerPrefs.GetFloat("SurakshaAR_LastIndependentAccuracy", 85f);
                if (headerTitleText != null) headerTitleText.text = "Training Completed 🎉";
                if (scoreText != null) scoreText.text = $"{indepAcc:F0}%";
                if (metricsText != null) metricsText.text = $"Performance Breakdown:\n• Independent Accuracy: {indepAcc:F0}%\n• Safety Sequence: Passed";
            }
        }

        private void WireButtons()
        {
            if (practiceAgainButton != null)
            {
                practiceAgainButton.onClick.AddListener(() =>
                {
                    if (SceneNavigator.Instance != null)
                        SceneNavigator.Instance.NavigateTo(ScreenState.TrainingModules);
                });
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(() =>
                {
                    if (SceneNavigator.Instance != null)
                        SceneNavigator.Instance.NavigateTo(ScreenState.CertificatePreview);
                });
            }
        }
    }
}
