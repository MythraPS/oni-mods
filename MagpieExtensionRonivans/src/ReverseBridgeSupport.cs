using System;
using UnityEngine;

namespace MagpieExtensionRonivans
{
    /// <summary>
    /// Gives our Ronivans-family gap bridges the Reverse button from Knoest's
    /// "Reverse Bridges" (Steam 2982311017) when that mod is installed.
    ///
    /// Reverse Bridges only postfixes the three VANILLA bridge configs
    /// (Gas/Liquid/SolidConduitBridgeConfig), so nothing it does ever reaches a
    /// building built on Ronivans' High Pressure configs -- ours included. The
    /// button is a plain KMonoBehaviour though, so adding it ourselves is enough;
    /// nothing else about their mod needs to know we exist.
    ///
    /// Resolved by name at runtime so there is no compile-time or load-order
    /// dependency: with Reverse Bridges absent this is a no-op and the bridges
    /// behave exactly as before.
    ///
    /// NOTE: the 4-wide variants also need the placement correction in
    /// MagpieExtension's ReverseBridgeCompat -- an even-width footprint cannot be
    /// symmetric about its origin cell, so a 180-degree flip lands it one tile
    /// over. Their IDs are listed there.
    /// </summary>
    internal static class ReverseBridgeSupport
    {
        private const string BUTTON_TYPE = "reverse_bridge.ReverseBridgeButton";

        private static bool resolved;
        private static Type buttonType;

        public static void TryAddButton(GameObject go)
        {
            if (go == null) return;

            if (!resolved)
            {
                resolved = true;
                buttonType = RonivansHelpers.FindType(BUTTON_TYPE);
            }
            if (buttonType == null) return; // Reverse Bridges not installed

            // Their Reverse() calls GetComponent<ConduitBridgeBase>().Start() with
            // no null check, so only offer the button on a building that actually
            // has one. Gas/liquid bridges get ConduitBridge, rail and logistic
            // bridges get SolidConduitBridge; both derive from ConduitBridgeBase.
            if (go.GetComponent<ConduitBridgeBase>() == null) return;

            try
            {
                if (go.GetComponent(buttonType) == null) go.AddComponent(buttonType);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[MagpieExtensionRonivans] could not add the Reverse Bridges button to "
                                 + go.name + " (the bridge still works, it just won't be reversible): " + e);
            }
        }
    }
}
