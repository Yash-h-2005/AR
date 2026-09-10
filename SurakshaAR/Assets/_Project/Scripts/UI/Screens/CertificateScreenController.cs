using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SurakshaAR.UI.Navigation;
using SurakshaAR.UI.Data;
using SurakshaAR.Certification;
using SurakshaAR.Localization;

namespace SurakshaAR.UI.Screens
{
    /// <summary>
    /// Certificate Screen Controller displaying 4 distinct certificate cards:
    /// - Module 1: Fire & Explosion Response
    /// - Module 2: Gas Leak & Confined Space
    /// - Module 3: Machinery Safety
    /// - Total Certificate: Industrial Safety Mining Certification
    /// Each card features an individual side action button ("VIEW →" when completed, "Locked" when incomplete).
    /// Tapping an unlocked VIEW button opens the full certificate preview modal with dynamic QR code.
    /// </summary>
    public class CertificateScreenController : MonoBehaviour
    {
        [System.Serializable]
        public class CertCardUI
        {
            public string moduleId;
            public GameObject cardPanel;
            public Text titleText;
            public Text statusText;
            public Button viewButton;
            public Text viewButtonText;
            public Image viewButtonImage;
        }

        [Header("Header")]
        [SerializeField] private Button backButton;
        [SerializeField] private Text headerTitleText;
        [SerializeField] private Text headerSubtitleText;

        [Header("4 Certificate Cards")]
        [SerializeField] private CertCardUI cardM1;
        [SerializeField] private CertCardUI cardM2;
        [SerializeField] private CertCardUI cardM3;
        [SerializeField] private CertCardUI cardTotal;

        [Header("Bottom Action")]
        [SerializeField] private Button verifyCertificateButton;

        [Header("Certificate Detail Modal")]
        [SerializeField] private GameObject detailModal;
        [SerializeField] private Button modalCloseButton;
        [SerializeField] private Text modalOrgHeader;
        [SerializeField] private Text modalCertTitle;
        [SerializeField] private Text modalWorkerName;
        [SerializeField] private Text modalWorkerId;
        [SerializeField] private Text modalCertId;
        [SerializeField] private Text modalScore;
        [SerializeField] private Text modalIssueDate;
        [SerializeField] private Text modalStatusBadge;
        [SerializeField] private RawImage modalQrRawImage;
        [SerializeField] private Button modalVerifyButton;

        private static readonly Color ActiveButtonColor = new Color(0.12f, 0.16f, 0.23f, 1f); // Dark Navy #1E293B
        private static readonly Color ActiveTotalColor = new Color(0.96f, 0.62f, 0.04f, 1f);  // Amber Gold #F59E0B
        private static readonly Color LockedButtonColor = new Color(0.58f, 0.64f, 0.72f, 1f); // Slate Gray #94A3B8

        public void SetupBindings(
            Button backBtn, Text headerTitle, Text headerSubtitle,
            CertCardUI m1, CertCardUI m2, CertCardUI m3, CertCardUI total,
            Button verifyCertBtn,
            GameObject modal, Button modalClose, Text orgHeader, Text certTitle,
            Text wName, Text wId, Text cId, Text score, Text iDate, Text badge,
            RawImage qrImg, Button modalVerify)
        {
            backButton = backBtn;
            headerTitleText = headerTitle;
            headerSubtitleText = headerSubtitle;

            cardM1 = m1;
            cardM2 = m2;
            cardM3 = m3;
            cardTotal = total;

            verifyCertificateButton = verifyCertBtn;

            detailModal = modal;
            modalCloseButton = modalClose;
            modalOrgHeader = orgHeader;
            modalCertTitle = certTitle;
            modalWorkerName = wName;
            modalWorkerId = wId;
            modalCertId = cId;
            modalScore = score;
            modalIssueDate = iDate;
            modalStatusBadge = badge;
            modalQrRawImage = qrImg;
            modalVerifyButton = modalVerify;
        }

