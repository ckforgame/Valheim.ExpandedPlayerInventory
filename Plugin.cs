using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace ExpandedPlayerInventory
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class ExpandedPlayerInventoryPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.custom.expandedplayerinventory";
        public const string ModName = "ExpandedPlayerInventory";
        public const string ModVersion = "1.0.2";

        public static ExpandedPlayerInventoryPlugin Instance { get; private set; } = null!;
        public static ManualLogSource Log { get; private set; } = null!;

        public static ConfigEntry<int> PlayerInventoryRows { get; private set; } = null!;

        private Harmony _harmony = null!;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            PlayerInventoryRows = Config.Bind(
                "General",
                "playerInventoryRows",
                20,
                new ConfigDescription("Number of player inventory rows (min 4, max 50).", new AcceptableValueRange<int>(4, 50))
            );

            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll();

            Log.LogInfo($"{ModName} {ModVersion} loaded successfully! Configured rows: {PlayerInventoryRows.Value}");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}