using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurakshaAR.UI.Data
{
    [System.Serializable]
    public class SubModuleItem
    {
        public string subId = "";
        public string title = "";
        public bool isCompleted = false;

        public SubModuleItem() { }
        public SubModuleItem(string id, string t, bool completed = false)
        {
            subId = id;
            title = t;
            isCompleted = completed;
        }
    }

    [System.Serializable]
    public class ModuleData
    {
        public string moduleId = "";          // "M1", "M2", "M3"
        public string title = "";
        public SubModuleItem sub1 = new SubModuleItem();
        public SubModuleItem sub2 = new SubModuleItem();
        public SubModuleItem sub3 = new SubModuleItem();
        public string assessmentStatus = "LOCKED"; // "LOCKED", "AVAILABLE", "PASSED", "FAILED"
        public float assessmentScore = 0f;
        public string certificateId = "";
        public string certificateIssueDate = "";

        public int CompletedSubModulesCount => (sub1.isCompleted ? 1 : 0) + (sub2.isCompleted ? 1 : 0) + (sub3.isCompleted ? 1 : 0);
        public bool AllSubModulesCompleted => sub1.isCompleted && sub2.isCompleted && sub3.isCompleted;
        public bool IsModuleCompleted => AllSubModulesCompleted && assessmentStatus == "PASSED" && !string.IsNullOrEmpty(certificateId);

        public string GetModuleOverallStatus()
        {
            if (IsModuleCompleted) return "COMPLETED";
            if (AllSubModulesCompleted) return "ASSESSMENT AVAILABLE";
            if (CompletedSubModulesCount > 0 || assessmentStatus == "FAILED") return "IN PROGRESS";
            return "NOT STARTED";
        }
    }

    [System.Serializable]
    public class CertificateRecord
    {
        public string certificateId = "";
        public string moduleId = "";          // "M1", "M2", "M3", "TOTAL"
        public string moduleTitle = "";
        public string workerName = "";
        public string workerId = "";
        public float score = 0f;
        public string issueDate = "";
        public string verificationUrl = "";
        public bool isTotalTraining = false;
    }

    [System.Serializable]
    public class WorkerRecord
    {
        public string workerId = "";
        public string workerName = "";
        public string selectedLanguage = "English";

        public ModuleData module1 = new ModuleData();
        public ModuleData module2 = new ModuleData();
        public ModuleData module3 = new ModuleData();

        public List<CertificateRecord> earnedCertificates = new List<CertificateRecord>();
        public List<string> legacyCertificates = new List<string>();
        public List<string> notifications = new List<string>();
    }

    /// <summary>
    /// Central manager for the active worker session, granular 3-module training progress,
    /// assessment gatekeeping, and dynamic certificates.
    /// Operates 100% offline with zero cross-contamination between workers.
    /// </summary>
    public class UserSession : MonoBehaviour
    {
        private static UserSession instance;
        public static UserSession Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<UserSession>();
                    if (instance == null)
                    {
                        var go = new GameObject("UserSession");
                        instance = go.AddComponent<UserSession>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        public string WorkerID { get; private set; } = "";
        public string WorkerName { get; private set; } = "";
        public string SelectedLanguage { get; private set; } = "English";

        public ModuleData Module1 { get; private set; } = new ModuleData();
        public ModuleData Module2 { get; private set; } = new ModuleData();
        public ModuleData Module3 { get; private set; } = new ModuleData();

        public bool FireModuleCompleted => Module1 != null && Module1.IsModuleCompleted;
        public bool GasModuleCompleted => Module2 != null && Module2.IsModuleCompleted;
        public bool MachineryModuleCompleted => Module3 != null && Module3.IsModuleCompleted;

        public int CompletedModules
        {
            get
            {
                int c = 0;
                if (Module1 != null && Module1.IsModuleCompleted) c++;
                if (Module2 != null && Module2.IsModuleCompleted) c++;
                if (Module3 != null && Module3.IsModuleCompleted) c++;
                return c;
            }
        }

        public int TotalModules => 3;

        public int CompletedSubModulesTotal
        {
            get
            {
                int c = 0;
                if (Module1 != null) c += Module1.CompletedSubModulesCount;
                if (Module2 != null) c += Module2.CompletedSubModulesCount;
                if (Module3 != null) c += Module3.CompletedSubModulesCount;
                return c;
            }
        }

        public int OverallProgressPercent
        {
            get
            {
                int completed = CompletedModules;
                if (completed <= 0) return 0;
                if (completed == 1) return 33;
                if (completed == 2) return 67;
                return 100;
            }
        }

        public float ProgressFraction => Mathf.Clamp01(OverallProgressPercent / 100f);

        public bool IsTotalTrainingCompleted => Module1.IsModuleCompleted && Module2.IsModuleCompleted && Module3.IsModuleCompleted;

        public bool IsModuleCompleted(string moduleId)
        {
            if (moduleId == "M1") return Module1 != null && Module1.IsModuleCompleted;
            if (moduleId == "M2") return Module2 != null && Module2.IsModuleCompleted;
            if (moduleId == "M3") return Module3 != null && Module3.IsModuleCompleted;
            if (moduleId == "TOTAL") return IsTotalTrainingCompleted;
            return false;
        }

        private List<CertificateRecord> earnedCertificates = new List<CertificateRecord>();
        public IReadOnlyList<CertificateRecord> EarnedCertificates => earnedCertificates;

        public CertificateRecord GetCertificateForModule(string moduleId)
        {
            if (moduleId == "TOTAL")
            {
                return earnedCertificates.Find(c => c.isTotalTraining);
            }
            return earnedCertificates.Find(c => c.moduleId == moduleId && !c.isTotalTraining);
        }

        public CertificateRecord GetOrCreateCertificate(string moduleId)
        {
            var cert = GetCertificateForModule(moduleId);
            if (cert != null) return cert;

            if (moduleId == "M1" && Module1 != null && Module1.IsModuleCompleted)
            {
                cert = new CertificateRecord
                {
                    certificateId = !string.IsNullOrEmpty(Module1.certificateId) ? Module1.certificateId : "SAR-2026-M1CERT",
                    moduleId = "M1",
                    moduleTitle = "Fire & Explosion Response Training Certificate",
                    workerName = WorkerName,
                    workerId = WorkerID,
                    score = Module1.assessmentScore > 0 ? Module1.assessmentScore : 95f,
                    issueDate = !string.IsNullOrEmpty(Module1.certificateIssueDate) ? Module1.certificateIssueDate : DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    verificationUrl = $"https://surakshaar.web.app/verify/{(!string.IsNullOrEmpty(Module1.certificateId) ? Module1.certificateId : "SAR-2026-M1CERT")}",
                    isTotalTraining = false
                };
                earnedCertificates.Add(cert);
                return cert;
            }
            if (moduleId == "M2" && Module2 != null && Module2.IsModuleCompleted)
            {
                cert = new CertificateRecord
                {
                    certificateId = !string.IsNullOrEmpty(Module2.certificateId) ? Module2.certificateId : "SAR-2026-M2CERT",
                    moduleId = "M2",
                    moduleTitle = "Gas Leak & Confined Space Protocol Training Certificate",
                    workerName = WorkerName,
                    workerId = WorkerID,
                    score = Module2.assessmentScore > 0 ? Module2.assessmentScore : 88f,
                    issueDate = !string.IsNullOrEmpty(Module2.certificateIssueDate) ? Module2.certificateIssueDate : DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    verificationUrl = $"https://surakshaar.web.app/verify/{(!string.IsNullOrEmpty(Module2.certificateId) ? Module2.certificateId : "SAR-2026-M2CERT")}",
                    isTotalTraining = false
                };
                earnedCertificates.Add(cert);
                return cert;
            }
            if (moduleId == "M3" && Module3 != null && Module3.IsModuleCompleted)
            {
                cert = new CertificateRecord
                {
                    certificateId = !string.IsNullOrEmpty(Module3.certificateId) ? Module3.certificateId : "SAR-2026-M3CERT",
                    moduleId = "M3",
                    moduleTitle = "Machinery Safety Training Certificate",
                    workerName = WorkerName,
                    workerId = WorkerID,
                    score = Module3.assessmentScore > 0 ? Module3.assessmentScore : 90f,
                    issueDate = !string.IsNullOrEmpty(Module3.certificateIssueDate) ? Module3.certificateIssueDate : DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    verificationUrl = $"https://surakshaar.web.app/verify/{(!string.IsNullOrEmpty(Module3.certificateId) ? Module3.certificateId : "SAR-2026-M3CERT")}",
                    isTotalTraining = false
                };
                earnedCertificates.Add(cert);
                return cert;
            }
            if (moduleId == "TOTAL" && IsTotalTrainingCompleted)
            {
                cert = new CertificateRecord
                {
                    certificateId = "SAR-TOTAL-CERT",
                    moduleId = "TOTAL",
                    moduleTitle = "Total Industrial Safety Training Completion Certificate",
                    workerName = WorkerName,
                    workerId = WorkerID,
                    score = 92f,
                    issueDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    verificationUrl = "https://surakshaar.web.app/verify/SAR-TOTAL",
                    isTotalTraining = true
                };
                earnedCertificates.Add(cert);
                return cert;
            }
            return null;
        }

        // Legacy compatibility string list
        public IReadOnlyList<string> Certificates
        {
            get
            {
                var list = new List<string>();
                foreach (var cert in earnedCertificates)
                {
                    list.Add($"{cert.moduleTitle} Certificate ({cert.certificateId})");
                }
                return list;
            }
        }

        private List<string> notifications = new List<string>();
        public IReadOnlyList<string> Notifications => notifications;

        public event Action OnSessionUpdated;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            int dataVer = PlayerPrefs.GetInt("SurakshaAR_DataVersion_V4", 0);
            if (dataVer < 1)
            {
                // Clear any old mock/dummy completed submodules from PlayerPrefs
                string activeId = PlayerPrefs.GetString("SurakshaAR_ActiveWorkerID", "");
                if (!string.IsNullOrEmpty(activeId))
                {
                    PlayerPrefs.DeleteKey($"SurakshaAR_Worker_{activeId.ToUpper()}");
                    PlayerPrefs.DeleteKey($"SurakshaAR_Worker_{activeId}");
                }
                PlayerPrefs.DeleteKey("SurakshaAR_Worker_JH-TEST001");
                PlayerPrefs.DeleteKey("SurakshaAR_Worker_JH-10284");
                PlayerPrefs.DeleteKey("SurakshaAR_Worker_TEST");
                PlayerPrefs.DeleteKey("SurakshaAR_Worker_");
                PlayerPrefs.DeleteKey("SurakshaAR_ActiveWorkerID");
                PlayerPrefs.SetInt("SurakshaAR_DataVersion_V4", 1);
                PlayerPrefs.Save();
            }

            InitializeDefaultModules();
            LoadLastSession();
        }

        private void InitializeDefaultModules()
        {
            Module1 = new ModuleData
            {
                moduleId = "M1",
                title = "Fire & Explosion Response",
                sub1 = new SubModuleItem("M1_S1", "Fire & Extinguisher for electric fire"),
                sub2 = new SubModuleItem("M1_S2", "Fire & mud for petroleum fire"),
                sub3 = new SubModuleItem("M1_S3", "Explosion & collision"),
                assessmentStatus = "LOCKED"
            };

            Module2 = new ModuleData
            {
                moduleId = "M2",
                title = "Gas Leak & Confined Space Protocol",
                sub1 = new SubModuleItem("M2_S1", "Gas Detection & Atmospheric Testing"),
                sub2 = new SubModuleItem("M2_S2", "PPE & Self-Contained Self-Rescuer (SCSR)"),
                sub3 = new SubModuleItem("M2_S3", "Confined Space Isolation & Extraction Protocol"),
                assessmentStatus = "LOCKED"
            };

            Module3 = new ModuleData
            {
                moduleId = "M3",
                title = "Machinery Safety",
                sub1 = new SubModuleItem("M3_S1", "Heavy Mobile Equipment & Blind Spot Awareness"),
                sub2 = new SubModuleItem("M3_S2", "Conveyor Belt & Pinch Point LOTO"),
                sub3 = new SubModuleItem("M3_S3", "Haul Road & Machinery Pre-Start Inspection"),
                assessmentStatus = "LOCKED"
            };
        }

        public void LoadLastSession()
        {
            string lastId = PlayerPrefs.GetString("SurakshaAR_ActiveWorkerID", "");
            if (!string.IsNullOrEmpty(lastId))
            {
                LoadWorkerData(lastId);
            }
        }

        public void SetWorkerProfile(string workerId, string workerName)
        {
            if (string.IsNullOrEmpty(workerId) || string.IsNullOrEmpty(workerName))
            {
                Debug.LogWarning("[UserSession] Worker ID and Name cannot be empty.");
                return;
            }

            WorkerID = workerId.Trim();
            WorkerName = workerName.Trim();

            LoadWorkerData(WorkerID);
            WorkerName = workerName.Trim(); // retain casing

            PlayerPrefs.SetString("SurakshaAR_ActiveWorkerID", WorkerID);
            SaveWorkerData();

            Debug.Log($"[UserSession] Active Worker Session: ID={WorkerID}, Name={WorkerName}, Completed={CompletedModules}/{TotalModules}, Progress={OverallProgressPercent}%");
            OnSessionUpdated?.Invoke();
        }

        public void SetLanguage(string languageName)
        {
            SelectedLanguage = languageName;
            SaveWorkerData();
            OnSessionUpdated?.Invoke();
        }

        public ModuleData GetModule(string moduleId)
        {
            if (moduleId == "M1" || moduleId.Contains("Fire")) return Module1;
            if (moduleId == "M2" || moduleId.Contains("Gas")) return Module2;
            if (moduleId == "M3" || moduleId.Contains("Machinery")) return Module3;
            return null;
        }

        /// <summary>
        /// Completes an individual sub-module.
        /// When all 3 sub-modules of a module are finished, unlocks the Assessment Simulation.
        /// Does NOT mark the module as completed yet.
        /// </summary>
        public void CompleteSubModule(string moduleId, int subIndex)
        {
            ModuleData m = GetModule(moduleId);
            if (m == null) return;

            if (subIndex == 1) m.sub1.isCompleted = true;
            else if (subIndex == 2) m.sub2.isCompleted = true;
            else if (subIndex == 3) m.sub3.isCompleted = true;

            // Check if all 3 sub-modules are completed
            if (m.AllSubModulesCompleted && m.assessmentStatus == "LOCKED")
            {
                m.assessmentStatus = "AVAILABLE";
                AddNotification($"{m.title}: All 3 sub-modules completed! Assessment simulation unlocked.");
            }

            SaveWorkerData();
            Debug.Log($"[UserSession] Sub-module {subIndex} of {m.title} completed. Assessment Status: {m.assessmentStatus}. Overall: {CompletedModules}/{TotalModules}");
            OnSessionUpdated?.Invoke();
        }

        /// <summary>
        /// Records assessment simulation attempt result.
        /// If PASSED:
        ///   - marks module completed
        ///   - generates module certificate
        ///   - if all 3 modules completed, generates Total Training Certificate
        /// If FAILED:
        ///   - marks assessment status as FAILED
        ///   - module remains incomplete (no certificate generated)
        /// </summary>
        public void RecordAssessmentResult(string moduleId, bool passed, float score = 85f)
        {
            ModuleData m = GetModule(moduleId);
            if (m == null) return;

            m.assessmentScore = score;

            if (passed)
            {
                m.assessmentStatus = "PASSED";
                string dateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
                m.certificateIssueDate = dateStr;

                if (string.IsNullOrEmpty(m.certificateId))
                {
                    string uniqueHash = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
                    m.certificateId = $"SAR-2026-{uniqueHash}";
                }

                // Add or update module certificate
                string certTitle = $"{m.title} Training Certificate";
                var certRec = earnedCertificates.Find(c => c.moduleId == m.moduleId && !c.isTotalTraining);
                if (certRec == null)
                {
                    certRec = new CertificateRecord
                    {
                        certificateId = m.certificateId,
                        moduleId = m.moduleId,
                        moduleTitle = certTitle,
                        workerName = WorkerName,
                        workerId = WorkerID,
                        score = score,
                        issueDate = dateStr,
                        verificationUrl = $"https://surakshaar.web.app/verify/{m.certificateId}",
                        isTotalTraining = false
                    };
                    earnedCertificates.Add(certRec);
                }

                AddNotification($"🎉 {m.title} completed with score {score:F0}%! Certificate issued.");

                // Check for Total Training Completion Certificate
                CheckTotalTrainingCertificate();
            }
            else
            {
                m.assessmentStatus = "FAILED";
                AddNotification($"⚠️ {m.title} assessment incomplete. Score: {score:F0}%. Review sub-modules and retry.");
            }

            SaveWorkerData();
            Debug.Log($"[UserSession] Assessment for {m.title}: Passed={passed}, Score={score:F0}%. Completed Modules: {CompletedModules}/3 ({OverallProgressPercent}%)");
            OnSessionUpdated?.Invoke();
        }

        private void CheckTotalTrainingCertificate()
        {
            if (IsTotalTrainingCompleted)
            {
                var totalCert = earnedCertificates.Find(c => c.isTotalTraining);
                if (totalCert == null)
                {
                    string uniqueHash = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
                    string dateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
                    float avgScore = (Module1.assessmentScore + Module2.assessmentScore + Module3.assessmentScore) / 3f;

                    totalCert = new CertificateRecord
                    {
                        certificateId = $"SAR-TOTAL-{uniqueHash}",
                        moduleId = "TOTAL",
                        moduleTitle = "Total Industrial Safety Training Completion Certificate",
                        workerName = WorkerName,
                        workerId = WorkerID,
                        score = avgScore,
                        issueDate = dateStr,
                        verificationUrl = $"https://surakshaar.web.app/verify/SAR-TOTAL-{uniqueHash}",
                        isTotalTraining = true
                    };
                    earnedCertificates.Add(totalCert);
                    AddNotification("🏆 Congratulations! You have completed all 3 modules and earned the Total Training Completion Certificate!");
                }
            }
            else
            {
                // Remove total certificate if any module became incomplete
                earnedCertificates.RemoveAll(c => c.isTotalTraining);
            }
        }

        // Backward compatibility method
        public void CompleteModule(string moduleName, float score = 85f)
        {
            string modId = "M1";
            if (moduleName.Contains("Gas")) modId = "M2";
            else if (moduleName.Contains("Machinery")) modId = "M3";

            // Complete all 3 sub-modules then pass assessment
            CompleteSubModule(modId, 1);
            CompleteSubModule(modId, 2);
            CompleteSubModule(modId, 3);
            RecordAssessmentResult(modId, true, score);
        }

        public void AddNotification(string notif)
        {
            if (!notifications.Contains(notif))
            {
                notifications.Insert(0, notif);
            }
        }

        public void Logout()
        {
            WorkerID = "";
            WorkerName = "";
            InitializeDefaultModules();
            earnedCertificates.Clear();
            notifications.Clear();
            PlayerPrefs.DeleteKey("SurakshaAR_ActiveWorkerID");
            PlayerPrefs.Save();
            Debug.Log("[UserSession] User logged out. Active session cleared.");
            OnSessionUpdated?.Invoke();
        }

        private void LoadWorkerData(string id)
        {
            string key = $"SurakshaAR_Worker_{id.ToUpper()}";
            InitializeDefaultModules();
            earnedCertificates = new List<CertificateRecord>();
            notifications = new List<string>();

            if (PlayerPrefs.HasKey(key))
            {
                string json = PlayerPrefs.GetString(key, "");
                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        var record = JsonUtility.FromJson<WorkerRecord>(json);
                        if (record != null)
                        {
                            WorkerID = record.workerId;
                            WorkerName = record.workerName;
                            SelectedLanguage = string.IsNullOrEmpty(record.selectedLanguage) ? "English" : record.selectedLanguage;

                            if (record.module1 != null && !string.IsNullOrEmpty(record.module1.moduleId))
                            {
                                Module1 = record.module1;
                                if (Module1.sub1 != null) Module1.sub1.title = "Fire & Extinguisher for electric fire";
                                if (Module1.sub2 != null) Module1.sub2.title = "Fire & mud for petroleum fire";
                                if (Module1.sub3 != null) Module1.sub3.title = "Explosion & collision";
                            }
                            if (record.module2 != null && !string.IsNullOrEmpty(record.module2.moduleId))
                                Module2 = record.module2;
                            if (record.module3 != null && !string.IsNullOrEmpty(record.module3.moduleId))
                                Module3 = record.module3;

                            earnedCertificates = record.earnedCertificates ?? new List<CertificateRecord>();
                            notifications = record.notifications ?? new List<string>();

                            CheckTotalTrainingCertificate();
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[UserSession] Failed to deserialize worker data: {ex.Message}");
                    }
                }
            }

            // Fresh worker profile
            WorkerID = id;
            notifications = new List<string>
            {
                "Welcome to SurakshaAR! Begin with Module 1: Fire & Explosion Response."
            };
        }

        private void SaveWorkerData()
        {
            if (string.IsNullOrEmpty(WorkerID)) return;

            var record = new WorkerRecord
            {
                workerId = WorkerID,
                workerName = WorkerName,
                selectedLanguage = SelectedLanguage,
                module1 = Module1,
                module2 = Module2,
                module3 = Module3,
                earnedCertificates = earnedCertificates,
                notifications = notifications
            };

            string json = JsonUtility.ToJson(record);
            string key = $"SurakshaAR_Worker_{WorkerID.ToUpper()}";
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }

        public string GetFormattedWorkerLabel()
        {
            if (string.IsNullOrEmpty(WorkerName)) return "Worker";
            return $"{WorkerName} ({WorkerID})";
        }
    }
}
