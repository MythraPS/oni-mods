using System.Collections.Generic;
using UnityEngine;
using TUNING;
using BUILDINGS = TUNING.BUILDINGS;

namespace CritterTurret
{
    // Everything the turret and the wrangler share: the same Auto-Miner chassis with
    // the same species checklist, range visual and logic ports, differing only in
    // colour, strings and what their brain does on discharge. Shipped as two separate
    // mods; this file is imported into the other one at build time (see its build.sh).
    public static class CritterBuildingTemplate
    {
        public static BuildingDef CreateDef(string id, string anim)
        {
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                id, 2, 2, anim, 10, 10f,
                BUILDINGS.CONSTRUCTION_MASS_KG.TIER3, MATERIALS.REFINED_METALS, 1600f,
                BuildLocationRule.OnFoundationRotatable, BUILDINGS.DECOR.PENALTY.TIER2,
                NOISE_POLLUTION.NOISY.TIER0, 0.2f);
            // Submersible: works while under liquid instead of shorting out. Ranches
            // and pools get flooded, and a turret that stops at the waterline is
            // useless for fish. (Same value the vanilla Auto-Miner uses.)
            def.Floodable = false;
            def.AudioCategory = "Metal";
            def.RequiresPowerInput = true;
            def.EnergyConsumptionWhenActive = 120f;
            def.ExhaustKilowattsWhenActive = 0f;
            def.SelfHeatKilowattsWhenActive = 2f;
            def.PermittedRotations = PermittedRotations.R360;
            def.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(new CellOffset(0, 0));
            // Green once every checked species is at or under the threshold. The
            // other footprint cell, so it doesn't collide with the on/off input.
            def.LogicOutputPorts = new List<LogicPorts.Port>
            {
                LogicPorts.Port.OutputPort(
                    CritterTargetingBrain.POPULATION_REACHED_PORT_ID,
                    new CellOffset(1, 0),
                    "Target Population Reached",
                    "Sends a green signal while every selected species is at or below the threshold",
                    "Sends a red signal while any selected species is over the threshold",
                    // show_wire_missing_icon: false. The port is optional -- the
                    // building works fine unwired, so it must not nag for a wire.
                    false, false)
            };
            return def;
        }

        // The species checklist -- the same TreeFilterableSideScreen the Critter
        // Drop-Off shows. The screen requires a Storage on the target and reads its
        // storageFilters for the category rows; this Storage never actually stores
        // anything (nothing ever fetches to it). Rows list every DISCOVERED species
        // under each category, so wranglable land critters and catchable fish all
        // show up; the brain checks targets against TreeFilterable's accepted tags.
        // extraCategories: any additional checklist category the caller invents.
        // Kept as a parameter rather than referenced directly so this file has no
        // dependency of its own -- it is shared verbatim between two separate mods.
        public static void ConfigureTemplate(GameObject go, params Tag[] extraCategories)
        {
            go.AddOrGet<Operational>();

            Storage storage = go.AddOrGet<Storage>();
            storage.allowItemRemoval = false;
            storage.showDescriptor = false;
            storage.allowSettingOnlyFetchMarkedItems = false;
            storage.storageFilters = new List<Tag>
            {
                GameTags.BagableCreature,
                GameTags.SwimmingCreature
            };
            if (extraCategories != null) storage.storageFilters.AddRange(extraCategories);

            TreeFilterable filterable = go.AddOrGet<TreeFilterable>();
            // Storage bins tint themselves + raise "no storage filter set" when
            // nothing is checked; on a turret that state just means "hold fire at
            // listed species", so keep the building's normal look.
            filterable.tintOnNoFiltersSet = false;
            // Would prune accepted tags against the category table on spawn; our
            // OtherCritters category registers per-game after load, so pruning
            // could race it and silently drop saved selections.
            filterable.filterByStorageCategoriesOnSpawn = false;
        }

        // Both buildings' kanims have their colour baked into the atlas, which fights
        // ONI's placement feedback: the drag preview is tinted by multiplication
        // (white = valid, red = invalid), and coloured art times white tint still
        // reads as coloured. Give just the preview the vanilla grey Auto-Miner art so
        // valid/invalid placement shows normally; the menu icon and built building
        // keep their colour.
        // NOTE: the preview/under-construction prefabs' anim controllers are already
        // initialized (Util.PreInit inside BuildingLoader.CreateBuilding*) by the time
        // these hooks run, so just assigning AnimFiles does nothing -- SwapAnims is the
        // rebuild-and-rebind path.
        public static void SwapToVanillaAnim(GameObject go, string context)
        {
            try
            {
                var kbac = go.GetComponent<KBatchedAnimController>();
                KAnimFile vanilla = Assets.GetAnim("auto_miner_kanim");
                if (kbac == null || vanilla == null)
                {
                    Debug.LogWarning("[" + context + "] anim swap skipped (controller=" + (kbac != null) + ", anim=" + (vanilla != null) + ")");
                    return;
                }
                kbac.SwapAnims(new KAnimFile[] { vanilla });
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[" + context + "] anim swap failed (stays coloured): " + e);
            }
        }

        // Range visual: a forward fan (trimmed by line of sight), shown when the
        // building is selected -- same RangeVisualizer the Auto-Miner uses. Matches
        // the brain's targeting rectangle.
        public static void ConfigureComplete(GameObject go)
        {
            go.AddOrGet<LogicOperationalController>();
            go.AddOrGet<LoopingSounds>();

            // NOT industrial machinery. The vanilla Auto-Miner tags itself with
            // RoomConstraints.ConstraintTags.IndustrialMachinery, which disqualifies
            // the room it sits in from the room types that forbid it -- exactly the
            // wrong thing for a building whose whole job is to stand in a ranch.
            // Nothing generic applies that tag (all 119 users add it by hand), so we
            // simply never add it; the explicit remove is belt-and-braces in case a
            // future template change or another mod's patch slips it in.
            KPrefabID kpid = go.GetComponent<KPrefabID>();
            if (kpid != null) kpid.RemoveTag(RoomConstraints.ConstraintTags.IndustrialMachinery);

            var rv = go.AddOrGet<RangeVisualizer>();
            rv.OriginOffset = new Vector2I(0, 0);
            rv.RangeMin = new Vector2I(CritterTargetingBrain.RANGE_MIN_X, CritterTargetingBrain.RANGE_MIN_Y);
            rv.RangeMax = new Vector2I(CritterTargetingBrain.RANGE_MAX_X, CritterTargetingBrain.RANGE_MAX_Y);
            rv.TestLineOfSight = true;
            rv.BlockingCb = (int cell) => !Grid.IsValidCell(cell) || Grid.Solid[cell];
        }
    }
}
