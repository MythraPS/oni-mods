using UnityEngine;

namespace CritterWrangler
{
    // The non-lethal sibling: a teal Auto-Miner that flags surplus critters for a
    // rancher to wrangle instead of shooting them.
    public class CritterWranglerConfig : IBuildingConfig
    {
        public const string ID = "CritterWrangler";

        public override BuildingDef CreateBuildingDef()
        {
            return CritterBuildingTemplate.CreateDef(ID, "critterwrangler_kanim");
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            // No extra category: critters that were never built to be caught have no
            // Capturable at all, so listing them would only offer dead options.
            CritterBuildingTemplate.ConfigureTemplate(go);
        }

        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
        {
            CritterBuildingTemplate.SwapToVanillaAnim(go, "wrangler preview");
        }

        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            CritterBuildingTemplate.SwapToVanillaAnim(go, "wrangler under-construction");
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            CritterBuildingTemplate.ConfigureComplete(go);
            go.AddOrGet<CritterWranglerBrain>();
        }
    }
}
