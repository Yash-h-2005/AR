using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurakshaAR.AR
{
    /// <summary>
    /// PHASE 3 — Realistic 3D AR Electrical Fire Hazard with Environmental Clues.
    /// 
    /// Features:
    ///   1. 3D Industrial Electrical Control Cabinet with High-Voltage Hazard Signs and Scorched Vents.
    ///   2. Realistic Industrial Conduit Cables entering cabinet base.
    ///   3. Electric Arcing & Blue/White Spark Particle System discharging from panel seams.
    ///   4. Dual-spectrum Light Flicker (Warm Orange Fire Light + Intermittent Electric Blue Arc Flashes).
    ///   5. Multi-layer Realistic Fire & Heavy Toxic Smoke VFX.
    ///   6. 3D World-Space Floating Observation Hotspots that billboard toward camera when in proximity.
    /// </summary>
    public class ARElectricalFireHazard : MonoBehaviour
    {
        [Header("Hazard Configuration")]
        [SerializeField] private float observationRadius = 2.8f;

        // 3D Cabinet & Environment Elements
        private GameObject cabinetGO;
        private GameObject cablesGO;
        private GameObject electricalArcGO;
        private GameObject hotspotsRootGO;

        // Fire Systems
        private GameObject fireEffectRoot;
        private ARRealisticFireEffect fireEffect;

        // Electrical Arcing System
        private ParticleSystem arcPS;
        private ParticleSystem arcSparksPS;
        private Light fireLight;
        private Light electricArcLight;

        // Hotspots
        private readonly List<Transform> hotspotTransforms = new List<Transform>();
        private Canvas worldCanvas;

        // Arc timing
        private float nextArcTime = 0f;
        private float arcDuration = 0f;
        private bool isArcing = false;

        private void Awake()
        {
            BuildCompleteElectricalHazard();
        }

        private void Update()
        {
            UpdateElectricalArcing();
            UpdateObservationHotspots();
        }

        private void BuildCompleteElectricalHazard()
        {
            // ── 1. Create Industrial Electrical Cabinet ────────────────────────
            BuildIndustrialCabinet();

            // ── 2. Create Conduit Power Cables ────────────────────────────────
            BuildPowerConduitCables();

            // ── 3. Create Multi-Layer AR Fire VFX ──────────────────────────────
            fireEffectRoot = new GameObject("ARRealisticFire");
            fireEffectRoot.transform.SetParent(transform, false);
            fireEffectRoot.transform.localPosition = new Vector3(0.05f, 0.35f, 0.05f); // Coming out of open door/vents
            fireEffect = fireEffectRoot.AddComponent<ARRealisticFireEffect>();

            // ── 4. Create Electrical Arcing & Discharge FX ────────────────────
            BuildElectricalArcSystem();

            // ── 5. Create 3D Observation Hotspots ─────────────────────────────
            BuildObservationHotspots();
        }

        // ── 1. Industrial Electrical Cabinet Construction ─────────────────────
        private void BuildIndustrialCabinet()
        {
            cabinetGO = new GameObject("IndustrialElectricalCabinet");
            cabinetGO.transform.SetParent(transform, false);
            cabinetGO.transform.localPosition = Vector3.zero;

            // Main Steel Housing (Box)
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Cabinet_Body";
            box.transform.SetParent(cabinetGO.transform, false);
            box.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            box.transform.localScale = new Vector3(0.55f, 0.90f, 0.35f);

            // Metallic dark industrial material with procedural warning label
            Material cabinetMat = CreateIndustrialCabinetMaterial();
            box.GetComponent<Renderer>().material = cabinetMat;

            // Cabinet Door (Ajar / Burst open from internal pressure)
            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "Cabinet_Door_Ajar";
            door.transform.SetParent(cabinetGO.transform, false);
            door.transform.localPosition = new Vector3(0.12f, 0.45f, 0.20f);
            door.transform.localRotation = Quaternion.Euler(0f, 32f, 0f); // Slightly open
            door.transform.localScale = new Vector3(0.28f, 0.86f, 0.025f);
            door.GetComponent<Renderer>().material = cabinetMat;

            // Danger High Voltage Hazard Sign on Door
            GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sign.name = "HighVoltage_Warning_Sign";
            sign.transform.SetParent(door.transform, false);
            sign.transform.localPosition = new Vector3(0f, 0.18f, -0.6f);
            sign.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            sign.transform.localScale = new Vector3(0.7f, 0.45f, 1f);
            sign.GetComponent<Renderer>().material = CreateDangerSignMaterial();

            // Cabinet Mounting Stand / Legs
            for (int i = -1; i <= 1; i += 2)
            {
                for (int j = -1; j <= 1; j += 2)
                {
                    GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    leg.name = $"StandLeg_{i}_{j}";
                    leg.transform.SetParent(cabinetGO.transform, false);
                    leg.transform.localPosition = new Vector3(i * 0.22f, 0.05f, j * 0.12f);
                    leg.transform.localScale = new Vector3(0.04f, 0.05f, 0.04f);
                    leg.GetComponent<Renderer>().material = CreateSteelLegMaterial();
                }
            }
        }

        // ── 2. Heavy Power Conduit Cables ─────────────────────────────────────
        private void BuildPowerConduitCables()
        {
            cablesGO = new GameObject("PowerConduitCables");
            cablesGO.transform.SetParent(transform, false);
            cablesGO.transform.localPosition = Vector3.zero;

            Material cableMat = CreateRubberCableMaterial();

            // Multiple heavy cables entering bottom of cabinet and spreading along floor
            for (int k = 0; k < 3; k++)
            {
                GameObject cable = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cable.name = $"HeavyCable_{k}";
                cable.transform.SetParent(cablesGO.transform, false);
                float xOff = (k - 1) * 0.14f;
                cable.transform.localPosition = new Vector3(xOff, 0.03f, -0.25f - (k * 0.05f));
                cable.transform.localRotation = Quaternion.Euler(85f, (k - 1) * 18f, 0f);
                cable.transform.localScale = new Vector3(0.045f, 0.35f, 0.045f);
                cable.GetComponent<Renderer>().material = cableMat;
            }
        }

        // ── 3. Electric Arcing & Discharge Particle System ────────────────────
        private void BuildElectricalArcSystem()
        {
            electricalArcGO = new GameObject("ElectricalArcDischarge");
            electricalArcGO.transform.SetParent(transform, false);
            electricalArcGO.transform.localPosition = new Vector3(0.05f, 0.45f, 0.15f);

            Material arcMat = CreateElectricArcMaterial();

            // Arc Sparks
            arcPS = electricalArcGO.AddComponent<ParticleSystem>();
            var arcRenderer = electricalArcGO.GetComponent<ParticleSystemRenderer>();
            arcRenderer.material = arcMat;
            arcRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            arcRenderer.lengthScale = 3.5f;

            var arcMain = arcPS.main;
            arcMain.loop = true;
            arcMain.startLifetime = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            arcMain.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4.0f);
            arcMain.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
            arcMain.startColor = new Color(0.2f, 0.8f, 1f, 1f); // Electric Cyan
            arcMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            arcMain.maxParticles = 50;

            var arcEmission = arcPS.emission;
            arcEmission.rateOverTime = 0f;
            // Bursts of electric discharge
            arcEmission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0.0f, 8, 16, 1, 0.3f)
            });

            var arcShape = arcPS.shape;
            arcShape.shapeType = ParticleSystemShapeType.Sphere;
            arcShape.radius = 0.12f;

            // Electric Discharge Point Light (Bright Cyan flash)
            GameObject arcLightGO = new GameObject("ElectricArcLight");
            arcLightGO.transform.SetParent(electricalArcGO.transform, false);
            arcLightGO.transform.localPosition = Vector3.zero;

            electricArcLight = arcLightGO.AddComponent<Light>();
            electricArcLight.type = LightType.Point;
            electricArcLight.color = new Color(0.3f, 0.85f, 1f);
            electricArcLight.intensity = 0f;
            electricArcLight.range = 3.0f;
            electricArcLight.shadows = LightShadows.None;
        }

        private void UpdateElectricalArcing()
        {
            if (Time.time >= nextArcTime)
            {
                // Trigger an electric arc burst
                isArcing = true;
                arcDuration = Random.Range(0.15f, 0.45f);
                nextArcTime = Time.time + Random.Range(0.8f, 2.2f);

                if (arcPS != null)
                    arcPS.Emit(Random.Range(10, 25));
            }

            if (isArcing)
            {
                arcDuration -= Time.deltaTime;
                if (arcDuration <= 0f)
                {
                    isArcing = false;
                    if (electricArcLight != null) electricArcLight.intensity = 0f;
                }
                else
                {
                    // Flash electric light intensely
                    if (electricArcLight != null)
                    {
                        electricArcLight.intensity = Random.Range(2.5f, 6.0f);
                    }
                }
            }
        }

        // ── 4. 3D World-Space Observation Clue Hotspots ───────────────────────
        private void BuildObservationHotspots()
        {
            hotspotsRootGO = new GameObject("ObservationHotspots_WorldSpace");
            hotspotsRootGO.transform.SetParent(transform, false);
            hotspotsRootGO.transform.localPosition = Vector3.zero;

            // Clue 1: ⚡ 440V Control Box
            CreateHotspotPin(
                "Pin_ControlBox",
                new Vector3(-0.35f, 0.75f, 0f),
                "⚡ 440V Control Box",
                new Color(1f, 0.8f, 0.1f)
            );

            // Clue 2: ⚡ Electric Arcing
            CreateHotspotPin(
                "Pin_ElectricArcing",
                new Vector3(0.35f, 0.55f, 0.2f),
                "⚡ Electric Arcing Discharge",
                new Color(0.2f, 0.9f, 1f)
            );

            // Clue 3: 🔌 Scorched Power Conduits
            CreateHotspotPin(
                "Pin_PowerCables",
                new Vector3(0f, 0.15f, -0.4f),
                "🔌 Scorched Power Conduits",
                new Color(1f, 0.4f, 0.1f)
            );
        }

        private void CreateHotspotPin(string name, Vector3 localPos, string labelText, Color accentColor)
        {
            GameObject pinGO = new GameObject(name);
            pinGO.transform.SetParent(hotspotsRootGO.transform, false);
            pinGO.transform.localPosition = localPos;

            // Floating Marker Quad
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "MarkerQuad";
            quad.transform.SetParent(pinGO.transform, false);
            quad.transform.localPosition = Vector3.zero;
            quad.transform.localScale = new Vector3(0.55f, 0.18f, 1f);

            Material tagMat = CreateHotspotTagMaterial(labelText, accentColor);
            quad.GetComponent<Renderer>().material = tagMat;

            // Add to tracked list for billboarding
            hotspotTransforms.Add(pinGO.transform);
        }

        private void UpdateObservationHotspots()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            float dist = Vector3.Distance(cam.transform.position, transform.position);
            bool inObservationRange = dist <= observationRadius;

            if (hotspotsRootGO != null && hotspotsRootGO.activeSelf != inObservationRange)
            {
                hotspotsRootGO.SetActive(inObservationRange);
            }

            if (inObservationRange)
            {
                // Billboard pins smoothly toward user camera
                foreach (var pin in hotspotTransforms)
                {
                    if (pin != null)
                    {
                        pin.LookAt(pin.position + cam.transform.rotation * Vector3.forward, cam.transform.rotation * Vector3.up);
                    }
                }
            }
        }

        // ── Procedural Materials & Textures (Zero Missing Pink Shaders) ────────

        private static Material CreateIndustrialCabinetMaterial()
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Mobile/Diffuse");
            if (shader == null) shader = Shader.Find("Unlit/Texture");
            if (shader == null) shader = Shader.Find("Diffuse");

            Material mat = new Material(shader);
            mat.color = new Color(0.25f, 0.28f, 0.32f); // Industrial Charcoal Steel
            mat.mainTexture = GenerateCabinetTexture();
            return mat;
        }

        private static Material CreateDangerSignMaterial()
        {
            Shader shader = Shader.Find("Unlit/Texture");
            if (shader == null) shader = Shader.Find("Mobile/Unlit");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = new Material(shader);
            mat.mainTexture = GenerateDangerSignTexture();
            return mat;
        }

        private static Material CreateRubberCableMaterial()
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Mobile/Diffuse");
            if (shader == null) shader = Shader.Find("Diffuse");

            Material mat = new Material(shader);
            mat.color = new Color(0.12f, 0.12f, 0.14f); // Matte Black Rubber
            return mat;
        }

        private static Material CreateSteelLegMaterial()
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Mobile/Diffuse");
            if (shader == null) shader = Shader.Find("Diffuse");

            Material mat = new Material(shader);
            mat.color = new Color(0.45f, 0.45f, 0.48f);
            return mat;
        }

        private static Material CreateElectricArcMaterial()
        {
            Shader shader = Shader.Find("Mobile/Particles/Additive");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            Material mat = new Material(shader);
            mat.color = new Color(0.3f, 0.9f, 1.0f, 1.0f);
            return mat;
        }

        private static Material CreateHotspotTagMaterial(string text, Color accent)
        {
            Shader shader = Shader.Find("Unlit/Transparent");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            Material mat = new Material(shader);
            mat.mainTexture = GenerateHotspotTexture(text, accent);
            return mat;
        }

        private static Texture2D GenerateCabinetTexture()
        {
            int w = 256, h = 256;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color baseCol = new Color(0.28f, 0.30f, 0.34f);
            Color ventCol = new Color(0.12f, 0.12f, 0.14f);
            Color sootCol = new Color(0.10f, 0.08f, 0.06f);

            Color[] cols = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = baseCol;

                    // Ventilation slots pattern
                    if (y > 170 && y < 230 && (y % 8 < 4) && (x > 30 && x < 226))
                    {
                        c = ventCol;
                    }

                    // Burn soot gradient around right/top
                    if (x > 140 || y > 150)
                    {
                        float sootAmount = Mathf.Clamp01(((x - 140f) / 116f) * 0.4f + ((y - 150f) / 106f) * 0.4f);
                        c = Color.Lerp(c, sootCol, sootAmount);
                    }

                    cols[y * w + x] = c;
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            return tex;
        }

        private static Texture2D GenerateDangerSignTexture()
        {
            int w = 256, h = 128;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color yellow = new Color(1.0f, 0.85f, 0.0f);
            Color black = new Color(0.08f, 0.08f, 0.08f);
            Color red = new Color(0.85f, 0.12f, 0.12f);

            Color[] cols = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Border
                    if (x < 6 || x > w - 7 || y < 6 || y > h - 7)
                    {
                        cols[y * w + x] = black;
                        continue;
                    }

                    // Header bar
                    if (y > h - 38)
                    {
                        cols[y * w + x] = red;
                    }
                    else
                    {
                        // Yellow background with lightning bolt symbol motif
                        cols[y * w + x] = yellow;

                        // Lightning bolt silhouette in center
                        int cx = w / 2;
                        int cy = (h - 38) / 2;
                        if (Mathf.Abs(x - cx + (y - cy) * 0.4f) < 8 && y > 18 && y < h - 48)
                        {
                            cols[y * w + x] = black;
                        }
                    }
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            return tex;
        }

        private static Texture2D GenerateHotspotTexture(string label, Color accent)
        {
            int w = 256, h = 96;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color bg = new Color(0.06f, 0.09f, 0.16f, 0.88f);
            Color border = accent;

            Color[] cols = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Rounded border
                    if (x < 3 || x > w - 4 || y < 3 || y > h - 4)
                    {
                        cols[y * w + x] = border;
                    }
                    else if (x < 12)
                    {
                        cols[y * w + x] = accent; // Left accent strip
                    }
                    else
                    {
                        cols[y * w + x] = bg;
                    }
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            return tex;
        }
    }
}
