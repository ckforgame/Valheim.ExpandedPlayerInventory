# Rule: Item Safety & Data Layer Invariants

## Context
In Valheim, the player inventory data layer is represented by `Inventory` objects. Items have coordinates `(m_pos.x, m_pos.y)`.
When the game or any mod invokes `Humanoid.DropInvalidItems()`, any item where `m_pos.y >= m_height` is permanently dropped on the ground or lost into the void.

## Mandatory Rules
1. **Never reduce player inventory height below configured value**:
   - `Player.SetInventorySize` has an IL transpiler and a postfix safety net.
   - Any modifications to inventory sizing logic must preserve the `AtLeastConfigured(rows)` logic.
2. **First-Priority Safety Net on `Humanoid.DropInvalidItems`**:
   - `Humanoid_DropInvalidItems_Patch` must retain `[HarmonyPriority(Priority.First)]`.
   - Before `DropInvalidItems` executes, it checks:
     ```csharp
     if (inventory != null && inventory.GetHeight() < configRows)
     {
         inventory.SetHeight(configRows);
     }
     ```
3. **Save/Load Integrity**:
   - `Player.Load` Prefix and `Player.OnSpawned` Postfix ensure the local player's inventory height is refreshed to at least `PlayerInventoryRows.Value` upon character loading or respawning.
