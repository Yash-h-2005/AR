using UnityEngine;
using UnityEngine.UI;
using SurakshaAR.UI.Navigation;
using SurakshaAR.Localization;

namespace SurakshaAR.UI.Components
{
    /// <summary>
    /// Modern 4-Tab Bottom Navigation Bar (HOME, MODULES, CERTIFICATE, PROFILE)
    /// Highlighted in Safety Orange (#EA580C / #F97316) for active screen.
    /// </summary>
    public class BottomNavigation : MonoBehaviour
    {
        [Header("Nav Buttons")]
        [SerializeField] private Button homeButton;
        [SerializeField] private Button modulesButton;
        [SerializeField] private Button certificateButton;
        [SerializeField] private Button profileButton;

        [Header("Highlight Colors")]
        [SerializeField] private Color activeColor = new Color(0.97f, 0.45f, 0.09f, 1f); // Safety Orange #F97316
        [SerializeField] private Color inactiveColor = new Color(0.58f, 0.64f, 0.72f, 1f); // Slate Gray #94A3B8

        [Header("Active Screen")]
        [SerializeField] private ScreenState currentActiveScreen = ScreenState.Home;

        private void Start()
        {
            WireButtons();
            HighlightActiveTab();
        }

        private void WireButtons()
        {
            if (homeButton != null)
                homeButton.onClick.AddListener(() => Navigate(ScreenState.Home));

            if (modulesButton != null)
                modulesButton.onClick.AddListener(() => Navigate(ScreenState.TrainingModules));

            if (certificateButton != null)
                certificateButton.onClick.AddListener(() => Navigate(ScreenState.CertificatePreview));

            if (profileButton != null)
                profileButton.onClick.AddListener(() => Navigate(ScreenState.Profile));
        }

        private void Navigate(ScreenState targetScreen)
        {
            if (SceneNavigator.Instance != null && SceneNavigator.Instance.CurrentScreen != targetScreen)
            {
                SceneNavigator.Instance.NavigateTo(targetScreen);
            }
        }

        private void HighlightActiveTab()
        {
            if (SceneNavigator.Instance != null)
            {
                currentActiveScreen = SceneNavigator.Instance.CurrentScreen;
            }

            SetButtonColor(homeButton, currentActiveScreen == ScreenState.Home);
            SetButtonColor(modulesButton, currentActiveScreen == ScreenState.TrainingModules || currentActiveScreen == ScreenState.FireModuleIntro);
            SetButtonColor(certificateButton, currentActiveScreen == ScreenState.CertificatePreview);
            SetButtonColor(profileButton, currentActiveScreen == ScreenState.Profile || currentActiveScreen == ScreenState.Settings);
        }

        private void SetButtonColor(Button btn, bool isActive)
        {
            if (btn == null) return;
            Text btnText = btn.GetComponentInChildren<Text>();
            if (btnText != null)
            {
                btnText.color = isActive ? activeColor : inactiveColor;
                btnText.fontStyle = isActive ? FontStyle.Bold : FontStyle.Normal;
            }
        }
    }
}
