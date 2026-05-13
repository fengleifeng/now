using System.Collections.Generic;
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
	private Label _headerLabel = null!;
	private Label _nutHdrLabel = null!;
	private PlayerSystem _player = null!;
	private TimeSystem _time = null!;
	private MapSystem _map = null!;
	private CardManager _cards = null!;
	private EffectSystem _effects = null!;
	private Button _craftBtn = null!;
	private Button _restBtn = null!;
	private Button _menuBtn = null!;
	private Button _sharpenBtn = null!;
	private Button _ritualBtn = null!;
	private readonly List<(Label Label, string Key)> _statTitleI18n = new();

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

		_headerLabel = new Label();
		_headerLabel.AddThemeFontSizeOverride("font_size", 15);
		_headerLabel.AddThemeColorOverride("font_color", GameTheme.TextPrimary);
		content.AddChild(_headerLabel);

		_craftBtn = MakeActionButton("", () => EmitSignal(SignalName.OnCraftClicked));
		content.AddChild(_craftBtn);
		_restBtn = MakeActionButton("", () => EmitSignal(SignalName.OnRestClicked));
		content.AddChild(_restBtn);
		_sharpenBtn = MakeActionButton("", () => EmitSignal(SignalName.OnSharpenClicked));
		content.AddChild(_sharpenBtn);
		_ritualBtn = MakeActionButton("", () => EmitSignal(SignalName.OnNightRitualClicked));
		_ritualBtn.Visible = false;
		content.AddChild(_ritualBtn);
		content.AddChild(MakeSeparator());

		AddStatRow(content, "ui.stat.health", out _hpBar, out _hpValue, false, false);
		AddStatRow(content, "ui.stat.hunger", out _hungerBar, out _hungerValue, false, false);
		AddStatRow(content, "ui.stat.thirst", out _thirstBar, out _thirstValue, false, false);
		AddStatRow(content, "ui.stat.energy", out _energyBar, out _energyValue, true, false);
		AddStatRow(content, "ui.stat.sanity", out _sanityBar, out _sanityValue, false, true);
		content.AddChild(MakeSeparator());
		_nutHdrLabel = new Label();
		GameTheme.StyleSectionLabel(_nutHdrLabel, GameTheme.TextMuted);
		content.AddChild(_nutHdrLabel);
		AddNutrientBar(content, "ui.stat.protein", out _proteinBar, out _proteinValue);
		AddNutrientBar(content, "ui.stat.vitamins", out _vitaminsBar, out _vitaminsValue);
		AddNutrientBar(content, "ui.stat.carb", out _carbBar, out _carbValue);
		AddBodyFatBar(content, "ui.stat.bodyfat", out _bodyFatBar, out _bodyFatValue);
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
		_menuBtn = MakeActionButton("", () => EmitSignal(SignalName.OnMenuClicked), 30);
		content.AddChild(_menuBtn);

		_player.OnStatsChanged += UpdateStats;
		_time.OnTimeChanged += (_, _, _) => UpdateStats();
		_time.OnWeatherChanged += _ => UpdateStats();
		_time.OnSeasonChanged += _ => UpdateStats();
		_effects.OnEffectAdded += (_, _) => UpdateStats();
		_effects.OnEffectRemoved += _ => UpdateStats();
		_map.OnLocationChanged += _ => UpdateStats();
		_cards.OnCardAdded += _ => UpdateStats();
		_cards.OnCardRemoved += _ => UpdateStats();
		I18n.LocaleChanged += ApplyStaticI18n;
		ApplyStaticI18n();
		UpdateStats();
	}

	public override void _ExitTree()
	{
		I18n.LocaleChanged -= ApplyStaticI18n;
		base._ExitTree();
	}

	private void ApplyStaticI18n()
	{
		_headerLabel.Text = I18n.T("ui.survivor");
		_nutHdrLabel.Text = I18n.T("ui.nutrition_header");
		_craftBtn.Text = I18n.T("ui.craft");
		_restBtn.Text = I18n.T("ui.rest");
		_sharpenBtn.Text = I18n.T("ui.sharpen");
		_ritualBtn.Text = I18n.T("ui.night_ritual");
		_menuBtn.Text = I18n.T("ui.menu");
		foreach (var pair in _statTitleI18n)
			pair.Label.Text = I18n.T(pair.Key);
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

	private void AddStatRow(VBoxContainer parent, string titleKey, out ProgressBar bar, out Label value, bool energy, bool sanity)
	{
		var row = new HBoxContainer();
		row.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		parent.AddChild(row);

		var label = new Label { Text = I18n.T(titleKey) };
		label.AddThemeFontSizeOverride("font_size", 12);
		label.AddThemeColorOverride("font_color", GameTheme.TextMuted);
		label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		row.AddChild(label);
		_statTitleI18n.Add((label, titleKey));

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

	private void AddNutrientBar(VBoxContainer parent, string titleKey, out ProgressBar bar, out Label value)
	{
		AddStatRow(parent, titleKey, out bar, out value, false, false);
		bar.SetMeta("nutrient", true);
	}

	private void AddBodyFatBar(VBoxContainer parent, string titleKey, out ProgressBar bar, out Label value)
	{
		AddStatRow(parent, titleKey, out bar, out value, false, false);
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
		_tempLabel.Text = I18n.Tf("ui.temp_fmt", _player.State.Temperature);
		_weatherLabel.Text = I18n.Tf("ui.weather_fmt", I18n.T(_time.GetWeatherMessageKey()));
		_timeLabel.Text = I18n.Tf("ui.time_fmt", _time.GetTimeDisplay());
		_dayLabel.Text = I18n.Tf("ui.day_fmt", _time.CurrentDay);
		_seasonLabel.Text = I18n.Tf("ui.season_fmt", I18n.T(_time.GetSeasonMessageKey()));
		var locName = _map.GetLocation(_player.State.CurrentLocation)?.Name ?? _player.State.CurrentLocation;
		_locationLabel.Text = I18n.Tf("ui.location_fmt", locName);

		UpdateEffects();
	}

	private void UpdateEffects()
	{
		var diseases = _effects.GetDiseases();
		var buffs = _effects.GetBuffs();

		var parts = new List<string>();
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

		_effectLabel.Text = parts.Count > 0 ? string.Join(" · ", parts) : I18n.T("ui.effects_ok");

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
