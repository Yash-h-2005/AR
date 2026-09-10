using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurakshaAR.Training.Fire
{
    public enum HelpType
    {
        Hint,
        Example,
        Practice
    }

    /// <summary>
    /// Adaptive multi-tiered hint escalation engine.
    /// Escalates assistance from text hints to visual outlines, ghost demos, and guided practice on repeated mistakes.
    /// Tracks help metrics separately from independent worker mastery.
    /// </summary>
    public class AdaptiveHelpController : MonoBehaviour
    {
        public static AdaptiveHelpController Instance { get; private set; }

        public event Action<string, HelpType> OnHelpTriggered;

        // Metrics tracking per step
        public int TotalHintsUsed { get; private set; } = 0;
        public int TotalExamplesUsed { get; private set; } = 0;
        public int TotalPracticeUsed { get; private set; } = 0;

        private readonly Dictionary<TrainingStepType, int> mistakeCounters = new Dictionary<TrainingStepType, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void ResetSessionHelp()
        {
            TotalHintsUsed = 0;
            TotalExamplesUsed = 0;
            TotalPracticeUsed = 0;
            mistakeCounters.Clear();
        }

        public void RegisterMistake(TrainingStepType currentStep, TrainingObject targetObject)
        {
            if (!mistakeCounters.ContainsKey(currentStep))
            {
                mistakeCounters[currentStep] = 0;
            }

            mistakeCounters[currentStep]++;
            int mistakes = mistakeCounters[currentStep];

            Debug.Log($"[AdaptiveHelpController] Mistake registered for step {currentStep}. Count = {mistakes}");

            // Multi-tiered help escalation
            switch (mistakes)
            {
                case 1:
                    // Tier 1: Contextual Tip Banner
                    string hintText = GetStepTip(currentStep);
                    OnHelpTriggered?.Invoke(hintText, HelpType.Hint);
                    break;

                case 2:
                    // Tier 2: Visual Highlight on target object
                    if (targetObject != null)
                    {
                        targetObject.SetHighlight(true, Color.yellow);
                    }
                    OnHelpTriggered?.Invoke("Target object highlighted in yellow.", HelpType.Hint);
                    break;

                case 3:
                    // Tier 3: Ghost Demo / Animated Directional Arrow
                    if (targetObject != null)
                    {
                        targetObject.SetHighlight(true, Color.cyan);
                    }
                    OnHelpTriggered?.Invoke("Demonstration arrow enabled pointing to target.", HelpType.Example);
                    break;

                default:
                    // Tier 4: Guided Mini-Practice
                    OnHelpTriggered?.Invoke("Guided practice step available. Select NEED HELP? -> PRACTICE THIS STEP.", HelpType.Practice);
                    break;
            }
        }

        public void RequestHelp(TrainingStepType currentStep, HelpType helpType, TrainingObject targetObject)
        {
            switch (helpType)
            {
                case HelpType.Hint:
                    TotalHintsUsed++;
                    string tip = GetStepTip(currentStep);
                    if (targetObject != null) targetObject.SetHighlight(true, Color.yellow);
                    OnHelpTriggered?.Invoke(tip, HelpType.Hint);
                    break;

                case HelpType.Example:
                    TotalExamplesUsed++;
                    if (targetObject != null) targetObject.SetHighlight(true, Color.cyan);
                    OnHelpTriggered?.Invoke($"[DEMO] Watch target location: {GetStepTip(currentStep)}", HelpType.Example);
                    break;

                case HelpType.Practice:
                    TotalPracticeUsed++;
                    OnHelpTriggered?.Invoke("[PRACTICE MODE] Follow guided arrows to complete this step.", HelpType.Practice);
                    break;
            }
        }

        private string GetStepTip(TrainingStepType step)
        {
            return step switch
            {
                TrainingStepType.IdentifyFire => "Look around and tap the 3D electrical fire hazard.",
                TrainingStepType.RaiseAlarm => "Locate the wall-mounted red emergency call point and tap to activate.",
                TrainingStepType.IdentifyExit => "Find the unblocked green emergency exit door (avoid smoke-filled exits).",
                TrainingStepType.SelectExtinguisher => "Select the CO2 / Dry Powder extinguisher suitable for electrical fires.",
                TrainingStepType.PickupExtinguisher => "Grasp the extinguisher handle and pull out the yellow safety ring pin.",
                TrainingStepType.UseExtinguisher => "Aim nozzle directly at the BASE of the fire and hold SPRAY.",
                TrainingStepType.Evacuate => "Crouch low below the smoke layer and physically walk toward the exit.",
                TrainingStepType.AssemblyPoint => "Physically walk outside to the green emergency assembly point beacon.",
                _ => "Follow safety training instructions."
            };
        }
    }
}
