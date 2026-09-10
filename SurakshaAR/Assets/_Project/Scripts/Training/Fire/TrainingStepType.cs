namespace SurakshaAR.Training.Fire
{
    /// <summary>
    /// Deterministic 8-step safety sequence for Fire & Explosion Response Learn Mode.
    /// </summary>
    public enum TrainingStepType
    {
        IdentifyFire = 1,      // Step 1: Recognize 3D fire hazard
        RaiseAlarm = 2,        // Step 2: Activate AR manual call point / alarm button
        IdentifyExit = 3,      // Step 3: Evaluate & select safe unblocked emergency exit
        SelectExtinguisher = 4, // Step 4: Choose correct extinguisher type (CO2 / Dry Powder)
        PickupExtinguisher = 5, // Step 5: Grasp extinguisher & pull safety pin
        UseExtinguisher = 6,    // Step 6: PASS technique (Aim at base & hold SPRAY)
        Evacuate = 7,          // Step 7: Physical evacuation below smoke layer
        AssemblyPoint = 8,     // Step 8: Reach designated outdoor assembly zone
        Complete = 9           // Training complete
    }

    /// <summary>
    /// Types of 3D AR training objects in the scenario.
    /// </summary>
    public enum TrainingObjectType
    {
        FireHazard,
        AlarmButton,
        SafeExit,
        BlockedExit,
        WaterExtinguisher,
        CO2Extinguisher,
        AssemblyZone,
        GasHazardSource,
        GasDetectorDevice,
        PPESelectionStation,
        ConfinedSpaceMarker,
        BuddyCheckPoint
    }

    /// <summary>
    /// State of the active fire hazard.
    /// </summary>
    public enum FireState
    {
        Active,
        Suppressing,
        Controlled
    }
}
