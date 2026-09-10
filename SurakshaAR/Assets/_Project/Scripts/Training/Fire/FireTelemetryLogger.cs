using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SurakshaAR.UI.Data;

namespace SurakshaAR.Training.Fire
{
    [Serializable]
    public class StepTelemetryData
    {
        public int stepId;
        public string stepName;
        public float timeSpentSeconds;
        public int mistakes;
        public int retries;
        public int hintsUsed;
        public int examplesUsed;
        public int practiceUsed;
        public bool independentSuccess;
    }

    [Serializable]
    public class SessionTelemetryData
    {
        public string sessionId;
        public string workerId;
        public string moduleId;
        public string mode;
        public string startTime;
        public string endTime;
        public float totalDurationSeconds;
        public int overallScore;
        public bool isCompleted;
        public List<StepTelemetryData> steps = new List<StepTelemetryData>();
    }

    /// <summary>
    /// Offline-first telemetry recorder. Writes formatted session JSON logs directly 
    /// to device local storage (Application.persistentDataPath/Telemetry/) without network calls.
    /// </summary>
    public class FireTelemetryLogger : MonoBehaviour
    {
        public static FireTelemetryLogger Instance { get; private set; }

        private SessionTelemetryData currentSession;
        private float stepStartTime;
        private float sessionStartTime;

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

        public void StartNewSession(string moduleId = "MOD_FIRE_01", string mode = "LEARN")
        {
            string workerId = UserSession.Instance != null ? UserSession.Instance.WorkerID : "WRK001";
            string sessionId = $"SES_{DateTime.Now:yyyyMMdd_HHmmss}";

            currentSession = new SessionTelemetryData
            {
                sessionId = sessionId,
                workerId = workerId,
                moduleId = moduleId,
                mode = mode,
                startTime = DateTime.UtcNow.ToString("o"),
                isCompleted = false
            };

            sessionStartTime = Time.time;
            stepStartTime = Time.time;

            Debug.Log($"[FireTelemetryLogger] Session Started: {sessionId} for Worker: {workerId}");
        }

        public void RecordStepCompletion(TrainingStepType step, int mistakes, int retries, int hints, int examples, int practice, bool independentSuccess)
        {
            if (currentSession == null) StartNewSession();

            float timeSpent = Time.time - stepStartTime;
            stepStartTime = Time.time;

            StepTelemetryData stepData = new StepTelemetryData
            {
                stepId = (int)step,
                stepName = step.ToString(),
                timeSpentSeconds = Mathf.Round(timeSpent * 10f) / 10f,
                mistakes = mistakes,
                retries = retries,
                hintsUsed = hints,
                examplesUsed = examples,
                practiceUsed = practice,
                independentSuccess = independentSuccess
            };

            currentSession.steps.Add(stepData);
            Debug.Log($"[FireTelemetryLogger] Step Completed: {step} in {timeSpent:F1}s (Mistakes: {mistakes}, Retries: {retries})");
        }

        public void FinalizeSession(int finalScore)
        {
            if (currentSession == null) return;

            currentSession.endTime = DateTime.UtcNow.ToString("o");
            currentSession.totalDurationSeconds = Mathf.Round((Time.time - sessionStartTime) * 10f) / 10f;
            currentSession.overallScore = finalScore;
            currentSession.isCompleted = true;

            SaveTelemetryToFile();
        }

        private void SaveTelemetryToFile()
        {
            try
            {
                string dirPath = Path.Combine(Application.persistentDataPath, "Telemetry");
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                string filePath = Path.Combine(dirPath, $"{currentSession.sessionId}.json");
                string jsonString = JsonUtility.ToJson(currentSession, true);

                File.WriteAllText(filePath, jsonString);
                PlayerPrefs.SetString("SurakshaAR_LastSessionJson", jsonString);
                PlayerPrefs.Save();

                Debug.Log($"[FireTelemetryLogger] Telemetry Saved Offline to: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FireTelemetryLogger] Failed to save telemetry: {ex.Message}");
            }
        }
    }
}
