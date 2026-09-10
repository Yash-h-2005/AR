using System.Collections;
using UnityEngine;

namespace SurakshaAR.Training.Fire
{
    /// <summary>
    /// PHASE 2A — Fire Incident Controller.
    ///
    /// Manages:
    ///   1. Dormant state: fire inactive until player reaches ~10m virtual distance.
    ///   2. Trigger: fire spawned at the wall-mounted electrical cabinet at Z≈10.
    ///   3. Growth phase: fire, smoke, and flicker light all scale up over 3s.
    ///   4. Crackling audio generated procedurally via AudioSource.
    ///   5. Events for the HUD and EmergencyAlarmController to subscribe to.
    /// </summary>
    public class FireHazardController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private MineMovementController movementController;

        [Header("Trigger")]
        [Tooltip("Virtual distance in metres at which the fire incident triggers (14m outdoor + 10m underground).")]
        [SerializeField] private float triggerDistance = 24f;

        [Header("Fire Object (set by MineEnvironmentBuilder)")]
        [SerializeField] private Transform fireAnchor; // world position of the fire origin

        [Header("Scenario Configuration")]
        [SerializeField] private FireScenarioType scenarioType = FireScenarioType.ElectricalEquipment;
        public FireScenarioType ScenarioType
        {
            get => scenarioType;
            set => scenarioType = value;
        }

        // ── State ──────────────────────────────────────────────────────────
        public enum FirePhase { Dormant, Growing, Burning }
        public FirePhase CurrentPhase { get; private set; } = FirePhase.Dormant;

        // ── Events ────────────────────────────────────────────────────────
        public event System.Action OnFireTriggered;
        public event System.Action OnFireFullyGrown;
        public event System.Action OnFireExtinguished;   // Phase 2B-2
        public event System.Action OnFireFlaredUp;       // Phase 2B-2

        // ── Visual Components ──────────────────────────────────────────────
        private GameObject fireRoot;
        private ParticleSystem coreFlamePS;
        private ParticleSystem turbulentFlamePS;
        private ParticleSystem secondaryFlamePS;
        private ParticleSystem smokePS;
        private ParticleSystem embersPS;
        private ParticleSystem arcSparkPS;
        private Light fireLight;

        // ── Audio ──────────────────────────────────────────────────────────
        private AudioSource crackleSource;

        // ── Growth ────────────────────────────────────────────────────────
        private float growthDuration = 3.0f;
        private float growthTimer = 0f;

        // ── Lifecycle ─────────────────────────────────────────────────────

        private void Start()
        {
            BuildFireVFX();
            SetFireActive(false);
        }

        private void Update()
        {
            if (CurrentPhase == FirePhase.Dormant)
            {
                CheckTrigger();
            }
            else if (CurrentPhase == FirePhase.Growing)
            {
                UpdateGrowth();
            }
            else if (CurrentPhase == FirePhase.Burning)
            {
                UpdateBurning();
            }
        }

        // ── Trigger Check ─────────────────────────────────────────────────

        private void CheckTrigger()
        {
            if (movementController == null) return;
            if (movementController.CurrentState != TrackingState.Tracking) return;

            // Trigger when player reaches the electrical hazard location inside the mine (Z >= 9.5m)
            // or when walk distance from outdoor start reaches triggerDistance (24m)
            if (movementController.VirtualPosition.z >= 9.5f || movementController.EstimatedWalkDistance >= triggerDistance)
            {
                TriggerFireIncident();
            }
        }

        private void TriggerFireIncident()
        {
            CurrentPhase = FirePhase.Growing;
            growthTimer = 0f;

            SetFireActive(true);
            SetFireScale(0.1f); // start tiny

            // Start crackling audio
            if (crackleSource != null)
            {
                crackleSource.volume = 0f;
                crackleSource.Play();
            }

            OnFireTriggered?.Invoke();
            Debug.Log("[FireHazard] FIRE INCIDENT TRIGGERED at distance: " + movementController.EstimatedWalkDistance.ToString("F2") + "m");
        }

