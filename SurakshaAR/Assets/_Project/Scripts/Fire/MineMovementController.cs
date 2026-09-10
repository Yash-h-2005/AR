using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;

namespace SurakshaAR.Training.Fire
{
    /// <summary>
    /// DIRECTION-BASED PHYSICAL WALKING SYSTEM
    ///
    /// Core Architecture:
    /// - Continuous Phone Facing Direction: The phone's current Yaw orientation determines the virtual movement direction in real-time.
    /// - Physical Motion & Cadence Detection: Isolates dynamic acceleration from device accelerometer (Input.acceleration) to detect actual walking steps.
    /// - Forward/Backward Walking:
    ///     Forward: virtualPosition += currentForward * walkingDistance
    ///     Backward: virtualPosition -= currentForward * walkingDistance
    /// - Smooth real-time movement dampened via Vector3.SmoothDamp.
    /// - Clamped strictly within the coal mine tunnel boundaries.
    /// </summary>
    public class MineMovementController : MonoBehaviour
    {
        // ── Inspector References ──────────────────────────────────────────────

        [Header("Virtual Camera & World")]
        [Tooltip("The Camera rendering the virtual mine corridor.")]
        [SerializeField] private Camera virtualCamera;

        [Tooltip("Root transform of the virtual mine environment.")]
        [SerializeField] private Transform mineOrigin;

        [Tooltip("Initial starting position outside the mine entrance.")]
        [SerializeField] private Vector3 startVirtualPosition = new Vector3(0f, 1.65f, -14.0f);

        [Header("AR Tracking Source")]
        [Tooltip("AR Foundation camera tracking device pose in real world.")]
        [SerializeField] private Camera arCamera;

        [Tooltip("AR Session handling tracking state.")]
        [SerializeField] private ARSession arSession;

        [Header("Motion Sensor Cadence Tuning")]
        [Tooltip("Dynamic acceleration threshold in g's to detect walking (0.12 - 0.22g).")]
        [SerializeField, Range(0.08f, 0.35f)] private float stepThreshold = 0.14f;

        [Tooltip("Minimum time between steps in seconds.")]
        [SerializeField, Range(0.20f, 0.60f)] private float minStepInterval = 0.28f;

        [Tooltip("Time with no step before returning to STATIONARY.")]
        [SerializeField, Range(0.30f, 1.20f)] private float stationaryTimeout = 0.55f;

        [Tooltip("Base walking speed in virtual meters per second.")]
        [SerializeField, Range(0.5f, 2.5f)] private float baseWalkSpeed = 1.20f;

        [Tooltip("Configurable scale multiplier for walking speed.")]
        [SerializeField, Range(0.5f, 3.0f)] private float physicalToVirtualScale = 1.0f;

        [Tooltip("Smoothing time for camera translation (steadycam dampening).")]
        [SerializeField] private float smoothTime = 0.06f;

        [Tooltip("Smoothing applied to virtual camera rotation.")]
        [SerializeField, Range(0.0f, 0.5f)] private float rotationSmoothing = 0.05f;

        [Header("Corridor Boundary Constraints")]
        [SerializeField] private bool clampToCorridor = true;
        [SerializeField] private float corridorMinX = -1.20f;
        [SerializeField] private float corridorMaxX =  1.20f;
        [SerializeField] private float corridorMinY =  1.10f;
        [SerializeField] private float corridorMaxY =  1.95f;
        [SerializeField] private float corridorMinZ = -15.00f;
        [SerializeField] private float corridorMaxZ = 110.00f;
        [SerializeField] private bool routeBEmergencyExit = false;

        public bool RouteBEmergencyExit
        {
            get => routeBEmergencyExit;
            set => routeBEmergencyExit = value;
        }

        // ── State (Exposed for Debug HUD & Architecture) ───────────────────────
        public TrackingState CurrentState { get; private set; } = TrackingState.Initializing;
        public float PhoneYaw { get; private set; } = 0f;
        public float PhonePitch { get; private set; } = 0f;
        public string MotionState { get; private set; } = "STATIONARY";
        public string WalkDirection { get; private set; } = "NONE";
        public float EstimatedWalkDistance { get; private set; } = 0f;
        public Vector3 VirtualPosition => virtualCamera != null ? virtualCamera.transform.position : Vector3.zero;
        public Vector3 VirtualEulerAngles => virtualCamera != null ? virtualCamera.transform.eulerAngles : Vector3.zero;

