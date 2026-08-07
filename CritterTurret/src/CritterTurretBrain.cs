using System.Collections.Generic;
using System.Runtime.CompilerServices;
using KSerialization;
using UnityEngine;

namespace CritterTurret
{
    // The lethal turret. Everything about finding a target lives in
    // CritterTargetingBrain; this adds the wind-up length and the kill.
    //
    // The [Serialize] field names below are load-bearing -- turrets built before
    // v1.2.0 carry them in their save data, so they must not be renamed or moved
    // into the base class.
    [SerializationConfig(MemberSerialization.OptIn)]
    public class CritterTurretBrain : CritterTargetingBrain
    {
        // The turret winds up on a locked target for CHARGE_TIME, then discharges
        // once and kills it outright, whether it's a 5 HP Shine Bug or a 400 HP
        // Smooth Hatch. This is also the gap between kills, since the wind-up is
        // the whole cycle -- three seconds is slow enough to watch a cull happen
        // and stop it, rather than a herd disappearing between glances.
        private const float CHARGE_TIME = 3f;

        // ---- persisted settings ----
        [Serialize] public float populationThreshold = 4f;
        [Serialize] public bool activateAboveThreshold = true;

        // Critters this turret has already shot, which the room has not caught up
        // with yet. A kill does not leave the room's creature list until the corpse
        // GameObject is destroyed, so between the shot and that moment the game
        // still reports the critter as living population. Firing once a second off a
        // count that stale is how a herd got wiped out instead of thinned: every
        // shot after the first was aimed using a number that already included the
        // ones lying dead on the floor.
        //
        // Deliberately not [Serialize]d. It only describes shots in flight this
        // session; on load the room is whatever it is, and an empty set is right.
        //
        // The comparer is not optional. KPrefabID overrides Equals to compare
        // PrefabTag and GetHashCode to return the tag's hash, so a default
        // HashSet<KPrefabID> is a set of SPECIES, not of individuals -- one shot
        // Puft Prince would mark every Puft Prince in the room as already dealt
        // with, and the turret would stop after a single kill.
        private readonly HashSet<KPrefabID> spent = new HashSet<KPrefabID>(new ByReference());

        // The subset of `spent` whose corpse has already been hurried along, so a
        // corpse is never pushed through its death state machine twice.
        private readonly HashSet<KPrefabID> hurried = new HashSet<KPrefabID>(new ByReference());

        // Identity, not equality: these are specific critters, not a kind of critter.
        private sealed class ByReference : IEqualityComparer<KPrefabID>
        {
            public bool Equals(KPrefabID a, KPrefabID b) { return ReferenceEquals(a, b); }
            public int GetHashCode(KPrefabID k) { return RuntimeHelpers.GetHashCode(k); }
        }

        protected override float ChargeTime { get { return CHARGE_TIME; } }
        protected override string LoopSoundName { get { return "AttackLaser_gun"; } }
        protected override string TitleKey { get { return "STRINGS.UI.UISIDESCREENS.CRITTERTURRET.OPEN_FIRE"; } }
        protected override string ValueNameKey { get { return "STRINGS.UI.UISIDESCREENS.CRITTERTURRET.VALUE_NAME"; } }

        // The catch-all category for critters that were never built to be caught
        // (Shockworms, hives). The turret can shoot those; only it offers the row.
        protected override Tag? ExtraCategory { get { return CritterTurretTags.OtherCritters; } }
        protected override void RegisterChecklistCategories() { CritterTurretTags.EnsureOtherCrittersRegistered(); }

        // Only things that can actually be killed, and never one already shot --
        // that drops the lock the instant the discharge lands rather than waiting
        // for hitPoints to be observed at zero.
        protected override bool CanEngage(GameObject go)
        {
            KPrefabID kpid = go.GetComponent<KPrefabID>();
            if (kpid != null && spent.Contains(kpid)) return false;
            Health health = go.GetComponent<Health>();
            return health != null && health.hitPoints > 0f;
        }

        // Already shot, so already gone as far as the threshold is concerned. This
        // is the same trick the wrangler uses for critters it has bagged: work a
        // room through once instead of re-counting what has been dealt with.
        protected override bool CountsTowardPopulation(KPrefabID kpid)
        {
            return !spent.Contains(kpid);
        }

        // Drop the ones the game has finished destroying. They have left the room's
        // creature list by now, so remembering them no longer changes any count --
        // it would only pin dead objects in memory for the rest of the colony.
        protected override void PruneRememberedCritters()
        {
            if (spent.Count == 0) return;
            spent.RemoveWhere(k => k == null);
            hurried.RemoveWhere(k => k == null);
            foreach (KPrefabID kpid in spent)
                if (!hurried.Contains(kpid) && HurryCorpse(kpid)) hurried.Add(kpid);
        }

        // Vanilla parks a fresh corpse in its death animation until the animation
        // ends or DeathStates.Def.DIE_ANIMATION_EXPIRATION_TIME (4s) runs out,
        // whichever comes first, and only then butchers it and deletes the object.
        // Nothing waits on that except the look of it -- and while it lasts the
        // room still lists the critter, which is the same corpse window that used
        // to confuse the population count. Sending the corpse straight to the post
        // state runs the ordinary butcher-then-destroy path with the wait removed,
        // so the meat still drops.
        //
        // Returns false while there is no state machine to send anywhere: the die
        // chore starts it a moment after the kill, so this is null on the tick of
        // the shot and picked up on a later one.
        private static bool HurryCorpse(KPrefabID kpid)
        {
            DeathStates.Instance smi = kpid.gameObject.GetSMI<DeathStates.Instance>();
            if (smi == null || smi.sm == null) return false;
            smi.GoTo(smi.sm.pst);
            return true;
        }

        protected override void Discharge(GameObject target)
        {
            Health health = target.GetComponent<Health>();
            if (health == null) return;
            KPrefabID kpid = target.GetComponent<KPrefabID>();
            if (kpid != null) spent.Add(kpid);
            // One discharge, one kill. Overkill by design: hitPoints covers an
            // already-wounded critter, maxHitPoints covers anything that heals
            // between the charge starting and the shot landing.
            health.Damage(Mathf.Max(health.hitPoints, health.maxHitPoints) + 1f);
            // The AttackLaser_gun loop is the wind-up; this is the crack of the
            // discharge on top of it.
            string s = GlobalAssets.GetSound("Building_Dmg_Metal", false);
            if (!string.IsNullOrEmpty(s)) KFMOD.PlayOneShot(s, Muzzle());
        }

        public override float Threshold { get { return populationThreshold; } set { populationThreshold = value; } }
        public override bool ActivateAboveThreshold { get { return activateAboveThreshold; } set { activateAboveThreshold = value; } }
        public override string AboveToolTip { get { return "Fire at each checked species whose own population in the room is above {0}"; } }
        public override string BelowToolTip { get { return "Fire at each checked species whose own population in the room is below {0}"; } }
    }
}
