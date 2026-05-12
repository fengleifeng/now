using Godot;
using CardSurvival.UI;
using CardSurvival.Data;

namespace CardSurvival;

public partial class GameRoot : Control
{
	private StatusPanel _statusPanel = null!;
	private LocationInfoBar _locationInfoBar = null!;
	private SceneArea _sceneArea = null!;
	private EnvironmentArea _environmentArea = null!;
	private HandArea _handArea = null!;
	private RichTextLabel _logPanel = null!;
	private CardManager _cards = null!;
	private CombineSystem _combine = null!;
	private PlayerSystem _player = null!;
	private TimeSystem _time = null!;
	private MapSystem _map = null!;
	private SaveSystem _saveSystem = null!;
	private EffectSystem _effects = null!;
	private Control? _popup;
	private List<CombineRule>? _rules;
	private readonly Random _random = new();

	// Dirty flag for debouncing RefreshAll
	private bool _needsRefresh;
	private bool _isRefreshing;

	public override void _Ready()
	{
		_cards = GetNode<CardManager>("/root/CardManager");
		_combine = GetNode<CombineSystem>("/root/CombineSystem");
		_player = GetNode<PlayerSystem>("/root/PlayerSystem");
		_time = GetNode<TimeSystem>("/root/TimeSystem");
		_map = GetNode<MapSystem>("/root/MapSystem");
		_saveSystem = GetNode<SaveSystem>("/root/SaveSystem");
		_effects = GetNode<EffectSystem>("/root/EffectSystem");

		BuildUI();
		ConnectSignals();
		StartOrLoadGame();
	}

	private void BuildUI()
	{
		CustomMinimumSize = new Vector2(1024, 600);
		SetAnchorsPreset(LayoutPreset.FullRect);

		var bg = new ColorRect { Color = new Color(0.06f, 0.08f, 0.1f) };
		bg.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(bg);

		var main = new HBoxContainer();
		main.SetAnchorsPreset(LayoutPreset.FullRect);
		main.AddThemeConstantOverride("separation", 0);
		AddChild(main);

		_statusPanel = new StatusPanel();
		_statusPanel.CustomMinimumSize = new Vector2(180, 0);
		main.AddChild(_statusPanel);

		main.AddChild(new VSeparator());

		var center = new VBoxContainer();
		center.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		center.SizeFlagsVertical = SizeFlags.ExpandFill;
		center.AddThemeConstantOverride("separation", 0);
		main.AddChild(center);

		_locationInfoBar = new LocationInfoBar();
		_locationInfoBar.CustomMinimumSize = new Vector2(0, 112);
		center.AddChild(_locationInfoBar);
		center.AddChild(new HSeparator());

		_sceneArea = new SceneArea();
		_sceneArea.CustomMinimumSize = new Vector2(0, 210);
		_sceneArea.SizeFlagsVertical = SizeFlags.ExpandFill;
		center.AddChild(_sceneArea);
		center.AddChild(new HSeparator());

		_handArea = new HandArea();
		_handArea.CustomMinimumSize = new Vector2(0, 170);
		_handArea.SizeFlagsVertical = SizeFlags.ShrinkBegin;
		center.AddChild(_handArea);
		center.AddChild(new HSeparator());

		_logPanel = new RichTextLabel();
		_logPanel.CustomMinimumSize = new Vector2(0, 78);
		_logPanel.SizeFlagsVertical = SizeFlags.ShrinkEnd;
		_logPanel.BbcodeEnabled = true;
		_logPanel.ScrollFollowing = true;
		_logPanel.AddThemeFontSizeOverride("normal_font_size", 12);
		center.AddChild(_logPanel);

		main.AddChild(new VSeparator());

		_environmentArea = new EnvironmentArea();
		_environmentArea.CustomMinimumSize = new Vector2(190, 0);
		main.AddChild(_environmentArea);
	}

