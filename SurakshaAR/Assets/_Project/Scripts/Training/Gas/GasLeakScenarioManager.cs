using System;
using System.Collections.Generic;
using UnityEngine;
using SurakshaAR.Training.Fire;

namespace SurakshaAR.Training.Gas
{
    [Serializable]
    public class GasScenarioConfig
    {
        public DifficultyLevel difficultyLevel;
        public SimulatedGasLevel initialGasLevel;
        public string gasTypeName;
        public bool requireSCBARespirator;
        public bool requireSafetyHelmet;
        public bool requireSafetyGoggles;
        public bool requireProtectiveGloves;
        public int distractorCount;
        public float detectorDistanceMeters;
        public float gasSpreadRate;
        public string description;
    }

    public class GasLeakScenarioManager : MonoBehaviour
    {
        public static GasLeakScenarioManager Instance { get; private set; }

        public DifficultyLevel ActiveDifficulty { get; private set; } = DifficultyLevel.BEGINNER;
        public GasScenarioConfig CurrentConfig { get; private set; }

        private readonly Dictionary<DifficultyLevel, GasScenarioConfig> approvedScenarios = new Dictionary<DifficultyLevel, GasScenarioConfig>()
        {
            {
                DifficultyLevel.BEGINNER,
                new GasScenarioConfig
                {
                    difficultyLevel = DifficultyLevel.BEGINNER,
                    initialGasLevel = SimulatedGasLevel.Low,
                    gasTypeName = "Toxic Methane (CH4) Leak",
                    requireSCBARespirator = true,
                    requireSafetyHelmet = true,
                    requireSafetyGoggles = true,
                    requireProtectiveGloves = true,
                    distractorCount = 1,
                    detectorDistanceMeters = 2.0f,
                    gasSpreadRate = 0.05f,
                    description = "Single low-concentration gas leak source with high visual guidance and basic PPE choices."
                }
            },
            {
                DifficultyLevel.INTERMEDIATE,
                new GasScenarioConfig
                {
                    difficultyLevel = DifficultyLevel.INTERMEDIATE,
                    initialGasLevel = SimulatedGasLevel.Medium,
                    gasTypeName = "Hydrogen Sulfide (H2S) Leak",
                    requireSCBARespirator = true,
                    requireSafetyHelmet = true,
                    requireSafetyGoggles = true,
                    requireProtectiveGloves = true,
                    distractorCount = 2,
                    detectorDistanceMeters = 3.5f,
                    gasSpreadRate = 0.12f,
                    description = "Medium gas leak with multiple distractor items and increased detector search distance."
                }
            },
            {
                DifficultyLevel.ADVANCED,
                new GasScenarioConfig
                {
                    difficultyLevel = DifficultyLevel.ADVANCED,
                    initialGasLevel = SimulatedGasLevel.High,
                    gasTypeName = "Confined Space H2S & Carbon Monoxide Multi-Leak",
                    requireSCBARespirator = true,
                    requireSafetyHelmet = true,
                    requireSafetyGoggles = true,
                    requireProtectiveGloves = true,
                    distractorCount = 3,
                    detectorDistanceMeters = 5.0f,
                    gasSpreadRate = 0.25f,
                    description = "High concentration multi-hazard gas leak with sequence pressure and reduced visual guidance."
                }
            }
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            SetDifficulty(DifficultyLevel.BEGINNER);
        }

        public void SetDifficulty(DifficultyLevel lvl)
        {
            ActiveDifficulty = lvl;
            if (approvedScenarios.TryGetValue(lvl, out var config))
            {
                CurrentConfig = config;
                Debug.Log($"[GasLeakScenarioManager] Activated scenario profile: {lvl} ({config.gasTypeName})");
            }
        }
    }
}
