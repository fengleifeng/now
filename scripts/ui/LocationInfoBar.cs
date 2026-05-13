using Godot;
using CardSurvival.Data;

namespace CardSurvival.UI;

public partial class LocationInfoBar : PanelContainer
{
    [Signal] public delegate void OnExploreClickedEventHandler();
    [Signal] public delegate void OnMoveToLocationEventHandler(string locationId);

    private Label _title = null!;
    private Label _description = null!;
    private HBoxContainer _actions = null!;
    private MapSystem _map = null!;
    private TimeSystem _time = null!;

    public override void _Ready()
    {
        _map = GetNode<MapSystem>("/root/MapSystem");
        _time = GetNode<TimeSystem>("/root/TimeSystem");

        GameTheme.ApplyPanel(this, GameTheme.PanelMain);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        margin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        AddChild(margin);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 5);
        box.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        margin.AddChild(box);

        var titleRow = new HBoxContainer();
        titleRow.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        box.AddChild(titleRow);

        _title = new Label();
        _title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _title.AddThemeFontSizeOverride("font_size", 16);
        _title.AddThemeColorOverride("font_color", GameTheme.AccentRegion);
        titleRow.AddChild(_title);

        _description = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _description.AddThemeFontSizeOverride("font_size", 12);
        _description.AddThemeColorOverride("font_color", GameTheme.TextMuted);
        _description.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        box.AddChild(_description);

        _actions = new HBoxContainer();
        _actions.AddThemeConstantOverride("separation", 8);
        _actions.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        box.AddChild(_actions);
    }

    public void Refresh(LocationData location)
    {
        _title.Text = $"{location.Icon} {location.Name}";
        _description.Text = location.Description;

        foreach (var child in _actions.GetChildren().ToArray())
        {
            _actions.RemoveChild(child);
            child.QueueFree();
        }

        var exploreCost = _time.CurrentWeather == WeatherType.Foggy ? 15 : 10;
        var explore = new Button { Text = $"探索 -{exploreCost}精力", CustomMinimumSize = new Vector2(128, 32) };
        GameTheme.StyleExploreButton(explore);
        explore.Pressed += () => EmitSignal(SignalName.OnExploreClicked);
        _actions.AddChild(explore);

        foreach (var connection in location.Connections)
        {
            var id = connection;
            var name = _map.GetLocation(id)?.Name ?? id;
            var move = new Button { Text = $"前往 {name}", CustomMinimumSize = new Vector2(118, 32) };
            GameTheme.StyleMoveButton(move);
            move.Pressed += () => EmitSignal(SignalName.OnMoveToLocation, id);
            _actions.AddChild(move);
        }
    }
}
