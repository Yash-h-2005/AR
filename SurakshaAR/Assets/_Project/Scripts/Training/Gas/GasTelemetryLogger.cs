using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace SurakshaAR.Training.Gas
{
    [Serializable]
    public class GasTelemetryEvent
    {
        public string timestamp;
        public string stepName;
        public string actionName;
        public bool isSuccess;
        public string mistakeCategory;
        public int hintTierUsed;
        public string simulatedGasLevel;
        public bool ppeSelectionCorrect;
        public bool detectorScanned;
        public bool buddyVerified;
        public float userDisplacementMeters;
        public int scoreDelta;
    }

    [Serializable]
    public class GasSessionTelemetry
    {
        public string sessionId;
        public string workerId;
        public string moduleName = "Gas Leak & Confined Space";
        public string startTime;
        public string endTime;
        public string difficultyProfile;
        public int overallScore;
        public float independentAccuracyPercent;
        public float assistedAccuracyPercent;
        public int totalMistakes;
        public int totalRetries;
        public int sequenceErrors;
        public List<GasTelemetryEvent> events = new List<GasTelemetryEvent>();
    }

    public class GasTelemetryLogger : MonoBehaviour
    {
        public static GasTelemetryLogger Instance { get; private set; }

        public GasSessionTelemetry CurrentSession { get; private set; }

        private string telemetryDirectory;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            telemetryDirectory = Path.Combine(Application.persistentDataPath, "Telemetry", "Gas");
            if (!Directory.Exists(telemetryDirectory))
            {
                Directory.CreateDirectory(telemetryDirectory);
            }
        }

        public void StartSession(string workerId, string difficultyProfile)
        {
            CurrentSession = new GasSessionTelemetry
            {
                sessionId = Guid.NewGuid().ToString("N").Substring(0, 8),
                workerId = string.IsNullOrEmpty(workerId) ? "WRK_OFFLINE" : workerId,
                startTime = DateTime.UtcNow.ToString("o"),
                difficultyProfile = difficultyProfile
            };
            Debug.Log($"[GasTelemetryLogger] Started session {CurrentSession.sessionId} for worker {CurrentSession.workerId}");
        }

        public void LogEvent(GasTelemetryEvent evt)
        {
            if (CurrentSession == null)
            {
                StartSession("WRK_OFFLINE", "BEGINNER");
            }

            evt.timestamp = DateTime.UtcNow.ToString("o");
            CurrentSession.events.Add(evt);
            Debug.Log($"[GasTelemetryLogger] Event logged: {evt.stepName} - {evt.actionName} (Success: {evt.isSuccess})");
        }

        public void EndSession(int finalScore, float independentAcc, float assistedAcc, int mistakes, int retries, int sequenceErrors)
        {
            if (CurrentSession == null) return;

            CurrentSession.endTime = DateTime.UtcNow.ToString("o");
            CurrentSession.overallScore = finalScore;
            CurrentSession.independentAccuracyPercent = independentAcc;
            CurrentSession.assistedAccuracyPercent = assistedAcc;
            CurrentSession.totalMistakes = mistakes;
            CurrentSession.totalRetries = retries;
            CurrentSession.sequenceErrors = sequenceErrors;

            SaveSessionToFile();
        }

        private void SaveSessionToFile()
        {
            try
            {
                string json = JsonUtility.ToJson(CurrentSession, true);
                string filePath = Path.Combine(telemetryDirectory, $"GasSession_{CurrentSession.sessionId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
                File.WriteAllText(filePath, json);
                Debug.Log($"[GasTelemetryLogger] Session saved offline to: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GasTelemetryLogger] Failed to save telemetry offline: {ex.Message}");
            }
        }
    }
}
