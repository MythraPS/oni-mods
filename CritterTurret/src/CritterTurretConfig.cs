using UnityEngine;

namespace CritterTurret
{
    // A red Auto-Miner that shoots critters instead of mining. The chassis, species
    // checklist, range visual and logic ports are shared with the wrangler; see
    // CritterBuildingTemplate.
    public class CritterTurretConfig : IBuildingConfig
    {
        public const string ID = "CritterTurret";

        public override BuildingDef CreateBuildingDef()
        {
            return CritterBuildingTemplate.CreateDef(ID, "critterturret_kanim");
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            CritterBuildingTemplate.ConfigureTemplate(go, CritterTurretTags.OtherCritters);
        }

        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
        {
            CritterBuildingTemplate.SwapToVanillaAnim(go, "turret preview");
        }

        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            CritterBuildingTemplate.SwapToVanillaAnim(go, "turret under-construction");
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            CritterBuildingTemplate.ConfigureComplete(go);
            // Custom components are safe on this install (PacusDieOutOfWater ships
            // one; the old "zero rva" crash came from Harmony-rebuilding
            // BuildingDef.IsValidPlaceLocation, not from components).
            go.AddOrGet<CritterTurretBrain>();
        }
    }
}
