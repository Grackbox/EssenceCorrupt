using System.Collections.Generic;
using System.Windows.Forms;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;

namespace EssenceCorrupt;

public class EssenceCorruptSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new ToggleNode(false);

    [Menu("Hotkey", "Press to click the Vaal Orb button on the nearest essence label")]
    public HotkeyNodeV2 Hotkey { get; set; } = new HotkeyNodeV2(Keys.F6);

    [Menu("Max distance", "Only monoliths this close to the player (game units)")]
    public RangeNode<int> SearchDistance { get; set; } = new RangeNode<int>(600, 50, 2000);

    [Menu("Click delay (ms)")]
    public RangeNode<int> ClickDelay { get; set; } = new RangeNode<int>(30, 5, 200);

    [Menu("Hold to repeat", "While the hotkey is held, keep clicking like PickIt does")]
    public ToggleNode HoldToRepeat { get; set; } = new ToggleNode(false);

    [Menu("Repeat interval (ms)", "Time between clicks while the hotkey is held")]
    public RangeNode<int> RepeatInterval { get; set; } = new RangeNode<int>(250, 50, 1000);

    [Menu("Return cursor", "Move the cursor back to where it was after the click")]
    public ToggleNode ReturnCursor { get; set; } = new ToggleNode(true);

    [Menu("Highlight button", "Draw a frame around the button the plugin would click")]
    public ToggleNode Highlight { get; set; } = new ToggleNode(true);

    [Menu("Debug", "Draw every element of the essence label with its index path")]
    public ToggleNode Debug { get; set; } = new ToggleNode(false);

    [Menu("Inventory: Ctrl+click hotkey", "Ctrl+clicks every item in the inventory that is not on a banned cell. Press again to stop")]
    public HotkeyNodeV2 InventoryHotkey { get; set; } = new HotkeyNodeV2(Keys.None);

    [Menu("Inventory: ban cell hotkey", "With the inventory open, bans or unbans the cell under the cursor")]
    public HotkeyNodeV2 BanCellHotkey { get; set; } = new HotkeyNodeV2(Keys.None);

    [Menu("Inventory: delay between items (ms)")]
    public RangeNode<int> InventoryClickDelay { get; set; } = new RangeNode<int>(40, 10, 300);

    [Menu("Inventory: show banned cells", "Red frames on banned cells while the inventory is open")]
    public ToggleNode ShowBannedCells { get; set; } = new ToggleNode(true);

    // Cell index = y * 12 + x. Edited from the grid in the settings or with the ban hotkey.
    [IgnoreMenu]
    public List<int> BannedCells { get; set; } = new List<int>();
}
