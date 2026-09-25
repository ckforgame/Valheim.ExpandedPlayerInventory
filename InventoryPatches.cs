using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ExpandedPlayerInventory
{
    [HarmonyPatch(typeof(Player), nameof(Player.SetInventorySize))]
    public static class Player_SetInventorySize_Patch
    {
        private static readonly MethodInfo Method_Inventory_SetHeight =
            AccessTools.Method(typeof(Inventory), nameof(Inventory.SetHeight));

        private static readonly MethodInfo Method_InventoryGui_SetInventorySize =
            AccessTools.Method(typeof(InventoryGui), nameof(InventoryGui.SetInventorySize));

        private static readonly MethodInfo Method_AtLeastConfigured =
            AccessTools.Method(typeof(Player_SetInventorySize_Patch), nameof(AtLeastConfigured));

        private static readonly MethodInfo Method_GuiRows =
            AccessTools.Method(typeof(Player_SetInventorySize_Patch), nameof(GuiRows));

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            if (ExpandedPlayerInventoryPlugin.PlayerInventoryRows.Value <= 4)
            {
                return instructions;
            }

            var il = instructions.ToList();
            try
            {
                return new CodeMatcher(il)
                    .MatchStartForward(new CodeMatch(i => i.Calls(Method_Inventory_SetHeight)))
                    .ThrowIfNotMatch("No match for this.m_inventory.SetHeight(rows).")
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Call, Method_AtLeastConfigured))
                    .MatchStartForward(new CodeMatch(i => i.Calls(Method_InventoryGui_SetInventorySize)))
                    .ThrowIfNotMatch("No match for InventoryGui.instance.SetInventorySize(rows).")
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Call, Method_GuiRows))
                    .InstructionEnumeration();
            }
            catch (Exception e)
            {
                ExpandedPlayerInventoryPlugin.Log.LogError($"Player_SetInventorySize_Patch transpiler failed: {e}");
                return il;
            }
        }

        /// <summary>
        /// Defensive safety net: Ensures inventory height is never clamped down by game or other systems
        /// even if the transpiler was bypassed or partially overridden.
        /// </summary>
        public static void Postfix(Player __instance)
        {
            if (__instance == null || ExpandedPlayerInventoryPlugin.PlayerInventoryRows.Value <= 4) return;
            try
            {
                var inventory = __instance.GetInventory();
                int configRows = ExpandedPlayerInventoryPlugin.PlayerInventoryRows.Value;
                if (inventory != null && inventory.GetHeight() < configRows)
                {
                    inventory.SetHeight(configRows);
                }
            }
            catch (Exception e)
            {
                ExpandedPlayerInventoryPlugin.Log.LogError($"Player_SetInventorySize_Patch Postfix error: {e}");
            }
        }

        public static int AtLeastConfigured(int rows)
        {
            return Math.Max(rows, ExpandedPlayerInventoryPlugin.PlayerInventoryRows.Value);
        }

        public static int GuiRows(int rows)
        {
            return Math.Min(6, AtLeastConfigured(rows));
        }
    }

    /// <summary>
    /// Critical protection: Ensures player inventory height is fully expanded before Humanoid checks
    /// for out-of-bounds items, preventing accidental item ejection/dropping upon loading or resizing.
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.DropInvalidItems))]
    public static class Humanoid_DropInvalidItems_Patch
    {
        [HarmonyPriority(Priority.First)]
        public static void Prefix(Humanoid __instance)
        {
            try
            {
                if (__instance is Player player && ExpandedPlayerInventoryPlugin.PlayerInventoryRows.Value > 4)
                {
                    var inventory = player.GetInventory();
                    int configRows = ExpandedPlayerInventoryPlugin.PlayerInventoryRows.Value;
                    if (inventory != null && inventory.GetHeight() < configRows)
                    {
                        inventory.SetHeight(configRows);
                    }
                }
            }
            catch (Exception e)
            {
                ExpandedPlayerInventoryPlugin.Log.LogError($"Humanoid_DropInvalidItems_Patch error: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Load))]
    public static class Player_Load_Patch
    {
        public static void Prefix(Player __instance)
        {
            try
            {
                if (__instance == null || ExpandedPlayerInventoryPlugin.PlayerInventoryRows.Value <= 4) return;

                var inventory = __instance.GetInventory();
                int rows = ExpandedPlayerInventoryPlugin.PlayerInventoryRows.Value;
                if (inventory != null && inventory.GetHeight() < rows)
                {
                    inventory.SetHeight(rows);
                }
            }
            catch (Exception e)
            {
                ExpandedPlayerInventoryPlugin.Log.LogError($"Player_Load_Patch error: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    public static class Player_OnSpawned_Patch
    {
        public static void Postfix(Player __instance)
        {
            try
            {
                if (__instance == null || __instance != Player.m_localPlayer) return;
                int configRows = ExpandedPlayerInventoryPlugin.PlayerInventoryRows.Value;
                if (configRows <= 4) return;

                var inventory = __instance.GetInventory();
                if (inventory != null)
                {
                    int rows = Math.Max(inventory.GetHeight(), configRows);
                    inventory.SetHeight(rows);

                    if (InventoryGui.instance != null)
                    {
                        InventoryGui.instance.SetInventorySize(Math.Min(6, rows));
                    }
                }
            }
            catch (Exception e)
            {
                ExpandedPlayerInventoryPlugin.Log.LogError($"Player_OnSpawned_Patch error: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetInventorySize))]
    public static class InventoryGui_SetInventorySize_Patch
    {
        [HarmonyPriority(Priority.First)]
        public static void Prefix(InventoryGui __instance, ref int rows)
        {
            try
            {
                int configRows = ExpandedPlayerInventoryPlugin.PlayerInventoryRows.Value;
                if (configRows <= 4) return;

                int visibleRows = Math.Min(6, Math.Max(4, configRows));
                // Intercept any mod (such as ValheimPlus) or game call trying to expand m_player to 20 rows.
                // Sizing the UI container beyond visible rows pushes the grid off-screen and corrupts viewport layout.
                rows = visibleRows;
            }
            catch (Exception e)
            {
                ExpandedPlayerInventoryPlugin.Log.LogError($"InventoryGui_SetInventorySize_Patch error: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(Game), nameof(Game.Logout))]
    public static class Game_Logout_Patch
    {
        public static void Prefix()
        {
            try
            {
                InventoryGui_Show_Patch.ResetSessionState();
            }
            catch (Exception e)
            {
                ExpandedPlayerInventoryPlugin.Log.LogError($"Game_Logout_Patch error: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    public static class Game_Start_Patch
    {
        public static void Prefix()
        {
            try
            {
                InventoryGui_Show_Patch.ResetSessionState();
            }
            catch (Exception e)
            {
                ExpandedPlayerInventoryPlugin.Log.LogError($"Game_Start_Patch error: {e}");
            }
        }
    }
}