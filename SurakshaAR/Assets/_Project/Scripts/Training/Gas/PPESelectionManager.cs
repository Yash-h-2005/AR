using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurakshaAR.Training.Gas
{
    public enum PPEType
    {
        SCBARespirator,
        SafetyHelmet,
        SafetyGoggles,
        ProtectiveGloves,
        DustMask,       // Distractor
        EarPlugs,       // Distractor
        StandardClothMask // Distractor
    }

    public class PPESelectionManager : MonoBehaviour
    {
        public static PPESelectionManager Instance { get; private set; }

        private readonly HashSet<PPEType> selectedPPE = new HashSet<PPEType>();
        private readonly HashSet<PPEType> requiredPPE = new HashSet<PPEType>()
        {
            PPEType.SCBARespirator,
            PPEType.SafetyHelmet,
            PPEType.SafetyGoggles,
            PPEType.ProtectiveGloves
        };

        public event Action<PPEType, bool> OnPPEItemToggled;
        public event Action<bool> OnPPESelectionValidated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void ResetSelection()
        {
            selectedPPE.Clear();
        }

        public bool SelectItem(PPEType ppeItem)
        {
            if (selectedPPE.Contains(ppeItem))
            {
                selectedPPE.Remove(ppeItem);
                OnPPEItemToggled?.Invoke(ppeItem, false);
                return false;
            }
            else
            {
                selectedPPE.Add(ppeItem);
                OnPPEItemToggled?.Invoke(ppeItem, true);
                return isRequired(ppeItem);
            }
        }

        public bool isRequired(PPEType item)
        {
            return requiredPPE.Contains(item);
        }

        public bool ValidatePPESelection(out string feedbackMessage)
        {
            bool hasRespirator = selectedPPE.Contains(PPEType.SCBARespirator);
            bool hasHelmet = selectedPPE.Contains(PPEType.SafetyHelmet);
            bool hasGoggles = selectedPPE.Contains(PPEType.SafetyGoggles);
            bool hasGloves = selectedPPE.Contains(PPEType.ProtectiveGloves);

            bool selectedDistractor = selectedPPE.Contains(PPEType.DustMask) ||
                                     selectedPPE.Contains(PPEType.EarPlugs) ||
                                     selectedPPE.Contains(PPEType.StandardClothMask);

            if (selectedDistractor)
            {
                feedbackMessage = "Incorrect PPE selected! Standard dust or cloth masks do NOT protect against toxic gas!";
                OnPPESelectionValidated?.Invoke(false);
                return false;
            }

            if (!hasRespirator)
            {
                feedbackMessage = "Missing SCBA / Self-Contained Breathing Apparatus! SCBA is mandatory for toxic gas atmospheres.";
                OnPPESelectionValidated?.Invoke(false);
                return false;
            }

            if (!hasHelmet || !hasGoggles || !hasGloves)
            {
                feedbackMessage = "Missing required protective gear (Helmet, Goggles, or Gloves). Full body safety required.";
                OnPPESelectionValidated?.Invoke(false);
                return false;
            }

            feedbackMessage = "Correct PPE Selected! Full gas protection suit & SCBA respirator equipped.";
            OnPPESelectionValidated?.Invoke(true);
            return true;
        }

        public bool IsSelected(PPEType ppeItem)
        {
            return selectedPPE.Contains(ppeItem);
        }
    }
}
