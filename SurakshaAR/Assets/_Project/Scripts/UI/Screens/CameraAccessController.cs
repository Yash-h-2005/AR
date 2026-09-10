using UnityEngine;
using UnityEngine.UI;
using SurakshaAR.UI.Navigation;
using SurakshaAR.Localization;

namespace SurakshaAR.UI.Screens
{
    public class CameraAccessController : MonoBehaviour
    {
        [Header("UI Controls")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button allowCameraButton;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descText;
        [SerializeField] private Text allowButtonText;

        private void OnEnable()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
            }
        }

        private void OnDisable()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
            }
        }

        private void Start()
        {
            RefreshLocalization();

            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
            }

            if (allowCameraButton != null)
            {
                allowCameraButton.onClick.AddListener(OnAllowCameraClicked);
            }
        }

        private void HandleLanguageChanged(Language lang)
        {
            RefreshLocalization();
        }

        private void RefreshLocalization()
        {
            var loc = LocalizationManager.Instance;
            if (loc == null) return;

            if (titleText != null)
            {
                titleText.text = loc.GetString("CAMERA_ACCESS");
            }

            if (descText != null)
            {
                descText.text = loc.GetString("CAMERA_ACCESS_DESC");
            }

            if (allowButtonText != null)
            {
                allowButtonText.text = loc.GetString("ALLOW_CAMERA");
            }
        }

        private void OnBackClicked()
        {
            if (SceneNavigator.Instance != null)
            {
                SceneNavigator.Instance.GoBack();
            }
        }

        private void OnAllowCameraClicked()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
            }
#endif
            ProceedToNotice();
        }

        private void ProceedToNotice()
        {
            if (SceneNavigator.Instance != null)
            {
                SceneNavigator.Instance.NavigateTo(ScreenState.TrainingNotice);
            }
        }
    }
}
