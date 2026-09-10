using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SurakshaAR.Training.Fire
{
    /// <summary>
    /// PHASE 2B-1: Manages the 3 physical 3D response resources in the mine environment:
    ///   1. Water Container (blue drum/canister)
    ///   2. Fire Extinguisher (red pressurized cylinder with nozzle & gauge)
    ///   3. Mud / Rock Dust Pile (mining rock dust mound with shovel)
    ///
    /// Handles proximity detection, contextual interaction prompts,
    /// and recording of the selected resource.
    /// </summary>
    public class FireResponseResourceManager : MonoBehaviour
    {
        // ── Inspector References ──────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private MineMovementController movementController;
        [SerializeField] private Camera virtualCamera;
        [SerializeField] private Transform mineOrigin;

        [Header("UI Prompts")]
        [SerializeField] private Text promptTitleText;
        [SerializeField] private Text promptActionText;
        [SerializeField] private CanvasGroup promptCanvasGroup;

        [Header("Status Toast")]
        [SerializeField] private Text toastText;
        [SerializeField] private CanvasGroup toastCanvasGroup;

        [Header("Settings")]
        [Tooltip("Maximum distance from camera to show interaction prompt.")]
        [SerializeField] private float proximityDistance = 2.0f;

        // ── Data & State ──────────────────────────────────────────────────────

        public enum ExtinguisherState
        {
            World,
            PickedUp,
            UsedOnFire
        }

        public ExtinguisherState CurrentExtinguisherState { get; private set; } = ExtinguisherState.World;
        public ResponseResourceType SelectedResource { get; private set; } = ResponseResourceType.None;

        public event Action<ResponseResourceType> OnResourceSelected;

        public bool IsAssessmentActive { get; private set; } = false;

        // First-Person Hand Held Extinguisher
        private GameObject firstPersonHandAnchor = null;
        private GameObject heldExtinguisherRoot = null;
        private readonly Vector3 handAnchorBaseLocalPos = new Vector3(0.12f, -0.18f, 0.42f);

        private class ResourceItem
        {
            public ResponseResourceType Type;
            public string Title;
            public string ActionPrompt;
            public Vector3 WorldPosition;
            public GameObject RootGO;
            public Renderer[] Renderers;
            public Color BaseColor;
            public bool IsCollected = false;
        }

        private readonly List<ResourceItem> resources = new List<ResourceItem>();
        private ResourceItem currentClosestItem = null;
        private float promptAlpha = 0f;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Start()
        {
            BuildAllResources();

            // Build first-person hand anchor child under VirtualCamera
            if (virtualCamera != null)
            {
                BuildHeldExtinguisher(virtualCamera.transform);
            }

            if (promptCanvasGroup != null)
            {
                promptCanvasGroup.alpha = 0f;
                var img = promptCanvasGroup.GetComponent<Image>();
                if (img != null) img.enabled = false;
            }

            if (toastCanvasGroup != null)
            {
                toastCanvasGroup.alpha = 0f;
                var img = toastCanvasGroup.GetComponent<Image>();
                if (img != null) img.enabled = false;
            }
        }

        private void Update()
        {
            UpdateProximityAndPrompts();
            CheckTapToSelect();
            UpdateHeldExtinguisherMotion();
        }

        private void UpdateHeldExtinguisherMotion()
        {
            if (heldExtinguisherRoot != null && heldExtinguisherRoot.activeSelf && firstPersonHandAnchor != null)
            {
                // Subtle natural bobbing when physically walking
                float bob = 0f;
                if (movementController != null && movementController.MotionState == "WALKING")
                {
                    bob = Mathf.Sin(Time.time * 7f) * 0.005f;
                }
                firstPersonHandAnchor.transform.localPosition = handAnchorBaseLocalPos + new Vector3(0f, bob, 0f);
            }
        }

        public void ActivateAssessment()
        {
            IsAssessmentActive = true;
            Debug.Log("[FireResourceManager] Fire Assessment activated. Worker searching for response resources.");
        }

        /// <summary>
        /// Called by FireResponseEvaluator after a wrong response — resets selection
        /// so worker can pick a different resource and retry.
        /// </summary>
        public void ActivateRetry()
        {
            SelectedResource = ResponseResourceType.None;
            IsAssessmentActive = true;
            currentClosestItem = null;
            HideHeldResource();
            Debug.Log("[FireResourceManager] Retry activated — worker can choose another available resource.");
        }

        // ── 3D Resource Spawning ──────────────────────────────────────────────

        private void BuildAllResources()
        {
            Transform parent = (mineOrigin != null) ? mineOrigin : transform;

            // 1. Fire Extinguisher (wall mounted on left side at Z = 16.2m, 4.2m past emergency alarm at Z = 12.0m)
            Vector3 extPos = new Vector3(-0.92f, 1.25f, 16.2f);
            var extGO = BuildFireExtinguisher(parent, extPos);
            resources.Add(new ResourceItem
            {
                Type = ResponseResourceType.FireExtinguisher,
                Title = "Fire Extinguisher",
                ActionPrompt = "TAP TO PICK UP",
                WorldPosition = extGO.transform.position,
                RootGO = extGO,
                Renderers = extGO.GetComponentsInChildren<Renderer>(),
                BaseColor = new Color(0.85f, 0.10f, 0.05f)
            });

            // 2. Water Container (on elevated equipment stand on right side at Z = 16.8m)
            Vector3 waterPos = new Vector3(0.85f, 0.35f, 16.8f);
            var waterGO = BuildWaterContainer(parent, waterPos);
            resources.Add(new ResourceItem
            {
                Type = ResponseResourceType.WaterContainer,
                Title = "Water Container",
                ActionPrompt = "TAP TO USE",
                WorldPosition = waterGO.transform.position,
                RootGO = waterGO,
                Renderers = waterGO.GetComponentsInChildren<Renderer>(),
                BaseColor = new Color(0.10f, 0.40f, 0.85f)
            });

            // 3. Mud / Rock Dust Pile (work area on left side at Z = 19.5m, ~3.3m past extinguisher)
            Vector3 mudPos = new Vector3(-0.75f, 0.20f, 19.5f);
            var mudGO = BuildMudPile(parent, mudPos);
            resources.Add(new ResourceItem
            {
                Type = ResponseResourceType.MudPile,
                Title = "Rock Dust & Mud Pile",
                ActionPrompt = "TAP TO COLLECT",
                WorldPosition = mudGO.transform.position,
                RootGO = mudGO,
                Renderers = mudGO.GetComponentsInChildren<Renderer>(),
                BaseColor = new Color(0.38f, 0.32f, 0.25f)
            });

            Debug.Log("[FireResourceManager] 3 response resources spawned: Extinguisher@16.2m (L) [separated 4.2m from Alarm@12.0m], Water@16.8m (R), Mud@19.5m (L).");
        }

        // ── 1. Water Container Construction ───────────────────────────────────

        private GameObject BuildWaterContainer(Transform parent, Vector3 localPos)
        {
            var root = new GameObject("Resource_WaterContainer");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;

            var stdShader = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse");

            // Base cribbing / wooden pallet beneath container
            var pallet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pallet.name = "WaterPalletBase";
            pallet.transform.SetParent(root.transform, false);
            pallet.transform.localPosition = new Vector3(0f, -0.16f, 0f);
            pallet.transform.localScale = new Vector3(0.48f, 0.08f, 0.48f);
            Destroy(pallet.GetComponent<Collider>());
            var palletMat = new Material(stdShader);
            palletMat.color = new Color(0.38f, 0.26f, 0.14f); // Mine timber
            pallet.GetComponent<Renderer>().material = palletMat;

            // Main canister body (industrial blue drum)
            var drum = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            drum.name = "WaterDrum";
            drum.transform.SetParent(root.transform, false);
            drum.transform.localPosition = Vector3.zero;
            drum.transform.localScale = new Vector3(0.32f, 0.22f, 0.32f);
            Destroy(drum.GetComponent<Collider>());

            var drumMat = new Material(stdShader);
            drumMat.color = new Color(0.08f, 0.38f, 0.82f); // Industrial safety blue
            drumMat.SetFloat("_Glossiness", 0.65f);
            drum.GetComponent<Renderer>().material = drumMat;

            // Top screw cap
            var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cap.name = "FillerCap";
            cap.transform.SetParent(root.transform, false);
            cap.transform.localPosition = new Vector3(0f, 0.23f, 0f);
            cap.transform.localScale = new Vector3(0.12f, 0.03f, 0.12f);
            Destroy(cap.GetComponent<Collider>());

            var capMat = new Material(stdShader);
            capMat.color = new Color(0.95f, 0.95f, 0.95f);
            cap.GetComponent<Renderer>().material = capMat;

            // Carry handle (black curved bar across top)
            var handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handle.name = "CarryHandle";
            handle.transform.SetParent(root.transform, false);
            handle.transform.localPosition = new Vector3(0f, 0.26f, 0f);
            handle.transform.localScale = new Vector3(0.04f, 0.06f, 0.24f);
            Destroy(handle.GetComponent<Collider>());

            var handleMat = new Material(stdShader);
            handleMat.color = new Color(0.12f, 0.12f, 0.12f);
            handle.GetComponent<Renderer>().material = handleMat;

            // Water emblem decal
            var emblem = GameObject.CreatePrimitive(PrimitiveType.Cube);
            emblem.name = "WaterLabel";
            emblem.transform.SetParent(root.transform, false);
            emblem.transform.localPosition = new Vector3(0.165f, 0.02f, 0f);
            emblem.transform.localScale = new Vector3(0.01f, 0.14f, 0.14f);
            Destroy(emblem.GetComponent<Collider>());

            var emblemMat = new Material(stdShader);
            emblemMat.color = new Color(0.90f, 0.95f, 1.0f);
            emblem.GetComponent<Renderer>().material = emblemMat;

            // Environmental Prop: Wall water pipe on right tunnel rock
            var wallPipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wallPipe.name = "WaterSupplyPipe";
            wallPipe.transform.SetParent(root.transform, false);
            wallPipe.transform.localPosition = new Vector3(0.18f, 0.60f, -0.20f);
            wallPipe.transform.localScale = new Vector3(0.05f, 0.70f, 0.05f);
            Destroy(wallPipe.GetComponent<Collider>());
            var pipeMat = new Material(stdShader);
            pipeMat.color = new Color(0.35f, 0.35f, 0.38f);
            wallPipe.GetComponent<Renderer>().material = pipeMat;

            // Valve wheel on water pipe
            var valve = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            valve.name = "SupplyValveWheel";
            valve.transform.SetParent(root.transform, false);
            valve.transform.localPosition = new Vector3(0.12f, 0.65f, -0.20f);
            valve.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            valve.transform.localScale = new Vector3(0.10f, 0.02f, 0.10f);
            Destroy(valve.GetComponent<Collider>());
            var valveMat = new Material(stdShader);
            valveMat.color = new Color(0.85f, 0.70f, 0.10f); // Brass/yellow valve
            valve.GetComponent<Renderer>().material = valveMat;

            // Environmental Prop: Coiled black hose on floor next to drum
            var hoseCoil = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hoseCoil.name = "CoiledRubberHose";
            hoseCoil.transform.SetParent(root.transform, false);
            hoseCoil.transform.localPosition = new Vector3(-0.08f, -0.15f, 0.32f);
            hoseCoil.transform.localScale = new Vector3(0.28f, 0.06f, 0.28f);
            Destroy(hoseCoil.GetComponent<Collider>());
            var hoseMat = new Material(stdShader);
            hoseMat.color = new Color(0.12f, 0.12f, 0.12f);
            hoseCoil.GetComponent<Renderer>().material = hoseMat;

            // Dedicated inspection lamp illuminating water station
            var lampGO = new GameObject("WaterWorkLamp");
            lampGO.transform.SetParent(root.transform, false);
            lampGO.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            var lamp = lampGO.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.color = new Color(0.85f, 0.92f, 1.0f);
            lamp.intensity = 5.0f;
            lamp.range = 4.0f;

            return root;
        }

        // ── 2. Fire Extinguisher Construction (Left Wall Mounted Z = 17m) ──────

        private GameObject BuildFireExtinguisher(Transform parent, Vector3 localPos)
        {
            var root = new GameObject("Resource_FireExtinguisher");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;

            var stdShader = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse");

            // Safety backboard on rock wall
            var backboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backboard.name = "SafetyBackboard";
            backboard.transform.SetParent(root.transform, false);
            backboard.transform.localPosition = new Vector3(-0.06f, 0.05f, 0f);
            backboard.transform.localScale = new Vector3(0.02f, 0.72f, 0.36f);
            Destroy(backboard.GetComponent<Collider>());
            var backMat = new Material(stdShader);
            backMat.color = new Color(0.90f, 0.90f, 0.92f); // White/red safety board
            backboard.GetComponent<Renderer>().material = backMat;

            // Red header banner on backboard
            var header = GameObject.CreatePrimitive(PrimitiveType.Cube);
            header.name = "SafetyHeader";
            header.transform.SetParent(root.transform, false);
            header.transform.localPosition = new Vector3(-0.048f, 0.36f, 0f);
            header.transform.localScale = new Vector3(0.01f, 0.10f, 0.34f);
            Destroy(header.GetComponent<Collider>());
            var headerMat = new Material(stdShader);
            headerMat.color = new Color(0.85f, 0.10f, 0.05f); // Red
            header.GetComponent<Renderer>().material = headerMat;

            // Wall mounting steel bracket
            var bracket = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bracket.name = "WallBracket";
            bracket.transform.SetParent(root.transform, false);
            bracket.transform.localPosition = new Vector3(-0.04f, 0f, 0f);
            bracket.transform.localScale = new Vector3(0.04f, 0.35f, 0.12f);
            Destroy(bracket.GetComponent<Collider>());

            var bracketMat = new Material(stdShader);
            bracketMat.color = new Color(0.20f, 0.20f, 0.22f);
            bracket.GetComponent<Renderer>().material = bracketMat;

            // Main cylinder (safety red)
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = "ExtinguisherBody";
            cylinder.transform.SetParent(root.transform, false);
            cylinder.transform.localPosition = Vector3.zero;
            cylinder.transform.localScale = new Vector3(0.15f, 0.30f, 0.15f);
            Destroy(cylinder.GetComponent<Collider>());

            var extMat = new Material(stdShader);
            extMat.color = new Color(0.85f, 0.08f, 0.04f); // Fire red
            extMat.SetFloat("_Glossiness", 0.75f);
            cylinder.GetComponent<Renderer>().material = extMat;

            // Rounded top dome
            var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.name = "TopDome";
            dome.transform.SetParent(root.transform, false);
            dome.transform.localPosition = new Vector3(0f, 0.30f, 0f);
            dome.transform.localScale = new Vector3(0.15f, 0.08f, 0.15f);
            Destroy(dome.GetComponent<Collider>());
            dome.GetComponent<Renderer>().material = extMat;

            // Squeeze valve handle (chrome metallic)
            var valve = GameObject.CreatePrimitive(PrimitiveType.Cube);
            valve.name = "ValveHandle";
            valve.transform.SetParent(root.transform, false);
            valve.transform.localPosition = new Vector3(0f, 0.37f, 0f);
            valve.transform.localScale = new Vector3(0.04f, 0.07f, 0.12f);
            Destroy(valve.GetComponent<Collider>());

            var valveMat = new Material(stdShader);
            valveMat.color = new Color(0.75f, 0.75f, 0.78f);
            valveMat.SetFloat("_Metallic", 0.85f);
            valveMat.SetFloat("_Glossiness", 0.85f);
            valve.GetComponent<Renderer>().material = valveMat;

            // Pressure gauge (brass ring)
            var gauge = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            gauge.name = "PressureGauge";
            gauge.transform.SetParent(root.transform, false);
            gauge.transform.localPosition = new Vector3(0.07f, 0.34f, 0f);
            gauge.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            gauge.transform.localScale = new Vector3(0.035f, 0.02f, 0.035f);
            Destroy(gauge.GetComponent<Collider>());

            var gaugeMat = new Material(stdShader);
            gaugeMat.color = new Color(0.15f, 0.80f, 0.25f); // green needle area
            gauge.GetComponent<Renderer>().material = gaugeMat;

            // Discharge hose & nozzle (flexible black tube hanging down side)
            var hose = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hose.name = "DischargeHose";
            hose.transform.SetParent(root.transform, false);
            hose.transform.localPosition = new Vector3(0.08f, 0.08f, 0.05f);
            hose.transform.localRotation = Quaternion.Euler(0f, 0f, -8f);
            hose.transform.localScale = new Vector3(0.025f, 0.24f, 0.025f);
            Destroy(hose.GetComponent<Collider>());

            var hoseMat = new Material(stdShader);
            hoseMat.color = new Color(0.10f, 0.10f, 0.10f);
            hose.GetComponent<Renderer>().material = hoseMat;

            // Certification instruction label band
            var label = GameObject.CreatePrimitive(PrimitiveType.Cube);
            label.name = "ExtinguisherLabel";
            label.transform.SetParent(root.transform, false);
            label.transform.localPosition = new Vector3(0.078f, -0.02f, 0f);
            label.transform.localScale = new Vector3(0.01f, 0.20f, 0.11f);
            Destroy(label.GetComponent<Collider>());

            var labelMat = new Material(stdShader);
            labelMat.color = Color.white;
            label.GetComponent<Renderer>().material = labelMat;

            // Environmental Prop: Wall electrical conduit behind station
            var conduit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            conduit.name = "WallConduit";
            conduit.transform.SetParent(root.transform, false);
            conduit.transform.localPosition = new Vector3(-0.06f, -0.55f, 0.15f);
            conduit.transform.localScale = new Vector3(0.03f, 0.45f, 0.03f);
            Destroy(conduit.GetComponent<Collider>());
            var conduitMat = new Material(stdShader);
            conduitMat.color = new Color(0.32f, 0.32f, 0.35f);
            conduit.GetComponent<Renderer>().material = conduitMat;

            // Dedicated inspection lamp illuminating fire extinguisher
            var lampGO = new GameObject("ExtinguisherInspectionLamp");
            lampGO.transform.SetParent(root.transform, false);
            lampGO.transform.localPosition = new Vector3(0.20f, 0.65f, 0f);
            var lamp = lampGO.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.color = new Color(1.0f, 0.92f, 0.85f);
            lamp.intensity = 5.5f;
            lamp.range = 4.5f;

            return root;
        }

        // ── 3. Mud / Rock Dust Pile Construction (Left Mine Work Area Z = 23m) ─

        private GameObject BuildMudPile(Transform parent, Vector3 localPos)
        {
            var root = new GameObject("Resource_MudPile");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;

            var stdShader = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse");

            // Mound base 1 (broad flat mound)
            var moundBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            moundBase.name = "MoundBase";
            moundBase.transform.SetParent(root.transform, false);
            moundBase.transform.localPosition = new Vector3(0f, 0f, 0f);
            moundBase.transform.localScale = new Vector3(0.65f, 0.08f, 0.65f);
            Destroy(moundBase.GetComponent<Collider>());

            var mudMat = new Material(stdShader);
            mudMat.color = new Color(0.36f, 0.30f, 0.22f); // Mine rock dust & mud
            mudMat.SetFloat("_Glossiness", 0.15f);
            moundBase.GetComponent<Renderer>().material = mudMat;

            // Mound upper dome
            var moundTop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            moundTop.name = "MoundTop";
            moundTop.transform.SetParent(root.transform, false);
            moundTop.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            moundTop.transform.localScale = new Vector3(0.50f, 0.18f, 0.50f);
            Destroy(moundTop.GetComponent<Collider>());
            moundTop.GetComponent<Renderer>().material = mudMat;

            // Mining shovel standing upright in pile
            var shovelShaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shovelShaft.name = "ShovelShaft";
            shovelShaft.transform.SetParent(root.transform, false);
            shovelShaft.transform.localPosition = new Vector3(-0.08f, 0.45f, 0f);
            shovelShaft.transform.localRotation = Quaternion.Euler(12f, 0f, -8f);
            shovelShaft.transform.localScale = new Vector3(0.025f, 0.45f, 0.025f);
            Destroy(shovelShaft.GetComponent<Collider>());

            var woodMat = new Material(stdShader);
            woodMat.color = new Color(0.55f, 0.38f, 0.20f); // wood handle
            shovelShaft.GetComponent<Renderer>().material = woodMat;

            // Shovel blade (steel)
            var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "ShovelBlade";
            blade.transform.SetParent(root.transform, false);
            blade.transform.localPosition = new Vector3(-0.12f, 0.10f, 0f);
            blade.transform.localRotation = Quaternion.Euler(12f, 0f, -8f);
            blade.transform.localScale = new Vector3(0.02f, 0.20f, 0.14f);
            Destroy(blade.GetComponent<Collider>());

            var steelMat = new Material(stdShader);
            steelMat.color = new Color(0.40f, 0.42f, 0.45f);
            blade.GetComponent<Renderer>().material = steelMat;

            // Environmental Prop: Stacked rock dust sacks (used for mine explosion suppression)
            var sack1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sack1.name = "RockDustSack1";
            sack1.transform.SetParent(root.transform, false);
            sack1.transform.localPosition = new Vector3(-0.35f, 0.08f, 0.25f);
            sack1.transform.localRotation = Quaternion.Euler(0f, 15f, 0f);
            sack1.transform.localScale = new Vector3(0.35f, 0.16f, 0.26f);
            Destroy(sack1.GetComponent<Collider>());
            var sackMat = new Material(stdShader);
            sackMat.color = new Color(0.68f, 0.64f, 0.56f); // Burlap stone dust sack
            sack1.GetComponent<Renderer>().material = sackMat;

            var sack2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sack2.name = "RockDustSack2";
            sack2.transform.SetParent(root.transform, false);
            sack2.transform.localPosition = new Vector3(-0.33f, 0.22f, 0.23f);
            sack2.transform.localRotation = Quaternion.Euler(0f, -10f, 0f);
            sack2.transform.localScale = new Vector3(0.32f, 0.14f, 0.24f);
            Destroy(sack2.GetComponent<Collider>());
            sack2.GetComponent<Renderer>().material = sackMat;

            // Environmental Prop: Timber lagging board behind the station
            var lagging = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lagging.name = "TimberLagging";
            lagging.transform.SetParent(root.transform, false);
            lagging.transform.localPosition = new Vector3(-0.42f, 0.55f, 0.05f);
            lagging.transform.localScale = new Vector3(0.05f, 1.10f, 0.85f);
            Destroy(lagging.GetComponent<Collider>());
            var lagMat = new Material(stdShader);
            lagMat.color = new Color(0.35f, 0.24f, 0.14f);
            lagging.GetComponent<Renderer>().material = lagMat;

            // Environmental Prop: Miner's steel toolbox on floor
            var toolbox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            toolbox.name = "MinersToolbox";
            toolbox.transform.SetParent(root.transform, false);
            toolbox.transform.localPosition = new Vector3(0.30f, 0.08f, 0.28f);
            toolbox.transform.localRotation = Quaternion.Euler(0f, -25f, 0f);
            toolbox.transform.localScale = new Vector3(0.30f, 0.16f, 0.18f);
            Destroy(toolbox.GetComponent<Collider>());
            var boxMat = new Material(stdShader);
            boxMat.color = new Color(0.24f, 0.26f, 0.28f);
            toolbox.GetComponent<Renderer>().material = boxMat;

            // Dedicated work lamp illuminating mud / rock dust work area
            var lampGO = new GameObject("MudPileWorkLamp");
            lampGO.transform.SetParent(root.transform, false);
            lampGO.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            var lamp = lampGO.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.color = new Color(1.0f, 0.90f, 0.75f);
            lamp.intensity = 5.0f;
            lamp.range = 4.5f;

            return root;
        }

        // ── Proximity & Contextual Prompts ────────────────────────────────────

        private void UpdateProximityAndPrompts()
        {
            if (virtualCamera == null) return;

            // Prompts must NEVER show at module start or before assessment mode is active
            if (!IsAssessmentActive)
            {
                if (promptCanvasGroup != null)
                {
                    promptCanvasGroup.alpha = 0f;
                    promptCanvasGroup.interactable = false;
                }
                return;
            }

            Vector3 camPos = virtualCamera.transform.position;
            ResourceItem closest = null;
            float minDistance = float.MaxValue;

            foreach (var item in resources)
            {
                if (item == null || item.IsCollected || item.RootGO == null || !item.RootGO.activeInHierarchy) continue;
                float dist = Vector3.Distance(camPos, item.RootGO.transform.position);
                if (dist <= proximityDistance && dist < minDistance)
                {
                    minDistance = dist;
                    closest = item;
                }
            }

            currentClosestItem = closest;
            bool hasTarget = (currentClosestItem != null) && (SelectedResource == ResponseResourceType.None);

            float targetAlpha = hasTarget ? 1f : 0f;
            promptAlpha = Mathf.MoveTowards(promptAlpha, targetAlpha, Time.deltaTime * 4f);

            if (promptCanvasGroup != null)
            {
                promptCanvasGroup.alpha = promptAlpha;
                promptCanvasGroup.interactable = hasTarget;
            }

            if (hasTarget && currentClosestItem != null)
            {
                if (promptTitleText != null)
                    promptTitleText.text = currentClosestItem.Title;

                if (promptActionText != null)
                    promptActionText.text = currentClosestItem.ActionPrompt;

                // Subtle emissive pulse on targeted object
                float pulse = 0.5f + Mathf.PingPong(Time.time * 2.5f, 0.5f);
                HighlightItem(currentClosestItem, pulse);
            }
        }

        private void HighlightItem(ResourceItem item, float intensity)
        {
            if (item == null || item.Renderers == null) return;
            foreach (var r in item.Renderers)
            {
                if (r == null) continue;
                if (r.material.HasProperty("_EmissionColor"))
                {
                    r.material.EnableKeyword("_EMISSION");
                    r.material.SetColor("_EmissionColor", item.BaseColor * intensity * 0.8f);
                }
            }
        }

        // ── Tap to Select ─────────────────────────────────────────────────────

        private void CheckTapToSelect()
        {
            if (SelectedResource != ResponseResourceType.None) return;
            if (currentClosestItem == null || promptAlpha < 0.4f) return;

            bool tapped = false;
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                    tapped = true;
            }
            else if (Input.GetMouseButtonDown(0))
            {
                tapped = true;
            }

            if (tapped)
            {
                SelectResource(currentClosestItem);
            }
        }

        private void SelectResource(ResourceItem item)
        {
            if (item == null || item.IsCollected) return;

            SelectedResource = item.Type;
            item.IsCollected = true;
            Debug.Log("[FireResourceManager] Worker selected resource: " + SelectedResource);

            // 1. Hide/remove the physical 3D object from the environment immediately
            if (item.RootGO != null)
            {
                item.RootGO.SetActive(false);
            }

            // 2. If fire extinguisher selected, visibly attach held extinguisher to player's first-person hand
            if (item.Type == ResponseResourceType.FireExtinguisher)
            {
                ShowHeldExtinguisher();
            }

            // 3. Remove interaction prompt immediately
            promptAlpha = 0f;
            if (promptCanvasGroup != null)
            {
                promptCanvasGroup.alpha = 0f;
                promptCanvasGroup.interactable = false;
            }
            currentClosestItem = null;

            // 4. Trigger event
            OnResourceSelected?.Invoke(SelectedResource);

            // 5. Start clean text indication sequence (NO black card, NO large background panel)
            StartCoroutine(ShowSelectionFeedbackSequence(item.Title));
        }

        public void ShowHeldExtinguisher()
        {
            if (firstPersonHandAnchor == null && virtualCamera != null)
            {
                BuildHeldExtinguisher(virtualCamera.transform);
            }

            if (heldExtinguisherRoot != null)
            {
                heldExtinguisherRoot.SetActive(true);
            }

            CurrentExtinguisherState = ExtinguisherState.PickedUp;
            Debug.Log("[FireResourceManager] 3D Fire Extinguisher is now visibly HELD in worker's lower-right first-person hand.");
        }

        public void HideHeldResource()
        {
            if (heldExtinguisherRoot != null)
            {
                heldExtinguisherRoot.SetActive(false);
            }

            if (CurrentExtinguisherState == ExtinguisherState.PickedUp)
            {
                CurrentExtinguisherState = ExtinguisherState.UsedOnFire;
            }

            Debug.Log("[FireResourceManager] Held resource consumed / hidden.");
        }

        // ── First-Person Hand Extinguisher 3D Model ────────────────────────────

        private void BuildHeldExtinguisher(Transform camTransform)
        {
            if (camTransform == null || firstPersonHandAnchor != null) return;

            // Anchor under first-person camera: positioned lower-right, forward
            firstPersonHandAnchor = new GameObject("FirstPersonHandAnchor");
            firstPersonHandAnchor.transform.SetParent(camTransform, false);
            firstPersonHandAnchor.transform.localPosition = handAnchorBaseLocalPos;
            firstPersonHandAnchor.transform.localRotation = Quaternion.identity;

            heldExtinguisherRoot = new GameObject("HeldFireExtinguisher");
            heldExtinguisherRoot.transform.SetParent(firstPersonHandAnchor.transform, false);
            heldExtinguisherRoot.transform.localPosition = Vector3.zero;
            heldExtinguisherRoot.transform.localRotation = Quaternion.Euler(14f, -18f, 10f);

            var stdShader = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse");

            // Main pressurized cylinder (fire safety red, glossiness)
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "ExtinguisherBody";
            body.transform.SetParent(heldExtinguisherRoot.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.082f, 0.14f, 0.082f);
            Destroy(body.GetComponent<Collider>());

            var extMat = new Material(stdShader);
            extMat.color = new Color(0.88f, 0.08f, 0.04f);
            extMat.SetFloat("_Glossiness", 0.8f);
            body.GetComponent<Renderer>().material = extMat;

            // Rounded top dome
            var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.name = "TopDome";
            dome.transform.SetParent(heldExtinguisherRoot.transform, false);
            dome.transform.localPosition = new Vector3(0f, 0.14f, 0f);
            dome.transform.localScale = new Vector3(0.082f, 0.042f, 0.082f);
            Destroy(dome.GetComponent<Collider>());
            dome.GetComponent<Renderer>().material = extMat;

            // Carrying handle & squeeze lever (chrome/metallic)
            var handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handle.name = "CarryHandle";
            handle.transform.SetParent(heldExtinguisherRoot.transform, false);
            handle.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            handle.transform.localScale = new Vector3(0.024f, 0.038f, 0.068f);
            Destroy(handle.GetComponent<Collider>());

            var chromeMat = new Material(stdShader);
            chromeMat.color = new Color(0.82f, 0.82f, 0.85f);
            chromeMat.SetFloat("_Metallic", 0.85f);
            chromeMat.SetFloat("_Glossiness", 0.85f);
            handle.GetComponent<Renderer>().material = chromeMat;

            var lever = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lever.name = "SqueezeLever";
            lever.transform.SetParent(heldExtinguisherRoot.transform, false);
            lever.transform.localPosition = new Vector3(0f, 0.208f, -0.012f);
            lever.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            lever.transform.localScale = new Vector3(0.020f, 0.014f, 0.062f);
            Destroy(lever.GetComponent<Collider>());
            lever.GetComponent<Renderer>().material = chromeMat;

            // Brass pressure gauge with green safe zone
            var gauge = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            gauge.name = "PressureGauge";
            gauge.transform.SetParent(heldExtinguisherRoot.transform, false);
            gauge.transform.localPosition = new Vector3(0.040f, 0.165f, 0f);
            gauge.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            gauge.transform.localScale = new Vector3(0.022f, 0.012f, 0.022f);
            Destroy(gauge.GetComponent<Collider>());

            var gaugeMat = new Material(stdShader);
            gaugeMat.color = new Color(0.15f, 0.85f, 0.25f);
            gauge.GetComponent<Renderer>().material = gaugeMat;

            // Flexible discharge hose & flared horn nozzle
            var hose = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hose.name = "DischargeHose";
            hose.transform.SetParent(heldExtinguisherRoot.transform, false);
            hose.transform.localPosition = new Vector3(0.044f, 0.07f, 0.032f);
            hose.transform.localRotation = Quaternion.Euler(15f, 0f, -12f);
            hose.transform.localScale = new Vector3(0.015f, 0.11f, 0.015f);
            Destroy(hose.GetComponent<Collider>());

            var hoseMat = new Material(stdShader);
            hoseMat.color = new Color(0.12f, 0.12f, 0.12f);
            hose.GetComponent<Renderer>().material = hoseMat;

            var nozzle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            nozzle.name = "DischargeNozzle";
            nozzle.transform.SetParent(heldExtinguisherRoot.transform, false);
            nozzle.transform.localPosition = new Vector3(0.048f, -0.035f, 0.045f);
            nozzle.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
            nozzle.transform.localScale = new Vector3(0.022f, 0.045f, 0.022f);
            Destroy(nozzle.GetComponent<Collider>());
            nozzle.GetComponent<Renderer>().material = hoseMat;

            // White certification & instruction label
            var label = GameObject.CreatePrimitive(PrimitiveType.Cube);
            label.name = "ExtinguisherLabel";
            label.transform.SetParent(heldExtinguisherRoot.transform, false);
            label.transform.localPosition = new Vector3(0.042f, -0.005f, 0f);
            label.transform.localScale = new Vector3(0.005f, 0.095f, 0.062f);
            Destroy(label.GetComponent<Collider>());

            var labelMat = new Material(stdShader);
            labelMat.color = new Color(0.96f, 0.96f, 0.96f);
            label.GetComponent<Renderer>().material = labelMat;

            // Worker hand grip cuff (slate dark leather)
            var glove = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glove.name = "WorkerGloveGrip";
            glove.transform.SetParent(heldExtinguisherRoot.transform, false);
            glove.transform.localPosition = new Vector3(-0.040f, 0.015f, -0.015f);
            glove.transform.localScale = new Vector3(0.036f, 0.085f, 0.052f);
            Destroy(glove.GetComponent<Collider>());

            var gloveMat = new Material(stdShader);
            gloveMat.color = new Color(0.20f, 0.22f, 0.25f);
            glove.GetComponent<Renderer>().material = gloveMat;

            // Initially inactive until picked up
            heldExtinguisherRoot.SetActive(false);
            Debug.Log("[FireResourceManager] FirstPersonHandAnchor & 3D HeldFireExtinguisher created under VirtualCamera.");
        }

        private IEnumerator ShowSelectionFeedbackSequence(string resourceTitle)
        {
            if (toastText == null || toastCanvasGroup == null) yield break;

            // Clean text indication: e.g. "Fire Extinguisher selected"
            toastText.text = $"{resourceTitle} selected";
            toastText.color = new Color(0.92f, 0.96f, 1.0f);
            toastCanvasGroup.alpha = 1f;

            yield return new WaitForSeconds(2.0f);

            // Smooth fade out
            float elapsed = 0f;
            while (elapsed < 0.8f)
            {
                elapsed += Time.deltaTime;
                toastCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.8f);
                yield return null;
            }
            toastCanvasGroup.alpha = 0f;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void SetReferences(
            MineMovementController movementCtrl,
            Camera cam,
            Transform origin,
            Text titleTxt,
            Text actionTxt,
            CanvasGroup promptGroup,
            Text toastTxt,
            CanvasGroup toastGroup)
        {
            movementController = movementCtrl;
            virtualCamera = cam;
            mineOrigin = origin;
            promptTitleText = titleTxt;
            promptActionText = actionTxt;
            promptCanvasGroup = promptGroup;
            toastText = toastTxt;
            toastCanvasGroup = toastGroup;

            if (virtualCamera != null && firstPersonHandAnchor == null)
            {
                BuildHeldExtinguisher(virtualCamera.transform);
            }
        }
    }
}
