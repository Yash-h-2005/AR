using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using SurakshaAR.Certification;

namespace SurakshaAR.Assessment
{
    public class AssessmentManager : MonoBehaviour
    {
        public static AssessmentManager Instance { get; private set; }

        public AssessmentCriteria Criteria { get; private set; } = AssessmentCriteria.Default;
        public AssessmentResult LatestResult { get; private set; }

        private string assessmentDataDirectory;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            assessmentDataDirectory = Path.Combine(Application.persistentDataPath, "Assessments");
            if (!Directory.Exists(assessmentDataDirectory))
            {
                Directory.CreateDirectory(assessmentDataDirectory);
            }
        }

        public AssessmentResult EvaluateModulePerformance(
            string workerId,
            string workerName,
            string moduleName,
            int rawBaseScore,
            float independentAccPercent,
            float assistedAccPercent,
            int mistakes,
            int retries,
            int sequenceErrors,
            int criticalViolations,
            float durationSec,
            List<string> mistakeLog = null)
        {
            float correctActionsScore = Mathf.Clamp(40f - (mistakes * Criteria.incorrectActionPenalty), 0f, 40f);
            
            float seqAccPercent = Mathf.Clamp(100f - (sequenceErrors * 15f), 0f, 100f);
            float sequenceScore = (seqAccPercent / 100f) * Criteria.sequenceAccuracyWeight;

            float independentScore = (independentAccPercent / 100f) * Criteria.independentPerformanceWeight;

            float responseScore = (durationSec < 180f) ? 10f : Mathf.Max(3f, 10f - ((durationSec - 180f) / 30f));
            float hazardScore = (mistakeLog != null && mistakeLog.Contains("WRONG_HAZARD_IDENTIFICATION")) ? 3f : 10f;

            float totalCalculatedScore = Mathf.Clamp(
                correctActionsScore + sequenceScore + independentScore + responseScore + hazardScore,
                0f, 100f
            );

            bool meetsScoreThreshold = totalCalculatedScore >= Criteria.minimumPassingScore;
            bool meetsSequenceThreshold = seqAccPercent >= Criteria.minimumSequenceAccuracyPercent;
            bool zeroCriticalViolations = criticalViolations <= Criteria.maximumCriticalViolationsAllowed;

            bool isPassed = meetsScoreThreshold && meetsSequenceThreshold && zeroCriticalViolations;

            AssessmentResult result = new AssessmentResult
            {
                assessmentId = "ASM-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                workerId = string.IsNullOrEmpty(workerId) ? "WRK_OFFLINE" : workerId,
                workerName = string.IsNullOrEmpty(workerName) ? "Industrial Worker" : workerName,
                moduleName = moduleName,
                timestamp = DateTime.UtcNow.ToString("o"),
                totalScore = Mathf.Round(totalCalculatedScore * 10f) / 10f,
                status = isPassed ? AssessmentStatus.PASSED : AssessmentStatus.FAILED,
                isCertified = isPassed,
                correctActionsScore = correctActionsScore,
                sequenceAccuracyScore = sequenceScore,
                independentPerformanceScore = independentScore,
                responseTimeScore = responseScore,
                hazardRecognitionScore = hazardScore,
                independentAccuracyPercent = independentAccPercent,
                assistedAccuracyPercent = assistedAccPercent,
                sequenceAccuracyPercent = seqAccPercent,
                totalMistakes = mistakes,
                totalRetries = retries,
                sequenceErrors = sequenceErrors,
                criticalViolationsCount = criticalViolations,
                durationSeconds = durationSec,
                mistakeCategories = mistakeLog ?? new List<string>()
            };

            // Derive skills to improve and recommended action
            if (seqAccPercent < 80f) result.skillsToImprove.Add("Safety Sequence Order");
            if (independentAccPercent < 80f) result.skillsToImprove.Add("Independent Decision Making");
            if (mistakes > 2) result.skillsToImprove.Add("Hazard & Gear Selection");

            if (isPassed)
            {
                result.recommendedAction = "Passed! Eligible for Digital Safety Certificate.";
                if (CertificateManager.Instance != null)
                {
                    CertificateManager.Instance.GenerateCertificate(result);
                }
            }
            else
            {
                result.recommendedAction = criticalViolations > 0
                    ? "Failed due to Critical Safety Violation. Complete guided practice and retry assessment."
                    : "Score below passing threshold (70%). Practice recommended steps before retrying.";
            }

            LatestResult = result;
            SaveResultOffline(result);

            Debug.Log($"[AssessmentManager] Evaluated {moduleName}: Score={result.totalScore}, Status={result.status}, Certified={result.isCertified}");
            return result;
        }

        private void SaveResultOffline(AssessmentResult result)
        {
            try
            {
                string json = JsonUtility.ToJson(result, true);
                string path = Path.Combine(assessmentDataDirectory, $"{result.assessmentId}.json");
                File.WriteAllText(path, json);
                Debug.Log($"[AssessmentManager] Assessment result saved offline: {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssessmentManager] Failed to save assessment offline: {ex.Message}");
            }
        }
    }
}