        // ── Growth Phase ──────────────────────────────────────────────────

        private void UpdateGrowth()
        {
            growthTimer += Time.deltaTime;
            float t = Mathf.Clamp01(growthTimer / growthDuration);
            float easedT = t * t * (3f - 2f * t); // smoothstep

            SetFireScale(easedT);

            // Ramp up fire light intensity
            if (fireLight != null)
            {
                fireLight.intensity = Mathf.Lerp(0f, 5.8f, easedT);
                fireLight.range = Mathf.Lerp(0f, 7.5f, easedT);
            }

            // Ramp up crackle audio
            if (crackleSource != null)
            {
                crackleSource.volume = Mathf.Lerp(0f, 0.65f, easedT);
            }

            if (growthTimer >= growthDuration)
            {
                CurrentPhase = FirePhase.Burning;
                OnFireFullyGrown?.Invoke();
                Debug.Log("[FireHazard] Fire fully grown — BURNING state.");
            }
        }

        // ── Burning Phase (flicker + maintain) ────────────────────────────

        private float _noiseOffset;
        private void UpdateBurning()
        {
            if (fireLight != null)
            {
                float n1 = Mathf.PerlinNoise(Time.time * 16f + _noiseOffset, 0f);
                float n2 = Mathf.PerlinNoise(0f, Time.time * 26f + _noiseOffset);
                float n3 = Mathf.PerlinNoise(Time.time * 7f, Time.time * 7f);
                float combined = n1 * 0.5f + n2 * 0.3f + n3 * 0.2f;
                fireLight.intensity = 5.8f * Mathf.Lerp(0.70f, 1.35f, combined);
                fireLight.range     = 7.5f * Mathf.Lerp(0.85f, 1.18f, n1);
            }
        }

        // ── VFX Construction ──────────────────────────────────────────────