	private void ConnectSignals()
	{
		_statusPanel.OnCraftClicked += OpenCraftPopup;
		_statusPanel.OnRestClicked += Rest;
		_statusPanel.OnMenuClicked += OpenMenuPopup;
		_locationInfoBar.OnExploreClicked += Explore;
		_locationInfoBar.OnMoveToLocation += MoveToLocation;
		_handArea.OnHandCardClicked += OnHandCardClicked;
		_handArea.OnHandCardDragEnded += OnHandCardDragEnded;
		_sceneArea.OnSceneCardClicked += OnSceneCardClicked;
		_sceneArea.OnSceneCardDragEnded += OnSceneCardDragEnded;

		_player.OnStatsChanged += RequestRefresh;
		_player.OnPlayerDeath += OnPlayerDeath;
		_cards.OnCardAdded += _ => RequestRefresh();
		_cards.OnCardRemoved += _ => RequestRefresh();
		_cards.OnCardConsumed += OnCardConsumed;
		_cards.OnSceneCardsChanged += RequestRefresh;
		_map.OnLocationChanged += _ => RequestRefresh();

		_combine.Connect(CombineSystem.SignalName.OnCombineSuccess,
			Callable.From((string a, string b, string[] results) => OnCombineSuccess(a, b, results)));
		_combine.Connect(CombineSystem.SignalName.OnCombineFail,
			Callable.From((string a, string b) => AddLog($"[color=red]合成失败：{a}+{b}[/color]")));
		_combine.Connect(CombineSystem.SignalName.OnMultiCombineSuccess,
			Callable.From((string[] ingredients, string[] results) => OnMultiCombineSuccess(ingredients, results)));
		_combine.Connect(CombineSystem.SignalName.OnMultiCombineFail,
			Callable.From(() => AddLog("[color=red]这些物品无法合成[/color]")));
	}

	private void StartOrLoadGame()
	{
		if (_player.ConsumeLoadSaveRequest() && LoadGame())
			return;

		_cards.ClearRuntimeCards();
		_map.ResetState();
		_effects.ClearAll();
		_time.CurrentDay = 1;
		_time.DayProgress = 0f;
		_time.CurrentSeason = "spring";
		_player.State.CurrentLocation = _map.CurrentLocation;
		EnsureAvailableProjects();
		_cards.DrawInitialHand(4);
		RefreshAll();
		AddLog("[color=green]你醒在荒野中。探索、拾取、合成，尽量活下去。[/color]");
	}

	public override void _Process(double delta)
	{
		if (_needsRefresh)
		{
			_needsRefresh = false;
			RefreshAll();
		}
	}

	private void RequestRefresh()
	{
		if (_isRefreshing)
		{
			_needsRefresh = true;
			return;
		}
		_needsRefresh = true;
	}

	private void RefreshAll()
	{
		if (_isRefreshing) return;
		_isRefreshing = true;
		_needsRefresh = false;
		try
		{
			var loc = _map.GetCurrentLocation();
			_handArea.Refresh(_cards.GetHand());
			_sceneArea.Refresh(_cards.GetSceneCards(), loc == null ? null : _cards.GetCard(loc.Id));
			_environmentArea.Refresh(_map.GetCurrentEnvironmentCards());
			if (loc != null)
				_locationInfoBar.Refresh(loc);
		}
		finally
		{
			_isRefreshing = false;
		}
	}

	private void AddLog(string message)
	{
		_logPanel.AppendText($"[color=gray]{_time.GetTimeDisplay()}[/color] {message}\n");
	}

	// ============================================================
	//  核心游戏操作
	// ============================================================

