using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Elements;
using ExileCore.Shared.Enums;
using SharpDX;
using Vector2 = System.Numerics.Vector2;

namespace EssenceCorrupt;

public partial class EssenceCorrupt : BaseSettingsPlugin<EssenceCorruptSettings>
{
    private const int MaxDepth = 6;

    private readonly System.Diagnostics.Stopwatch _sinceClick = System.Diagnostics.Stopwatch.StartNew();

    private Vector2 WindowOffset
    {
        get
        {
            var tl = GameController.Window.GetWindowRectangleTimeCache.TopLeft;
            return new Vector2(tl.X, tl.Y);
        }
    }

    public override bool Initialise() => true;

    public override void Render()
    {
        if (!GameController.InGame || GameController.Player == null)
        {
            StopInventoryClicking();
            return;
        }

        RenderInventory();
        RenderNoCorruptAlerts();
        RenderEssence();
    }

    private readonly HashSet<long> _alerted = new HashSet<long>();

    // Red frame around every visible label that has an essence from the
    // alert list, and a sound the first time each one is seen.
    private void RenderNoCorruptAlerts()
    {
        var alertList = SplitList(Settings.AlertEssences.Value);
        if (alertList.Count == 0) return;

        foreach (var l in AllLabels())
        {
            var e = l.ItemOnGround;
            if (e?.Path == null || !e.Path.Contains("Monolith", StringComparison.OrdinalIgnoreCase)) continue;
            if (e.Path.Contains("Legion", StringComparison.OrdinalIgnoreCase)) continue;
            var root = l.Label;
            if (root == null || !root.IsVisible) continue;
            if (FindListedEssence(root, alertList) == null) continue;

            if (Settings.RedFrame)
                Graphics.DrawFrame(root.GetClientRect(), Color.Red, 3);

            if (Settings.AlertSound && _alerted.Add(e.Id))
            {
                try { GameController.SoundController.PlaySound(Settings.AlertSoundName.Value); }
                catch (Exception ex) { LogError($"EssenceCorrupt: cannot play sound '{Settings.AlertSoundName.Value}': {ex.Message}", 5); }
            }
        }
    }

    private void RenderEssence()
    {
        // A first press always clicks; with Hold to repeat on, holding the key
        // clicks again every RepeatInterval ms. Diagnostics only on a real press.
        var firstPress = Settings.Hotkey.PressedOnce();
        var repeat = !firstPress
                     && Settings.HoldToRepeat
                     && Settings.Hotkey.IsPressed()
                     && _sinceClick.ElapsedMilliseconds >= Settings.RepeatInterval.Value;
        var pressed = firstPress || repeat;

        var label = FindNearestEssenceLabel();
        if (label == null)
        {
            if (firstPress) WriteDiagnostics("no essence label found");
            return;
        }

        var root = label.Label;
        if (root == null || !root.IsVisible) return;

        if (Settings.Debug) DrawDebug(root);

        // An essence from the "Do not corrupt" list: open the monolith instead.
        var skip = FindNoCorruptEssence(root);
        if (skip != null)
        {
            var openRect = OpenTarget(root);
            if (Settings.Highlight)
            {
                var rr = root.GetClientRect();
                Graphics.DrawFrame(openRect, Color.Red, 2);
                Graphics.DrawText($"open: {skip}", new Vector2(rr.Center.X, rr.Bottom + 4), Color.Red, FontAlign.Center);
            }

            if (!pressed) return;
            Click(openRect);
            _sinceClick.Restart();
            return;
        }

        var button = FindVaalButton(root);

        if (button != null && Settings.Highlight)
            Graphics.DrawFrame(button.GetClientRect(), Color.Lime, 2);

        if (!pressed) return;

        if (button == null)
        {
            if (firstPress) WriteDiagnostics("label found, Vaal Orb button not found", root);
            return;
        }

        Click(button.GetClientRect());
        _sinceClick.Restart();
    }

    private static List<string> SplitList(string value) =>
        (value ?? "")
        .Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(s => s.Trim())
        .Where(s => s.Length > 0)
        .ToList();

