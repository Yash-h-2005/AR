using System.Collections;
using UnityEngine;

namespace SurakshaAR.Training.Fire
{
    /// <summary>
    /// Manages intermittent emergency audio alarms and phone haptic vibration pulses.
    /// Operates on configurable intervals to prevent continuous jarring vibration while keeping worker alerted.
    /// </summary>
    public class AudioHapticManager : MonoBehaviour
    {
        public static AudioHapticManager Instance { get; private set; }

        [Header("Audio Settings")]
        [SerializeField] private AudioSource alarmAudioSource;

        [Header("Intermittent Vibration Intervals (Seconds)")]
        [SerializeField] private float highSeverityInterval = 1.5f;
        [SerializeField] private float mediumSeverityInterval = 4.0f;
        [SerializeField] private float lowSeverityInterval = 7.0f;

        private Coroutine alertCoroutine;
        private bool isAlertActive = false;

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

        public void StartEmergencyAlert(int severityLevel = 2) // 1=High, 2=Medium, 3=Low
        {
            StopEmergencyAlert();

            isAlertActive = true;
            float interval = severityLevel switch
            {
                1 => highSeverityInterval,
                3 => lowSeverityInterval,
                _ => mediumSeverityInterval
            };

            alertCoroutine = StartCoroutine(AlertLoop(interval));
            Debug.Log($"[AudioHapticManager] Emergency Alert Started. Severity: {severityLevel}, Interval: {interval}s");
        }

        public void StopEmergencyAlert()
        {
            isAlertActive = false;
            if (alertCoroutine != null)
            {
                StopCoroutine(alertCoroutine);
                alertCoroutine = null;
            }

            if (alarmAudioSource != null && alarmAudioSource.isPlaying)
            {
                alarmAudioSource.Stop();
            }

            Debug.Log("[AudioHapticManager] Emergency Alert Stopped.");
        }

        private IEnumerator AlertLoop(float interval)
        {
            while (isAlertActive)
            {
                // Play short alarm sound pulse
                if (alarmAudioSource != null)
                {
                    alarmAudioSource.Play();
                }

                // Trigger Android haptic vibration pulse
#if UNITY_ANDROID && !UNITY_EDITOR
                Handheld.Vibrate();
#endif

                yield return new WaitForSeconds(interval);
            }
        }
    }
}
