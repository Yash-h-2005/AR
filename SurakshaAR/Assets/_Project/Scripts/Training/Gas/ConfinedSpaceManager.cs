using System;
using UnityEngine;
using SurakshaAR.Training.Fire;

namespace SurakshaAR.Training.Gas
{
    public class ConfinedSpaceManager : MonoBehaviour
    {
        public static ConfinedSpaceManager Instance { get; private set; }

        public bool IsAuthorized { get; private set; } = false;
        public bool SafetyChecksCompleted { get; private set; } = false;

        public event Action<bool, string> OnConfinedSpaceEntryAttempted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void ResetState()
        {
            IsAuthorized = false;
            SafetyChecksCompleted = false;
        }

        public bool AttemptConfinedSpaceEntry(bool ppeValid, bool detectorScanned, bool buddyVerified, out string feedbackMsg)
        {
            if (!ppeValid)
            {
                feedbackMsg = "ENTRY DENIED: Full SCBA PPE must be equipped before approaching confined space entry!";
                OnConfinedSpaceEntryAttempted?.Invoke(false, feedbackMsg);
                return false;
            }

            if (!detectorScanned)
            {
                feedbackMsg = "SEQUENCE ERROR: Complete mandatory multi-gas detector scan before entering confined space!";
                OnConfinedSpaceEntryAttempted?.Invoke(false, feedbackMsg);
                return false;
            }

            if (!buddyVerified)
            {
                feedbackMsg = "ENTRY DENIED: Two-person buddy system communication must be verified!";
                OnConfinedSpaceEntryAttempted?.Invoke(false, feedbackMsg);
                return false;
            }

            SafetyChecksCompleted = true;
            IsAuthorized = true;
            feedbackMsg = "CONFINED SPACE PERMIT AUTHORIZED: Safety protocol sequence verified.";
            OnConfinedSpaceEntryAttempted?.Invoke(true, feedbackMsg);
            return true;
        }
    }
}
