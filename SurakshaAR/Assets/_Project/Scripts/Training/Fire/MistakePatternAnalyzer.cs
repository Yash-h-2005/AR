using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurakshaAR.Training.Fire
{
    /// <summary>
    /// Deterministic mistake classification categories for SurakshaAR.
    /// Safety sequence rules are 100% hardcoded; this classifier evaluates action correctness.
    /// </summary>
    public enum MistakeCategory
    {
        WRONG_HAZARD_IDENTIFICATION,
        WRONG_ALARM_ACTION,
        WRONG_EXIT_SELECTION,
        WRONG_EXTINGUISHER_SELECTION,
        WRONG_PICKUP_SEQUENCE,
        WRONG_EXTINGUISHER_TECHNIQUE,
        WRONG_EVACUATION_ROUTE,
        SLOW_RESPONSE,
        SEQUENCE_ERROR,
        REPEATED_ACTION_ERROR
    }

    /// <summary>
    /// Analyzes mistake frequency and repetition patterns per step.
    /// Drives multi-tiered adaptive hint escalation (Tip -> Highlight -> Arrow -> Guided Practice)
    /// without modifying underlying safety protocols.
    /// </summary>
    public class MistakePatternAnalyzer : MonoBehaviour
    {
        public static MistakePatternAnalyzer Instance { get; private set; }

        private readonly Dictionary<TrainingStepType, int> stepMistakeCounts = new Dictionary<TrainingStepType, int>();
        private readonly Dictionary<MistakeCategory, int> categoryMistakeCounts = new Dictionary<MistakeCategory, int>();

        public event Action<TrainingStepType, MistakeCategory, int> OnMistakePatternDetected; // step, category, repeatCount

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void ResetAnalyzer()
        {
            stepMistakeCounts.Clear();
            categoryMistakeCounts.Clear();
        }

        public MistakeCategory ClassifyMistake(TrainingStepType step, TrainingObjectType selectedObject)
        {
            return step switch
            {
                TrainingStepType.IdentifyFire => MistakeCategory.WRONG_HAZARD_IDENTIFICATION,
                TrainingStepType.RaiseAlarm => MistakeCategory.WRONG_ALARM_ACTION,
                TrainingStepType.IdentifyExit => MistakeCategory.WRONG_EXIT_SELECTION,
                TrainingStepType.SelectExtinguisher => (selectedObject == TrainingObjectType.WaterExtinguisher) 
                    ? MistakeCategory.WRONG_EXTINGUISHER_SELECTION 
                    : MistakeCategory.SEQUENCE_ERROR,
                TrainingStepType.PickupExtinguisher => MistakeCategory.WRONG_PICKUP_SEQUENCE,
                TrainingStepType.UseExtinguisher => MistakeCategory.WRONG_EXTINGUISHER_TECHNIQUE,
                TrainingStepType.Evacuate => MistakeCategory.WRONG_EVACUATION_ROUTE,
                TrainingStepType.AssemblyPoint => MistakeCategory.SLOW_RESPONSE,
                _ => MistakeCategory.REPEATED_ACTION_ERROR
            };
        }

        public int RegisterMistake(TrainingStepType step, MistakeCategory category)
        {
            if (!stepMistakeCounts.ContainsKey(step)) stepMistakeCounts[step] = 0;
            if (!categoryMistakeCounts.ContainsKey(category)) categoryMistakeCounts[category] = 0;

            stepMistakeCounts[step]++;
            categoryMistakeCounts[category]++;

            int count = categoryMistakeCounts[category];
            Debug.Log($"[MistakePatternAnalyzer] Step {step} | Category {category} | Repeat Count: {count}");

            if (count >= 2)
            {
                OnMistakePatternDetected?.Invoke(step, category, count);
            }

            return count;
        }

        public int GetCategoryMistakeCount(MistakeCategory category)
        {
            return categoryMistakeCounts.TryGetValue(category, out int count) ? count : 0;
        }
    }
}
