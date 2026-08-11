using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using UnityEngine;

// Lets refinement buildings run without a duplicant standing at them.
//
// ONI already supports this natively: ComplexFabricator has a public bool
// duplicantOperated, and when it is false Sim200ms advances the craft itself,
// UpdateChore early-returns so no operate errand is ever created, and HasWorker
// reports true so the state machine never stalls in waitingForWorker. Seven vanilla
// buildings ship this way -- Chlorinator, Data Miner, Food Dehydrator, Kiln, Rubber
// Maker, Smoker and Uranium Centrifuge. This mod just extends that set.
//
// The building's own working animation is unaffected: working_pre/working_loop/
// working_pst are driven by ComplexFabricatorSM off Operational.IsActive, not off a
// worker. What is lost is the duplicant's interact animation, which plays on the dupe
// rather than on the building -- hence the progress bar, exactly as vanilla does it.
//
// duplicantOperated carries no [Serialize] attribute, so it is never saved; every
// instance takes its value from the prefab. Patching the prefab is therefore enough
// for both new and existing colonies, and the OnSpawn patch below is only a safety
// net for anything already instantiated by the time the buildings finish loading.
namespace AutoRefinement
{
    public class AutoRefinementMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            // Puts the gear icon next to this mod in the Mods menu.
            new POptions().RegisterOptions(this, typeof(AutoRefinementOptions));
            harmony.PatchAll();
        }
    }

    internal static class AutoOperate
    {
        // Prefabs already handled, so the OnSpawn safety net stays quiet.
        private static readonly HashSet<string> converted = new HashSet<string>();

        // Prefabs left duplicant-operated on purpose, running slowly by themselves.
        private static readonly HashSet<string> assisted = new HashSet<string>();

        internal static bool Apply(GameObject go, ComplexFabricator fab, string prefabId)
        {
            if (fab == null || !fab.duplicantOperated) return false;
            // Assisted buildings keep the flag set, so without this the pass would
            // "convert" them again every time it runs.
            if (AlreadyConverted(prefabId)) return false;

            var opts = AutoRefinementOptions.Instance;
            if (opts.DuplicantsCanHelp)
            {
                // duplicantOperated has to stay true or no errand exists and no dupe can
                // ever work it. Patch_Sim200ms supplies the unattended progress instead.
                if (!string.IsNullOrEmpty(prefabId)) assisted.Add(prefabId);
            }
            else
            {
                fab.duplicantOperated = false;
                fab.showProgressBar = opts.ShowProgressBar;
            }

            // Operation is no longer required either way, and this is the only thing that
            // drives the "Requires Manual Operation" line in the building's info panel.
            var complete = go.GetComponent<BuildingComplete>();
            if (complete != null) complete.isManuallyOperated = false;

            if (!string.IsNullOrEmpty(prefabId)) converted.Add(prefabId);
            return true;
        }

        internal static bool AlreadyConverted(string prefabId)
        {
            return !string.IsNullOrEmpty(prefabId) && converted.Contains(prefabId);
        }

        internal static bool IsAssisted(string prefabId)
        {
            return !string.IsNullOrEmpty(prefabId) && assisted.Contains(prefabId);
        }

        internal static string PrefabIdOf(GameObject go)
        {
            if (go == null) return null;
            var kpid = go.GetComponent<KPrefabID>();
            return kpid == null ? null : kpid.PrefabTag.Name;
        }
    }

    // Main hook. By the time buildings have finished registering, Assets.BuildingDefs
    // holds every def from the game and from every other mod, so this catches modded
    // refineries without needing to reference those mods.
    [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
    internal static class Patch_LoadGeneratedBuildings
    {
        // The game calls LoadGeneratedBuildings more than once. The pass is idempotent
        // and cheap, so it runs every time in case a mod registers buildings late --
        // but the report is only worth printing on the first pass, or on a later pass
        // that actually changed something.
        private static bool reported;

        private static void Postfix()
        {
            var settings = AutoRefinementOptions.Instance;
            var found = new List<string>();
            var fixedUp = new List<string>();
            int changed = 0;

            foreach (var def in Assets.BuildingDefs)
            {
                if (def == null || def.BuildingComplete == null) continue;

                var fab = def.BuildingComplete.GetComponent<ComplexFabricator>();
                if (fab == null || !fab.duplicantOperated) continue;

                string id = AutoOperate.PrefabIdOf(def.BuildingComplete);
                found.Add(id);

                if (!settings.Matches(id)) continue;
                if (AutoOperate.Apply(def.BuildingComplete, fab, id))
                {
                    changed++;
                    fixedUp.Add(id);
                }
            }

            if (reported && changed == 0) return;
            reported = true;

            Debug.Log("[AutoRefinement] converted " + changed + " of " + found.Count +
                      " duplicant-operated fabricators; mode = " +
                      (settings.DuplicantsCanHelp
                          ? "assisted (errand kept, " + settings.UnattendedSpeed + "% unattended / "
                            + settings.AttendedSpeed + "% with a duplicant)"
                          : "fully automatic (no errand)"));

            if (settings.LogFabricators && found.Count > 0)
            {
                // Both lists are printed quoted and comma-terminated so a line can be
                // pasted straight into AutoRefinement.json.
                var sb = new StringBuilder(
                    "[AutoRefinement] now automatic -- paste any of these into ManualBuildings to undo:");
                foreach (string id in fixedUp) sb.Append("\n    \"").Append(id).Append("\",");

                var stillManual = new List<string>();
                foreach (string id in found)
                    if (!AutoOperate.AlreadyConverted(id)) stillManual.Add(id);

                if (stillManual.Count > 0)
                {
                    sb.Append("\n[AutoRefinement] still duplicant-operated:");
                    foreach (string id in stillManual) sb.Append("\n    \"").Append(id).Append("\",");
                }
                Debug.Log(sb.ToString());
            }
        }
    }

    // Unattended progress for "duplicants can still help" mode.
    //
    // The two work paths share one value: CreateChore seeds the work chore with the
    // current orderProgress, and ComplexFabricatorWorkable.UpdateOrderProgress writes it
    // back as the duplicant works. So a building can tick along on its own and a duplicant
    // who wanders over simply finishes the remainder at full speed.
    //
    // Rather than reimplement the progress maths, this borrows vanilla's own automatic
    // path for the duration of one tick. That is safe because dt is used exactly once in
    // Sim200ms -- passed to ComputeWorkProgress, which is literally dt / recipe.time --
    // so scaling dt scales the craft rate linearly and touches nothing else.
    [HarmonyPatch(typeof(ComplexFabricator), "Sim200ms")]
    internal static class Patch_ComplexFabricator_Sim200ms
    {
        private static void Postfix(ComplexFabricator __instance, float dt)
        {
            var opts = AutoRefinementOptions.Instance;
            if (!opts.DuplicantsCanHelp || __instance == null) return;
            if (!__instance.duplicantOperated) return;   // fully automatic: vanilla's job

            var workable = __instance.GetComponent<ComplexFabricatorWorkable>();
            if (workable == null || workable.worker != null) return;  // a dupe is on it
            if (!AutoOperate.IsAssisted(AutoOperate.PrefabIdOf(__instance.gameObject))) return;

            var operational = __instance.GetComponent<Operational>();
            if (operational == null || !operational.IsOperational) return;

            var recipe = workable.CurrentWorkingOrder;
            if (recipe == null || recipe.time <= 0f) return;

            // ComputeWorkProgress is protected, but it is literally dt / recipe.time.
            float progress = __instance.OrderProgress
                             + dt * (opts.UnattendedSpeed / 100f) / recipe.time;

            if (progress >= 1f)
            {
                __instance.CompleteWorkingOrder();
                return;
            }

            __instance.OrderProgress = progress;
            // CreateWorkChore seeds workTimeRemaining from orderProgress ONCE, at creation.
            // Without this the trickle would be discarded the moment a duplicant started:
            // they would resume from the stale seed and UpdateOrderProgress would write
            // that back over it.
            workable.WorkTimeRemaining = workable.GetWorkTime() * (1f - progress);
        }
    }

    // Makes an assisted building look busy while it works unattended.
    //
    // HasWorker is read in exactly two places -- Sim200ms, where it decides
    // Operational.SetActive and therefore whether the working animation plays, and
    // WaitingForWorker, which drives the "waiting for duplicant" status. Neither is
    // involved in creating the operate errand (UpdateChore keys off IsOperational and
    // HasWorkingOrder), so reporting true here is safe: duplicants are still offered
    // the errand exactly as vanilla intends.
    [HarmonyPatch(typeof(ComplexFabricator), "get_HasWorker")]
    internal static class Patch_ComplexFabricator_HasWorker
    {
        private static void Postfix(ComplexFabricator __instance, ref bool __result)
        {
            if (__result || __instance == null) return;
            var opts = AutoRefinementOptions.Instance;
            if (!opts.DuplicantsCanHelp || !__instance.duplicantOperated) return;
            if (!AutoOperate.IsAssisted(AutoOperate.PrefabIdOf(__instance.gameObject))) return;

            __result = true;
        }
    }

    // Diagnostic for the one interaction that cannot be eyeballed in game: whether a
    // duplicant starting work resumes from the unattended progress or restarts from
    // whatever the chore was seeded with. Logs both numbers at the moment they start.
    // Capped so it cannot fill the log, and only while ID logging is on.
    [HarmonyPatch(typeof(ComplexFabricatorWorkable), "OnStartWork")]
    internal static class Patch_ComplexFabricatorWorkable_OnStartWork
    {
        private const int MAX_LINES = 20;
        private static int logged;

        private static void Postfix(ComplexFabricatorWorkable __instance)
        {
            var opts = AutoRefinementOptions.Instance;
            if (!opts.LogFabricators || !opts.DuplicantsCanHelp) return;
            if (logged >= MAX_LINES || __instance == null) return;

            string id = AutoOperate.PrefabIdOf(__instance.gameObject);
            if (!AutoOperate.IsAssisted(id)) return;

            var fab = __instance.GetComponent<ComplexFabricator>();
            if (fab == null) return;

            float total = __instance.GetWorkTime();
            float implied = total > 0f ? 1f - (__instance.WorkTimeRemaining / total) : 0f;
            bool mismatch = Mathf.Abs(fab.OrderProgress - implied) > 0.02f;

            logged++;
            Debug.Log(string.Format(
                "[AutoRefinement] duplicant started {0}: unattended progress {1:P1}, "
                + "chore resumes at {2:P1}{3}",
                id, fab.OrderProgress, implied,
                mismatch ? "  <-- MISMATCH: progress would reset" : "  (in step)"));
        }
    }

    // The other half of the ratio: how fast a duplicant works an assisted building.
    // Scaling dt here scales only the craft rate -- OnWorkTick forwards dt to the tick
    // actions, to UpdateOrderProgress (which recomputes from workTimeRemaining anyway),
    // and to Workable.OnWorkTick, which is what actually consumes it.
    [HarmonyPatch(typeof(ComplexFabricatorWorkable), "OnWorkTick")]
    internal static class Patch_ComplexFabricatorWorkable_OnWorkTick
    {
        private static void Prefix(ComplexFabricatorWorkable __instance, ref float dt)
        {
            var opts = AutoRefinementOptions.Instance;
            if (!opts.DuplicantsCanHelp || __instance == null) return;
            if (!AutoOperate.IsAssisted(AutoOperate.PrefabIdOf(__instance.gameObject))) return;

            dt *= opts.AttendedSpeed / 100f;
        }
    }

    // The Desalinator is not a ComplexFabricator, so nothing above touches it. It is an
    // ElementConverter plus its own state machine, and the salt it accumulates is removed
    // by a WorkChore<DesalinatorWorkableEmpty> under ChoreTypes.EmptyDesalinator -- a real
    // duplicant errand with no duplicantOperated equivalent to switch off.
    //
    // The state machine calls CreateEmptyChore() once the salt storage fills. Its private
    // OnEmptyComplete(Chore) is the chore's own completion callback, and it ignores the
    // chore argument entirely: it nulls emptyChore, then drops every Salt item in storage
    // onto the floor. So invoking it directly IS "a dupe just emptied this", and skipping
    // the original means the errand is never queued in the first place.
    //
    // Dropping to the floor is deliberate -- it puts the salt exactly where a duplicant
    // would have left it, which keeps auto-sweeper pickup working as normal.
    [HarmonyPatch(typeof(Desalinator.StatesInstance), "CreateEmptyChore")]
    internal static class Patch_Desalinator_CreateEmptyChore
    {
        private static bool warned;

        private static bool Prefix(Desalinator.StatesInstance __instance)
        {
            if (!AutoRefinementOptions.Instance.AutomaticDesalinator) return true;

            try
            {
                var onComplete = AccessTools.Method(typeof(Desalinator.StatesInstance), "OnEmptyComplete");
                if (onComplete == null)
                {
                    if (!warned)
                    {
                        warned = true;
                        Debug.LogWarning("[AutoRefinement] Desalinator.OnEmptyComplete not found; " +
                                         "leaving the manual empty errand in place.");
                    }
                    return true;
                }

                onComplete.Invoke(__instance, new object[] { null });
                return false;
            }
            catch (System.Exception e)
            {
                // Fall through to vanilla rather than strand the building with neither an
                // errand nor an automatic dump.
                if (!warned)
                {
                    warned = true;
                    Debug.LogWarning("[AutoRefinement] could not auto-empty the Desalinator; " +
                                     "falling back to the duplicant errand: " + e);
                }
                return true;
            }
        }
    }

    // Safety net for instances that exist before the prefab pass runs. Prefix, so the
    // flag is already false when OnSpawn decides whether to create an operate chore.
    [HarmonyPatch(typeof(ComplexFabricator), "OnSpawn")]
    internal static class Patch_ComplexFabricator_OnSpawn
    {
        private static void Prefix(ComplexFabricator __instance)
        {
            if (__instance == null || !__instance.duplicantOperated) return;

            string id = AutoOperate.PrefabIdOf(__instance.gameObject);
            if (!AutoRefinementOptions.Instance.Matches(id)) return;

            AutoOperate.Apply(__instance.gameObject, __instance, id);
        }
    }
}
