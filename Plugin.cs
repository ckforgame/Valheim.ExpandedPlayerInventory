using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace ExpandedPlayerInventory
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class ExpandedPlayerInventoryPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "ckforgame.ExpandedPlayerInventory";
        public const string ModName = "ExpandedPlayerInventory";
        public const string ModVersion = "1.1.0";

        public static ExpandedPlayerInventoryPlugin Instance { get; private set; } = null!;
        public static ManualLogSource Log { get; private set; } = null!;

        public static ConfigEntry<int> PlayerInventoryRows { get; private set; } = null!;
        public static ConfigEntry<float> ScrollSensitivity { get; private set; } = null!;

        private Harmony _harmony = null!;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            // Migrate configuration from legacy GUID if needed
            string oldConfigPath = Path.Combine(Paths.ConfigPath, "com.custom.expandedplayerinventory.cfg");
            string newConfigPath = Path.Combine(Paths.ConfigPath, $"{ModGuid}.cfg");
            if (File.Exists(oldConfigPath) && !File.Exists(newConfigPath))
            {
                try
                {
                    File.Copy(oldConfigPath, newConfigPath);
                    Log.LogInfo($"Migrated old configuration from '{oldConfigPath}' to '{newConfigPath}'.");
                    Config.Reload();
                }
                catch (Exception ex)
                {
                    Log.LogWarning($"Failed to migrate old configuration: {ex.Message}");
                }
            }

            PlayerInventoryRows = Config.Bind(
                "General",
                "playerInventoryRows",
                20,
                new ConfigDescription("Number of player inventory rows (min 4, max 50).", new AcceptableValueRange<int>(4, 50))
            );

            ScrollSensitivity = Config.Bind(
                "General",
                "scrollSensitivity",
                350f,
                new ConfigDescription("Mouse wheel scroll sensitivity for player inventory (default 350, min 50, max 1500). Higher values scroll faster with less finger movement.", new AcceptableValueRange<float>(50f, 1500f))
            );

            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll();

            Log.LogInfo($"{ModName} {ModVersion} loaded successfully! Configured rows: {PlayerInventoryRows.Value}, Scroll sensitivity: {ScrollSensitivity.Value}");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}