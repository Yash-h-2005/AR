using System;
using UnityEngine;
using UnityEngine.UI;
using SurakshaAR.UI.Navigation;
using SurakshaAR.UI.Data;
using SurakshaAR.Localization;

namespace SurakshaAR.UI.Screens
{
    /// <summary>
    /// 2-Tier Training Controller:
    /// Level 1: 3 clean, uncluttered module cards. Module 3 is completely locked on this page.
    /// Level 2: Dedicated detail view for the selected module with strict sequential unlocking
    /// ("as 1 finishes only other should open", starts at START for sub1, sub2/3 locked),
    /// and direct routing of Module 1 Fire Assessment & Extinguisher to the finished FireMineSimulator.
    /// </summary>
    public class TrainingModulesController : MonoBehaviour
    {
        public static string TargetModuleToOpen = null;

        [System.Serializable]
        public class TopModuleCardUI
        {
            public string moduleId;
            public Text titleText;
            public Text statusText;
            public Image statusBadge;
            public Text progressSummaryText;
            public Button openButton;
            public Text openButtonText;
        }

        [Header("Level 1: Top-Level Module List")]
        [SerializeField] private GameObject moduleListPanel;
        [SerializeField] private Button headerBackButton;
        [SerializeField] private Text headerTitleText;
        [SerializeField] private Text headerSubheadText;
        [SerializeField] private TopModuleCardUI cardM1;
        [SerializeField] private TopModuleCardUI cardM2;
        [SerializeField] private TopModuleCardUI cardM3;

        [Header("Level 2: Module Detail View")]
        [SerializeField] private GameObject moduleDetailPanel;
        [SerializeField] private Button detailBackButton;
        [SerializeField] private Text detailModuleTitleText;
        [SerializeField] private Text detailModuleStatusText;
        [SerializeField] private Image detailModuleStatusBadge;

        [Header("Detail Sub-Modules")]
        [SerializeField] private Text sub1TitleText;
        [SerializeField] private Text sub1StatusText;
        [SerializeField] private Button sub1ActionButton;
        [SerializeField] private Text sub1ActionButtonText;

        [SerializeField] private Text sub2TitleText;
        [SerializeField] private Text sub2StatusText;
        [SerializeField] private Button sub2ActionButton;
        [SerializeField] private Text sub2ActionButtonText;

        [SerializeField] private Text sub3TitleText;
        [SerializeField] private Text sub3StatusText;
        [SerializeField] private Button sub3ActionButton;
        [SerializeField] private Text sub3ActionButtonText;

        [Header("Detail Assessment & Certificate")]
        [SerializeField] private Text assessmentTitleText;
        [SerializeField] private Text assessmentStatusText;
        [SerializeField] private Button startAssessmentButton;
        [SerializeField] private Text startAssessmentButtonText;

        [SerializeField] private GameObject certificateRow;
        [SerializeField] private Text certificateStatusText;
        [SerializeField] private Button viewCertificateButton;
        [SerializeField] private Text viewCertificateButtonText;

        private string selectedModuleId = "M1";

