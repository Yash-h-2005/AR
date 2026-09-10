using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using SurakshaAR.Assessment;
using SurakshaAR.Data;

namespace SurakshaAR.Certification
{
    [Serializable]
    public class DigitalCertificate
    {
        public string certificateId;       // e.g. SAR-2026-A1B2C3
        public string workerId;
        public string workerName;
        public string moduleName;
        public float score;
        public string issueDate;
        public string expiryDate;
        public string status;              // "VALID", "REVOKED", "EXPIRED"
        public string verificationUrl;
        public bool isOnlineVerified;
    }

    public class CertificateManager : MonoBehaviour
    {
        public static CertificateManager Instance { get; private set; }

        public DigitalCertificate LatestCertificate { get; private set; }

        private string certificatesDirectory;
        private readonly Dictionary<string, DigitalCertificate> localCertificates = new Dictionary<string, DigitalCertificate>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            certificatesDirectory = Path.Combine(Application.persistentDataPath, "Certificates");
            if (!Directory.Exists(certificatesDirectory))
            {
                Directory.CreateDirectory(certificatesDirectory);
            }

            LoadLocalCertificates();
        }

        public DigitalCertificate GenerateCertificate(AssessmentResult result)
        {
            if (result == null || result.status != AssessmentStatus.PASSED)
            {
                Debug.LogWarning("[CertificateManager] Cannot generate valid certificate for failed or incomplete assessment.");
                return null;
            }

            string uniqueHash = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
            string certId = $"SAR-2026-{uniqueHash}";
            string verifyUrl = $"https://surakshaar.web.app/verify/{certId}";

            DigitalCertificate cert = new DigitalCertificate
            {
                certificateId = certId,
                workerId = result.workerId,
                workerName = result.workerName,
                moduleName = result.moduleName,
                score = result.totalScore,
                issueDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                expiryDate = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd"),
                status = "VALID",
                verificationUrl = verifyUrl,
                isOnlineVerified = false
            };

            LatestCertificate = cert;
            localCertificates[certId] = cert;

            SaveCertificateOffline(cert);

            if (WorkerProfile.Instance != null)
            {
                WorkerProfile.Instance.RegisterCompletedModule(cert.moduleName, (int)cert.score, cert.certificateId);
            }

            Debug.Log($"[CertificateManager] Generated valid digital certificate {certId} for {cert.workerName}");
            return cert;
        }

        public DigitalCertificate GetCertificate(string certId)
        {
            if (string.IsNullOrEmpty(certId)) return null;
            if (localCertificates.TryGetValue(certId, out var cert)) return cert;

            string filePath = Path.Combine(certificatesDirectory, $"{certId}.json");
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    DigitalCertificate loadedCert = JsonUtility.FromJson<DigitalCertificate>(json);
                    if (loadedCert != null)
                    {
                        localCertificates[certId] = loadedCert;
                        return loadedCert;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CertificateManager] Error loading certificate {certId}: {ex.Message}");
                }
            }
            return null;
        }

        public List<DigitalCertificate> GetAllCertificates()
        {
            return new List<DigitalCertificate>(localCertificates.Values);
        }

        private void SaveCertificateOffline(DigitalCertificate cert)
        {
            try
            {
                string json = JsonUtility.ToJson(cert, true);
                string filePath = Path.Combine(certificatesDirectory, $"{cert.certificateId}.json");
                File.WriteAllText(filePath, json);
                Debug.Log($"[CertificateManager] Saved certificate offline to: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CertificateManager] Failed to save certificate offline: {ex.Message}");
            }
        }

        private void LoadLocalCertificates()
        {
            try
            {
                string[] files = Directory.GetFiles(certificatesDirectory, "SAR-2026-*.json");
                foreach (var file in files)
                {
                    string json = File.ReadAllText(file);
                    DigitalCertificate cert = JsonUtility.FromJson<DigitalCertificate>(json);
                    if (cert != null && !string.IsNullOrEmpty(cert.certificateId))
                    {
                        localCertificates[cert.certificateId] = cert;
                        if (LatestCertificate == null) LatestCertificate = cert;
                    }
                }
                Debug.Log($"[CertificateManager] Loaded {localCertificates.Count} offline certificates.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CertificateManager] Error reading local certificates directory: {ex.Message}");
            }
        }
    }
}
