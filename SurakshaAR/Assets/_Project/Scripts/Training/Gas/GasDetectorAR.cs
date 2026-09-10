using System;
using System.Collections;
using UnityEngine;

namespace SurakshaAR.Training.Gas
{
    public class GasDetectorAR : MonoBehaviour
    {
        [SerializeField] private string detectorName = "Multi-Gas Detector 4-X";
        public string DetectorName => detectorName;

        public bool IsScanning { get; private set; } = false;
        public bool HasScanned { get; private set; } = false;
        public SimulatedGasLevel LastReading { get; private set; } = SimulatedGasLevel.Safe;

        public event Action<SimulatedGasLevel, string> OnScanCompleted;

        public void PerformScan(SimulatedGasLevel scenarioSimulatedLevel, Action onFinished = null)
        {
            if (IsScanning) return;
            StartCoroutine(ScanRoutine(scenarioSimulatedLevel, onFinished));
        }

        private IEnumerator ScanRoutine(SimulatedGasLevel targetLevel, Action onFinished)
        {
            IsScanning = true;
            Debug.Log($"[GasDetectorAR] Starting simulated multi-gas scan routine...");

            yield return new WaitForSeconds(1.5f); // 1.5s simulated scan animation

            HasScanned = true;
            LastReading = targetLevel;
            IsScanning = false;

            string readingText = $"SIMULATED GAS DETECTOR READING: {targetLevel.ToString().ToUpper()} CONCENTRATION";
            Debug.Log($"[GasDetectorAR] Scan finished. Result: {readingText}");

            OnScanCompleted?.Invoke(targetLevel, readingText);
            onFinished?.Invoke();
        }
    }
}