        // Legacy / Compatibility Properties for existing scene hooks
        public Vector3 RawTrackedPosition { get; private set; } = Vector3.zero;
        public Quaternion RawTrackedRotation { get; private set; } = Quaternion.identity;
        public Vector3 RawTrackedEuler => RawTrackedRotation.eulerAngles;
        public Vector3 PreviousTrackedPosition { get; private set; } = Vector3.zero;
        public Vector3 PositionDelta { get; private set; } = Vector3.zero;
        public Vector3 AccumulatedPhysicalDisplacement => VirtualPosition - startVirtualPosition;
        public float AccumulatedPhysicalDistance => EstimatedWalkDistance;
        public string CurrentMovementDirection => WalkDirection;
        public float PhysicalToVirtualScale => physicalToVirtualScale;

        // ── Events ────────────────────────────────────────────────────────────
        public event System.Action<TrackingState> OnTrackingStateChanged;

        // ── Private Reference Frame State ─────────────────────────────────────
        private Quaternion referenceYaw0 = Quaternion.identity;
        private Vector3 virtualTargetPosition;
        private Vector3 posSmoothVelocity = Vector3.zero;
        private bool isCalibrated = false;

        // Accelerometer & Step Detection
        private Vector3 gravityEstimate = new Vector3(0f, -1f, 0f);
        private float lastStepTime = -10f;
        private float currentWalkVelocity = 0f;
        private float walkVelocitySmooth = 0f;
        private float lastLogTime = 0f;

        // Gyro fallback
        private bool gyroAvailable = false;
        private Quaternion gyroCorrection;

        private static readonly List<InputDevice> s_XRDevices = new List<InputDevice>();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (SystemInfo.supportsGyroscope)
            {
                Input.gyro.enabled = true;
                gyroAvailable = true;
                gyroCorrection = Quaternion.Euler(90f, 0f, 0f);
            }

            virtualTargetPosition = startVirtualPosition;
        }

        private void OnEnable()
        {
            ARSession.stateChanged += HandleARSessionStateChanged;
        }

        private void OnDisable()
        {
            ARSession.stateChanged -= HandleARSessionStateChanged;
        }

        private void Start()
        {
            virtualTargetPosition = startVirtualPosition;
            if (virtualCamera != null)
            {
                virtualCamera.transform.position = startVirtualPosition;
            }

            StartCoroutine(CalibrationRoutine());
        }

        // ── Calibration Routine ───────────────────────────────────────────────

        private IEnumerator CalibrationRoutine()
        {
            SetState(TrackingState.Calibrating);
            Debug.Log("[MineMovement] Calibration started: Face the direction you want to start walking...");

            // Wait briefly for AR session or sensor stabilization
            float waitTimer = 0f;
            while (ARSession.state != ARSessionState.SessionTracking &&
                   ARSession.state != ARSessionState.Ready &&
                   waitTimer < 2.0f)
            {
                waitTimer += Time.deltaTime;
                yield return null;
            }

            if (arCamera == null)
            {
                arCamera = Camera.main;
            }

            // Stabilization sampling window (0.8s)
            yield return new WaitForSeconds(0.8f);

            // Establish Reference Yaw0 to align phone's starting facing direction with mine corridor (+Z)
            Quaternion rot0 = SampleTrackedRotation();
            Vector3 euler0 = rot0.eulerAngles;
            referenceYaw0 = Quaternion.Euler(0f, euler0.y, 0f);

            virtualTargetPosition = startVirtualPosition;
            if (virtualCamera != null)
            {
                virtualCamera.transform.position = startVirtualPosition;
            }

            // Initialize accelerometer gravity filter
            if (Input.acceleration != Vector3.zero)
            {
                gravityEstimate = Input.acceleration;
            }

            isCalibrated = true;
            SetState(TrackingState.Tracking);
            Debug.Log($"[MineMovement] CALIBRATION COMPLETE! Reference Yaw0: {referenceYaw0.eulerAngles.y:F1}° | Mine start: {startVirtualPosition}");
        }

        // ── Main Update Loop ──────────────────────────────────────────────────

        private void Update()
        {
            if (!isCalibrated) return;
            if (virtualCamera == null) return;

            // 1. Continuously Sample Phone Orientation (Yaw & Pitch)
            UpdateOrientation();

            // 2. Detect Physical Walking from Accelerometer Sensor Cadence
            DetectPhysicalWalking();

            // 3. Translate Virtual Player along Current Phone Facing Direction
            UpdateDirectionalMovement();

            // 4. Periodic Diagnostics
            LogDiagnostics();
        }

