using Godot;
using CardSurvival.Data;
using CardSurvival;

namespace CardSurvival.UI;

public partial class StatusPanel : PanelContainer
{
	[Signal] public delegate void OnCraftClickedEventHandler();
	[Signal] public delegate void OnRestClickedEventHandler();
	[Signal] public delegate void OnSharpenClickedEventHandler();
	[Signal] public delegate void OnNightRitualClickedEventHandler();
	[Signal] public delegate void OnMenuClickedEventHandler();

	private ProgressBar _hpBar = null!;
	private ProgressBar _hungerBar = null!;
	private ProgressBar _thirstBar = null!;
	private ProgressBar _energyBar = null!;
	private ProgressBar _sanityBar = null!;
	private ProgressBar _proteinBar = null!;
	private ProgressBar _vitaminsBar = null!;
	private ProgressBar _carbBar = null!;
	private ProgressBar _bodyFatBar = null!;
	private Label _hpValue = null!;
	private Label _hungerValue = null!;
	private Label _thirstValue = null!;
	private Label _energyValue = null!;
	private Label _sanityValue = null!;
	private Label _proteinValue = null!;
	private Label _vitaminsValue = null!;
	private Label _carbValue = null!;
	private Label _bodyFatValue = null!;
	private Label _tempLabel = null!;
	private Label _weatherLabel = null!;
	private Label _timeLabel = null!;
	private Label _dayLabel = null!;
	private Label _seasonLabel = null!;
	private Label _locationLabel = null!;
	private Label _effectLabel = null!;
	private PlayerSystem _player = null!;
	private TimeSystem _time = null!;
	private MapSystem _map = null!;
	private CardManager _cards = null!;
	private EffectSystem _effects = null!;
	private Button _sharpenBtn = null!;
	private Button _ritualBtn = null!;

	public override void _Ready()
	{
		_player = GetNode<PlayerSystem>("/root/PlayerSystem");
		_time = GetNode<TimeSystem>("/root/TimeSystem");
		_map = GetNode<MapSystem>("/root/MapSystem");
		_cards = GetNode<CardManager>("/root/CardManager");
		_effects = GetNode<EffectSystem>("/root/EffectSystem");

		CustomMinimumSize = new Vector2(200, 0);
		SizeFlagsVertical = SizeFlags.ExpandFill;
		GameTheme.ApplyPanel(this, GameTheme.PanelSidebar);

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

		var header = new Label { Text = "幸存者" };
		header.AddThemeFontSizeOverride("font_size", 15);
		header.AddThemeColorOverride("font_color", GameTheme.TextPrimary);
		content.AddChild(header);

		content.AddChild(MakeActionButton("合成", () => EmitSignal(SignalName.OnCraftClicked)));
		content.AddChild(MakeActionButton("休息", () => EmitSignal(SignalName.OnRestClicked)));
		_sharpenBtn = MakeActionButton("磨刀 -8精力", () => EmitSignal(SignalName.OnSharpenClicked));
		content.AddChild(_sharpenBtn);
		_ritualBtn = MakeActionButton("夜仪", () => EmitSignal(SignalName.OnNightRitualClicked));
		_ritualBtn.Visible = false;
		content.AddChild(_ritualBtn);
		content.AddChild(MakeSeparator());

		AddStat(content, "生命", out _hpBar, out _hpValue, false, false);
		AddStat(content, "饱食", out _hungerBar, out _hungerValue, false, false);
		AddStat(content, "口渴", out _thirstBar, out _thirstValue, false, false);
		AddStat(content, "精力", out _energyBar, out _energyValue, true, false);
		AddStat(content, "精神", out _sanityBar, out _sanityValue, false, true);
		content.AddChild(MakeSeparator());
		var nutHdr = new Label { Text = "营养 / 体成分" };
		GameTheme.StyleSectionLabel(nutHdr, GameTheme.TextMuted);
		content.AddChild(nutHdr);
		AddNutrientBar(content, "蛋白质", out _proteinBar, out _proteinValue);
		AddNutrientBar(content, "维生素", out _vitaminsBar, out _vitaminsValue);
		AddNutrientBar(content, "碳水", out _carbBar, out _carbValue);
		AddBodyFatBar(content, "体脂倾向", out _bodyFatBar, out _bodyFatValue);
		content.AddChild(MakeSeparator());

		_tempLabel = MakeInfoLine(content);
		_weatherLabel = MakeInfoLine(content);
		_timeLabel = MakeInfoLine(content);
		_dayLabel = MakeInfoLine(content);
		_seasonLabel = MakeInfoLine(content);
		_locationLabel = MakeInfoLine(content);

		content.AddChild(MakeSeparator());

		_effectLabel = new Label();
		_effectLabel.AddThemeFontSizeOverride("font_size", 10);
		_effectLabel.AddThemeColorOverride("font_color", new Color(0.92f, 0.72f, 0.72f));
		_effectLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_effectLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		content.AddChild(_effectLabel);

		var spacer = new Control();
		spacer.SizeFlagsVertical = SizeFlags.ExpandFill;
		content.AddChild(spacer);

		content.AddChild(MakeSeparator());
		content.AddChild(MakeActionButton("菜单", () => EmitSignal(SignalName.OnMenuClicked), 30));

		_player.OnStatsChanged += UpdateStats;
		_time.OnTimeChanged += (_, _, _) => UpdateStats();
		_time.OnWeatherChanged += _ => UpdateStats();
		_time.OnSeasonChanged += _ => UpdateStats();
		_effects.OnEffectAdded += (_, _) => UpdateStats();
		_effects.OnEffectRemoved += _ => UpdateStats();
		_map.OnLocationChanged += _ => UpdateStats();
		_cards.OnCardAdded += _ => UpdateStats();
		_cards.OnCardRemoved += _ => UpdateStats();
		UpdateStats();
	}

