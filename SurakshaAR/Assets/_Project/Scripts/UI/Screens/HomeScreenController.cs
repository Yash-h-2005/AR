using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SurakshaAR.UI.Navigation;
using SurakshaAR.UI.Data;
using SurakshaAR.UI.Components;
using SurakshaAR.Localization;

namespace SurakshaAR.UI.Screens
{
    public class HomeScreenController : MonoBehaviour
    {
        [Header("Top Header & Profile")]
        [SerializeField] private Button hamburgerButton;
        [SerializeField] private Button notificationButton;
        [SerializeField] private Text greetingText;
        [SerializeField] private Text workerIdText;
        [SerializeField] private GameObject notificationBadge;

        [Header("Progress Card")]
        [SerializeField] private Text trainingProgressTitleText;
        [SerializeField] private Text progressPercentText;
        [SerializeField] private Text progressSubtext;
        [SerializeField] private Slider progressBar;

        [Header("Modules Section")]
        [SerializeField] private Text trainingModulesTitleText;

        [Header("Fire Module Card")]
        [SerializeField] private Button fireModuleCardButton;
        [SerializeField] private Text fireCardTitleText;
        [SerializeField] private Text fireCardDescText;
        [SerializeField] private Text fireStatusText;
        [SerializeField] private Image fireStatusBadge;

        [Header("Gas Module Card")]
        [SerializeField] private Button gasModuleCardButton;
        [SerializeField] private Text gasCardTitleText;
        [SerializeField] private Text gasCardDescText;
        [SerializeField] private Text gasStatusText;

        [Header("Machinery Module Card")]
        [SerializeField] private Button machineryModuleCardButton;
        [SerializeField] private Text machineryCardTitleText;
        [SerializeField] private Text machineryCardDescText;
        [SerializeField] private Text machineryStatusText;

        [Header("My Certificates Button")]
        [SerializeField] private Button myCertificatesButton;
        [SerializeField] private Text myCertificatesButtonText;

        [Header("Side Drawer")]
        [SerializeField] private SideDrawerController sideDrawer;

        [Header("Notification Popup")]
        [SerializeField] private GameObject notificationPopup;
        [SerializeField] private Button notificationCloseButton;
        [SerializeField] private Button notificationBackdropButton;
        [SerializeField] private Text notificationTitleText;
        [SerializeField] private Text notificationItemsText;

        private void OnEnable()
        {
            if (UserSession.Instance != null)
            {
                UserSession.Instance.OnSessionUpdated += RefreshUI;
            }

            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
            }
        }

