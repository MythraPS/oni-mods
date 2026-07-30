using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace MagpieExtension
{
    // Longer Bridges' own settings file. Deliberately a plain JSON file rather
    // than a PLib options screen: this mod ships zero third-party assemblies and
    // that is worth more than an in-game settings dialog. Newtonsoft.Json is the
    // game's own dependency, already in OxygenNotIncluded_Data/Managed.
    //
    // Lives beside mods.json in the mods directory, NOT inside the mod folder,
    // so a Workshop update cannot wipe it. Written with defaults on first run.
    public class LongerBridgesSettings
    {
        private const string FILE_NAME = "LongerBridges.json";

        public string _comment =
            "ReverseButtonOnAllBridges: with Knoest's Reverse Bridges installed, also add its " +
            "Reverse button to bridges from OTHER mods that don't have one. On by default. " +
            "Set it to false if a bridge from another mod misbehaves when reversed -- that turns " +
            "this off without disabling either mod. Longer Bridges' own bridges get the button " +
            "either way.";

        public bool ReverseButtonOnAllBridges = true;

        private static LongerBridgesSettings instance;
        public static LongerBridgesSettings Instance
        {
            get
            {
                if (instance == null) instance = Load();
                return instance;
            }
        }

        private static string Path
        {
            get { return System.IO.Path.Combine(KMod.Manager.GetDirectory(), FILE_NAME); }
        }

        private static LongerBridgesSettings Load()
        {
            // Any failure here must fall back to defaults rather than throw: this
            // runs during mod load, where an exception costs the user the mod.
            try
            {
                string path = Path;
                if (File.Exists(path))
                {
                    var loaded = JsonConvert.DeserializeObject<LongerBridgesSettings>(File.ReadAllText(path));
                    if (loaded != null) return loaded;
                    Debug.LogWarning("[LongerBridges] " + FILE_NAME + " is empty or unreadable; using defaults.");
                }
                else
                {
                    var defaults = new LongerBridgesSettings();
                    File.WriteAllText(path, JsonConvert.SerializeObject(defaults, Formatting.Indented));
                    return defaults;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[LongerBridges] could not read " + FILE_NAME + "; using defaults: " + e);
            }
            return new LongerBridgesSettings();
        }
    }
}
