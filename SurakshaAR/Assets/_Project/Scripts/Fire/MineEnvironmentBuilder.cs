using UnityEngine;
using System.Collections.Generic;

namespace SurakshaAR.Training.Fire
{
    /// <summary>
    /// Procedurally builds the Phase 1 test mine corridor at runtime.
    /// No external art assets required — all geometry is generated from Unity primitives
    /// with procedural textures to simulate a realistic underground coal mine environment.
    ///
    /// Layout:
    ///   Starting Chamber (4m × 3m × 2.5m)
    ///   → Tunnel A (14m × 2.6m × 2.4m)
    ///   → End Chamber with orange marker lamp
    /// </summary>
    public class MineEnvironmentBuilder : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Environment Root")]
        [Tooltip("Parent transform under which all mine geometry is placed.")]
        [SerializeField] private Transform mineRoot;

        [Header("Lighting")]
        [SerializeField] private Color ambientLight = new Color(0.04f, 0.04f, 0.06f, 1f);
        [SerializeField] private Color workLightColor = new Color(1.0f, 0.85f, 0.55f, 1f);
        [SerializeField] private Color markerLampColor = new Color(1.0f, 0.5f, 0.1f, 1f);

        [Header("Assessment Configuration")]
        [Tooltip("When true, Route B (Right) is the emergency exit and Route A (Left) is haulage drift.")]
        [SerializeField] private bool isRouteBEmergencyExit = false;

        public bool IsRouteBEmergencyExit
        {
            get => isRouteBEmergencyExit;
            set => isRouteBEmergencyExit = value;
        }

        // ── Materials (generated) ─────────────────────────────────────────────
        private Material rockMat;
        private Material floorMat;
        private Material woodMat;
        private Material metalMat;
        private Material markerLampMat;
        private Material railMat;
        private Material pipeMat;
        private Material emergencyExitMat;
        private Material cautionMat;
        private Material signBackMat;
        private Material outdoorFloorMat;
        private Material skyMat;
        private Material concreteMat;
        private Material vestOrangeMat;
        private Material vestYellowMat;
        private Material hardhatYellowMat;
        private Material hardhatWhiteMat;
        private Material silverReflectiveMat;
        private Material darkTrouserMat;
        private Material skinMat;
        private Material hazardYellowMat;
        private Material hazardBlackMat;

        // ── Outdoor / Portal Materials (new) ──────────────────────────────────
        private Material outdoorRockMat;
        private Material outdoorDirtMat;
        private Material outdoorGravelMat;
        private Material minePortalRockMat;
        private Material skyDomeMat;
        private Material foliageMat;