        // ── Orientation Tracking (Continuous Facing Direction) ────────────────

        private void UpdateOrientation()
        {
            Quaternion rawRot = SampleTrackedRotation();
            RawTrackedRotation = rawRot;

            // Transform relative to initial reference yaw establishing mine frame
            Quaternion relativeRot = Quaternion.Inverse(referenceYaw0) * rawRot;
            Vector3 euler = relativeRot.eulerAngles;

            // Normalize pitch to -180..+180
            float pitch = euler.x;
            if (pitch > 180f) pitch -= 360f;
            PhonePitch = pitch;

            // Yaw in mine frame (0..360)
            PhoneYaw = euler.y;

            // Stabilize roll within comfortable bounds to eliminate motion sickness
            float rawRoll = Mathf.DeltaAngle(0f, euler.z);
            float clampedRoll = Mathf.Clamp(rawRoll, -8f, 8f);

            Quaternion targetRot = Quaternion.Euler(euler.x, euler.y, clampedRoll);

            virtualCamera.transform.rotation = Quaternion.Slerp(
                virtualCamera.transform.rotation,
                targetRot,
                1f - rotationSmoothing
            );
        }

        // ── Physical Motion & Cadence Detection ────────────────────────────────

        private void DetectPhysicalWalking()
        {
            Vector3 rawAccel = Input.acceleration;

            // 1. Low-pass filter to isolate Earth gravity vector
            gravityEstimate = Vector3.Lerp(gravityEstimate, rawAccel, 0.12f);

            // 2. High-pass dynamic acceleration (isolates human body movement & steps)
            Vector3 dynAccel = rawAccel - gravityEstimate;
            float dynMag = dynAccel.magnitude;

            float now = Time.time;

            // 3. Step Cadence Peak Detection
            if (dynMag > stepThreshold && (now - lastStepTime > minStepInterval))
            {
                lastStepTime = now;
            }

            // 4. Walking State Machine
            bool isWithinCadenceWindow = (now - lastStepTime < stationaryTimeout);
            bool hasActiveMotion = (dynMag > (stepThreshold * 0.70f));

            if (isWithinCadenceWindow && (hasActiveMotion || now - lastStepTime < 0.35f))
            {
                MotionState = "WALKING";

                // Classify Forward vs Backward Walking
                // Human forward walking produces forward acceleration surges along the body horizontal axis
                // or characteristic pitch dynamics
                float forwardComponent = Vector3.Dot(dynAccel, Vector3.up); // in portrait screen orientation, vertical dynamic surge
                float zSurge = -dynAccel.z; // acceleration perpendicular to screen

                if (zSurge < -0.15f)
                {
                    WalkDirection = "BACKWARD";
                }
                else
                {
                    WalkDirection = "FORWARD";
                }

                // Target velocity scaled by cadence intensity
                float speedMultiplier = Mathf.Clamp(dynMag / stepThreshold, 0.8f, 1.8f);
                float targetSpeed = baseWalkSpeed * speedMultiplier * physicalToVirtualScale;

                // Smoothly ramp speed up
                currentWalkVelocity = Mathf.SmoothDamp(currentWalkVelocity, targetSpeed, ref walkVelocitySmooth, 0.12f);
            }
            else
            {
                // Worker has stopped walking -> completely STATIONARY
                MotionState = "STATIONARY";
                WalkDirection = "NONE";

                // Smoothly decelerate to a dead stop within 0.15s
                currentWalkVelocity = Mathf.SmoothDamp(currentWalkVelocity, 0f, ref walkVelocitySmooth, 0.15f);
                if (currentWalkVelocity < 0.02f)
                {
                    currentWalkVelocity = 0f;
                }
            }
        }

        // ── Direction-Based Virtual Movement ───────────────────────────────────

