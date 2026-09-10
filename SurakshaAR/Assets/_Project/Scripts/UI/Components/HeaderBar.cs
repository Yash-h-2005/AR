using UnityEngine;
using UnityEngine.UI;
using SurakshaAR.UI.Navigation;
using SurakshaAR.UI.Data;

namespace SurakshaAR.UI.Components
{
    /// <summary>
    /// Reusable top title bar with back button, screen title, and worker badge.
    /// </summary>
    public class HeaderBar : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [SerializeField] private Text titleText;
        [SerializeField] private Text workerBadgeText;

        private void Start()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
            }

            UpdateWorkerBadge();
        }

        public void SetTitle(string title)
        {
            if (titleText != null)
            {
                titleText.text = title;
            }
        }

        public void UpdateWorkerBadge()
        {
            if (workerBadgeText != null && UserSession.Instance != null)
            {
                workerBadgeText.text = UserSession.Instance.GetFormattedWorkerLabel();
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
