using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace AutoRefinement
{
    // Shown by the gear icon next to this mod in the Mods menu.
    //
    // PLib renders scalar properties only, so a 36-entry list of prefab IDs cannot be a
    // control. Grouping the buildings into categories gives the same reach in six
    // checkboxes, and ManualBuildings stays in the JSON for per-building exceptions.
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile("AutoRefinement.json", true, false)]
    [ModInfo("https://github.com/MythraPS/oni-mods")]
    [RestartRequired]
    public sealed class AutoRefinementOptions
    {
        // PLib keeps categories in a SortedList, so they render in alphabetical order.
        // These names are chosen so that reads top-to-bottom: A, D, O.
        private const string CAT_AUTO = "Automatic buildings";
        private const string CAT_HELP = "Duplicant help";
        private const string CAT_OTHER = "Other settings";

        [Option("Refinement and materials",
                "Metal Refinery, Rock Crusher, Glass Forge, Supermaterial Refinery, presses, "
                + "kilns and furnaces, including modded ones.", CAT_AUTO)]
        [JsonProperty] public bool Refinement { get; set; }

        [Option("Cooking and food",
                "Electric Grill, Gourmet Cooking Station, Microbe Musher, Deep Fryer, Sushi Bar, "
                + "Egg Cracker.\n\nThis one is a real difficulty change: it removes duplicant "
                + "time from the whole food chain.", CAT_AUTO)]
        [JsonProperty] public bool Cooking { get; set; }

        [Option("Medicine", "Apothecary and Advanced Apothecary.", CAT_AUTO)]
        [JsonProperty] public bool Medicine { get; set; }

        [Option("Clothing and suits",
                "Textile Loom, Clothing Alteration Station, Exosuit Forge.", CAT_AUTO)]
        [JsonProperty] public bool Clothing { get; set; }

        [Option("Crafting and misc",
                "Crafting Table, Advanced Crafting Table, Fossil Dig and similar.", CAT_AUTO)]
        [JsonProperty] public bool Crafting { get; set; }

        [Option("Research and space",
                "Orbital Research Centre, telescopes, Missile Fabricator, Radbolt Generator.",
                CAT_AUTO)]
        [JsonProperty] public bool Research { get; set; }

        [Option("Anything else",
                "Buildings this mod does not recognise -- usually from other mods. Leave on to "
                + "automate everything, off to only touch the categories above.", CAT_AUTO)]
        [JsonProperty] public bool Unrecognised { get; set; }

        [Option("Duplicants can still help",
                "Keeps the operate errand, so a duplicant may work the building -- it just "
                + "isn't required. Off means fully automatic, with no errand at all.", CAT_HELP)]
        [JsonProperty] public bool DuplicantsCanHelp { get; set; }

        [Option("Speed with nobody on it (%)",
                "Craft speed with no duplicant working, against vanilla speed. 100 = normal.",
                CAT_HELP)]
        [Limit(1, 500)]
        [JsonProperty] public int UnattendedSpeed { get; set; }

        [Option("Speed with a duplicant (%)",
                "Craft speed while a duplicant works it, against vanilla speed. 150 = half "
                + "again as fast. Machinery skill still applies on top.", CAT_HELP)]
        [Limit(1, 500)]
        [JsonProperty] public int AttendedSpeed { get; set; }

        [Option("Desalinator empties itself",
                "The Desalinator is not a fabricator, so the categories above do not affect it. "
                + "This drops its salt automatically instead of queueing a duplicant errand. The "
                + "salt lands on the floor where a dupe would have left it, so an auto-sweeper "
                + "in range still collects it.", CAT_OTHER)]
        [JsonProperty] public bool AutomaticDesalinator { get; set; }

        [Option("Show progress bar",
                "Vanilla automatic fabricators (Kiln, Rubber Maker) show a progress bar in place "
                + "of the duplicant animation. Turn off to hide it.", CAT_OTHER)]
        [JsonProperty] public bool ShowProgressBar { get; set; }

        [Option("Log building IDs",
                "Writes every affected building ID to the Player log. Use it to find IDs for the "
                + "ManualBuildings list in AutoRefinement.json.", CAT_OTHER)]
        [JsonProperty] public bool LogFabricators { get; set; }

        // No [Option]: PLib cannot render a list. Edit AutoRefinement.json directly for
        // per-building exceptions. Always wins over the category toggles above.
        [JsonProperty] public List<string> ManualBuildings { get; set; }

        public AutoRefinementOptions()
        {
            Refinement = true;
            Cooking = true;
            Medicine = true;
            Clothing = true;
            Crafting = true;
            Research = true;
            Unrecognised = true;
            DuplicantsCanHelp = true;
            UnattendedSpeed = 100;
            AttendedSpeed = 150;
            AutomaticDesalinator = true;
            ShowProgressBar = true;
            LogFabricators = true;
            ManualBuildings = new List<string>();
        }

        private enum Group { Refinement, Cooking, Medicine, Clothing, Crafting, Research, Unknown }

        // Classification is by prefab ID because that is the only stable handle a building
        // has across the game and every mod. Anything absent falls to Unknown, which is why
        // the "Anything else" toggle exists.
        private static readonly Dictionary<string, Group> GROUPS =
            new Dictionary<string, Group>(StringComparer.OrdinalIgnoreCase)
        {
            // Vanilla / DLC refinement
            { "MetalRefinery", Group.Refinement },
            { "RockCrusher", Group.Refinement },
            { "GlassForge", Group.Refinement },
            { "SupermaterialRefinery", Group.Refinement },
            { "ChemicalRefinery", Group.Refinement },
            { "DiamondPress", Group.Refinement },
            { "SludgePress", Group.Refinement },
            { "MilkPress", Group.Refinement },
            { "FabricatedWoodMaker", Group.Refinement },
            { "OxyliteRefinery", Group.Refinement },
            // Ronivan's Legacy - Industrial Revolution
            { "Chemical_AdvancedKiln", Group.Refinement },
            { "Chemical_GlassFoundry", Group.Refinement },
            { "Chemical_SelectiveArcFurnace", Group.Refinement },
            { "CementMixer", Group.Refinement },
            { "Metallurgy_PlasmaFurnace", Group.Refinement },
            { "Chemical_AdvancedMetalRefinery", Group.Refinement },
            { "HepCalcinator", Group.Refinement },
            { "Metallurgy_PyrolysisKiln", Group.Refinement },
            { "Custom_MetalRefinery", Group.Refinement },
            { "Chemical_MixingUnit", Group.Refinement },
            { "Chemical_BallCrusherMill", Group.Refinement },
            { "Chemical_SmallCrusherMill", Group.Refinement },
            { "Chemical_SoilMixer", Group.Refinement },
            { "Biochemistry_ExpellerPress", Group.Refinement },

            { "CookingStation", Group.Cooking },
            { "GourmetCookingStation", Group.Cooking },
            { "MicrobeMusher", Group.Cooking },
            { "Deepfryer", Group.Cooking },
            { "SushiBar", Group.Cooking },
            { "EggCracker", Group.Cooking },

            { "Apothecary", Group.Medicine },
            { "AdvancedApothecary", Group.Medicine },

            { "ClothingFabricator", Group.Clothing },
            { "ClothingAlterationStation", Group.Clothing },
            { "SuitFabricator", Group.Clothing },

            { "CraftingTable", Group.Crafting },
            { "AdvancedCraftingTable", Group.Crafting },
            { "GenericFabricator", Group.Crafting },
            { "FossilDig", Group.Crafting },

            { "OrbitalResearchCenter", Group.Research },
            { "RTB_DeepSpaceResearchTelescope", Group.Research },
            { "ManualHighEnergyParticleSpawner", Group.Research },
            { "MissileFabricator", Group.Research },
        };

        private HashSet<string> manual;

        public bool Matches(string prefabId)
        {
            if (string.IsNullOrEmpty(prefabId)) return false;

            if (manual == null)
            {
                manual = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (ManualBuildings != null)
                    foreach (string b in ManualBuildings)
                        if (!string.IsNullOrEmpty(b)) manual.Add(b.Trim());
            }
            if (manual.Contains(prefabId)) return false;

            Group g;
            if (!GROUPS.TryGetValue(prefabId, out g)) g = Group.Unknown;

            switch (g)
            {
                case Group.Refinement: return Refinement;
                case Group.Cooking: return Cooking;
                case Group.Medicine: return Medicine;
                case Group.Clothing: return Clothing;
                case Group.Crafting: return Crafting;
                case Group.Research: return Research;
                default: return Unrecognised;
            }
        }

        private static AutoRefinementOptions instance;
        public static AutoRefinementOptions Instance
        {
            get
            {
                if (instance == null)
                {
                    // Reading can fail on a malformed file; defaults are better than no mod.
                    try { instance = POptions.ReadSettings<AutoRefinementOptions>(); }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogWarning(
                            "[AutoRefinement] could not read options; using defaults: " + e);
                    }
                    if (instance == null) instance = new AutoRefinementOptions();
                }
                return instance;
            }
        }
    }
}
