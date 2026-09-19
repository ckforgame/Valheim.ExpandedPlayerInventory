# Changelog

All notable changes to this project will be documented in this file.

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
