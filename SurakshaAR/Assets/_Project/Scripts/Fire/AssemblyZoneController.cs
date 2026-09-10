using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SurakshaAR.Training.Fire
{
    /// <summary>
    /// Monitors player physical arrival at the outdoor Emergency Assembly Point.
    /// When entering the zone, updates the objective to: "Report to the assembly point."
    /// After remaining inside for ~2 seconds, triggers: "Safe assembly area reached."
    /// Does NOT teleport the player and does NOT show "Training Complete".
    /// </summary>
    public class AssemblyZoneController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera virtualCamera;
        [SerializeField] private MineSimulatorHUD hud;

        [Header("Assembly Zone Settings")]
        [Tooltip("Center of the outdoor assembly area in world space.")]
        [SerializeField] private Vector3 assemblyCenter = new Vector3(-8.5f, 1.6f, 94.0f);

        public Vector3 AssemblyCenter
        {
            get => assemblyCenter;
            set => assemblyCenter = value;
        }

        [Tooltip("Radius of the assembly detection zone in meters.")]
        [SerializeField] private float zoneRadius = 6.0f;

        [Tooltip("Time in seconds the player must remain inside the zone.")]
        [SerializeField] private float dwellTimeRequired = 2.0f;

        // ── State ──────────────────────────────────────────────────────────────
        private bool hasEnteredZone = false;
        private bool hasCompletedAssembly = false;
        private float dwellTimer = 0f;

        public event Action OnPlayerEnteredAssemblyZone;
        public event Action OnAssemblyCompleted;

        private void Update()
        {
            if (hasCompletedAssembly) return;
            if (virtualCamera == null) return;

            Vector3 playerPos = virtualCamera.transform.position;
            // Measure 2D horizontal distance (ignore small vertical head bob)
            Vector2 playerXZ = new Vector2(playerPos.x, playerPos.z);
            Vector2 centerXZ = new Vector2(assemblyCenter.x, assemblyCenter.z);
            float dist = Vector2.Distance(playerXZ, centerXZ);

            bool inZone = dist <= zoneRadius;

            if (inZone)
            {
                if (!hasEnteredZone)
                {
                    hasEnteredZone = true;
                    HandleEnteredZone();
                }

                dwellTimer += Time.deltaTime;
                if (dwellTimer >= dwellTimeRequired)
                {
                    hasCompletedAssembly = true;
                    HandleAssemblyCompleted();
                }
            }
            else
            {
                // Reset timer if player steps back outside before completing 2s dwell
                if (hasEnteredZone && !hasCompletedAssembly)
                {
                    dwellTimer = Mathf.Max(0f, dwellTimer - Time.deltaTime * 2f);
                }
            }
        }

        private void HandleEnteredZone()
        {
            Debug.Log("[AssemblyZone] Worker entered Emergency Assembly Zone.");
            if (hud != null)
            {
                hud.SetObjective("Report to the assembly point.");
                hud.SetBadge("At Assembly Point", new Color(0.15f, 0.95f, 0.45f));
            }
            OnPlayerEnteredAssemblyZone?.Invoke();
        }

        private void HandleAssemblyCompleted()
        {
            Debug.Log("[AssemblyZone] 🟢 Safe assembly area reached! Evacuation successful.");
            if (hud != null)
            {
                hud.SetObjective("Safe assembly area reached. Safety check in progress.");
                hud.ShowStatusToast("✅ Safe assembly area reached.\nRoll call in progress.", new Color(0.15f, 0.95f, 0.45f), 4.5f);
            }
            // Sub-module completion is now handled by SubModuleCompletionScreen
            // which listens to OnAssemblyCompleted and shows the confirmation overlay.
            OnAssemblyCompleted?.Invoke();
        }

        public void SetReferences(Camera cam, MineSimulatorHUD hudRef, Vector3 center, float radius)
        {
            virtualCamera = cam;
            hud = hudRef;
            assemblyCenter = center;
            zoneRadius = radius;
        }
    }
}
