using Godot;
using CardSurvival.Data;

namespace CardSurvival.UI;

public partial class MapPopup : Control
{
    public event Action<string>? OnMoveToLocation;
    public event Action? OnClose;

    private VBoxContainer _list = null!;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        BuildUI();
    }

    private void BuildUI()
    {
        AddChild(CreateOverlay());

        var panel = CreateCenteredPanel(new Vector2(500, 430));
        AddChild(panel);

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        panel.AddChild(margin);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 8);
        margin.AddChild(box);

        var titleRow = new HBoxContainer();
        box.AddChild(titleRow);

        var title = new Label { Text = "荒野地图" };
        title.AddThemeFontSizeOverride("font_size", 16);
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        titleRow.AddChild(title);

        var close = new Button { Text = "关闭", CustomMinimumSize = new Vector2(70, 28) };
        close.Pressed += () => OnClose?.Invoke();
        titleRow.AddChild(close);

        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto
        };
        box.AddChild(scroll);

        _list = new VBoxContainer();
        _list.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(_list);
    }

    private ColorRect CreateOverlay()
    {
        var overlay = new ColorRect { Color = new Color(0, 0, 0, 0.62f) };
        overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        overlay.MouseFilter = MouseFilterEnum.Stop;
        overlay.GuiInput += e =>
        {
            if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
                OnClose?.Invoke();
        };
        return overlay;
    }

    private static Panel CreateCenteredPanel(Vector2 size)
    {
        var panel = new Panel();
        panel.CustomMinimumSize = size;
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        return panel;
    }

    public void Refresh(string currentLocationId, List<LocationData> allLocations)
    {
        var current = allLocations.FirstOrDefault(l => l.Id == currentLocationId);
        var adjacent = current?.Connections ?? new List<string>();

        foreach (var child in _list.GetChildren())
            child.QueueFree();

        foreach (var loc in allLocations)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);
            _list.AddChild(row);

            var info = new Label();
            info.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            info.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            var suffix = loc.Id == currentLocationId ? "（当前）" : adjacent.Contains(loc.Id) ? "（可达）" : "（未连接）";
            info.Text = $"{loc.Icon} {loc.Name}{suffix}\n{loc.Description}";
            row.AddChild(info);

            if (loc.Id != currentLocationId && adjacent.Contains(loc.Id))
            {
                var id = loc.Id;
                var move = new Button { Text = "前往", CustomMinimumSize = new Vector2(70, 30) };
                move.Pressed += () => OnMoveToLocation?.Invoke(id);
                row.AddChild(move);
            }
            else
            {
                row.AddChild(new Control { CustomMinimumSize = new Vector2(70, 0) });
            }
        }
    }
}
