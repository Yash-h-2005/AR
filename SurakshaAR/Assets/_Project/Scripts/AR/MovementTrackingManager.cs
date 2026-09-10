using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace SurakshaAR.AR
{
    /// <summary>
    /// Tracks real-world physical worker movement via AR Foundation camera pose (ARCamera.transform.position).
    /// Does NOT use GPS for indoor short-distance movement tracking.
    /// Evaluates proximity to spatial targets (Exit, Assembly Point) and crouch compliance below smoke layer.
    /// </summary>
    public class MovementTrackingManager : MonoBehaviour
    {
        public static MovementTrackingManager Instance { get; private set; }

        [Header("Camera & Target Anchors")]
        [SerializeField] private Camera arCamera;
        [SerializeField] private Transform safeExitTarget;
        [SerializeField] private Transform assemblyTarget;

        [Header("Target Threshold Radii (Meters)")]
        [SerializeField] private float exitTargetRadius = 1.5f;
        [SerializeField] private float assemblyTargetRadius = 2.0f;
        [SerializeField] private float safeCrouchHeightLimit = 1.4f;

        [Header("Tracking Status")]
        public bool IsTrackingActive { get; private set; } = true;
        public float CurrentCameraHeight { get; private set; } = 1.6f;
        public float DistanceToExit { get; private set; } = float.MaxValue;
        public float DistanceToAssembly { get; private set; } = float.MaxValue;

        public event Action OnExitReached;
        public event Action OnAssemblyReached;
        public event Action<bool> OnSmokeCrouchViolation; // true if standing in smoke layer
        public event Action<bool> OnTrackingStateChanged; // false = tracking lost/degraded

        private Vector3 groundOriginPosition = Vector3.zero;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (arCamera == null) arCamera = Camera.main;
        }

        private void OnEnable()
        {
            ARSession.stateChanged += HandleARSessionStateChanged;
        }

        private void OnDisable()
        {
            ARSession.stateChanged -= HandleARSessionStateChanged;
        }

        private void HandleARSessionStateChanged(ARSessionStateChangedEventArgs args)
        {
            bool isGoodState = (args.state == ARSessionState.SessionTracking || args.state == ARSessionState.Ready);
            if (IsTrackingActive != isGoodState)
            {
                IsTrackingActive = isGoodState;
                Debug.Log($"[MovementTrackingManager] AR Tracking State Changed: {args.state} (Active={IsTrackingActive})");
                OnTrackingStateChanged?.Invoke(IsTrackingActive);
            }
        }

        public void SetTargets(Transform exitTransform, Transform assemblyTransform, Vector3 groundOrigin)
        {
            safeExitTarget = exitTransform;
            assemblyTarget = assemblyTransform;
            groundOriginPosition = groundOrigin;
        }

        private void Update()
        {
            if (arCamera == null || !IsTrackingActive) return;

            Vector3 currentCamPos = arCamera.transform.position;

            // Height check relative to ground origin
            CurrentCameraHeight = currentCamPos.y - groundOriginPosition.y;
            bool isViolatingSmoke = (CurrentCameraHeight > safeCrouchHeightLimit);
            OnSmokeCrouchViolation?.Invoke(isViolatingSmoke);

            // Exit Distance tracking
            if (safeExitTarget != null)
            {
                DistanceToExit = Vector3.Distance(currentCamPos, safeExitTarget.position);
                if (DistanceToExit <= exitTargetRadius)
                {
                    Debug.Log($"[MovementTrackingManager] Physical Exit Reached! Distance: {DistanceToExit:F2}m");
                    OnExitReached?.Invoke();
                }
            }

            // Assembly Distance tracking
            if (assemblyTarget != null)
            {
                DistanceToAssembly = Vector3.Distance(currentCamPos, assemblyTarget.position);
                if (DistanceToAssembly <= assemblyTargetRadius)
                {
                    Debug.Log($"[MovementTrackingManager] Physical Assembly Point Reached! Distance: {DistanceToAssembly:F2}m");
                    OnAssemblyReached?.Invoke();
                }
            }
        }
    }
}