        private void UpdateDirectionalMovement()
        {
            // CORE REQUIREMENT:
            // The phone's CURRENT facing direction determines virtual movement direction continuously.
            // TURN PHONE -> movement direction changes immediately.
            Vector3 currentFacingForward = Quaternion.Euler(0f, PhoneYaw, 0f) * Vector3.forward;

            if (currentWalkVelocity > 0.001f)
            {
                float displacement = currentWalkVelocity * Time.deltaTime;

                if (WalkDirection == "FORWARD")
                {
                    virtualTargetPosition += currentFacingForward * displacement;
                    EstimatedWalkDistance += displacement;
                }
                else if (WalkDirection == "BACKWARD")
                {
                    virtualTargetPosition -= currentFacingForward * displacement;
                    EstimatedWalkDistance += displacement;
                }
            }

            // Corridor Boundary Constraints: keeps player safely inside rock walls
            if (clampToCorridor)
            {
                float z = virtualTargetPosition.z;
                float x = virtualTargetPosition.x;

                if (z < 0f)
                {
                    // Outdoor open surface — generous width so player can explore freely
                    virtualTargetPosition.x = Mathf.Clamp(x, -8.0f, 8.0f);
                }
                else if (z < 2f)
                {
                    // Portal mouth / start chamber transition
                    virtualTargetPosition.x = Mathf.Clamp(x, -2.2f, 2.2f);
                }
                else if (z < 29f)
                {
                    // Main tunnel A (2.6m wide -> half width 1.20m)
                    virtualTargetPosition.x = Mathf.Clamp(x, -1.20f, 1.20f);
                }
                else if (z < 33f)
                {
                    // Junction approach (widening from 2.6m to 5.4m)
                    float t = (z - 29f) / 4.0f;
                    float halfW = Mathf.Lerp(1.20f, 2.50f, t);
                    virtualTargetPosition.x = Mathf.Clamp(x, -halfW, halfW);
                }
                else if (z < 47.0f)
                {
                    // Inside Route A (Left) or Route B (Right) branches (Z = 33m to 47m)
                    float dZ = z - 33.0f;
                    if (x <= 0f)
                    {
                        // Route A (angled -22 deg: center shifts left)
                        float centerA = -1.75f - dZ * 0.40f;
                        float minXA = centerA - 1.15f;
                        float maxXA = (z < 37.0f) ? Mathf.Min(-0.35f, centerA + 1.15f) : (centerA + 1.15f);
                        virtualTargetPosition.x = Mathf.Clamp(x, minXA, maxXA);
                    }
                    else
                    {
                        // Route B (angled +22 deg: center shifts right)
                        float centerB = 1.75f + dZ * 0.40f;
                        float minXB = (z < 37.0f) ? Mathf.Max(0.35f, centerB - 1.15f) : (centerB - 1.15f);
                        float maxXB = centerB + 1.15f;
                        virtualTargetPosition.x = Mathf.Clamp(x, minXB, maxXB);
                    }
                }
                else if (z < 75.0f)
                {
                    if (routeBEmergencyExit)
                    {
                        // Route A is blocked at 47m by rockfall/barricade
                        if (x < 3.0f)
                        {
                            virtualTargetPosition.z = Mathf.Min(virtualTargetPosition.z, 47.0f);
                        }
                        else
                        {
                            // Extended Route B narrow rocky tunnel (Z = 47m to 75m)
                            float tTunnel = Mathf.Clamp01((z - 47.0f) / 28.0f);
                            float centerTunnel = Mathf.Lerp(7.35f, 8.50f, tTunnel);
                            virtualTargetPosition.x = Mathf.Clamp(x, centerTunnel - 1.20f, centerTunnel + 1.20f);
                        }
                    }
                    else
                    {
                        // Route B is blocked at 47m by rockfall/barricade
                        if (x > -3.0f)
                        {
                            virtualTargetPosition.z = Mathf.Min(virtualTargetPosition.z, 47.0f);
                        }
                        else
                        {
                            // Extended Route A narrow rocky tunnel (Z = 47m to 75m)
                            float tTunnel = Mathf.Clamp01((z - 47.0f) / 28.0f);
                            float centerTunnel = Mathf.Lerp(-7.35f, -8.50f, tTunnel);
                            virtualTargetPosition.x = Mathf.Clamp(x, centerTunnel - 1.20f, centerTunnel + 1.20f);
                        }
                    }
                }
                else
                {
                    // Outdoor Open Ground Area (Z = 75m to 110m)
                    if (routeBEmergencyExit)
                    {
                        // Portal exit mouth at Z = 75m (around X = +8.5m)
                        float tOpen = Mathf.Clamp01((z - 75.0f) / 5.0f);
                        float minXOpen = Mathf.Lerp(8.5f - 1.60f, -4.0f, tOpen);
                        float maxXOpen = Mathf.Lerp(8.5f + 1.60f, 18.0f, tOpen);
                        virtualTargetPosition.x = Mathf.Clamp(x, minXOpen, maxXOpen);
                    }
                    else
                    {
                        // Portal exit mouth at Z = 75m (around X = -8.5m)
                        float tOpen = Mathf.Clamp01((z - 75.0f) / 5.0f);
                        float minXOpen = Mathf.Lerp(-8.5f - 1.60f, -18.0f, tOpen);
                        float maxXOpen = Mathf.Lerp(-8.5f + 1.60f, 4.0f, tOpen);
                        virtualTargetPosition.x = Mathf.Clamp(x, minXOpen, maxXOpen);
                    }
                }

                virtualTargetPosition.y = Mathf.Clamp(virtualTargetPosition.y, corridorMinY, corridorMaxY);
                virtualTargetPosition.z = Mathf.Clamp(virtualTargetPosition.z, corridorMinZ, corridorMaxZ);
            }

            // Steadycam smoothing: smooth real-time glide
            if (smoothTime > 0.001f)
            {
                virtualCamera.transform.position = Vector3.SmoothDamp(
                    virtualCamera.transform.position,
                    virtualTargetPosition,
                    ref posSmoothVelocity,
                    smoothTime
                );
            }
            else
            {
                virtualCamera.transform.position = virtualTargetPosition;
            }
        }

