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
        // Smooth Hatch.
        private const float CHARGE_TIME = 1f;

        // ---- persisted settings ----
        [Serialize] public float populationThreshold = 4f;
        [Serialize] public bool activateAboveThreshold = true;

        protected override float ChargeTime { get { return CHARGE_TIME; } }
        protected override string LoopSoundName { get { return "AttackLaser_gun"; } }
        protected override string TitleKey { get { return "STRINGS.UI.UISIDESCREENS.CRITTERTURRET.OPEN_FIRE"; } }
        protected override string ValueNameKey { get { return "STRINGS.UI.UISIDESCREENS.CRITTERTURRET.VALUE_NAME"; } }

        // The catch-all category for critters that were never built to be caught
        // (Shockworms, hives). The turret can shoot those; only it offers the row.
        protected override Tag? ExtraCategory { get { return CritterTurretTags.OtherCritters; } }
        protected override void RegisterChecklistCategories() { CritterTurretTags.EnsureOtherCrittersRegistered(); }

        // Only things that can actually be killed.
        protected override bool CanEngage(GameObject go)
        {
            Health health = go.GetComponent<Health>();
            return health != null && health.hitPoints > 0f;
        }

        protected override void Discharge(GameObject target)
        {
            Health health = target.GetComponent<Health>();
            if (health == null) return;
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