	private void Explore()
	{
		var loc = _map.GetCurrentLocation();
		if (loc == null) return;

		const int cost = 10;
		if (_player.State.Energy < cost)
		{
			AddLog("[color=red]精力不足，无法探索。[/color]");
			return;
		}

		_player.ConsumeEnergy(cost);
		_time.AdvanceTime(0.05f);

		// 根据季节获取探索池
		var pool = loc.GetExplorePoolForSeason(_time.CurrentSeason);
		var found = ExploreFromPool(pool);
		AddLog(found == null
			? $"探索了 {loc.Name}，一无所获。"
			: $"探索了 {loc.Name}，发现 {found.Name}。");

		// 地点特殊效果
		if (loc.SpecialEffect == "energy_bonus")
			_player.RestoreEnergy(5);
		if (loc.SpecialEffect == "wolf_danger" && _random.NextDouble() < 0.10)
			ResolveWolfEvent();

		// 探索技能经验
		_player.GainSkill("explore");
		TriggerRandomEvent();
		RefreshAll();
	}

	private CardData? ExploreFromPool(List<string> pool)
	{
		if (pool.Count == 0) return _cards.DrawCard();

		var id = pool[_random.Next(pool.Count)];
		var template = _cards.GetCard(id);
		if (template == null) return null;

		var instance = _cards.CreateCardInstance(id);
		if (instance != null)
			_cards.AddCardToScene(instance);
		return instance;
	}

	private void Rest()
	{
		if (_player.State.Energy >= _player.State.MaxEnergy)
		{
			AddLog("[color=gray]你精神饱满，不需要休息。[/color]");
			return;
		}

		if (_player.State.Hunger < 10 || _player.State.Thirst < 10)
		{
			AddLog("[color=red]太饿或太渴，无法安心休息。[/color]");
			return;
		}

		// 根据庇护所质量决定精力恢复量
		int energyRestore = 20;
		string shelterType = "野外";
		if (_player.HasCompletedBuilding("stone_house"))
		{
			energyRestore = 60;
			shelterType = "石屋";
		}
		else if (_player.HasCompletedBuilding("house"))
		{
			energyRestore = 45;
			shelterType = "木屋";
		}
		else if (_player.HasCompletedBuilding("tent") || _cards.GetHand().Any(c => c.Id == "tent"))
		{
			energyRestore = 30;
			shelterType = "帐篷";
		}

		float timePassed = 0.12f;
		if (_time.IsNight())
		{
			timePassed = 0.20f;
			energyRestore = (int)(energyRestore * 1.5f);
		}

		_player.RestoreEnergy(energyRestore);
		_player.ConsumeHunger(5);
		_player.ConsumeThirst(3);
		_time.AdvanceTime(timePassed);
		AddLog($"[color=cyan]在{shelterType}休息，精力 +{energyRestore}。[/color]");
		RefreshAll();
	}

	private void MoveToLocation(string id)
	{
		if (!_map.GetConnections(_map.CurrentLocation).Contains(id))
		{
			AddLog("[color=red]只能前往相邻区域。[/color]");
			return;
		}

		var cost = Math.Max(1, (int)MathF.Round(15 * _player.State.MoveEnergyCostMultiplier));
		if (_player.State.Energy < cost)
		{
			AddLog("[color=red]精力不足，无法移动。[/color]");
			return;
		}

		_player.ConsumeEnergy(cost);
		_player.ConsumeHunger(3);
		_player.ConsumeThirst(3);
		_time.AdvanceTime(0.10f);
		_map.MoveToLocation(id);
		_player.State.CurrentLocation = id;
		AddLog($"移动到 {(_map.GetLocation(id)?.Name ?? id)}。");
		RefreshAll();
	}

	private void TriggerRandomEvent()
	{
		var chance = 0.30f + _player.State.DiscoverBonus;
		if (_player.HasCompletedBuilding("watchtower"))
			chance *= 0.5f;
		var roll = _random.NextDouble();
		if (roll >= chance) return;

		var subRoll = _random.NextDouble();
		if (subRoll < 0.33f) ResolveWolfEvent();
		else if (subRoll < 0.50f) ResolveStormEvent();
		else if (subRoll < 0.83f)
		{
			var bonus = _cards.ExploreLocation(_map.CurrentLocation);
			AddLog(bonus == null
				? "[color=yellow]发现了一些痕迹，但没有可用物资。[/color]"
				: $"[color=yellow]幸运发现：{bonus.Name}。[/color]");
		}
		else
		{
			_player.TakeDamage(10);
			AddLog("[color=orange]被毒蛇咬伤，生命 -10。[/color]");
		}
	}

