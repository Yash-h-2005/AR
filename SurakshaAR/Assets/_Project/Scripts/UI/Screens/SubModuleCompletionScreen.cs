using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using SurakshaAR.UI.Navigation;
using SurakshaAR.UI.Data;
using SurakshaAR.UI.Screens;

namespace SurakshaAR.Training.Fire
{
    /// <summary>
    /// Full-screen overlay shown after the worker reaches the assembly point.
    /// Displays "Successfully Completed" and a "NEXT & CONFIRM" button.
    /// Only marks the sub-module as DONE when the worker explicitly taps the button.
    /// </summary>
    public class SubModuleCompletionScreen : MonoBehaviour
    {
        // ── Static Configuration ─────────────────────────────────────────────
        /// <summary>
        /// Set before loading the simulation scene. Format: "M1_S1", "M1_S2", "M1_S3"
        /// </summary>
        public static string ActiveSubModuleId = "";

        // ── Inspector References ─────────────────────────────────────────────
        [Header("UI References")]
        [SerializeField] private GameObject overlayPanel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Text confirmButtonText;

        [Header("Scene Controller")]
        [SerializeField] private AssemblyZoneController assemblyZoneController;

        // ── State ────────────────────────────────────────────────────────────
        private bool hasConfirmed = false;

        private void Start()
        {
            if (overlayPanel != null)
                overlayPanel.SetActive(false);

            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnNextConfirmClicked);

            if (assemblyZoneController == null)
#if UNITY_2023_1_OR_NEWER
                assemblyZoneController = FindFirstObjectByType<AssemblyZoneController>();
#else
                assemblyZoneController = FindObjectOfType<AssemblyZoneController>();
#endif

            if (assemblyZoneController != null)
                assemblyZoneController.OnAssemblyCompleted += ShowCompletionScreen;
        }

        private void OnDestroy()
        {
            if (assemblyZoneController != null)
                assemblyZoneController.OnAssemblyCompleted -= ShowCompletionScreen;
        }

        /// <summary>
        /// Called by AssemblyZoneController.OnAssemblyCompleted event.
        /// Shows the completion overlay.
        /// </summary>
        public void ShowCompletionScreen()
        {
            if (hasConfirmed) return;

            Debug.Log($"[CompletionScreen] Showing completion screen for sub-module: {ActiveSubModuleId}");

            if (overlayPanel != null)
                overlayPanel.SetActive(true);

            if (titleText != null)
                titleText.text = "✅ Successfully Completed";

            if (descriptionText != null)
            {
                if (ActiveSubModuleId == "M1_S3")
                {
                    descriptionText.text = "You successfully escaped the mine collapse\nand reached the assembly point.";
                }
                else
                {
                    descriptionText.text = "You have successfully completed\nthis training simulation.";
                }
            }

            if (confirmButtonText != null)
                confirmButtonText.text = "NEXT & CONFIRM";
        }

        private void OnNextConfirmClicked()
        {
            if (hasConfirmed) return;
            hasConfirmed = true;

            Debug.Log($"[CompletionScreen] Worker confirmed completion: {ActiveSubModuleId}");

            // Parse sub-module ID and mark as completed
            if (UserSession.Instance != null && !string.IsNullOrEmpty(ActiveSubModuleId))
            {
                string moduleId = "M1";
                int subIndex = 1;

                if (ActiveSubModuleId.Contains("_S1")) subIndex = 1;
                else if (ActiveSubModuleId.Contains("_S2")) subIndex = 2;
                else if (ActiveSubModuleId.Contains("_S3")) subIndex = 3;

                if (ActiveSubModuleId.StartsWith("M2")) moduleId = "M2";
                else if (ActiveSubModuleId.StartsWith("M3")) moduleId = "M3";

                UserSession.Instance.CompleteSubModule(moduleId, subIndex);
                Debug.Log($"[CompletionScreen] Sub-module {moduleId} #{subIndex} marked as DONE.");
            }

            // Navigate back to Training Modules, opening the correct module detail
            TrainingModulesController.TargetModuleToOpen = "M1";
            if (ActiveSubModuleId.StartsWith("M2")) TrainingModulesController.TargetModuleToOpen = "M2";

            if (SceneNavigator.Instance != null)
            {
                SceneNavigator.Instance.NavigateTo(ScreenState.TrainingModules);
            }
            else
            {
                // Fallback: direct scene load
                SceneManager.LoadScene("TrainingModules");
            }
        }

        /// <summary>
        /// Wire references from SceneBuilderScript.
        /// </summary>
        public void SetReferences(GameObject panel, Text title, Text desc, Button btn, Text btnText, AssemblyZoneController azCtrl = null)
        {
            overlayPanel = panel;
            titleText = title;
            descriptionText = desc;
            confirmButton = btn;
            confirmButtonText = btnText;

            if (overlayPanel != null)
                overlayPanel.SetActive(false);

            if (azCtrl != null)
            {
                if (assemblyZoneController != null)
                    assemblyZoneController.OnAssemblyCompleted -= ShowCompletionScreen;
                assemblyZoneController = azCtrl;
                assemblyZoneController.OnAssemblyCompleted += ShowCompletionScreen;
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(OnNextConfirmClicked);
                confirmButton.onClick.AddListener(OnNextConfirmClicked);
            }
        }
    }
}
