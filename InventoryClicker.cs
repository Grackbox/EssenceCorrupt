using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using ExileCore;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using Hexa.NET.ImGui;
using SharpDX;
using Vector2 = System.Numerics.Vector2;

namespace EssenceCorrupt;

// Second feature: Ctrl+click every item in the player inventory, skipping banned cells.
public partial class EssenceCorrupt
{
    private const int InvColumns = 12;
    private const int InvRows = 5;

    private readonly Queue<Vector2> _invQueue = new Queue<Vector2>();
    private readonly Stopwatch _sinceInvClick = Stopwatch.StartNew();
    private bool _invRunning;
    private Vector2 _invCursorBefore;

    private Inventory PlayerInventory
    {
        get
        {
            var panel = GameController.IngameState.IngameUi.InventoryPanel;
            if (panel == null || !panel.IsVisible) return null;
            return panel[InventoryIndex.PlayerInventory];
        }
    }

    private void RenderInventory()
    {
        var inv = PlayerInventory;
        if (inv == null || !inv.IsVisible)
        {
            StopInventoryClicking();
            return;
        }

        var invRect = inv.GetClientRect();

        if (Settings.ShowBannedCells)
            foreach (var cell in Settings.BannedCells)
                Graphics.DrawFrame(CellRect(invRect, cell % InvColumns, cell / InvColumns), Color.Red, 2);

        if (Settings.BanCellHotkey.PressedOnce())
            ToggleBanUnderCursor(invRect);

        if (Settings.InventoryHotkey.PressedOnce())
        {
            if (_invRunning) StopInventoryClicking();
            else StartInventoryClicking(inv);
        }

        if (!_invRunning || _sinceInvClick.ElapsedMilliseconds < Settings.InventoryClickDelay.Value) return;

        if (_invQueue.Count == 0)
        {
            StopInventoryClicking();
            return;
        }

        ClickAt(_invQueue.Dequeue() + WindowOffset);
        _sinceInvClick.Restart();
    }

    private void StartInventoryClicking(Inventory inv)
    {
        var banned = Settings.BannedCells.ToHashSet();
        var invRect = inv.GetClientRect();
        var cellW = invRect.Width / InvColumns;
        var cellH = invRect.Height / InvRows;

        // Cells are worked out from the item's place on screen: InventPosX/Y are
        // obsolete in this ExileCore and return 0 for several item types.
        var items = inv.VisibleInventoryItems
            .Where(i => i != null && i.Address != 0)
            .Select(i =>
            {
                var r = i.GetClientRect();
                var x = (int)System.Math.Round((r.X - invRect.X) / cellW);
                var y = (int)System.Math.Round((r.Y - invRect.Y) / cellH);
                var w = System.Math.Max(1, (int)System.Math.Round(r.Width / cellW));
                var h = System.Math.Max(1, (int)System.Math.Round(r.Height / cellH));
                return (r, x, y, w, h);
            })
            .Where(t => !OccupiesBannedCell(t.x, t.y, t.w, t.h, banned))
            .OrderBy(t => t.x)
            .ThenBy(t => t.y)
            .ToList();

        _invQueue.Clear();
        foreach (var t in items)
            _invQueue.Enqueue(new Vector2(t.r.Center.X, t.r.Center.Y));

        if (_invQueue.Count == 0) return;

        _invCursorBefore = Input.ForceMousePositionNum;
        Input.LeftUp();
        Input.KeyDown(Keys.LControlKey);
        _invRunning = true;
        _sinceInvClick.Restart();
    }

    private void StopInventoryClicking()
    {
        if (!_invRunning) return;
        _invRunning = false;
        _invQueue.Clear();
        Input.KeyUp(Keys.LControlKey);
        if (Settings.ReturnCursor)
            Input.SetCursorPos(_invCursorBefore);
    }

    private static bool OccupiesBannedCell(int x, int y, int w, int h, HashSet<int> banned)
    {
        for (var dx = 0; dx < w; dx++)
        for (var dy = 0; dy < h; dy++)
            if (banned.Contains((y + dy) * InvColumns + x + dx))
                return true;
        return false;
    }

    private void ClickAt(Vector2 screenPos)
    {
        Input.SetCursorPos(screenPos);
        Thread.Sleep(5);
        Input.MouseMove();
        Thread.Sleep(5);
        Input.LeftDown();
        Thread.Sleep(10);
        Input.LeftUp();
    }

    private static RectangleF CellRect(RectangleF invRect, int x, int y)
    {
        var w = invRect.Width / InvColumns;
        var h = invRect.Height / InvRows;
        return new RectangleF(invRect.X + x * w, invRect.Y + y * h, w, h);
    }

    private void ToggleBanUnderCursor(RectangleF invRect)
    {
        var mouse = Input.ForceMousePositionNum - WindowOffset;
        if (!invRect.Contains(mouse.X, mouse.Y)) return;

        var x = (int)((mouse.X - invRect.X) / (invRect.Width / InvColumns));
        var y = (int)((mouse.Y - invRect.Y) / (invRect.Height / InvRows));
        if (x < 0 || x >= InvColumns || y < 0 || y >= InvRows) return;

        ToggleBan(y * InvColumns + x);
    }

    private void ToggleBan(int cell)
    {
        if (!Settings.BannedCells.Remove(cell))
            Settings.BannedCells.Add(cell);
    }

    public override void DrawSettings()
    {
        base.DrawSettings();

        ImGui.Separator();
        ImGui.Text("Inventory: banned cells (checked = never clicked)");
        for (var y = 0; y < InvRows; y++)
        {
            for (var x = 0; x < InvColumns; x++)
            {
                var cell = y * InvColumns + x;
                var banned = Settings.BannedCells.Contains(cell);
                if (x > 0) ImGui.SameLine();
                if (ImGui.Checkbox($"##inv{cell}", ref banned))
                    ToggleBan(cell);
            }
        }

        if (ImGui.Button("Clear bans"))
            Settings.BannedCells.Clear();
    }

    public override void AreaChange(AreaInstance area)
    {
        StopInventoryClicking();
        _alerted.Clear();
    }
}