	private void ResolveWolfEvent()
	{
		if (_player.HasCompletedBuilding("wall"))
		{
			AddLog("[color=green]围墙挡住了野兽袭击。[/color]");
			return;
		}

		var hasWeapon = _cards.GetHand().Any(c => c.Tags.Contains(CardTag.Weapon));
		var successChance = hasWeapon ? 0.75f + _player.State.HuntBonus : 0.15f + _player.State.HuntBonus;
		if (_random.NextDouble() <= successChance)
		{
			_player.GainSkill("fight");
			AddLog("[color=green]你赶走了野兽。[/color]");
		}
		else
		{
			_player.TakeDamage(15);
			_effects.TryTriggerDisease("injury", 0.6f);
			AddLog("[color=orange]野兽袭击，生命 -15，可能受伤。[/color]");
		}
	}

	private void ResolveStormEvent()
	{
		var sheltered = _cards.GetHand().Any(c => c.Tags.Contains(CardTag.Shelter))
			|| _player.HasCompletedBuilding("house")
			|| _player.HasCompletedBuilding("stone_house");
		if (sheltered)
			AddLog("[color=cyan]暴风雨来了，但庇护所保护了你。[/color]");
		else
		{
			_player.UpdateTemperature(-5);
			_effects.TryTriggerDisease("cold", 0.3f);
			AddLog("[color=cyan]暴风雨来袭，温度 -5，可能感冒。[/color]");
		}
	}

	// ============================================================
	//  卡牌操作
	// ============================================================

	private void OnHandCardClicked(CardNode node)
	{
		var card = node.Data;
		OpenCardActionPopup(card);
	}

	private void OpenCardActionPopup(CardData card)
	{
		ClosePopup();
		var popup = new CardActionPopup();
		_popup = popup;
		popup.Setup(card);
		AddChild(popup);
		popup.OnAction += OnCardAction;
		popup.OnClose += ClosePopup;
	}

	private void OnCardAction(CardData card, string action)
	{
		switch (action)
		{
			case "drink":
				_player.Drink(card.ThirstValue);
				_cards.ConsumeCardFromHand(card, "drink");
				AddLog($"喝了 {card.Name}，解渴 +{card.ThirstValue}。");
				break;

			case "eat":
				_player.Eat(card.FoodValue);
				if (card.HealValue > 0)
					_player.Heal(card.HealValue);
				if (card.Tags.Contains(CardTag.Raw))
					_effects.TryTriggerDisease("food_poisoning", 0.2f);
				_cards.ConsumeCardFromHand(card, "eat");
				AddLog($"食用了 {card.Name}。");
				break;

			case "use":
				_player.Heal(card.HealValue);
				_cards.ConsumeCardFromHand(card, "use");
				var cured = _effects.TryCureWithCard(card);
				foreach (var disease in cured)
					AddLog($"[color=green]治愈了 {disease}！[/color]");
				AddLog($"使用了 {card.Name}。");
				break;

			case "discard":
				_cards.ConsumeCardFromHand(card, "discard");
				AddLog($"[color=gray]丢弃了 {card.Name}。[/color]");
				break;

			case "view":
				AddLog($"{card.Name}: {card.Description}");
				break;
		}
		RequestRefresh();
	}

	private void OnHandCardDragEnded(CardNode dropped)
	{
		if (!dropped.WasDragged) return;
		var data = dropped.Data;

		var target = dropped.GetOverlappingCard(_handArea);
		if (target != null && _cards.GetHand().Contains(data) && _cards.GetHand().Contains(target.Data))
		{
			if (_combine.TryCombine(data, target.Data))
			{
				_cards.RemoveCardFromHand(data);
				_cards.RemoveCardFromHand(target.Data);
			}
			RequestRefresh();
			return;
		}

		if (dropped.IsOverArea(_sceneArea) && _cards.GetHand().Contains(data))
		{
			_cards.MoveHandCardToScene(data);
			AddLog($"放下了 {data.Name}。");
		}
		RefreshAll();
	}

