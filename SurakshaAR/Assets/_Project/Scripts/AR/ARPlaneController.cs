using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SurakshaAR.AR
{
    /// <summary>
    /// Manages AR plane detection mode, surface discovery events, 
    /// and tracking performance limits for mid-range Android hardware.
    /// </summary>
    public class ARPlaneController : MonoBehaviour
    {
        [Header("Plane Manager Settings")]
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private int maxActivePlanes = 8;

        public event Action OnFirstPlaneDetected;
        public bool HasDetectedPlane { get; private set; }

        private void Awake()
        {
            if (planeManager == null)
            {
                planeManager = GetComponent<ARPlaneManager>();
            }
        }

        private void OnEnable()
        {
            if (planeManager != null)
            {
                planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
                planeManager.trackablesChanged.AddListener(OnTrackablesChanged);
            }
        }

        private void OnDisable()
        {
            if (planeManager != null)
            {
                planeManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
            }
        }

        private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARPlane> eventArgs)
        {
            if (!HasDetectedPlane && eventArgs.added.Count > 0)
            {
                HasDetectedPlane = true;
                OnFirstPlaneDetected?.Invoke();
            }

            // Limit active trackable visualizers for performance budget
            int activeCount = 0;
            foreach (var plane in planeManager.trackables)
            {
                activeCount++;
                if (activeCount > maxActivePlanes)
                {
                    plane.gameObject.SetActive(false);
                }
            }
        }

        public void SetPlaneDetectionEnabled(bool isEnabled)
        {
            if (planeManager != null)
            {
                planeManager.enabled = isEnabled;
                foreach (var plane in planeManager.trackables)
                {
                    plane.gameObject.SetActive(isEnabled);
                }
            }
        }
    }
}
