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

    [Menu("Return cursor", "Move the cursor back to where it was after the click")]
    public ToggleNode ReturnCursor { get; set; } = new ToggleNode(true);

    [Menu("Highlight button", "Draw a frame around the button the plugin would click")]
    public ToggleNode Highlight { get; set; } = new ToggleNode(true);

    [Menu("Debug", "Draw every element of the essence label with its index path")]
    public ToggleNode Debug { get; set; } = new ToggleNode(false);
}