	private void OnSceneCardClicked(CardNode node)
	{
		var card = node.Data;
		if (card.Type == CardType.Location) return;
		if (_cards.GetSceneCards().Contains(card))
		{
			_cards.MoveSceneCardToHand(card);
			AddLog($"拾取了 {card.Name}。");
		}
	}

	private void OnSceneCardDragEnded(CardNode dropped)
	{
		var card = dropped.Data;
		if (dropped.IsOverArea(_handArea) && _cards.GetSceneCards().Contains(card))
		{
			_cards.MoveSceneCardToHand(card);
			AddLog($"拾取了 {card.Name}。");
		}
		RefreshAll();
	}

	// ============================================================
	//  Popup 管理
	// ============================================================

	private void OpenCraftPopup()
	{
		ClosePopup();
		var popup = new CraftPopup();
		_popup = popup;
		popup.Setup(card);
		AddChild(popup);
		popup.Refresh(GetLearnedRecipes(), _player.GetActiveProjects(), _cards.GetHand());
		popup.OnCraftRecipe += CraftRecipe;
		popup.OnBuildProject += BuildProject;
		popup.OnFreeCraft += FreeCraft;
		popup.OnClearFreeCraft += () => { };
		popup.OnClose += ClosePopup;
	}

	private void OpenMenuPopup()
	{
		ClosePopup();
		var popup = new MenuPopup();
		_popup = popup;
		popup.Setup(card);
		AddChild(popup);
		popup.OnSave += () =>
		{
			SaveGame();
			AddLog("[color=green]游戏已保存。[/color]");
		};
		popup.OnQuit += () => GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
		popup.OnClose += ClosePopup;
	}

	private void ClosePopup()
	{
		_popup?.QueueFree();
		_popup = null;
		RefreshAll();
	}

	// ============================================================
	//  合成与建造
	// ============================================================

	private void CraftRecipe(CombineRule rule)
	{
		if (!_combine.CanCraftRecipe(rule, _cards.GetHand()))
		{
			AddLog("[color=red]材料不足。[/color]");
			return;
		}

		var consumed = _combine.CraftRecipe(rule, _cards.GetHand());
		foreach (var card in consumed)
			_cards.RemoveCardFromHand(card);
		foreach (var id in rule.Results)
			AddResultToScene(id);

		EnsureAvailableProjects();
		AddLog($"[color=green]合成成功：{DescribeRule(rule)}。[/color]");
		RefreshCraftPopup();
	}

	private void FreeCraft(List<CardData> ingredients)
	{
		if (ingredients.Count < 2) return;
		if (!_combine.TryMultiCombine(ingredients)) return;

		foreach (var card in ingredients)
			if (_cards.GetHand().Contains(card))
				_cards.RemoveCardFromHand(card);
		EnsureAvailableProjects();
		RefreshCraftPopup();
	}

	private void BuildProject(string id)
	{
		var project = _player.GetActiveProjects().FirstOrDefault(p => p.Id == id);
		var def = _cards.GetProject(id);
		if (project == null || def == null) return;

		var material = _cards.GetHand().FirstOrDefault(c => c.Id == def.MaterialId);
		if (material == null)
		{
			AddLog("[color=red]缺少建造材料。[/color]");
			return;
		}

		_cards.RemoveCardFromHand(material);
		_time.AdvanceTime(Math.Max(0.01f, 0.02f / _player.State.BuildSpeed));
		var done = _player.BuildProject(id);
		AddLog($"建造 {def.Name}: {project.Progress}/{project.Required}");

		if (done)
		{
			_player.CompleteProject(id);
			_player.AddCompletedBuilding(id);
			_map.AddCompletedBuildingToEnvironment(def.ResultCardId);
			AddResultToScene(def.ResultCardId);
			AddLog($"[color=green]{def.Name} 完成。[/color]");

			// 木屋/石屋提供庇护Buff
			if (id == "house" || id == "stone_house")
				_effects.AddEffect("sheltered", 1, -1);

			EnsureAvailableProjects();
		}
		RefreshCraftPopup();
	}

