using UnityEngine;
using UnityEngine.UI;
using SurakshaAR.UI.Navigation;
using SurakshaAR.UI.Data;
using SurakshaAR.Localization;

namespace SurakshaAR.UI.Components
{
    public class SideDrawerController : MonoBehaviour
    {
        [Header("Drawer Panels")]
        [SerializeField] private GameObject drawerContainer;
        [SerializeField] private RectTransform drawerPanel;
        [SerializeField] private Button backdropButton;

        [Header("Profile Labels")]
        [SerializeField] private Text avatarInitialText;
        [SerializeField] private Text profileNameText;
        [SerializeField] private Text workerIdText;

        [Header("Navigation Buttons")]
        [SerializeField] private Button homeButton;
        [SerializeField] private Button trainingButton;
        [SerializeField] private Button certificatesButton;
        [SerializeField] private Button profileButton;
        [SerializeField] private Button offlineTrainingButton;
        [SerializeField] private Button achievementsButton;
        [SerializeField] private Button settingsButton;

        public bool IsOpen { get; private set; } = false;

        private void OnEnable()
        {
            if (UserSession.Instance != null)
            {
                UserSession.Instance.OnSessionUpdated += UpdateProfileInfo;
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
                UserSession.Instance.OnSessionUpdated -= UpdateProfileInfo;
            }
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
            }
        }

        private void Start()
        {
            if (backdropButton != null)
            {
                backdropButton.onClick.AddListener(CloseDrawer);
            }

            WireNavButtons();
            UpdateProfileInfo();
            SetDrawerVisible(false);
        }

        private void HandleLanguageChanged(Language lang)
        {
            UpdateProfileInfo();
        }

        public void UpdateProfileInfo()
        {
            var session = UserSession.Instance;
            var loc = LocalizationManager.Instance;

            string name = session != null && !string.IsNullOrEmpty(session.WorkerName) ? session.WorkerName : "Worker";
            string id = session != null && !string.IsNullOrEmpty(session.WorkerID) ? session.WorkerID : "---";

            if (profileNameText != null) profileNameText.text = name;
            if (workerIdText != null)
            {
                string idFmt = loc != null ? loc.GetString("WORKER_ID_FORMAT") : "Worker ID: {0}";
                workerIdText.text = string.Format(idFmt, id);
            }

            if (avatarInitialText != null && !string.IsNullOrEmpty(name))
            {
                avatarInitialText.text = name.Substring(0, 1).ToUpper();
            }
        }

        private void WireNavButtons()
        {
            if (homeButton != null) homeButton.onClick.AddListener(() => NavigateAndClose(ScreenState.Home));
            if (trainingButton != null) trainingButton.onClick.AddListener(() => NavigateAndClose(ScreenState.TrainingModules));
            if (certificatesButton != null) certificatesButton.onClick.AddListener(() => NavigateAndClose(ScreenState.CertificatePreview));
            if (profileButton != null) profileButton.onClick.AddListener(() => NavigateAndClose(ScreenState.Profile));
            if (offlineTrainingButton != null) offlineTrainingButton.onClick.AddListener(() => NavigateAndClose(ScreenState.TrainingModules));
            if (achievementsButton != null) achievementsButton.onClick.AddListener(() => NavigateAndClose(ScreenState.PerformanceSummary));
            if (settingsButton != null) settingsButton.onClick.AddListener(() => NavigateAndClose(ScreenState.Settings));
        }

        public void ToggleDrawer()
        {
            if (IsOpen) CloseDrawer();
            else OpenDrawer();
        }

        public void OpenDrawer()
        {
            IsOpen = true;
            UpdateProfileInfo();
            if (drawerContainer != null)
            {
                drawerContainer.transform.SetAsLastSibling();
            }
            SetDrawerVisible(true);
        }

        public void CloseDrawer()
        {
            IsOpen = false;
            SetDrawerVisible(false);
        }

        private void SetDrawerVisible(bool visible)
        {
            if (drawerContainer != null)
            {
                drawerContainer.SetActive(visible);
            }
        }

        private void NavigateAndClose(ScreenState targetScreen)
        {
            CloseDrawer();
            if (SceneNavigator.Instance != null)
            {
                SceneNavigator.Instance.NavigateTo(targetScreen);
            }
        }
    }
}
