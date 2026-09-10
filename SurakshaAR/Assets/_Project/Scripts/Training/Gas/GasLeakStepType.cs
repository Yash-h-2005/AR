namespace SurakshaAR.Training.Gas
{
    /// <summary>
    /// Deterministic 8-step safety sequence for Gas Leak & Confined Space Training Module.
    /// </summary>
    public enum GasLeakStepType
    {
        RecognizeHazard = 1,      // Step 1: Recognize gas hazard & leak location
        RaiseAlarm = 2,           // Step 2: Raise emergency alarm / alert work crew
        IdentifySafeExit = 3,     // Step 3: Identify safe unblocked upwind exit route
        SelectPPE = 4,            // Step 4: Select required PPE (SCBA/Respirator, Helmet, Gloves, Goggles)
        CheckGasDetector = 5,     // Step 5: Locate and perform AR gas detector scan
        ConfinedSpaceProtocol = 6,// Step 6: Follow confined-space entry authorization checks
        BuddyProcedure = 7,       // Step 7: Verify two-person buddy system check
        AssemblyPoint = 8,        // Step 8: Evacuate & reach safe assembly area
        Complete = 9              // Module complete
    }

    /// <summary>
    /// 100% Software-simulated gas concentration levels.
    /// </summary>
    public enum SimulatedGasLevel
    {
        Safe = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }
}
