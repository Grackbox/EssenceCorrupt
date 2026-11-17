# EssenceCorrupt

ExileCore / PoEHelper plugin: press a hotkey to click the Vaal Orb button on the nearest essence monolith label.

## Install

PluginUpdater → **Add&Browse** → paste `https://github.com/Grackbox/EssenceCorrupt` → **Clone**, then restart the HUD and enable **EssenceCorrupt**.

## Settings

- **Hotkey** — clicks the Vaal Orb button of the nearest essence label.
- **Search distance** — how far from the player a monolith may be.
- **Click delay (ms)** — raise it if the click lands before the cursor arrives.
- **Hold to repeat** — while the hotkey is held, keep clicking (like PickIt).
- **Repeat interval (ms)** — time between clicks while held.
- **Return cursor** — move the cursor back after the click.
- **Highlight button** — green frame around the button the plugin would click.
- **Debug** — draws every element of the label; on a failed hotkey press writes `diag.txt` next to the plugin.

## Inventory Ctrl+click

- **Inventory: Ctrl+click hotkey** — with the inventory open, Ctrl+clicks every item that is not on a banned cell; press again to stop.
- **Inventory: ban cell hotkey** — bans or unbans the inventory cell under the cursor.
- **Inventory: delay between items (ms)**.
- **Inventory: show banned cells** — red frames on banned cells.
- A 12×5 grid at the bottom of the settings: checked cells are never clicked.