	private static Button MakeActionButton(string text, Action onPressed, int height = 34)
	{
		var button = new Button { Text = text, CustomMinimumSize = new Vector2(0, height) };
		button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		GameTheme.StyleSidebarButton(button);
		button.Pressed += onPressed;
		return button;
	}

	private static HSeparator MakeSeparator()
	{
		var sep = new HSeparator();
		sep.SelfModulate = GameTheme.Separator;
		return sep;
	}

	private static void AddStat(VBoxContainer parent, string title, out ProgressBar bar, out Label value, bool energy, bool sanity)
	{
		var row = new HBoxContainer();
		row.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		parent.AddChild(row);

		var label = new Label { Text = title };
		label.AddThemeFontSizeOverride("font_size", 12);
		label.AddThemeColorOverride("font_color", GameTheme.TextMuted);
		label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		row.AddChild(label);

		value = new Label();
		value.HorizontalAlignment = HorizontalAlignment.Right;
		value.AddThemeFontSizeOverride("font_size", 11);
		value.AddThemeColorOverride("font_color", GameTheme.TextPrimary);
		row.AddChild(value);

		bar = new ProgressBar
		{
			MinValue = 0,
			MaxValue = 100,
			Value = 100,
			CustomMinimumSize = new Vector2(0, 14),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			ShowPercentage = false
		};
		parent.AddChild(bar);
		bar.SetMeta("energy", energy);
		bar.SetMeta("sanity", sanity);
	}

	private static void AddNutrientBar(VBoxContainer parent, string title, out ProgressBar bar, out Label value)
	{
		AddStat(parent, title, out bar, out value, false, false);
		bar.SetMeta("nutrient", true);
	}

	private static void AddBodyFatBar(VBoxContainer parent, string title, out ProgressBar bar, out Label value)
	{
		AddStat(parent, title, out bar, out value, false, false);
		bar.SetMeta("bodyfat", true);
	}

