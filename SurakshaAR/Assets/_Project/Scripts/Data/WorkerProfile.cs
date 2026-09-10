using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace SurakshaAR.Data
{
    [Serializable]
    public class WorkerProfileData
    {
        public string workerId = "";
        public string workerName = "";
        public string selectedLanguage = "en";
        public int overallScore = 0;
        public List<string> completedModules = new List<string>();
        public List<string> activeCertificates = new List<string>(); // Certificate IDs
        public string lastTrainingDate = "";
        public string skillStatus = "GOOD";
    }

    public class WorkerProfile : MonoBehaviour
    {
        public static WorkerProfile Instance { get; private set; }

        public WorkerProfileData Data { get; private set; } = new WorkerProfileData();

        private string profileFilePath;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            profileFilePath = Path.Combine(Application.persistentDataPath, "WorkerProfile.json");
            LoadProfile();
        }

        public void LoadProfile()
        {
            if (File.Exists(profileFilePath))
            {
                try
                {
                    string json = File.ReadAllText(profileFilePath);
                    Data = JsonUtility.FromJson<WorkerProfileData>(json) ?? new WorkerProfileData();
                    Debug.Log($"[WorkerProfile] Loaded profile for worker: {Data.workerId} ({Data.workerName})");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WorkerProfile] Failed to load profile: {ex.Message}");
                    Data = new WorkerProfileData();
                }
            }
            else
            {
                Data = new WorkerProfileData();
                SaveProfile();
            }
        }

        public void SaveProfile()
        {
            try
            {
                string json = JsonUtility.ToJson(Data, true);
                File.WriteAllText(profileFilePath, json);
                Debug.Log($"[WorkerProfile] Saved profile to: {profileFilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorkerProfile] Failed to save profile: {ex.Message}");
            }
        }

        public void UpdateWorkerInfo(string id, string name)
        {
            if (!string.IsNullOrEmpty(id)) Data.workerId = id;
            if (!string.IsNullOrEmpty(name)) Data.workerName = name;
            SaveProfile();
        }

        public void RegisterCompletedModule(string moduleName, int score, string certificateId = null)
        {
            if (!Data.completedModules.Contains(moduleName))
            {
                Data.completedModules.Add(moduleName);
            }

            if (!string.IsNullOrEmpty(certificateId) && !Data.activeCertificates.Contains(certificateId))
            {
                Data.activeCertificates.Add(certificateId);
            }

            Data.overallScore = Mathf.RoundToInt((Data.overallScore + score) / 2f);
            Data.lastTrainingDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
            SaveProfile();
        }
    }
}
