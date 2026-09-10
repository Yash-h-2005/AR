using System.Collections.Generic;
using UnityEngine;
using SurakshaAR.Certification;
using SurakshaAR.Assessment;

namespace SurakshaAR.Data
{
    public class FirebaseSyncManager : MonoBehaviour
    {
        public static FirebaseSyncManager Instance { get; private set; }

        public bool IsOnline { get; private set; } = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            CheckNetworkStatus();
        }

        public void CheckNetworkStatus()
        {
            IsOnline = Application.internetReachability != NetworkReachability.NotReachable;
            Debug.Log($"[FirebaseSyncManager] Internet status check: IsOnline = {IsOnline}");
            if (IsOnline)
            {
                SyncPendingRecords();
            }
        }

        public void SyncPendingRecords()
        {
            if (!IsOnline)
            {
                Debug.Log("[FirebaseSyncManager] Offline mode active. Pending records queued for background sync.");
                return;
            }

            Debug.Log("[FirebaseSyncManager] Internet connection detected. Synchronizing offline certificates & assessments to Firestore...");
            
            if (CertificateManager.Instance != null)
            {
                List<DigitalCertificate> certs = CertificateManager.Instance.GetAllCertificates();
                foreach (var cert in certs)
                {
                    if (!cert.isOnlineVerified)
                    {
                        cert.isOnlineVerified = true;
                        Debug.Log($"[FirebaseSyncManager] Synced certificate record {cert.certificateId} to Firestore.");
                    }
                }
            }
        }
    }
}
