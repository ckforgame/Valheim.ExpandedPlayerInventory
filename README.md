# ExpandedPlayerInventory

A standalone Valheim BepInEx mod that expands player inventory rows (up to 50 rows, default 20) with a built-in scrollbar and mouse-wheel scrolling support.

## Known Issues & Workaround

> [!NOTE]
> **Occasional Blank Grid on Initial Open**:
>
> - **Symptom**: In certain environments or heavy modded setups, opening the inventory for the very first time after joining a world, respawning, or loading into a new area may occasionally display only the wooden background panel without the item slots rendering immediately.
> - **Quick Solution**: Simply close (`Tab` or `Esc`) and reopen the inventory once. This forces the UI layout to synchronize, after which all item slots, graphics, and scrolling will render smoothly and normally for the remainder of your session.

## Features

- Configurable player inventory rows (4 to 50 rows).
- Integrated Valheim-themed scrollbar.
- Mouse scroll wheel and Gamepad navigation support.
- Fully standalone: can be used with or without ValheimPlus.

## Configuration

Configuration file is generated at `BepInEx/config/ckforgame.ExpandedPlayerInventory.cfg` after first launch:

- `playerInventoryRows`: Number of player inventory rows (min 4, max 50, default 20).
- `scrollSensitivity`: Mouse wheel scroll sensitivity (min 50, max 1500, default 350). Higher values scroll faster with less wheel movement.
- `rememberScrollPosition`: Remember the last scroll position when opening the inventory, instead of always jumping back to the top (default `true`).

## Compatibility with ValheimPlus

> [!IMPORTANT]
> If you are using **ValheimPlus**, please ensure `inventoryRows` in your ValheimPlus configuration (`valheim_plus.cfg`) is set to **`4`** (the vanilla default) or leave the `[Player]` inventory expansion disabled.
>
> Letting ValheimPlus expand inventory rows simultaneously will conflict with this mod's scrollbar positioning and grid layout management. Use **ExpandedPlayerInventory**'s configuration to specify your desired rows instead.

## Installation

- **Using Mod Manager (r2modman / Thunderstore)**: Click **Install with Mod Manager**.
- **Manual Installation**:
  1. Install [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/).
  2. Extract `ExpandedPlayerInventory.dll` into `<GameDirectory>/BepInEx/plugins/`.

## Changelog

- **v1.2.0**:
  - **Feature**: Added **Scroll Position Memory** (`rememberScrollPosition`, default `true`), keeping your scroll view intact across inventory toggles and chest looting.
  - **Feature**: Dynamic Resolution & UI Scale adaptation (automatically recalculates layouts upon settings changes without game restarts).
  - **Reliability**: Added item safety defense-in-depth net in `Humanoid.DropInvalidItems` and `Player.SetInventorySize` to strictly prevent item dropping or loss.
  - **Reliability**: Enhanced scrollbar template search with programmatic UI fallback.
  - **Reliability**: Thorough session state reset on logout and world join.
- **v1.1.0**:
  - **Feature**: Added configurable mouse wheel `scrollSensitivity` (default `350`, range `50`–`1500`) for ~5x faster, effortless scrolling.
  - **Feature**: Standardized plugin GUID and configuration filename to `ckforgame.ExpandedPlayerInventory.cfg` (with automatic migration from legacy config).
  - **Fix**: Resolved ValheimPlus mod conflict where container resizing caused negative viewport heights and culled item slots on first open.
  - **Fix**: Fixed repeated 20-row pop-in / flashing by restricting opening synchronization strictly to the first open of each world session.
  - **Fix**: Added runtime layout failsafes in `InventoryGui.Update` preventing item graphics from ever being culled while the canvas settles.
- **v1.0.4**: Deferred clipping to after Animator transition; fixed compatibility with ValheimPlus and other inventory mods.
- **v1.0.3**: Added known issues and temporary workaround documentation; improved canvas and clipping update handling during inventory opening animation.
- **v1.0.2**: Fixed first-time inventory open render bug where slots were displaced off-screen and invisible until reopened.
- **v1.0.1**: Initial release with standalone scrolling inventory and ValheimPlus compatibility guidance.

## Credits & Special Thanks

- **[ValheimPlus](https://github.com/Grantapher/ValheimPlus)**: Special thanks to the ValheimPlus project and its contributors for the inspiration and foundational concept of inventory scrolling and grid expansion mechanics.

## License

This project is licensed under the GNU General Public License v3.0 - see the [LICENSE](LICENSE) file for details.
