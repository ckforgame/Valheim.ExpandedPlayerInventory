# Valheim.ExpandedPlayerInventory - Agent Context

See [AGENTS.md](AGENTS.md) for full project rules, architecture map, invariants, and build procedures.

## Quick Summary for AI Assistants
- **Type**: Valheim BepInEx 5.x C# Mod (.NET Framework 4.8, HarmonyX).
- **Core Functionality**: Expands player inventory height up to 50 rows while constraining UI viewport to at most 6 rows with a scrollbar and mouse-wheel scrolling.
- **Critical Invariant #1**: Never allow `Player.m_inventory.GetHeight()` to drop below configured rows before `Humanoid.DropInvalidItems()` runs. Doing so causes player item loss.
- **Critical Invariant #2**: Always clamp `InventoryGui.SetInventorySize` to at most 6 rows (`Math.Min(6, rows)`) to prevent GUI canvas overflow and incompatibility with ValheimPlus.
- **Critical Invariant #3**: Keep `RectMask2D` disabled during opening animation frame 1 or when viewport height <= 10f to prevent blank grid culling.
- **Build & Package**:
  - Build: `dotnet build -c Release`
  - Package: `powershell -ExecutionPolicy Bypass -File .\package-thunderstore.ps1`
  - Version bump requires 3 synchronized files: `ExpandedPlayerInventory.csproj`, `Plugin.cs`, and `manifest.json`.