        private void BuildFireVFX()
        {
            _noiseOffset = Random.Range(0f, 100f);

            // Root positioned at fire anchor (set from builder)
            fireRoot = new GameObject("FireVFX_Root");
            fireRoot.transform.SetParent(transform, false);
            if (fireAnchor != null)
            {
                fireRoot.transform.position = fireAnchor.position;
                fireRoot.transform.rotation = fireAnchor.rotation;
            }

            // ── Materials ──────────────────────────────────────────────────
            Material additiveMat = CreateAdditiveParticleMat();
            Material blendedMat  = CreateAlphaBlendedParticleMat();

            // ── 1. Core Bright Flames (White-hot to Yellow — inside cabinet) ────
            coreFlamePS = BuildParticleLayer(
                "CoreFlames", fireRoot.transform, new Vector3(-0.02f, 0.35f, 0.05f),
                additiveMat,
                lifetime: new Vector2(0.40f, 0.70f),
                speed: new Vector2(0.8f, 1.6f),
                size: new Vector2(0.22f, 0.38f),
                rateOverTime: 55f,
                startColor: new Color(1f, 0.95f, 0.75f, 0.95f),
                shapeAngle: 10f, shapeRadius: 0.06f,
                colorOverLifetime: BuildFlameGradient(),
                isSmoke: false
            );

            // ── 2. Turbulent Outer Flames (bursting out of cabinet opening) ───
            turbulentFlamePS = BuildParticleLayer(
                "TurbulentFlames", fireRoot.transform, new Vector3(-0.05f, 0.40f, 0.10f),
                additiveMat,
                lifetime: new Vector2(0.50f, 0.90f),
                speed: new Vector2(0.5f, 1.1f),
                size: new Vector2(0.30f, 0.58f),
                rateOverTime: 32f,
                startColor: new Color(1f, 0.45f, 0.05f, 0.75f),
                shapeAngle: 22f, shapeRadius: 0.10f,
                colorOverLifetime: BuildOuterFlameGradient(),
                isSmoke: false
            );

            // ── 2b. Secondary Flames Licking Around Charred Cabinet Door ─────
            secondaryFlamePS = BuildParticleLayer(
                "SecondaryFlames", fireRoot.transform, new Vector3(-0.10f, 0.38f, 0.22f),
                additiveMat,
                lifetime: new Vector2(0.35f, 0.65f),
                speed: new Vector2(0.40f, 0.95f),
                size: new Vector2(0.18f, 0.34f),
                rateOverTime: 28f,
                startColor: new Color(1f, 0.65f, 0.10f, 0.85f),
                shapeAngle: 18f, shapeRadius: 0.08f,
                colorOverLifetime: BuildOuterFlameGradient(),
                isSmoke: false
            );

            // ── 3. Billowing Dark Volumetric Smoke (rising from cabinet top) ───
            smokePS = BuildParticleLayer(
                "Smoke", fireRoot.transform, new Vector3(0.0f, 0.65f, 0.08f),
                blendedMat,
                lifetime: new Vector2(2.5f, 4.5f),
                speed: new Vector2(0.20f, 0.50f),
                size: new Vector2(0.35f, 0.70f),
                rateOverTime: 22f,
                startColor: new Color(0.12f, 0.11f, 0.10f, 0.65f),
                shapeAngle: 28f, shapeRadius: 0.12f,
                colorOverLifetime: BuildSmokeGradient(),
                isSmoke: true
            );

            // ── 4. Flying Embers (drifting upward from fire source) ───────────
            embersPS = BuildParticleLayer(
                "Embers", fireRoot.transform, new Vector3(-0.02f, 0.45f, 0.08f),
                additiveMat,
                lifetime: new Vector2(1.2f, 2.4f),
                speed: new Vector2(0.8f, 2.4f),
                size: new Vector2(0.02f, 0.06f),
                rateOverTime: 14f,
                startColor: new Color(1f, 0.65f, 0.08f, 1.0f),
                shapeAngle: 35f, shapeRadius: 0.06f,
                colorOverLifetime: BuildEmberGradient(),
                gravity: -0.3f,
                isSmoke: false
            );

            // ── 5. Electrical Arc Sparks (crackling from wire connections, electrical only) ────
            if (scenarioType == FireScenarioType.ElectricalEquipment)
            {
                arcSparkPS = BuildParticleLayer(
                    "ArcSparks", fireRoot.transform, new Vector3(-0.05f, 0.45f, 0.08f),
                    additiveMat,
                    lifetime: new Vector2(0.10f, 0.30f),
                    speed: new Vector2(2.5f, 5.0f),
                    size: new Vector2(0.04f, 0.12f),
                    rateOverTime: 8f,
                    startColor: new Color(0.65f, 0.88f, 1.0f, 1.0f), // blue-white arc flash
                    shapeAngle: 65f, shapeRadius: 0.08f,
                    colorOverLifetime: BuildArcSparkGradient(),
                    gravity: 0.5f,
                    isSmoke: false
                );
            }

            // ── 6. Dynamic Fire Light (Realistic Illumination) ─────────────
            var lightGO = new GameObject("FireLight");
            lightGO.transform.SetParent(fireRoot.transform, false);
            lightGO.transform.localPosition = new Vector3(-0.05f, 0.45f, 0.10f);
            fireLight = lightGO.AddComponent<Light>();
            fireLight.type = LightType.Point;
            fireLight.color = new Color(1f, 0.52f, 0.14f);
            fireLight.intensity = 0f;
            fireLight.range = 0f;
            fireLight.shadows = LightShadows.None;

            // ── 7. Crackle Audio Source ────────────────────────────────────
            var audioGO = new GameObject("CrackleAudio");
            audioGO.transform.SetParent(fireRoot.transform, false);
            crackleSource = audioGO.AddComponent<AudioSource>();
            crackleSource.clip = GenerateCrackleClip();
            crackleSource.loop = true;
            crackleSource.spatialBlend = 1f; // 3D
            crackleSource.minDistance = 1.5f;
            crackleSource.maxDistance = 14f;
            crackleSource.rolloffMode = AudioRolloffMode.Linear;
            crackleSource.volume = 0f;
            crackleSource.playOnAwake = false;

            // ── 8. Electrical Equipment Context Clues ──────────────────────
            BuildElectricalCabinet();

            Debug.Log("[FireHazard] Realistic Electrical Fire VFX & Cabinet constructed.");
        }

