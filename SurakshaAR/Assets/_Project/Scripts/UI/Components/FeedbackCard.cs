using System;
using UnityEngine;
using UnityEngine.UI;

namespace SurakshaAR.UI.Components
{
    /// <summary>
    /// Contextual feedback overlay card displayed when a worker performs an incorrect action in AR.
    /// Non-intrusive banner with explanation, TRY AGAIN action button, and optional help.
    /// </summary>
    public class FeedbackCard : MonoBehaviour
    {
        [SerializeField] private Text headerText;
        [SerializeField] private Text explanationText;
        [SerializeField] private Button tryAgainButton;
        [SerializeField] private Button needHelpButton;

        public void ShowFeedback(string header, string tipExplanation, Action onTryAgain, Action onNeedHelp = null)
        {
            gameObject.SetActive(true);

            if (headerText != null)
                headerText.text = string.IsNullOrEmpty(header) ? "⚠ Try Again" : header;

            if (explanationText != null)
                explanationText.text = tipExplanation;

            if (tryAgainButton != null)
            {
                tryAgainButton.onClick.RemoveAllListeners();
                tryAgainButton.onClick.AddListener(() =>
                {
                    Hide();
                    onTryAgain?.Invoke();
                });
            }

            if (needHelpButton != null)
            {
                needHelpButton.gameObject.SetActive(onNeedHelp != null);
                if (onNeedHelp != null)
                {
                    needHelpButton.onClick.RemoveAllListeners();
                    needHelpButton.onClick.AddListener(() =>
                    {
                        onNeedHelp?.Invoke();
                    });
                }
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
