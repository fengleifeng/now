using System.Linq;
using Godot;
using CardSurvival.UI;
using CardSurvival.Data;
using CardSurvival.Game;
using CardSurvival;

namespace CardSurvival;

/// <summary>
/// 主界面：只负责 UI 树、弹窗与刷新；生存规则在 <see cref="CardSurvivalGame"/>。
/// </summary>
public partial class GameRoot : Control
{
	private StatusPanel _statusPanel = null!;
	private SceneArea _sceneArea = null!;
	private EnvironmentArea _environmentArea = null!;
	private HandArea _handArea = null!;
	private PanelContainer _anchoredStrip = null!;
	private HBoxContainer _anchoredRow = null!;
	private RichTextLabel _logPanel = null!;
	private CardManager _cards = null!;
	private CombineSystem _combine = null!;
	private PlayerSystem _player = null!;
	private TimeSystem _time = null!;
	private MapSystem _map = null!;
	private SaveSystem _saveSystem = null!;
	private EffectSystem _effects = null!;
	private GameSettings _settings = null!;
	private CardSurvivalGame _game = null!;
	private CanvasLayer _popupLayer = null!;
	private Control? _popup;
	private bool _needsRefresh;
	private bool _isRefreshing;
	private Label _anchoredTitleLabel = null!;

	public override void _Ready()
	{
		_cards = GetNode<CardManager>("/root/CardManager");
		_combine = GetNode<CombineSystem>("/root/CombineSystem");
		_player = GetNode<PlayerSystem>("/root/PlayerSystem");
		_time = GetNode<TimeSystem>("/root/TimeSystem");
		_map = GetNode<MapSystem>("/root/MapSystem");
		_saveSystem = GetNode<SaveSystem>("/root/SaveSystem");
		_effects = GetNode<EffectSystem>("/root/EffectSystem");
		_settings = GetNode<GameSettings>("/root/GameSettings");

		_game = new CardSurvivalGame(new CardSurvivalGame.Host
		{
			Cards = _cards,
			Player = _player,
			Time = _time,
			Map = _map,
			Combine = _combine,
			Effects = _effects,
			Save = _saveSystem,
			Settings = _settings,
			Log = AppendLog,
			RequestUiRefresh = RequestRefresh,
			RefreshCraftIfOpen = RefreshCraftPopupIfOpen,
			OnAchievementProbe = () => GetNode<AchievementSystem>("/root/AchievementSystem").OnStateMayHaveChanged()
		});

		BuildUI();
		ConnectSignals();
		I18n.LocaleChanged += OnGameLocaleChanged;
		StartOrLoadGame();
	}

	public override void _ExitTree()
	{
		I18n.LocaleChanged -= OnGameLocaleChanged;
		base._ExitTree();
	}

	private void OnGameLocaleChanged()
	{
		_anchoredTitleLabel.Text = I18n.T("ui.anchored_strip");
		RequestRefresh();
	}

	private void AppendLog(string message)
	{
		_logPanel.AppendText($"[color=gray]{_time.GetTimeDisplay()}[/color] {message}\n");
	}

