# Changelog

All notable changes to this project will be documented in this file.

## [1.0.5] - 2026-09-20

### Fixed
- **First-time inventory open bug (complete resolution)**: Permanently resolved the issue where item slots occasionally rendered as an empty wooden panel on first open or respawn:
  - Removed premature UI setup triggers on player spawn/respawn (`Player.OnSpawned`) that were causing background clipping timeouts.
  - Temporarily disabled `RectMask2D` during the Animator's zero-scale opening transition, guaranteeing that item slot graphics can never be culled as invisible.
  - Synchronized `ScrollRect` bounds, top-aligned pivot, and `RectMask2D.PerformClipping()` once the panel achieves its usable scale during `InventoryGui.Update`.

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
