using UnityEngine;
using UnityEngine.UI;
using SurakshaAR.UI.Navigation;

namespace SurakshaAR.UI.Screens
{
    public class FireModuleIntroController : MonoBehaviour
    {
        [Header("Action Buttons")]
        [SerializeField] private Button learnModeButton;
        [SerializeField] private Button simulationButton;
        [SerializeField] private Button backButton;

        private void Start()
        {
            if (learnModeButton != null)
                learnModeButton.onClick.AddListener(OnLearnClicked);

            if (simulationButton != null)
                simulationButton.onClick.AddListener(OnSimulationClicked);

            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);
        }

        private void OnLearnClicked()
        {
            // Set active sub-module ID for the completion screen
            SurakshaAR.Training.Fire.SubModuleCompletionScreen.ActiveSubModuleId = "M1_S1";

            if (SceneNavigator.Instance != null)
            {
                // Fire Module Phase 1: Enter spatial-tracking virtual mine simulator.
                SceneNavigator.Instance.NavigateTo(ScreenState.FireMineSimulator);
            }
        }

        private void OnSimulationClicked()
        {
            if (SceneNavigator.Instance != null)
            {
                SceneNavigator.Instance.NavigateTo(ScreenState.ARSimulationPlaceholder);
            }
        }

        private void OnBackClicked()
        {
            if (SceneNavigator.Instance != null)
            {
                SceneNavigator.Instance.GoBack();
            }
        }
    }
}
