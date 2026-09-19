# ExpandedPlayerInventory

A standalone Valheim BepInEx mod that expands player inventory rows (up to 50 rows, default 20) with a built-in scrollbar and mouse-wheel scrolling support.

## Features

- Configurable player inventory rows (4 to 50 rows).
- Integrated Valheim-themed scrollbar.
- Mouse scroll wheel and Gamepad navigation support.
- Fully standalone: can be used with or without ValheimPlus.

## Configuration

Configuration file is generated at `BepInEx/config/com.custom.expandedplayerinventory.cfg` after first launch:

- `playerInventoryRows`: Number of player inventory rows (min 4, max 50, default 20).

## Compatibility with ValheimPlus

> [!IMPORTANT]
> If you are using **ValheimPlus**, please ensure `inventoryRows` in your ValheimPlus configuration (`valheim_plus.cfg`) is set to **`4`** (the vanilla default) or leave the `[Player]` inventory expansion disabled.
>
> Letting ValheimPlus expand inventory rows simultaneously will conflict with this mod's scrollbar positioning and grid layout management. Use **ExpandedPlayerInventory**'s configuration to specify your desired rows instead.

## Known Issues & Workaround

> [!NOTE]
> **First-Time Inventory Open (Empty Background)**:
> - **Issue**: When opening the inventory for the very first time after logging into a world or respawning, the item slots may occasionally not render, showing only the wooden background panel.
> - **Temporary Workaround**: Simply close (`Tab` or `Esc`) and reopen the inventory once. All slots and items will render and scroll normally for the remainder of your session.


## Installation

- **Using Mod Manager (r2modman / Thunderstore)**: Click **Install with Mod Manager**.
- **Manual Installation**:
  1. Install [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/).
  2. Extract `ExpandedPlayerInventory.dll` into `<GameDirectory>/BepInEx/plugins/`.

## Changelog

- **v1.0.3**: Added known issues and temporary workaround documentation; improved canvas and clipping update handling during inventory opening animation.
- **v1.0.2**: Fixed first-time inventory open render bug where slots were displaced off-screen and invisible until reopened.
- **v1.0.1**: Initial release with standalone scrolling inventory and ValheimPlus compatibility guidance.

## Credits & Special Thanks

- **[ValheimPlus](https://github.com/Grantapher/ValheimPlus)**: Special thanks to the ValheimPlus project and its contributors for the inspiration and foundational concept of inventory scrolling and grid expansion mechanics.

## License

This project is licensed under the GNU General Public License v3.0 - see the [LICENSE](LICENSE) file for details.
