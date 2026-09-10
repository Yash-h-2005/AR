using System;
using UnityEngine;

namespace SurakshaAR.Training.Fire
{
    /// <summary>
    /// Selects pre-approved, safety-validated AR scenario configurations.
    /// Dynamically enables or disables visual distractors, alternative extinguisher choices,
    /// and exit options based on current DifficultyProfile without altering safety rules.
    /// </summary>
    public class AdaptiveScenarioManager : MonoBehaviour
    {
        public static AdaptiveScenarioManager Instance { get; private set; }

        [Header("Active Difficulty Profile")]
        [SerializeField] private DifficultyLevel currentLevel = DifficultyLevel.BEGINNER;
        public DifficultyLevel CurrentLevel => currentLevel;

        public DifficultyProfile ActiveProfile { get; private set; }

        public event Action<DifficultyProfile> OnDifficultyProfileChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Load saved difficulty level from PlayerPrefs if available
            int savedLvl = PlayerPrefs.GetInt("SurakshaAR_WorkerDifficultyLevel", (int)DifficultyLevel.BEGINNER);
            currentLevel = (DifficultyLevel)Mathf.Clamp(savedLvl, 1, 3);
            ActiveProfile = DifficultyProfile.GetProfileForLevel(currentLevel);
        }

        public void SetDifficultyLevel(DifficultyLevel newLevel)
        {
            currentLevel = newLevel;
            ActiveProfile = DifficultyProfile.GetProfileForLevel(currentLevel);
            PlayerPrefs.SetInt("SurakshaAR_WorkerDifficultyLevel", (int)currentLevel);
            PlayerPrefs.Save();

            Debug.Log($"[AdaptiveScenarioManager] Difficulty Profile Set to: {currentLevel}");
            OnDifficultyProfileChanged?.Invoke(ActiveProfile);
        }

        public void EvaluateAndAdaptDifficulty(int sessionScore, int unassistedSuccessCount, int mistakeCount)
        {
            if (sessionScore >= 90 && unassistedSuccessCount >= 7 && currentLevel < DifficultyLevel.ADVANCED)
            {
                // Upgrade difficulty level for high performance
                SetDifficultyLevel(currentLevel + 1);
            }
            else if (sessionScore < 60 && mistakeCount >= 3 && currentLevel > DifficultyLevel.BEGINNER)
            {
                // Downgrade difficulty level for extra support
                SetDifficultyLevel(currentLevel - 1);
            }
        }
    }
}
