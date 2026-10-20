using TUNING;
using UnityEngine;
using BUILDINGS = TUNING.BUILDINGS;

namespace MagpieExtension
{
    /// <summary>Shared hardcoded build parameters that match vanilla/Ronivans bridge tiers.</summary>
    internal static class BridgeHelpers
    {
        // Solid conveyor bridge: matches vanilla SolidConduitBridgeConfig
        public static float[]  ConveyorMass      = BUILDINGS.CONSTRUCTION_MASS_KG.TIER4;
        public static string[] ConveyorMaterials = MATERIALS.ALL_METALS;

    }
}