    private string FindNoCorruptEssence(Element root) =>
        FindListedEssence(root, SplitList(Settings.NoCorruptEssences.Value));

    // Returns the first list entry found in the label's text, or null.
    private static string FindListedEssence(Element root, List<string> list)
    {
        if (list.Count == 0) return null;

        var texts = new List<string>();
        CollectTexts(root, 0, texts);
        return list.FirstOrDefault(entry =>
            texts.Any(t => t.Contains(entry, StringComparison.OrdinalIgnoreCase)));
    }

    private static void CollectTexts(Element e, int depth, List<string> into)
    {
        if (depth > MaxDepth || e == null) return;
        foreach (var c in e.Children)
        {
            if (c == null || c.Address == 0 || !c.IsVisible) continue;
            var t = c.Text;
            if (!string.IsNullOrEmpty(t)) into.Add(t);
            CollectTexts(c, depth + 1, into);
        }
    }

    // Clicking the label opens the monolith. Aim at its bottom line ("The monster
    // is imprisoned..."): the title is often above the top of the screen. The
    // point is kept inside the game window.
    private RectangleF OpenTarget(Element root)
    {
        var r = root.GetClientRect();
        var win = GameController.Window.GetWindowRectangleTimeCache;
        const float margin = 12;

        var y = r.Bottom - Math.Min(18f, r.Height * 0.1f);
        y = Math.Clamp(y, margin, win.Height - margin);
        var x = Math.Clamp(r.Center.X, margin, win.Width - margin);

        return new RectangleF(x - 10, y - 6, 20, 12);
    }

    private LabelOnGround FindNearestEssenceLabel()
    {
        var playerPos = GameController.Player.PosNum;
        LabelOnGround best = null;
        var bestDist = float.MaxValue;

        foreach (var l in AllLabels())
        {
            var e = l?.ItemOnGround;
            if (e == null || e.Path == null) continue;
            if (!e.Path.Contains("Monolith", StringComparison.OrdinalIgnoreCase)) continue;
            if (e.Path.Contains("Legion", StringComparison.OrdinalIgnoreCase)) continue;
            if (l.Label == null || !l.Label.IsVisible) continue;

            var d = (e.PosNum - playerPos).Length();
            if (d > Settings.SearchDistance.Value || d >= bestDist) continue;
            bestDist = d;
            best = l;
        }

        return best;
    }

    private IEnumerable<LabelOnGround> AllLabels()
    {
        var ui = GameController.IngameState.IngameUi;
        var seen = new HashSet<long>();
        IEnumerable<LabelOnGround> visible = ui.ItemsOnGroundLabelsVisible ?? (IEnumerable<LabelOnGround>)Array.Empty<LabelOnGround>();
        IEnumerable<LabelOnGround> all = ui.ItemsOnGroundLabels ?? (IEnumerable<LabelOnGround>)Array.Empty<LabelOnGround>();
        foreach (var l in visible.Concat(all))
        {
            if (l == null || l.Address == 0 || !seen.Add(l.Address)) continue;
            yield return l;
        }
    }

    private void WriteDiagnostics(string reason, Element root = null)
    {
        try
        {
            var playerPos = GameController.Player.PosNum;
            var lines = new List<string> { $"{DateTime.Now:HH:mm:ss} {reason}" };

            var labels = AllLabels()
                .Where(l => l.ItemOnGround != null)
                .Select(l => (l, d: (l.ItemOnGround.PosNum - playerPos).Length()))
                .OrderBy(x => x.d)
                .Take(25);
            foreach (var (l, d) in labels)
            {
                var r = l.Label?.GetClientRect() ?? default;
                lines.Add($"  label d={d:F0} vis={l.Label?.IsVisible} rect={r} path={l.ItemOnGround.Path}");
            }

            var monoliths = GameController.EntityListWrapper.OnlyValidEntities
                .Where(e => e.Path != null && e.Path.Contains("Monolith", StringComparison.OrdinalIgnoreCase))
                .Select(e => $"  entity d={(e.PosNum - playerPos).Length():F0} path={e.Path}");
            lines.Add(" monolith entities:");
            lines.AddRange(monoliths);

            if (root != null)
            {
                lines.Add(" label tree:");
                DumpTree(root, "", 0, lines);
            }

            lines.Add("");
            System.IO.File.AppendAllLines(System.IO.Path.Join(DirectoryFullName, "diag.txt"), lines);
            LogMessage($"EssenceCorrupt: {reason} (written to diag.txt)", 3);
        }
        catch (Exception ex)
        {
            LogError($"EssenceCorrupt diagnostics failed: {ex}", 5);
        }
    }

