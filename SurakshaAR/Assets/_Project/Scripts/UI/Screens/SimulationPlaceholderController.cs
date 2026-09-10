using UnityEngine;
using UnityEngine.UI;
using SurakshaAR.UI.Navigation;

namespace SurakshaAR.UI.Screens
{
    public class SimulationPlaceholderController : MonoBehaviour
    {
        [Header("HUD Controls")]
        [SerializeField] private Button exitSimulationButton;
        [SerializeField] private Text statusText;

        private void Start()
        {
            if (statusText != null)
                statusText.text = "FIRE & EXPLOSION RESPONSE — SIMULATION\nComplete the safety procedure using the AR environment.";

            if (exitSimulationButton != null)
                exitSimulationButton.onClick.AddListener(OnExitSimulationClicked);
        }

        private void OnExitSimulationClicked()
        {
            if (SceneNavigator.Instance != null)
            {
                SceneNavigator.Instance.NavigateTo(ScreenState.Result);
            }
        }
    }
}
