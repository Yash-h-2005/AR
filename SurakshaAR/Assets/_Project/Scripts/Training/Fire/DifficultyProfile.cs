using System;
using UnityEngine;

namespace SurakshaAR.Training.Fire
{
    public enum DifficultyLevel
    {
        BEGINNER = 1,
        INTERMEDIATE = 2,
        ADVANCED = 3
    }

    /// <summary>
    /// Configuration profile for scenario complexity parameters.
    /// Alters decision complexity and guidance levels without modifying safety rules.
    /// </summary>
    [Serializable]
    public class DifficultyProfile
    {
        public DifficultyLevel level = DifficultyLevel.BEGINNER;
        public int extinguisherChoicesCount = 1;
        public int exitChoicesCount = 1;
        public int distractorCount = 0;
        public float clueVisibilityRatio = 1.0f; // 1.0 = Full guidance, 0.3 = Minimal indicators
        public float timePressureLimitSec = 0f;  // 0 = No time limit
        public float crouchStrictnessHeight = 1.4f;

        public static DifficultyProfile GetProfileForLevel(DifficultyLevel lvl)
        {
            return lvl switch
            {
                DifficultyLevel.BEGINNER => new DifficultyProfile
                {
                    level = DifficultyLevel.BEGINNER,
                    extinguisherChoicesCount = 1,
                    exitChoicesCount = 1,
                    distractorCount = 0,
                    clueVisibilityRatio = 1.0f,
                    timePressureLimitSec = 0f,
                    crouchStrictnessHeight = 1.4f
                },
                DifficultyLevel.INTERMEDIATE => new DifficultyProfile
                {
                    level = DifficultyLevel.INTERMEDIATE,
                    extinguisherChoicesCount = 2,
                    exitChoicesCount = 2,
                    distractorCount = 2,
                    clueVisibilityRatio = 0.7f,
                    timePressureLimitSec = 120f,
                    crouchStrictnessHeight = 1.35f
                },
                DifficultyLevel.ADVANCED => new DifficultyProfile
                {
                    level = DifficultyLevel.ADVANCED,
                    extinguisherChoicesCount = 2,
                    exitChoicesCount = 2,
                    distractorCount = 4,
                    clueVisibilityRatio = 0.3f,
                    timePressureLimitSec = 60f,
                    crouchStrictnessHeight = 1.30f
                },
                _ => GetProfileForLevel(DifficultyLevel.BEGINNER)
            };
        }
    }
}
