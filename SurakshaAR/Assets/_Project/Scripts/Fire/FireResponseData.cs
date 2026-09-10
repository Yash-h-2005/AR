using System;
using UnityEngine;

namespace SurakshaAR.Training.Fire
{
    /// <summary>
    /// Classification of fire hazard scenarios in the mine.
    /// Phase 2B-1 uses ElectricalEquipment (deterministic).
    /// </summary>
    public enum FireScenarioType
    {
        ElectricalEquipment, // Class C/E: Electrical panel/transformer/cables
        LiquidFuel,          // Class B: Diesel/oil
        OrdinaryCombustibles // Class A: Timber/conveyor
    }

    /// <summary>
    /// Available response resources in the mine environment.
    /// </summary>
    public enum ResponseResourceType
    {
        None,
        FireExtinguisher,    // CO2 / Dry Chemical extinguisher
        WaterContainer,      // Water canister/bucket
        MudPile              // Mining rock dust / tamping mud pile
    }

    /// <summary>
    /// Deterministic scenario configuration data for Phase 2B.
    /// </summary>
    [Serializable]
    public class FireScenarioConfig
    {
        public FireScenarioType ScenarioType = FireScenarioType.ElectricalEquipment;
        public ResponseResourceType CorrectResponse = ResponseResourceType.FireExtinguisher;

        public string HazardTitle = "Electrical Equipment Fire";
        public string ContextClue = "High-voltage distribution panel and heavy cable conduit arcing";

        // Assessment objective text
        public const string ObjectiveAssess = "Assess the fire and identify a safe response.";
        public const string ObjectiveUseResponse = "Use the selected response on the fire.";
    }
}
