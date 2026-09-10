using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SurakshaAR.AR;

namespace SurakshaAR.UI
{
    /// <summary>
    /// Manages minimal HUD overlay UI for Phase 2 AR prototype: 
    /// displays status text, scan instructions, placement alerts, and error messages.
    /// </summary>
    public class ARUIController : MonoBehaviour
    {
        [Header("UI Text Components")]
        [SerializeField] private Text headerTitleText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text alertBannerText;

        [Header("AR Controller References")]
        [SerializeField] private ARSessionController sessionController;
        [SerializeField] private ARPlaneController planeController;
        [SerializeField] private ARRaycastPlacementController raycastController;
        [SerializeField] private ARObjectPlacementController placementController;

        private Coroutine alertCoroutine;

        private void Start()
        {
            if (headerTitleText != null)
            {
                headerTitleText.text = "SurakshaAR";
            }

            SetStatusText("Scanning for surface...");

            WireEvents();
        }

        private void WireEvents()
        {
            if (sessionController != null)
            {
                sessionController.OnErrorEncountered += ShowErrorAlert;
            }

            if (planeController != null)
            {
                planeController.OnFirstPlaneDetected += HandleFirstPlaneDetected;
            }

            if (raycastController != null)
            {
                raycastController.OnInvalidTap += HandleInvalidTap;
            }

            if (placementController != null)
            {
                placementController.OnObjectPlacedOrMoved += HandleObjectPlaced;
            }
        }

        private void HandleFirstPlaneDetected()
        {
            if (placementController != null && !placementController.IsObjectPlaced)
            {
                SetStatusText("Tap a surface to place");
            }
        }

        private void HandleObjectPlaced(Vector3 position)
        {
            SetStatusText("AR object placed");
            ShowAlert("AR TEST OBJECT placed on surface");
        }

        private void HandleInvalidTap()
        {
            ShowAlert("Place on a detected surface");
        }

        private void ShowErrorAlert(string errorMessage)
        {
            SetStatusText("AR Error");
            ShowAlert(errorMessage);
        }

        public void SetStatusText(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        public void ShowAlert(string alertMessage)
        {
            if (alertBannerText != null)
            {
                alertBannerText.text = alertMessage;
                alertBannerText.gameObject.SetActive(true);

                if (alertCoroutine != null)
                {
                    StopCoroutine(alertCoroutine);
                }
                alertCoroutine = StartCoroutine(HideAlertAfterDelay(2.5f));
            }
        }

        private IEnumerator HideAlertAfterDelay(float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            if (alertBannerText != null)
            {
                alertBannerText.gameObject.SetActive(false);
            }
        }
    }
}