	private static Label MakeInfoLine(VBoxContainer parent)
	{
		var label = new Label();
		label.AddThemeFontSizeOverride("font_size", 12);
		label.AddThemeColorOverride("font_color", GameTheme.TextPrimary);
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
		SetNutrientBar(_proteinBar, _proteinValue, _player.State.Protein);
		SetNutrientBar(_vitaminsBar, _vitaminsValue, _player.State.Vitamins);
		SetNutrientBar(_carbBar, _carbValue, _player.State.Carbohydrate);
		SetBodyFatBar(_bodyFatBar, _bodyFatValue, _player.State.BodyFatIndex);
		_tempLabel.Text = $"温度 {_player.State.Temperature}°C";
		_weatherLabel.Text = $"天气 {_time.GetWeatherDescription()}";
		_timeLabel.Text = $"时间 {_time.GetTimeDisplay()}";
		_dayLabel.Text = $"第 {_time.CurrentDay} 天";
		_seasonLabel.Text = $"季节 {_time.GetSeasonDisplay()}";
		var locName = _map.GetLocation(_player.State.CurrentLocation)?.Name ?? _player.State.CurrentLocation;
		_locationLabel.Text = $"位置 {locName}";

		UpdateEffects();
	}

	private void UpdateEffects()
	{
		var diseases = _effects.GetDiseases();
		var buffs = _effects.GetBuffs();

		var parts = new System.Collections.Generic.List<string>();
		foreach (var d in diseases)
		{
			var def = _effects.GetDefinition(d.EffectId);
			if (def != null)
				parts.Add(def.Name);
		}
		foreach (var b in buffs)
		{
			var def = _effects.GetDefinition(b.EffectId);
			if (def != null)
				parts.Add(def.Name);
		}

		_effectLabel.Text = parts.Count > 0 ? string.Join(" · ", parts) : "状态良好";

		var ritualOk = _time.IsNight()
		               && _cards.HasCardInHand("campfire")
		               && _cards.HasCardInHand("herb")
		               && (diseases.Count > 0 || _player.State.Sanity < 50);
		_ritualBtn.Visible = ritualOk;
		_sharpenBtn.Disabled = _player.State.Energy < 8 || !_cards.CanSharpenToolInHand();
	}

	private static void SetBar(ProgressBar bar, Label value, int current, int max)
	{
		if (bar.HasMeta("nutrient") && bar.GetMeta("nutrient").AsBool())
		{
			SetNutrientBar(bar, value, current);
			return;
		}
		if (bar.HasMeta("bodyfat") && bar.GetMeta("bodyfat").AsBool())
		{
			SetBodyFatBar(bar, value, current);
			return;
		}

		bar.MaxValue = max;
		bar.Value = current;
		value.Text = $"{current}/{max}";
		var ratio = max <= 0 ? 0f : (float)current / max;
		var energy = bar.GetMeta("energy").AsBool();
		var sanity = bar.GetMeta("sanity").AsBool();
		var fill = GameTheme.ProgressFillForRatio(ratio, energy, sanity);
		bar.AddThemeColorOverride("fill", fill);
	}

	private static void SetNutrientBar(ProgressBar bar, Label value, int current)
	{
		bar.MaxValue = 100;
		bar.Value = current;
		value.Text = $"{current}";
		var ratio = current / 100f;
		bar.AddThemeColorOverride("fill", GameTheme.ProgressFillForRatio(ratio, false, false));
	}

	private static void SetBodyFatBar(ProgressBar bar, Label value, int fat)
	{
		bar.MaxValue = 100;
		bar.Value = fat;
		value.Text = $"{fat}";
		var stress = Mathf.Clamp((fat - 18) / 72f, 0f, 1f);
		bar.AddThemeColorOverride("fill", GameTheme.ProgressFillForRatio(1f - stress, false, false));
	}
}