    private static void DumpTree(Element e, string path, int depth, List<string> into)
    {
        if (depth > MaxDepth) return;
        var i = 0;
        foreach (var c in e.Children)
        {
            var p = path + "/" + i++;
            if (c == null || c.Address == 0) continue;
            into.Add($"  {p} vis={c.IsVisible} rect={c.GetClientRect()} text={c.Text}");
            DumpTree(c, p, depth + 1, into);
        }
    }

    // The Vaal Orb button hangs under the label: a visible, roughly square
    // element without text. Essence icons are square too but smaller and higher,
    // so take the lowest square one, preferring a tooltip that mentions Vaal.
    private Element FindVaalButton(Element root)
    {
        var rootRect = root.GetClientRect();
        var candidates = new List<Element>();
        Collect(root, 0, candidates);

        var squares = candidates.Where(e =>
        {
            var r = e.GetClientRect();
            if (r.Width < 16 || r.Height < 16) return false;
            if (r.Width > rootRect.Width * 0.5f) return false;
            var ratio = r.Width / r.Height;
            if (ratio < 0.7f || ratio > 1.4f) return false;
            return string.IsNullOrEmpty(e.Text);
        }).ToList();

        var byTooltip = squares.FirstOrDefault(TooltipMentionsVaal);
        if (byTooltip != null) return byTooltip;

        return squares
            .Where(e => e.GetClientRect().Center.Y > rootRect.Center.Y)
            .OrderByDescending(e => e.GetClientRect().Bottom)
            .FirstOrDefault();
    }

    private static bool TooltipMentionsVaal(Element e)
    {
        try
        {
            var tip = e.Tooltip;
            if (tip == null || tip.Address == 0) return false;
            return tip.FindChildRecursive("Vaal", contains: true, maxDepth: 6) != null;
        }
        catch
        {
            return false;
        }
    }

    private static void Collect(Element e, int depth, List<Element> into)
    {
        if (depth > MaxDepth || e == null) return;
        foreach (var c in e.Children)
        {
            if (c == null || c.Address == 0 || !c.IsVisible) continue;
            into.Add(c);
            Collect(c, depth + 1, into);
        }
    }

    private void Click(RectangleF rect)
    {
        var target = new Vector2(rect.Center.X, rect.Center.Y) + WindowOffset;
        var prev = Input.ForceMousePositionNum;
        var delay = Settings.ClickDelay.Value;

        Input.LeftUp();
        Input.SetCursorPos(target);
        Thread.Sleep(delay);
        Input.MouseMove();
        Thread.Sleep(delay);
        Input.LeftDown();
        Thread.Sleep(10);
        Input.LeftUp();
        Thread.Sleep(delay);

        if (Settings.ReturnCursor)
            Input.SetCursorPos(prev);
    }

    private void DrawDebug(Element root)
    {
        DrawDebugRec(root, "", 0);
    }

    private void DrawDebugRec(Element e, string path, int depth)
    {
        if (depth > MaxDepth) return;
        var i = 0;
        foreach (var c in e.Children)
        {
            var p = path + "/" + i++;
            if (c == null || c.Address == 0 || !c.IsVisible) continue;
            var r = c.GetClientRect();
            Graphics.DrawFrame(r, Color.Yellow, 1);
            Graphics.DrawText($"{p} {(int)r.Width}x{(int)r.Height}", new Vector2(r.X, r.Y), Color.Yellow, FontAlign.Left);
            DrawDebugRec(c, p, depth + 1);
        }
    }
}
