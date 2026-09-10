using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SurakshaAR.Training.Fire
{
    /// <summary>
    /// PHASE 2B-2 — Fire Response Evaluator.
    ///
    /// After the worker selects a resource (Phase 2B-1), this handles:
    ///   1. Proximity detection near fire + "USE ON FIRE" tap prompt.
    ///   2. Deterministic evaluation: Extinguisher = correct, Water/Mud = unsafe.
    ///   3. Correct response => progressive extinguishing + "Fire Controlled" success.
    ///   4. Wrong response  => fire flare-up, -20 HP, haptic, warning, retry allowed.
    ///   5. Assessment metrics recording via FireAssessmentRecord.
    ///   6. No HP drain over time; HP only decreases on wrong tap.
    /// </summary>
    public class FireResponseEvaluator : MonoBehaviour
    {
        // ── Inspector References ─────────────────────────────────────────────

        [Header("Phase 2B-2 — Required References")]
        [SerializeField] private FireHazardController fireHazardController;
        [SerializeField] private FireResponseResourceManager resourceManager;
        [SerializeField] private Camera virtualCamera;

        [Header("Use-on-Fire Prompt UI")]
        [SerializeField] private Text usePromptTitleText;
        [SerializeField] private Text usePromptActionText;
        [SerializeField] private CanvasGroup usePromptCanvasGroup;

        [Header("Outcome Toast UI")]
        [SerializeField] private Text outcomeToastText;
        [SerializeField] private CanvasGroup outcomeToastCanvasGroup;

        [Header("HP UI (synced with MineSimulatorHUD)")]
        [SerializeField] private Text hpText;

        [Header("Settings")]
        [Tooltip("Distance from camera to fire within which Use-on-Fire prompt appears.")]
        [SerializeField] private float useOnFireDistance = 3.5f;
        [SerializeField] private int hpPerWrongAttempt = 20;
        [SerializeField] private float extinguishDuration = 3.5f;
        [SerializeField] private float flareDuration = 2.5f;

        // ── Events ────────────────────────────────────────────────────────
        public event Action OnFireControlled;
        public event Action<int> OnHPChanged;

        // ── Assessment Metrics ────────────────────────────────────────────
        public FireAssessmentRecord AssessmentRecord { get; private set; } = new FireAssessmentRecord();

        // ── State ─────────────────────────────────────────────────────────
        private int currentHP = 100;
        private bool evaluationActive = false;
        private bool outcomeProcessing = false;
        private bool fireControlled = false;
        private float responseStartTime = 0f;

        /// <summary>
        /// Determines which resource is the correct response.
        /// Set by the scene builder based on the fire scenario.
        /// </summary>
        public FireScenarioType ScenarioType { get; set; } = FireScenarioType.ElectricalEquipment;

        private float usePromptAlpha = 0f;
        private Vector3 fireWorldPos = Vector3.zero;

        // ── Lifecycle ──────────────────────────────────────────────────────

        private void Start()
        {
            if (resourceManager != null)
                resourceManager.OnResourceSelected += HandleResourceSelected;

            if (fireHazardController != null)
            {
                fireHazardController.OnFireExtinguished += HandleFireExtinguished;
                fireHazardController.OnFireFlaredUp     += HandleFlareUpComplete;
            }

            if (usePromptCanvasGroup != null)
            {
                usePromptCanvasGroup.alpha = 0f;
            }

            if (outcomeToastCanvasGroup != null)
            {
                outcomeToastCanvasGroup.alpha = 0f;
                var img = outcomeToastCanvasGroup.GetComponent<Image>();
                if (img != null) img.enabled = false;
            }

            UpdateHPText();
        }

        private void OnDestroy()
        {
            if (resourceManager != null)
                resourceManager.OnResourceSelected -= HandleResourceSelected;

            if (fireHazardController != null)
            {
                fireHazardController.OnFireExtinguished -= HandleFireExtinguished;
                fireHazardController.OnFireFlaredUp     -= HandleFlareUpComplete;
            }
        }

        private void Update()
        {
            if (!evaluationActive || fireControlled || outcomeProcessing) return;
            UpdateUseOnFirePrompt();
            CheckUseOnFireTap();
        }

        // ── Resource Selection ────────────────────────────────────────────

        private void HandleResourceSelected(ResponseResourceType resource)
        {
            evaluationActive  = true;
            responseStartTime = Time.time;

            AssessmentRecord.selectedResource    = resource;
            AssessmentRecord.scenarioType        = ScenarioType;
            AssessmentRecord.responseStartTimeMs = (long)(Time.realtimeSinceStartup * 1000);

            if (fireHazardController != null)
                fireWorldPos = fireHazardController.GetFireWorldPosition();

            Debug.Log("[Evaluator] Resource selected: " + resource + ". Approach fire to use it.");
        }

        // ── Use-on-Fire Proximity Prompt ──────────────────────────────────

        private void UpdateUseOnFirePrompt()
        {
            if (virtualCamera == null || resourceManager == null) return;
            if (resourceManager.SelectedResource == ResponseResourceType.None) return;

            float dist = Vector3.Distance(virtualCamera.transform.position, fireWorldPos);
            bool inRange = dist <= useOnFireDistance;

            float targetAlpha = inRange ? 1f : 0f;
            usePromptAlpha = Mathf.MoveTowards(usePromptAlpha, targetAlpha, Time.deltaTime * 5f);

            if (usePromptCanvasGroup != null)
            {
                usePromptCanvasGroup.alpha = usePromptAlpha;
                usePromptCanvasGroup.interactable = inRange;
            }

            if (inRange)
            {
                string rName = GetResourceDisplayName(resourceManager.SelectedResource);
                if (usePromptTitleText  != null) usePromptTitleText.text  = "🔥 " + rName + " Ready";
                if (usePromptActionText != null) usePromptActionText.text = "TAP TO USE ON FIRE";
            }
        }

        // ── Tap Interaction ───────────────────────────────────────────────

        private void CheckUseOnFireTap()
        {
            if (usePromptAlpha < 0.5f) return;

            bool tapped = false;
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began) tapped = true;
            }
            else if (Input.GetMouseButtonDown(0))
            {
                tapped = true;
            }

            if (tapped)
                EvaluateResponse(resourceManager.SelectedResource);
        }

        // ── Evaluation ────────────────────────────────────────────────────

        private void EvaluateResponse(ResponseResourceType resource)
        {
            outcomeProcessing = true;
            usePromptAlpha = 0f;
            if (usePromptCanvasGroup != null) usePromptCanvasGroup.alpha = 0f;

            bool isCorrect = false;
            switch (ScenarioType)
            {
                case FireScenarioType.ElectricalEquipment:
                    isCorrect = (resource == ResponseResourceType.FireExtinguisher);
                    break;
                case FireScenarioType.LiquidFuel:
                    isCorrect = (resource == ResponseResourceType.MudPile);
                    break;
                case FireScenarioType.OrdinaryCombustibles:
                    isCorrect = (resource == ResponseResourceType.WaterContainer);
                    break;
            }
            Debug.Log($"[Evaluator] Evaluated — {resource} | Scenario: {ScenarioType} | Correct: {isCorrect}");

            if (isCorrect)
                HandleCorrectResponse(resource);
            else
                HandleWrongResponse(resource);
        }

        // ── Correct Response ──────────────────────────────────────────────

        private void HandleCorrectResponse(ResponseResourceType resource)
        {
            float responseTime = Time.time - responseStartTime;
            AssessmentRecord.responseCorrect = true;
            AssessmentRecord.fireControlled  = true;
            AssessmentRecord.responseTimeS   = responseTime;

            Debug.Log($"[Evaluator] CORRECT — {resource}. Extinguishing. Time: {responseTime:F1}s");
            Debug.Log("[Evaluator] RECORD: " + AssessmentRecord.ToString());

            // Consume / hide the held extinguisher from the worker's hand as extinguishing begins
            if (resourceManager != null)
                resourceManager.HideHeldResource();

            if (fireHazardController != null)
                fireHazardController.ExtinguishFire(extinguishDuration);

            StartCoroutine(ShowExtinguishingSequence(resource));
        }

        private IEnumerator ShowExtinguishingSequence(ResponseResourceType resource)
        {
            string applyMsg = (resource == ResponseResourceType.MudPile) ? "Applying rock dust & mud..." : "Applying extinguisher...";
            ShowOutcomeToast(applyMsg, new Color(0.2f, 0.85f, 1.0f));
            yield return new WaitForSeconds(extinguishDuration * 0.55f);
            ShowOutcomeToast("Smothering flames...", new Color(0.2f, 0.85f, 1.0f));
            // Fire extinguished event drives final message
        }

        private void HandleFireExtinguished()
        {
            fireControlled = true;
            Debug.Log("[Evaluator] 🟢 FIRE CONTROLLED.");
            ShowOutcomeToast("Fire extinguished.", new Color(0.15f, 0.95f, 0.45f));
            StartCoroutine(ShowSuccessAndFinish());
            OnFireControlled?.Invoke();
        }

        private IEnumerator ShowSuccessAndFinish()
        {
            yield return new WaitForSeconds(2.5f);
            float e = 0f;
            while (e < 1.0f)
            {
                e += Time.deltaTime;
                if (outcomeToastCanvasGroup != null)
                    outcomeToastCanvasGroup.alpha = Mathf.Lerp(1f, 0f, e / 1.0f);
                yield return null;
            }
            if (outcomeToastCanvasGroup != null) outcomeToastCanvasGroup.alpha = 0f;
        }

        // ── Wrong Response ────────────────────────────────────────────────

        private void HandleWrongResponse(ResponseResourceType resource)
        {
            AssessmentRecord.wrongAttempts++;
            currentHP = Mathf.Max(0, currentHP - hpPerWrongAttempt);
            AssessmentRecord.hpLost += hpPerWrongAttempt;
            UpdateHPText();
            OnHPChanged?.Invoke(currentHP);

            TriggerHaptic();

            string dangerMsg = GetWrongResponseMessage(resource);
            Debug.Log($"[Evaluator] ❌ WRONG — {resource}. HP: {currentHP}. Attempt #{AssessmentRecord.wrongAttempts}.");

            if (fireHazardController != null)
                fireHazardController.FireFlareUp(flareDuration);

            ShowOutcomeToast("⚠️ UNSAFE RESPONSE\n" + dangerMsg, new Color(1.0f, 0.25f, 0.15f));
            StartCoroutine(WrongResponseSequence());
        }

        private IEnumerator WrongResponseSequence()
        {
            yield return new WaitForSeconds(flareDuration * 0.4f);
            ShowOutcomeToast("🔥 Fire intensified!\nChoose a correct response.", new Color(1.0f, 0.55f, 0.10f));
            yield return new WaitForSeconds(flareDuration * 0.8f);

            // Allow retry
            outcomeProcessing = false;

            yield return new WaitForSeconds(1.5f);

            // Fade toast
            float e = 0f;
            while (e < 1.2f)
            {
                e += Time.deltaTime;
                if (outcomeToastCanvasGroup != null)
                    outcomeToastCanvasGroup.alpha = Mathf.Lerp(1f, 0f, e / 1.2f);
                yield return null;
            }
            if (outcomeToastCanvasGroup != null) outcomeToastCanvasGroup.alpha = 0f;

            // Let worker pick another resource
            if (resourceManager != null)
                resourceManager.ActivateRetry();

            Debug.Log("[Evaluator] Retry activated — worker can select another resource.");
        }

        private void HandleFlareUpComplete()
        {
            Debug.Log("[Evaluator] Flare-up animation complete.");
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private void ShowOutcomeToast(string message, Color color)
        {
            if (outcomeToastText != null)
            {
                outcomeToastText.text  = message;
                outcomeToastText.color = color;
            }
            if (outcomeToastCanvasGroup != null)
                outcomeToastCanvasGroup.alpha = 1f;
        }

        private void UpdateHPText()
        {
            if (hpText != null)
                hpText.text = "HP: " + currentHP;
        }

        private void TriggerHaptic()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try { Handheld.Vibrate(); } catch { }
#else
            Debug.Log("[Evaluator] Haptic triggered (editor simulation).");
#endif
        }

        private string GetResourceDisplayName(ResponseResourceType r)
        {
            switch (r)
            {
                case ResponseResourceType.FireExtinguisher: return "Fire Extinguisher";
                case ResponseResourceType.WaterContainer:  return "Water Container";
                case ResponseResourceType.MudPile:         return "Rock Dust Pile";
                default:                                   return "Resource";
            }
        }

        private string GetWrongResponseMessage(ResponseResourceType r)
        {
            if (ScenarioType == FireScenarioType.LiquidFuel)
            {
                // Petroleum fire: mud is correct
                switch (r)
                {
                    case ResponseResourceType.WaterContainer:
                        return "DANGER! Water spreads\npetroleum fires!";
                    case ResponseResourceType.FireExtinguisher:
                        return "CO2 extinguisher is ineffective\non large petroleum fires.\nUse rock dust/mud.";
                    default:
                        return "This resource is unsafe for this fire type.";
                }
            }
            // Electrical fire: extinguisher is correct
            switch (r)
            {
                case ResponseResourceType.WaterContainer:
                    return "Water conducts electricity!\nNEVER use water on electrical fires.";
                case ResponseResourceType.MudPile:
                    return "Rock dust is ineffective\non electrical fires.";
                default:
                    return "This resource is unsafe for this fire type.";
            }
        }

        // ── Public API ────────────────────────────────────────────────────

        public void SetReferences(
            FireHazardController fireCtrl,
            FireResponseResourceManager resMgr,
            Camera cam,
            Text useTitle, Text useAction, CanvasGroup useGroup,
            Text toastTxt, CanvasGroup toastGroup,
            Text hpTxt)
        {
            // Unsubscribe existing
            if (resourceManager != null)
                resourceManager.OnResourceSelected -= HandleResourceSelected;
            if (fireHazardController != null)
            {
                fireHazardController.OnFireExtinguished -= HandleFireExtinguished;
                fireHazardController.OnFireFlaredUp     -= HandleFlareUpComplete;
            }

            fireHazardController    = fireCtrl;
            resourceManager         = resMgr;
            virtualCamera           = cam;
            usePromptTitleText      = useTitle;
            usePromptActionText     = useAction;
            usePromptCanvasGroup    = useGroup;
            outcomeToastText        = toastTxt;
            outcomeToastCanvasGroup = toastGroup;
            hpText                  = hpTxt;

            // Re-subscribe
            if (resourceManager != null)
                resourceManager.OnResourceSelected += HandleResourceSelected;
            if (fireHazardController != null)
            {
                fireHazardController.OnFireExtinguished += HandleFireExtinguished;
                fireHazardController.OnFireFlaredUp     += HandleFlareUpComplete;
            }
        }

        public int CurrentHP => currentHP;
    }

    // ── Assessment Record ─────────────────────────────────────────────────────

    [Serializable]
    public class FireAssessmentRecord
    {
        public FireScenarioType     scenarioType        = FireScenarioType.ElectricalEquipment;
        public ResponseResourceType selectedResource    = ResponseResourceType.None;
        public bool                 responseCorrect     = false;
        public int                  wrongAttempts       = 0;
        public int                  hpLost              = 0;
        public float                responseTimeS       = 0f;
        public bool                 fireControlled      = false;
        public long                 responseStartTimeMs = 0L;

        public override string ToString() =>
            $"Scenario={scenarioType} | Resource={selectedResource} | Correct={responseCorrect} | " +
            $"WrongAttempts={wrongAttempts} | HPLost={hpLost} | Time={responseTimeS:F1}s | Controlled={fireControlled}";
    }
}