        public void SetupBindings(
            GameObject listPanel, Button headerBack, Text headerTitle, Text headerSubhead,
            TopModuleCardUI cM1, TopModuleCardUI cM2, TopModuleCardUI cM3,
            GameObject detailPanel, Button detailBack, Text detailTitle, Text detailStatus, Image detailBadge,
            Text s1Title, Text s1Status, Button s1Btn, Text s1BtnTxt,
            Text s2Title, Text s2Status, Button s2Btn, Text s2BtnTxt,
            Text s3Title, Text s3Status, Button s3Btn, Text s3BtnTxt,
            Text assessTitle, Text assessStatus, Button assessBtn, Text assessBtnTxt,
            GameObject certRow, Text certStatus, Button certBtn, Text certBtnTxt)
        {
            moduleListPanel = listPanel;
            headerBackButton = headerBack;
            headerTitleText = headerTitle;
            headerSubheadText = headerSubhead;
            cardM1 = cM1;
            cardM2 = cM2;
            cardM3 = cM3;

            moduleDetailPanel = detailPanel;
            detailBackButton = detailBack;
            detailModuleTitleText = detailTitle;
            detailModuleStatusText = detailStatus;
            detailModuleStatusBadge = detailBadge;

            sub1TitleText = s1Title;
            sub1StatusText = s1Status;
            sub1ActionButton = s1Btn;
            sub1ActionButtonText = s1BtnTxt;

            sub2TitleText = s2Title;
            sub2StatusText = s2Status;
            sub2ActionButton = s2Btn;
            sub2ActionButtonText = s2BtnTxt;

            sub3TitleText = s3Title;
            sub3StatusText = s3Status;
            sub3ActionButton = s3Btn;
            sub3ActionButtonText = s3BtnTxt;

            assessmentTitleText = assessTitle;
            assessmentStatusText = assessStatus;
            startAssessmentButton = assessBtn;
            startAssessmentButtonText = assessBtnTxt;

            certificateRow = certRow;
            certificateStatusText = certStatus;
            viewCertificateButton = certBtn;
            viewCertificateButtonText = certBtnTxt;
        }

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
            // Level 1 Back Button -> Home
            if (headerBackButton != null)
            {
                headerBackButton.onClick.AddListener(() =>
                {
                    if (SceneNavigator.Instance != null) SceneNavigator.Instance.NavigateTo(ScreenState.Home);
                });
            }

            // Level 2 Back Button -> Return to Level 1
            if (detailBackButton != null)
            {
                detailBackButton.onClick.AddListener(() =>
                {
                    ShowModuleList();
                });
            }

            // Wire Top Cards
            if (cardM1 != null && cardM1.openButton != null)
            {
                cardM1.openButton.onClick.AddListener(() => OpenModuleDetail("M1"));
            }
            if (cardM2 != null && cardM2.openButton != null)
            {
                cardM2.openButton.onClick.AddListener(() => OpenModuleDetail("M2"));
            }
            if (cardM3 != null && cardM3.openButton != null)
            {
                // Module 3 is completely locked on this page
                cardM3.openButton.interactable = false;
            }

            // Wire Detail Sub-Module buttons
            if (sub1ActionButton != null)
            {
                sub1ActionButton.onClick.AddListener(OnSub1Clicked);
            }
            if (sub2ActionButton != null)
            {
                sub2ActionButton.onClick.AddListener(OnSub2Clicked);
            }
            if (sub3ActionButton != null)
            {
                sub3ActionButton.onClick.AddListener(OnSub3Clicked);
            }

            // Wire Assessment Button
            if (startAssessmentButton != null)
            {
                startAssessmentButton.onClick.AddListener(OnAssessmentClicked);
            }

            if (viewCertificateButton != null)
            {
                viewCertificateButton.onClick.AddListener(() =>
                {
                    if (SceneNavigator.Instance != null)
                    {
                        SceneNavigator.Instance.NavigateTo(ScreenState.CertificatePreview);
                    }
                });
            }

            if (!string.IsNullOrEmpty(TargetModuleToOpen))
            {
                string target = TargetModuleToOpen;
                TargetModuleToOpen = null;
                if (target == "M1" || target == "M2")
                {
                    OpenModuleDetail(target);
                }
                else
                {
                    ShowModuleList();
                }
            }
            else
            {
                ShowModuleList();
            }
            RefreshUI();
        }

