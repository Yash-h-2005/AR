using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace SurakshaAR.Training.Fire
{
    /// <summary>
    /// Sub-Module 3: Mine Collapse Controller.
    ///
    /// Manages:
    ///   1. Moving 3D physical collapse front advancing BEHIND the worker.
    ///   2. Progressive blockage using tumbled boulders, crushed timber props, and falling rock particles.
    ///   3. Rubber-banded chase mechanics to ensure fair and engaging physical evacuation.
    ///   4. Proximity danger detection with increasing rumbling, dust, camera shake, and warnings.
    ///   5. Scenario failure handling with full-screen Retry overlay if caught.
    ///   6. Safe portal exit detection ($Z \ge 75m$) halting the collapse behind the worker.
    /// </summary>
    public class MineCollapseController : MonoBehaviour
    {
        // ── Inspector References ─────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private MineMovementController movementController;
        [SerializeField] private Camera virtualCamera;
        [SerializeField] private MineSimulatorHUD hud;

        [Header("Failure UI Overlay")]
        [SerializeField] private GameObject failureOverlayPanel;
        [SerializeField] private Text failureTitleText;
        [SerializeField] private Text failureDescText;
        [SerializeField] private Button retryButton;

        [Header("Chase Settings")]
        [Tooltip("Base speed in m/s at which the collapse advances behind the worker.")]
        [SerializeField] private float baseCollapseSpeed = 0.85f;
        [Tooltip("Target distance to maintain behind the worker when worker is actively moving.")]
        [SerializeField] private float idealChaseDistance = 5.5f;
        [Tooltip("Minimum safe distance before danger warnings and tremors trigger.")]
        [SerializeField] private float dangerDistance = 3.2f;
        [Tooltip("Distance at which the collapse catches the worker and triggers failure.")]
        [SerializeField] private float fatalDistance = 1.1f;

        [Header("Assessment Configuration")]
        [SerializeField] private bool isRouteBEmergencyExit = false;

        public bool IsRouteBEmergencyExit
        {
            get => isRouteBEmergencyExit;
            set => isRouteBEmergencyExit = value;
        }

        public event Action OnEvacuatedToSafeGround;
        public event Action OnPlayerCaughtInCollapse;

        // ── State ─────────────────────────────────────────────────────────────
        public bool IsCollapseActive { get; private set; } = false;
        public bool IsPlayerCaught { get; private set; } = false;
        public bool IsEvacuatedToSafeGround { get; private set; } = false;

        public float CurrentCollapseZ { get; private set; } = 0f;

        // 3D Visual & Audio Elements
        private GameObject collapseRoot;
        private AudioSource rumbleAudioSource;
        private ParticleSystem fallingRocksPS;
        private ParticleSystem heavyDustPS;

        private float warningCooldown = 0f;
        private float intermittentVibeTimer = 3.5f;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Start()
        {
            if (failureOverlayPanel != null)
                failureOverlayPanel.SetActive(false);

            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetryClicked);

            BuildCollapseProps();
            SetCollapseVisualsActive(false);
        }

        private void Update()
        {
            if (!IsCollapseActive || IsPlayerCaught || IsEvacuatedToSafeGround) return;

            UpdateCollapsePosition();
            CheckWorkerProximity();
            CheckExitReached();
            UpdateIntermittentVibration();
        }

        private void UpdateIntermittentVibration()
        {
            intermittentVibeTimer -= Time.deltaTime;
            if (intermittentVibeTimer <= 0f)
            {
                float playerZ = (virtualCamera != null) ? virtualCamera.transform.position.z : CurrentCollapseZ + 5f;
                float dist = playerZ - CurrentCollapseZ;

                // Urgency-based intervals:
                // Close behind (< 3.2m): short vibration every 2.0s
                // Safe buffer (> 3.2m): warning pulse every 3.8s
                intermittentVibeTimer = (dist < dangerDistance) ? 2.0f : 3.8f;

#if UNITY_ANDROID && !UNITY_EDITOR
                try { Handheld.Vibrate(); } catch { }
#endif
            }
        }

        /// <summary>
        /// Starts the moving collapse sequence behind the worker.
        /// </summary>
        public void StartCollapse(float startZ)
        {
            CurrentCollapseZ = startZ;
            IsCollapseActive = true;
            IsPlayerCaught = false;
            IsEvacuatedToSafeGround = false;

            SetCollapseVisualsActive(true);

            if (rumbleAudioSource != null)
            {
                rumbleAudioSource.volume = 0.85f;
                rumbleAudioSource.Play();
            }

            UpdateCollapseTransform();
            Debug.Log($"[MineCollapse] Collapse initiated at Z={CurrentCollapseZ:F1}m behind worker.");
        }

        private void UpdateCollapsePosition()
        {
            if (virtualCamera == null) return;

            float playerZ = virtualCamera.transform.position.z;
            float distanceToPlayer = playerZ - CurrentCollapseZ;

            // Rubber-banding speed adjustment:
            // If player is well ahead, collapse speeds up slightly to stay menacing.
            // If player slows down, collapse advances at base speed, giving them time to react.
            float currentSpeed = baseCollapseSpeed;
            if (distanceToPlayer > idealChaseDistance + 2.0f)
            {
                currentSpeed = Mathf.Min(1.4f, baseCollapseSpeed * 1.35f);
            }
            else if (distanceToPlayer < dangerDistance)
            {
                // Never stop completely, but don't accelerate into the player unfairly
                currentSpeed = baseCollapseSpeed * 0.80f;
            }

            CurrentCollapseZ += currentSpeed * Time.deltaTime;
            UpdateCollapseTransform();
        }

        private void UpdateCollapseTransform()
        {
            if (collapseRoot == null) return;

            // Calculate tunnel X based on tunnel path from MineEnvironmentBuilder:
            // Z <= 29m: X = 0
            // Z between 29m and 48m: Y-junction branch curves to target exit X (+8.5m or -8.5m)
            // Z >= 48m: X = target exit X
            float targetExitX = isRouteBEmergencyExit ? 8.50f : -8.50f;
            float collapseX = 0f;
            if (CurrentCollapseZ > 29.0f && CurrentCollapseZ < 48.0f)
            {
                float t = (CurrentCollapseZ - 29.0f) / (48.0f - 29.0f);
                collapseX = Mathf.Lerp(0f, targetExitX, t * t * (3f - 2f * t));
            }
            else if (CurrentCollapseZ >= 48.0f)
            {
                collapseX = targetExitX;
            }

            collapseRoot.transform.position = new Vector3(collapseX, 0f, CurrentCollapseZ);
        }

        private void CheckWorkerProximity()
        {
            if (virtualCamera == null) return;

            float playerZ = virtualCamera.transform.position.z;
            float distanceToPlayer = playerZ - CurrentCollapseZ;

            warningCooldown -= Time.deltaTime;

            // Danger zone warning
            if (distanceToPlayer <= dangerDistance && distanceToPlayer > fatalDistance)
            {
                if (warningCooldown <= 0f)
                {
                    warningCooldown = 3.0f;
                    if (hud != null)
                    {
                        hud.ShowStatusToast("⚠️ COLLAPSE APPROACHING!\nRUN TOWARD THE EXIT!", new Color(1.0f, 0.35f, 0.10f), 2.5f);
                    }
                    TriggerIntermittentTremor();
                }
            }
            // Fatal catch condition
            else if (distanceToPlayer <= fatalDistance)
            {
                TriggerFailure();
            }
        }

        private void TriggerIntermittentTremor()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try { Handheld.Vibrate(); } catch { }
#endif
            StartCoroutine(MinorCameraTremor());
        }

        private IEnumerator MinorCameraTremor()
        {
            if (virtualCamera == null) yield break;
            Vector3 origPos = virtualCamera.transform.localPosition;
            float dur = 0.6f;
            float elapsed = 0f;

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float d = 1f - (elapsed / dur);
                float x = (UnityEngine.Random.value - 0.5f) * 0.04f * d;
                float y = (UnityEngine.Random.value - 0.5f) * 0.04f * d;
                virtualCamera.transform.localPosition = origPos + new Vector3(x, y, 0);
                yield return null;
            }

            virtualCamera.transform.localPosition = origPos;
        }

        private void CheckExitReached()
        {
            if (virtualCamera == null) return;

            // Outside portal mouth is at Z >= 74m (opening onto open ground toward assembly point at Z=94m)
            Vector3 pos = virtualCamera.transform.position;
            if (pos.z >= 74.0f)
            {
                SurviveCollapseAndReachOpenGround();
            }
        }

        private void SurviveCollapseAndReachOpenGround()
        {
            if (IsEvacuatedToSafeGround) return;
            IsEvacuatedToSafeGround = true;

            Debug.Log("[MineCollapse] 🟢 Worker reached open ground outside portal! Collapse halted.");

            // Stop collapse progression behind player
            if (rumbleAudioSource != null)
            {
                StartCoroutine(FadeOutRumble());
            }

            if (hud != null)
            {
                hud.ShowStatusToast("✅ Safe ground reached!\nProceed to Emergency Assembly Point.", new Color(0.15f, 0.95f, 0.45f), 4.5f);
                hud.SetObjective("Report to the Emergency Assembly Point.");
                hud.SetBadge("OUTDOORS", new Color(0.15f, 0.92f, 0.45f));
            }

            OnEvacuatedToSafeGround?.Invoke();
        }

        private IEnumerator FadeOutRumble()
        {
            float elapsed = 0f;
            float duration = 2.5f;
            float startVol = rumbleAudioSource.volume;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                rumbleAudioSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                yield return null;
            }
            rumbleAudioSource.Stop();
        }

        private void TriggerFailure()
        {
            IsPlayerCaught = true;
            Debug.Log("[MineCollapse] ❌ WORKER CAUGHT IN COLLAPSE — Evacuation Failed.");

            if (rumbleAudioSource != null)
                rumbleAudioSource.Stop();

            OnPlayerCaughtInCollapse?.Invoke();

            if (failureOverlayPanel != null)
            {
                failureOverlayPanel.SetActive(true);
            }

            if (failureTitleText != null)
            {
                failureTitleText.text = "⚠️ Evacuation Failed";
            }

            if (failureDescText != null)
            {
                failureDescText.text = "The mine tunnel collapsed behind you.\nYou must move quickly toward the emergency exit when the alarm sounds.";
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            try { Handheld.Vibrate(); } catch { }
#endif
        }

        private void OnRetryClicked()
        {
            Debug.Log("[MineCollapse] Worker tapped Retry — reloading scene.");
            string currentScene = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(currentScene);
        }

        // ── 3D Environmental Collapse Construction ────────────────────────────

        private void BuildCollapseProps()
        {
            collapseRoot = new GameObject("MovingCollapse_Front");
            collapseRoot.transform.SetParent(transform, false);

            var stdShader = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse");

            // 1. Tumbled Large Rock Boulders (blocking tunnel width)
            for (int i = 0; i < 5; i++)
            {
                var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rock.name = $"CollapseBoulder_{i + 1}";
                rock.transform.SetParent(collapseRoot.transform, false);

                float xPos = (i - 2) * 0.52f;
                float yPos = UnityEngine.Random.Range(0.35f, 0.95f);
                float zPos = UnityEngine.Random.Range(-0.8f, 0.2f);
                rock.transform.localPosition = new Vector3(xPos, yPos, zPos);
                rock.transform.localRotation = Quaternion.Euler(
                    UnityEngine.Random.Range(15f, 45f),
                    UnityEngine.Random.Range(0f, 180f),
                    UnityEngine.Random.Range(15f, 45f)
                );
                rock.transform.localScale = new Vector3(
                    UnityEngine.Random.Range(0.65f, 0.95f),
                    UnityEngine.Random.Range(0.60f, 0.90f),
                    UnityEngine.Random.Range(0.65f, 0.95f)
                );

                Destroy(rock.GetComponent<Collider>());
                var rockMat = new Material(stdShader);
                rockMat.color = new Color(0.22f, 0.20f, 0.18f); // dark cave stone
                rockMat.SetFloat("_Glossiness", 0.15f);
                rock.GetComponent<Renderer>().material = rockMat;
            }

            // 2. Broken / Splintered Timber Props
            for (int i = 0; i < 2; i++)
            {
                var timber = GameObject.CreatePrimitive(PrimitiveType.Cube);
                timber.name = $"CrushedTimber_{i + 1}";
                timber.transform.SetParent(collapseRoot.transform, false);
                timber.transform.localPosition = new Vector3((i == 0 ? -0.5f : 0.5f), 0.75f, -0.4f);
                timber.transform.localRotation = Quaternion.Euler(0f, 0f, (i == 0 ? 32f : -38f));
                timber.transform.localScale = new Vector3(0.18f, 1.6f, 0.18f);

                Destroy(timber.GetComponent<Collider>());
                var woodMat = new Material(stdShader);
                woodMat.color = new Color(0.32f, 0.22f, 0.12f); // splintered mine timber
                timber.GetComponent<Renderer>().material = woodMat;
            }

            // 3. Falling Rocks Particle Shower (cascading from ceiling right at the collapse edge)
            Material blendedMat = CreateParticleMat();
            fallingRocksPS = BuildParticleSystem(
                "FallingRocksPS", collapseRoot.transform, blendedMat,
                lifetime: new Vector2(0.6f, 1.2f),
                speed: new Vector2(3.0f, 6.5f),
                size: new Vector2(0.06f, 0.18f),
                startColor: new Color(0.25f, 0.22f, 0.20f, 1.0f),
                rate: 35f,
                gravity: 2.2f
            );

            // 4. Heavy Rolling Dust Cloud
            heavyDustPS = BuildParticleSystem(
                "HeavyDustPS", collapseRoot.transform, blendedMat,
                lifetime: new Vector2(2.0f, 3.5f),
                speed: new Vector2(1.2f, 2.5f),
                size: new Vector2(0.6f, 1.6f),
                startColor: new Color(0.28f, 0.25f, 0.22f, 0.75f),
                rate: 24f,
                gravity: -0.1f
            );

            // 5. Rumble Audio Source
            var audioGO = new GameObject("CollapseRumbleAudio");
            audioGO.transform.SetParent(collapseRoot.transform, false);
            rumbleAudioSource = audioGO.AddComponent<AudioSource>();
            rumbleAudioSource.clip = GenerateContinuousRumbleClip();
            rumbleAudioSource.loop = true;
            rumbleAudioSource.spatialBlend = 0.5f; // Partially 3D spatialized so it sounds behind player
            rumbleAudioSource.minDistance = 3.0f;
            rumbleAudioSource.maxDistance = 25.0f;
            rumbleAudioSource.volume = 0.90f;
            rumbleAudioSource.playOnAwake = false;
        }

        private ParticleSystem BuildParticleSystem(string name, Transform parent, Material mat,
            Vector2 lifetime, Vector2 speed, Vector2 size, Color startColor, float rate, float gravity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var ps = go.AddComponent<ParticleSystem>();
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.material = mat;

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startColor = startColor;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 120;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(2.4f, 0.4f, 1.2f);
            shape.position = new Vector3(0f, 2.1f, 0f);

            var emission = ps.emission;
            emission.rateOverTime = rate;

            return ps;
        }

        private Material CreateParticleMat()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended")
                ?? Shader.Find("Standard");

            return new Material(shader);
        }

        private void SetCollapseVisualsActive(bool active)
        {
            if (collapseRoot != null)
                collapseRoot.SetActive(active);

            if (active)
            {
                if (fallingRocksPS != null) fallingRocksPS.Play();
                if (heavyDustPS != null) heavyDustPS.Play();
            }
            else
            {
                if (fallingRocksPS != null) fallingRocksPS.Stop();
                if (heavyDustPS != null) heavyDustPS.Stop();
            }
        }

        /// <summary>
        /// Synthesizes a low-frequency continuous cavern rumble and rock grinding sound.
        /// </summary>
        private AudioClip GenerateContinuousRumbleClip()
        {
            int sampleRate = 44100;
            float duration = 3.0f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float r1 = Mathf.Sin(2.0f * Mathf.PI * 34.0f * t);
                float r2 = Mathf.Sin(2.0f * Mathf.PI * 46.0f * t) * 0.7f;
                float r3 = Mathf.Sin(2.0f * Mathf.PI * 62.0f * t + Mathf.Sin(t * 12f)) * 0.4f;
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.25f;

                samples[i] = Mathf.Clamp((r1 + r2 + r3 + noise) * 0.35f, -1.0f, 1.0f);
            }

            AudioClip clip = AudioClip.Create("ContinuousRumble", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Wires the UI failure overlay from SceneBuilderScript.
        /// </summary>
        public void SetFailureUI(GameObject panel, Text title, Text desc, Button btn)
        {
            failureOverlayPanel = panel;
            failureTitleText = title;
            failureDescText = desc;
            retryButton = btn;

            if (failureOverlayPanel != null)
                failureOverlayPanel.SetActive(false);

            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(OnRetryClicked);
                retryButton.onClick.AddListener(OnRetryClicked);
            }
        }
    }
}
