// Gas-Tight Doors - an Oxygen Not Included mod by MythraPS.
//
// A door that is open lets gas and liquid through: the game empties its cells while it is open
// and fills them with the door's own material once it closes. This mod keeps that material in
// place while the door opens too, so the door holds a seal while duplicants and critters walk
// through it. Walking through is set up by the game separately (Door.SetPassableState), so it
// is unaffected.
//
// Which doors seal: every door except the Pneumatic Door (an "internal" door, which the game
// never lets seal). A door set to "Open" in its controls stays open the vanilla way, to vent a
// room on purpose.

using HarmonyLib;

namespace GasTightDoors
{
    public sealed class GasTightDoorsMod : KMod.UserMod2
    {
    }

    [HarmonyPatch(typeof(Door), "SetSimState")]
    internal static class Door_SetSimState_Patch
    {
        // When a sealing door opens, run the game's own "door closed" step instead of its "door
        // opened" one. That step keeps the cells filled with the door's material, applies the
        // door's insulation and keeps its temperature in sync, all as the game normally does.
        private static void Prefix(Door __instance, ref bool is_door_open)
        {
            if (is_door_open && Seals(__instance))
                is_door_open = false;
        }

        private static bool Seals(Door door) =>
            door != null
            && door.doorType != Door.DoorType.Internal
            && door.CurrentState != Door.ControlState.Opened;
    }
}
