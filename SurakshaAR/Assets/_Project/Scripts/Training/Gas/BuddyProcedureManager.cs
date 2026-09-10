using System;
using UnityEngine;

namespace SurakshaAR.Training.Gas
{
    public class BuddyProcedureManager : MonoBehaviour
    {
        public static BuddyProcedureManager Instance { get; private set; }

        public bool BuddyPresent { get; private set; } = false;
        public bool CommChecked { get; private set; } = false;
        public bool EntryProcedureConfirmed { get; private set; } = false;

        public event Action<bool> OnBuddyProcedureCompleted;

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
            BuddyPresent = false;
            CommChecked = false;
            EntryProcedureConfirmed = false;
        }

        public void ConfirmBuddyPresent()
        {
            BuddyPresent = true;
            Debug.Log("[BuddyProcedureManager] Buddy present confirmed.");
        }

        public void ConfirmCommChecked()
        {
            CommChecked = true;
            Debug.Log("[BuddyProcedureManager] Two-way radio communication confirmed.");
        }

        public bool ConfirmEntryProcedure(out string feedbackMsg)
        {
            if (!BuddyPresent)
            {
                feedbackMsg = "BUDDY CHECK ERROR: A stand-by safety buddy must be present at entry point!";
                OnBuddyProcedureCompleted?.Invoke(false);
                return false;
            }

            if (!CommChecked)
            {
                feedbackMsg = "COMMUNICATION ERROR: Verify two-way radio contact with safety buddy before proceeding!";
                OnBuddyProcedureCompleted?.Invoke(false);
                return false;
            }

            EntryProcedureConfirmed = true;
            feedbackMsg = "BUDDY SYSTEM VERIFIED: Stand-by safety monitor active with clear radio link.";
            OnBuddyProcedureCompleted?.Invoke(true);
            return true;
        }
    }
}
