using UnityEngine;
using UnityEngine.UI;
using SurakshaAR.Localization;
using SurakshaAR.UI.Navigation;

namespace SurakshaAR.UI.Screens
{
    public class SettingsController : MonoBehaviour
    {
        [Header("Language Controls")]
        [SerializeField] private Button englishLangButton;
        [SerializeField] private Button hindiLangButton;
        [SerializeField] private Button santaliLangButton;

        [Header("Toggles")]
        [SerializeField] private Toggle audioToggle;
        [SerializeField] private Toggle hapticToggle;
        [SerializeField] private Toggle assistanceToggle;

        [Header("Back Button")]
        [SerializeField] private Button backButton;

        private void Start()
        {
            if (englishLangButton != null) englishLangButton.onClick.AddListener(() => ChangeLang(Language.English));
            if (hindiLangButton != null) hindiLangButton.onClick.AddListener(() => ChangeLang(Language.Hindi));
            if (santaliLangButton != null) santaliLangButton.onClick.AddListener(() => ChangeLang(Language.Santali));

            if (backButton != null)
            {
                backButton.onClick.AddListener(() =>
                {
                    if (SceneNavigator.Instance != null)
                        SceneNavigator.Instance.GoBack();
                });
            }
        }

        private void ChangeLang(Language lang)
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.SetLanguage(lang);
            }
        }
    }
}
