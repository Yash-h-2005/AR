using UnityEngine;
using UnityEngine.UI;
using SurakshaAR.UI.Navigation;
using SurakshaAR.Certification;

namespace SurakshaAR.UI.Screens
{
    public class QRVerificationController : MonoBehaviour
    {
        [Header("Verification Search UI")]
        [SerializeField] private InputField certIdInputField;
        [SerializeField] private Button verifySearchButton;
        [SerializeField] private Button backButton;

        [Header("Result Card Overlay")]
        [SerializeField] private GameObject resultCardPanel;
        [SerializeField] private Text resultStatusText;
        [SerializeField] private Text certDetailsText;

        private void Start()
        {
            if (verifySearchButton != null)
            {
                verifySearchButton.onClick.AddListener(OnVerifySearchClicked);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
            }

            if (resultCardPanel != null)
            {
                resultCardPanel.SetActive(false);
            }

            // Auto-populate latest certificate ID if available
            if (CertificateManager.Instance != null && CertificateManager.Instance.LatestCertificate != null)
            {
                if (certIdInputField != null)
                {
                    certIdInputField.text = CertificateManager.Instance.LatestCertificate.certificateId;
                }
            }
        }

        private void OnVerifySearchClicked()
        {
            string queryId = certIdInputField != null ? certIdInputField.text.Trim() : "";
            if (string.IsNullOrEmpty(queryId))
            {
                ShowVerificationResult("INVALID INPUT", "Please enter a valid Certificate ID (e.g. SAR-2026-XXXXXX).", false);
                return;
            }

            DigitalCertificate cert = CertificateManager.Instance != null ? CertificateManager.Instance.GetCertificate(queryId) : null;

            if (cert != null)
            {
                if (cert.status == "REVOKED")
                {
                    ShowVerificationResult("⚠ CERTIFICATE REVOKED", $"Certificate ID: {cert.certificateId}\nWorker: {cert.workerName}\nStatus: REVOKED by Compliance Admin", false);
                }
                else if (cert.status == "VALID")
                {
                    string details = $"Worker: {cert.workerName}\nWorker ID: {cert.workerId}\nModule: {cert.moduleName}\nScore: {cert.score:F1}%\nIssue Date: {cert.issueDate}\nVerification: Authentic Local/Online Record";
                    ShowVerificationResult("✓ CERTIFICATE VERIFIED", details, true);
                }
                else
                {
                    ShowVerificationResult("EXPIRED", $"Certificate ID: {cert.certificateId}\nStatus: EXPIRED", false);
                }
            }
            else
            {
                ShowVerificationResult("✖ NOT FOUND", $"No local or online certificate record found matching ID: {queryId}", false);
            }
        }

        private void ShowVerificationResult(string header, string details, bool isValid)
        {
            if (resultCardPanel != null)
            {
                resultCardPanel.SetActive(true);
            }

            if (resultStatusText != null)
            {
                resultStatusText.text = header;
                resultStatusText.color = isValid ? new Color(0.06f, 0.73f, 0.51f) : Color.red;
            }

            if (certDetailsText != null)
            {
                certDetailsText.text = details;
            }
        }

        private void OnBackClicked()
        {
            if (SceneNavigator.Instance != null)
            {
                SceneNavigator.Instance.GoBack();
            }
        }
    }
}