        // ── Electrical Cabinet Clues (Deterministic Class C Scenario) ──────

        private void BuildElectricalCabinet()
        {
            if (fireAnchor == null) return;

            var cabinetRoot = new GameObject("ElectricalCabinet_Hazard");
            cabinetRoot.transform.SetParent(fireAnchor, false);
            cabinetRoot.transform.localPosition = new Vector3(0.05f, 0.45f, 0f);

            var stdShader = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse");

            // Main steel cabinet body mounted on tunnel wall
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "CabinetBody";
            body.transform.SetParent(cabinetRoot.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.18f, 0.70f, 0.52f);
            Destroy(body.GetComponent<Collider>());

            var bodyMat = new Material(stdShader);
            bodyMat.color = new Color(0.24f, 0.26f, 0.28f); // dark industrial steel
            body.GetComponent<Renderer>().material = bodyMat;

            // Charred opened cabinet door (ajar at 35 degrees)
            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "CabinetDoor_Charred";
            door.transform.SetParent(cabinetRoot.transform, false);
            door.transform.localPosition = new Vector3(-0.10f, 0f, 0.24f);
            door.transform.localRotation = Quaternion.Euler(0f, -35f, 0f);
            door.transform.localScale = new Vector3(0.02f, 0.68f, 0.48f);
            Destroy(door.GetComponent<Collider>());

            var doorMat = new Material(stdShader);
            doorMat.color = new Color(0.12f, 0.10f, 0.08f); // burnt soot steel
            door.GetComponent<Renderer>().material = doorMat;

            // High-Voltage Warning Label (yellow safety plate)
            var label = GameObject.CreatePrimitive(PrimitiveType.Cube);
            label.name = "HighVoltageLabel";
            label.transform.SetParent(cabinetRoot.transform, false);
            label.transform.localPosition = new Vector3(-0.10f, 0.18f, -0.05f);
            label.transform.localScale = new Vector3(0.01f, 0.16f, 0.16f);
            Destroy(label.GetComponent<Collider>());

            var labelMat = new Material(stdShader);
            labelMat.color = new Color(1.0f, 0.82f, 0.05f); // safety yellow
            labelMat.EnableKeyword("_EMISSION");
            labelMat.SetColor("_EmissionColor", new Color(1.0f, 0.82f, 0.05f) * 0.4f);
            label.GetComponent<Renderer>().material = labelMat;

            // Warning symbol inside label (dark lightning icon representation)
            var symbol = GameObject.CreatePrimitive(PrimitiveType.Cube);
            symbol.name = "LightningSymbol";
            symbol.transform.SetParent(label.transform, false);
            symbol.transform.localPosition = new Vector3(-0.55f, 0f, 0f);
            symbol.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
            symbol.transform.localScale = new Vector3(0.1f, 0.65f, 0.20f);
            Destroy(symbol.GetComponent<Collider>());

            var symMat = new Material(stdShader);
            symMat.color = Color.black;
            symbol.GetComponent<Renderer>().material = symMat;

            // Heavy electrical conduit cables running from tunnel roof into top of cabinet
            for (int i = -1; i <= 1; i++)
            {
                var cable = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cable.name = "PowerCable_" + i;
                cable.transform.SetParent(cabinetRoot.transform, false);
                cable.transform.localPosition = new Vector3(0f, 0.65f, i * 0.12f);
                cable.transform.localScale = new Vector3(0.045f, 0.35f, 0.045f);
                Destroy(cable.GetComponent<Collider>());

                var cableMat = new Material(stdShader);
                cableMat.color = (i == 0) ? new Color(0.9f, 0.6f, 0.1f) : new Color(0.12f, 0.12f, 0.12f);
                cable.GetComponent<Renderer>().material = cableMat;
            }
        }

