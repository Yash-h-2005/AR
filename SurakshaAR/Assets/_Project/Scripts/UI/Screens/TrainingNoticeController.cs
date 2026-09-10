using UnityEngine;
using UnityEngine.UI;
using SurakshaAR.UI.Navigation;

namespace SurakshaAR.UI.Screens
{
    public class TrainingNoticeController : MonoBehaviour
    {
        [Header("UI Controls")]
        [SerializeField] private Button backButton;
        [SerializeField] private Toggle understandToggle;
        [SerializeField] private Button continueButton;
        [SerializeField] private Image continueButtonImage;

        private static readonly Color EnabledOrange = new Color(0.95f, 0.42f, 0.13f, 1f); // #F26B21
        private static readonly Color DisabledGray = new Color(0.6f, 0.65f, 0.7f, 1f);

        private void Start()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
            }

            if (understandToggle != null)
            {
                understandToggle.onValueChanged.AddListener(OnToggleChanged);
                OnToggleChanged(understandToggle.isOn);
            }
            else
            {
                SetContinueEnabled(false);
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
            }
        }

        private void OnToggleChanged(bool isOn)
        {
            SetContinueEnabled(isOn);
        }

        private void SetContinueEnabled(bool enabled)
        {
            if (continueButton != null)
            {
                continueButton.interactable = enabled;
            }

            if (continueButtonImage != null)
            {
                continueButtonImage.color = enabled ? EnabledOrange : DisabledGray;
            }
        }

        private void OnBackClicked()
        {
            if (SceneNavigator.Instance != null)
            {
                SceneNavigator.Instance.GoBack();
            }
        }

        private void OnContinueClicked()
        {
            if (understandToggle != null && !understandToggle.isOn)
            {
                return;
            }

            if (SceneNavigator.Instance != null)
            {
                SceneNavigator.Instance.NavigateTo(ScreenState.Home);
            }
        }
    }
}
