using UnityEngine;

namespace SurakshaAR.Training.Fire
{
    /// <summary>
    /// PHASE 2A Orchestrator.
    /// Lightweight bridge that listens to FireHazardController events and
    /// triggers EmergencyAlarmController placement at the correct world position.
    ///
    /// This decouples FireHazardController from EmergencyAlarmController.
    /// </summary>
    public class Phase2AOrchestrator : MonoBehaviour
    {
        [Header("Phase 2A Components")]
        [SerializeField] private FireHazardController fireHazardController;
        [SerializeField] private EmergencyAlarmController emergencyAlarmController;

        private void Start()
        {
            if (fireHazardController == null)
            {
                Debug.LogError("[Phase2AOrchestrator] FireHazardController not assigned!");
                return;
            }
            if (emergencyAlarmController == null)
            {
                Debug.LogError("[Phase2AOrchestrator] EmergencyAlarmController not assigned!");
                return;
            }

            // Subscribe: when fire is triggered, place the alarm 2m ahead of the fire
            fireHazardController.OnFireTriggered += HandleFireTriggered;
        }

        private void OnDestroy()
        {
            if (fireHazardController != null)
                fireHazardController.OnFireTriggered -= HandleFireTriggered;
        }

        private void HandleFireTriggered()
        {
            Vector3 firePos = fireHazardController.GetFireWorldPosition();
            emergencyAlarmController.PlaceAlarm(firePos);
            Debug.Log("[Phase2AOrchestrator] Alarm placed at fire position: " + firePos);
        }
    }
}
