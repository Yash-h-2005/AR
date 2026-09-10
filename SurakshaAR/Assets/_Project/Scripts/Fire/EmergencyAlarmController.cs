using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SurakshaAR.Training.Fire
{
    /// <summary>
    /// PHASE 2A — Emergency Alarm Station Controller.
    ///
    /// Manages:
    ///   1. Real 3D industrial emergency alarm station mounted on the left tunnel wall near the starting point.
    ///   2. Wall-mounted casing, label "EMERGENCY ALARM", physical red push-button, and red warning beacon.
    ///   3. Before activation: beacon is dark/off, alarm is silent.
    ///   4. Proximity detection: when worker is within range, interaction prompt appears.
    ///   5. On activation: button depresses into casing, beacon flashes pulsing red light illuminating
    ///      nearby tunnel surfaces, emergency siren plays loudly through phone speaker.
    ///   6. OnAlarmActivated event for HUD to transition into fire assessment.
    /// </summary>
    public class EmergencyAlarmController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private MineMovementController movementController;
        [SerializeField] private Camera virtualCamera;

        [Header("Station Placement")]
        [Tooltip("Response area world position of the alarm station on the left tunnel wall.")]
        [SerializeField] private Vector3 responseAreaStationPos = new Vector3(-1.08f, 1.35f, 12.0f);
        [SerializeField] private float alarmOffsetFromFire = 2.0f;
        [SerializeField] private Vector3 startingStationPos = new Vector3(-1.08f, 1.35f, 12.0f);

        [Header("Interaction")]
        [Tooltip("Distance from camera within which the interaction prompt appears.")]
        [SerializeField] private float proximityDistance = 2.0f;

        [Header("UI References")]
        [SerializeField] private Text alarmPromptText;
        [SerializeField] private CanvasGroup alarmPromptGroup;
        [SerializeField] private Text alarmStatusText;
        [SerializeField] private CanvasGroup alarmStatusGroup;

        // ── State ──────────────────────────────────────────────────────────
        public enum AlarmState { Inactive, Visible, Activated }
        public AlarmState CurrentState { get; private set; } = AlarmState.Inactive;

        // ── Events ────────────────────────────────────────────────────────
        public event System.Action OnAlarmActivated;

        // ── 3D Components ──────────────────────────────────────────────────
        private GameObject alarmGO;
        private GameObject alarmButtonGO;
        private Light alarmFlashLight;
        private AudioSource alarmSirenSource;
        private Renderer beaconRenderer;
        private Material beaconMat;
        private Material buttonMat;
        private Vector3 buttonUnpressedLocalPos;
        private Vector3 buttonPressedLocalPos;

        private bool isPlaced = false;
        private Vector3 alarmWorldPos;

        // Flash state
        private float flashTimer = 0f;
        private bool flashOn = false;

        // Prompt fade state
        private float promptAlpha = 0f;

        // ── Lifecycle ─────────────────────────────────────────────────────

        private void Start()
        {
            // Ensure no black background cards are rendered behind prompt or status text
            if (alarmPromptGroup != null)
            {
                alarmPromptGroup.alpha = 0f;
                alarmPromptGroup.interactable = false;
                var bgImage = alarmPromptGroup.GetComponent<Image>();
                if (bgImage != null) bgImage.enabled = false;
            }

            if (alarmStatusGroup != null)
            {
                alarmStatusGroup.alpha = 0f;
                var bgImage = alarmStatusGroup.GetComponent<Image>();
                if (bgImage != null) bgImage.enabled = false;
            }

            // At module start: alarm is dormant. Exploration phase has no alarm prompt.
            CurrentState = AlarmState.Inactive;
            isPlaced = false;
        }

        private void Update()
        {
            if (!isPlaced || CurrentState == AlarmState.Inactive) return;

            if (CurrentState == AlarmState.Visible)
            {
                UpdateProximityPrompt();
                CheckTapToActivate();
            }
            else if (CurrentState == AlarmState.Activated)
            {
                UpdateAlarmFlash();
            }
        }

        // ── Placement API ─────────────────────────────────────────────────

        /// <summary>
        /// Places the alarm station on the left tunnel wall near the fire extinguisher in the response area.
        /// </summary>
        public void PlaceAlarmStation(Vector3 worldPos)
        {
            alarmWorldPos = worldPos;
            BuildAlarmStationObject(alarmWorldPos);
            isPlaced = true;
            CurrentState = AlarmState.Visible;

            Debug.Log("[EmergencyAlarm] 3D Alarm Station ready in response area near extinguisher at: " + alarmWorldPos);
        }

        /// <summary>
        /// Called by Phase2AOrchestrator when the fire incident triggers.
        /// Activates the emergency alarm on the left tunnel wall.
        /// </summary>
        public void PlaceAlarm(Vector3 firePosition)
        {
            // Positioned on the left wall at Z = 12.0m (2.0m past fire trigger at Z=10m, and 4.2m before extinguisher at Z=16.2m)
            Vector3 targetPos = new Vector3(-1.08f, 1.35f, 12.0f);
            if (!isPlaced)
            {
                PlaceAlarmStation(targetPos);
            }
            else
            {
                CurrentState = AlarmState.Visible;
                Debug.Log("[EmergencyAlarm] Alarm confirmed active at response station: " + alarmWorldPos);
            }
        }

        // ── Proximity Prompt ──────────────────────────────────────────────

        private void UpdateProximityPrompt()
        {
            if (virtualCamera == null) return;

            float dist = Vector3.Distance(virtualCamera.transform.position, alarmWorldPos);
            bool inRange = dist <= proximityDistance;

            // Smooth fade in/out of prompt
            float targetAlpha = inRange ? 1f : 0f;
            promptAlpha = Mathf.MoveTowards(promptAlpha, targetAlpha, Time.deltaTime * 5f);

            if (alarmPromptGroup != null)
            {
                alarmPromptGroup.alpha = promptAlpha;
                alarmPromptGroup.interactable = inRange;
            }

            // Subtle button pulsation when in range to invite interaction
            if (alarmButtonGO != null && buttonMat != null)
            {
                float pulse = inRange ? (0.6f + Mathf.Sin(Time.time * 6f) * 0.4f) : 0.2f;
                buttonMat.SetColor("_EmissionColor", new Color(1.0f, 0.1f, 0.05f) * pulse);
            }
        }

        // ── Tap to Activate ───────────────────────────────────────────────

        private void CheckTapToActivate()
        {
            if (promptAlpha < 0.4f) return;

            bool tapped = false;

            // Touch input
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                    tapped = true;
            }

            // Mouse fallback for editor testing
            if (Input.GetMouseButtonDown(0))
                tapped = true;

            if (tapped)
            {
                ActivateAlarm();
            }
        }

        // ── Alarm Activation ──────────────────────────────────────────────

        /// <summary>
        /// Automatically triggers the emergency alarm immediately without requiring proximity or player tap.
        /// Used by ExplosionHazardController in Sub-module 3.
        /// </summary>
        public void TriggerAutomaticAlarm()
        {
            if (!isPlaced)
            {
                PlaceAlarmStation(startingStationPos);
            }
            ActivateAlarm();
        }

        public void ActivateAlarm()
        {
            if (CurrentState == AlarmState.Activated) return;

            CurrentState = AlarmState.Activated;

            // Hide prompt
            if (alarmPromptGroup != null)
            {
                alarmPromptGroup.alpha = 0f;
                alarmPromptGroup.interactable = false;
            }

            // 1. Physically depress the push-button
            if (alarmButtonGO != null)
            {
                alarmButtonGO.transform.localPosition = buttonPressedLocalPos;
                if (buttonMat != null)
                {
                    buttonMat.color = new Color(0.70f, 0.04f, 0.02f);
                    buttonMat.SetColor("_EmissionColor", new Color(0.9f, 0.1f, 0.05f) * 0.4f);
                }
            }

            // 2. Enable flashing beacon light
            if (alarmFlashLight != null)
            {
                alarmFlashLight.enabled = true;
                alarmFlashLight.intensity = 10f;
            }

            // 3. Ensure AudioListener is active on camera and global audio is unmuted
            var listener = FindFirstObjectByType<AudioListener>();
            if (listener == null && virtualCamera != null)
            {
                listener = virtualCamera.gameObject.AddComponent<AudioListener>();
            }
            if (listener != null) listener.enabled = true;
            AudioListener.volume = 1.0f;
            AudioListener.pause = false;

            // 4. Play siren through phone speaker
            if (alarmSirenSource != null)
            {
                if (alarmSirenSource.clip == null)
                    alarmSirenSource.clip = GenerateSirenClip();

                alarmSirenSource.spatialBlend = 0.0f; // Pure 2D audio for full phone speaker volume
                alarmSirenSource.volume = 0.95f;
                alarmSirenSource.mute = false;
                alarmSirenSource.loop = true;
                alarmSirenSource.Play();
                Debug.Log("[EmergencyAlarm] Emergency siren playing on phone speaker.");
            }

            // 5. Show clean text status (no black card)
            StartCoroutine(ShowAlarmStatusSequence());

            OnAlarmActivated?.Invoke();
            Debug.Log("[EmergencyAlarm] ALARM ACTIVATED by worker at starting point.");
        }

        private IEnumerator ShowAlarmStatusSequence()
        {
            ShowStatus("ALARM ACTIVATED", new Color(1.0f, 0.35f, 0.25f));
            yield return new WaitForSeconds(2.0f);

            ShowStatus("Emergency teams notified.\nResponse initiated.", new Color(1.0f, 0.85f, 0.20f));
            yield return new WaitForSeconds(2.5f);

            ShowStatus("ALARM ACTIVE\nEmergency teams notified.", new Color(0.25f, 0.95f, 0.45f));
        }

        private void ShowStatus(string message, Color color)
        {
            if (alarmStatusText != null)
            {
                alarmStatusText.text = message;
                alarmStatusText.color = color;
            }
            if (alarmStatusGroup != null)
            {
                alarmStatusGroup.alpha = 1f;
            }
            Debug.Log("[EmergencyAlarm] Status: " + message);
        }

        // ── Alarm Flash Update ────────────────────────────────────────────

        private void UpdateAlarmFlash()
        {
            flashTimer += Time.deltaTime;
            if (flashTimer >= 0.35f)
            {
                flashTimer = 0f;
                flashOn = !flashOn;

                // Beacon light pulsing
                if (alarmFlashLight != null)
                {
                    alarmFlashLight.intensity = flashOn ? 10.0f : 0.0f;
                }

                // Beacon dome emissive pulsing
                if (beaconMat != null)
                {
                    beaconMat.SetColor("_EmissionColor",
                        flashOn ? new Color(1.0f, 0.12f, 0.05f) * 6.0f : Color.black);
                }
            }
        }

        // ── 3D Alarm Station Construction ──────────────────────────────────

        private void BuildAlarmStationObject(Vector3 worldPos)
        {
            alarmGO = new GameObject("EmergencyAlarmStation");
            alarmGO.transform.SetParent(transform, false);
            alarmGO.transform.position = worldPos;
            // Face inward toward the tunnel center (+X direction from left wall)
            alarmGO.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            var stdShader = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse");

            // 1. Backplate / Wall Mounting Frame (Dark metal)
            var backplate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backplate.name = "StationBackplate";
            backplate.transform.SetParent(alarmGO.transform, false);
            backplate.transform.localPosition = new Vector3(0f, 0f, -0.04f);
            backplate.transform.localScale = new Vector3(0.40f, 0.54f, 0.04f);
            Destroy(backplate.GetComponent<Collider>());
            var backMat = new Material(stdShader);
            backMat.color = new Color(0.18f, 0.20f, 0.22f);
            backMat.SetFloat("_Metallic", 0.7f);
            backMat.SetFloat("_Glossiness", 0.3f);
            backplate.GetComponent<Renderer>().material = backMat;

            // 2. Yellow Industrial Enclosure Box
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "AlarmEnclosure";
            box.transform.SetParent(alarmGO.transform, false);
            box.transform.localPosition = new Vector3(0f, -0.02f, 0.05f);
            box.transform.localScale = new Vector3(0.32f, 0.38f, 0.14f);
            Destroy(box.GetComponent<Collider>());
            var boxMat = new Material(stdShader);
            boxMat.color = new Color(0.92f, 0.72f, 0.08f); // Industrial safety yellow
            boxMat.SetFloat("_Metallic", 0.2f);
            boxMat.SetFloat("_Glossiness", 0.4f);
            box.GetComponent<Renderer>().material = boxMat;

            // 3. Black Bezel Ring around Button
            var bezel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bezel.name = "ButtonBezel";
            bezel.transform.SetParent(alarmGO.transform, false);
            bezel.transform.localPosition = new Vector3(0f, 0.04f, 0.125f);
            bezel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            bezel.transform.localScale = new Vector3(0.16f, 0.02f, 0.16f);
            Destroy(bezel.GetComponent<Collider>());
            var bezelMat = new Material(stdShader);
            bezelMat.color = new Color(0.12f, 0.12f, 0.12f);
            bezel.GetComponent<Renderer>().material = bezelMat;

            // 4. Large Red Push Button
            buttonUnpressedLocalPos = new Vector3(0f, 0.04f, 0.145f);
            buttonPressedLocalPos   = new Vector3(0f, 0.04f, 0.115f); // 3cm pressed inward

            alarmButtonGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            alarmButtonGO.name = "EmergencyPushButton";
            alarmButtonGO.transform.SetParent(alarmGO.transform, false);
            alarmButtonGO.transform.localPosition = buttonUnpressedLocalPos;
            alarmButtonGO.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            alarmButtonGO.transform.localScale = new Vector3(0.12f, 0.025f, 0.12f);
            Destroy(alarmButtonGO.GetComponent<Collider>());

            buttonMat = new Material(stdShader);
            buttonMat.color = new Color(0.92f, 0.08f, 0.05f); // Bright emergency red
            buttonMat.EnableKeyword("_EMISSION");
            buttonMat.SetColor("_EmissionColor", new Color(1.0f, 0.1f, 0.05f) * 0.2f);
            alarmButtonGO.GetComponent<Renderer>().material = buttonMat;

            // 5. White Acrylic Label Plate: "EMERGENCY ALARM"
            var labelPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            labelPlate.name = "LabelPlate";
            labelPlate.transform.SetParent(alarmGO.transform, false);
            labelPlate.transform.localPosition = new Vector3(0f, -0.12f, 0.125f);
            labelPlate.transform.localScale = new Vector3(0.26f, 0.08f, 0.015f);
            Destroy(labelPlate.GetComponent<Collider>());
            var plateMat = new Material(stdShader);
            plateMat.color = new Color(0.95f, 0.95f, 0.95f);
            labelPlate.GetComponent<Renderer>().material = plateMat;

            var labelTextGO = new GameObject("LabelText");
            labelTextGO.transform.SetParent(labelPlate.transform, false);
            labelTextGO.transform.localPosition = new Vector3(0f, 0f, -0.55f);
            labelTextGO.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            labelTextGO.transform.localScale = Vector3.one * 0.012f;
            var tm = labelTextGO.AddComponent<TextMesh>();
            tm.text = "EMERGENCY\nALARM";
            tm.fontSize = 32;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = Color.black;

            // 6. Prominent Red Warning Beacon atop the Alarm Box
            var beaconBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beaconBase.name = "BeaconBase";
            beaconBase.transform.SetParent(alarmGO.transform, false);
            beaconBase.transform.localPosition = new Vector3(0f, 0.20f, 0.05f);
            beaconBase.transform.localScale = new Vector3(0.14f, 0.03f, 0.14f);
            Destroy(beaconBase.GetComponent<Collider>());
            var beaconBaseMat = new Material(stdShader);
            beaconBaseMat.color = new Color(0.15f, 0.15f, 0.15f);
            beaconBase.GetComponent<Renderer>().material = beaconBaseMat;

            var beaconDome = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beaconDome.name = "BeaconDomeLens";
            beaconDome.transform.SetParent(alarmGO.transform, false);
            beaconDome.transform.localPosition = new Vector3(0f, 0.30f, 0.05f);
            beaconDome.transform.localScale = new Vector3(0.11f, 0.07f, 0.11f);
            Destroy(beaconDome.GetComponent<Collider>());

            beaconMat = new Material(stdShader);
            beaconMat.color = new Color(0.70f, 0.08f, 0.05f);
            beaconMat.EnableKeyword("_EMISSION");
            beaconMat.SetColor("_EmissionColor", Color.black); // OFF before activation
            beaconMat.SetFloat("_Glossiness", 0.9f);
            beaconRenderer = beaconDome.GetComponent<Renderer>();
            beaconRenderer.material = beaconMat;

            // 7. Beacon Point Light (Illuminates tunnel surfaces on activation)
            var lightGO = new GameObject("BeaconPointLight");
            lightGO.transform.SetParent(alarmGO.transform, false);
            lightGO.transform.localPosition = new Vector3(0f, 0.36f, 0.05f);
            alarmFlashLight = lightGO.AddComponent<Light>();
            alarmFlashLight.type = LightType.Point;
            alarmFlashLight.color = new Color(1.0f, 0.15f, 0.05f);
            alarmFlashLight.intensity = 0f; // OFF before activation
            alarmFlashLight.range = 8.5f;
            alarmFlashLight.shadows = LightShadows.None;
            alarmFlashLight.enabled = false;

            // 8. Siren Audio Source (2D audio for clear, unattenuated phone speaker playback)
            var sirenGO = new GameObject("AlarmSiren");
            sirenGO.transform.SetParent(alarmGO.transform, false);
            alarmSirenSource = sirenGO.AddComponent<AudioSource>();
            alarmSirenSource.clip = GenerateSirenClip();
            alarmSirenSource.loop = true;
            alarmSirenSource.spatialBlend = 0.0f; // 2D audio ensures maximum clarity on mobile phone speaker
            alarmSirenSource.volume = 0.95f;
            alarmSirenSource.mute = false;
            alarmSirenSource.playOnAwake = false;

            Debug.Log("[EmergencyAlarm] Real 3D Emergency Alarm Station built on left wall at: " + worldPos);
        }

        // ── Procedural Siren Audio (Industrial High-Penetration Alarm) ─────

        private AudioClip GenerateSirenClip()
        {
            int sampleRate  = 44100;
            int durationSec = 2;
            int samples     = sampleRate * durationSec;
            float[] data    = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float sweep  = Mathf.PingPong(t * 1.8f, 1f);
                float freq   = Mathf.Lerp(800f, 1350f, sweep);
                float phase1 = t * freq * Mathf.PI * 2f;
                float phase2 = t * (freq * 2.0f) * Mathf.PI * 2f;

                float rawSignal = Mathf.Sin(phase1) * 0.72f + Mathf.Sin(phase2) * 0.22f;
                float saturated = Mathf.Clamp(rawSignal * 1.20f, -0.88f, 0.88f);

                float fade = 1f;
                if (i < 512) fade = (float)i / 512f;
                else if (i > samples - 512) fade = (float)(samples - i) / 512f;

                data[i] = saturated * fade;
            }

            var clip = AudioClip.Create("IndustrialSirenLoop", samples, 1, sampleRate, false);
            if (clip != null)
            {
                clip.SetData(data, 0);
            }
            return clip;
        }
    }
}
