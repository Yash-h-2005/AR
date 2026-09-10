using UnityEngine;
using UnityEngine.UI;
using System;

namespace SurakshaAR.UI.Components
{
    public class ModuleCard : MonoBehaviour
    {
        [SerializeField] private Text moduleTitleText;
        [SerializeField] private Text moduleDescriptionText;
        [SerializeField] private Text statusBadgeText;
        [SerializeField] private Button actionButton;

        public void SetupCard(string title, string description, bool isAvailable, Action onClick)
        {
            if (moduleTitleText != null) moduleTitleText.text = title;
            if (moduleDescriptionText != null) moduleDescriptionText.text = description;

            if (statusBadgeText != null)
            {
                statusBadgeText.text = isAvailable ? "Available" : "Coming Soon";
                statusBadgeText.color = isAvailable ? new Color(0.18f, 0.8f, 0.44f, 1f) : new Color(0.9f, 0.5f, 0.13f, 1f);
            }

            if (actionButton != null)
            {
                actionButton.interactable = isAvailable;
                actionButton.onClick.RemoveAllListeners();
                if (isAvailable && onClick != null)
                {
                    actionButton.onClick.AddListener(() => onClick.Invoke());
                }
            }
        }
    }
}
