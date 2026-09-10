using UnityEngine;
using UnityEngine.UI;
using SurakshaAR.UI.Data;
using SurakshaAR.UI.Navigation;
using SurakshaAR.Localization;

namespace SurakshaAR.UI.Screens
{
    public class ProfileScreenController : MonoBehaviour
    {
        [Header("Worker Profile Fields")]
        [SerializeField] private Text workerNameText;
        [SerializeField] private Text workerIdText;
        [SerializeField] private Text completedModulesText;
        [SerializeField] private Text overallPerformanceText;

        [Header("Language Buttons")]
        [SerializeField] private Button englishButton;
        [SerializeField] private Button hindiButton;
        [SerializeField] private Button santaliButton;
        [SerializeField] private Button settingsButton;

        private void OnEnable()
        {
            if (UserSession.Instance != null)
            {
                UserSession.Instance.OnSessionUpdated += UpdateProfile;
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
                UserSession.Instance.OnSessionUpdated -= UpdateProfile;
            }
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
            }
        }

        private void Start()
        {
            UpdateProfile();
            WireButtons();
        }

        private void HandleLanguageChanged(Language lang)
        {
            UpdateProfile();
        }

        private void UpdateProfile()
        {
            var session = UserSession.Instance;
            var loc = LocalizationManager.Instance;

            string name = session != null && !string.IsNullOrEmpty(session.WorkerName) ? session.WorkerName : "Worker";
            string id = session != null && !string.IsNullOrEmpty(session.WorkerID) ? session.WorkerID : "---";

            if (workerNameText != null) workerNameText.text = name;
            if (workerIdText != null) 
            {
                string idFormat = loc != null ? loc.GetString("WORKER_ID_FORMAT") : "Worker ID: {0}";
                workerIdText.text = string.Format(idFormat, id);
            }

            int completed = session != null ? session.CompletedModules : 0;
            int total = session != null ? session.TotalModules : 3;

            if (completedModulesText != null)
            {
                string subFormat = loc != null ? loc.GetString("MODULES_COMPLETED_FORMAT") : "{0} of {1} modules completed";
                completedModulesText.text = string.Format(subFormat, completed, total);
            }

            if (overallPerformanceText != null)
            {
                int pct = total > 0 ? Mathf.RoundToInt(((float)completed / total) * 100f) : 0;
                overallPerformanceText.text = $"{pct}%";
            }
        }

        private void WireButtons()
        {
            if (englishButton != null) englishButton.onClick.AddListener(() => SetLang(Language.English));
            if (hindiButton != null) hindiButton.onClick.AddListener(() => SetLang(Language.Hindi));
            if (santaliButton != null) santaliButton.onClick.AddListener(() => SetLang(Language.Santali));

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(() =>
                {
                    if (SceneNavigator.Instance != null)
                        SceneNavigator.Instance.NavigateTo(ScreenState.Settings);
                });
            }
        }

        private void SetLang(Language lang)
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.SetLanguage(lang);
            }
            if (UserSession.Instance != null)
            {
                UserSession.Instance.SetLanguage(lang.ToString());
            }
        }
    }
}
