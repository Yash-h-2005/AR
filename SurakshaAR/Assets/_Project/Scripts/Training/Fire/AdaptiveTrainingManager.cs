using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurakshaAR.Training.Fire
{
    public enum SkillMasteryState
    {
        MASTERED,
        GOOD,
        NEEDS_PRACTICE,
        PRACTICE_RECOMMENDED
    }

    [Serializable]
    public class SkillCategoryProgress
    {
        public string categoryName;
        public SkillMasteryState masteryState;
        public int attempts;
        public int mistakes;
        public int unassistedSuccesses;
    }

    /// <summary>
    /// Central offline engine for AI-Adaptive Training.
    /// Tracks worker skill mastery, calculates independent vs assisted accuracy,
    /// and generates personalized offline training recommendations.
    /// </summary>
    public class AdaptiveTrainingManager : MonoBehaviour
    {
        public static AdaptiveTrainingManager Instance { get; private set; }

        public SkillCategoryProgress ExtinguisherSelectionSkill { get; private set; } = new SkillCategoryProgress { categoryName = "Extinguisher Selection", masteryState = SkillMasteryState.GOOD };
        public SkillCategoryProgress EvacuationSkill { get; private set; } = new SkillCategoryProgress { categoryName = "Evacuation Route", masteryState = SkillMasteryState.GOOD };
        public SkillCategoryProgress AlarmSkill { get; private set; } = new SkillCategoryProgress { categoryName = "Alarm Response", masteryState = SkillMasteryState.MASTERED };

        public event Action<SessionTelemetryData> OnSessionAnalyzed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadSkillProgress();
        }

        public void LoadSkillProgress()
        {
            string json = PlayerPrefs.GetString("SurakshaAR_WorkerSkillProfile", "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    Debug.Log("[AdaptiveTrainingManager] Loaded local worker skill profile.");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AdaptiveTrainingManager] Error loading skill profile: {ex.Message}");
                }
            }
        }

        public void AnalyzeSession(SessionTelemetryData session)
        {
            if (session == null || session.steps == null) return;

            int totalSteps = session.steps.Count;
            int unassistedCount = 0;
            int totalMistakes = 0;
            int totalHints = 0;

            string weakCategory = "";
            int maxStepMistakes = 0;

            foreach (var step in session.steps)
            {
                if (step.independentSuccess) unassistedCount++;
                totalMistakes += step.mistakes;
                totalHints += step.hintsUsed + step.examplesUsed + step.practiceUsed;

                if (step.mistakes > maxStepMistakes)
                {
                    maxStepMistakes = step.mistakes;
                    weakCategory = step.stepName;
                }
            }

            float independentAccuracy = totalSteps > 0 ? ((float)unassistedCount / totalSteps) * 100f : 0f;
            float assistedAccuracy = totalSteps > 0 ? ((float)(totalSteps - unassistedCount) / totalSteps) * 100f : 0f;

            Debug.Log($"[AdaptiveTrainingManager] Session Analyzed: Score={session.overallScore}, Independent={independentAccuracy:F0}%, Assisted={assistedAccuracy:F0}%");

            // Evaluate adaptive difficulty transition
            if (AdaptiveScenarioManager.Instance != null)
            {
                AdaptiveScenarioManager.Instance.EvaluateAndAdaptDifficulty(session.overallScore, unassistedCount, totalMistakes);
            }

            // Generate Adaptive Recommendation
            string recommendation = string.IsNullOrEmpty(weakCategory)
                ? "Excellent performance! Ready for Advanced AR Assessment Mode."
                : $"Recommended Practice: Re-run {weakCategory} in Learn Mode to achieve independent mastery.";

            PlayerPrefs.SetString("SurakshaAR_LastRecommendation", recommendation);
            PlayerPrefs.SetFloat("SurakshaAR_LastIndependentAccuracy", independentAccuracy);
            PlayerPrefs.SetFloat("SurakshaAR_LastAssistedAccuracy", assistedAccuracy);
            PlayerPrefs.Save();

            OnSessionAnalyzed?.Invoke(session);
        }
    }
}
