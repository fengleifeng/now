using Godot;
using CardSurvival.Core;
using CardSurvival.UI.Menu;

namespace CardSurvival.UI;

/// <summary>
/// 主菜单场景：负责按钮绑定、文案与场景跳转，不直接散落 GetNode 路径。
/// </summary>
public partial class MainMenu : Control
{
	private GameServices _services = null!;
	private Button _startButton = null!;
	private Button _traitModeButton = null!;
	private Button _continueButton = null!;
	private Button _settingsButton = null!;
	private Button _quitButton = null!;
	private Label _titleLabel = null!;

	/// <summary>初始化服务引用、控件、样式与响应式布局。</summary>
	public override void _Ready()
	{
		_services = GameServices.From(this);
		_services.Settings.Reload();

		var mainVBox = MenuScenePaths.ResolveMainMenuVBox(this);
		_titleLabel = mainVBox.GetNode<Label>("TitleLabel");
		_startButton = mainVBox.GetNode<Button>("StartButton");
		_traitModeButton = mainVBox.GetNode<Button>("TraitModeButton");
		_continueButton = mainVBox.GetNode<Button>("ContinueButton");
		_settingsButton = mainVBox.GetNode<Button>("SettingsButton");
		_quitButton = mainVBox.GetNode<Button>("QuitButton");

		ApplyPanelChrome();
		WireButtons();
		_continueButton.Disabled = !_services.Save.HasSaveFile();
		ApplyMenuTexts();
		I18n.LocaleChanged += ApplyMenuTexts;
		UiLayout.BindResponsive(this, ApplyMenuLayout);
	}

	/// <summary>取消国际化订阅。</summary>
	public override void _ExitTree()
	{
		I18n.LocaleChanged -= ApplyMenuTexts;
		base._ExitTree();
	}

	/// <summary>应用菜单面板主题与内边距。</summary>
	private void ApplyPanelChrome()
	{
		if (!MenuScenePaths.TryGetMainMenuPanel(this, out var menuPanel))
			return;

		GameTheme.ApplyModalPanel(menuPanel);
		const int pad = 22;
		menuPanel.AddThemeConstantOverride("margin_left", pad);
		menuPanel.AddThemeConstantOverride("margin_top", pad);
		menuPanel.AddThemeConstantOverride("margin_right", pad);
		menuPanel.AddThemeConstantOverride("margin_bottom", pad);
	}

	/// <summary>连接各按钮的 Pressed 事件。</summary>
	private void WireButtons()
	{
		foreach (var b in new[] { _startButton, _traitModeButton, _continueButton, _settingsButton, _quitButton })
			GameTheme.StyleSidebarButton(b);

		_startButton.Pressed += OnStartPressed;
		_traitModeButton.Pressed += OnTraitModePressed;
		_continueButton.Pressed += OnContinuePressed;
		_settingsButton.Pressed += OnSettingsPressed;
		_quitButton.Pressed += OnQuitPressed;
	}

	/// <summary>按视口宽度调整菜单面板最小宽度。</summary>
	private void ApplyMenuLayout()
	{
		if (!MenuScenePaths.TryGetMainMenuPanel(this, out var panel))
			return;
		var vp = UiLayout.ViewportSize(this);
		var w = Mathf.Min(UiLayout.Scaled(this, 420), vp.X - 40f);
		panel.CustomMinimumSize = new Vector2(Mathf.Max(260f, w), 0);
	}

	/// <summary>刷新所有菜单文案（语言切换时复用）。</summary>
	private void ApplyMenuTexts()
	{
		_titleLabel.Text = I18n.T("menu.game_title");
		_startButton.Text = I18n.T("menu.start_default");
		_traitModeButton.Text = I18n.T("menu.trait_mode");
		_continueButton.Text = I18n.T("menu.continue");
		_settingsButton.Text = I18n.T("menu.settings");
		_quitButton.Text = I18n.T("menu.quit");
	}

	/// <summary>默认开局：无特质，直接进入游戏。</summary>
	private void OnStartPressed()
	{
		_services.Player.BeginNewGameWithDefaults();
		GetTree().ChangeSceneToFile(ScenePaths.GameRoot);
	}

	/// <summary>进入特质选择页。</summary>
	private void OnTraitModePressed()
	{
		_services.Player.ResetState();
		GetTree().ChangeSceneToFile(ScenePaths.CharacterSelect);
	}

	/// <summary>请求读档并进入游戏。</summary>
	private void OnContinuePressed()
	{
		_services.Player.RequestLoadSave();
		GetTree().ChangeSceneToFile(ScenePaths.GameRoot);
	}

	/// <summary>设置按钮占位（待实现设置界面）。</summary>
	private void OnSettingsPressed() =>
		GD.Print("[MainMenu] Settings clicked");

	/// <summary>退出应用。</summary>
	private void OnQuitPressed() =>
		GetTree().Quit();
}