	private void BuildUI()
	{
		CustomMinimumSize = new Vector2(1280, 720);
		SetAnchorsPreset(LayoutPreset.FullRect);

		var bg = new ColorRect { Color = GameTheme.BgDeep };
		bg.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(bg);

		var main = new HBoxContainer();
		main.SetAnchorsPreset(LayoutPreset.FullRect);
		main.AddThemeConstantOverride("separation", 0);
		AddChild(main);

		_statusPanel = new StatusPanel();
		_statusPanel.CustomMinimumSize = new Vector2(200, 0);
		main.AddChild(_statusPanel);

		var sepLeft = new VSeparator { SelfModulate = GameTheme.Separator };
		main.AddChild(sepLeft);

		var centerShell = new PanelContainer();
		centerShell.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		centerShell.SizeFlagsVertical = SizeFlags.ExpandFill;
		GameTheme.ApplyPanelSoft(centerShell, GameTheme.PanelMain);
		main.AddChild(centerShell);

		var center = new VBoxContainer();
		center.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		center.SizeFlagsVertical = SizeFlags.ExpandFill;
		center.AddThemeConstantOverride("separation", 0);
		centerShell.AddChild(center);

		_sceneArea = new SceneArea();
		_sceneArea.CustomMinimumSize = new Vector2(0, 288);
		_sceneArea.SizeFlagsVertical = SizeFlags.ExpandFill;
		center.AddChild(_sceneArea);
		center.AddChild(new HSeparator { SelfModulate = GameTheme.Separator });

		_anchoredStrip = new PanelContainer();
		_anchoredStrip.Visible = false;
		_anchoredStrip.CustomMinimumSize = new Vector2(0, 108);
		_anchoredStrip.SizeFlagsVertical = SizeFlags.ShrinkBegin;
		GameTheme.ApplyPanelSoft(_anchoredStrip, GameTheme.PanelElevated);
		center.AddChild(_anchoredStrip);

		var anchoredMargin = new MarginContainer();
		anchoredMargin.AddThemeConstantOverride("margin_left", 10);
		anchoredMargin.AddThemeConstantOverride("margin_right", 10);
		anchoredMargin.AddThemeConstantOverride("margin_top", 6);
		anchoredMargin.AddThemeConstantOverride("margin_bottom", 6);
		anchoredMargin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		anchoredMargin.SizeFlagsVertical = SizeFlags.ExpandFill;
		_anchoredStrip.AddChild(anchoredMargin);

		var anchoredBox = new VBoxContainer();
		anchoredBox.AddThemeConstantOverride("separation", 4);
		anchoredBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		anchoredBox.SizeFlagsVertical = SizeFlags.ExpandFill;
		anchoredMargin.AddChild(anchoredBox);

		_anchoredTitleLabel = new Label { Text = I18n.T("ui.anchored_strip") };
		GameTheme.StyleSectionLabel(_anchoredTitleLabel, GameTheme.TextMuted);
		anchoredBox.AddChild(_anchoredTitleLabel);

		var anchoredScroll = new ScrollContainer
		{
			HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
			VerticalScrollMode = ScrollContainer.ScrollMode.Disabled,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
			ClipContents = true
		};
		anchoredBox.AddChild(anchoredScroll);

		_anchoredRow = new HBoxContainer();
		_anchoredRow.AddThemeConstantOverride("separation", 8);
		anchoredScroll.AddChild(_anchoredRow);

		center.AddChild(new HSeparator { SelfModulate = GameTheme.Separator });

		_handArea = new HandArea();
		_handArea.CustomMinimumSize = new Vector2(0, 168);
		_handArea.SizeFlagsVertical = SizeFlags.ShrinkBegin;
		center.AddChild(_handArea);
		center.AddChild(new HSeparator { SelfModulate = GameTheme.Separator });

		var logShell = new PanelContainer();
		logShell.CustomMinimumSize = new Vector2(0, 86);
		logShell.SizeFlagsVertical = SizeFlags.ShrinkEnd;
		GameTheme.ApplyPanel(logShell, GameTheme.PanelElevated);
		center.AddChild(logShell);

		var logMargin = new MarginContainer();
		logMargin.AddThemeConstantOverride("margin_left", 8);
		logMargin.AddThemeConstantOverride("margin_right", 8);
		logMargin.AddThemeConstantOverride("margin_top", 6);
		logMargin.AddThemeConstantOverride("margin_bottom", 6);
		logMargin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		logMargin.SizeFlagsVertical = SizeFlags.ExpandFill;
		logShell.AddChild(logMargin);

		_logPanel = new RichTextLabel();
		_logPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		_logPanel.SizeFlagsVertical = SizeFlags.ExpandFill;
		_logPanel.BbcodeEnabled = true;
		_logPanel.ScrollFollowing = true;
		GameTheme.StyleRichLog(_logPanel);
		logMargin.AddChild(_logPanel);

		var sepRight = new VSeparator { SelfModulate = GameTheme.Separator };
		main.AddChild(sepRight);

		_environmentArea = new EnvironmentArea();
		_environmentArea.CustomMinimumSize = new Vector2(200, 0);
		main.AddChild(_environmentArea);

		_popupLayer = new CanvasLayer { Layer = 90, Name = "PopupLayer" };
		AddChild(_popupLayer);
	}