        private Gradient BuildArcSparkGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.7f, 0.9f, 1f), 0.0f),
                    new GradientColorKey(new Color(1f, 1f, 0.8f), 0.4f),
                    new GradientColorKey(new Color(1f, 0.5f, 0.1f), 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(0.9f, 0.5f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            return g;
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private void SetFireActive(bool active)
        {
            if (fireRoot == null) return;
            var systems = new ParticleSystem[] { coreFlamePS, turbulentFlamePS, secondaryFlamePS, smokePS, embersPS, arcSparkPS };
            foreach (var ps in systems)
            {
                if (ps == null) continue;
                if (active) ps.Play();
                else        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            if (fireLight != null) fireLight.enabled = active;
        }

        private void SetFireScale(float t)
        {
            if (fireRoot == null) return;
            float s = Mathf.Lerp(0.3f, 1.5f, t);
            fireRoot.transform.localScale = new Vector3(s, s, s);
        }

        // ── Particle Layer Builder ─────────────────────────────────────────

        private ParticleSystem BuildParticleLayer(
            string name, Transform parent, Vector3 localPos,
            Material mat,
            Vector2 lifetime, Vector2 speed, Vector2 size,
            float rateOverTime, Color startColor,
            float shapeAngle, float shapeRadius,
            Gradient colorOverLifetime = null,
            float gravity = 0f,
            bool isSmoke = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var ps = go.AddComponent<ParticleSystem>();
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.material = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.alignment = ParticleSystemRenderSpace.View;
            r.enableGPUInstancing = false;
            r.sortingOrder = isSmoke ? 0 : 1;

            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
            main.startSpeed    = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize     = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startColor    = startColor;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles  = 250;
            main.gravityModifier = gravity;

            var emission = ps.emission;
            emission.rateOverTime = rateOverTime;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle     = shapeAngle;
            shape.radius    = shapeRadius;
            shape.position  = Vector3.zero;
            shape.rotation  = new Vector3(-90f, 0f, 0f); // emit upward

            if (colorOverLifetime != null)
            {
                var col = ps.colorOverLifetime;
                col.enabled = true;
                col.color = new ParticleSystem.MinMaxGradient(colorOverLifetime);
            }

            // Realistic size curve: smoke expands outward, fire tapers into point
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            if (isSmoke)
            {
                sizeCurve.AddKey(0.0f, 0.45f);
                sizeCurve.AddKey(0.4f, 1.25f);
                sizeCurve.AddKey(1.0f, 2.40f);
            }
            else
            {
                sizeCurve.AddKey(0.0f, 0.65f);
                sizeCurve.AddKey(0.3f, 1.15f);
                sizeCurve.AddKey(1.0f, 0.15f);
            }
            sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Subtle rotation over lifetime
            var rol = ps.rotationOverLifetime;
            rol.enabled = true;
            rol.z = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f);

            return ps;
        }

        // ── Gradients ─────────────────────────────────────────────────────

        private Gradient BuildFlameGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 1f, 0.8f), 0.0f),
                    new GradientColorKey(new Color(1f, 0.60f, 0.1f), 0.5f),
                    new GradientColorKey(new Color(0.9f, 0.2f, 0.05f), 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.95f, 0.0f),
                    new GradientAlphaKey(0.6f,  0.6f),
                    new GradientAlphaKey(0.0f,  1.0f)
                }
            );
            return g;
        }

        private Gradient BuildOuterFlameGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.55f, 0.05f), 0.0f),
                    new GradientColorKey(new Color(0.8f, 0.15f, 0.02f), 0.65f),
                    new GradientColorKey(new Color(0.3f, 0.05f, 0.02f), 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.7f,  0.0f),
                    new GradientAlphaKey(0.4f,  0.5f),
                    new GradientAlphaKey(0.0f,  1.0f)
                }
            );
            return g;
        }

        private Gradient BuildSmokeGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.25f, 0.20f, 0.15f), 0.0f),
                    new GradientColorKey(new Color(0.10f, 0.09f, 0.08f), 0.4f),
                    new GradientColorKey(new Color(0.06f, 0.06f, 0.06f), 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.0f,  0.0f),
                    new GradientAlphaKey(0.55f, 0.2f),
                    new GradientAlphaKey(0.35f, 0.7f),
                    new GradientAlphaKey(0.0f,  1.0f)
                }
            );
            return g;
        }

        private Gradient BuildEmberGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.9f, 0.4f), 0.0f),
                    new GradientColorKey(new Color(1f, 0.4f, 0.05f), 0.5f),
                    new GradientColorKey(new Color(0.4f, 0.1f, 0.0f), 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(0.6f, 0.5f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            return g;
        }

        // ── Procedural Soft Textures & Materials ───────────────────────────

        private static Texture2D _flameTex;
        private static Texture2D _smokeTex;

        private static Texture2D GetOrCreateFlameTexture()
        {
            if (_flameTex != null) return _flameTex;
            int dim = 128;
            _flameTex = new Texture2D(dim, dim, TextureFormat.RGBA32, false);
            _flameTex.filterMode = FilterMode.Bilinear;
            _flameTex.wrapMode = TextureWrapMode.Clamp;
            Vector2 center = new Vector2(dim * 0.5f, dim * 0.5f);
            float maxR = dim * 0.48f;

            for (int y = 0; y < dim; y++)
            {
                for (int x = 0; x < dim; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / maxR;
                    if (dist >= 1.0f)
                    {
                        _flameTex.SetPixel(x, y, new Color(1f, 1f, 1f, 0f));
                        continue;
                    }

                    // Gaussian radial falloff: smooth exponential decay
                    float g = Mathf.Exp(-3.8f * dist * dist);
                    // Fade strictly to 0 at perimeter
                    float perimeterFade = Mathf.Clamp01((1.0f - dist) * 3.0f);
                    float alpha = g * perimeterFade;

                    _flameTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            _flameTex.Apply(false);
            return _flameTex;
        }

        private static Texture2D GetOrCreateSmokeTexture()
        {
            if (_smokeTex != null) return _smokeTex;
            int dim = 128;
            _smokeTex = new Texture2D(dim, dim, TextureFormat.RGBA32, false);
            _smokeTex.filterMode = FilterMode.Bilinear;
            _smokeTex.wrapMode = TextureWrapMode.Clamp;
            Vector2 center = new Vector2(dim * 0.5f, dim * 0.5f);
            float maxR = dim * 0.48f;

            for (int y = 0; y < dim; y++)
            {
                for (int x = 0; x < dim; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / maxR;
                    if (dist >= 1.0f)
                    {
                        _smokeTex.SetPixel(x, y, new Color(1f, 1f, 1f, 0f));
                        continue;
                    }

                    // Soft pillowy smoke Gaussian
                    float g = Mathf.Exp(-2.5f * dist * dist);
                    float perimeterFade = Mathf.Clamp01((1.0f - dist) * 2.5f);
                    float alpha = g * perimeterFade;

                    _smokeTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            _smokeTex.Apply(false);
            return _smokeTex;
        }

        private Material CreateAdditiveParticleMat()
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                     ?? Shader.Find("Particles/Additive")
                     ?? Shader.Find("Mobile/Particles/Additive")
                     ?? Shader.Find("Standard");
            var mat = new Material(sh);
            var tex = GetOrCreateFlameTexture();
            mat.mainTexture = tex;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // Transparent
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 2f); // Additive
            if (mat.HasProperty("_BlendMode")) mat.SetFloat("_BlendMode", 2f);
            if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_BLENDMODE_ADD");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.color = Color.white;
            return mat;
        }

        private Material CreateAlphaBlendedParticleMat()
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                     ?? Shader.Find("Particles/Alpha Blended")
                     ?? Shader.Find("Mobile/Particles/Alpha Blended")
                     ?? Shader.Find("Standard");
            var mat = new Material(sh);
            var tex = GetOrCreateSmokeTexture();
            mat.mainTexture = tex;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // Transparent
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f); // Alpha
            if (mat.HasProperty("_BlendMode")) mat.SetFloat("_BlendMode", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_BLENDMODE_ALPHA");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.color = new Color(0.12f, 0.11f, 0.10f, 0.65f);
            return mat;
        }

        // ── Procedural Crackle Audio ──────────────────────────────────────

        private AudioClip GenerateCrackleClip()
        {
            int sampleRate  = 22050;
            int durationSec = 3;
            int samples     = sampleRate * durationSec;
            float[] data    = new float[samples];

            System.Random rng = new System.Random(42);
            float prev = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float rumble  = Mathf.Sin(t * 80f * Mathf.PI * 2f) * 0.06f;
                float crackle = ((float)rng.NextDouble() * 2f - 1f) * 0.18f;
                float spike   = 0f;
                if (rng.NextDouble() < 0.003)
                    spike = ((float)rng.NextDouble() * 2f - 1f) * 0.7f;
                float smoothed = prev * 0.4f + crackle * 0.6f;
                prev = smoothed;
                data[i] = Mathf.Clamp(rumble + smoothed + spike, -1f, 1f);
            }

            var clip = AudioClip.Create("CrackleLoop", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>Set by MineEnvironmentBuilder to position the fire in the corridor.</summary>
        public void SetFireAnchor(Transform anchor)
        {
            fireAnchor = anchor;
            if (fireRoot != null)
            {
                fireRoot.transform.position = anchor.position;
                fireRoot.transform.rotation = anchor.rotation;
            }
        }

        /// <summary>Returns the world-space fire position (for alarm placement).</summary>
        public Vector3 GetFireWorldPosition()
        {
            return fireRoot != null ? fireRoot.transform.position : transform.position;
        }

        /// <summary>Force trigger for testing.</summary>
        public void ForceTrigger()
        {
            if (CurrentPhase == FirePhase.Dormant)
                TriggerFireIncident();
        }

        // ── Phase 2B-2: Fire Response Outcomes ───────────────────────────

        /// <summary>
        /// CORRECT RESPONSE — progressively extinguishes the fire over extinguishDuration seconds.
        /// </summary>
        public void ExtinguishFire(float extinguishDuration = 3.0f)
        {
            if (CurrentPhase == FirePhase.Dormant) return;
            StartCoroutine(ExtinguishCoroutine(extinguishDuration));
        }

        private IEnumerator ExtinguishCoroutine(float duration)
        {
            Debug.Log("[FireHazard] EXTINGUISHING fire...");
            float elapsed = 0f;
            float startScale = fireRoot != null ? fireRoot.transform.localScale.x : 1.5f;
            float startIntensity = fireLight != null ? fireLight.intensity : 4.5f;

            // Progressively reduce emission, scale, and light
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = t * t; // ease-in: slow start, fast finish

                // Shrink fire
                float scale = Mathf.Lerp(startScale, 0.05f, easedT);
                if (fireRoot != null)
                    fireRoot.transform.localScale = new Vector3(scale, scale, scale);

                // Dim fire light
                if (fireLight != null)
                {
                    fireLight.intensity = Mathf.Lerp(startIntensity, 0f, easedT);
                    fireLight.range     = Mathf.Lerp(6f, 0f, easedT);
                }

                // Reduce audio
                if (crackleSource != null)
                    crackleSource.volume = Mathf.Lerp(0.65f, 0f, easedT);

                // Reduce particle emission rates progressively
                float rateMultiplier = 1f - easedT;
                SetParticleEmissionMultiplier(coreFlamePS,      55f * rateMultiplier);
                SetParticleEmissionMultiplier(turbulentFlamePS, 32f * rateMultiplier);
                SetParticleEmissionMultiplier(secondaryFlamePS, 28f * rateMultiplier);
                SetParticleEmissionMultiplier(smokePS,          22f * rateMultiplier);
                SetParticleEmissionMultiplier(embersPS,         14f * rateMultiplier);
                SetParticleEmissionMultiplier(arcSparkPS,       0f);  // sparks die immediately

                yield return null;
            }

            // Fully stop all fire effects
            SetFireActive(false);
            if (fireLight != null) fireLight.intensity = 0f;
            if (crackleSource != null) { crackleSource.Stop(); }

            CurrentPhase = FirePhase.Dormant;
            Debug.Log("[FireHazard] Fire EXTINGUISHED.");
            OnFireExtinguished?.Invoke();
        }

        private void SetParticleEmissionMultiplier(ParticleSystem ps, float rate)
        {
            if (ps == null) return;
            var emission = ps.emission;
            emission.rateOverTime = rate;
        }

        /// <summary>
        /// WRONG RESPONSE — momentarily flares the fire up (intensify then return to normal).
        /// </summary>
        public void FireFlareUp(float flareDuration = 2.5f)
        {
            if (CurrentPhase == FirePhase.Dormant) return;
            StartCoroutine(FlareUpCoroutine(flareDuration));
        }

        private IEnumerator FlareUpCoroutine(float duration)
        {
            Debug.Log("[FireHazard] Fire FLARE UP — wrong response applied.");
            float halfDuration = duration * 0.4f;
            float returnDuration = duration * 0.6f;

            float normalScale = fireRoot != null ? fireRoot.transform.localScale.x : 1.5f;
            float flareScale  = normalScale * 2.2f;
            float normalIntensity = fireLight != null ? fireLight.intensity : 5.8f;
            float flareIntensity  = normalIntensity * 2.2f;

            // Ramp emission up sharply
            SetParticleEmissionMultiplier(coreFlamePS,      125f);
            SetParticleEmissionMultiplier(turbulentFlamePS,  75f);
            SetParticleEmissionMultiplier(secondaryFlamePS,  65f);
            SetParticleEmissionMultiplier(smokePS,           55f);
            SetParticleEmissionMultiplier(embersPS,          35f);
            SetParticleEmissionMultiplier(arcSparkPS,        22f);

            // Expand to peak
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                float s = Mathf.Lerp(normalScale, flareScale, t);
                if (fireRoot != null)
                    fireRoot.transform.localScale = new Vector3(s, s, s);
                if (fireLight != null)
                    fireLight.intensity = Mathf.Lerp(normalIntensity, flareIntensity, t);
                if (crackleSource != null)
                    crackleSource.volume = Mathf.Lerp(0.65f, 1.0f, t);
                yield return null;
            }

            // Return to normal
            elapsed = 0f;
            while (elapsed < returnDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / returnDuration);
                float s = Mathf.Lerp(flareScale, normalScale, t);
                if (fireRoot != null)
                    fireRoot.transform.localScale = new Vector3(s, s, s);
                if (fireLight != null)
                    fireLight.intensity = Mathf.Lerp(flareIntensity, normalIntensity, t);
                if (crackleSource != null)
                    crackleSource.volume = Mathf.Lerp(1.0f, 0.65f, t);
                yield return null;
            }

            // Restore emission rates
            SetParticleEmissionMultiplier(coreFlamePS,       55f);
            SetParticleEmissionMultiplier(turbulentFlamePS,  32f);
            SetParticleEmissionMultiplier(secondaryFlamePS,  28f);
            SetParticleEmissionMultiplier(smokePS,           22f);
            SetParticleEmissionMultiplier(embersPS,          14f);
            SetParticleEmissionMultiplier(arcSparkPS,         8f);

            Debug.Log("[FireHazard] Flare-up settled. Fire returned to burning state.");
            OnFireFlaredUp?.Invoke();
        }
    }
}
