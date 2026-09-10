using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SurakshaAR.Training.Fire
{
    /// <summary>
    /// Master orchestrator for Module 1 Final Assessment.
    /// Combines Sub-module 1 (Electrical Fire) and Sub-module 3 (Explosion & Evacuation)
    /// into one continuous, deterministic simulation.
    ///
    /// Sequence:
    ///   1. Worker moves into mine and extinguishes the electrical fire with CO2 extinguisher.
    ///   2. Explosion occurs ONLY AFTER the fire is extinguished (1.5s dramatic delay).
    ///   3. Automatic emergency siren and flashing beacon activate.
    ///   4. Progressive 3D collapse begins advancing behind worker toward Route B.
    ///   5. Worker must follow Route B (Emergency Exit). Entering Route A triggers a warning toast with score deduction.
    ///   6. Reaching the Assembly Point halts the collapse and evaluates performance dynamically.
    /// </summary>
    public class FireAssessmentController : MonoBehaviour
    {
        public enum AssessmentPhase
        {
            EnterAndExtinguishFire,
            ExplosionTriggering,
            Evacuation,
            AssessmentCompleted,
            Failed
        }

        // ── Inspector References ─────────────────────────────────────────────
        [Header("Simulation Controllers")]
        [SerializeField] private FireResponseEvaluator fireEvaluator;
        [SerializeField] private ExplosionHazardController explosionController;
        [SerializeField] private EmergencyAlarmController alarmController;
        [SerializeField] private MineCollapseController collapseController;
        [SerializeField] private AssemblyZoneController assemblyZoneController;
        [SerializeField] private AssessmentResultScreen resultScreen;
        [SerializeField] private MineSimulatorHUD hud;
        [SerializeField] private Camera virtualCamera;

        // ── State Tracking ───────────────────────────────────────────────────
        public AssessmentPhase CurrentPhase { get; private set; } = AssessmentPhase.EnterAndExtinguishFire;

        private float firePhaseStartTime = 0f;
        private float fireExtinguishedTime = 0f;
        private float evacuationStartTime = 0f;
        private float evacuationEndTime = 0f;

        private bool wrongResourceAttempted = false;
        private bool tookWrongRoute = false;
        private bool wrongRouteWarningShown = false;

        private float calculatedFireScore = 0f;
        private float calculatedRouteScore = 30f;
        private float calculatedEvacScore = 0f;
        private float totalScore = 0f;

        private void Start()
        {
            firePhaseStartTime = Time.time;

            if (hud != null)
            {
                hud.SetObjective("Inspect corridor and extinguish the electrical equipment fire.");
                hud.SetBadge("ASSESSMENT", new Color(0.95f, 0.70f, 0.15f));
            }

            // Hook into fire response evaluator
            if (fireEvaluator != null)
            {
                fireEvaluator.OnFireControlled += HandleFireExtinguished;
                fireEvaluator.OnHPChanged += HandleWorkerHealthChanged;
            }

            // Hook into collapse events
            if (collapseController != null)
            {
                collapseController.OnPlayerCaughtInCollapse += HandleCaughtInCollapse;
            }

            // Hook into assembly zone
            if (assemblyZoneController != null)
            {
                assemblyZoneController.OnAssemblyCompleted += HandleAssemblyReached;
            }
        }

        private void OnDestroy()
        {
            if (fireEvaluator != null)
            {
                fireEvaluator.OnFireControlled -= HandleFireExtinguished;
                fireEvaluator.OnHPChanged -= HandleWorkerHealthChanged;
            }

            if (collapseController != null)
            {
                collapseController.OnPlayerCaughtInCollapse -= HandleCaughtInCollapse;
            }

            if (assemblyZoneController != null)
            {
                assemblyZoneController.OnAssemblyCompleted -= HandleAssemblyReached;
            }
        }

        private void Update()
        {
            if (CurrentPhase == AssessmentPhase.Evacuation)
            {
                CheckWrongRouteEntry();
            }
        }

        // ── Phase 1: Electric Fire Extinguished ──────────────────────────────

        private void HandleWorkerHealthChanged(int hp)
        {
            // If worker took damage during fire phase, they attempted an unsafe action (water or mud on electric fire)
            if (CurrentPhase == AssessmentPhase.EnterAndExtinguishFire && hp < 100)
            {
                wrongResourceAttempted = true;
                Debug.Log("[FireAssessment] Worker attempted incorrect resource on electrical fire.");
            }
        }

        private void HandleFireExtinguished()
        {
            if (CurrentPhase != AssessmentPhase.EnterAndExtinguishFire) return;

            fireExtinguishedTime = Time.time;
            float fireDuration = fireExtinguishedTime - firePhaseStartTime;

            // Score Fire Extinguishment (Max 40 pts)
            // 25 pts for extinguisher correctness
            float accuracyPts = wrongResourceAttempted ? 15f : 25f;
            // 15 pts for response speed
            float speedPts = 15f;
            if (fireDuration > 35f) speedPts = 6f;
            else if (fireDuration > 22f) speedPts = 10f;

            calculatedFireScore = accuracyPts + speedPts;
            Debug.Log($"[FireAssessment] Fire extinguished in {fireDuration:F1}s! Fire Score: {calculatedFireScore}/40");

            CurrentPhase = AssessmentPhase.ExplosionTriggering;
            StartCoroutine(SequenceExplosionAfterFire());
        }

        private IEnumerator SequenceExplosionAfterFire()
        {
            if (hud != null)
            {
                hud.ShowStatusToast("✅ Electric fire suppressed!", new Color(0.15f, 0.95f, 0.45f), 1.5f);
            }

            // 1.5s dramatic pause after extinguishing before collision occurs
            yield return new WaitForSeconds(1.5f);

            Debug.Log("[FireAssessment] Triggering explosion & collision sequence post-fire!");
            CurrentPhase = AssessmentPhase.Evacuation;
            evacuationStartTime = Time.time;

            if (explosionController != null)
            {
                explosionController.TriggerExplosion();
            }

            if (hud != null)
            {
                hud.SetObjective("EVACUATE! Follow ROUTE B to the Emergency Exit!");
                hud.SetBadge("EVACUATING", new Color(1.0f, 0.25f, 0.20f));
            }
        }

        // ── Phase 2: Evacuation & Route Monitoring ───────────────────────────

        private void CheckWrongRouteEntry()
        {
            if (virtualCamera == null) return;

            Vector3 pos = virtualCamera.transform.position;

            // Route A is the left branch (X < -0.6m) starting at junction (Z >= 33m)
            if (pos.z >= 33.0f && pos.x < -0.6f && pos.z < 49.0f)
            {
                if (!wrongRouteWarningShown)
                {
                    wrongRouteWarningShown = true;
                    tookWrongRoute = true;
                    calculatedRouteScore = 15f; // Deduct 15 pts for choosing wrong route

                    if (hud != null)
                    {
                        hud.ShowStatusToast("⚠️ WRONG ROUTE!\nRoute A is Haulage Drift (Blocked).\nTurn back and take ROUTE B!", new Color(1.0f, 0.35f, 0.15f), 4.5f);
                    }
                    Debug.Log("[FireAssessment] Worker entered Route A (Haulage Drift). Warning shown, score penalized.");
                }
            }
        }

        // ── Evacuation Outcomes ──────────────────────────────────────────────

        private void HandleCaughtInCollapse()
        {
            if (CurrentPhase == AssessmentPhase.AssessmentCompleted || CurrentPhase == AssessmentPhase.Failed) return;
            CurrentPhase = AssessmentPhase.Failed;

            calculatedEvacScore = 0f;
            totalScore = calculatedFireScore + calculatedRouteScore + calculatedEvacScore;

            Debug.Log($"[FireAssessment] ❌ Evacuation Failed — caught in collapse. Total Score: {totalScore:F0}%");

            if (resultScreen != null)
            {
                resultScreen.ShowAssessmentResult(
                    totalScore,
                    calculatedFireScore,
                    calculatedRouteScore,
                    calculatedEvacScore,
                    passed: false,
                    customFeedback: "You were caught by the collapsing mine roof. Quick response and evacuation along Route B is required to survive."
                );
            }
        }

        private void HandleAssemblyReached()
        {
            if (CurrentPhase == AssessmentPhase.AssessmentCompleted || CurrentPhase == AssessmentPhase.Failed) return;
            CurrentPhase = AssessmentPhase.AssessmentCompleted;

            evacuationEndTime = Time.time;
            float evacDuration = evacuationEndTime - evacuationStartTime;

            // Score Evacuation & Survival (Max 30 pts)
            // 20 pts base survival + up to 10 pts for prompt speed
            float evacSpeedPts = 10f;
            if (evacDuration > 55f) evacSpeedPts = 4f;
            else if (evacDuration > 40f) evacSpeedPts = 7f;

            calculatedEvacScore = 20f + evacSpeedPts;

            // Calculate final total score
            totalScore = Mathf.Clamp(calculatedFireScore + calculatedRouteScore + calculatedEvacScore, 0f, 100f);
            bool passed = totalScore >= 70f;

            Debug.Log($"[FireAssessment] 🎉 Assessment Finished! Score: {totalScore:F1}% (Fire={calculatedFireScore}, Route={calculatedRouteScore}, Evac={calculatedEvacScore}) Passed={passed}");

            if (resultScreen != null)
            {
                resultScreen.ShowAssessmentResult(
                    totalScore,
                    calculatedFireScore,
                    calculatedRouteScore,
                    calculatedEvacScore,
                    passed: passed
                );
            }
        }

        public void SetReferences(
            FireResponseEvaluator fireEval,
            ExplosionHazardController expCtrl,
            EmergencyAlarmController alarmCtrl,
            MineCollapseController collapseCtrl,
            AssemblyZoneController assemblyCtrl,
            AssessmentResultScreen resScreen,
            MineSimulatorHUD hudRef,
            Camera cam)
        {
            fireEvaluator = fireEval;
            explosionController = expCtrl;
            alarmController = alarmCtrl;
            collapseController = collapseCtrl;
            assemblyZoneController = assemblyCtrl;
            resultScreen = resScreen;
            hud = hudRef;
            virtualCamera = cam;
        }
    }
}
