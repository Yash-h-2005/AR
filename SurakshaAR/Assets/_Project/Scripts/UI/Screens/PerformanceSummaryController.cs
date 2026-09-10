using UnityEngine;
using UnityEngine.UI;
using SurakshaAR.UI.Navigation;

namespace SurakshaAR.UI.Screens
{
    public class PerformanceSummaryController : MonoBehaviour
    {
        [SerializeField] private Button backToHomeButton;

        private void Start()
        {
            if (backToHomeButton != null)
            {
                backToHomeButton.onClick.AddListener(() =>
                {
                    if (SceneNavigator.Instance != null)
                        SceneNavigator.Instance.NavigateTo(ScreenState.Home);
                });
            }
        }
    }
}
