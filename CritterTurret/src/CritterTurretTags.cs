using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace CritterTurret
{
    // The species checklist can only show species registered under a category in
    // DiscoveredResources. Wranglable (BagableCreature) and net-catchable
    // (SwimmingCreature) critters register themselves as they're encountered;
    // every other shootable critter (Beetas, hives, robots...) gets a row under
    // this category of ours so "checked = shot" holds for everything.
    public static class CritterTurretTags
    {
        // Second Create arg registers the display name the row header uses.
        public static readonly Tag OtherCritters = TagManager.Create("CritterTurretOther", "Other Critters");

        // DiscoverCategory is private; invoked by reflection rather than
        // duplicating its bookkeeping. Not a Harmony patch, so no rebuilt-IL risk.
        private static readonly MethodInfo DiscoverCategory =
            AccessTools.Method(typeof(DiscoveredResources), "DiscoverCategory");

        // Which per-game DiscoveredResources we've already registered into. Runs
        // again after every load because save deserialization rebuilds the
        // category table. Called from CritterTurretBrain.OnSpawn, which is always
        // after deserialization and before the side screen can target a turret.
        private static DiscoveredResources registeredFor;

        public static void EnsureOtherCrittersRegistered()
        {
            DiscoveredResources inst = DiscoveredResources.Instance;
            if (inst == null || inst == registeredFor || DiscoverCategory == null) return;
            registeredFor = inst;

            foreach (GameObject prefab in Assets.GetPrefabsWithComponent<CreatureBrain>())
            {
                if (prefab == null || prefab.GetComponent<Health>() == null) continue;
                KPrefabID kpid = prefab.GetComponent<KPrefabID>();
                if (kpid == null) continue;
                if (kpid.HasTag(GameTags.BagableCreature) || kpid.HasTag(GameTags.SwimmingCreature)) continue;
                // Robots (Biobots, Rovers, Flydos, Sweepys...) aren't critters;
                // the brain refuses to shoot them, so no checkbox either.
                if (kpid.HasTag(GameTags.Robot)) continue;
                DiscoverCategory.Invoke(inst, new object[] { OtherCritters, kpid.PrefabTag });
            }
        }
    }
}
