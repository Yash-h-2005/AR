using System.Collections;
using UnityEngine;
using SurakshaAR.UI.Navigation;

namespace SurakshaAR.UI.Screens
{
    public class SplashScreenController : MonoBehaviour
    {
        [SerializeField] private float splashDuration = 2.5f;

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(splashDuration);

            if (SceneNavigator.Instance != null)
            {
                SceneNavigator.Instance.NavigateTo(ScreenState.Login, addToStack: false);
            }
        }
    }
}
