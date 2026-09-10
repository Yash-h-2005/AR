using System;
using System.Collections;
using UnityEngine;

namespace SurakshaAR.Training.Fire
{
    /// <summary>
    /// Sub-Module 3: Explosion Hazard Controller.
    ///
    /// Manages:
    ///   1. 3D Industrial Electrical Cabinet mounted on the tunnel wall at (1.05, 1.25, 10.0).
    ///   2. Strict proximity trigger: worker must enter the mine (Z >= 1.0m) and physically reach
    ///      the electric box hazard area before the explosion can occur.
    ///   3. Sudden underground explosion: procedural blast audio, camera trauma shake, bright flash,
    ///      dust bursts, rock shrapnel, and falling ceiling debris.
    ///   4. Strong short haptic vibration pulse at the moment of blast.
    ///   5. Automatic emergency alarm siren and flashing red beacon activation.
    ///   6. Clean emergency objective transition ("EMERGENCY — EVACUATE" -> "Reach the emergency exit!").
    ///   7. Spawning the progressive collapse BEHIND the worker toward the emergency exit.
    /// </summary>
    public class ExplosionHazardController : MonoBehaviour
    {
        // ── Inspector References ─────────────────────────────────────────────

        [Header("Scene References")]
        [SerializeField] private MineMovementController movementController;
        [SerializeField] private Camera virtualCamera;
        [SerializeField] private EmergencyAlarmController emergencyAlarmController;
        [SerializeField] private MineSimulatorHUD hud;
        [SerializeField] private MineCollapseController collapseController;

        [Header("Electric Box Hazard")]
        [Tooltip("World position of the wall-mounted electrical cabinet inside the mine.")]
        [SerializeField] private Vector3 electricBoxPosition = new Vector3(1.05f, 1.25f, 10.0f);
        [Tooltip("Trigger radius around the electric box within which the blast occurs.")]
        [SerializeField] private float triggerRadius = 2.6f;

        [Tooltip("When false, proximity check will not auto-trigger the blast (used in Assessment where fire must be put out first).")]
        [SerializeField] private bool autoTriggerOnProximity = true;

        public bool AutoTriggerOnProximity
        {
            get => autoTriggerOnProximity;
            set => autoTriggerOnProximity = value;
        }

        [Header("Effects Settings")]
        [SerializeField] private float cameraShakeDuration = 1.4f;
        [SerializeField] private float cameraShakeIntensity = 0.085f;

        // ── Events ────────────────────────────────────────────────────────────
        public event Action OnExplosionTriggered;

        // ── State ─────────────────────────────────────────────────────────────
        public bool HasExploded { get; private set; } = false;

        private GameObject electricBoxGO;
        private AudioSource blastAudioSource;
        private Light blastFlashLight;
        private ParticleSystem blastDustPS;
        private ParticleSystem blastDebrisPS;
        private ParticleSystem ceilingRocksPS;
        private ParticleSystem cabinetSparkPS;

        private void Start()
        {
            BuildElectricCabinetProp();
            BuildBlastVFX();
            BuildBlastAudio();

            if (hud != null)
            {
                hud.SetObjective("Inspect the mine corridor and electrical equipment.");
                hud.SetBadge("NORMAL", new Color(0.12f, 0.85f, 0.45f));
            }
        }

        private void Update()
        {
            if (HasExploded) return;
            CheckTrigger();
        }

        /// <summary>
        /// Strictly checks if the worker has entered the mine and reached the electric box hazard area.
        /// </summary>
        private void CheckTrigger()
        {
            if (!autoTriggerOnProximity) return;
            if (virtualCamera == null) return;

            Vector3 playerPos = virtualCamera.transform.position;

            // TEST A & B: Outside mine (Z < 0.5m) or just inside entrance (Z < 7.5m) NEVER triggers
            if (playerPos.z < 0.5f) return;

            // Calculate horizontal 2D distance from player to electric box
            float horizontalDistToBox = Vector2.Distance(
                new Vector2(playerPos.x, playerPos.z),
                new Vector2(electricBoxPosition.x, electricBoxPosition.z)
            );

            // TEST C: Trigger ONLY when worker physically reaches within radius of the electric box
            if (horizontalDistToBox <= triggerRadius && playerPos.z >= 7.5f)
            {
                TriggerExplosion();
            }
        }