	private void ConnectSignals()
	{
		_statusPanel.OnCraftClicked += OpenCraftPopup;
		_statusPanel.OnRestClicked += () => _game.Rest();
		_statusPanel.OnSharpenClicked += () => _game.Sharpen();
		_statusPanel.OnNightRitualClicked += () => _game.NightRitual();
		_statusPanel.OnMenuClicked += OpenMenuPopup;
		_sceneArea.OnLocationExploreClicked += () => _game.Explore();
		_sceneArea.OnLocationMoveClicked += id => _game.MoveToLocation(id);
		_handArea.OnHandCardClicked += OnHandCardClicked;
		_handArea.OnHandCardDragEnded += OnHandCardDragEnded;
		_sceneArea.OnSceneCardClicked += OnSceneCardClicked;
		_sceneArea.OnSceneCardDragEnded += OnSceneCardDragEnded;

		_player.OnStatsChanged += RequestRefresh;
		_player.OnPlayerDeath += OnPlayerDeath;
		_cards.OnCardAdded += _ => RequestRefresh();
		_cards.OnCardRemoved += _ => RequestRefresh();
		_cards.OnCardConsumed += _game.OnCardConsumed;
		_cards.OnSceneCardsChanged += RequestRefresh;
		_map.OnLocationChanged += _ => RequestRefresh();
		_time.OnWeatherChanged += _ => RequestRefresh();
		_time.OnDayChanged += _ => RequestRefresh();

		_combine.Connect(CombineSystem.SignalName.OnCombineSuccess,
			Callable.From((string a, string b, string[] results, double synthesisMinutes) =>
			{
				_game.OnCombineSuccess(a, b, results, synthesisMinutes);
				_settings.PlayCardUiSound(CardUiSoundKind.CombineSuccess);
			}));
		_combine.Connect(CombineSystem.SignalName.OnCombineFail,
			Callable.From((string a, string b) =>
			{
				AppendLog(I18n.Tf("ui.combine_fail_fmt", a, b));
				_game.AdvanceCombineFailTime();
				_settings.PlayCardUiSound(CardUiSoundKind.CombineFail);
			}));
		_combine.Connect(CombineSystem.SignalName.OnMultiCombineSuccess,
			Callable.From((string[] ingredients, string[] results, double synthesisMinutes) =>
			{
				_game.OnMultiCombineSuccess(ingredients, results, synthesisMinutes);
				_settings.PlayCardUiSound(CardUiSoundKind.CombineSuccess);
			}));
		_combine.Connect(CombineSystem.SignalName.OnMultiCombineFail,
			Callable.From(() =>
			{
				AppendLog(I18n.T("ui.combine_impossible"));
				_game.AdvanceCombineFailTime();
				_settings.PlayCardUiSound(CardUiSoundKind.CombineFail);
			}));
		_combine.Connect(CombineSystem.SignalName.OnRecipeLearned,
			Callable.From((string _) => _game.InvalidateRulesCache()));
	}

	private void StartOrLoadGame()
	{
		if (_player.ConsumeLoadSaveRequest() && _game.TryLoadGame())
			return;

		_game.NewGame();
	}

