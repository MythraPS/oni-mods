using HarmonyLib;
using KMod;
using System.Reflection;
using UnityEngine;
using BUILDINGS = TUNING.BUILDINGS;

namespace CritterWrangler
{
    public class CritterWranglerMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    // Research: same node as the Critter Drop-Off and friends.
    [HarmonyPatch(typeof(Db), "Initialize")]
    public static class Db_Initialize_Patch
    {
        public static void Postfix()
        {
            Tech tech = Db.Get().Techs.TryGet("AnimalControl");
            if (tech != null)
                tech.unlockedItemIDs.Add(CritterWranglerConfig.ID);
            else
                Debug.LogWarning("[CritterWrangler] AnimalControl tech not found; building stays unlocked from the start");
        }
    }

    // IBuildingConfig subclasses are auto-discovered/registered by the game.
    // Here we only add the building's strings and its build-menu placement.
    [HarmonyPatch(typeof(GeneratedBuildings), nameof(GeneratedBuildings.LoadGeneratedBuildings))]
    public static class GeneratedBuildings_Patch
    {
        private static bool _added;

        public static void Prefix()
        {
            if (_added) return;
            _added = true;

            string w = "STRINGS.BUILDINGS.PREFABS." + CritterWranglerConfig.ID.ToUpperInvariant() + ".";
            Strings.Add(w + "NAME",
                "Critter Wrangler");
            Strings.Add(w + "DESC",
                "A teal robo-miner fitted with a Rancher's net gun. It mounts on a floor, wall, or ceiling and wrangles within a directional arc (line of sight required).");
            Strings.Add(w + "EFFECT",
                "Automatically wrangles surplus critters within its room, leaving them bagged on the ground for a Duplicant to carry to a Critter Drop-Off. No Ranching skill required, and it reaches critters at range through line of sight. Each species is counted separately against its own threshold, with babies listed separately from adults. Sends a green Automation signal once every selected species is at or below the threshold.");

            // The threshold side screen's Title/ValueName are LocStrings, whose 1-arg
            // constructor stores the string as a lookup KEY (not text) -- so unless the
            // key is registered it renders as "MISSING.". Register key == text.
            Strings.Add("STRINGS.UI.UISIDESCREENS.CRITTERWRANGLER.OPEN_FIRE", "Wrangle At Population");
            // Already-bagged critters are excluded from this count, so the number is
            // "how many are still loose", not total room population.
            Strings.Add("STRINGS.UI.UISIDESCREENS.CRITTERWRANGLER.VALUE_NAME", "Loose critters of one species in room");

            // Same category + subcategory + menu position as the Robo-Miner (Auto-Miner):
            // category "Conveyance" (the Shipping tab), subcategory "automated". Placed
            // after the Critter Turret when that mod is also installed, and after the
            // Robo-Miner when it isn't -- AddBuildingToPlanScreen falls back to
            // appending if the named neighbour is absent.
            BUILDINGS.PLANSUBCATEGORYSORTING[CritterWranglerConfig.ID] = "automated";
            ModUtil.AddBuildingToPlanScreen("Conveyance", CritterWranglerConfig.ID, "automated", "AutoMiner");
        }
    }
}