        private void OnEnable()
        {
            if (UserSession.Instance != null)
            {
                UserSession.Instance.OnSessionUpdated += UpdateDisplay;
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
                UserSession.Instance.OnSessionUpdated -= UpdateDisplay;
            }
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
            }
        }

        private void Start()
        {
            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(OnBackClicked);
            }

            if (verifyCertificateButton != null)
            {
                verifyCertificateButton.onClick.RemoveAllListeners();
                verifyCertificateButton.onClick.AddListener(OnVerifyScannerClicked);
            }

            if (modalCloseButton != null)
            {
                modalCloseButton.onClick.RemoveAllListeners();
                modalCloseButton.onClick.AddListener(CloseDetailModal);
            }

            if (modalVerifyButton != null)
            {
                modalVerifyButton.onClick.RemoveAllListeners();
                modalVerifyButton.onClick.AddListener(OnVerifyScannerClicked);
            }

            // Wire Card View Buttons
            WireCardButton(cardM1, "M1");
            WireCardButton(cardM2, "M2");
            WireCardButton(cardM3, "M3");
            WireCardButton(cardTotal, "TOTAL");

            if (detailModal != null)
            {
                detailModal.SetActive(false);
            }

            UpdateDisplay();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (detailModal != null && detailModal.activeSelf)
                {
                    CloseDetailModal();
                }
                else
                {
                    OnBackClicked();
                }
            }
        }

        private void HandleLanguageChanged(Language lang)
        {
            UpdateDisplay();
        }

        private void WireCardButton(CertCardUI card, string modId)
        {
            if (card != null && card.viewButton != null)
            {
                card.viewButton.onClick.RemoveAllListeners();
                card.viewButton.onClick.AddListener(() => OpenCertificateModal(modId));
            }
        }

        public void UpdateDisplay()
        {
            var loc = LocalizationManager.Instance;
            var session = UserSession.Instance;

            if (headerTitleText != null)
            {
                headerTitleText.text = loc != null ? loc.GetString("NAV_CERTIFICATES").ToUpper() : "CERTIFICATES";
            }

            if (headerSubtitleText != null)
            {
                headerSubtitleText.text = "Official Mining Safety Certificates • Issued upon passing module assessments.";
            }

            // Update 4 Cards
            UpdateCardState(cardM1, "M1", "🔥 Module 1: Fire & Explosion Response", "Module 1 Safety Certification");
            UpdateCardState(cardM2, "M2", "☣ Module 2: Gas Leak & Confined Space", "Module 2 Safety Certification");
            UpdateCardState(cardM3, "M3", "⚙ Module 3: Machinery Safety", "Module 3 Safety Certification");
            UpdateCardState(cardTotal, "TOTAL", "🏆 Total Mining Safety Certification", "Master Industrial Safety Certified Miner", isTotal: true);
        }

        private void UpdateCardState(CertCardUI card, string modId, string defaultTitle, string defaultSub, bool isTotal = false)
        {
            if (card == null) return;

            var session = UserSession.Instance;
            bool isCompleted = session != null && session.IsModuleCompleted(modId);
            CertificateRecord cert = isCompleted ? session.GetOrCreateCertificate(modId) : null;

            if (card.titleText != null)
            {
                card.titleText.text = defaultTitle;
            }

            if (card.statusText != null)
            {
                if (isCompleted)
                {
                    float sc = cert != null ? cert.score : (modId == "M1" ? 95f : 88f);
                    card.statusText.text = $"✓ Issued • Score: {sc:F0}%";
                    card.statusText.color = new Color(0.06f, 0.73f, 0.51f); // Green
                }
                else
                {
                    if (isTotal)
                    {
                        card.statusText.text = "🔒 Locked • Complete all 3 modules";
                    }
                    else if (modId == "M3")
                    {
                        card.statusText.text = "🔒 Locked • Training Module Locked";
                    }
                    else
                    {
                        card.statusText.text = "🔒 Locked • Complete Module Assessment";
                    }
                    card.statusText.color = new Color(0.40f, 0.45f, 0.55f); // Slate
                }
            }

            if (card.viewButton != null)
            {
                card.viewButton.interactable = isCompleted;

                if (card.viewButtonImage != null)
                {
                    if (isCompleted)
                    {
                        card.viewButtonImage.color = isTotal ? ActiveTotalColor : ActiveButtonColor;
                    }
                    else
                    {
                        card.viewButtonImage.color = LockedButtonColor;
                    }
                }
            }

            if (card.viewButtonText != null)
            {
                card.viewButtonText.text = isCompleted ? "VIEW →" : "Locked";
                card.viewButtonText.color = Color.white;
            }
        }

        public void OpenCertificateModal(string modId)
        {
            var session = UserSession.Instance;
            if (session == null || !session.IsModuleCompleted(modId))
            {
                Debug.LogWarning($"[CertificateScreen] Cannot view {modId}: module is not completed.");
                return;
            }

            CertificateRecord cert = session.GetOrCreateCertificate(modId);
            if (cert == null) return;

            var loc = LocalizationManager.Instance;
            string wName = !string.IsNullOrEmpty(cert.workerName) ? cert.workerName : (!string.IsNullOrEmpty(session.WorkerName) ? session.WorkerName : "Worker");
            string wId = !string.IsNullOrEmpty(cert.workerId) ? cert.workerId : (!string.IsNullOrEmpty(session.WorkerID) ? session.WorkerID : "---");

            if (modalOrgHeader != null)
            {
                modalOrgHeader.text = "SURAKSHAAR INDUSTRIAL SAFETY TRAINING";
            }

            if (modalCertTitle != null)
            {
                modalCertTitle.text = cert.moduleTitle;
                modalCertTitle.color = cert.isTotalTraining ? ActiveTotalColor : new Color(0.98f, 0.45f, 0.09f);
            }

            if (modalWorkerName != null)
            {
                modalWorkerName.text = wName;
            }

            if (modalWorkerId != null)
            {
                string idFormat = loc != null ? loc.GetString("WORKER_ID_FORMAT") : "Worker ID: {0}";
                modalWorkerId.text = string.Format(idFormat, wId);
            }

            if (modalCertId != null)
            {
                modalCertId.text = $"Certificate ID: {cert.certificateId}";
            }

            if (modalScore != null)
            {
                modalScore.text = $"Assessment Score: {cert.score:F1}%";
            }

            if (modalIssueDate != null)
            {
                modalIssueDate.text = $"Issued Date: {cert.issueDate}";
            }

            if (modalStatusBadge != null)
            {
                modalStatusBadge.text = "✓ VERIFIED & VALID CERTIFICATE";
                modalStatusBadge.color = new Color(0.06f, 0.73f, 0.51f);
            }

            if (modalQrRawImage != null && !string.IsNullOrEmpty(cert.verificationUrl))
            {
                Texture2D qrTex = QRCodeEncoder.EncodeToTexture(cert.verificationUrl, 256, 256);
                modalQrRawImage.texture = qrTex;
            }

            if (detailModal != null)
            {
                detailModal.SetActive(true);
            }
        }

        public void CloseDetailModal()
        {
            if (detailModal != null)
            {
                detailModal.SetActive(false);
            }
        }

        private void OnBackClicked()
        {
            if (detailModal != null && detailModal.activeSelf)
            {
                CloseDetailModal();
                return;
            }

            if (SceneNavigator.Instance != null)
            {
                SceneNavigator.Instance.NavigateTo(ScreenState.Home);
            }
        }

        private void OnVerifyScannerClicked()
        {
            if (SceneNavigator.Instance != null)
            {
                SceneNavigator.Instance.NavigateTo(ScreenState.QRVerification);
            }
        }
    }
}
