# Changelog

All notable changes to this project will be documented in this file.

## [1.0.5] - 2026-09-20

### Added
- **Configurable Mouse Wheel Scroll Sensitivity**: Added `scrollSensitivity` configuration option (default `350`, range `50`–`1500`). Scrolling is now 5x faster by default, allowing smooth navigation across inventory rows with minimal finger movement. Configurable via `com.custom.expandedplayerinventory.cfg` or in-game Configuration Manager.

### Fixed
- **ValheimPlus Mod Conflict & Negative Bounds**: Resolved conflict where ValheimPlus's `playerInventoryRows` set the background frame to 20 rows (990.5px), causing stretch-anchored grids to calculate negative viewport heights (`-566.0px`) and cull all item slots:
  - Added `InventoryGui_SetInventorySize_Patch` to clamp the UI container height to visible rows (maximum 6 rows), preventing other mods from blowing up the frame.
  - Decoupled `gridRect` from parent vertical stretch anchors to fixed top anchors (`anchorMin.y = 1f, anchorMax.y = 1f, pivot.y = 1f`), guaranteeing positive height bounds (`+424.5px`) at all times.
  - Aligned scrollbar anchors identically to ensure pixel-perfect positioning.
- **World-Session Opening Optimization (No Flashing)**: Opening synchronization and delayed clipping now only run once on the very first open of each world session. All subsequent inventory opens in the same world keep the mask active and open cleanly, smoothly, and instantly with the scrollbar and 6 rows already rendered. Session state automatically resets on `Game.Logout`, `Game.Start`, and `InventoryGui.Awake`.
- **First-Time Open Blank Background Bug**: Permanently resolved the first-time open issue by removing premature spawn triggers, temporarily disabling `RectMask2D` during the opening animation on first session open, and adding continuous runtime failsafes so item graphics can never be culled.

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
