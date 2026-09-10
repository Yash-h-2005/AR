using UnityEngine;
using UnityEngine.UI;
using SurakshaAR.UI.Navigation;
using SurakshaAR.UI.Data;
using SurakshaAR.Localization;

namespace SurakshaAR.UI.Screens
{
    public class LoginScreenController : MonoBehaviour
    {
        [Header("Form Inputs")]
        [SerializeField] private InputField workerNameInput;
        [SerializeField] private InputField workerIdInput;

        [Header("Buttons")]
        [SerializeField] private Button loginButton;
        [SerializeField] private Button backButton;

        [Header("Labels & Text")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private Text workerNameLabel;
        [SerializeField] private Text workerIdLabel;
        [SerializeField] private Text loginButtonText;
        [SerializeField] private Text errorBannerText;
        [SerializeField] private Text helpFooterText;

        private void Start()
        {
            if (loginButton != null)
            {
                loginButton.onClick.AddListener(OnLoginClicked);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
            }

            if (errorBannerText != null)
            {
                errorBannerText.gameObject.SetActive(false);
            }

            // Pre-fill active session if returning
            if (UserSession.Instance != null)
            {
                if (workerNameInput != null && !string.IsNullOrEmpty(UserSession.Instance.WorkerName))
                {
                    workerNameInput.text = UserSession.Instance.WorkerName;
                }
                if (workerIdInput != null && !string.IsNullOrEmpty(UserSession.Instance.WorkerID))
                {
                    workerIdInput.text = UserSession.Instance.WorkerID;
                }
            }

            ApplyLocalization();

            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
            }
        }

        private void OnDestroy()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
            }
        }

        private void HandleLanguageChanged(Language lang)
        {
            ApplyLocalization();
        }

        public void ApplyLocalization()
        {
            if (LocalizationManager.Instance == null) return;

            if (titleText != null) titleText.text = LocalizationManager.Instance.GetString("LOGIN");
            if (subtitleText != null) subtitleText.text = LocalizationManager.Instance.GetString("LOGIN_SUBTITLE");
            if (workerNameLabel != null) workerNameLabel.text = LocalizationManager.Instance.GetString("WORKER_NAME");
            if (workerIdLabel != null) workerIdLabel.text = LocalizationManager.Instance.GetString("WORKER_ID");
            if (loginButtonText != null) loginButtonText.text = LocalizationManager.Instance.GetString("LOGIN");
            if (helpFooterText != null) helpFooterText.text = LocalizationManager.Instance.GetString("NEED_HELP");

            if (workerNameInput != null && workerNameInput.placeholder is Text pName)
            {
                pName.text = LocalizationManager.Instance.GetString("ENTER_WORKER_NAME");
            }
            if (workerIdInput != null && workerIdInput.placeholder is Text pId)
            {
                pId.text = LocalizationManager.Instance.GetString("ENTER_WORKER_ID");
            }
        }

        private void OnLoginClicked()
        {
            string workerName = workerNameInput != null ? workerNameInput.text.Trim() : string.Empty;
            string workerId = workerIdInput != null ? workerIdInput.text.Trim() : string.Empty;

            if (string.IsNullOrEmpty(workerName) && string.IsNullOrEmpty(workerId))
            {
                ShowError(LocalizationManager.Instance != null 
                    ? LocalizationManager.Instance.GetString("NAME_AND_ID_REQUIRED") 
                    : "Worker Name and Worker ID are required!");
                return;
            }

            if (string.IsNullOrEmpty(workerName))
            {
                ShowError(LocalizationManager.Instance != null 
                    ? LocalizationManager.Instance.GetString("WORKER_NAME_REQUIRED") 
                    : "Worker Name is required!");
                return;
            }

            if (string.IsNullOrEmpty(workerId))
            {
                ShowError(LocalizationManager.Instance != null 
                    ? LocalizationManager.Instance.GetString("WORKER_ID_REQUIRED") 
                    : "Worker ID is required!");
                return;
            }

            // Save to current user session
            if (UserSession.Instance != null)
            {
                UserSession.Instance.SetWorkerProfile(workerId, workerName);
            }

            // Hide error
            if (errorBannerText != null)
            {
                errorBannerText.gameObject.SetActive(false);
            }

            // Navigate to Language Selection
            if (SceneNavigator.Instance != null)
            {
                SceneNavigator.Instance.NavigateTo(ScreenState.LanguageSelection);
            }
        }

        private void OnBackClicked()
        {
            if (SceneNavigator.Instance != null)
            {
                SceneNavigator.Instance.GoBack();
            }
        }

        private void ShowError(string message)
        {
            if (errorBannerText != null)
            {
                errorBannerText.text = message;
                errorBannerText.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"[Login] Validation Error: {message}");
            }
        }
    }
}
