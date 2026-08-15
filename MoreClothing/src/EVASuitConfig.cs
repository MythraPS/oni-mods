using System.Collections.Generic;
using Klei.AI;
using UnityEngine;

namespace ProtectiveWear
{
    // EVA Suit: a lightweight, checkpoint-free alternative to the Atmo Suit.
    //
    // It lives in the regular CLOTHING slot (delegating to WarmVestConfig), so
    // dupes put it on like any vest -- no Atmo Suit Checkpoint or dock. That
    // also means it can't carry a real oxygen tank the way an Atmo Suit does,
    // so instead of "never breathes" it grants a much larger breath reserve
    // (BreathMax) -- the dupe simply lasts a lot longer before running out of
    // air. Everything else is layered on as equipment AttributeModifiers, the
    // same mechanism the Atmo/Lead suits use, so the game applies them on
    // equip and removes them on unequip automatically:
    //
    //   Insulation          +30    slows cold/heat exchange (mild, both ways)
    //   ScaldingThreshold   +15    tolerate hotter surroundings before Scalding
    //   ScoldingThreshold   -40    tolerate colder surroundings before Frostbite
    //   RadiationResistance +0.30  mild rad shielding (Lead Suit is 0.66)
    //   BreathMax          +200    a longer lungful before suffocation
    //   BladderDelta       -0.05   bladder fills slower -> fewer bathroom trips
    // Frostbite and Low Oxygen are RESISTED, not negated -- a truly extreme freeze or
    // a long stint in vacuum will still get through eventually.
    //
    // Deliberately weaker than an Atmo Suit (which fully seals + carries O2),
    // balanced by mild values, a two-garment + fiber craft cost, and the fact
    // that it occupies the clothing slot (no snazzy suit / warm coat alongside).
    public class EVASuitConfig : IEquipmentConfig
    {
        public const string ID = "EVASuit";

        // Extra breath reserve the suit grants over a normal dupe's lungful.
        // Shared so the helmet manager knows where "normal" ends for its
        // two-tier refill (fast up to normal, slow for the bonus above it).
        public const float BREATH_BONUS = 200f;

        // Delegate to the Warm Sweater: gives us the clothing slot, a worn body
        // kanim, and its own base cold insulation -- all fitting for a suit.
        private readonly WarmVestConfig vanilla = new WarmVestConfig();

        public EquipmentDef CreateEquipmentDef()
        {
            EquipmentDef def = vanilla.CreateEquipmentDef();
            def.Id = ID;
            def.Anim = Assets.GetAnim((HashedString)"eva_suit_item_kanim");
            def.BuildOverride = Assets.GetAnim((HashedString)"body_eva_suit_kanim");

            if (def.AttributeModifiers == null)
                def.AttributeModifiers = new List<AttributeModifier>();

            Db db = Db.Get();
            var mods = new[]
            {
                new KeyValuePair<string, float>(db.Attributes.Insulation.Id, 30f),
                new KeyValuePair<string, float>(db.Attributes.ScaldingThreshold.Id, 15f),
                // Frostbite RESISTANCE (not immunity): ScoldingThreshold is the cold
                // side of the temperature monitor -- frostbite fires when external temp
                // < threshold, so a negative modifier lets the dupe tolerate ~40 C
                // colder before Frostbite sets in. Still freezes in truly extreme cold.
                new KeyValuePair<string, float>(db.Attributes.ScoldingThreshold.Id, -40f),
                new KeyValuePair<string, float>(db.Attributes.RadiationResistance.Id, 0.30f),
                new KeyValuePair<string, float>(db.Amounts.Breath.maxAttribute.Id, BREATH_BONUS),
                new KeyValuePair<string, float>(db.Amounts.Bladder.deltaAttribute.Id, -0.05f),
            };
            foreach (var m in mods)
                def.AttributeModifiers.Add(new AttributeModifier(m.Key, m.Value, "Soft Suit", false));

            // Sealed suit: resist the wet effects (Sopping Wet, Soggy Feet), the
            // ambient-temperature moods (ColdAir = "Chilly Surroundings", WarmAir =
            // "Warm Surroundings") like the Atmo Suit does, plus eye irritation
            // (Minor/Major Irritation).
            // NOTE: "Frostbite" is a STATUS ITEM, not a Klei.AI.Effect, so it can't be
            // granted here (Effects.Get logs a fatal error on an unknown id and crashes
            // prefab load). It's RESISTED via ScoldingThreshold above instead.
            // "LowOxygen" IS used as an effect id -- SuffocationMonitor's "satisfied.low"
            // state calls ToggleEffect("LowOxygen") -- so immunity is listed here as a
            // backstop. The real cure is in EVAHelmetManager: thin oxygen now counts as
            // unbreathable, so the suit seals and the wearer breathes suit air, and the
            // monitor never enters the low state to begin with. TryGet means an id that
            // ever stops existing is skipped rather than crashing.
            if (def.EffectImmunites == null)
                def.EffectImmunites = new List<Effect>();
            // "ContaminatedLungs" is "Yucky Lungs", from inhaling Polluted Oxygen.
            // Listed here for completeness; what actually prevents it is the
            // CoughMonitor.OnBreatheDirtyAir patch in EVAHelmetManager, since the
            // effect is only ever applied at the end of a cough.
            foreach (string effId in new[] { "SoakingWet", "WetFeet", "ColdAir", "WarmAir", "MinorIrritation", "MajorIrritation", "PoppedEarDrums", "Sunlight_Burning", "LowOxygen", "ContaminatedLungs" })
            {
                Effect eff = db.effects.TryGet(effId);   // TryGet: no error log / crash if an id is ever absent
                if (eff != null && !def.EffectImmunites.Contains(eff))
                    def.EffectImmunites.Add(eff);
            }

            // Track wearers so the dynamic helmet appears in dangerous air,
            // and fill the SHOES slot with the suit's own hidden boots.
            System.Action<Equippable> baseEquip = def.OnEquipCallBack;
            System.Action<Equippable> baseUnequip = def.OnUnequipCallBack;
            def.OnEquipCallBack = (Equippable eq) =>
            {
                baseEquip?.Invoke(eq);
                EVAHelmetManager.OnEquip(eq);
                EVABoots.OnSuitEquipped(eq);
            };
            def.OnUnequipCallBack = (Equippable eq) =>
            {
                EVABoots.OnSuitUnequipped(eq);
                EVAHelmetManager.OnUnequip(eq);
                baseUnequip?.Invoke(eq);
            };

            return def;
        }

        public void DoPostConfigure(GameObject go)
        {
            vanilla.DoPostConfigure(go);
        }

        public string[] GetDlcIds()
        {
            return null;
        }
    }
}
