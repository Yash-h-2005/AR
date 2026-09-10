using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SurakshaAR.AR
{
    /// <summary>
    /// Manages real-world ground scanning via ARPlaneManager and surface tap placement via ARRaycastManager.
    /// Establishes the real-world TRAINING_ORIGIN on the detected ground plane, spawns spatial training objects,
    /// and anchors them using ARAnchorManager to prevent drift.
    /// </summary>
    public class ARTrainingPlacementManager : MonoBehaviour
    {
        public static ARTrainingPlacementManager Instance { get; private set; }

        [Header("AR Foundation References")]
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private ARAnchorManager anchorManager;
        [SerializeField] private Camera arCamera;

        [Header("State Tracking")]
        public bool HasDetectedGround { get; private set; } = false;
        public bool IsOriginEstablished { get; private set; } = false;
        public Vector3 TrainingOriginPosition { get; private set; } = Vector3.zero;

        public event Action OnGroundPlaneDetected;
        public event Action<Vector3> OnTrainingOriginPlaced;

        private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
        private ARAnchor rootAnchor;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (planeManager == null) planeManager = FindFirstObjectByType<ARPlaneManager>();
            if (raycastManager == null) raycastManager = FindFirstObjectByType<ARRaycastManager>();
            if (anchorManager == null) anchorManager = FindFirstObjectByType<ARAnchorManager>();
            if (arCamera == null) arCamera = Camera.main;
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
            if (!HasDetectedGround && eventArgs.added.Count > 0)
            {
                HasDetectedGround = true;
                Debug.Log("[ARTrainingPlacementManager] Real-world ground plane detected!");
                OnGroundPlaneDetected?.Invoke();
            }
        }

        public bool TryPlaceTrainingOriginFromTap(Vector2 touchPosition, out Pose hitPose, out ARPlane hitPlane)
        {
            hitPose = default;
            hitPlane = null;

            if (raycastManager == null) return false;

            if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
            {
                hitPose = hits[0].pose;
                hitPlane = planeManager != null ? planeManager.GetPlane(hits[0].trackableId) : null;

                EstablishTrainingOrigin(hitPose, hitPlane);
                return true;
            }

            return false;
        }

        public void EstablishTrainingOrigin(Pose pose, ARPlane plane)
        {
            IsOriginEstablished = true;
            TrainingOriginPosition = pose.position;

            Debug.Log($"[ARTrainingPlacementManager] TRAINING_ORIGIN established at real-world position: {pose.position}");

            // Create Root Anchor on real ground plane
            if (anchorManager != null && plane != null)
            {
                if (rootAnchor != null)
                {
                    Destroy(rootAnchor);
                }
                rootAnchor = anchorManager.AttachAnchor(plane, pose);
            }

            // Disable plane visualizers once training origin is confirmed
            SetPlaneVisualizationEnabled(false);

            OnTrainingOriginPlaced?.Invoke(pose.position);
        }

        public void SetPlaneVisualizationEnabled(bool enable)
        {
            if (planeManager == null) return;

            planeManager.enabled = enable;
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(enable);
            }
        }
    }
}
