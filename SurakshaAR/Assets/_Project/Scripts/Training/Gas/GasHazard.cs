using UnityEngine;

namespace SurakshaAR.Training.Gas
{
    public class GasHazard : MonoBehaviour
    {
        [SerializeField] private SimulatedGasLevel currentGasLevel = SimulatedGasLevel.Low;
        public SimulatedGasLevel CurrentGasLevel => currentGasLevel;

        [SerializeField] private Renderer cloudRenderer;
        [SerializeField] private Light hazardWarningLight;

        private void Awake()
        {
            if (cloudRenderer == null)
            {
                cloudRenderer = GetComponent<Renderer>();
            }
        }

        public void SetGasLevel(SimulatedGasLevel level)
        {
            currentGasLevel = level;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (cloudRenderer != null && cloudRenderer.material != null)
            {
                Color targetColor;
                float scaleMultiplier;

                switch (currentGasLevel)
                {
                    case SimulatedGasLevel.Safe:
                        targetColor = new Color(0.1f, 0.8f, 0.2f, 0.1f);
                        scaleMultiplier = 0.5f;
                        break;
                    case SimulatedGasLevel.Low:
                        targetColor = new Color(0.9f, 0.7f, 0.1f, 0.35f);
                        scaleMultiplier = 1.0f;
                        break;
                    case SimulatedGasLevel.Medium:
                        targetColor = new Color(0.95f, 0.45f, 0.1f, 0.55f);
                        scaleMultiplier = 1.5f;
                        break;
                    case SimulatedGasLevel.High:
                    case SimulatedGasLevel.Critical:
                        targetColor = new Color(0.9f, 0.1f, 0.1f, 0.8f);
                        scaleMultiplier = 2.2f;
                        break;
                    default:
                        targetColor = new Color(0.9f, 0.7f, 0.1f, 0.35f);
                        scaleMultiplier = 1.0f;
                        break;
                }

                cloudRenderer.material.color = targetColor;
                transform.localScale = Vector3.one * scaleMultiplier;
            }

            if (hazardWarningLight != null)
            {
                hazardWarningLight.enabled = (currentGasLevel >= SimulatedGasLevel.Medium);
                hazardWarningLight.color = (currentGasLevel >= SimulatedGasLevel.High) ? Color.red : Color.yellow;
            }
        }
    }
}
