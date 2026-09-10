using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SurakshaAR.AR
{
    /// <summary>
    /// Handles user screen touch input, executes AR raycasts against detected planes, 
    /// and triggers placement or error callbacks.
    /// </summary>
    public class ARRaycastPlacementController : MonoBehaviour
    {
        [Header("Raycast Manager Reference")]
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private Camera arCamera;

        public event Action<Pose, ARPlane> OnValidPlacementTap;
        public event Action OnInvalidTap;

        private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

        private void Awake()
        {
            if (raycastManager == null)
            {
                raycastManager = GetComponent<ARRaycastManager>();
            }
            if (planeManager == null)
            {
                planeManager = GetComponent<ARPlaneManager>();
            }
            if (arCamera == null)
            {
                arCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (Input.touchCount == 0) return;

            Touch touch = Input.GetTouch(0);

            if (touch.phase != TouchPhase.Began) return;

            // Ignore taps over UI elements
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            {
                return;
            }

            PerformRaycast(touch.position);
        }

        private void PerformRaycast(Vector2 touchPosition)
        {
            if (raycastManager == null) return;

            if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = hits[0].pose;
                ARPlane plane = planeManager != null ? planeManager.GetPlane(hits[0].trackableId) : null;

                OnValidPlacementTap?.Invoke(hitPose, plane);
            }
            else
            {
                OnInvalidTap?.Invoke();
            }
        }
    }
}