        // ── Dynamic Lighting State ────────────────────────────────────────────
        private static readonly Color OutdoorAmbient      = new Color(0.58f, 0.55f, 0.48f);  // bright daylight
        private static readonly Color UndergroundAmbient  = new Color(0.02f, 0.02f, 0.03f);  // very dark underground
        private Light _dirSunLight;
        private MineMovementController _movCtrl;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (mineRoot == null) mineRoot = transform;
            BuildMaterials();
            BuildEnvironment();
            ConfigureLighting();
        }

        // ── Material Creation ─────────────────────────────────────────────────

        private void BuildMaterials()
        {
            rockMat             = CreateLitMaterial(MakeRockTexture(256, 256),  new Color(0.22f, 0.20f, 0.18f));
            floorMat            = CreateLitMaterial(MakeFloorTexture(256, 256), new Color(0.18f, 0.16f, 0.14f));
            woodMat             = CreateLitMaterial(MakeWoodTexture(128, 256),  new Color(0.45f, 0.30f, 0.15f));
            metalMat            = CreateLitMaterial(MakeMetalTexture(128, 128), new Color(0.30f, 0.30f, 0.32f));
            railMat             = CreateLitMaterial(MakeMetalTexture(64, 64),   new Color(0.25f, 0.25f, 0.28f));
            pipeMat             = CreateLitMaterial(MakeMetalTexture(64, 64),   new Color(0.40f, 0.38f, 0.35f));
            markerLampMat       = CreateEmissiveMaterial(markerLampColor);
            emergencyExitMat    = CreateEmissiveMaterial(new Color(0.12f, 0.90f, 0.35f));
            cautionMat          = CreateEmissiveMaterial(new Color(0.95f, 0.65f, 0.12f));
            signBackMat         = CreateLitMaterial(MakeMetalTexture(64, 64),   new Color(0.12f, 0.12f, 0.14f));
            outdoorFloorMat     = CreateLitMaterial(MakeFloorTexture(256, 256), new Color(0.40f, 0.38f, 0.34f));
            skyMat              = CreateLitMaterial(MakeMetalTexture(64, 64),   new Color(0.35f, 0.62f, 0.92f));
            skyMat.EnableKeyword("_EMISSION");
            skyMat.SetColor("_EmissionColor", new Color(0.20f, 0.40f, 0.65f));
            concreteMat         = CreateLitMaterial(MakeMetalTexture(64, 64),   new Color(0.45f, 0.46f, 0.45f));
            vestOrangeMat       = CreateLitMaterial(MakeMetalTexture(64, 64),   new Color(1.0f, 0.40f, 0.05f));
            vestYellowMat       = CreateLitMaterial(MakeMetalTexture(64, 64),   new Color(0.85f, 0.95f, 0.10f));
            hardhatYellowMat    = CreateLitMaterial(MakeMetalTexture(64, 64),   new Color(0.98f, 0.85f, 0.05f));
            hardhatWhiteMat     = CreateLitMaterial(MakeMetalTexture(64, 64),   new Color(0.95f, 0.95f, 0.95f));
            silverReflectiveMat = CreateEmissiveMaterial(new Color(0.85f, 0.88f, 0.95f));
            darkTrouserMat      = CreateLitMaterial(MakeMetalTexture(64, 64),   new Color(0.12f, 0.15f, 0.22f));
            skinMat             = CreateLitMaterial(MakeMetalTexture(64, 64),   new Color(0.85f, 0.65f, 0.52f));
            hazardYellowMat     = CreateEmissiveMaterial(new Color(0.95f, 0.85f, 0.05f));
            hazardBlackMat      = CreateLitMaterial(MakeMetalTexture(64, 64),   new Color(0.10f, 0.10f, 0.10f));

            // Outdoor materials
            outdoorRockMat    = CreateLitMaterial(MakeOutdoorRockTexture(256, 256), new Color(0.48f, 0.42f, 0.34f));
            outdoorDirtMat    = CreateLitMaterial(MakeFloorTexture(256, 256),       new Color(0.42f, 0.36f, 0.28f));
            outdoorGravelMat  = CreateLitMaterial(MakeFloorTexture(128, 128),       new Color(0.38f, 0.35f, 0.30f));
            minePortalRockMat = CreateLitMaterial(MakeRockTexture(256, 256),        new Color(0.18f, 0.16f, 0.14f));
            foliageMat        = CreateLitMaterial(MakeRockTexture(128, 128),        new Color(0.20f, 0.30f, 0.14f));
            // Sky dome must be rendered from INSIDE — use Cull Off so back-faces are visible
            skyDomeMat = new Material(Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse"));
            skyDomeMat.color = new Color(0.38f, 0.58f, 0.85f);
            skyDomeMat.EnableKeyword("_EMISSION");
            skyDomeMat.SetColor("_EmissionColor", new Color(0.28f, 0.50f, 0.82f) * 2.2f);
            skyDomeMat.SetFloat("_Glossiness", 0f);
            // Cull Off = 0 → renders both faces so inside of sphere is visible
            skyDomeMat.SetInt("_Cull", 0);
            // Disable shadows — sky is background only
            skyDomeMat.SetInt("_ZWrite", 0);
        }

        private Material CreateLitMaterial(Texture2D tex, Color tint)
        {
            var mat = new Material(Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse"));
            mat.mainTexture = tex;
            mat.color = tint;
            mat.SetFloat("_Glossiness", 0.05f);
            mat.SetFloat("_Metallic", 0.0f);
            return mat;
        }

        private Material CreateEmissiveMaterial(Color emissive)
        {
            var mat = new Material(Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse"));
            mat.color = emissive;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissive * 3f);
            return mat;
        }

        // ── Texture Generation ────────────────────────────────────────────────

        private Texture2D MakeRockTexture(int w, int h)
        {
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++)
            {
                int x = i % w, y = i / w;
                float n  = Mathf.PerlinNoise(x * 0.05f, y * 0.05f);
                float n2 = Mathf.PerlinNoise(x * 0.12f + 100f, y * 0.12f + 100f);
                float f  = n * 0.6f + n2 * 0.4f;
                float v  = Mathf.Lerp(0.12f, 0.32f, f);
                pixels[i] = new Color(v * 0.95f, v * 0.92f, v * 0.88f);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Repeat;
            return tex;
        }

        private Texture2D MakeFloorTexture(int w, int h)
        {
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++)
            {
                int x = i % w, y = i / w;
                float n = Mathf.PerlinNoise(x * 0.08f, y * 0.08f);
                float f = n * 0.5f + 0.2f;
                // Dark coal/dirt floor
                pixels[i] = new Color(f * 0.18f, f * 0.16f, f * 0.13f);
                // Add random gravel spots
                if (Mathf.PerlinNoise(x * 0.3f + 50, y * 0.3f + 50) > 0.72f)
                    pixels[i] *= 0.7f;
            }
            tex.SetPixels(pixels);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Repeat;
            return tex;
        }

        private Texture2D MakeWoodTexture(int w, int h)
        {
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++)
            {
                int x = i % w, y = i / w;
                float grain = Mathf.PerlinNoise(x * 0.02f, y * 0.4f);
                float knot  = Mathf.PerlinNoise(x * 0.15f + 20, y * 0.03f + 20);
                float f = grain * 0.7f + knot * 0.3f;
                pixels[i] = new Color(
                    Mathf.Lerp(0.28f, 0.50f, f),
                    Mathf.Lerp(0.18f, 0.32f, f),
                    Mathf.Lerp(0.08f, 0.14f, f)
                );
            }
            tex.SetPixels(pixels);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Repeat;
            return tex;
        }

        private Texture2D MakeMetalTexture(int w, int h)
        {
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++)
            {
                int x = i % w, y = i / w;
                float scratch = Mathf.PerlinNoise(x * 0.4f, y * 0.1f);
                float rust    = Mathf.PerlinNoise(x * 0.1f + 80, y * 0.1f + 80) * 0.3f;
                float v = 0.28f + scratch * 0.12f + rust;
                pixels[i] = new Color(v * 1.0f, v * 0.95f, v * 0.88f);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Repeat;
            return tex;
        }

        // ── Environment Construction ──────────────────────────────────────────

        private void BuildEnvironment()
        {
            // ── Outdoor Surface + Mine Portal (Z = -15m to 0m) ───────────────
            // Player starts outside at Z=-14m and walks into the mine entrance
            BuildOutdoorSurface();
            BuildMinePortal();

            // ── Starting Chamber ─────────────────────────────────────────────
            // Irregular rocky cave chamber at mine entrance
            BuildRockyChamber(Vector3.zero, new Vector3(4f, 2.5f, 4f), "StartChamber");

            // Dim entry lamp — tunnel is dark, single work light near entrance
            CreatePointLight(new Vector3(0, 2.1f, 1.5f), workLightColor, 5f, 5f, "StartLamp");

            // ── Main Tunnel A — realistic rock-walled cave ───────────────────
            // Runs from Z=2 to Z=29
            float tunnelLength = 27f;
            float tunnelZ = 2f + tunnelLength * 0.5f;   // centre Z = 15.5
            BuildRockyTunnel(
                new Vector3(0, 0, tunnelZ),
                new Vector3(2.6f, 2.4f, tunnelLength),
                "TunnelA"
            );

            // Timber pit props every ~4m along tunnel
            for (float z = 4f; z < 29f; z += 4f)
            {
                BuildPitProp(new Vector3(0, 0, z), 2.4f, 2.6f);
            }

            // Floor rails along the entire extended path
            BuildRails(2f, 29f, 0.8f);

            // Wall pipes on left side
            BuildWallPipes(-1.25f, 2f, 29f);

            // Overhead cable tray on right side
            BuildCableTray(1.1f, 2f, 29f);

            // Sparse, localized work lamps — dark between them (realistic mine)
            // Light intensity 6, range 5.5 — creates pools of light, dark sections between
            CreatePointLight(new Vector3(0, 2.1f,  7f), workLightColor, 6f, 5.5f, "TunnelLamp1");
            CreatePointLight(new Vector3(0, 2.1f, 15f), workLightColor, 6f, 5.5f, "TunnelLamp2");
            CreatePointLight(new Vector3(0, 2.1f, 23f), workLightColor, 6f, 5.5f, "TunnelLamp3");
            CreatePointLight(new Vector3(0, 2.1f, 28f), workLightColor, 5f, 4.5f, "TunnelLamp4");

            // Subtle mine dust particles
            BuildDustParticles(new Vector3(0, 1.2f, 16f));

            // ── Phase 2C Y-Junction (Z = 29m to 48m) ──────────────────────────
            BuildYJunction(29f);

            // ── Underground Enhancement ───────────────────────────────────────
            BuildUndergroundEnhancement();
        }

        // ── Structural Builders ───────────────────────────────────────────────

        /// <summary>
        /// Builds a rocky cave chamber with irregular walls, ceiling protrusions and uneven back wall.
        /// Replaces the old flat-panel BuildChamber.
        /// </summary>
        private void BuildRockyChamber(Vector3 centre, Vector3 size, string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(mineRoot, false);
            root.transform.localPosition = centre;

            float hw = size.x * 0.5f;
            float hh = size.y;
            float hd = size.z * 0.5f;

            // Floor — slightly uneven
            CreatePanel(root.transform, $"{name}_Floor",
                new Vector3(0, 0, 0), new Vector3(size.x, 0.05f, size.z),
                floorMat, new Vector2(2f, 2f));

            // --- LEFT WALL: multiple overlapping rock slabs for cave appearance ---
            // Primary wall panel (slightly inset)
            CreatePanel(root.transform, $"{name}_WallL_Base",
                new Vector3(-hw - 0.04f, hh * 0.5f, 0),
                new Vector3(0.18f, hh, size.z),
                rockMat, new Vector2(1.5f, 1.5f));
            // Protruding rock slab mid-left
            CreatePanel(root.transform, $"{name}_WallL_Slab1",
                new Vector3(-hw + 0.12f, hh * 0.35f, -0.3f),
                new Vector3(0.35f, hh * 0.55f, size.z * 0.55f),
                rockMat, new Vector2(0.8f, 0.8f),
                Quaternion.Euler(-3f, 6f, -2f));
            // Upper overhang left
            CreatePanel(root.transform, $"{name}_WallL_Upper",
                new Vector3(-hw + 0.08f, hh * 0.82f, 0.2f),
                new Vector3(0.28f, hh * 0.30f, size.z * 0.7f),
                rockMat, new Vector2(0.6f, 0.6f),
                Quaternion.Euler(5f, -4f, 3f));

            // --- RIGHT WALL: similar irregular treatment ---
            CreatePanel(root.transform, $"{name}_WallR_Base",
                new Vector3(+hw + 0.04f, hh * 0.5f, 0),
                new Vector3(0.18f, hh, size.z),
                rockMat, new Vector2(1.5f, 1.5f));
            CreatePanel(root.transform, $"{name}_WallR_Slab1",
                new Vector3(+hw - 0.10f, hh * 0.4f, 0.2f),
                new Vector3(0.32f, hh * 0.60f, size.z * 0.60f),
                rockMat, new Vector2(0.8f, 0.8f),
                Quaternion.Euler(4f, -7f, 2f));
            CreatePanel(root.transform, $"{name}_WallR_Upper",
                new Vector3(+hw - 0.06f, hh * 0.78f, -0.15f),
                new Vector3(0.25f, hh * 0.35f, size.z * 0.65f),
                rockMat, new Vector2(0.6f, 0.6f),
                Quaternion.Euler(-4f, 5f, -3f));

            // --- CEILING: irregular, with hanging protrusions ---
            CreatePanel(root.transform, $"{name}_Ceiling_Base",
                new Vector3(0, hh + 0.04f, 0),
                new Vector3(size.x, 0.12f, size.z),
                rockMat, new Vector2(2f, 2f));
            // Ceiling rib / boss 1
            CreatePanel(root.transform, $"{name}_CeilProtrusion1",
                new Vector3(-0.4f, hh - 0.10f, -0.4f),
                new Vector3(0.60f, 0.22f, 0.50f),
                rockMat, new Vector2(0.5f, 0.5f),
                Quaternion.Euler(-6f, 8f, 3f));
            // Ceiling rib / boss 2
            CreatePanel(root.transform, $"{name}_CeilProtrusion2",
                new Vector3(0.5f, hh - 0.08f, 0.5f),
                new Vector3(0.45f, 0.18f, 0.65f),
                rockMat, new Vector2(0.4f, 0.5f),
                Quaternion.Euler(4f, -5f, -2f));

            // Entrance portal is at -hd (Z=0). Keep open so player can see into the mine from outside and look back out to sunlight.
            // Front wall is open to Main Tunnel A (Z=2).
        }

        // Keep original for compatibility (unused but avoids compiler errors if referenced)
        private void BuildChamber(Vector3 centre, Vector3 size, string name)
            => BuildRockyChamber(centre, size, name);

        /// <summary>
        /// Builds a realistic rock-walled mine tunnel with irregular surfaces, protruding boulders,
        /// uneven ceiling, and natural cave appearance. Replaces the old flat-panel BuildTunnel.
        /// </summary>
        private void BuildRockyTunnel(Vector3 centre, Vector3 size, string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(mineRoot, false);
            root.transform.localPosition = centre;

            float hw  = size.x * 0.5f;    // half-width  (1.3m)
            float hh  = size.y;            // height      (2.4m)
            float hl  = size.z * 0.5f;    // half-length (13.5m)
            float len = size.z;

            // ── FLOOR: rough mine floor ──
            CreatePanel(root.transform, $"{name}_Floor",
                Vector3.zero, new Vector3(size.x, 0.05f, len),
                floorMat, new Vector2(2f, len * 0.5f));

            // ── LEFT WALL: multiple rock slab layers ──
            // Base slab (back wall layer)
            CreatePanel(root.transform, $"{name}_WallL_Back",
                new Vector3(-hw - 0.06f, hh * 0.5f, 0),
                new Vector3(0.20f, hh, len),
                rockMat, new Vector2(len * 0.25f, 1.5f));
            // Mid layer — slightly protruding, shifted
            CreatePanel(root.transform, $"{name}_WallL_Mid",
                new Vector3(-hw + 0.15f, hh * 0.45f, 0),
                new Vector3(0.30f, hh * 0.85f, len * 0.9f),
                rockMat, new Vector2(len * 0.20f, 1.2f),
                Quaternion.Euler(0f, 1.5f, -1f));
            // Upper ledge layer
            CreatePanel(root.transform, $"{name}_WallL_Upper",
                new Vector3(-hw + 0.08f, hh * 0.82f, 0),
                new Vector3(0.22f, hh * 0.28f, len * 0.95f),
                rockMat, new Vector2(len * 0.18f, 0.5f),
                Quaternion.Euler(2f, -2f, 2f));

            // ── RIGHT WALL: similar irregular treatment ──
            CreatePanel(root.transform, $"{name}_WallR_Back",
                new Vector3(+hw + 0.06f, hh * 0.5f, 0),
                new Vector3(0.20f, hh, len),
                rockMat, new Vector2(len * 0.25f, 1.5f));
            CreatePanel(root.transform, $"{name}_WallR_Mid",
                new Vector3(+hw - 0.12f, hh * 0.42f, 0),
                new Vector3(0.28f, hh * 0.80f, len * 0.88f),
                rockMat, new Vector2(len * 0.20f, 1.2f),
                Quaternion.Euler(0f, -1.5f, 1f));
            CreatePanel(root.transform, $"{name}_WallR_Upper",
                new Vector3(+hw - 0.06f, hh * 0.78f, 0),
                new Vector3(0.20f, hh * 0.32f, len * 0.92f),
                rockMat, new Vector2(len * 0.18f, 0.5f),
                Quaternion.Euler(-2f, 2f, -2f));

            // ── CEILING: irregular — main layer plus hanging bosses every 4m ──
            CreatePanel(root.transform, $"{name}_Ceiling_Base",
                new Vector3(0, hh + 0.05f, 0),
                new Vector3(size.x, 0.14f, len),
                rockMat, new Vector2(2f, len * 0.25f));

            // Ceiling variation bumps — placed every ~4m
            float[] ceilBumpZ  = { -hl + 2f, -hl + 6f, -hl + 10f, -hl + 14f, -hl + 18f, -hl + 22f };
            float[] ceilBumpX  = { -0.3f,  0.4f, -0.5f,  0.3f, -0.2f,  0.45f };
            float[] ceilBumpSz = {  0.7f,  0.5f,  0.85f, 0.6f,  0.75f, 0.55f };
            float[] ceilAngY   = {  8f,   -6f,   12f,   -9f,   5f,   -11f };
            for (int ci = 0; ci < ceilBumpZ.Length && ci < ceilBumpSz.Length; ci++)
            {
                CreatePanel(root.transform, $"{name}_CeilBoss_{ci}",
                    new Vector3(ceilBumpX[ci], hh - 0.08f, ceilBumpZ[ci]),
                    new Vector3(ceilBumpSz[ci] * 0.8f, 0.22f + (ci % 3) * 0.06f, ceilBumpSz[ci]),
                    rockMat, new Vector2(0.6f, 0.6f),
                    Quaternion.Euler((ci % 3 - 1) * 5f, ceilAngY[ci], (ci % 2 == 0 ? 3f : -3f)));
            }

            // ── LARGE WALL BOULDERS: embedded in walls at intervals ──
            // These are large rock cubes half-embedded in the wall surface
            float[] boulderZ  = { -hl + 3.5f, -hl + 8f, -hl + 12.5f, -hl + 17f, -hl + 21.5f };
            float[] boulderSz = {  0.55f,      0.65f,    0.50f,        0.70f,     0.58f };
            for (int bi = 0; bi < boulderZ.Length; bi++)
            {
                float side = (bi % 2 == 0) ? -1f : 1f; // alternate left/right
                float xPos = side * (hw - 0.1f);
                float yPos = 0.35f + (bi % 3) * 0.25f;
                float sz   = boulderSz[bi];
                CreatePanel(root.transform, $"{name}_WallBoulder_{bi}",
                    new Vector3(xPos, yPos, boulderZ[bi]),
                    new Vector3(sz * 0.55f, sz * 0.75f, sz),
                    rockMat, new Vector2(0.5f, 0.5f),
                    Quaternion.Euler((bi % 3 - 1) * 7f, side * (10f + bi * 5f), (bi % 2 == 0 ? -4f : 4f)));
            }

            // ── ARCH-SHAPED CEILING CUTOUT: round top feel ──
            // Angled top-corner panels that give impression of arched ceiling
            CreatePanel(root.transform, $"{name}_ArchL",
                new Vector3(-hw + 0.25f, hh - 0.08f, 0),
                new Vector3(0.45f, 0.35f, len),
                rockMat, new Vector2(len * 0.20f, 0.4f),
                Quaternion.Euler(0f, 0f, 28f));
            CreatePanel(root.transform, $"{name}_ArchR",
                new Vector3(+hw - 0.25f, hh - 0.08f, 0),
                new Vector3(0.45f, 0.35f, len),
                rockMat, new Vector2(len * 0.20f, 0.4f),
                Quaternion.Euler(0f, 0f, -28f));
        }

        private void BuildTunnel(Vector3 centre, Vector3 size, string name)
            => BuildRockyTunnel(centre, size, name);

        private void BuildPitProp(Vector3 position, float height, float width)
        {
            var root = new GameObject("PitProp");
            root.transform.SetParent(mineRoot, false);
            root.transform.localPosition = position;

            // Left leg
            CreateCylinder(root.transform, "LegL",
                new Vector3(-width * 0.5f + 0.08f, height * 0.5f, 0),
                new Vector3(0.08f, height * 0.5f, 0.08f), woodMat);

            // Right leg
            CreateCylinder(root.transform, "LegR",
                new Vector3(+width * 0.5f - 0.08f, height * 0.5f, 0),
                new Vector3(0.08f, height * 0.5f, 0.08f), woodMat);

            // Cross cap
            CreatePanel(root.transform, "Cap",
                new Vector3(0, height, 0),
                new Vector3(width, 0.12f, 0.18f),
                woodMat, new Vector2(1f, 0.3f));
        }

        private void BuildRails(float zStart, float zEnd, float separation)
        {
            float length = zEnd - zStart;
            float centreZ = zStart + length * 0.5f;

            // Left rail
            CreatePanel(mineRoot, "Rail_L",
                new Vector3(-separation * 0.5f, 0.03f, centreZ),
                new Vector3(0.06f, 0.06f, length),
                railMat, new Vector2(0.1f, length));

            // Right rail
            CreatePanel(mineRoot, "Rail_R",
                new Vector3(+separation * 0.5f, 0.03f, centreZ),
                new Vector3(0.06f, 0.06f, length),
                railMat, new Vector2(0.1f, length));

            // Sleepers every 0.6m
            for (float z = zStart + 0.3f; z < zEnd; z += 0.6f)
            {
                CreatePanel(mineRoot, "Sleeper",
                    new Vector3(0, 0.01f, z),
                    new Vector3(separation + 0.15f, 0.04f, 0.12f),
                    woodMat, new Vector2(0.8f, 0.1f));
            }
        }

        private void BuildWallPipes(float xPos, float zStart, float zEnd)
        {
            float length = zEnd - zStart;
            float centreZ = zStart + length * 0.5f;

            // Main pipe
            CreateCylinder(mineRoot, "WallPipe_Main",
                new Vector3(xPos, 1.8f, centreZ),
                new Vector3(0.06f, length * 0.5f, 0.06f),
                pipeMat,
                rotation: Quaternion.Euler(90, 0, 0));

            // Secondary smaller pipe
            CreateCylinder(mineRoot, "WallPipe_Small",
                new Vector3(xPos, 1.55f, centreZ),
                new Vector3(0.04f, length * 0.5f, 0.04f),
                pipeMat,
                rotation: Quaternion.Euler(90, 0, 0));

            // Pipe brackets every 2m
            for (float z = zStart + 1f; z < zEnd; z += 2f)
            {
                CreatePanel(mineRoot, "PipeBracket",
                    new Vector3(xPos + (xPos < 0 ? 0.08f : -0.08f), 1.72f, z),
                    new Vector3(0.12f, 0.3f, 0.04f),
                    metalMat, new Vector2(0.2f, 0.3f));
            }
        }

        private void BuildCableTray(float xPos, float zStart, float zEnd)
        {
            float length = zEnd - zStart;
            float centreZ = zStart + length * 0.5f;

            // Cable tray base
            CreatePanel(mineRoot, "CableTray",
                new Vector3(xPos, 2.1f, centreZ),
                new Vector3(0.3f, 0.04f, length),
                metalMat, new Vector2(0.3f, length * 0.5f));

            // Simulate cable bundle (dark cylinder)
            CreateCylinder(mineRoot, "CableBundle",
                new Vector3(xPos, 2.14f, centreZ),
                new Vector3(0.05f, length * 0.5f, 0.05f),
                CreateSolidMaterial(new Color(0.10f, 0.10f, 0.10f)),
                rotation: Quaternion.Euler(90, 0, 0));
        }

        private void BuildMarkerLamp(Vector3 position)
        {
            var root = new GameObject("MarkerLamp");
            root.transform.SetParent(mineRoot, false);
            root.transform.localPosition = position;

            // Lamp housing
            var housing = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            housing.name = "LampGlobe";
            housing.transform.SetParent(root.transform, false);
            housing.transform.localPosition = Vector3.zero;
            housing.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
            housing.GetComponent<Renderer>().material = markerLampMat;
            Destroy(housing.GetComponent<Collider>());

            // Emissive point light
            CreatePointLight(root.transform, Vector3.zero, markerLampColor, 12f, 6f, "MarkerLight");

            // Mounting rod
            CreateCylinder(root.transform, "LampRod",
                new Vector3(0, 0.2f, 0),
                new Vector3(0.03f, 0.2f, 0.03f),
                metalMat);
        }

        private void BuildDustParticles(Vector3 centre)
        {
            var go = new GameObject("MineDustParticles");
            go.transform.SetParent(mineRoot, false);
            go.transform.localPosition = centre;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.025f);
            main.startColor = new Color(0.7f, 0.65f, 0.55f, 0.15f);
            main.maxParticles = 300;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 30f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(2.4f, 2.0f, 14f);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            // Use URP-compatible particle shader with fallback chain
            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended")
                ?? Shader.Find("Standard");
            var particleMat = new Material(particleShader);
            particleMat.color = new Color(0.7f, 0.65f, 0.55f, 0.18f);
            if (particleMat.HasProperty("_Surface")) particleMat.SetFloat("_Surface", 1f); // Transparent
            renderer.material = particleMat;
            renderer.sortingOrder = 1;

            ps.Play();
        }

        // ── Lighting ──────────────────────────────────────────────────────────

        private void ConfigureLighting()
        {
            // Player starts outdoors — bright sunlit ambient.
            // UpdateDynamicLighting() blends to dark underground as player walks in.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = OutdoorAmbient;
            RenderSettings.fog = false;         // outdoors: no fog
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.04f, 0.04f, 0.05f, 1f);
            RenderSettings.fogStartDistance = 6f;
            RenderSettings.fogEndDistance = 18f;

            // Disable default Unity scene directional lights — we use our own sun
            var dirLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in dirLights)
            {
                if (l.type == LightType.Directional && l.gameObject.name != "Outdoor_DirectionalSun")
                    l.enabled = false;
            }
        }

        // ── Dynamic Lighting: blend outdoor ↔ underground as player moves ────

        private void Update()
        {
            if (_movCtrl == null)
                _movCtrl = FindObjectOfType<MineMovementController>();
            if (_movCtrl != null)
                UpdateDynamicLighting(_movCtrl.VirtualPosition.z);
        }

        private void UpdateDynamicLighting(float playerZ)
        {
            // t = 0 → fully outdoor (bright), t = 1 → fully underground (very dark)
            // Transition zone: Z = -2m .. 5m (7-metre blend through portal & entry)
            float t = Mathf.Clamp01((playerZ + 2f) / 7f);

            RenderSettings.ambientLight = Color.Lerp(OutdoorAmbient, UndergroundAmbient, t);

            if (playerZ < -2f)
            {
                // Fully outdoors — no fog
                RenderSettings.fog = false;
            }
            else if (t < 1f)
            {
                // Transitioning through portal — subtle haze builds up
                RenderSettings.fog = true;
                RenderSettings.fogColor = Color.Lerp(
                    new Color(0.55f, 0.62f, 0.78f),
                    new Color(0.04f, 0.04f, 0.05f), t);
                RenderSettings.fogStartDistance = Mathf.Lerp(28f, 6f,  t);
                RenderSettings.fogEndDistance   = Mathf.Lerp(65f, 18f, t);
            }
            else
            {
                // Fully underground — dark narrow fog
                RenderSettings.fog = true;
                RenderSettings.fogColor = new Color(0.04f, 0.04f, 0.05f);
                RenderSettings.fogStartDistance = 6f;
                RenderSettings.fogEndDistance = 18f;
            }

            // Gradually dim directional sun as player walks in
            if (_dirSunLight == null)
            {
                foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (l.name == "Outdoor_DirectionalSun") { _dirSunLight = l; break; }
            }
            if (_dirSunLight != null)
                _dirSunLight.intensity = Mathf.Lerp(1.15f, 0f, t);
        }

        // ── Primitive Helpers ─────────────────────────────────────────────────

        private void CreatePanel(Transform parent, string name, Vector3 localPos, Vector3 localScale,
            Material mat, Vector2 tiling)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var r = go.GetComponent<Renderer>();
            r.material = mat;
            r.material.mainTextureScale = tiling;
            Destroy(go.GetComponent<Collider>());
        }

        // Overload for mineRoot (Transform)
        private void CreatePanel(Transform parent, string name, Vector3 worldPos, Vector3 scale,
            Material mat, Vector2 tiling, bool worldSpace = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (worldSpace)
            {
                go.transform.SetParent(parent, true);
                go.transform.position = mineRoot.TransformPoint(worldPos);
            }
            else
            {
                go.transform.SetParent(parent, false);
                go.transform.localPosition = worldPos;
            }
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.material = mat;
            r.material.mainTextureScale = tiling;
            Destroy(go.GetComponent<Collider>());
        }

        private void CreateCylinder(Transform parent, string name, Vector3 localPos,
            Vector3 localScale, Material mat, Quaternion? rotation = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            if (rotation.HasValue)
                go.transform.localRotation = rotation.Value;
            go.GetComponent<Renderer>().material = mat;
            Destroy(go.GetComponent<Collider>());
        }

        private void CreatePointLight(Vector3 worldPos, Color color, float intensity,
            float range, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(mineRoot, false);
            go.transform.localPosition = worldPos;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
        }

        private void CreatePointLight(Transform parent, Vector3 localPos, Color color,
            float intensity, float range, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
        }

        private void CreatePanel(Transform parent, string name, Vector3 localPos, Vector3 localScale,
            Material mat, Vector2 tiling, Quaternion rotation)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = rotation;
            go.transform.localScale = localScale;
            var r = go.GetComponent<Renderer>();
            r.material = mat;
            r.material.mainTextureScale = tiling;
            Destroy(go.GetComponent<Collider>());
        }

        // ── Phase 2C: Y-Junction & Branch Construction ─────────────────────────

        private void BuildYJunction(float startZ)
        {
            var junctionRoot = new GameObject("Y_Junction_EmergencyExit");
            junctionRoot.transform.SetParent(mineRoot, false);

            float splitZ = startZ + 4.0f; // 33m

            // 0. ── Continuous Bedrock Sub-Floor (FIX 1: Prevents any blue background glitch or gap) ──
            // Spans Z = 25m to 55m and X = -14m to +14m so camera clearFlags sky-blue can NEVER be seen
            CreatePanel(junctionRoot.transform, "Junction_Bedrock_SubFloor",
                new Vector3(0, -0.05f, startZ + 12.0f),
                new Vector3(28.0f, 0.10f, 32.0f),
                floorMat, new Vector2(8f, 10f));

            // 1. ── Junction Approach / Expansion (Z = 29m to 33m) ─────────────
            // Expanded width 6.4m extends well beyond the angled walls (X = -3.2m to +3.2m)
            CreatePanel(junctionRoot.transform, "Junction_Floor_Main",
                new Vector3(0, 0, startZ + 2.0f),
                new Vector3(6.4f, 0.05f, 4.5f),
                floorMat, new Vector2(3f, 2f));

            // Wide Junction Throat Transition Floor Plate (Z = 31.5m to 37.5m, X = -4.0m to +4.0m)
            // Bridges seamlessly into both Route A and Route B angled floor thresholds
            CreatePanel(junctionRoot.transform, "Junction_Floor_Throat",
                new Vector3(0, 0.005f, startZ + 5.0f),
                new Vector3(8.2f, 0.05f, 6.0f),
                floorMat, new Vector2(4f, 3f));

            // Solid Splitter Pillar Base Plate directly under and around the rock wedge
            CreatePanel(junctionRoot.transform, "Junction_Splitter_BaseFloor",
                new Vector3(0, 0.01f, splitZ + 2.5f),
                new Vector3(3.2f, 0.05f, 6.5f),
                floorMat, new Vector2(2f, 3f));

            // Ceiling
            CreatePanel(junctionRoot.transform, "Junction_Ceiling",
                new Vector3(0, 2.45f, startZ + 2.0f),
                new Vector3(4.4f, 0.08f, 4.0f),
                rockMat, new Vector2(3f, 2f));

            // Left Expanding Wall: angles left from (-1.3, 29) to (-2.7, 33)
            CreatePanel(junctionRoot.transform, "Junction_WallL_Angle",
                new Vector3(-2.0f, 1.22f, startZ + 2.0f),
                new Vector3(0.08f, 2.45f, 4.25f),
                rockMat, new Vector2(2f, 1.5f),
                Quaternion.Euler(0, -19.0f, 0));

            // Right Expanding Wall: angles right from (+1.3, 29) to (+2.7, 33)
            CreatePanel(junctionRoot.transform, "Junction_WallR_Angle",
                new Vector3(+2.0f, 1.22f, startZ + 2.0f),
                new Vector3(0.08f, 2.45f, 4.25f),
                rockMat, new Vector2(2f, 1.5f),
                Quaternion.Euler(0, +19.0f, 0));

            // Junction Entrance Arch Prop at 29.5m
            BuildPitProp(new Vector3(0, 0, startZ + 0.5f), 2.4f, 2.8f);

            // Mid-junction wide timber support set at 31.5m
            BuildPitProp(new Vector3(0, 0, startZ + 2.5f), 2.45f, 4.2f);

            // Overhead Hub Lamp at 31.2m
            CreatePointLight(junctionRoot.transform, new Vector3(0, 2.2f, startZ + 2.2f),
                workLightColor, 12f, 9f, "Junction_HubLamp");

            // Fanning Rails from 29m to 33m — diverts exclusively into Route B (Haulage Drift)
            BuildFanningRails(junctionRoot.transform, startZ, startZ + 4.0f);

            // 2. ── Central Splitter Rock Pillar & Sign (Z = 33m to 38m) ────────
            BuildSplitterPillarAndSign(junctionRoot.transform, splitZ);

            bool routeAIsEmergency = !isRouteBEmergencyExit;
            bool routeBIsEmergency = isRouteBEmergencyExit;

            // 3. ── Route A: Left Branch ──────
            BuildBranch(junctionRoot.transform, routeAIsEmergency ? "RouteA_EmergencyExit" : "RouteA_HaulageDrift",
                origin: new Vector3(-1.75f, 0, splitZ),
                angleDeg: -22.0f,
                length: 15.0f,
                isEmergencyRoute: routeAIsEmergency);

            // 4. ── Route B: Right Branch ──────
            BuildBranch(junctionRoot.transform, routeBIsEmergency ? "RouteB_EmergencyExit" : "RouteB_HaulageDrift",
                origin: new Vector3(+1.75f, 0, splitZ),
                angleDeg: +22.0f,
                length: 15.0f,
                isEmergencyRoute: routeBIsEmergency);

            // 5. ── Dust Particles for Junction Volume ──────────────────────────
            BuildDustParticles(new Vector3(0, 1.2f, splitZ + 4.0f));
        }

        private void BuildSplitterPillarAndSign(Transform parent, float splitZ)
        {
            var pillarRoot = new GameObject("CentralSplitterPillar");
            pillarRoot.transform.SetParent(parent, false);

            // Front nose bumper posts
            CreateCylinder(pillarRoot.transform, "BumperPost1",
                new Vector3(-0.25f, 1.0f, splitZ - 0.15f),
                new Vector3(0.16f, 1.0f, 0.16f), woodMat);
            CreateCylinder(pillarRoot.transform, "BumperPost2",
                new Vector3(+0.25f, 1.0f, splitZ - 0.15f),
                new Vector3(0.16f, 1.0f, 0.16f), woodMat);
            CreateCylinder(pillarRoot.transform, "BumperPostCenter",
                new Vector3(0f, 1.1f, splitZ - 0.28f),
                new Vector3(0.18f, 1.1f, 0.18f), woodMat);

            // Central solid rock wedge
            CreatePanel(pillarRoot.transform, "RockWedge_Core",
                new Vector3(0, 1.25f, splitZ + 2.5f),
                new Vector3(1.6f, 2.45f, 5.0f),
                rockMat, new Vector2(2f, 2f));

            // Left wedge angled face (Route A side)
            CreatePanel(pillarRoot.transform, "WedgeFace_L",
                new Vector3(-0.65f, 1.25f, splitZ + 2.0f),
                new Vector3(0.08f, 2.45f, 4.5f),
                rockMat, new Vector2(2f, 1.5f),
                Quaternion.Euler(0, -11.0f, 0));

            // Right wedge angled face (Route B side)
            CreatePanel(pillarRoot.transform, "WedgeFace_R",
                new Vector3(+0.65f, 1.25f, splitZ + 2.0f),
                new Vector3(0.08f, 2.45f, 4.5f),
                rockMat, new Vector2(2f, 1.5f),
                Quaternion.Euler(0, +11.0f, 0));

            // ── Dual-Arrow Directional Sign (High-Contrast Industrial Mine Gantry) ──
            var signGO = new GameObject("JunctionDirectionalSign");
            signGO.transform.SetParent(pillarRoot.transform, false);
            // Positioned in front of bumper posts at eye level for immediate legibility
            signGO.transform.localPosition = new Vector3(0, 1.60f, splitZ - 0.55f);

            // 1. Heavy dark steel backing plate
            CreatePanel(signGO.transform, "SignBacking",
                Vector3.zero, new Vector3(1.72f, 0.54f, 0.04f),
                signBackMat, new Vector2(1f, 1f));

            // 2. Center vertical steel separator bar
            CreatePanel(signGO.transform, "SignCenterDivider",
                new Vector3(0, 0, -0.025f), new Vector3(0.04f, 0.52f, 0.02f),
                metalMat, new Vector2(1f, 1f));

            // 3. Left half — Route A
            Material leftBorderMat = isRouteBEmergencyExit ? cautionMat : emergencyExitMat;
            Color leftTextColor = isRouteBEmergencyExit ? new Color(1.0f, 0.78f, 0.15f) : new Color(0.12f, 1.0f, 0.45f);
            string leftText = isRouteBEmergencyExit ? "◄ ROUTE A\nHAULAGE DRIFT" : "◄ ROUTE A\nEMERGENCY EXIT";

            // Left safety border
            CreatePanel(signGO.transform, "SignLeft_Border",
                new Vector3(-0.42f, 0, -0.025f), new Vector3(0.78f, 0.46f, 0.015f),
                leftBorderMat, new Vector2(1f, 1f));
            // Dark high-contrast inner panel
            CreatePanel(signGO.transform, "SignLeft_InnerDark",
                new Vector3(-0.42f, 0, -0.035f), new Vector3(0.74f, 0.42f, 0.01f),
                signBackMat, new Vector2(1f, 1f));

            // Left text
            var textLGO = new GameObject("Text_RouteA");
            textLGO.transform.SetParent(signGO.transform, false);
            textLGO.transform.localPosition = new Vector3(-0.42f, 0, -0.065f);
            textLGO.transform.localScale = Vector3.one * 0.015f;
            var tmA = textLGO.AddComponent<TextMesh>();
            tmA.text = leftText;
            tmA.fontSize = 42;
            tmA.alignment = TextAlignment.Center;
            tmA.anchor = TextAnchor.MiddleCenter;
            tmA.color = leftTextColor;
            var mrA = textLGO.GetComponent<MeshRenderer>();
            if (mrA != null) mrA.sortingOrder = 10;

            // 4. Right half — Route B
            Material rightBorderMat = isRouteBEmergencyExit ? emergencyExitMat : cautionMat;
            Color rightTextColor = isRouteBEmergencyExit ? new Color(0.12f, 1.0f, 0.45f) : new Color(1.0f, 0.78f, 0.15f);
            string rightText = isRouteBEmergencyExit ? "ROUTE B ►\nEMERGENCY EXIT" : "ROUTE B ►\nHAULAGE DRIFT";

            // Right safety border
            CreatePanel(signGO.transform, "SignRight_Border",
                new Vector3(+0.42f, 0, -0.025f), new Vector3(0.78f, 0.46f, 0.015f),
                rightBorderMat, new Vector2(1f, 1f));
            // Dark high-contrast inner panel
            CreatePanel(signGO.transform, "SignRight_InnerDark",
                new Vector3(+0.42f, 0, -0.035f), new Vector3(0.74f, 0.42f, 0.01f),
                signBackMat, new Vector2(1f, 1f));

            // Right text
            var textRGO = new GameObject("Text_RouteB");
            textRGO.transform.SetParent(signGO.transform, false);
            textRGO.transform.localPosition = new Vector3(+0.42f, 0, -0.065f);
            textRGO.transform.localScale = Vector3.one * 0.015f;
            var tmB = textRGO.AddComponent<TextMesh>();
            tmB.text = rightText;
            tmB.fontSize = 42;
            tmB.alignment = TextAlignment.Center;
            tmB.anchor = TextAnchor.MiddleCenter;
            tmB.color = rightTextColor;
            var mrB = textRGO.GetComponent<MeshRenderer>();
            if (mrB != null) mrB.sortingOrder = 10;

            // 5. Overhead Dedicated Sign Downlight
            CreatePointLight(signGO.transform, new Vector3(0, 0.50f, -0.35f),
                new Color(1.0f, 1.0f, 0.95f), 7f, 4.0f, "SignSpotlight");
        }

        private void BuildBranch(Transform parent, string branchName, Vector3 origin,
            float angleDeg, float length, bool isEmergencyRoute)
        {
            var branchRoot = new GameObject(branchName);
            branchRoot.transform.SetParent(parent, false);

            Quaternion rot = Quaternion.Euler(0, angleDeg, 0);
            Vector3 fwd = rot * Vector3.forward;
            Vector3 right = rot * Vector3.right;
            Vector3 midPoint = origin + fwd * (length * 0.5f);

            float branchWidth = 2.40f;
            float branchHeight = 2.40f;

            // Floor
            CreatePanel(branchRoot.transform, $"{branchName}_Floor",
                midPoint, new Vector3(branchWidth, 0.05f, length),
                floorMat, new Vector2(2f, length * 0.4f), rot);

            // Ceiling
            CreatePanel(branchRoot.transform, $"{branchName}_Ceiling",
                midPoint + new Vector3(0, branchHeight, 0),
                new Vector3(branchWidth, 0.08f, length),
                rockMat, new Vector2(2f, length * 0.4f), rot);

            // Outer Wall
            Vector3 outerWallPos = midPoint + (angleDeg < 0 ? -right : right) * (branchWidth * 0.5f);
            outerWallPos.y = branchHeight * 0.5f;
            CreatePanel(branchRoot.transform, $"{branchName}_OuterWall",
                outerWallPos, new Vector3(0.08f, branchHeight, length),
                rockMat, new Vector2(length * 0.4f, 1.5f), rot);

            // Inner Wall
            Vector3 innerWallPos = midPoint + (angleDeg < 0 ? right : -right) * (branchWidth * 0.5f);
            innerWallPos.y = branchHeight * 0.5f;
            CreatePanel(branchRoot.transform, $"{branchName}_InnerWall",
                innerWallPos, new Vector3(0.08f, branchHeight, length),
                rockMat, new Vector2(length * 0.4f, 1.5f), rot);

            // Branch Termination / Extension
            if (!isEmergencyRoute)
            {
                // Route B is a dead end (Haulage Drift)
                Vector3 endPos = origin + fwd * length;
                endPos.y = branchHeight * 0.5f;
                CreatePanel(branchRoot.transform, $"{branchName}_EndWall",
                    endPos, new Vector3(branchWidth, branchHeight, 0.08f),
                    rockMat, new Vector2(2f, 1.5f), rot);
                BuildRouteBBlockage(branchRoot.transform, endPos, rot, branchWidth);
            }
            else
            {
                // Route A extends continuously through the narrower rocky tunnel, exterior portal, and outdoor assembly area
                Vector3 branchEnd = origin + fwd * length;
                BuildExtendedEmergencyRoute(branchRoot.transform, branchEnd);
            }

            // Timber Pit Props every 3.5m along branch
            for (float d = 3.0f; d < length - 1.5f; d += 3.5f)
            {
                Vector3 propPos = origin + fwd * d;
                BuildBranchPitProp(branchRoot.transform, propPos, branchHeight, branchWidth, rot);
            }

            // Rails along branch (ONLY on Route B — Route A is a dedicated walking evacuation route)
            if (!isEmergencyRoute)
            {
                BuildBranchRails(branchRoot.transform, origin, fwd, right, length, 0.8f, rot);
            }
            else
            {
                // FIX 2: Dedicated Emergency Evacuation Walking Route (NO rails + rich mining evacuation props)
                BuildRouteAEvacuationProps(branchRoot.transform, origin, fwd, right, length, branchWidth, branchHeight, rot);
            }

            // Overhead Lights along branch
            CreatePointLight(branchRoot.transform, origin + fwd * 4.0f + new Vector3(0, 2.1f, 0),
                isEmergencyRoute ? new Color(0.85f, 1.0f, 0.85f) : workLightColor, 9f, 6.5f, $"{branchName}_Lamp1");
            CreatePointLight(branchRoot.transform, origin + fwd * 10.5f + new Vector3(0, 2.1f, 0),
                isEmergencyRoute ? new Color(0.85f, 1.0f, 0.85f) : workLightColor, 9f, 6.5f, $"{branchName}_Lamp2");

            // Branch Signage
            if (isEmergencyRoute)
            {
                // Green emergency exit running man plaques on outer wall at 3.5m and 8.5m
                Vector3 sign1Pos = origin + fwd * 3.5f - right * (branchWidth * 0.5f - 0.05f) + new Vector3(0, 1.5f, 0);
                CreateBranchExitSign(branchRoot.transform, sign1Pos, rot * Quaternion.Euler(0, 90f, 0));

                Vector3 sign2Pos = origin + fwd * 8.5f - right * (branchWidth * 0.5f - 0.05f) + new Vector3(0, 1.5f, 0);
                CreateBranchExitSign(branchRoot.transform, sign2Pos, rot * Quaternion.Euler(0, 90f, 0));
            }
            else
            {
                // Caution plaque on Route B wall
                Vector3 cautionPos = origin + fwd * 3.5f + right * (branchWidth * 0.5f - 0.05f) + new Vector3(0, 1.5f, 0);
                CreateBranchCautionSign(branchRoot.transform, cautionPos, rot * Quaternion.Euler(0, -90f, 0));
            }
        }

        private void BuildBranchPitProp(Transform parent, Vector3 position, float height, float width, Quaternion rot)
        {
            var root = new GameObject("BranchPitProp");
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.transform.rotation = rot;

            // Left leg
            CreateCylinder(root.transform, "LegL",
                new Vector3(-width * 0.5f + 0.08f, height * 0.5f, 0),
                new Vector3(0.08f, height * 0.5f, 0.08f), woodMat);

            // Right leg
            CreateCylinder(root.transform, "LegR",
                new Vector3(+width * 0.5f - 0.08f, height * 0.5f, 0),
                new Vector3(0.08f, height * 0.5f, 0.08f), woodMat);

            // Cross cap
            CreatePanel(root.transform, "Cap",
                new Vector3(0, height, 0),
                new Vector3(width, 0.12f, 0.18f),
                woodMat, new Vector2(1f, 0.3f));
        }

        private void BuildBranchRails(Transform parent, Vector3 origin, Vector3 fwd, Vector3 right, float length, float separation, Quaternion rot)
        {
            Vector3 midPoint = origin + fwd * (length * 0.5f);

            // Left rail
            CreatePanel(parent, "BranchRail_L",
                midPoint - right * (separation * 0.5f) + new Vector3(0, 0.03f, 0),
                new Vector3(0.06f, 0.06f, length), railMat, new Vector2(0.1f, length), rot);

            // Right rail
            CreatePanel(parent, "BranchRail_R",
                midPoint + right * (separation * 0.5f) + new Vector3(0, 0.03f, 0),
                new Vector3(0.06f, 0.06f, length), railMat, new Vector2(0.1f, length), rot);

            // Sleepers every 0.7m
            for (float d = 0.5f; d < length; d += 0.7f)
            {
                Vector3 sleeperPos = origin + fwd * d + new Vector3(0, 0.015f, 0);
                CreatePanel(parent, "BranchSleeper",
                    sleeperPos, new Vector3(separation + 0.15f, 0.04f, 0.12f),
                    woodMat, new Vector2(0.8f, 0.1f), rot);
            }
        }

        private void BuildFanningRails(Transform parent, float startZ, float endZ)
        {
            // Curved rail turnout that diverts exclusively into Route B (Haulage Drift)
            // Ensures Route A has ZERO rails leading into it
            int railSteps = 8;
            float stepLen = (endZ - startZ) / railSteps;
            float railSeparation = 0.8f;

            for (int i = 0; i < railSteps; i++)
            {
                float z0 = startZ + i * stepLen;
                float z1 = startZ + (i + 1) * stepLen;
                float midZ = (z0 + z1) * 0.5f;
                float midT = (float)(i + 0.5f) / railSteps;

                // Smooth S-curve into Route B's start at X = +1.75m
                float smoothT = midT * midT * (3f - 2f * midT);
                float midX = Mathf.Lerp(0f, +1.75f, smoothT);

                float dx = (+1.75f) * (6f * midT * (1f - midT)) * (1f / railSteps);
                float angle = Mathf.Atan2(dx, stepLen) * Mathf.Rad2Deg;
                Quaternion segRot = Quaternion.Euler(0, angle, 0);

                Vector3 segPos = new Vector3(midX, 0.03f, midZ);
                Vector3 right = segRot * Vector3.right;

                CreatePanel(parent, $"TurnoutRail_L_{i}",
                    segPos - right * (railSeparation * 0.5f),
                    new Vector3(0.06f, 0.06f, stepLen + 0.06f),
                    railMat, new Vector2(0.1f, 1f), segRot);

                CreatePanel(parent, $"TurnoutRail_R_{i}",
                    segPos + right * (railSeparation * 0.5f),
                    new Vector3(0.06f, 0.06f, stepLen + 0.06f),
                    railMat, new Vector2(0.1f, 1f), segRot);

                // Timber sleeper every step
                CreatePanel(parent, $"TurnoutSleeper_{i}",
                    new Vector3(midX, 0.015f, midZ),
                    new Vector3(railSeparation + 0.35f, 0.04f, 0.14f),
                    woodMat, new Vector2(0.8f, 0.1f), segRot);
            }
        }

        /// <summary>
        /// FIX 2: Builds rich, realistic natural mining evacuation props along Route A.
        /// Props are kept on the side margins (X offsets ±0.85m to ±1.05m),
        /// maintaining a completely clear 1.5m wide central walking corridor.
        /// </summary>
        private void BuildRouteAEvacuationProps(Transform parent, Vector3 origin, Vector3 fwd, Vector3 right,
            float length, float width, float height, Quaternion rot)
        {
            var propsRoot = new GameObject("RouteA_EvacuationProps");
            propsRoot.transform.SetParent(parent, false);

            float halfW = width * 0.5f;

            // 1. Steel Support Arch Sets (Occasional steel arches at d = 4.8m and d = 11.2m)
            BuildSteelArchSet(propsRoot.transform, origin + fwd * 4.8f, height, width, rot);
            BuildSteelArchSet(propsRoot.transform, origin + fwd * 11.2f, height, width, rot);

            // 2. Left Wall Margin (X' ≈ -halfW + 0.26m) — Mining Tools, Safety Equipment, Hardhats
            // d = 1.8m: Illuminated green emergency exit running-man plaque
            CreateBranchExitSign(propsRoot.transform,
                origin + fwd * 1.8f - right * (halfW - 0.05f) + new Vector3(0, 1.55f, 0),
                rot * Quaternion.Euler(0, 90f, 0));

            // d = 2.8m: Miner hardhats on timber support bench
            BuildHardhatProp(propsRoot.transform, origin + fwd * 2.8f - right * (halfW - 0.28f) + new Vector3(0, 0.05f, 0));
            var whiteHatGO = new GameObject("SupervisorHardhat");
            whiteHatGO.transform.SetParent(propsRoot.transform, false);
            whiteHatGO.transform.position = origin + fwd * 3.1f - right * (halfW - 0.28f);
            CreatePanel(whiteHatGO.transform, "CapWhite", Vector3.zero, new Vector3(0.24f, 0.14f, 0.26f),
                hardhatWhiteMat, new Vector2(0.2f, 0.2f), rot);

            // d = 5.5m: Mining Tools — Pickaxe and Shovel leaning against wall
            BuildPickaxeProp(propsRoot.transform, origin + fwd * 5.4f - right * (halfW - 0.22f),
                rot * Quaternion.Euler(0, 35f, 55f));
            BuildShovelProp(propsRoot.transform, origin + fwd * 6.0f - right * (halfW - 0.20f),
                rot * Quaternion.Euler(0, -25f, 60f));

            // d = 6.8m: Industrial Red Steel Toolbox on floor
            var tboxGO = new GameObject("EvacToolbox");
            tboxGO.transform.SetParent(propsRoot.transform, false);
            tboxGO.transform.position = origin + fwd * 6.8f - right * (halfW - 0.28f);
            tboxGO.transform.rotation = rot * Quaternion.Euler(0, 20f, 0);
            CreatePanel(tboxGO.transform, "BoxBody", new Vector3(0, 0.12f, 0), new Vector3(0.44f, 0.24f, 0.22f),
                CreateSolidMaterial(new Color(0.85f, 0.15f, 0.10f)), new Vector2(0.4f, 0.2f));
            CreatePanel(tboxGO.transform, "BoxHandle", new Vector3(0, 0.26f, 0), new Vector3(0.14f, 0.04f, 0.03f),
                metalMat, new Vector2(0.1f, 0.04f));

            // d = 9.0m: Emergency First Aid & Respirator Wall Cabinet
            var cabGO = new GameObject("EmergencyCabinet");
            cabGO.transform.SetParent(propsRoot.transform, false);
            cabGO.transform.position = origin + fwd * 9.0f - right * (halfW - 0.06f) + new Vector3(0, 1.40f, 0);
            cabGO.transform.rotation = rot * Quaternion.Euler(0, 90f, 0);
            CreatePanel(cabGO.transform, "CabinetBody", Vector3.zero, new Vector3(0.40f, 0.52f, 0.16f),
                emergencyExitMat, new Vector2(1f, 1f));
            CreatePanel(cabGO.transform, "CrossH", new Vector3(0, 0, -0.09f), new Vector3(0.24f, 0.06f, 0.02f),
                CreateSolidMaterial(Color.white), new Vector2(1f, 1f));
            CreatePanel(cabGO.transform, "CrossV", new Vector3(0, 0, -0.09f), new Vector3(0.06f, 0.24f, 0.02f),
                CreateSolidMaterial(Color.white), new Vector2(1f, 1f));

            // d = 11.5m: Pneumatic Rock Drill on timber skid base
            BuildPneumaticDrillProp(propsRoot.transform, origin + fwd * 11.5f - right * (halfW - 0.26f), rot);

            // d = 13.8m: Illuminated green directional exit sign
            CreateBranchExitSign(propsRoot.transform,
                origin + fwd * 13.8f - right * (halfW - 0.05f) + new Vector3(0, 1.55f, 0),
                rot * Quaternion.Euler(0, 90f, 0));

            // 3. Right Wall Margin (X' ≈ +halfW - 0.26m) — Sacks, Cable Reel, Rubble, Beams
            // d = 2.4m: Stacked stone dust / sand bags
            BuildSandbagStack(propsRoot.transform, origin + fwd * 2.4f + right * (halfW - 0.28f), rot);

            // d = 4.2m: Rockfall rubble pile along the rock wall base
            BuildRockPileProp(propsRoot.transform, origin + fwd * 4.2f + right * (halfW - 0.28f), 0.55f);

            // d = 7.2m: Industrial wooden cable reel drum
            BuildCableReelProp(propsRoot.transform, origin + fwd * 7.2f + right * (halfW - 0.32f));

            // d = 9.8m: Stack of spare wooden timber lagging beams
            BuildTimberBeamStack(propsRoot.transform, origin + fwd * 9.8f + right * (halfW - 0.26f), rot);

            // d = 12.8m: Scattered rock chunks along right wall base
            BuildRockPileProp(propsRoot.transform, origin + fwd * 12.8f + right * (halfW - 0.30f), 0.44f);

            // 4. Overhead utility conduit & air lines
            BuildConduitPipes(propsRoot.transform, origin, fwd, right, length, width, height, rot);
        }

        private void BuildSteelArchSet(Transform parent, Vector3 center, float height, float width, Quaternion rot)
        {
            var archRoot = new GameObject("SteelArchSupport");
            archRoot.transform.SetParent(parent, false);
            archRoot.transform.position = center;
            archRoot.transform.rotation = rot;

            float hw = width * 0.5f - 0.06f;

            // Left steel upright column
            CreatePanel(archRoot.transform, "ColumnL",
                new Vector3(-hw, height * 0.5f, 0),
                new Vector3(0.10f, height, 0.12f), metalMat, new Vector2(0.2f, 1f));

            // Right steel upright column
            CreatePanel(archRoot.transform, "ColumnR",
                new Vector3(+hw, height * 0.5f, 0),
                new Vector3(0.10f, height, 0.12f), metalMat, new Vector2(0.2f, 1f));

            // Overhead arch cross beam
            CreatePanel(archRoot.transform, "CrossBeam",
                new Vector3(0, height - 0.06f, 0),
                new Vector3(width, 0.12f, 0.14f), metalMat, new Vector2(1f, 0.2f));

            // Corner steel gusset braces
            CreatePanel(archRoot.transform, "GussetL",
                new Vector3(-hw + 0.18f, height - 0.18f, 0),
                new Vector3(0.26f, 0.05f, 0.10f), metalMat, new Vector2(0.2f, 0.1f),
                Quaternion.Euler(0, 0, -45f));
            CreatePanel(archRoot.transform, "GussetR",
                new Vector3(+hw - 0.18f, height - 0.18f, 0),
                new Vector3(0.26f, 0.05f, 0.10f), metalMat, new Vector2(0.2f, 0.1f),
                Quaternion.Euler(0, 0, +45f));
        }

        private void BuildSandbagStack(Transform parent, Vector3 position, Quaternion rot)
        {
            var stackRoot = new GameObject("SandbagStack");
            stackRoot.transform.SetParent(parent, false);
            stackRoot.transform.position = position;
            stackRoot.transform.rotation = rot;

            CreatePanel(stackRoot.transform, "Bag1", new Vector3(0, 0.09f, -0.15f),
                new Vector3(0.40f, 0.16f, 0.30f), outdoorDirtMat, new Vector2(0.5f, 0.5f), Quaternion.Euler(0, 5f, 0));
            CreatePanel(stackRoot.transform, "Bag2", new Vector3(0, 0.09f, +0.15f),
                new Vector3(0.40f, 0.16f, 0.30f), outdoorDirtMat, new Vector2(0.5f, 0.5f), Quaternion.Euler(0, -8f, 0));
            CreatePanel(stackRoot.transform, "Bag3", new Vector3(0, 0.24f, 0),
                new Vector3(0.38f, 0.15f, 0.32f), outdoorDirtMat, new Vector2(0.5f, 0.5f), Quaternion.Euler(0, 12f, 0));
        }

        private void BuildTimberBeamStack(Transform parent, Vector3 position, Quaternion rot)
        {
            var beamRoot = new GameObject("TimberBeamStack");
            beamRoot.transform.SetParent(parent, false);
            beamRoot.transform.position = position;
            beamRoot.transform.rotation = rot;

            for (int i = 0; i < 3; i++)
            {
                CreatePanel(beamRoot.transform, $"Beam_{i}",
                    new Vector3(0, 0.06f + i * 0.11f, 0),
                    new Vector3(0.22f, 0.10f, 1.40f),
                    woodMat, new Vector2(0.3f, 1.5f), Quaternion.Euler(0, (i % 2) * 3f, 0));
            }
        }

        private void BuildPneumaticDrillProp(Transform parent, Vector3 position, Quaternion rot)
        {
            var drillRoot = new GameObject("PneumaticRockDrill");
            drillRoot.transform.SetParent(parent, false);
            drillRoot.transform.position = position;
            drillRoot.transform.rotation = rot * Quaternion.Euler(0, -15f, 0);

            // Timber skid plate
            CreatePanel(drillRoot.transform, "Skid", new Vector3(0, 0.04f, 0),
                new Vector3(0.35f, 0.06f, 0.70f), woodMat, new Vector2(0.4f, 0.6f));

            // Drill cylinder body
            CreateCylinder(drillRoot.transform, "DrillCylinder",
                new Vector3(0, 0.22f, 0),
                new Vector3(0.14f, 0.25f, 0.14f), metalMat, Quaternion.Euler(90f, 0, 0));

            // Drill steel rod / bit
            CreateCylinder(drillRoot.transform, "DrillSteel",
                new Vector3(0, 0.22f, 0.38f),
                new Vector3(0.04f, 0.35f, 0.04f), metalMat, Quaternion.Euler(90f, 0, 0));

            // T-handle at back
            CreatePanel(drillRoot.transform, "Handle",
                new Vector3(0, 0.22f, -0.28f),
                new Vector3(0.24f, 0.04f, 0.04f), metalMat, new Vector2(0.2f, 0.05f));

            // Coiled yellow air hose on skid
            CreateCylinder(drillRoot.transform, "HoseCoil",
                new Vector3(0.12f, 0.10f, -0.12f),
                new Vector3(0.18f, 0.06f, 0.18f), cautionMat);
        }

        private void BuildConduitPipes(Transform parent, Vector3 origin, Vector3 fwd, Vector3 right,
            float length, float width, float height, Quaternion rot)
        {
            Vector3 midPoint = origin + fwd * (length * 0.5f);
            float hw = width * 0.5f - 0.08f;

            // Upper left air pipe
            CreateCylinder(parent, "RouteA_AirPipe",
                midPoint - right * hw + new Vector3(0, height - 0.25f, 0),
                new Vector3(0.06f, length * 0.5f, 0.06f), pipeMat, rot * Quaternion.Euler(90f, 0, 0));

            // Upper right electrical cable bundle
            CreateCylinder(parent, "RouteA_CableLine",
                midPoint + right * hw + new Vector3(0, height - 0.22f, 0),
                new Vector3(0.04f, length * 0.5f, 0.04f), metalMat, rot * Quaternion.Euler(90f, 0, 0));
        }

        private void CreateBranchExitSign(Transform parent, Vector3 position, Quaternion rotation)
        {
            var go = new GameObject("ExitWallSign");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = rotation;

            CreatePanel(go.transform, "ExitPlate",
                Vector3.zero, new Vector3(0.65f, 0.30f, 0.03f),
                emergencyExitMat, new Vector2(1f, 1f));

            var textGO = new GameObject("ExitText");
            textGO.transform.SetParent(go.transform, false);
            textGO.transform.localPosition = new Vector3(0, 0, -0.025f);
            textGO.transform.localScale = Vector3.one * 0.012f;
            var tm = textGO.AddComponent<TextMesh>();
            tm.text = "EMERGENCY EXIT ➔";
            tm.fontSize = 36;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = Color.white;
        }

        private void CreateBranchCautionSign(Transform parent, Vector3 position, Quaternion rotation)
        {
            var go = new GameObject("CautionWallSign");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = rotation;

            CreatePanel(go.transform, "CautionPlate",
                Vector3.zero, new Vector3(0.65f, 0.30f, 0.03f),
                cautionMat, new Vector2(1f, 1f));

            var textGO = new GameObject("CautionText");
            textGO.transform.SetParent(go.transform, false);
            textGO.transform.localPosition = new Vector3(0, 0, -0.025f);
            textGO.transform.localScale = Vector3.one * 0.012f;
            var tm = textGO.AddComponent<TextMesh>();
            tm.text = "CAUTION: HAULAGE";
            tm.fontSize = 36;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = Color.black;
        }

        private void BuildRouteBBlockage(Transform parent, Vector3 endPos, Quaternion rot, float width)
        {
            var blockageRoot = new GameObject("RouteB_Blockage");
            blockageRoot.transform.SetParent(parent, false);

            // Caution Barricade Plaque
            var signGO = new GameObject("BarricadeSign");
            signGO.transform.SetParent(blockageRoot.transform, false);
            signGO.transform.position = endPos - rot * Vector3.forward * 0.8f + new Vector3(0, 1.2f, 0);
            signGO.transform.rotation = rot;

            CreatePanel(signGO.transform, "BarricadeBoard",
                Vector3.zero, new Vector3(width * 0.85f, 0.50f, 0.04f),
                cautionMat, new Vector2(1f, 1f));

            var textGO = new GameObject("BarricadeText");
            textGO.transform.SetParent(signGO.transform, false);
            textGO.transform.localPosition = new Vector3(0, 0, -0.025f);
            textGO.transform.localScale = Vector3.one * 0.011f;
            var tm = textGO.AddComponent<TextMesh>();
            tm.text = "NO ENTRY — HAULAGE DRIFT\nUNVENTILATED / BLOCKED";
            tm.fontSize = 32;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = Color.black;

            // Rockfall rubble cubes blocking the passage
            for (int i = 0; i < 6; i++)
            {
                float xOff = (i - 2.5f) * 0.35f;
                Vector3 rPos = endPos - rot * Vector3.forward * (0.3f + (i % 2) * 0.4f) + rot * Vector3.right * xOff + new Vector3(0, 0.35f + (i % 3) * 0.25f, 0);
                CreatePanel(blockageRoot.transform, $"Rubble_{i}",
                    rPos, new Vector3(0.55f, 0.50f, 0.55f),
                    rockMat, new Vector2(1f, 1f), rot * Quaternion.Euler(15f * i, 25f * i, 0));
            }
        }

        private void BuildExtendedEmergencyRoute(Transform parent, Vector3 startPos)
        {
            var extRoot = new GameObject("ExtendedEmergencyRoute");
            extRoot.transform.SetParent(parent, false);

            float startZ = startPos.z; // approx 46.9m
            float endZ = 75.0f;
            float tunnelLength = endZ - startZ; // approx 28.1m
            int numSegments = 7;
            float segLen = tunnelLength / numSegments; // approx 4.01m per segment

            float tunnelW = 2.30f;
            float tunnelH = 2.30f;

            float targetX = isRouteBEmergencyExit ? 8.50f : -8.50f;

            // 1. Build tunnel segments from Z=47m to 75m
            for (int i = 0; i < numSegments; i++)
            {
                float z0 = startZ + i * segLen;
                float z1 = startZ + (i + 1) * segLen;
                float midZ = (z0 + z1) * 0.5f;

                float t0 = (float)i / numSegments;
                float t1 = (float)(i + 1) / numSegments;

                float x0 = Mathf.Lerp(startPos.x, targetX, t0);
                float x1 = Mathf.Lerp(startPos.x, targetX, t1);
                float midX = (x0 + x1) * 0.5f;

                Vector3 p0 = new Vector3(x0, 0, z0);
                Vector3 p1 = new Vector3(x1, 0, z1);
                Vector3 dir = (p1 - p0).normalized;
                Quaternion segRot = Quaternion.LookRotation(dir, Vector3.up);

                // Floor
                CreatePanel(extRoot.transform, $"ExtTunnel_Floor_{i}",
                    new Vector3(midX, 0, midZ),
                    new Vector3(tunnelW, 0.05f, segLen + 0.1f),
                    floorMat, new Vector2(2f, 1.5f), segRot);

                // Ceiling
                CreatePanel(extRoot.transform, $"ExtTunnel_Ceiling_{i}",
                    new Vector3(midX, tunnelH, midZ),
                    new Vector3(tunnelW, 0.08f, segLen + 0.1f),
                    rockMat, new Vector2(2f, 1.5f), segRot);

                // Left Wall
                Vector3 leftPos = new Vector3(midX, tunnelH * 0.5f, midZ) - segRot * Vector3.right * (tunnelW * 0.5f);
                CreatePanel(extRoot.transform, $"ExtTunnel_WallL_{i}",
                    leftPos, new Vector3(0.08f, tunnelH, segLen + 0.1f),
                    rockMat, new Vector2(1.5f, 1.5f), segRot);

                // Right Wall
                Vector3 rightPos = new Vector3(midX, tunnelH * 0.5f, midZ) + segRot * Vector3.right * (tunnelW * 0.5f);
                CreatePanel(extRoot.transform, $"ExtTunnel_WallR_{i}",
                    rightPos, new Vector3(0.08f, tunnelH, segLen + 0.1f),
                    rockMat, new Vector2(1.5f, 1.5f), segRot);

                // Timber Arch Pit Prop at segment boundary
                if (i > 0)
                {
                    Vector3 archPos = new Vector3(x0, 0, z0);
                    BuildBranchPitProp(extRoot.transform, archPos, tunnelH, tunnelW, segRot);
                }

                // Green running-man emergency exit signs on outer wall
                if (i == 1 || i == 3 || i == 5)
                {
                    Vector3 signPos = isRouteBEmergencyExit
                        ? (rightPos + new Vector3(0, 0.35f, 0) - segRot * Vector3.right * 0.05f)
                        : (leftPos + new Vector3(0, 0.35f, 0) + segRot * Vector3.right * 0.05f);
                    Quaternion signRot = isRouteBEmergencyExit
                        ? (segRot * Quaternion.Euler(0, -90f, 0))
                        : (segRot * Quaternion.Euler(0, 90f, 0));
                    CreateBranchExitSign(extRoot.transform, signPos, signRot);
                }
            }

            // 2. Tunnel Lights & Daylight Penetration
            float lampX1 = isRouteBEmergencyExit ? 7.7f : -7.7f;
            float lampX2 = isRouteBEmergencyExit ? 8.2f : -8.2f;
            float glowFarX = isRouteBEmergencyExit ? 8.3f : -8.3f;
            float glowMidX = targetX;
            float flareX = targetX;

            // Dim electric emergency bulkhead lights in early tunnel
            CreatePointLight(extRoot.transform, new Vector3(lampX1, 2.1f, 52.0f),
                new Color(0.85f, 1.0f, 0.85f), 7f, 5f, "ExtTunnel_Lamp1");
            CreatePointLight(extRoot.transform, new Vector3(lampX2, 2.1f, 60.0f),
                new Color(0.85f, 1.0f, 0.85f), 7f, 5f, "ExtTunnel_Lamp2");

            // Penetrating Daylight: increases in intensity & range towards portal
            // 58m: subtle cool daylight glow
            CreatePointLight(extRoot.transform, new Vector3(glowFarX, 1.8f, 58.0f),
                new Color(0.70f, 0.85f, 1.0f), 8f, 2.0f, "Daylight_GlowFar");
            // 66m: stronger natural daylight
            CreatePointLight(extRoot.transform, new Vector3(glowMidX, 1.8f, 66.0f),
                new Color(0.85f, 0.92f, 1.0f), 12f, 4.5f, "Daylight_GlowMid");
            // 73m: bright outdoor portal flare
            CreatePointLight(extRoot.transform, new Vector3(flareX, 2.0f, 73.5f),
                new Color(1.0f, 0.98f, 0.90f), 16f, 8.0f, "Daylight_PortalFlare");

            // 3. 3D Mine Portal Mouth (Z = 75.0m)
            BuildMinePortal(extRoot.transform, new Vector3(targetX, 0, 75.0f));

            // 4. Outdoor Open Ground Area (Z = 75m to 120m)
            BuildOutdoorEnvironment(extRoot.transform, new Vector3(targetX, 0, 75.0f));

            // 5. Emergency Assembly Point (Z = 94.0m) with 3D Worker Models
            BuildAssemblyArea(extRoot.transform, new Vector3(targetX, 0, 94.0f));
        }

        private void BuildMinePortal(Transform parent, Vector3 center)
        {
            var portalRoot = new GameObject("MineExitPortal");
            portalRoot.transform.SetParent(parent, false);

            float pZ = center.z; // 75.0m
            float cX = center.x; // -8.50m
            float archW = 3.60f;
            float archH = 2.90f;
            float depth = 1.20f;

            // Left massive stone portal pillar
            CreatePanel(portalRoot.transform, "PortalPillar_L",
                new Vector3(cX - archW * 0.5f - 0.35f, archH * 0.5f, pZ),
                new Vector3(0.70f, archH, depth),
                rockMat, new Vector2(1f, 2f));

            // Right massive stone portal pillar
            CreatePanel(portalRoot.transform, "PortalPillar_R",
                new Vector3(cX + archW * 0.5f + 0.35f, archH * 0.5f, pZ),
                new Vector3(0.70f, archH, depth),
                rockMat, new Vector2(1f, 2f));

            // Overhead lintel beam
            CreatePanel(portalRoot.transform, "PortalLintel",
                new Vector3(cX, archH + 0.35f, pZ),
                new Vector3(archW + 1.40f, 0.70f, depth),
                rockMat, new Vector2(3f, 1f));

            // Headwall rock mountain face above portal
            CreatePanel(portalRoot.transform, "PortalHeadwall",
                new Vector3(cX, archH + 3.2f, pZ + 0.2f),
                new Vector3(archW + 16.0f, 5.0f, 0.6f),
                rockMat, new Vector2(8f, 3f));

            // Flanking rock wing retaining walls (angling outwards to open ground)
            CreatePanel(portalRoot.transform, "RetainingWall_L",
                new Vector3(cX - 4.5f, 2.0f, pZ + 2.0f),
                new Vector3(0.5f, 4.0f, 5.0f),
                rockMat, new Vector2(3f, 2f),
                Quaternion.Euler(0, -35.0f, 0));

            CreatePanel(portalRoot.transform, "RetainingWall_R",
                new Vector3(cX + 4.5f, 2.0f, pZ + 2.0f),
                new Vector3(0.5f, 4.0f, 5.0f),
                rockMat, new Vector2(3f, 2f),
                Quaternion.Euler(0, 35.0f, 0));

            // ── Portal Sign: "MAIN ADIT PORTAL NO. 4 — EMERGENCY EXIT" ──
            var signGO = new GameObject("PortalSign");
            signGO.transform.SetParent(portalRoot.transform, false);
            signGO.transform.position = new Vector3(cX, archH + 0.35f, pZ - depth * 0.5f - 0.05f);

            CreatePanel(signGO.transform, "PortalSignBack",
                Vector3.zero, new Vector3(archW * 0.85f, 0.50f, 0.03f),
                signBackMat, new Vector2(1f, 1f));

            CreatePanel(signGO.transform, "PortalSignPlate",
                new Vector3(0, 0, -0.015f), new Vector3(archW * 0.82f, 0.44f, 0.02f),
                emergencyExitMat, new Vector2(1f, 1f));

            var textGO = new GameObject("PortalSignText");
            textGO.transform.SetParent(signGO.transform, false);
            textGO.transform.localPosition = new Vector3(0, 0, -0.035f);
            textGO.transform.localScale = Vector3.one * 0.013f;
            var tm = textGO.AddComponent<TextMesh>();
            tm.text = "MAIN ADIT PORTAL NO. 4\nEMERGENCY EXIT ➔";
            tm.fontSize = 32;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = Color.white;
        }

        private void BuildOutdoorEnvironment(Transform parent, Vector3 portalCenter)
        {
            var outdoorRoot = new GameObject("OutdoorEnvironment");
            outdoorRoot.transform.SetParent(parent, false);

            float cX = portalCenter.x; // -8.50m
            float pZ = portalCenter.z; // 75.0m

            // 1. Vast Outdoor Terrain Ground Plane (Z = 74m to 125m, X = -40m to +25m)
            CreatePanel(outdoorRoot.transform, "OutdoorGround",
                new Vector3(cX, -0.02f, pZ + 25.0f),
                new Vector3(65.0f, 0.06f, 52.0f),
                outdoorFloorMat, new Vector2(16f, 16f));

            // Gravel Path leading from Portal to Assembly Point
            CreatePanel(outdoorRoot.transform, "GravelPathway",
                new Vector3(cX, 0.01f, pZ + 10.0f),
                new Vector3(5.0f, 0.03f, 20.0f),
                floorMat, new Vector2(2f, 8f));

            // 2. Open Sky Dome / Sky Canopy (Y = 25m)
            CreatePanel(outdoorRoot.transform, "SkyCanopy",
                new Vector3(cX, 24.0f, pZ + 25.0f),
                new Vector3(80.0f, 0.10f, 70.0f),
                skyMat, new Vector2(1f, 1f));

            // 3. Directional Sunlight illuminating outdoor world
            var sunGO = new GameObject("Outdoor_DirectionalSun");
            sunGO.transform.SetParent(outdoorRoot.transform, false);
            var sun = sunGO.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1.0f, 0.96f, 0.88f);
            sun.intensity = 1.35f;
            sunGO.transform.rotation = Quaternion.Euler(42f, 30f, 0f);

            // 4. Rocky Mountains / Hills Flanking the Outdoor Quarry Grounds
            // Left mountain ridges
            for (int i = 0; i < 4; i++)
            {
                float mz = pZ + 6.0f + i * 11.0f;
                float mx = cX - 16.0f - (i % 2) * 3.0f;
                float mh = 7.0f + (i % 3) * 2.5f;
                CreatePanel(outdoorRoot.transform, $"MountainRidge_L_{i}",
                    new Vector3(mx, mh * 0.5f, mz),
                    new Vector3(9.0f, mh, 12.0f),
                    rockMat, new Vector2(4f, 3f),
                    Quaternion.Euler(5f, -15f * i, 0));
            }

            // Right mountain ridges
            for (int i = 0; i < 4; i++)
            {
                float mz = pZ + 6.0f + i * 11.0f;
                float mx = cX + 16.0f + (i % 2) * 3.0f;
                float mh = 6.5f + ((i + 1) % 3) * 2.5f;
                CreatePanel(outdoorRoot.transform, $"MountainRidge_R_{i}",
                    new Vector3(mx, mh * 0.5f, mz),
                    new Vector3(9.0f, mh, 12.0f),
                    rockMat, new Vector2(4f, 3f),
                    Quaternion.Euler(5f, 15f * i, 0));
            }

            // Back distant mountain ridge (Z = 122m)
            CreatePanel(outdoorRoot.transform, "MountainBackdrop",
                new Vector3(cX, 6.0f, pZ + 46.0f),
                new Vector3(65.0f, 12.0f, 8.0f),
                rockMat, new Vector2(10f, 4f));

            // 5. Industrial Mine Surface Structures
            // Mine Surface Exhaust Fan House (near left mountain cut)
            var fanHouse = new GameObject("SurfaceExhaustFanTower");
            fanHouse.transform.SetParent(outdoorRoot.transform, false);
            fanHouse.transform.position = new Vector3(cX - 9.5f, 0, pZ + 6.0f);
            CreatePanel(fanHouse.transform, "FanBuilding",
                new Vector3(0, 2.0f, 0), new Vector3(4.0f, 4.0f, 4.5f),
                metalMat, new Vector2(2f, 2f));
            CreateCylinder(fanHouse.transform, "FanDuct",
                new Vector3(0, 4.5f, 0), new Vector3(1.2f, 1.0f, 1.2f),
                metalMat);

            // Substation Enclosure on Right
            var subStation = new GameObject("SubstationEnclosure");
            subStation.transform.SetParent(outdoorRoot.transform, false);
            subStation.transform.position = new Vector3(cX + 8.5f, 0, pZ + 8.0f);
            CreatePanel(subStation.transform, "Transformer1",
                new Vector3(-0.8f, 1.0f, 0), new Vector3(1.4f, 2.0f, 1.4f),
                metalMat, new Vector2(1f, 1f));
            CreatePanel(subStation.transform, "Transformer2",
                new Vector3(+0.8f, 1.0f, 0), new Vector3(1.4f, 2.0f, 1.4f),
                metalMat, new Vector2(1f, 1f));

            // Safety Equipment Shipping Container
            var container = new GameObject("SafetyStorageContainer");
            container.transform.SetParent(outdoorRoot.transform, false);
            container.transform.position = new Vector3(cX - 12.0f, 0, pZ + 20.0f);
            container.transform.rotation = Quaternion.Euler(0, 20f, 0);
            CreatePanel(container.transform, "ContainerBody",
                new Vector3(0, 1.35f, 0), new Vector3(2.5f, 2.7f, 6.0f),
                metalMat, new Vector2(2f, 3f));
        }

        private void BuildAssemblyArea(Transform parent, Vector3 center)
        {
            var areaRoot = new GameObject("EmergencyAssemblyArea");
            areaRoot.transform.SetParent(parent, false);

            float cX = center.x; // -8.50m
            float cZ = center.z; // 94.0m
            float padW = 10.0f;
            float padL = 8.0f;

            // 1. Concrete Muster Pad
            CreatePanel(areaRoot.transform, "MusterPad_Concrete",
                new Vector3(cX, 0.02f, cZ),
                new Vector3(padW, 0.04f, padL),
                concreteMat, new Vector2(4f, 3f));

            // 2. Yellow & Black Hazard Border Panels around perimeter
            float bThick = 0.35f;
            // North border
            CreatePanel(areaRoot.transform, "HazardBorder_N",
                new Vector3(cX, 0.045f, cZ + padL * 0.5f - bThick * 0.5f),
                new Vector3(padW, 0.02f, bThick),
                hazardYellowMat, new Vector2(10f, 1f));
            // South border
            CreatePanel(areaRoot.transform, "HazardBorder_S",
                new Vector3(cX, 0.045f, cZ - padL * 0.5f + bThick * 0.5f),
                new Vector3(padW, 0.02f, bThick),
                hazardYellowMat, new Vector2(10f, 1f));
            // West border
            CreatePanel(areaRoot.transform, "HazardBorder_W",
                new Vector3(cX - padW * 0.5f + bThick * 0.5f, 0.045f, cZ),
                new Vector3(bThick, 0.02f, padL),
                hazardYellowMat, new Vector2(1f, 8f));
            // East border
            CreatePanel(areaRoot.transform, "HazardBorder_E",
                new Vector3(cX + padW * 0.5f - bThick * 0.5f, 0.045f, cZ),
                new Vector3(bThick, 0.02f, padL),
                hazardYellowMat, new Vector2(1f, 8f));

            // Central Green ISO Muster Symbol Marking
            CreatePanel(areaRoot.transform, "MusterCenterSquare",
                new Vector3(cX, 0.045f, cZ),
                new Vector3(2.6f, 0.02f, 2.6f),
                emergencyExitMat, new Vector2(1f, 1f));

            // 3. Illuminated Assembly Point Sign on Tall Mast
            var mastGO = new GameObject("AssemblyPointMast");
            mastGO.transform.SetParent(areaRoot.transform, false);
            mastGO.transform.position = new Vector3(cX, 0, cZ + padL * 0.5f - 0.5f);

            // Steel mast pole (height 3.8m)
            CreateCylinder(mastGO.transform, "MastPole",
                new Vector3(0, 1.9f, 0), new Vector3(0.12f, 1.9f, 0.12f), metalMat);

            // Large green illuminated assembly point sign
            var signBox = new GameObject("SignBox");
            signBox.transform.SetParent(mastGO.transform, false);
            signBox.transform.position = new Vector3(0, 3.4f, 0);

            CreatePanel(signBox.transform, "SignPlate",
                Vector3.zero, new Vector3(2.0f, 1.10f, 0.08f),
                emergencyExitMat, new Vector2(1f, 1f));

            // Sign Front Text (facing approaching workers from portal, towards south)
            var textFGO = new GameObject("SignText_Front");
            textFGO.transform.SetParent(signBox.transform, false);
            textFGO.transform.localPosition = new Vector3(0, 0, -0.05f);
            textFGO.transform.localScale = Vector3.one * 0.013f;
            var tmF = textFGO.AddComponent<TextMesh>();
            tmF.text = "EMERGENCY\nASSEMBLY POINT\n# 1";
            tmF.fontSize = 36;
            tmF.alignment = TextAlignment.Center;
            tmF.anchor = TextAnchor.MiddleCenter;
            tmF.color = Color.white;

            // Green beacon atop the mast
            CreateCylinder(mastGO.transform, "BeaconHousing",
                new Vector3(0, 3.95f, 0), new Vector3(0.22f, 0.12f, 0.22f), emergencyExitMat);
            CreatePointLight(mastGO.transform, new Vector3(0, 4.10f, 0),
                new Color(0.15f, 1.0f, 0.35f), 12f, 6.0f, "AssemblyBeaconLight");

            // 4. Safety Perimeter Barricades at Corners
            CreateSafetyBarricade(areaRoot.transform, new Vector3(cX - padW * 0.5f + 0.3f, 0, cZ - padL * 0.5f + 0.3f), Quaternion.Euler(0, 45f, 0));
            CreateSafetyBarricade(areaRoot.transform, new Vector3(cX + padW * 0.5f - 0.3f, 0, cZ - padL * 0.5f + 0.3f), Quaternion.Euler(0, -45f, 0));
            CreateSafetyBarricade(areaRoot.transform, new Vector3(cX - padW * 0.5f + 0.3f, 0, cZ + padL * 0.5f - 0.3f), Quaternion.Euler(0, 135f, 0));
            CreateSafetyBarricade(areaRoot.transform, new Vector3(cX + padW * 0.5f - 0.3f, 0, cZ + padL * 0.5f - 0.3f), Quaternion.Euler(0, -135f, 0));

            // 5. Procedural 3D Character Models (3 Miners + 1 Safety Officer)
            // Safety Officer at front facing the mine portal
            BuildCharacterModel(areaRoot.transform, "SafetyOfficer",
                new Vector3(cX, 0, cZ - 2.0f),
                Quaternion.Euler(0, 180f, 0),
                vestYellowMat, hardhatWhiteMat, isSafetyOfficer: true);

            // Miner 1 (standing on left side)
            BuildCharacterModel(areaRoot.transform, "Miner_1",
                new Vector3(cX - 2.2f, 0, cZ - 0.5f),
                Quaternion.Euler(0, 160f, 0),
                vestOrangeMat, hardhatYellowMat, isSafetyOfficer: false);

            // Miner 2 (standing on right side)
            BuildCharacterModel(areaRoot.transform, "Miner_2",
                new Vector3(cX + 2.4f, 0, cZ + 0.2f),
                Quaternion.Euler(0, -155f, 0),
                vestOrangeMat, hardhatWhiteMat, isSafetyOfficer: false);

            // Miner 3 (standing near back)
            BuildCharacterModel(areaRoot.transform, "Miner_3",
                new Vector3(cX - 0.8f, 0, cZ + 1.8f),
                Quaternion.Euler(0, 175f, 0),
                vestYellowMat, hardhatYellowMat, isSafetyOfficer: false);
        }

        private void CreateSafetyBarricade(Transform parent, Vector3 position, Quaternion rotation)
        {
            var barGO = new GameObject("SafetyBarricade");
            barGO.transform.SetParent(parent, false);
            barGO.transform.position = position;
            barGO.transform.rotation = rotation;

            // Two uprights
            CreateCylinder(barGO.transform, "Post1", new Vector3(-0.6f, 0.45f, 0), new Vector3(0.06f, 0.45f, 0.06f), metalMat);
            CreateCylinder(barGO.transform, "Post2", new Vector3(+0.6f, 0.45f, 0), new Vector3(0.06f, 0.45f, 0.06f), metalMat);

            // Cross board with hazard stripes
            CreatePanel(barGO.transform, "CrossBoard",
                new Vector3(0, 0.65f, 0), new Vector3(1.35f, 0.25f, 0.03f),
                hazardYellowMat, new Vector2(4f, 1f));
        }

        private void BuildCharacterModel(Transform parent, string name, Vector3 position,
            Quaternion rotation, Material vestMat, Material hardhatMat, bool isSafetyOfficer)
        {
            var charRoot = new GameObject(name);
            charRoot.transform.SetParent(parent, false);
            charRoot.transform.position = position;
            charRoot.transform.rotation = rotation;

            // Legs (Navy blue trousers)
            CreateCylinder(charRoot.transform, "Leg_L",
                new Vector3(-0.13f, 0.44f, 0), new Vector3(0.10f, 0.44f, 0.10f), darkTrouserMat);
            CreateCylinder(charRoot.transform, "Leg_R",
                new Vector3(+0.13f, 0.44f, 0), new Vector3(0.10f, 0.44f, 0.10f), darkTrouserMat);

            // Boots
            CreatePanel(charRoot.transform, "Boot_L",
                new Vector3(-0.13f, 0.05f, 0.04f), new Vector3(0.12f, 0.10f, 0.22f), hazardBlackMat, new Vector2(1f, 1f));
            CreatePanel(charRoot.transform, "Boot_R",
                new Vector3(+0.13f, 0.05f, 0.04f), new Vector3(0.12f, 0.10f, 0.22f), hazardBlackMat, new Vector2(1f, 1f));

            // Torso (Hi-vis vest over shirt)
            CreatePanel(charRoot.transform, "Torso",
                new Vector3(0, 1.15f, 0), new Vector3(0.48f, 0.58f, 0.26f),
                vestMat, new Vector2(1f, 1f));

            // Silver reflective safety stripes on vest
            CreatePanel(charRoot.transform, "Stripe_Upper",
                new Vector3(0, 1.28f, 0.135f), new Vector3(0.44f, 0.05f, 0.01f),
                silverReflectiveMat, new Vector2(1f, 1f));
            CreatePanel(charRoot.transform, "Stripe_Lower",
                new Vector3(0, 1.02f, 0.135f), new Vector3(0.44f, 0.05f, 0.01f),
                silverReflectiveMat, new Vector2(1f, 1f));

            // Arms
            CreateCylinder(charRoot.transform, "Arm_L",
                new Vector3(-0.30f, 1.12f, 0), new Vector3(0.07f, 0.26f, 0.07f), darkTrouserMat);
            CreateCylinder(charRoot.transform, "Arm_R",
                new Vector3(+0.30f, 1.12f, 0), new Vector3(0.07f, 0.26f, 0.07f), darkTrouserMat);

            // Head (natural skin tone)
            CreateCylinder(charRoot.transform, "Head",
                new Vector3(0, 1.58f, 0), new Vector3(0.11f, 0.13f, 0.11f), skinMat);

            // Hardhat (Dome + Brim)
            CreatePanel(charRoot.transform, "HardhatBrim",
                new Vector3(0, 1.68f, 0.03f), new Vector3(0.32f, 0.03f, 0.36f),
                hardhatMat, new Vector2(1f, 1f));
            CreateCylinder(charRoot.transform, "HardhatDome",
                new Vector3(0, 1.74f, 0), new Vector3(0.14f, 0.07f, 0.14f), hardhatMat);

            // Cap Lamp on hardhat front
            CreatePanel(charRoot.transform, "CapLamp",
                new Vector3(0, 1.72f, 0.16f), new Vector3(0.06f, 0.06f, 0.04f),
                silverReflectiveMat, new Vector2(1f, 1f));

            // Safety Officer Specific: Clipboard and "SAFETY" badge
            if (isSafetyOfficer)
            {
                var clipGO = new GameObject("Clipboard");
                clipGO.transform.SetParent(charRoot.transform, false);
                clipGO.transform.localPosition = new Vector3(0.08f, 1.18f, 0.22f);
                clipGO.transform.localRotation = Quaternion.Euler(35f, -10f, 0);

                // Wooden backing board
                CreatePanel(clipGO.transform, "Board",
                    Vector3.zero, new Vector3(0.24f, 0.32f, 0.015f), woodMat, new Vector2(1f, 1f));

                // White paper sheet
                CreatePanel(clipGO.transform, "Paper",
                    new Vector3(0, 0, -0.01f), new Vector3(0.21f, 0.28f, 0.01f), hardhatWhiteMat, new Vector2(1f, 1f));

                // Safety Badge on chest
                var badgeGO = new GameObject("SafetyBadge");
                badgeGO.transform.SetParent(charRoot.transform, false);
                badgeGO.transform.localPosition = new Vector3(-0.12f, 1.34f, 0.14f);
                badgeGO.transform.localScale = Vector3.one * 0.007f;
                var tm = badgeGO.AddComponent<TextMesh>();
                tm.text = "SAFETY";
                tm.fontSize = 28;
                tm.alignment = TextAlignment.Center;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.color = Color.white;
            }
        }

        private Material CreateSolidMaterial(Color color)
        {
            var mat = new Material(Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse"));
            mat.color = color;
            return mat;
        }

        /// <summary>Returns the world-space starting position for the virtual camera.</summary>
        public Vector3 GetStartPosition() => mineRoot.TransformPoint(new Vector3(0, 1.65f, -14.0f));

        /// <summary>Returns world-space end marker position.</summary>
        public Vector3 GetEndMarkerPosition() => mineRoot.TransformPoint(new Vector3(0, 1.6f, 20f));

        // =====================================================================
        // OUTDOOR SURFACE  (Z = -15m to 0m)
        // =====================================================================

        private void BuildOutdoorSurface()
        {
            var outdoorRoot = new GameObject("OutdoorSurface");
            outdoorRoot.transform.SetParent(mineRoot, false);

            // ── Ground terrain — player starts at Z=-14, facing +Z toward mine ──
            // Expansive base bedrock ground (Z = -55m to +10m, X = -65m to +65m)
            CreatePanel(outdoorRoot.transform, "OutdoorGround_Base",
                new Vector3(0, -0.22f, -22f), new Vector3(115f, 0.40f, 75f),
                outdoorRockMat, new Vector2(16f, 16f));

            // Central Working Yard (Z = -22m to 0m, X = -24m to +24m)
            CreatePanel(outdoorRoot.transform, "OutdoorGround_WorkingYard",
                new Vector3(0, -0.05f, -11f), new Vector3(46f, 0.15f, 24f),
                outdoorDirtMat, new Vector2(10f, 6f));

            // Gravel apron leading directly into mine entrance (Z = -4.5m to 0m)
            CreatePanel(outdoorRoot.transform, "MineApron_Gravel",
                new Vector3(0, 0.02f, -2.2f), new Vector3(10f, 0.05f, 4.5f),
                outdoorGravelMat, new Vector2(3f, 1.5f));

            // ── Sky Dome — large sphere centred on player start position ──────────
            // Must render from INSIDE — skyDomeMat has Cull Off set in BuildMaterials()
            var skyGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            skyGO.name = "SkyDome";
            skyGO.transform.SetParent(outdoorRoot.transform, false);
            skyGO.transform.localPosition = new Vector3(0, 0f, -18f);
            skyGO.transform.localScale = new Vector3(150f, 70f, 140f);
            var skyRend = skyGO.GetComponent<Renderer>();
            skyRend.material = skyDomeMat;
            skyRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            skyRend.receiveShadows = false;
            Destroy(skyGO.GetComponent<Collider>());

            // ── Directional Sunlight ──────────────────────────────────────────────
            var sunGO = new GameObject("Outdoor_DirectionalSun");
            sunGO.transform.SetParent(outdoorRoot.transform, false);
            var sunLight = sunGO.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.color = new Color(1.0f, 0.95f, 0.84f);
            sunLight.intensity = 1.25f;
            sunGO.transform.localRotation = Quaternion.Euler(42f, 32f, 0f);
            _dirSunLight = sunLight;

            // ── Sky fill light — simulates sky bounce/ambient from above ──────────
            var fillGO = new GameObject("OutdoorSkyFill");
            fillGO.transform.SetParent(outdoorRoot.transform, false);
            fillGO.transform.localPosition = new Vector3(0, 14f, -14f);
            var fillLight = fillGO.AddComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.color = new Color(0.52f, 0.62f, 0.80f);
            fillLight.intensity = 0.70f;
            fillLight.range = 65f;

            // ── FIX 3: 3D Mountain Amphitheater & Mining Quarry Terrain ────────────
            BuildSurroundingMountains(outdoorRoot.transform);

            // Mine Cliff Face framing entrance portal
            BuildMineCliffFace(outdoorRoot.transform);

            // Scattered surface boulders
            BuildScatteredOutdoorRocks(outdoorRoot.transform);

            // Mining surface props near entrance
            BuildOutdoorProps(outdoorRoot.transform);
        }

        /// <summary>
        /// FIX 3: Natural 3D mountain amphitheater and quarry terrain surrounding the starting area.
        /// Mountains encircle the site at a natural distance, leaving the central mining yard wide open.
        /// </summary>
        private void BuildSurroundingMountains(Transform parent)
        {
            var mountainRoot = new GameObject("SurroundingMountains_Amphitheater");
            mountainRoot.transform.SetParent(parent, false);

            // ── 1. Distant Mountain Formations (Ring behind and around starting area) ──
            // Center High Backdrop Ridge (Directly behind player at Z=-48m)
            BuildMountainPeak(mountainRoot.transform, "Mountain_CenterHigh",
                new Vector3(0, 15.0f, -48f), new Vector3(34f, 26f, 22f), new Vector3(18f, 13f, 14f),
                new Vector3(4f, 15f, 0));

            // Rear Left Peak
            BuildMountainPeak(mountainRoot.transform, "Mountain_RearL",
                new Vector3(-18f, 12.5f, -42f), new Vector3(26f, 22f, 18f), new Vector3(14f, 10f, 12f),
                new Vector3(6f, -22f, 3f));

            // Rear Right Peak
            BuildMountainPeak(mountainRoot.transform, "Mountain_RearR",
                new Vector3(+18f, 12.5f, -42f), new Vector3(26f, 22f, 18f), new Vector3(14f, 10f, 12f),
                new Vector3(6f, 22f, -3f));

            // Far Left Flank Mountain
            BuildMountainPeak(mountainRoot.transform, "Mountain_FlankFarL",
                new Vector3(-35f, 10.5f, -34f), new Vector3(24f, 19f, 20f), new Vector3(12f, 9f, 10f),
                new Vector3(8f, -45f, 4f));

            // Far Right Flank Mountain
            BuildMountainPeak(mountainRoot.transform, "Mountain_FlankFarR",
                new Vector3(+35f, 10.5f, -34f), new Vector3(24f, 19f, 20f), new Vector3(12f, 9f, 10f),
                new Vector3(8f, 45f, -4f));

            // Mid Left Flank Mountain
            BuildMountainPeak(mountainRoot.transform, "Mountain_MidFlankL",
                new Vector3(-28f, 8.5f, -20f), new Vector3(20f, 16f, 18f), new Vector3(10f, 8f, 10f),
                new Vector3(6f, -70f, 2f));

            // Mid Right Flank Mountain
            BuildMountainPeak(mountainRoot.transform, "Mountain_MidFlankR",
                new Vector3(+28f, 8.5f, -20f), new Vector3(20f, 16f, 18f), new Vector3(10f, 8f, 10f),
                new Vector3(6f, 70f, -2f));

            // ── 2. Terraced Quarry Benches (Stepped rock cuts along the side perimeter) ──
            BuildQuarryBenches(mountainRoot.transform);

            // ── 3. Distant Industrial Mining Structures (Backdrop, non-blocking) ──
            BuildMiningBackdropStructures(mountainRoot.transform);

            // ── 4. Natural Arid Scrub Vegetation ──
            BuildScrubVegetation(mountainRoot.transform);
        }

        private void BuildMountainPeak(Transform parent, string name, Vector3 pos, Vector3 baseScale, Vector3 capScale, Vector3 rot)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;

            // Massive Base Mountain Rock Block
            CreatePanel(root.transform, "BaseMass",
                Vector3.zero, baseScale, outdoorRockMat, new Vector2(5f, 4f), Quaternion.Euler(rot));

            // Jagged Summit Crag Peak
            CreatePanel(root.transform, "SummitCrag",
                new Vector3(rot.y > 0 ? -1.5f : 1.5f, baseScale.y * 0.45f, 0.5f),
                capScale, outdoorRockMat, new Vector2(3f, 3f),
                Quaternion.Euler(rot.x + 8f, rot.y * 1.3f, rot.z + 6f));

            // Sloping Talus Scree Base Apron
            CreatePanel(root.transform, "TalusApron",
                new Vector3(0, -baseScale.y * 0.35f, 4f),
                new Vector3(baseScale.x * 0.9f, baseScale.y * 0.35f, baseScale.z * 0.8f),
                outdoorRockMat, new Vector2(3f, 2f),
                Quaternion.Euler(rot.x + 18f, rot.y * 0.5f, 0));
        }

        private void BuildQuarryBenches(Transform parent)
        {
            var quarryRoot = new GameObject("QuarryBenches");
            quarryRoot.transform.SetParent(parent, false);

            // Left Quarry Excavation Steps (X ≈ -18m to -24m)
            CreatePanel(quarryRoot.transform, "QuarryL_Bench1",
                new Vector3(-19f, 1.8f, -10f), new Vector3(7f, 3.6f, 22f),
                minePortalRockMat, new Vector2(3f, 5f), Quaternion.Euler(0, 10f, 0));
            CreatePanel(quarryRoot.transform, "QuarryL_Bench2",
                new Vector3(-23f, 4.2f, -12f), new Vector3(6f, 5.0f, 20f),
                outdoorRockMat, new Vector2(2.5f, 4f), Quaternion.Euler(0, 12f, 0));

            // Right Quarry Excavation Steps (X ≈ +18m to +24m)
            CreatePanel(quarryRoot.transform, "QuarryR_Bench1",
                new Vector3(+19f, 1.8f, -10f), new Vector3(7f, 3.6f, 22f),
                minePortalRockMat, new Vector2(3f, 5f), Quaternion.Euler(0, -10f, 0));
            CreatePanel(quarryRoot.transform, "QuarryR_Bench2",
                new Vector3(+23f, 4.2f, -12f), new Vector3(6f, 5.0f, 20f),
                outdoorRockMat, new Vector2(2.5f, 4f), Quaternion.Euler(0, -12f, 0));
        }

        private void BuildMiningBackdropStructures(Transform parent)
        {
            var indRoot = new GameObject("MiningIndustrialBackdrop");
            indRoot.transform.SetParent(parent, false);

            // Structure 1: Heavy Timber Conveyor Trestle & Ore Loading Chute (Left backdrop, X = -16.5m, Z = -18.5m)
            var trestleGO = new GameObject("OreConveyorTrestle");
            trestleGO.transform.SetParent(indRoot.transform, false);
            trestleGO.transform.localPosition = new Vector3(-16.5f, 0, -18.5f);
            trestleGO.transform.localRotation = Quaternion.Euler(0, 35f, 0);

            CreatePanel(trestleGO.transform, "Post1", new Vector3(-1.2f, 2.5f, -1.2f), new Vector3(0.24f, 5.0f, 0.24f), woodMat, new Vector2(0.3f, 2f));
            CreatePanel(trestleGO.transform, "Post2", new Vector3(+1.2f, 2.5f, -1.2f), new Vector3(0.24f, 5.0f, 0.24f), woodMat, new Vector2(0.3f, 2f));
            CreatePanel(trestleGO.transform, "Post3", new Vector3(-1.2f, 2.0f, +1.2f), new Vector3(0.24f, 4.0f, 0.24f), woodMat, new Vector2(0.3f, 1.8f));
            CreatePanel(trestleGO.transform, "Post4", new Vector3(+1.2f, 2.0f, +1.2f), new Vector3(0.24f, 4.0f, 0.24f), woodMat, new Vector2(0.3f, 1.8f));

            CreatePanel(trestleGO.transform, "HopperBin", new Vector3(0, 4.5f, 0), new Vector3(3.2f, 1.8f, 3.2f), metalMat, new Vector2(1f, 1f));
            CreatePanel(trestleGO.transform, "DischargeChute", new Vector3(0, 2.8f, 1.8f), new Vector3(1.4f, 0.20f, 2.6f),
                metalMat, new Vector2(0.5f, 1f), Quaternion.Euler(28f, 0, 0));

            // Structure 2: Mine Equipment Workshop Container Shack (Right backdrop, X = +18m, Z = -17m)
            var shackGO = new GameObject("GeneratorContainerShack");
            shackGO.transform.SetParent(indRoot.transform, false);
            shackGO.transform.localPosition = new Vector3(+18.0f, 0, -17.0f);
            shackGO.transform.localRotation = Quaternion.Euler(0, -25f, 0);

            CreatePanel(shackGO.transform, "ContainerBody", new Vector3(0, 1.4f, 0), new Vector3(2.6f, 2.8f, 5.5f),
                CreateSolidMaterial(new Color(0.32f, 0.35f, 0.38f)), new Vector2(1f, 2f));
            CreateCylinder(shackGO.transform, "ExhaustStack", new Vector3(0.6f, 3.4f, -1.2f), new Vector3(0.18f, 1.2f, 0.18f), metalMat);

            // Structure 3: High-Mast Industrial Floodlight Tower (Right backdrop, X = +15m, Z = -12.5m)
            var mastGO = new GameObject("IndustrialFloodlightTower");
            mastGO.transform.SetParent(indRoot.transform, false);
            mastGO.transform.localPosition = new Vector3(+15.0f, 0, -12.5f);

            CreatePanel(mastGO.transform, "MastColumn", new Vector3(0, 3.5f, 0), new Vector3(0.35f, 7.0f, 0.35f), metalMat, new Vector2(0.2f, 3f));
            CreatePanel(mastGO.transform, "TowerHeadPlatform", new Vector3(0, 7.0f, 0), new Vector3(1.6f, 0.15f, 1.2f), metalMat, new Vector2(0.5f, 0.5f));

            for (int j = 0; j < 4; j++)
            {
                float ox = (j % 2 == 0 ? -0.5f : 0.5f);
                float oz = (j < 2 ? -0.3f : 0.3f);
                CreatePanel(mastGO.transform, $"Floodlamp_{j}", new Vector3(ox, 7.3f, oz), new Vector3(0.35f, 0.25f, 0.35f),
                    cautionMat, new Vector2(0.2f, 0.2f), Quaternion.Euler(25f, -30f, 0));
            }
        }

        private void BuildScrubVegetation(Transform parent)
        {
            var vegRoot = new GameObject("AridScrubVegetation");
            vegRoot.transform.SetParent(parent, false);

            Vector3[] bushPositions = new Vector3[]
            {
                new Vector3(-8.5f,  0.25f, -5.5f),
                new Vector3(-12.0f, 0.30f, -8.0f),
                new Vector3(-15.5f, 0.35f, -14.0f),
                new Vector3(-7.2f,  0.22f, -16.0f),
                new Vector3(-11.0f, 0.28f, -20.0f),
                new Vector3( 9.5f,  0.25f, -6.5f),
                new Vector3( 13.0f, 0.32f, -10.0f),
                new Vector3( 11.5f, 0.26f, -15.5f),
                new Vector3(  7.5f, 0.20f, -18.0f),
                new Vector3( 14.5f, 0.35f, -22.0f)
            };

            for (int i = 0; i < bushPositions.Length; i++)
            {
                var bushGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bushGO.name = $"ScrubBush_{i}";
                bushGO.transform.SetParent(vegRoot.transform, false);
                bushGO.transform.localPosition = bushPositions[i];
                float s = 0.55f + (i % 3) * 0.22f;
                bushGO.transform.localScale = new Vector3(s * 1.2f, s * 0.65f, s * 1.1f);
                bushGO.GetComponent<Renderer>().material = foliageMat;
                Destroy(bushGO.GetComponent<Collider>());
            }
        }

        private void BuildMineCliffFace(Transform parent)
        {
            var cliffRoot = new GameObject("MineCliffFace");
            cliffRoot.transform.SetParent(parent, false);
            cliffRoot.transform.localPosition = new Vector3(0, 0, 1.5f);

            CreatePanel(cliffRoot.transform, "Cliff_Left",
                new Vector3(-8f, 4f, 0), new Vector3(12f, 8.5f, 2.5f),
                minePortalRockMat, new Vector2(4f, 3f),
                Quaternion.Euler(0, 3f, -2f));
            CreatePanel(cliffRoot.transform, "Cliff_Right",
                new Vector3(8f, 4f, 0), new Vector3(12f, 8.5f, 2.5f),
                minePortalRockMat, new Vector2(4f, 3f),
                Quaternion.Euler(0, -3f, 2f));
            CreatePanel(cliffRoot.transform, "Cliff_Top",
                new Vector3(0, 7.5f, 0), new Vector3(6f, 3.5f, 2.5f),
                minePortalRockMat, new Vector2(2.5f, 1.5f),
                Quaternion.Euler(-4f, 0, 0));
            CreatePanel(cliffRoot.transform, "Cliff_Ledge",
                new Vector3(1.5f, 5.5f, -0.8f), new Vector3(4f, 0.6f, 1.5f),
                minePortalRockMat, new Vector2(1.5f, 0.6f),
                Quaternion.Euler(8f, 5f, -3f));
        }

        private void BuildScatteredOutdoorRocks(Transform parent)
        {
            var rocksRoot = new GameObject("OutdoorRocks");
            rocksRoot.transform.SetParent(parent, false);

            var rocks = new (Vector3 pos, Vector3 scale, Vector3 rot)[]
            {
                (new Vector3(-4.5f, 0.4f,  -2f),   new Vector3(0.90f, 0.70f, 1.10f), new Vector3( 8f,  25f, -5f)),
                (new Vector3( 5.5f, 0.3f,  -1.5f), new Vector3(0.75f, 0.55f, 0.90f), new Vector3(-6f, -18f,  4f)),
                (new Vector3(-6.0f, 0.25f, -4f),   new Vector3(1.20f, 0.50f, 0.80f), new Vector3( 4f,  45f,  0f)),
                (new Vector3( 7.0f, 0.4f,  -5f),   new Vector3(0.60f, 0.65f, 0.70f), new Vector3(-8f, -32f,  6f)),
                (new Vector3(-9.0f, 0.35f, -7f),   new Vector3(1.50f, 0.80f, 1.20f), new Vector3( 5f,  62f, -3f)),
                (new Vector3(10.0f, 0.3f,  -6f),   new Vector3(0.80f, 0.60f, 1.00f), new Vector3(-4f, -55f,  2f)),
                (new Vector3(-3.0f, 0.2f,  -8f),   new Vector3(0.40f, 0.35f, 0.50f), new Vector3(10f,  78f,  5f)),
                (new Vector3( 3.0f, 0.3f,  -9f),   new Vector3(0.70f, 0.55f, 0.65f), new Vector3(-5f,  30f, -4f)),
                (new Vector3(-11f,  0.4f,  -5f),   new Vector3(1.00f, 0.75f, 1.30f), new Vector3( 6f, 110f,  2f)),
                (new Vector3(12.0f, 0.3f,  -8f),   new Vector3(0.55f, 0.45f, 0.60f), new Vector3(-7f, -85f,  3f)),
                (new Vector3(-7.0f, 0.3f, -11f),   new Vector3(1.80f, 1.00f, 1.40f), new Vector3( 3f, 142f, -2f)),
                (new Vector3( 6.0f, 0.25f,-12f),   new Vector3(0.90f, 0.60f, 1.10f), new Vector3(-4f,-110f,  4f)),
                (new Vector3(-2.0f, 0.2f, -13f),   new Vector3(0.45f, 0.35f, 0.50f), new Vector3( 8f, 200f, -3f)),
                // Entrance-side rubble
                (new Vector3(-2.5f, 0.08f, -0.8f), new Vector3(0.22f, 0.16f, 0.28f), new Vector3(15f,  45f, -8f)),
                (new Vector3( 3.0f, 0.06f, -0.5f), new Vector3(0.18f, 0.12f, 0.20f), new Vector3(-12f,-60f,  6f)),
                (new Vector3(-1.5f, 0.09f, -1.2f), new Vector3(0.25f, 0.14f, 0.22f), new Vector3(18f,  90f, -5f)),
                (new Vector3( 4.5f, 0.07f, -2.0f), new Vector3(0.16f, 0.10f, 0.19f), new Vector3(-10f,135f,  3f)),
            };

            foreach (var (pos, scale, rot) in rocks)
            {
                var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rock.name = "OutdoorRock";
                rock.transform.SetParent(rocksRoot.transform, false);
                rock.transform.localPosition = pos;
                rock.transform.localScale    = scale;
                rock.transform.localRotation = Quaternion.Euler(rot);
                rock.GetComponent<Renderer>().material = outdoorRockMat;
                Destroy(rock.GetComponent<Collider>());
            }
        }

        private void BuildOutdoorProps(Transform parent)
        {
            var propsRoot = new GameObject("OutdoorProps");
            propsRoot.transform.SetParent(parent, false);

            // Warning sign — left of entrance
            BuildWarningSignProp(propsRoot.transform, new Vector3(-3.5f, 0, -2.5f), Quaternion.Euler(0, 15f, 0));

            // Cable drum — right side
            BuildCableDrumProp(propsRoot.transform, new Vector3(4.5f, 0, -3.5f));

            // Rock spoil pile
            BuildRockPileProp(propsRoot.transform, new Vector3(-5.5f, 0, -6.0f), 0.8f);

            // Safety cones flanking entrance
            BuildSafetyConeProp(propsRoot.transform, new Vector3(-2.2f, 0, -1.2f));
            BuildSafetyConeProp(propsRoot.transform, new Vector3( 2.8f, 0, -1.4f));

            // Wooden pallet
            BuildWoodenPalletProp(propsRoot.transform, new Vector3(5.5f, 0, -5.0f));
        }

        // =====================================================================
        // MINE PORTAL / ENTRANCE  (Z = 0m)
        // =====================================================================

        private void BuildMinePortal()
        {
            var portalRoot = new GameObject("MinePortal_Entrance");
            portalRoot.transform.SetParent(mineRoot, false);

            // ── Rock columns flanking portal ─────────────────────────────────────
            BuildPortalRockColumn(portalRoot.transform, -3.2f, false);
            BuildPortalRockColumn(portalRoot.transform, +3.2f, true);

            // ── Arch keystone (irregular rock cap above opening) ──────────────────
            CreatePanel(portalRoot.transform, "Portal_Arch_Main",
                new Vector3(0, 3.5f, 0.5f), new Vector3(5.8f, 1.2f, 2.0f),
                minePortalRockMat, new Vector2(3f, 1f), Quaternion.Euler(-8f, 0, 0));
            CreatePanel(portalRoot.transform, "Portal_Arch_OverhangL",
                new Vector3(-1.8f, 3.8f, -0.3f), new Vector3(2.5f, 0.8f, 1.5f),
                minePortalRockMat, new Vector2(1.5f, 0.8f), Quaternion.Euler(12f, 8f, -5f));
            CreatePanel(portalRoot.transform, "Portal_Arch_OverhangR",
                new Vector3( 1.8f, 3.8f, -0.3f), new Vector3(2.5f, 0.8f, 1.5f),
                minePortalRockMat, new Vector2(1.5f, 0.8f), Quaternion.Euler(12f,-8f,  5f));

            // ── Timber frame inside portal ────────────────────────────────────────
            CreatePanel(portalRoot.transform, "Portal_Timber_L",
                new Vector3(-1.4f, 1.2f, 0.8f), new Vector3(0.22f, 2.4f, 0.22f), woodMat, new Vector2(0.2f, 1.2f));
            CreatePanel(portalRoot.transform, "Portal_Timber_R",
                new Vector3( 1.4f, 1.2f, 0.8f), new Vector3(0.22f, 2.4f, 0.22f), woodMat, new Vector2(0.2f, 1.2f));
            CreatePanel(portalRoot.transform, "Portal_Timber_Lintel",
                new Vector3(0, 2.5f, 0.8f), new Vector3(3.2f, 0.22f, 0.22f), woodMat, new Vector2(1.6f, 0.2f));

            // ── "ADIT NO. 3 — EMERGENCY EXIT" sign above arch ─────────────────────
            var signGO = new GameObject("PortalSignboard");
            signGO.transform.SetParent(portalRoot.transform, false);
            signGO.transform.localPosition = new Vector3(0, 3.05f, -0.5f);
            CreatePanel(signGO.transform, "Sign_Backing",
                Vector3.zero, new Vector3(2.3f, 0.52f, 0.06f), signBackMat, new Vector2(1f, 1f));
            var sTGO = new GameObject("PortalSignText");
            sTGO.transform.SetParent(signGO.transform, false);
            sTGO.transform.localPosition = new Vector3(0, 0, -0.06f);
            sTGO.transform.localScale = Vector3.one * 0.016f;
            var stm = sTGO.AddComponent<TextMesh>();
            stm.text = "ADIT NO. 3\nEMERGENCY EXIT";
            stm.fontSize = 36;
            stm.alignment = TextAlignment.Center;
            stm.anchor = TextAnchor.MiddleCenter;
            stm.color = new Color(1.0f, 0.85f, 0.15f);

            // ── Base rubble at portal foot ─────────────────────────────────────────
            BuildPortalBaseRubble(portalRoot.transform);

            // ── Rail tracks emerging from portal (Z = -4 to 2) ────────────────────
            BuildRails(-4.0f, 2.0f, 0.8f);

            // ── Conduit cables through portal ──────────────────────────────────────
            float[] conduitH = { 1.9f, 2.1f, 2.3f };
            float[] conduitX = { -0.8f, 0f, 0.6f };
            for (int i = 0; i < 3; i++)
                CreateCylinder(portalRoot.transform, $"PortalConduit_{i}",
                    new Vector3(conduitX[i], conduitH[i], 1.0f),
                    new Vector3(0.04f, 1.5f, 0.04f), pipeMat, Quaternion.Euler(90, 0, 0));

            // ── Portal lamps ───────────────────────────────────────────────────────
            CreatePointLight(portalRoot.transform, new Vector3(-1.8f, 2.4f, 0.5f), workLightColor, 5f, 5f, "Portal_LampL");
            CreatePointLight(portalRoot.transform, new Vector3( 1.8f, 2.4f, 0.5f), workLightColor, 5f, 5f, "Portal_LampR");
        }

        private void BuildPortalRockColumn(Transform parent, float xPos, bool mirror)
        {
            var col = new GameObject($"PortalCol_{(xPos < 0 ? "L" : "R")}");
            col.transform.SetParent(parent, false);
            col.transform.localPosition = new Vector3(xPos, 0, 0.5f);
            float s = mirror ? -1f : 1f;

            CreatePanel(col.transform, "Col_Base",
                new Vector3(0, 1.5f, 0), new Vector3(2.2f, 3.0f, 2.0f),
                minePortalRockMat, new Vector2(1.2f, 1.5f), Quaternion.Euler(2f*s, 5f*s, 1f));
            CreatePanel(col.transform, "Col_Mid",
                new Vector3(0.2f*s, 3.5f, -0.1f), new Vector3(2.0f, 1.8f, 1.8f),
                minePortalRockMat, new Vector2(1f, 1f), Quaternion.Euler(-3f, 8f*s, 2f*s));
            CreatePanel(col.transform, "Col_Top",
                new Vector3(-0.2f*s, 4.8f, 0.15f), new Vector3(1.6f, 1.4f, 1.5f),
                minePortalRockMat, new Vector2(0.8f, 0.7f), Quaternion.Euler(5f*s, -12f*s, -3f));
            CreatePanel(col.transform, "Col_Overhang",
                new Vector3(0.4f*s, 2.8f, -0.5f), new Vector3(0.8f, 1.2f, 1.2f),
                minePortalRockMat, new Vector2(0.5f, 0.6f), Quaternion.Euler(-8f, 15f*s, 4f*s));
        }

        private void BuildPortalBaseRubble(Transform parent)
        {
            var rubbleRoot = new GameObject("Portal_BaseRubble");
            rubbleRoot.transform.SetParent(parent, false);

            var rubbles = new (Vector3 pos, Vector3 scale, Vector3 rot)[]
            {
                (new Vector3(-3.5f, 0.12f, -0.5f), new Vector3(0.5f, 0.24f, 0.6f), new Vector3(10f, 25f, -5f)),
                (new Vector3(-2.8f, 0.08f, -0.8f), new Vector3(0.3f, 0.16f, 0.4f), new Vector3(-8f, 45f,  3f)),
                (new Vector3( 3.5f, 0.12f, -0.5f), new Vector3(0.5f, 0.24f, 0.6f), new Vector3(10f,-25f,  5f)),
                (new Vector3( 2.8f, 0.08f, -0.8f), new Vector3(0.3f, 0.16f, 0.4f), new Vector3(-8f,-45f, -3f)),
                (new Vector3(-4.2f, 0.15f,  0.2f), new Vector3(0.7f, 0.30f, 0.5f), new Vector3( 5f, 68f, -2f)),
                (new Vector3( 4.2f, 0.15f,  0.2f), new Vector3(0.7f, 0.30f, 0.5f), new Vector3( 5f,-68f,  2f)),
                (new Vector3( 0.5f, 0.06f, -1.0f), new Vector3(0.2f, 0.12f, 0.25f),new Vector3(15f, 90f, -6f)),
                (new Vector3(-1.2f, 0.07f, -0.9f), new Vector3(0.18f,0.10f, 0.22f),new Vector3(-12f,130f,  4f)),
            };

            foreach (var (pos, scale, rot) in rubbles)
            {
                var rb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rb.name = "PortalRubble";
                rb.transform.SetParent(rubbleRoot.transform, false);
                rb.transform.localPosition = pos;
                rb.transform.localScale    = scale;
                rb.transform.localRotation = Quaternion.Euler(rot);
                rb.GetComponent<Renderer>().material = minePortalRockMat;
                Destroy(rb.GetComponent<Collider>());
            }
        }

        // =====================================================================
        // UNDERGROUND ENHANCEMENT  (rock variation + mining props along tunnel)
        // =====================================================================

        private void BuildUndergroundEnhancement()
        {
            var enhRoot = new GameObject("UndergroundEnhancement");
            enhRoot.transform.SetParent(mineRoot, false);

            BuildTunnelRockVariation(enhRoot.transform);
            BuildMiningEquipmentProps(enhRoot.transform);
            BuildTunnelDebris(enhRoot.transform);
        }

        private void BuildTunnelRockVariation(Transform parent)
        {
            var root = new GameObject("TunnelRockVariation");
            root.transform.SetParent(parent, false);

            float[] zPos = { 3f, 5.5f, 8f, 10.5f, 13f, 15.5f, 18f, 20.5f, 23f, 25.5f, 27.5f };
            for (int i = 0; i < zPos.Length; i++)
            {
                float z  = zPos[i];
                float vs = 0.30f + (i % 3) * 0.12f;
                float yo = 0.40f + (i % 4) * 0.18f;

                // Left wall bump
                CreatePanel(root.transform, $"RockBump_L_{i}",
                    new Vector3(-1.25f, yo, z),
                    new Vector3(0.25f, vs, vs * 1.3f),
                    rockMat, new Vector2(0.5f, 0.5f),
                    Quaternion.Euler((i%3-1)*6f, (i%4-2)*8f, (i%2==0?3f:-3f)));

                // Right wall bump (offset)
                if (z + 1.2f < 29f)
                {
                    float zr  = z + 1.2f;
                    float vsr = 0.28f + ((i+1)%3) * 0.10f;
                    float yor = 0.35f + ((i+1)%4) * 0.15f;
                    CreatePanel(root.transform, $"RockBump_R_{i}",
                        new Vector3(1.25f, yor, zr),
                        new Vector3(0.25f, vsr, vsr * 1.2f),
                        rockMat, new Vector2(0.5f, 0.5f),
                        Quaternion.Euler(((i+1)%3-1)*5f, ((i+1)%4-2)*9f, (i%2==0?-3f:3f)));
                }

                // Ceiling irregularity
                float vc = 0.20f + (i%3)*0.08f;
                CreatePanel(root.transform, $"CeilBump_{i}",
                    new Vector3((i%3-1)*0.5f, 2.42f, z+0.6f),
                    new Vector3(vc*1.5f, 0.18f, vc),
                    rockMat, new Vector2(0.4f, 0.4f),
                    Quaternion.Euler((i%2)*3f, (i%4)*5f, (i%3)*4f));
            }
        }

        private void BuildMiningEquipmentProps(Transform parent)
        {
            var propsRoot = new GameObject("MiningEquipmentProps");
            propsRoot.transform.SetParent(parent, false);

            // Z≈5: pickaxe + shovel + hardhat
            BuildPickaxeProp(propsRoot.transform, new Vector3(-1.0f, 0, 4.5f), Quaternion.Euler(0, 30f, 65f));
            BuildShovelProp(propsRoot.transform, new Vector3(-1.1f, 0, 5.3f), Quaternion.Euler(0,-20f, 60f));
            BuildHardhatProp(propsRoot.transform, new Vector3(-0.9f, 0.15f, 6.0f));

            // Z≈9: safety lamp on wall shelf
            BuildSafetyLampProp(propsRoot.transform, new Vector3(-1.12f, 1.2f, 9.0f));

            // Z≈11: toolbox + cable reel
            BuildToolboxProp(propsRoot.transform, new Vector3(1.0f, 0, 11.0f));
            BuildCableReelProp(propsRoot.transform, new Vector3(0.9f, 0, 12.2f));

            // Z≈17: wheelbarrow
            BuildWheelbarrowProp(propsRoot.transform, new Vector3(-0.7f, 0, 17.5f));

            // Z≈20: crate stack
            BuildCrateStackProp(propsRoot.transform, new Vector3(1.0f, 0, 20.0f));

            // Z≈24: mining cart on rails
            BuildMiningCartProp(propsRoot.transform, new Vector3(0f, 0, 24.5f));
        }

        private void BuildTunnelDebris(Transform parent)
        {
            var root = new GameObject("TunnelDebris");
            root.transform.SetParent(parent, false);

            // Rock piles at wall edges
            var piles = new (Vector3 pos, float size)[]
            {
                (new Vector3(-1.1f, 0, 6.5f),  0.50f),
                (new Vector3( 1.1f, 0, 10.0f), 0.42f),
                (new Vector3(-1.0f, 0, 14.0f), 0.58f),
                (new Vector3( 1.1f, 0, 18.5f), 0.45f),
                (new Vector3(-1.1f, 0, 22.5f), 0.55f),
                (new Vector3( 1.0f, 0, 26.0f), 0.40f),
            };
            foreach (var (pos, size) in piles)
                BuildRockPileProp(root.transform, pos, size);

            // Floor loose stones
            var stones = new (Vector3 pos, Vector3 sc)[]
            {
                (new Vector3(-0.8f, 0.04f,  7.5f), new Vector3(0.12f,0.08f,0.14f)),
                (new Vector3( 0.6f, 0.03f, 12.5f), new Vector3(0.09f,0.06f,0.11f)),
                (new Vector3(-0.5f, 0.04f, 16.0f), new Vector3(0.14f,0.07f,0.12f)),
                (new Vector3( 0.8f, 0.03f, 21.0f), new Vector3(0.10f,0.06f,0.13f)),
                (new Vector3(-0.7f, 0.04f, 24.0f), new Vector3(0.11f,0.08f,0.10f)),
                (new Vector3( 0.4f, 0.03f, 27.0f), new Vector3(0.08f,0.05f,0.09f)),
            };
            int si = 0;
            foreach (var (pos, sc) in stones)
            {
                var stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stone.name = "LooseStone";
                stone.transform.SetParent(root.transform, false);
                stone.transform.localPosition = pos;
                stone.transform.localScale    = sc;
                stone.transform.localRotation = Quaternion.Euler(
                    (si * 17 % 30) - 15f, si * 37 % 180f, (si * 13 % 20) - 10f);
                stone.GetComponent<Renderer>().material = rockMat;
                Destroy(stone.GetComponent<Collider>());
                si++;
            }
        }

        // =====================================================================
        // PROP BUILDER HELPERS
        // =====================================================================

        private void BuildPickaxeProp(Transform parent, Vector3 pos, Quaternion rot)
        {
            var r = new GameObject("Pickaxe");
            r.transform.SetParent(parent, false);
            r.transform.localPosition = pos;
            r.transform.localRotation = rot;
            CreateCylinder(r.transform, "Handle", new Vector3(0, 0.5f, 0), new Vector3(0.04f,0.5f,0.04f), woodMat);
            CreatePanel(r.transform, "Head", new Vector3(0.15f,1.05f,0), new Vector3(0.35f,0.06f,0.07f),
                metalMat, new Vector2(0.3f,0.1f), Quaternion.Euler(0,0,15f));
        }

        private void BuildShovelProp(Transform parent, Vector3 pos, Quaternion rot)
        {
            var r = new GameObject("Shovel");
            r.transform.SetParent(parent, false);
            r.transform.localPosition = pos;
            r.transform.localRotation = rot;
            CreateCylinder(r.transform, "Handle", new Vector3(0,0.6f,0), new Vector3(0.035f,0.6f,0.035f), woodMat);
            CreatePanel(r.transform, "Blade", new Vector3(0,0.05f,0), new Vector3(0.22f,0.04f,0.28f), metalMat, new Vector2(0.2f,0.25f));
        }

        private void BuildHardhatProp(Transform parent, Vector3 pos)
        {
            var r = new GameObject("Hardhat");
            r.transform.SetParent(parent, false);
            r.transform.localPosition = pos;
            CreatePanel(r.transform, "Cap", Vector3.zero, new Vector3(0.25f,0.15f,0.28f),
                hardhatYellowMat, new Vector2(0.2f,0.2f));
        }

        private void BuildSafetyLampProp(Transform parent, Vector3 pos)
        {
            var r = new GameObject("SafetyLamp");
            r.transform.SetParent(parent, false);
            r.transform.localPosition = pos;
            CreateCylinder(r.transform, "Body", Vector3.zero, new Vector3(0.08f,0.12f,0.08f), metalMat);
            var globe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            globe.name = "Globe";
            globe.transform.SetParent(r.transform, false);
            globe.transform.localPosition = new Vector3(0,0.15f,0);
            globe.transform.localScale    = new Vector3(0.07f,0.07f,0.07f);
            globe.GetComponent<Renderer>().material = markerLampMat;
            Destroy(globe.GetComponent<Collider>());
        }

        private void BuildToolboxProp(Transform parent, Vector3 pos)
        {
            var r = new GameObject("Toolbox");
            r.transform.SetParent(parent, false);
            r.transform.localPosition = pos;
            r.transform.localRotation = Quaternion.Euler(0, 25f, 0);
            CreatePanel(r.transform, "Body", new Vector3(0,0.15f,0), new Vector3(0.45f,0.30f,0.25f), metalMat, new Vector2(0.4f,0.25f));
            CreatePanel(r.transform, "Lid", new Vector3(0,0.31f,-0.08f), new Vector3(0.45f,0.04f,0.25f),
                metalMat, new Vector2(0.4f,0.2f), Quaternion.Euler(-20f,0,0));
            CreatePanel(r.transform, "Handle", new Vector3(0,0.34f,0), new Vector3(0.15f,0.04f,0.04f), metalMat, new Vector2(0.1f,0.04f));
        }

        private void BuildCableReelProp(Transform parent, Vector3 pos)
        {
            var r = new GameObject("CableReel");
            r.transform.SetParent(parent, false);
            r.transform.localPosition = pos;
            CreateCylinder(r.transform, "Drum", Vector3.zero, new Vector3(0.35f,0.18f,0.35f), metalMat, Quaternion.Euler(0,0,90f));
            CreatePanel(r.transform, "FlangeL", new Vector3(-0.22f,0,0), new Vector3(0.04f,0.44f,0.44f), metalMat, new Vector2(0.3f,0.3f));
            CreatePanel(r.transform, "FlangeR", new Vector3( 0.22f,0,0), new Vector3(0.04f,0.44f,0.44f), metalMat, new Vector2(0.3f,0.3f));
        }

        private void BuildWheelbarrowProp(Transform parent, Vector3 pos)
        {
            var r = new GameObject("Wheelbarrow");
            r.transform.SetParent(parent, false);
            r.transform.localPosition = pos;
            r.transform.localRotation = Quaternion.Euler(0, 15f, 0);
            CreatePanel(r.transform, "Tray", new Vector3(0,0.4f,0), new Vector3(0.5f,0.15f,0.7f), metalMat, new Vector2(0.4f,0.6f));
            CreateCylinder(r.transform, "Wheel", new Vector3(0,0.12f,0.32f), new Vector3(0.12f,0.06f,0.12f), metalMat, Quaternion.Euler(0,0,90f));
            CreatePanel(r.transform, "HandleL", new Vector3(-0.2f,0.45f,-0.4f), new Vector3(0.04f,0.04f,0.55f), woodMat, new Vector2(0.1f,0.5f), Quaternion.Euler(10f,0,0));
            CreatePanel(r.transform, "HandleR", new Vector3( 0.2f,0.45f,-0.4f), new Vector3(0.04f,0.04f,0.55f), woodMat, new Vector2(0.1f,0.5f), Quaternion.Euler(10f,0,0));
            CreatePanel(r.transform, "Load", new Vector3(0,0.5f,0), new Vector3(0.42f,0.12f,0.62f), rockMat, new Vector2(0.4f,0.5f));
        }

        private void BuildCrateStackProp(Transform parent, Vector3 pos)
        {
            var r = new GameObject("CrateStack");
            r.transform.SetParent(parent, false);
            r.transform.localPosition = pos;
            r.transform.localRotation = Quaternion.Euler(0, 10f, 0);
            for (int i = 0; i < 3; i++)
                CreatePanel(r.transform, $"Crate_{i}", new Vector3(0, 0.22f + i*0.24f, 0),
                    new Vector3(0.42f, 0.22f, 0.42f), woodMat, new Vector2(0.4f,0.4f), Quaternion.Euler(0, i*8f, 0));
        }

        private void BuildMiningCartProp(Transform parent, Vector3 pos)
        {
            var r = new GameObject("MiningCart");
            r.transform.SetParent(parent, false);
            r.transform.localPosition = pos;
            CreatePanel(r.transform, "Body", new Vector3(0,0.4f,0), new Vector3(0.55f,0.35f,0.85f), metalMat, new Vector2(0.5f,0.8f));
            float[] wx = { -0.3f, 0.3f };
            float[] wz = { -0.35f, 0.35f };
            foreach (float wx2 in wx)
            foreach (float wz2 in wz)
                CreateCylinder(r.transform, "Wheel", new Vector3(wx2,0.1f,wz2), new Vector3(0.12f,0.06f,0.12f), metalMat, Quaternion.Euler(0,0,90f));
        }

        private void BuildRockPileProp(Transform parent, Vector3 pos, float size)
        {
            var r = new GameObject("RockPile");
            r.transform.SetParent(parent, false);
            r.transform.localPosition = pos;
            CreatePanel(r.transform, "Main", new Vector3(0, size*0.25f, 0),
                new Vector3(size*1.2f, size*0.5f, size), rockMat, new Vector2(0.6f,0.5f), Quaternion.Euler(5f,20f,0));
            CreatePanel(r.transform, "Sub1", new Vector3(-size*0.4f, size*0.12f, 0.05f),
                new Vector3(size*0.5f, size*0.25f, size*0.45f), rockMat, new Vector2(0.3f,0.3f), Quaternion.Euler(-4f,45f,3f));
            CreatePanel(r.transform, "Sub2", new Vector3(size*0.3f, size*0.10f, -size*0.1f),
                new Vector3(size*0.4f, size*0.20f, size*0.35f), rockMat, new Vector2(0.25f,0.25f), Quaternion.Euler(6f,-30f,-2f));
        }

        private void BuildWarningSignProp(Transform parent, Vector3 pos, Quaternion rot)
        {
            var r = new GameObject("WarningSign");
            r.transform.SetParent(parent, false);
            r.transform.localPosition = pos;
            r.transform.localRotation = rot;
            CreateCylinder(r.transform, "Post", new Vector3(0,0.8f,0), new Vector3(0.05f,0.8f,0.05f), metalMat);
            CreatePanel(r.transform, "Board", new Vector3(0,1.75f,0), new Vector3(0.5f,0.38f,0.04f), cautionMat, new Vector2(0.5f,0.35f));
            var tGO = new GameObject("SignText");
            tGO.transform.SetParent(r.transform, false);
            tGO.transform.localPosition = new Vector3(0, 1.75f, -0.05f);
            tGO.transform.localScale = Vector3.one * 0.012f;
            var stm = tGO.AddComponent<TextMesh>();
            stm.text = "⚠ HARD HAT\nAREA";
            stm.fontSize = 28;
            stm.alignment = TextAlignment.Center;
            stm.anchor    = TextAnchor.MiddleCenter;
            stm.color = new Color(0.08f, 0.06f, 0.04f);
        }

        private void BuildCableDrumProp(Transform parent, Vector3 pos)
        {
            var r = new GameObject("CableDrum");
            r.transform.SetParent(parent, false);
            r.transform.localPosition = pos;
            r.transform.localRotation = Quaternion.Euler(0, -20f, 0);
            CreateCylinder(r.transform, "Drum", new Vector3(0,0.4f,0), new Vector3(0.40f,0.40f,0.40f), metalMat);
            CreatePanel(r.transform, "FlangeB", new Vector3(0,0.04f,0), new Vector3(0.72f,0.04f,0.72f), metalMat, new Vector2(0.5f,0.5f));
            CreatePanel(r.transform, "FlangeT", new Vector3(0,0.76f,0), new Vector3(0.72f,0.04f,0.72f), metalMat, new Vector2(0.5f,0.5f));
            CreateCylinder(r.transform, "Cable", new Vector3(0,0.4f,0), new Vector3(0.38f,0.42f,0.38f),
                CreateSolidMaterial(new Color(0.08f,0.08f,0.08f)));
        }

        private void BuildSafetyConeProp(Transform parent, Vector3 pos)
        {
            var r = new GameObject("SafetyCone");
            r.transform.SetParent(parent, false);
            r.transform.localPosition = pos;
            CreatePanel(r.transform, "Body", new Vector3(0,0.20f,0), new Vector3(0.10f,0.40f,0.10f), cautionMat, new Vector2(0.1f,0.3f));
            CreatePanel(r.transform, "Base", new Vector3(0,0.02f,0), new Vector3(0.22f,0.04f,0.22f),
                CreateSolidMaterial(new Color(0.15f,0.15f,0.15f)), new Vector2(0.2f,0.2f));
        }

        private void BuildWoodenPalletProp(Transform parent, Vector3 pos)
        {
            var r = new GameObject("WoodenPallet");
            r.transform.SetParent(parent, false);
            r.transform.localPosition = pos;
            r.transform.localRotation = Quaternion.Euler(0, 35f, 0);
            CreatePanel(r.transform, "PalletTop", new Vector3(0,0.06f,0), new Vector3(0.6f,0.06f,0.8f), woodMat, new Vector2(0.5f,0.7f));
        }

        // ── Outdoor Rock Texture ─────────────────────────────────────────────────

        private Texture2D MakeOutdoorRockTexture(int w, int h)
        {
            var tex    = new Texture2D(w, h);
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++)
            {
                int   x  = i % w, y = i / w;
                float n1 = Mathf.PerlinNoise(x * 0.04f,         y * 0.04f);
                float n2 = Mathf.PerlinNoise(x * 0.10f + 50f,   y * 0.10f + 50f);
                float n3 = Mathf.PerlinNoise(x * 0.22f + 200f,  y * 0.22f + 200f);
                float f  = n1 * 0.5f + n2 * 0.3f + n3 * 0.2f;
                pixels[i] = new Color(
                    Mathf.Lerp(0.38f, 0.62f, f),
                    Mathf.Lerp(0.32f, 0.54f, f),
                    Mathf.Lerp(0.22f, 0.40f, f));
            }
            tex.SetPixels(pixels);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Repeat;
            return tex;
        }
    }
}
