using UnityEngine;
using UnityEngine.UI;
using SurakshaAR.Localization;
using SurakshaAR.UI.Navigation;
using SurakshaAR.UI.Data;

namespace SurakshaAR.UI.Screens
{
    public class LanguageSelectionController : MonoBehaviour
    {
        [Header("UI Controls")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button englishButton;
        [SerializeField] private Button hindiButton;
        [SerializeField] private Button santaliButton;
        [SerializeField] private Button continueButton;

        [Header("Texts for Localization")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private Text continueButtonText;

        [Header("Card Outlines / Highlights")]
        [SerializeField] private Outline englishOutline;
        [SerializeField] private Outline hindiOutline;
        [SerializeField] private Outline santaliOutline;

        [Header("Checkmark Indicators")]
        [SerializeField] private GameObject englishCheckmark;
        [SerializeField] private GameObject hindiCheckmark;
        [SerializeField] private GameObject santaliCheckmark;

        private static readonly Color SelectedOrange = new Color(0.95f, 0.42f, 0.13f, 1f); // #F26B21
        private static readonly Color UnselectedBorder = new Color(0.85f, 0.88f, 0.92f, 1f);

        private Language selectedLanguage = Language.English;

        private void Start()
        {
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
            if (englishButton != null) englishButton.onClick.AddListener(() => OnLanguagePicked(Language.English));
            if (hindiButton != null) hindiButton.onClick.AddListener(() => OnLanguagePicked(Language.Hindi));
            if (santaliButton != null) santaliButton.onClick.AddListener(() => OnLanguagePicked(Language.Santali));
            if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);

            // Default to currently saved language
            if (LocalizationManager.Instance != null)
            {
                selectedLanguage = LocalizationManager.Instance.CurrentLanguage;
            }

            UpdateCardVisuals();
            RefreshLocalization();
        }

        private void OnLanguagePicked(Language lang)
        {
            selectedLanguage = lang;
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.SetLanguage(lang);
            }
            if (UserSession.Instance != null)
            {
                UserSession.Instance.SetLanguage(lang.ToString());
            }
            UpdateCardVisuals();
            RefreshLocalization();
        }

        private void RefreshLocalization()
        {
            var loc = LocalizationManager.Instance;
            if (loc == null) return;

            if (titleText != null)
            {
                titleText.text = loc.GetString("SELECT_LANGUAGE");
            }

            if (subtitleText != null)
            {
                subtitleText.text = loc.GetString("CHOOSE_PREFERRED_LANG");
            }

            if (continueButtonText != null)
            {
                continueButtonText.text = loc.GetString("CONTINUE");
            }
        }

        private void UpdateCardVisuals()
        {
            SetCardState(englishOutline, englishCheckmark, selectedLanguage == Language.English);
            SetCardState(hindiOutline, hindiCheckmark, selectedLanguage == Language.Hindi);
            SetCardState(santaliOutline, santaliCheckmark, selectedLanguage == Language.Santali);
        }

        private void SetCardState(Outline outline, GameObject checkmark, bool isSelected)
        {
            if (outline != null)
            {
                outline.effectColor = isSelected ? SelectedOrange : UnselectedBorder;
                outline.effectDistance = isSelected ? new Vector2(4, -4) : new Vector2(2, -2);
            }

            if (checkmark != null)
            {
                checkmark.SetActive(isSelected);
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
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.SetLanguage(selectedLanguage);
            }
            if (UserSession.Instance != null)
            {
                UserSession.Instance.SetLanguage(selectedLanguage.ToString());
            }

            if (SceneNavigator.Instance != null)
            {
                SceneNavigator.Instance.NavigateTo(ScreenState.CameraAccess);
            }
        }
    }
}