        private void Update()
        {
            // Handle Android hardware back inside detail view
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (moduleDetailPanel != null && moduleDetailPanel.activeSelf)
                {
                    ShowModuleList();
                }
                else if (SceneNavigator.Instance != null)
                {
                    SceneNavigator.Instance.NavigateTo(ScreenState.Home);
                }
            }
        }

        private void HandleLanguageChanged(Language lang)
        {
            RefreshUI();
        }

        public void ShowModuleList()
        {
            if (moduleListPanel != null) moduleListPanel.SetActive(true);
            if (moduleDetailPanel != null) moduleDetailPanel.SetActive(false);
            RefreshUI();
        }

        public void OpenModuleDetail(string moduleId)
        {
            // Module 3 is locked in previous page itself
            if (moduleId == "M3")
            {
                Debug.Log("[Training] Module 3 (Machinery Safety) is completely locked.");
                return;
            }

            selectedModuleId = moduleId;
            if (moduleListPanel != null) moduleListPanel.SetActive(false);
            if (moduleDetailPanel != null) moduleDetailPanel.SetActive(true);
            RefreshUI();
        }

        private void OnSub1Clicked()
        {
            if (UserSession.Instance == null) return;
            ModuleData mod = UserSession.Instance.GetModule(selectedModuleId);
            if (mod == null) return;

            if (selectedModuleId == "M1")
            {
                // Set active sub-module for completion screen
                SurakshaAR.Training.Fire.SubModuleCompletionScreen.ActiveSubModuleId = "M1_S1";
                // Open Fire Module Intro (Learn Mode) → leads to FireMineSimulator
                if (SceneNavigator.Instance != null)
                {
                    SceneNavigator.Instance.NavigateTo(ScreenState.FireModuleIntro);
                }
            }
            else if (selectedModuleId == "M2")
            {
                if (SceneNavigator.Instance != null)
                {
                    SceneNavigator.Instance.NavigateTo(ScreenState.GasModuleIntro);
                }
            }
        }

        private void OnSub2Clicked()
        {
            if (UserSession.Instance == null) return;
            ModuleData mod = UserSession.Instance.GetModule(selectedModuleId);
            if (mod == null) return;

            // Strict sequential check: Sub-module 1 must be completed
            if (!mod.sub1.isCompleted)
            {
                Debug.LogWarning("[Training] Cannot start Sub-module 2: Sub-module 1 is not completed.");
                return;
            }

            // For Module 1: Launch Petroleum Fire Simulator
            if (selectedModuleId == "M1")
            {
                SurakshaAR.Training.Fire.SubModuleCompletionScreen.ActiveSubModuleId = "M1_S2";
                if (SceneNavigator.Instance != null)
                {
                    SceneNavigator.Instance.NavigateTo(ScreenState.PetroleumFireSimulator);
                }
                return;
            }

            // For M2: Gas SCBA — no auto-completion
            if (!mod.sub2.isCompleted)
            {
                UserSession.Instance.CompleteSubModule(selectedModuleId, 2);
            }
        }

        private void OnSub3Clicked()
        {
            if (UserSession.Instance == null) return;
            ModuleData mod = UserSession.Instance.GetModule(selectedModuleId);
            if (mod == null) return;

            // Strict sequential check: Sub-module 2 must be completed
            if (!mod.sub2.isCompleted)
            {
                Debug.LogWarning("[Training] Cannot start Sub-module 3: Sub-module 2 is not completed.");
                return;
            }

            if (selectedModuleId == "M1")
            {
                // Explosion & Collision — dedicated explosion & progressive collapse simulation
                SurakshaAR.Training.Fire.SubModuleCompletionScreen.ActiveSubModuleId = "M1_S3";
                if (SceneNavigator.Instance != null)
                {
                    SceneNavigator.Instance.NavigateTo(ScreenState.ExplosionMineSimulator);
                }
                return;
            }

            // Complete sub3 and unlock Assessment Simulation
            if (!mod.sub3.isCompleted)
            {
                UserSession.Instance.CompleteSubModule(selectedModuleId, 3);
            }
        }

        private void OnAssessmentClicked()
        {
            if (UserSession.Instance == null) return;
            ModuleData mod = UserSession.Instance.GetModule(selectedModuleId);
            if (mod == null || !mod.AllSubModulesCompleted)
            {
                Debug.LogWarning("[Training] Cannot start Assessment: all 3 sub-modules must be completed first.");
                return;
            }

            if (selectedModuleId == "M1")
            {
                // Launch dedicated Fire Assessment Simulator
                if (SceneNavigator.Instance != null)
                {
                    SceneNavigator.Instance.NavigateTo(ScreenState.FireAssessmentSimulator);
                }
                else
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene("FireAssessmentSimulator");
                }
                return;
            }
            else
            {
                // Pass assessment for Gas
                UserSession.Instance.RecordAssessmentResult(selectedModuleId, true, 88f);
            }
        }

        public void RefreshUI()
        {
            var loc = LocalizationManager.Instance;
            if (headerTitleText != null && loc != null)
                headerTitleText.text = loc.GetString("NAV_TRAINING").ToUpper();
            if (headerSubheadText != null && loc != null)
                headerSubheadText.text = loc.GetString("MODULE_SELECT_HINT");

            // Refresh Top Cards (Level 1)
            UpdateTopCard(cardM1, "M1", "🔥 " + GetModuleTitle("M1", loc));
            UpdateTopCard(cardM2, "M2", "☣️ " + GetModuleTitle("M2", loc));
            UpdateTopCard(cardM3, "M3", "⚙️ " + GetModuleTitle("M3", loc));

            // Refresh Detail View (Level 2)
            UpdateDetailView();
        }

        private string GetModuleTitle(string modId, LocalizationManager loc)
        {
            if (loc != null)
            {
                if (modId == "M1") return loc.GetString("M1_TITLE");
                if (modId == "M2") return loc.GetString("M2_TITLE");
                if (modId == "M3") return loc.GetString("M3_TITLE");
            }
            if (modId == "M1") return "Fire & Explosion Response";
            if (modId == "M2") return "Gas Leak & Confined Space";
            return "Machinery Safety";
        }

        private void UpdateTopCard(TopModuleCardUI card, string modId, string defaultTitle)
        {
            if (card == null) return;
            var loc = LocalizationManager.Instance;
            ModuleData mod = UserSession.Instance != null ? UserSession.Instance.GetModule(modId) : null;

            if (card.titleText != null) card.titleText.text = defaultTitle;

            // Module 3: Completely locked on previous page itself
            if (modId == "M3")
            {
                if (card.statusText != null) card.statusText.text = "🔒 " + (loc != null ? loc.GetString("STATUS_LOCKED") : "LOCKED");
                if (card.statusBadge != null) card.statusBadge.color = new Color(0.40f, 0.45f, 0.55f); // Slate gray
                if (card.progressSummaryText != null) card.progressSummaryText.text = loc != null ? loc.GetString("MODULE_LOCKED_MSG") : "This module is locked.";
                if (card.openButton != null)
                {
                    card.openButton.interactable = false;
                    var btnImg = card.openButton.GetComponent<Image>();
                    if (btnImg != null) btnImg.color = new Color(0.60f, 0.65f, 0.72f);
                }
                if (card.openButtonText != null) card.openButtonText.text = "🔒 " + (loc != null ? loc.GetString("STATUS_LOCKED") : "LOCKED");
                return;
            }

            int completedSubs = 0;
            if (mod != null)
            {
                if (mod.sub1.isCompleted) completedSubs++;
                if (mod.sub2.isCompleted) completedSubs++;
                if (mod.sub3.isCompleted) completedSubs++;
            }

            if (card.progressSummaryText != null && loc != null)
            {
                card.progressSummaryText.text = string.Format(loc.GetString("SUBMODULES_COUNT_FMT"), completedSubs);
            }

            if (card.openButton != null)
            {
                card.openButton.interactable = true;
                var btnImg = card.openButton.GetComponent<Image>();
                if (btnImg != null) btnImg.color = new Color(0.97f, 0.45f, 0.09f); // Primary Orange
            }

            if (card.openButtonText != null && loc != null)
            {
                card.openButtonText.text = loc.GetString("OPEN_MODULE");
            }

            if (mod != null && card.statusText != null && card.statusBadge != null)
            {
                if (mod.assessmentStatus == "PASSED")
                {
                    card.statusText.text = "✓ " + (loc != null ? loc.GetString("STATUS_COMPLETED") : "COMPLETED");
                    card.statusBadge.color = new Color(0.06f, 0.73f, 0.51f); // Green
                }
                else if (mod.AllSubModulesCompleted)
                {
                    card.statusText.text = loc != null ? loc.GetString("STATUS_ASSESSMENT_AVAILABLE") : "ASSESSMENT AVAILABLE";
                    card.statusBadge.color = new Color(0.97f, 0.45f, 0.09f); // Orange
                }
                else if (completedSubs > 0)
                {
                    card.statusText.text = loc != null ? loc.GetString("STATUS_IN_PROGRESS") : "IN PROGRESS";
                    card.statusBadge.color = new Color(0.96f, 0.62f, 0.04f); // Amber
                }
                else
                {
                    card.statusText.text = loc != null ? loc.GetString("STATUS_NOT_STARTED") : "NOT STARTED";
                    card.statusBadge.color = new Color(0.40f, 0.45f, 0.55f); // Slate
                }
            }
        }

        private void UpdateDetailView()
        {
            var loc = LocalizationManager.Instance;
            ModuleData mod = UserSession.Instance != null ? UserSession.Instance.GetModule(selectedModuleId) : null;
            if (mod == null) return;

            // Detail Header
            if (detailModuleTitleText != null)
            {
                string icon = selectedModuleId == "M1" ? "🔥 " : (selectedModuleId == "M2" ? "☣️ " : "⚙️ ");
                detailModuleTitleText.text = icon + GetModuleTitle(selectedModuleId, loc);
            }

            if (detailModuleStatusText != null && detailModuleStatusBadge != null)
            {
                if (mod.assessmentStatus == "PASSED")
                {
                    detailModuleStatusText.text = "✓ " + (loc != null ? loc.GetString("STATUS_COMPLETED") : "COMPLETED");
                    detailModuleStatusBadge.color = new Color(0.06f, 0.73f, 0.51f);
                }
                else if (mod.AllSubModulesCompleted)
                {
                    detailModuleStatusText.text = loc != null ? loc.GetString("STATUS_ASSESSMENT_AVAILABLE") : "ASSESSMENT AVAILABLE";
                    detailModuleStatusBadge.color = new Color(0.97f, 0.45f, 0.09f);
                }
                else if (mod.sub1.isCompleted || mod.sub2.isCompleted || mod.sub3.isCompleted)
                {
                    detailModuleStatusText.text = loc != null ? loc.GetString("STATUS_IN_PROGRESS") : "IN PROGRESS";
                    detailModuleStatusBadge.color = new Color(0.96f, 0.62f, 0.04f);
                }
                else
                {
                    detailModuleStatusText.text = loc != null ? loc.GetString("STATUS_NOT_STARTED") : "NOT STARTED";
                    detailModuleStatusBadge.color = new Color(0.40f, 0.45f, 0.55f);
                }
            }

            // ── Sequential Sub-module Unlocking ──
            // Sub-module 1: Always Unlocked
            UpdateSubRow(
                sub1TitleText, sub1StatusText, sub1ActionButton, sub1ActionButtonText,
                $"1. {(loc != null ? loc.GetString(selectedModuleId + "_S1_TITLE") : mod.sub1.title)}",
                isCompleted: mod.sub1.isCompleted,
                isUnlocked: true
            );

            // Sub-module 2: Locked until Sub-module 1 is completed
            bool sub2Unlocked = mod.sub1.isCompleted;
            UpdateSubRow(
                sub2TitleText, sub2StatusText, sub2ActionButton, sub2ActionButtonText,
                $"2. {(loc != null ? loc.GetString(selectedModuleId + "_S2_TITLE") : mod.sub2.title)}",
                isCompleted: mod.sub2.isCompleted,
                isUnlocked: sub2Unlocked
            );

            // Sub-module 3: Locked until Sub-module 2 is completed
            bool sub3Unlocked = mod.sub2.isCompleted;
            UpdateSubRow(
                sub3TitleText, sub3StatusText, sub3ActionButton, sub3ActionButtonText,
                $"3. {(loc != null ? loc.GetString(selectedModuleId + "_S3_TITLE") : mod.sub3.title)}",
                isCompleted: mod.sub3.isCompleted,
                isUnlocked: sub3Unlocked
            );

            // ── Assessment Simulation Row ──
            bool assessUnlocked = mod.AllSubModulesCompleted;
            if (assessmentTitleText != null)
            {
                assessmentTitleText.text = "4. " + (loc != null ? loc.GetString("ASSESSMENT_SIMULATION") : "Assessment Simulation");
            }

            if (mod.assessmentStatus == "PASSED")
            {
                if (assessmentStatusText != null)
                {
                    assessmentStatusText.text = $"✓ {(loc != null ? loc.GetString("ASSESSMENT_PASSED") : "PASSED")} ({mod.assessmentScore:F0}%)";
                    assessmentStatusText.color = new Color(0.06f, 0.73f, 0.51f);
                }
                if (startAssessmentButton != null)
                {
                    startAssessmentButton.interactable = true;
                    var btnImg = startAssessmentButton.GetComponent<Image>();
                    if (btnImg != null) btnImg.color = new Color(0.20f, 0.25f, 0.33f);
                    if (startAssessmentButtonText != null) startAssessmentButtonText.text = "RETRY";
                }
            }
            else if (assessUnlocked)
            {
                if (assessmentStatusText != null)
                {
                    assessmentStatusText.text = loc != null ? loc.GetString("STATUS_ASSESSMENT_AVAILABLE") : "Assessment Available";
                    assessmentStatusText.color = new Color(0.97f, 0.45f, 0.09f);
                }
                if (startAssessmentButton != null)
                {
                    startAssessmentButton.interactable = true;
                    var btnImg = startAssessmentButton.GetComponent<Image>();
                    if (btnImg != null) btnImg.color = new Color(0.97f, 0.45f, 0.09f);
                    if (startAssessmentButtonText != null)
                        startAssessmentButtonText.text = (loc != null ? loc.GetString("START_ASSESSMENT") : "START ASSESSMENT") + " →";
                }
            }
            else
            {
                if (assessmentStatusText != null)
                {
                    string lockedDesc = loc != null ? loc.GetString("ASSESSMENT_LOCKED_DESC") : "Complete all 3 sub-modules to unlock assessment.";
                    assessmentStatusText.text = (loc != null ? loc.GetString("STATUS_LOCKED") : "Locked") + " (" + lockedDesc + ")";
                    assessmentStatusText.color = new Color(0.40f, 0.45f, 0.55f);
                }
                if (startAssessmentButton != null)
                {
                    startAssessmentButton.interactable = false;
                    var btnImg = startAssessmentButton.GetComponent<Image>();
                    if (btnImg != null) btnImg.color = new Color(0.60f, 0.65f, 0.72f);
                    if (startAssessmentButtonText != null) startAssessmentButtonText.text = loc != null ? loc.GetString("STATUS_LOCKED") : "Locked";
                }
            }

            // Card 4 remains the Assessment card; certificates are on the dedicated Certificates screen
            if (certificateRow != null)
            {
                certificateRow.SetActive(false);
            }
        }

        private void UpdateSubRow(Text titleText, Text statusText, Button actionBtn, Text btnText, string title, bool isCompleted, bool isUnlocked)
        {
            var loc = LocalizationManager.Instance;
            if (titleText != null) titleText.text = title;

            if (isCompleted)
            {
                if (statusText != null)
                {
                    statusText.text = "✓ " + (loc != null ? loc.GetString("STATUS_COMPLETED") : "Completed");
                    statusText.color = new Color(0.06f, 0.73f, 0.51f);
                }
                if (actionBtn != null)
                {
                    actionBtn.interactable = true;
                    if (btnText != null) btnText.text = loc != null ? loc.GetString("REVIEW") : "REVIEW";
                    var img = actionBtn.GetComponent<Image>();
                    if (img != null) img.color = new Color(0.20f, 0.25f, 0.33f);
                }
            }
            else if (isUnlocked)
            {
                if (statusText != null)
                {
                    statusText.text = loc != null ? loc.GetString("STATUS_NOT_STARTED") : "Not Started";
                    statusText.color = new Color(0.97f, 0.45f, 0.09f);
                }
                if (actionBtn != null)
                {
                    actionBtn.interactable = true;
                    if (btnText != null) btnText.text = "START →";
                    var img = actionBtn.GetComponent<Image>();
                    if (img != null) img.color = new Color(0.97f, 0.45f, 0.09f);
                }
            }
            else
            {
                // Locked
                if (statusText != null)
                {
                    statusText.text = loc != null ? loc.GetString("STATUS_LOCKED") : "Locked";
                    statusText.color = new Color(0.40f, 0.45f, 0.55f);
                }
                if (actionBtn != null)
                {
                    actionBtn.interactable = false;
                    if (btnText != null) btnText.text = loc != null ? loc.GetString("STATUS_LOCKED") : "Locked";
                    var img = actionBtn.GetComponent<Image>();
                    if (img != null) img.color = new Color(0.60f, 0.65f, 0.72f);
                }
            }
        }
    }
}