        private void OnDisable()
        {
            if (UserSession.Instance != null)
            {
                UserSession.Instance.OnSessionUpdated -= RefreshUI;
            }

            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
            }
        }

        private void Start()
        {
            // Close notification popup by default
            if (notificationPopup != null)
            {
                notificationPopup.SetActive(false);
            }

            RefreshUI();
            WireButtons();
        }

        private void Update()
        {
            // Handle Android Back button (KeyCode.Escape) to close notification popup
            if (notificationPopup != null && notificationPopup.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    CloseNotificationPopup();
                }
            }
        }

        private void HandleLanguageChanged(Language lang)
        {
            RefreshUI();
        }

        public void RefreshUI()
        {
            UpdateDashboardInfo();
            UpdateNotificationPopupContent();
        }

        private void UpdateDashboardInfo()
        {
            var session = UserSession.Instance;
            var loc = LocalizationManager.Instance;

            // Worker greeting and ID
            string rawName = session != null && !string.IsNullOrEmpty(session.WorkerName) ? session.WorkerName : "Worker";
            string rawId = session != null && !string.IsNullOrEmpty(session.WorkerID) ? session.WorkerID : "---";

            if (greetingText != null)
            {
                string helloFormat = loc != null ? loc.GetString("HELLO_FORMAT") : "Hello, {0} 👋";
                greetingText.text = string.Format(helloFormat, rawName);
            }

            if (workerIdText != null)
            {
                string idFormat = loc != null ? loc.GetString("WORKER_ID_FORMAT") : "Worker ID: {0}";
                workerIdText.text = string.Format(idFormat, rawId);
            }

            // Real progress calculation: derived strictly from session
            // Real progress calculation: derived strictly from session
            int completedCount = session != null ? session.CompletedModules : 0;
            int totalCount = session != null ? session.TotalModules : 3;
            int progressPercent = session != null ? session.OverallProgressPercent : 0;
            float progressFraction = session != null ? session.ProgressFraction : 0f;

            if (trainingProgressTitleText != null && loc != null)
            {
                trainingProgressTitleText.text = loc.GetString("TRAINING_PROGRESS");
            }

            if (progressPercentText != null)
            {
                progressPercentText.text = $"{progressPercent}%";
            }

            if (progressSubtext != null)
            {
                string subFormat = loc != null ? loc.GetString("MODULES_COMPLETED_FORMAT") : "{0} of {1} modules completed";
                progressSubtext.text = string.Format(subFormat, completedCount, totalCount);
            }

            if (progressBar != null)
            {
                progressBar.value = progressFraction;
            }

            if (trainingModulesTitleText != null && loc != null)
            {
                trainingModulesTitleText.text = loc.GetString("CURRENT_TRAINING");
            }

            // Fire Module Status Pill & Texts
            string fireStatus = session != null && session.Module1 != null ? session.Module1.GetModuleOverallStatus() : "NOT STARTED";
            if (fireCardTitleText != null && loc != null)
            {
                fireCardTitleText.text = loc.GetString("FIRE_MODULE_TITLE");
            }

            if (fireCardDescText != null)
            {
                int completed = session != null && session.Module1 != null ? session.Module1.CompletedSubModulesCount : 0;
                fireCardDescText.text = $"{completed} of 3 Sub-modules Complete";
            }

            if (fireStatusText != null && loc != null)
            {
                if (fireStatus == "COMPLETED") fireStatusText.text = loc.GetString("COMPLETED");
                else if (fireStatus == "ASSESSMENT AVAILABLE") fireStatusText.text = loc.GetString("ASSESSMENT_AVAILABLE");
                else if (fireStatus == "IN PROGRESS") fireStatusText.text = loc.GetString("IN_PROGRESS");
                else fireStatusText.text = loc.GetString("START_TRAINING");
            }

            if (fireStatusBadge != null)
            {
                if (fireStatus == "COMPLETED")
                    fireStatusBadge.color = new Color(0.13f, 0.77f, 0.37f, 1f); // Green
                else if (fireStatus == "ASSESSMENT AVAILABLE")
                    fireStatusBadge.color = new Color(0.96f, 0.62f, 0.04f, 1f); // Amber
                else if (fireStatus == "IN PROGRESS")
                    fireStatusBadge.color = new Color(0.20f, 0.55f, 0.95f, 1f); // Blue
                else
                    fireStatusBadge.color = new Color(0.98f, 0.45f, 0.09f, 1f); // Orange
            }

            // Gas Module Status
            string gasStatus = session != null && session.Module2 != null ? session.Module2.GetModuleOverallStatus() : "NOT STARTED";
            if (gasCardTitleText != null && loc != null)
            {
                gasCardTitleText.text = loc.GetString("GAS_MODULE_TITLE");
            }
            if (gasCardDescText != null)
            {
                int completed = session != null && session.Module2 != null ? session.Module2.CompletedSubModulesCount : 0;
                gasCardDescText.text = $"{completed} of 3 Sub-modules Complete";
            }
            if (gasStatusText != null && loc != null)
            {
                if (gasStatus == "COMPLETED") gasStatusText.text = loc.GetString("COMPLETED");
                else if (gasStatus == "ASSESSMENT AVAILABLE") gasStatusText.text = loc.GetString("ASSESSMENT_AVAILABLE");
                else if (gasStatus == "IN PROGRESS") gasStatusText.text = loc.GetString("IN_PROGRESS");
                else gasStatusText.text = loc.GetString("NOT_STARTED");
            }

            // Machinery Safety Module Status
            string machStatus = session != null && session.Module3 != null ? session.Module3.GetModuleOverallStatus() : "LOCKED";
            if (machineryCardTitleText != null && loc != null)
            {
                machineryCardTitleText.text = loc.GetString("MACHINERY_MODULE_TITLE");
            }
            if (machineryCardDescText != null)
            {
                machineryCardDescText.text = "This module is locked.";
            }
            if (machineryStatusText != null && loc != null)
            {
                if (machStatus == "COMPLETED") machineryStatusText.text = loc.GetString("COMPLETED");
                else if (machStatus == "ASSESSMENT AVAILABLE") machineryStatusText.text = loc.GetString("ASSESSMENT_AVAILABLE");
                else if (machStatus == "IN PROGRESS") machineryStatusText.text = loc.GetString("IN_PROGRESS");
                else machineryStatusText.text = loc.GetString("LOCKED");
            }

            // Certificates button
            if (myCertificatesButtonText != null && loc != null)
            {
                myCertificatesButtonText.text = loc.GetString("MY_CERTIFICATES");
            }

            // Notification badge indicator
            if (notificationBadge != null)
            {
                int notifCount = session != null ? session.Notifications.Count : 0;
                notificationBadge.SetActive(notifCount > 0);
            }
        }

        private void UpdateNotificationPopupContent()
        {
            var loc = LocalizationManager.Instance;
            if (notificationTitleText != null && loc != null)
            {
                notificationTitleText.text = loc.GetString("NOTIFICATIONS");
            }

            if (notificationItemsText != null)
            {
                var session = UserSession.Instance;
                IReadOnlyList<string> notifs = session != null ? session.Notifications : null;

                if (notifs == null || notifs.Count == 0)
                {
                    notificationItemsText.text = loc != null ? loc.GetString("NO_NEW_NOTIFICATIONS") : "No new notifications";
                }
                else
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    for (int i = 0; i < notifs.Count; i++)
                    {
                        sb.AppendLine($"• {notifs[i]}");
                        if (i < notifs.Count - 1)
                        {
                            sb.AppendLine();
                        }
                    }
                    notificationItemsText.text = sb.ToString();
                }
            }
        }

        private void WireButtons()
        {
            if (hamburgerButton != null)
            {
                hamburgerButton.onClick.RemoveAllListeners();
                hamburgerButton.onClick.AddListener(() =>
                {
                    if (sideDrawer != null)
                    {
                        sideDrawer.ToggleDrawer();
                    }
                });
            }

            if (notificationButton != null)
            {
                notificationButton.onClick.RemoveAllListeners();
                notificationButton.onClick.AddListener(OpenNotificationPopup);
            }

            if (notificationCloseButton != null)
            {
                notificationCloseButton.onClick.RemoveAllListeners();
                notificationCloseButton.onClick.AddListener(CloseNotificationPopup);
            }

            if (notificationBackdropButton != null)
            {
                notificationBackdropButton.onClick.RemoveAllListeners();
                notificationBackdropButton.onClick.AddListener(CloseNotificationPopup);
            }

            if (fireModuleCardButton != null)
            {
                fireModuleCardButton.onClick.RemoveAllListeners();
                fireModuleCardButton.onClick.AddListener(() =>
                {
                    TrainingModulesController.TargetModuleToOpen = "M1";
                    if (SceneNavigator.Instance != null)
                        SceneNavigator.Instance.NavigateTo(ScreenState.TrainingModules);
                });
            }

            if (gasModuleCardButton != null)
            {
                gasModuleCardButton.onClick.RemoveAllListeners();
                gasModuleCardButton.onClick.AddListener(() =>
                {
                    TrainingModulesController.TargetModuleToOpen = "M2";
                    if (SceneNavigator.Instance != null)
                        SceneNavigator.Instance.NavigateTo(ScreenState.TrainingModules);
                });
            }

            if (machineryModuleCardButton != null)
            {
                machineryModuleCardButton.onClick.RemoveAllListeners();
                machineryModuleCardButton.onClick.AddListener(() =>
                {
                    TrainingModulesController.TargetModuleToOpen = "M3";
                    if (SceneNavigator.Instance != null)
                        SceneNavigator.Instance.NavigateTo(ScreenState.TrainingModules);
                });
            }

            if (myCertificatesButton != null)
            {
                myCertificatesButton.onClick.RemoveAllListeners();
                myCertificatesButton.onClick.AddListener(() =>
                {
                    if (SceneNavigator.Instance != null)
                        SceneNavigator.Instance.NavigateTo(ScreenState.CertificatePreview);
                });
            }
        }

        public void OpenNotificationPopup()
        {
            UpdateNotificationPopupContent();
            if (notificationPopup != null)
            {
                notificationPopup.SetActive(true);
            }
        }

        public void CloseNotificationPopup()
        {
            if (notificationPopup != null)
            {
                notificationPopup.SetActive(false);
            }
        }
    }
}
