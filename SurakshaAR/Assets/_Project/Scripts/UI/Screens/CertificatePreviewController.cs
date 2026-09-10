using UnityEngine;
using UnityEngine.UI;
using SurakshaAR.UI.Data;
using SurakshaAR.UI.Navigation;
using SurakshaAR.Localization;

namespace SurakshaAR.UI.Screens
{
    public class CertificatePreviewController : MonoBehaviour
    {
        [Header("Certificate Fields")]
        [SerializeField] private Text headerTitleText;
        [SerializeField] private Text workerNameText;
        [SerializeField] private Text moduleText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text dateText;

        [Header("Action Buttons")]
        [SerializeField] private Button viewCertificateButton;
        [SerializeField] private Button verifyQrButton;

        private void OnEnable()
        {
            if (UserSession.Instance != null)
            {
                UserSession.Instance.OnSessionUpdated += UpdateCertificateData;
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
                UserSession.Instance.OnSessionUpdated -= UpdateCertificateData;
            }
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
            }
        }

        private void Start()
        {
            UpdateCertificateData();
            WireButtons();
        }

        private void HandleLanguageChanged(Language lang)
        {
            UpdateCertificateData();
        }

        private void UpdateCertificateData()
        {
            var loc = LocalizationManager.Instance;
            var session = UserSession.Instance;

            if (headerTitleText != null)
            {
                headerTitleText.text = loc != null ? loc.GetString("CERTIFICATE_HEADER") : "SURAKSHAAR\nSafety Training Certificate";
            }
            
            string workerName = session != null && !string.IsNullOrEmpty(session.WorkerName) ? session.WorkerName : "Worker";
            string workerId = session != null && !string.IsNullOrEmpty(session.WorkerID) ? session.WorkerID : "---";

            if (workerNameText != null)
            {
                workerNameText.text = $"{workerName} ({workerId})";
            }

            bool completed = session != null && session.FireModuleCompleted;
            if (moduleText != null)
            {
                string modTitle = loc != null ? loc.GetString("FIRE_MODULE_TITLE") : "Fire & Explosion Response";
                moduleText.text = completed ? $"🔥 {modTitle}" : (loc != null ? loc.GetString("NO_CERTIFICATES_YET") : "No certificates yet");
            }

            if (scoreText != null)
            {
                scoreText.text = completed ? "Score: 100%" : "Score: --";
            }

            if (dateText != null)
            {
                string datePrefix = loc != null ? loc.GetString("COMPLETION_DATE") : "Completion Date";
                dateText.text = completed ? $"{datePrefix}: {System.DateTime.Now:dd MMM yyyy}" : $"{datePrefix}: --";
            }
        }

        private void WireButtons()
        {
            if (viewCertificateButton != null)
            {
                viewCertificateButton.onClick.AddListener(() =>
                {
                    Debug.Log("[CertificatePreview] View Certificate clicked.");
                });
            }

            if (verifyQrButton != null)
            {
                verifyQrButton.onClick.AddListener(() =>
                {
                    Debug.Log("[CertificatePreview] Verify QR clicked.");
                });
            }
        }
    }
}