        // ── Sensor Sampling (ARCore + XR + Gyroscope) ─────────────────────────

        private Quaternion SampleTrackedRotation()
        {
            // 1. Direct XR InputDevices query
            s_XRDevices.Clear();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.TrackedDevice, s_XRDevices);
            foreach (var dev in s_XRDevices)
            {
                if (dev.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion devR) && devR != Quaternion.identity)
                    return devR;
                if (dev.TryGetFeatureValue(CommonUsages.centerEyeRotation, out Quaternion eyeR) && eyeR != Quaternion.identity)
                    return eyeR;
            }

            // 2. AR Camera transform localRotation
            if (arCamera != null && arCamera.transform.localRotation != Quaternion.identity)
            {
                return arCamera.transform.localRotation;
            }

            // 3. Device Gyroscope fallback
            if (gyroAvailable)
            {
                Quaternion att = Input.gyro.attitude;
                return gyroCorrection * new Quaternion(att.x, att.y, -att.z, -att.w);
            }

            return Quaternion.identity;
        }

        public Vector3 SampleTrackedPosition()
        {
            if (arCamera != null && arCamera.transform.localPosition != Vector3.zero)
                return arCamera.transform.localPosition;
            return VirtualPosition;
        }

        // ── Diagnostics Logging ───────────────────────────────────────────────

        private void LogDiagnostics()
        {
            if (Time.time - lastLogTime >= 0.5f)
            {
                lastLogTime = Time.time;
                Vector3 v = VirtualPosition;
                Debug.Log($"[MineMovement] YAW: {PhoneYaw:F0}° | MOTION: {MotionState} | DIR: {WalkDirection} | DIST: {EstimatedWalkDistance:F2}m | VIRTUAL: ({v.x:F2}, {v.z:F2})");
            }
        }

        // ── Tracking Loss & State Handlers ─────────────────────────────────────

        private void HandleARSessionStateChanged(ARSessionStateChangedEventArgs args)
        {
            if (args.state == ARSessionState.SessionTracking)
            {
                SetState(TrackingState.Tracking);
            }
            else if (args.state == ARSessionState.SessionInitializing || args.state == ARSessionState.Ready)
            {
                if (isCalibrated)
                {
                    SetState(TrackingState.Tracking);
                }
            }
            else if (args.state == ARSessionState.None || args.state == ARSessionState.Unsupported)
            {
                SetState(TrackingState.Lost);
            }
        }

        private void SetState(TrackingState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            OnTrackingStateChanged?.Invoke(newState);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Recalibrate()
        {
            StartCoroutine(CalibrationRoutine());
        }

        public void SetMovementScale(float scale)
        {
            physicalToVirtualScale = Mathf.Clamp(scale, 0.2f, 5.0f);
            Debug.Log($"[MineMovement] Movement scale set to: {physicalToVirtualScale:F2}x");
        }
    }

    public enum TrackingState
    {
        Initializing,
        Calibrating,
        Tracking,
        Lost
    }
}