        /// <summary>
        /// Triggers the sudden underground explosion at the electric box.
        /// </summary>
        public void TriggerExplosion()
        {
            if (HasExploded) return;
            HasExploded = true;

            Debug.Log("[ExplosionHazard] 💥 SUDDEN UNDERGROUND EXPLOSION TRIGGERED at Electric Box (" +
                      electricBoxPosition + ") by worker at: " +
                      (virtualCamera != null ? virtualCamera.transform.position.ToString("F2") : "unknown"));

            // 1. Light Flash Burst on tunnel walls
            if (blastFlashLight != null)
            {
                StartCoroutine(FlashLightCoroutine());
            }

            // 2. Play Procedural Blast & Electrical Arc Sound
            if (blastAudioSource != null)
            {
                blastAudioSource.Play();
            }

            // 3. Fire Particle Systems
            if (blastDustPS != null) blastDustPS.Play();
            if (blastDebrisPS != null) blastDebrisPS.Play();
            if (ceilingRocksPS != null) ceilingRocksPS.Play();
            if (cabinetSparkPS != null) cabinetSparkPS.Play();

            // 4. Camera Trauma Shake
            if (virtualCamera != null)
            {
                StartCoroutine(CameraShakeCoroutine(cameraShakeDuration, cameraShakeIntensity));
            }

            // 5. Strong Short Vibration (At explosion moment)
            TriggerStrongShortVibration();

            // 6. Automatic Emergency Alarm Activation (No worker button tap required)
            if (emergencyAlarmController != null)
            {
                emergencyAlarmController.TriggerAutomaticAlarm();
            }

            // 7. Clean Emergency Objective (No debug info)
            if (hud != null)
            {
                hud.ShowStatusToast("🚨 EMERGENCY — MINE COLLAPSE!\nEVACUATE IMMEDIATELY!", new Color(1.0f, 0.25f, 0.15f), 3.5f);
                hud.SetObjective("EMERGENCY — EVACUATE");
                hud.SetBadge("🚨 EVACUATE", new Color(1.0f, 0.15f, 0.05f));
                StartCoroutine(DelayedObjectiveChange("Reach the emergency exit!", 2.0f));
            }

            // 8. Start Moving Collapse BEHIND Worker
            if (collapseController != null)
            {
                float playerZ = (virtualCamera != null) ? virtualCamera.transform.position.z : 10.0f;
                // Collapse begins 5.0m safely behind the worker, leaving the forward path toward exit clear
                float startCollapseZ = Mathf.Max(2.0f, playerZ - 5.0f);
                collapseController.StartCollapse(startCollapseZ);
            }

            OnExplosionTriggered?.Invoke();
        }

        private IEnumerator DelayedObjectiveChange(string newObjective, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (hud != null && !HasExploded) yield break;
            if (hud != null)
            {
                hud.SetObjective(newObjective);
            }
        }

        private IEnumerator FlashLightCoroutine()
        {
            blastFlashLight.enabled = true;
            blastFlashLight.intensity = 26.0f;

            float elapsed = 0f;
            float duration = 0.55f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                blastFlashLight.intensity = Mathf.Lerp(26.0f, 0f, t * t);
                yield return null;
            }

            blastFlashLight.enabled = false;
        }

        private IEnumerator CameraShakeCoroutine(float duration, float magnitude)
        {
            Vector3 originalCamPos = virtualCamera.transform.localPosition;
            Quaternion originalCamRot = virtualCamera.transform.localRotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float damp = 1.0f - (elapsed / duration);

                float offsetX = (Mathf.PerlinNoise(Time.time * 32f, 0f) - 0.5f) * 2f * magnitude * damp;
                float offsetY = (Mathf.PerlinNoise(0f, Time.time * 32f) - 0.5f) * 2f * magnitude * damp;
                float rotZ    = (Mathf.PerlinNoise(Time.time * 24f, Time.time * 24f) - 0.5f) * 5.5f * damp;

                virtualCamera.transform.localPosition = originalCamPos + new Vector3(offsetX, offsetY, 0f);
                virtualCamera.transform.localRotation = originalCamRot * Quaternion.Euler(0f, 0f, rotZ);

                yield return null;
            }

            virtualCamera.transform.localPosition = originalCamPos;
            virtualCamera.transform.localRotation = originalCamRot;
        }

        private void TriggerStrongShortVibration()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                Handheld.Vibrate();
            }
            catch { }
