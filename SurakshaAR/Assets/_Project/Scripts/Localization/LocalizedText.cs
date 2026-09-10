using UnityEngine;
using UnityEngine.UI;

namespace SurakshaAR.Localization
{
    /// <summary>
    /// Component attached to UI Text components. Automatically updates text 
    /// whenever the active language changes in LocalizationManager.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string localizationKey;
        public string LocalizationKey
        {
            get => localizationKey;
            set
            {
                localizationKey = value;
                UpdateText();
            }
        }

        private Text textComponent;

        private void Awake()
        {
            textComponent = GetComponent<Text>();
        }

        private void OnEnable()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
            }
            UpdateText();
        }

        private void OnDisable()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
            }
        }

        private void HandleLanguageChanged(Language newLanguage)
        {
            UpdateText();
        }

        public void UpdateText()
        {
            if (textComponent == null) textComponent = GetComponent<Text>();

            if (textComponent != null && !string.IsNullOrEmpty(localizationKey) && LocalizationManager.Instance != null)
            {
                textComponent.text = LocalizationManager.Instance.GetString(localizationKey);
            }
        }
    }
}
