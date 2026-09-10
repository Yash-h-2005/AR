using UnityEngine;
using UnityEngine.UI;
using SurakshaAR.UI.Navigation;

namespace SurakshaAR.UI.Screens
{
    public class GasModuleIntroController : MonoBehaviour
    {
        [Header("Action Buttons")]
        [SerializeField] private Button learnModeButton;
        [SerializeField] private Button simulationButton;
        [SerializeField] private Button backButton;

        private void Start()
        {
            if (learnModeButton != null)
            {
                learnModeButton.onClick.AddListener(OnLearnModeClicked);
            }

            if (simulationButton != null)
            {
                simulationButton.onClick.AddListener(OnSimulationModeClicked);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
            }
        }

        private void OnLearnModeClicked()
        {
            if (SceneNavigator.Instance != null)
            {
                SceneNavigator.Instance.NavigateTo(ScreenState.GasARLearnPlaceholder);
            }
        }

        private void OnSimulationModeClicked()
        {
            if (SceneNavigator.Instance != null)
            {
                SceneNavigator.Instance.NavigateTo(ScreenState.GasARLearnPlaceholder);
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
