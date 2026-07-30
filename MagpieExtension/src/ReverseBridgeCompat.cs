using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace MagpieExtension
{
    // Compatibility shim for Knoest's "Reverse Bridges" (Steam 2982311017).
    //
    // Its Reverse button rebuilds the bridge on the same origin cell with the
    // orientation turned 180 degrees. Rotatable.GetRotatedCellOffset implements
    // R180 as a plain (x,y) -> (-x,-y) negation about the origin cell, so the
    // footprint only lands back where it started if it is symmetric about that
    // cell. Vanilla bridges are 3 wide with ports at -1 and +1, so they are;
    // our 3-tile-gap bridges are 5 wide with ports at -2 and +2, so they are
    // too. The 2-tile-gap bridges are 4 wide and occupy -1..2, which negates to
    // -2..1 -- the whole building lands one tile to the left of where it was.
    // Even widths cannot be symmetric about a cell, so this is unavoidable
    // geometry rather than a mistake on either side.
    //
    // The fix nudges the building one tile along its own axis for the duration
    // of their MakeNewBuilding, which reads the rebuild cell straight off the
    // transform, and puts it back immediately afterwards -- before their
    // DestroyOldBuilding runs, so the old building still releases the cells it
    // actually occupied. Scoped to our own 4-wide bridges: their handling of
    // vanilla and every other mod's bridges is left exactly as it was.
    public static class ReverseBridgeCompat
    {
        private const string BUTTON_TYPE = "reverse_bridge.ReverseBridgeButton";
        private const string TARGET_METHOD = "MakeNewBuilding";

        // Set in OnLoad; the patch itself is applied later (see TryPatch).
        public static Harmony HarmonyInstance;

        private static bool attempted;

        // Resolved by TryPatch; null when Reverse Bridges isn't installed.
        public static Type ButtonType;

        // Only the even-width (4-wide, 2-tile-gap) bridges are affected; the
        // 5-wide ones are symmetric about their origin cell and reverse cleanly.
        //
        // The first three come from their mod's own patches on
        // GasConduitBridgeConfig / LiquidConduitBridgeConfig /
        // SolidConduitBridgeConfig. The rest are the Ronivans-family bridges,
        // which their mod never touches -- MagpieExtensionRonivans attaches the
        // button to those itself (see ReverseBridgeSupport). Listed as literals
        // because that is a separate assembly, loaded only when Ronivans Legacy
        // is present; an ID here that no building uses simply never matches.
        private static readonly HashSet<string> AffectedIds = new HashSet<string>
        {
            GasConduitBridge2Config.ID,
            LiquidConduitBridge2Config.ID,
            SolidConduitBridge2Config.ID,
            "HPGasBridge2",
            "HPLiquidBridge2",
            "HPARailBridge2",
            "HPARailBridgeTile2",
            "LogisticBridge2",
        };

        // Deliberately NOT called from OnLoad: if Reverse Bridges loads after us
        // its assembly is not around yet and TypeByName would come back empty.
        // Db.Initialize runs once every mod has loaded.
        public static void TryPatch()
        {
            if (attempted || HarmonyInstance == null) return;
            attempted = true;

            ButtonType = AccessTools.TypeByName(BUTTON_TYPE);
            Type buttonType = ButtonType;
            if (buttonType == null) return; // Reverse Bridges not installed

            try
            {
                MethodInfo target = AccessTools.Method(buttonType, TARGET_METHOD);
                if (target == null)
                {
                    Debug.LogWarning("[LongerBridges] Reverse Bridges found but " + BUTTON_TYPE + "."
                        + TARGET_METHOD + " is missing; 2-tile-gap bridges will shift one tile when reversed.");
                    return;
                }
                HarmonyInstance.Patch(target,
                    prefix: new HarmonyMethod(typeof(ReverseBridgeCompat), nameof(Prefix)),
                    postfix: new HarmonyMethod(typeof(ReverseBridgeCompat), nameof(Postfix)));
                Debug.Log("[LongerBridges] Reverse Bridges detected; patched " + TARGET_METHOD
                    + " to keep 4-wide bridges in place.");
            }
            catch (Exception e)
            {
                // A failed shim must never cost anyone the bridges themselves.
                Debug.LogWarning("[LongerBridges] Reverse Bridges compat patch failed (bridges still work, "
                    + "reversing a 2-tile-gap bridge will move it one tile): " + e);
            }
        }

        // Gives every bridge in the game the Reverse button, not just ours --
        // Ronivans' own 1-tile HP bridges, Splitters MK II, Underground Conduit,
        // anything else with a ConduitBridgeBase. Runs once, after
        // LoadGeneratedBuildings has registered every mod's buildings.
        public static void AddButtonToAllBridges()
        {
            TryPatch(); // resolves ButtonType if it hasn't been already
            if (!LongerBridgesSettings.Instance.ReverseButtonOnAllBridges) return;
            Type buttonType = ButtonType;
            if (buttonType == null || Assets.BuildingDefs == null) return;

            int added = 0;
            foreach (BuildingDef def in Assets.BuildingDefs)
            {
                GameObject go = def != null ? def.BuildingComplete : null;
                if (go == null) continue;
                // Their Reverse() calls GetComponent<ConduitBridgeBase>().Start()
                // unguarded, so a building without one would hard-NRE on click.
                if (go.GetComponent<ConduitBridgeBase>() == null) continue;
                if (go.GetComponent(buttonType) != null) continue;
                try { go.AddComponent(buttonType); added++; }
                catch (Exception e)
                {
                    Debug.LogWarning("[LongerBridges] could not add the Reverse button to "
                        + def.PrefabID + ": " + e);
                }
            }
            if (added > 0)
                Debug.Log("[LongerBridges] added the Reverse Bridges button to " + added
                    + " bridge(s) from other mods (ReverseButtonOnAllBridges).");
        }

        public static void Prefix(object __instance, ref Vector3 __state)
        {
            __state = Vector3.zero;

            Component comp = __instance as Component;
            if (comp == null) return;
            Building building = comp.GetComponent<Building>();
            if (building == null || building.Def == null) return;

            // The shift is a property of the geometry, not of who owns the
            // building, so it applies to any even-width bridge -- but only once
            // we're the reason other mods' bridges can be reversed at all.
            bool ours = AffectedIds.Contains(building.Def.PrefabID);
            bool adopted = LongerBridgesSettings.Instance.ReverseButtonOnAllBridges
                           && building.Def.WidthInCells % 2 == 0;
            if (!ours && !adopted) return;

            // Their GetReverseOrientation flips the Rotatable before reading the
            // cell, but this prefix runs first, so this is still the orientation
            // the bridge was built at -- which is the axis the correction is on.
            Rotatable rot = comp.GetComponent<Rotatable>();
            Orientation orientation = rot != null ? rot.GetOrientation() : Orientation.Neutral;
            CellOffset shift = Rotatable.GetRotatedCellOffset(new CellOffset(1, 0), orientation);

            __state = new Vector3(shift.x, shift.y, 0f);
            comp.transform.localPosition += __state;
        }

        public static void Postfix(object __instance, Vector3 __state)
        {
            if (__state == Vector3.zero) return;
            Component comp = __instance as Component;
            if (comp == null) return;
            comp.transform.localPosition -= __state;
        }
    }
}
