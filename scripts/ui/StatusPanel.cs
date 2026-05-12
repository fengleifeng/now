using Godot;
using CardSurvival.Data;

namespace CardSurvival.UI;

public partial class StatusPanel : VBoxContainer
{
    [Signal] public delegate void OnCraftClickedEventHandler();
    [Signal] public delegate void OnRestClickedEventHandler();
    [Signal] public delegate void OnMenuClickedEventHandler();

    private ProgressBar _hpBar = null!;
    private ProgressBar _hungerBar = null!;
    private ProgressBar _thirstBar = null!;
    private ProgressBar _energyBar = null!;
    private ProgressBar _sanityBar = null!;
    private Label _hpValue = null!;
    private Label _hungerValue = null!;
    private Label _thirstValue = null!;
    private Label _energyValue = null!;
    private Label _sanityValue = null!;
    private Label _tempLabel = null!;
    private Label _weatherLabel = null!;
    private Label _timeLabel = null!;
    private Label _dayLabel = null!;
    private Label _seasonLabel = null!;
    private Label _locationLabel = null!;
    private Label _effectLabel = null!;
    private PlayerSystem _player = null!;
    private TimeSystem _time = null!;

    public override void _Ready()
    {
        _player = GetNode<PlayerSystem>("/root/PlayerSystem");
        _time = GetNode<TimeSystem>("/root/TimeSystem");

        CustomMinimumSize = new Vector2(180, 0);
        SizeFlagsVertical = SizeFlags.ExpandFill;

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        margin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        margin.SizeFlagsVertical = SizeFlags.ExpandFill;
        AddChild(margin);

        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 8);
        content.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        content.SizeFlagsVertical = SizeFlags.ExpandFill;
        margin.AddChild(content);

        content.AddChild(MakeButton("合成", () => EmitSignal(SignalName.OnCraftClicked), 34));
        content.AddChild(MakeButton("休息", () => EmitSignal(SignalName.OnRestClicked), 34));
        content.AddChild(MakeSeparator());
        AddStat(content, "生命", out _hpBar, out _hpValue);
        AddStat(content, "饱食", out _hungerBar, out _hungerValue);
        AddStat(content, "口渴", out _thirstBar, out _thirstValue);
        AddStat(content, "精力", out _energyBar, out _energyValue);
        AddStat(content, "精神", out _sanityBar, out _sanityValue);
        content.AddChild(MakeSeparator());

        _tempLabel = MakeInfo(content);
        _weatherLabel = MakeInfo(content);
        _timeLabel = MakeInfo(content);
        _dayLabel = MakeInfo(content);
        _seasonLabel = MakeInfo(content);
        _locationLabel = MakeInfo(content);

        content.AddChild(MakeSeparator());

        // 效果/状态显示
        _effectLabel = new Label();
        _effectLabel.AddThemeFontSizeOverride("font_size", 10);
        _effectLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.6f, 0.6f));
        _effectLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _effectLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        content.AddChild(_effectLabel);

        var spacer = new Control();
        spacer.SizeFlagsVertical = SizeFlags.ExpandFill;
        content.AddChild(spacer);

        content.AddChild(MakeSeparator());
        content.AddChild(MakeButton("菜单", () => EmitSignal(SignalName.OnMenuClicked), 30));

        _player.OnStatsChanged += UpdateStats;
        _time.OnTimeChanged += (_, _, _) => UpdateStats();
        _time.OnWeatherChanged += _ => UpdateStats();
        _time.OnSeasonChanged += _ => UpdateStats();
        UpdateStats();
    }

    private static Button MakeButton(string text, Action onPressed, int height)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(0, height) };
        button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        button.Pressed += onPressed;
        return button;
    }

    private static HSeparator MakeSeparator()
    {
        var sep = new HSeparator();
        sep.AddThemeColorOverride("color", new Color(0.3f, 0.28f, 0.25f));
        return sep;
    }

    private static void AddStat(VBoxContainer parent, string title, out ProgressBar bar, out Label value)
    {
        var row = new HBoxContainer();
        row.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        parent.AddChild(row);

        var label = new Label { Text = title };
        label.AddThemeFontSizeOverride("font_size", 12);
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(label);

        value = new Label();
        value.HorizontalAlignment = HorizontalAlignment.Right;
        value.AddThemeFontSizeOverride("font_size", 11);
        value.AddThemeColorOverride("font_color", new Color(0.72f, 0.72f, 0.72f));
        row.AddChild(value);

        bar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            Value = 100,
            CustomMinimumSize = new Vector2(0, 13),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            ShowPercentage = false
        };
        parent.AddChild(bar);
    }

    private static Label MakeInfo(VBoxContainer parent)
    {
        var label = new Label();
        label.AddThemeFontSizeOverride("font_size", 12);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        parent.AddChild(label);
        return label;
    }

    private void UpdateStats()
    {
        SetBar(_hpBar, _hpValue, _player.State.Health, _player.State.MaxHealth);
        SetBar(_hungerBar, _hungerValue, _player.State.Hunger, _player.State.MaxHunger);
        SetBar(_thirstBar, _thirstValue, _player.State.Thirst, _player.State.MaxThirst);
        SetBar(_energyBar, _energyValue, _player.State.Energy, _player.State.MaxEnergy);
        SetBar(_sanityBar, _sanityValue, _player.State.Sanity, _player.State.MaxSanity);
        _tempLabel.Text = $"温度 {_player.State.Temperature}C";
        _weatherLabel.Text = $"天气 {_time.GetWeatherDescription()}";
        _timeLabel.Text = $"时间 {_time.GetTimeDisplay()}";
        _dayLabel.Text = $"Day {_time.CurrentDay}";
        _seasonLabel.Text = $"季节 {_time.GetSeasonDisplay()}";
        _locationLabel.Text = $"位置 {_player.State.CurrentLocation}";

        // 显示活跃效果
        UpdateEffects();
    }

    private void UpdateEffects()
    {
        var effects = GetNode<EffectSystem>("/root/EffectSystem");
        var diseases = effects.GetDiseases();
        var buffs = effects.GetBuffs();

        var parts = new System.Collections.Generic.List<string>();
        foreach (var d in diseases)
        {
            var def = effects.GetDefinition(d.EffectId);
            if (def != null)
                parts.Add($"[color=red]{def.Name}[/color]");
        }
        foreach (var b in buffs)
        {
            var def = effects.GetDefinition(b.EffectId);
            if (def != null)
                parts.Add($"[color=green]{def.Name}[/color]");
        }

        _effectLabel.Text = parts.Count > 0
            ? string.Join(" | ", parts)
            : "";
    }

    private static void SetBar(ProgressBar bar, Label value, int current, int max)
    {
        bar.MaxValue = max;
        bar.Value = current;
        value.Text = $"{current}/{max}";
        var ratio = max <= 0 ? 0 : (float)current / max;
        var color = ratio > 0.6f ? new Color(0.3f, 0.8f, 0.3f) :
            ratio > 0.3f ? new Color(0.9f, 0.7f, 0.3f) :
            new Color(0.9f, 0.3f, 0.3f);
        bar.AddThemeColorOverride("fill", color);
    }
}