	private void OnCombineSuccess(string a, string b, string[] results)
	{
		foreach (var id in results)
			AddResultToScene(id);
		EnsureAvailableProjects();
		AddLog(results.Length == 0
			? $"[color=green]{a}+{b} 被消除。[/color]"
			: $"[color=green]合成成功：{a}+{b}。[/color]");
	}

	private void OnMultiCombineSuccess(string[] ingredients, string[] results)
	{
		foreach (var id in results)
			AddResultToScene(id);
		AddLog(results.Length == 0
			? $"[color=green]{string.Join("+", ingredients)} 被消除。[/color]"
			: $"[color=green]自由合成成功：{string.Join("+", ingredients)}。[/color]");
	}

	private void AddResultToScene(string id)
	{
		if (string.IsNullOrEmpty(id)) return;
		var card = _cards.CreateCardInstance(id);
		if (card != null)
			_cards.AddCardToScene(card);
	}

	private void EnsureAvailableProjects()
	{
		foreach (var project in _cards.GetAllProjects())
		{
			if (_player.HasCompletedBuilding(project.Id)) continue;
			if (_player.GetActiveProjects().Any(p => p.Id == project.Id)) continue;
			if (string.IsNullOrEmpty(project.UnlockRecipe)
				|| _player.IsRecipeLearned(project.UnlockRecipe)
				|| _player.HasCompletedBuilding(project.UnlockRecipe))
				_player.AddProject(project);
		}
	}

	private List<CombineRule> GetLearnedRecipes()
	{
		_rules ??= _combine.GetRules();
		return _rules
			.Where(r => IsStarterRecipe(r) || _player.IsRecipeLearned(_combine.GetRecipeKey(r)))
			.ToList();
	}

	private static bool IsStarterRecipe(CombineRule rule)
	{
		if (rule.Ingredients.Count > 0)
			return false;
		if (rule.Results.Contains("axe") && rule.CardA == "wood" && rule.CardB == "stone")
			return true;
		if (rule.Results.Contains("campfire") && rule.CardA == "wood" && rule.CardB == "wood")
			return true;
		if (rule.Results.Contains("cooked_meat") && (rule.CardA == "meat" || rule.CardA == "fish") && rule.CardB == "campfire")
			return true;
		if (rule.Results.Contains("tent") && rule.CardA == "wood" && rule.CardB == "hide")
			return true;
		return false;
	}

	private string DescribeRule(CombineRule rule)
	{
		var inputs = rule.Ingredients.Count > 0 ? rule.Ingredients : new List<string> { rule.CardA, rule.CardB };
		return $"{string.Join("+", inputs)} -> {string.Join("+", rule.Results)}";
	}

	private void RefreshCraftPopup()
	{
		RequestRefresh();
		if (_popup is CraftPopup popup)
			popup.Refresh(GetLearnedRecipes(), _player.GetActiveProjects(), _cards.GetHand());
	}

	private void OnCardConsumed(string id, string reason)
	{
		if (reason == "hand_limit")
			AddLog($"[color=orange]手牌超过上限，丢弃 {id}。[/color]");
		RequestRefresh();
	}

	private void OnPlayerDeath()
	{
		AddLog("[color=red]你倒下了。[/color]");
		GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
	}

	// ============================================================
	//  存档（委托给 SaveSystem）
	// ============================================================

	private void SaveGame()
	{
		_saveSystem.SaveGame(_player.State, _cards, _map, _time);
	}

	private bool LoadGame()
	{
		var loaded = _saveSystem.LoadGame(_player.State, _cards, _map, _time);
		if (loaded)
		{
			EnsureAvailableProjects();
			RefreshAll();
			AddLog("[color=green]游戏已加载。[/color]");
		}
		return loaded;
	}
}
