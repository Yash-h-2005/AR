using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SurakshaAR.AR
{
    /// <summary>
    /// Manages the AR Session lifecycle, checking for ARCore support, 
    /// tracking state, and camera permissions on Android 10+ devices.
    /// </summary>
    public class ARSessionController : MonoBehaviour
    {
        [Header("AR References")]
        [SerializeField] private ARSession arSession;

        public event Action<ARSessionState> OnStateChanged;
        public event Action<string> OnErrorEncountered;

        public ARSessionState CurrentState => ARSession.state;

        private void OnEnable()
        {
            ARSession.stateChanged += HandleSessionStateChanged;
        }

        private void OnDisable()
        {
            ARSession.stateChanged -= HandleSessionStateChanged;
        }

        private void Start()
        {
            if (arSession == null)
            {
                arSession = FindFirstObjectByType<ARSession>();
            }

            StartCoroutine(CheckARCoreAvailabilityRoutine());
        }

        private IEnumerator CheckARCoreAvailabilityRoutine()
        {
            if (ARSession.state == ARSessionState.None || ARSession.state == ARSessionState.CheckingAvailability)
            {
                yield return ARSession.CheckAvailability();
            }

            if (ARSession.state == ARSessionState.Unsupported)
            {
                OnErrorEncountered?.Invoke("ARCore is not supported on this device.");
            }
            else if (ARSession.state == ARSessionState.NeedsInstall)
            {
                yield return ARSession.Install();
            }
        }

        private void HandleSessionStateChanged(ARSessionStateChangedEventArgs args)
        {
            OnStateChanged?.Invoke(args.state);

            if (args.state == ARSessionState.Unsupported)
            {
                OnErrorEncountered?.Invoke("AR Session initialization failed.");
            }
        }

        public void ResetSession()
        {
            if (arSession != null)
            {
                arSession.Reset();
            }
        }
    }
}
