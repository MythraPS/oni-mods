using KSerialization;
using UnityEngine;

namespace CritterWrangler
{
    // The non-lethal sibling. Same targeting as the turret, but instead of killing
    // the critter it wrangles it outright -- exactly what a Rancher's net gun does
    // at the end of a Capture chore. The critter ends up bagged on the ground for
    // someone to carry to a Critter Drop-Off.
    //
    // The discharge replicates Capturable.OnCompleteWork(worker) step for step:
    // clear the capture mark, Baggable.SetWrangled(), then drop the bundle on the
    // Ore layer at the critter's cell (or the cell above, if it was standing in
    // solid ground). That method is protected, so it is reproduced rather than
    // called -- see Discharge below.
    //
    // Why this building exists at all: the vanilla Critter Drop-Off's "auto-wrangle
    // surplus" only fires for adults. FixedCapturableMonitor.ShouldGoGetCaptured()
    // returns def.allowBabies for a baby, and CreatureDeliveryPointConfig never sets
    // that field, so it sits at its default of false. The species checklist here
    // lists babies as their own rows, so this covers the case vanilla can't.
    [SerializationConfig(MemberSerialization.OptIn)]
    public class CritterWranglerBrain : CritterTargetingBrain
    {
        // Slower than the turret's kill shot on purpose: catching a critter alive
        // should read as work, and at 1s a full ranch emptied faster than the eye
        // could follow it.
        private const float CHARGE_TIME = 1.5f;

        // ---- persisted settings ----
        [Serialize] public float populationThreshold = 4f;
        [Serialize] public bool activateAboveThreshold = true;

        protected override float ChargeTime { get { return CHARGE_TIME; } }
        // The rope/net the dupes shoot when wrangling: BaseMinionConfig's LaserEffect
        // table maps context "capture" to CaptureEffect / net_gun_fx_kanim / "loop".
        protected override string BeamFxAnimName { get { return "net_gun_fx_kanim"; } }
        // The net gun's own sound. Unlike the attack laser this is not registered as a
        // LaserSoundEvent, so it may not loop -- if it cuts out mid-wind-up, the other
        // candidate in the bank is "NetGun_object".
        protected override string LoopSoundName { get { return "Wrangling_creature"; } }
        protected override string TitleKey { get { return "STRINGS.UI.UISIDESCREENS.CRITTERWRANGLER.OPEN_FIRE"; } }
        protected override string ValueNameKey { get { return "STRINGS.UI.UISIDESCREENS.CRITTERWRANGLER.VALUE_NAME"; } }

        // What still counts as surplus this building could do something about.
        //
        // Excluded, for three different reasons:
        //  - Already bagged. It has been wrangled and is waiting for a haul; as far
        //    as room population goes it is dealt with.
        //  - Already flagged for capture by hand. A Rancher is on the way.
        //  - Can never be wrangled at all: no Capturable component (Shockworms), or
        //    allowCapture false (Morbs, adult Beetas). Counting those would hold the
        //    population permanently over the threshold, jamming the building on a
        //    target it can never reach and pinning the logic output red forever.
        //    Trapped/Stored are deliberately NOT excluded -- those are temporary, and
        //    the critter should count again when they lift.
        protected override bool CountsTowardPopulation(KPrefabID kpid)
        {
            if (kpid.HasTag(GameTags.Creatures.Bagged)) return false;
            Capturable c = kpid.GetComponent<Capturable>();
            if (c == null || !c.allowCapture) return false;
            return !c.IsMarkedForCapture;
        }

        // Capturable.IsCapturable() is allowCapture && not Trapped/Stored/Bagged.
        // Baggable is what actually does the wrangling, so require both rather than
        // winding up on something that cannot be finished.
        protected override bool CanEngage(GameObject go)
        {
            Capturable c = go.GetComponent<Capturable>();
            return c != null && c.IsCapturable() && go.GetComponent<Baggable>() != null;
        }

        protected override void Discharge(GameObject target)
        {
            Capturable capturable = target.GetComponent<Capturable>();
            Baggable baggable = target.GetComponent<Baggable>();
            if (capturable == null || baggable == null) return;

            // Where the bundle lands. A critter caught while inside solid ground
            // would otherwise be buried, so step up one cell -- same check the
            // vanilla capture chore does.
            int cell = Grid.PosToCell(target);
            if (Grid.IsValidCell(cell) && Grid.Solid[cell])
            {
                int above = Grid.CellAbove(cell);
                if (Grid.IsValidCell(above) && !Grid.Solid[above]) cell = above;
            }

            // Clears the flag and cancels any Capture chore a duplicant had taken,
            // so nobody walks over to wrangle something already wrangled.
            capturable.MarkForCapture(false);
            baggable.SetWrangled();
            if (Grid.IsValidCell(cell))
                baggable.transform.SetPosition(Grid.CellToPosCCC(cell, Grid.SceneLayer.Ore));
        }

        public override float Threshold { get { return populationThreshold; } set { populationThreshold = value; } }
        public override bool ActivateAboveThreshold { get { return activateAboveThreshold; } set { activateAboveThreshold = value; } }
        public override string AboveToolTip { get { return "Wrangle each checked species whose own loose population in the room is above {0}"; } }
        public override string BelowToolTip { get { return "Wrangle each checked species whose own loose population in the room is below {0}"; } }
    }
}