	public override void _Process(double delta)
	{
		var ach = GetNodeOrNull<AchievementSystem>("/root/AchievementSystem");
		if (ach != null)
		{
			while (ach.TryDequeueLog(out var line))
				AppendLog(line);
		}

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
			_handArea.Refresh(_cards.GetHandForUi());
			_sceneArea.Refresh(
				_cards.GetSceneCards(),
				loc == null ? null : _cards.GetCard(loc.Id),
				loc);
			_environmentArea.Refresh(_map.GetCurrentEnvironmentCards());
			RefreshAnchoredStrip();
		}
		finally
		{
			_isRefreshing = false;
		}
	}

	private void OnHandCardClicked(CardNode node)
	{
		ClosePopup();
		_settings.PlayCardUiSound(CardUiSoundKind.Tap);
		if (CardManager.IsStagedCombineDisplay(node.Data))
		{
			OpenStagedCombinePopup();
			return;
		}

		if (node.Data.IsStagedIngredient)
			return;

		var popup = new CardActionPopup();
		_popup = popup;
		popup.Setup(node.Data);
		_popupLayer.AddChild(popup);
		popup.OnAction += OnCardAction;
		popup.OnClose += ClosePopup;
	}

	private void OpenStagedCombinePopup()
	{
		var rule = _cards.GetStagedCombineRule();
		if (rule == null) return;

		var popup = new StagedCombinePopup();
		popup.Setup(rule, _cards);
		_popup = popup;
		_popupLayer.AddChild(popup);
		popup.OnSynthesize += () =>
		{
			if (_game.TryCompleteStagedCombine())
			{
				_settings.PlayCardUiSound(CardUiSoundKind.CombineSuccess);
				ClosePopup();
			}
		};
		popup.OnCancelStage += () =>
		{
			_game.CancelStagedCombine();
			ClosePopup();
		};
		popup.OnClose += ClosePopup;
	}

	private void RefreshAnchoredStrip()
	{
		foreach (var ch in _anchoredRow.GetChildren().ToArray())
		{
			_anchoredRow.RemoveChild(ch);
			ch.QueueFree();
		}

		var c = _cards.GetAnchoredCard();
		if (c == null)
		{
			_anchoredStrip.Visible = false;
			return;
		}

		_anchoredStrip.Visible = true;
		var node = new CardNode();
		node.Setup(c);
		node.OnCardClicked += OnAnchoredCardClicked;
		_anchoredRow.AddChild(node);
	}

	private void OnAnchoredCardClicked(CardNode cardNode)
	{
		ClosePopup();
		_settings.PlayCardUiSound(CardUiSoundKind.Tap);
		var popup = new CardActionPopup();
		_popup = popup;
		popup.Setup(cardNode.Data);
		_popupLayer.AddChild(popup);
		popup.OnAction += OnCardAction;
		popup.OnClose += ClosePopup;
	}

	private void OnCardAction(CardData card, string action)
	{
		_game.HandleCardAction(card, action);
		switch (action)
		{
			case "view":
				break;
			case "discard":
				_settings.PlayCardUiSound(CardUiSoundKind.Discard);
				break;
			default:
				_settings.PlayCardUiSound(CardUiSoundKind.Use);
				break;
		}
	}

	private void OnHandCardDragEnded(CardNode dropped)
	{
		if (!dropped.WasDragged) return;
		var data = dropped.Data;
		if (CardManager.IsStagedCombineDisplay(data) || data.IsStagedIngredient)
		{
			RefreshAll();
			return;
		}

		var target = dropped.GetOverlappingCard(_handArea);
		if (target != null && _cards.GetHandForUi().Contains(data) && _cards.GetHandForUi().Contains(target.Data))
		{
			if (_game.TryCombineHandPair(data, target.Data))
				RequestRefresh();
			else
				RefreshAll();
			return;
		}

		var overSceneZone = dropped.IsOverArea(_sceneArea);
		if (overSceneZone && _cards.GetHandForUi().Contains(data))
		{
			if (_game.TrySceneHandDance(data, out var dancePulse))
			{
				if (dancePulse)
					_sceneArea.PlayDancePulse();
				_settings.PlayCardUiSound(dancePulse ? CardUiSoundKind.CombineSuccess : CardUiSoundKind.Tap);
				RequestRefresh();
				return;
			}
		}

		if (dropped.IsOverArea(_sceneArea) && _cards.GetHandForUi().Contains(data))
		{
			_game.OnHandCardToScene(data);
			_settings.PlayCardUiSound(CardUiSoundKind.Drop);
		}
		else
			RefreshAll();
	}

	private void OnSceneCardClicked(CardNode node)
	{
		var card = node.Data;
		if (card.Type == CardType.Location) return;
		if (_cards.GetSceneCards().Contains(card))
		{
			_game.OnSceneCardToHand(card);
			_settings.PlayCardUiSound(CardUiSoundKind.Pickup);
		}
	}

	private void OnSceneCardDragEnded(CardNode dropped)
	{
		var card = dropped.Data;
		if (dropped.IsOverArea(_handArea) && _cards.GetSceneCards().Contains(card))
		{
			_game.OnSceneCardToHand(card);
			_settings.PlayCardUiSound(CardUiSoundKind.Pickup);
		}
		else
			RefreshAll();
	}

	private void OpenCraftPopup()
	{
		ClosePopup();
		_settings.PlayCardUiSound(CardUiSoundKind.Tap);
		var popup = new CraftPopup();
		_popup = popup;
		_popupLayer.AddChild(popup);
		popup.Refresh(_game.GetLearnedRecipes(), _player.GetActiveProjects(), _cards.GetPlayableHand());
		popup.OnCraftRecipe += rule =>
		{
			if (_game.CraftRecipe(rule))
				_settings.PlayCardUiSound(CardUiSoundKind.CombineSuccess);
		};
		popup.OnStageRecipe += rule =>
		{
			if (!_game.TryStageCombineRecipe(rule)) return;
			_settings.PlayCardUiSound(CardUiSoundKind.Tap);
			ClosePopup();
		};
		popup.OnBuildProject += id =>
		{
			if (_game.BuildProject(id))
				_settings.PlayCardUiSound(CardUiSoundKind.Tap);
		};
		popup.OnFreeCraft += ingredients => { _game.FreeCraft(ingredients); };
		popup.OnClearFreeCraft += () => { };
		popup.OnClose += ClosePopup;
	}

	private void RefreshCraftPopupIfOpen()
	{
		RequestRefresh();
		if (_popup is CraftPopup popup)
			popup.Refresh(_game.GetLearnedRecipes(), _player.GetActiveProjects(), _cards.GetPlayableHand());
	}

	private void OpenMenuPopup()
	{
		ClosePopup();
		var popup = new MenuPopup();
		_popup = popup;
		_popupLayer.AddChild(popup);
		popup.OnSave += () =>
		{
			_game.SaveGame();
			AppendLog(I18n.T("ui.save_ok"));
		};
		popup.OnBackup += () =>
		{
			_game.SaveUserBackup();
			AppendLog(I18n.T("ui.backup_ok"));
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

	private void OnPlayerDeath()
	{
		_game.OnPlayerDeath();
		GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
	}
}