#else
            Debug.Log("[ExplosionHazard] Strong short vibration triggered.");
#endif
        }

        // ── 3D Electrical Cabinet Construction ───────────────────────────────

        private void BuildElectricCabinetProp()
        {
            electricBoxGO = new GameObject("ElectricBox_HazardStation");
            electricBoxGO.transform.SetParent(transform, false);
            electricBoxGO.transform.position = electricBoxPosition;

            var stdShader = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse");

            // Main steel cabinet body mounted flush on tunnel wall
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "CabinetBody";
            body.transform.SetParent(electricBoxGO.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.22f, 0.85f, 0.60f);
            Destroy(body.GetComponent<Collider>());

            var bodyMat = new Material(stdShader);
            bodyMat.color = new Color(0.22f, 0.24f, 0.26f); // dark industrial steel
            body.GetComponent<Renderer>().material = bodyMat;

            // Charred opened cabinet door (ajar at 35 degrees)
            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "CabinetDoor_Ajar";
            door.transform.SetParent(electricBoxGO.transform, false);
            door.transform.localPosition = new Vector3(-0.12f, 0f, 0.26f);
            door.transform.localRotation = Quaternion.Euler(0f, -38f, 0f);
            door.transform.localScale = new Vector3(0.02f, 0.82f, 0.55f);
            Destroy(door.GetComponent<Collider>());

            var doorMat = new Material(stdShader);
            doorMat.color = new Color(0.12f, 0.10f, 0.08f); // burnt steel
            door.GetComponent<Renderer>().material = doorMat;

            // High-Voltage Warning Label
            var label = GameObject.CreatePrimitive(PrimitiveType.Cube);
            label.name = "HighVoltagePlate";
            label.transform.SetParent(electricBoxGO.transform, false);
            label.transform.localPosition = new Vector3(-0.12f, 0.22f, -0.05f);
            label.transform.localScale = new Vector3(0.015f, 0.20f, 0.20f);
            Destroy(label.GetComponent<Collider>());

            var labelMat = new Material(stdShader);
            labelMat.color = new Color(1.0f, 0.80f, 0.05f); // safety yellow
            labelMat.EnableKeyword("_EMISSION");
            labelMat.SetColor("_EmissionColor", new Color(1.0f, 0.80f, 0.05f) * 0.45f);
            label.GetComponent<Renderer>().material = labelMat;

            // Heavy electrical conduit cables running from tunnel ceiling and into floor
            for (int i = -1; i <= 1; i++)
            {
                var topCable = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                topCable.name = "ConduitCable_Top_" + i;
                topCable.transform.SetParent(electricBoxGO.transform, false);
                topCable.transform.localPosition = new Vector3(0f, 0.72f, i * 0.14f);
                topCable.transform.localScale = new Vector3(0.045f, 0.35f, 0.045f);
                Destroy(topCable.GetComponent<Collider>());
                var cableMat = new Material(stdShader);
                cableMat.color = new Color(0.10f, 0.10f, 0.10f);
                topCable.GetComponent<Renderer>().material = cableMat;

                var bottomCable = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bottomCable.name = "ConduitCable_Bottom_" + i;
                bottomCable.transform.SetParent(electricBoxGO.transform, false);
                bottomCable.transform.localPosition = new Vector3(0f, -0.70f, i * 0.14f);
                bottomCable.transform.localScale = new Vector3(0.045f, 0.35f, 0.045f);
                Destroy(bottomCable.GetComponent<Collider>());
                bottomCable.GetComponent<Renderer>().material = cableMat;
            }
        }

        // ── 3D Blast Visuals & Particle Construction ─────────────────────────

        private void BuildBlastVFX()
        {
            GameObject blastRoot = new GameObject("ExplosionVFX_Origin");
            blastRoot.transform.SetParent(transform, false);
            blastRoot.transform.position = electricBoxPosition + new Vector3(-0.2f, 0.1f, 0f);

            // Flash Point Light
            blastFlashLight = blastRoot.AddComponent<Light>();
            blastFlashLight.type = LightType.Point;
            blastFlashLight.color = new Color(1.0f, 0.88f, 0.60f); // bright explosion flash
            blastFlashLight.intensity = 0f;
            blastFlashLight.range = 18.0f;
            blastFlashLight.enabled = false;

            Material blendedMat = CreateParticleMat(false);
            Material additiveMat = CreateParticleMat(true);

            // 1. Dust Cloud (Expands and billows across corridor)
            blastDustPS = BuildParticleSystem(
                "BlastDust", blastRoot.transform, blendedMat,
                lifetime: new Vector2(2.5f, 4.2f),
                speed: new Vector2(2.0f, 5.5f),
                size: new Vector2(0.45f, 1.35f),
                startColor: new Color(0.35f, 0.32f, 0.28f, 0.88f),
                gravity: -0.04f,
                maxParticles: 160
            );

            // 2. Flying Rock Shrapnel / Fragments
            blastDebrisPS = BuildParticleSystem(
                "RockDebris", blastRoot.transform, blendedMat,
                lifetime: new Vector2(0.8f, 1.8f),
                speed: new Vector2(4.5f, 9.5f),
                size: new Vector2(0.08f, 0.22f),
                startColor: new Color(0.20f, 0.18f, 0.16f, 1.0f),
                gravity: 1.3f,
                maxParticles: 90
            );

            // 3. Ceiling Rock Shower
            ceilingRocksPS = BuildParticleSystem(
                "CeilingRockShower", blastRoot.transform, blendedMat,
                lifetime: new Vector2(1.0f, 2.4f),
                speed: new Vector2(0.5f, 2.2f),
                size: new Vector2(0.06f, 0.16f),
                startColor: new Color(0.26f, 0.22f, 0.18f, 1.0f),
                gravity: 1.8f,
                maxParticles: 70
            );

            // 4. Electrical Arcing Sparks from Cabinet
            cabinetSparkPS = BuildParticleSystem(
                "CabinetArcSparks", blastRoot.transform, additiveMat,
                lifetime: new Vector2(0.4f, 1.0f),
                speed: new Vector2(2.0f, 6.0f),
                size: new Vector2(0.04f, 0.10f),
                startColor: new Color(0.60f, 0.85f, 1.0f, 1.0f),
                gravity: 0.5f,
                maxParticles: 50
            );
        }

        private ParticleSystem BuildParticleSystem(string name, Transform parent, Material mat,
            Vector2 lifetime, Vector2 speed, Vector2 size, Color startColor, float gravity, int maxParticles)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var ps = go.AddComponent<ParticleSystem>();
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.material = mat;

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startColor = startColor;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.8f;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)maxParticles) });

            return ps;
        }

        private Material CreateParticleMat(bool additive)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended")
                ?? Shader.Find("Standard");

            Material mat = new Material(shader);
            if (additive)
            {
                mat.SetFloat("_Mode", 2);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            }
            return mat;
        }

        private void BuildBlastAudio()
        {
            GameObject audioGO = new GameObject("BlastAudioSource");
            audioGO.transform.SetParent(transform, false);
            blastAudioSource = audioGO.AddComponent<AudioSource>();
            blastAudioSource.clip = GenerateExplosionClip();
            blastAudioSource.spatialBlend = 0.0f; // 2D playback for maximum speaker punch
            blastAudioSource.volume = 1.0f;
            blastAudioSource.playOnAwake = false;
        }

        /// <summary>
        /// Synthesizes a subterranean blast with heavy sub-bass thump and resonant cavern rumble.
        /// </summary>
        private AudioClip GenerateExplosionClip()
        {
            int sampleRate = 44100;
            float duration = 2.4f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            float rumbleFreq = 52.0f;
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;

                // Sharp initial detonation transient
                float attack = Mathf.Clamp01(t / 0.012f);
                float decay  = Mathf.Exp(-3.4f * t);
                float envelope = attack * decay;

                // Sub-bass sine wave decaying in frequency (pitch drop shockwave)
                float currentFreq = Mathf.Lerp(rumbleFreq, 28.0f, t / duration);
                float subBass = Mathf.Sin(2.0f * Mathf.PI * currentFreq * t);

                // Chaotic noise crunch (rock fracturing + electric arc pop)
                float noise = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-6.5f * t);

                // Cavern low-frequency rumble resonance
                float rumble = Mathf.Sin(2.0f * Mathf.PI * 38.0f * t) * 0.45f * Mathf.Exp(-1.8f * t);

                samples[i] = Mathf.Clamp((subBass * 0.70f + noise * 0.55f + rumble) * envelope, -1.0f, 1.0f);
            }

            AudioClip clip = AudioClip.Create("UndergroundBlast", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
