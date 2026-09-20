# Changelog

All notable changes to this project will be documented in this file.

## [1.1.0] - 2026-09-20

### Added
- **Configurable Mouse Wheel Scroll Sensitivity**: Added `scrollSensitivity` setting in `ckforgame.ExpandedPlayerInventory.cfg` (default `350`, range `50`–`1500`).
  - Scrolling is now ~5x faster by default, allowing effortless navigation through large inventories without excessive wheel swiping.
  - Fully adjustable via configuration file or BepInEx Configuration Manager in-game.
- **Valheim Community Standard Plugin GUID & Config**: Standardized Mod GUID to `ckforgame.ExpandedPlayerInventory` and configuration filename to `ckforgame.ExpandedPlayerInventory.cfg`. Automatically migrates settings from legacy `com.custom.expandedplayerinventory.cfg` on first run so existing configurations are seamlessly preserved.

### Fixed
- **ValheimPlus Mod Conflict & Negative Viewport Bounds**: Resolved conflict where ValheimPlus resized the background frame (`m_player`) to the full configured row count (e.g. 20 rows / 990.5px). Stretch-anchored grids evaluated to a negative viewport height (`-566.0px`), causing `RectMask2D` to cull all inventory slots on first open:
  - Added `InventoryGui_SetInventorySize_Patch` (`Priority.First`) to clamp background frame sizing to visible rows (maximum 6).
  - Decoupled `m_playerGrid` and scrollbar from parent stretch anchors to fixed top anchors (`anchorMin.y = 1f, anchorMax.y = 1f, pivot.y = 1f`), guaranteeing positive viewport height (`+424.5px`) at all times.
- **Repeated 20-Row Inventory Flashing on Open**: Fixed an issue where opening synchronization disabled `RectMask2D` on every single inventory open, briefly displaying all 20 unclipped rows across the screen for 2-3 frames:
  - Opening synchronization and delayed clipping now strictly run once on the first open of each world session.
  - Subsequent inventory opens in the same world session render instantly, smoothly, and cleanly with 6 rows and scrollbar already in place.
  - Automatically resets session state on `Game.Logout`, `Game.Start`, and `InventoryGui.Awake`.
- **First-Time Open Blank Background Failsafe**: Added continuous runtime check in `InventoryGui.Update` that immediately disables `RectMask2D` if viewport height is non-positive or tiny, preventing graphics from being culled during canvas layout initialization.

## [1.0.4] - 2026-09-19

### Fixed
- **First-time inventory open bug (definitive fix)**: Resolved the root cause where `RectMask2D.PerformClipping()` was called during the Animator's scale-0 opening transition, causing all inventory slots to be culled as invisible. Clipping is now deferred to the Update loop and only performed after the panel reaches non-zero world-space size.
- **Mod compatibility (ValheimPlus, etc.)**: Fixed conflict where other mods setting `m_scrollbar` before us caused `ScrollRect` and `ScrollRectEnsureVisible` to never be created. These components are now ensured independently via `EnsureScrollRect()`.
- **Patch execution order**: Added `[HarmonyPriority(Priority.Low)]` on `InventoryGui.Show` postfix and moved all critical visual state (pivot lock, scroll position, grid update, clipping) into the deferred Update handler so it runs after all other mods' postfixes complete.
- **Layout rebuild before clipping**: Added `LayoutRebuilder.ForceRebuildLayoutImmediate()` to ensure grid content is properly sized before clipping pass.

## [1.0.3] - 2026-09-19

### Documentation
- **Known Issues & Workaround**: Added notice and temporary workaround instructions for first-time open empty background issue.

### Changed
- **Opening Animation & Clipping Updates**: Added dynamic canvas layout and clipping synchronization during the opening animation in `InventoryGui.Update`.
- **Pre-warming on Spawn**: Pre-initializes inventory grid elements upon player spawn.

## [1.0.2] - 2026-09-19

### Fixed
- **First-time inventory open bug**: Fixed an issue where the inventory appeared completely blank (no item slots) when opened for the first time after loading or spawning into the world.
- **Top-aligned pivot guarantee**: Ensured the grid root pivot is locked to top (`pivot.y = 1f`), preventing Unity ScrollRect from displacing item slots off-screen.
- **Scroll synchronization**: Synchronized ScrollRect normalized position and scrollbar value directly upon opening so hotbar slots are always visible.

## [1.0.1] - 2026-09-19

### Added
- Integrated native Valheim-styled scrollbar for player inventory.
- Mouse scroll-wheel support for inventory grid.
- Gamepad/controller navigation follow support.
- ValheimPlus compatibility guidelines.

## [1.0.0] - 2026-09-19

### Added
- Initial release of **ExpandedPlayerInventory**.
- Configurable player inventory rows (min 4, max 50, default 20).
- Data-layer persistence across game sessions and world saves.
