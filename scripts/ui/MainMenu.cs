// scripts/ui/MainMenu.cs
using Godot;
using CardSurvival;

namespace CardSurvival.UI;

public partial class MainMenu : Control
{
	private Button _startButton = null!;
	private Button _traitModeButton = null!;
	private Button _continueButton = null!;
	private Button _settingsButton = null!;
	private Button _quitButton = null!;
	private Label _titleLabel = null!;

	public override void _Ready()
	{
		GetNode<GameSettings>("/root/GameSettings").Reload();

		var mainVBox = ResolveMainMenuVBox();
		_titleLabel = mainVBox.GetNode<Label>("TitleLabel");
		_startButton = mainVBox.GetNode<Button>("StartButton");
		_traitModeButton = mainVBox.GetNode<Button>("TraitModeButton");
		_continueButton = mainVBox.GetNode<Button>("ContinueButton");
		_settingsButton = mainVBox.GetNode<Button>("SettingsButton");
		_quitButton = mainVBox.GetNode<Button>("QuitButton");

		if (TryGetMenuPanel(out var menuPanel))
		{
			GameTheme.ApplyModalPanel(menuPanel);
			const int pad = 22;
			menuPanel.AddThemeConstantOverride("margin_left", pad);
			menuPanel.AddThemeConstantOverride("margin_top", pad);
			menuPanel.AddThemeConstantOverride("margin_right", pad);
			menuPanel.AddThemeConstantOverride("margin_bottom", pad);
		}

		foreach (var b in new[] { _startButton, _traitModeButton, _continueButton, _settingsButton, _quitButton })
			GameTheme.StyleSidebarButton(b);

		_startButton.Pressed += OnStartPressed;
		_traitModeButton.Pressed += OnTraitModePressed;
		_continueButton.Pressed += OnContinuePressed;
		_settingsButton.Pressed += OnSettingsPressed;
		_quitButton.Pressed += OnQuitPressed;

		var saveSystem = GetNode<SaveSystem>("/root/SaveSystem");
		_continueButton.Disabled = !saveSystem.HasSaveFile();

		ApplyMenuTexts();
		I18n.LocaleChanged += ApplyMenuTexts;
	}

	public override void _ExitTree()
	{
		I18n.LocaleChanged -= ApplyMenuTexts;
		base._ExitTree();
	}

	/// <summary>场景可能为 CenterRoot/MenuPanel/MainContainer（推荐）或旧版 CenterRoot/MainContainer。</summary>
	private VBoxContainer ResolveMainMenuVBox()
	{
		if (HasNode("CenterRoot/MenuPanel/MainContainer"))
			return GetNode<VBoxContainer>("CenterRoot/MenuPanel/MainContainer");
		return GetNode<VBoxContainer>("CenterRoot/MainContainer");
	}

	private bool TryGetMenuPanel(out PanelContainer panel)
	{
		if (HasNode("CenterRoot/MenuPanel") && GetNode("CenterRoot/MenuPanel") is PanelContainer p)
		{
			panel = p;
			return true;
		}

		panel = null!;
		return false;
	}

	private void ApplyMenuTexts()
	{
		_titleLabel.Text = I18n.T("menu.game_title");
		_startButton.Text = I18n.T("menu.start_default");
		_traitModeButton.Text = I18n.T("menu.trait_mode");
		_continueButton.Text = I18n.T("menu.continue");
		_settingsButton.Text = I18n.T("menu.settings");
		_quitButton.Text = I18n.T("menu.quit");
	}

	/// <summary>默认幸存者：不选特质，营养为 PlayerState 默认值，直接进入游戏。</summary>
	private void OnStartPressed()
	{
		GetNode<PlayerSystem>("/root/PlayerSystem").BeginNewGameWithDefaults();
		GetTree().ChangeSceneToFile("res://scenes/GameRoot.tscn");
	}

	/// <summary>可选一条特质；仍可不选即以默认营养开局。</summary>
	private void OnTraitModePressed()
	{
		GetNode<PlayerSystem>("/root/PlayerSystem").ResetState();
		GetTree().ChangeSceneToFile("res://scenes/CharacterSelect.tscn");
	}

	private void OnContinuePressed()
	{
		GetNode<PlayerSystem>("/root/PlayerSystem").RequestLoadSave();
		GetTree().ChangeSceneToFile("res://scenes/GameRoot.tscn");
	}

	private void OnSettingsPressed()
	{
		GD.Print("[MainMenu] Settings clicked");
	}

	private void OnQuitPressed()
	{
		GetTree().Quit();
	}
}
