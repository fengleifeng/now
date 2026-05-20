using Godot;
using CardSurvival;

namespace CardSurvival.UI;

public partial class MenuPopup : Control
{
	public event Action? OnSave;
	public event Action? OnBackup;
	public event Action? OnQuit;
	public event Action? OnClose;

	private Label _title = null!;
	private CheckBox _soundRow = null!;
	private CheckBox _revealRow = null!;
	private Button _saveBtn = null!;
	private Button _backupBtn = null!;
	private Button _quitBtn = null!;
	private Button _closeBtn = null!;
	private OptionButton _localeOption = null!;
	private Label _langLabel = null!;
	private GameSettings? _settings;
	private bool _suppressLocaleUi;

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		_settings = GetNodeOrNull<GameSettings>("/root/GameSettings");
		BuildUI();
		ApplyTexts();
		I18n.LocaleChanged += ApplyTexts;
	}

	public override void _ExitTree()
	{
		I18n.LocaleChanged -= ApplyTexts;
		base._ExitTree();
	}

	private void BuildUI()
	{
		ModalUi.AddDimOverlay(this, () => OnClose?.Invoke());
		var center = ModalUi.AddCenterLayer(this);

		var shell = new PanelContainer();
		UiLayout.ClampPanelMinSize(shell, new Vector2(300, 380));
		UiLayout.BindResponsive(this, () => UiLayout.ClampPanelMinSize(shell, new Vector2(300, 380)));
		GameTheme.ApplyModalPanel(shell);
		center.AddChild(shell);

		var margin = new MarginContainer();
		margin.SetAnchorsPreset(LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 14);
		margin.AddThemeConstantOverride("margin_right", 14);
		margin.AddThemeConstantOverride("margin_top", 12);
		margin.AddThemeConstantOverride("margin_bottom", 12);
		shell.AddChild(margin);

		var box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 10);
		margin.AddChild(box);

		_title = new Label { HorizontalAlignment = HorizontalAlignment.Center };
		_title.AddThemeFontSizeOverride("font_size", 18);
		_title.AddThemeColorOverride("font_color", GameTheme.TextPrimary);
		box.AddChild(_title);

		if (_settings != null)
		{
			_soundRow = new CheckBox { ButtonPressed = _settings.CardActionSoundsEnabled };
			_soundRow.AddThemeFontSizeOverride("font_size", 13);
			_soundRow.AddThemeColorOverride("font_color", GameTheme.TextPrimary);
			_soundRow.Toggled += pressed => { _settings.CardActionSoundsEnabled = pressed; };
			box.AddChild(_soundRow);

			_revealRow = new CheckBox { ButtonPressed = _settings.RevealAllRecipesInCraftUi };
			_revealRow.AddThemeFontSizeOverride("font_size", 13);
			_revealRow.AddThemeColorOverride("font_color", GameTheme.TextPrimary);
			_revealRow.Toggled += pressed => { _settings.RevealAllRecipesInCraftUi = pressed; };
			box.AddChild(_revealRow);

			var langRow = new HBoxContainer();
			langRow.AddThemeConstantOverride("separation", 8);
			langRow.Alignment = BoxContainer.AlignmentMode.Center;
			box.AddChild(langRow);

			var langLabel = new Label();
			langLabel.AddThemeFontSizeOverride("font_size", 13);
			langLabel.AddThemeColorOverride("font_color", GameTheme.TextPrimary);
			langRow.AddChild(langLabel);
			_langLabel = langLabel;

			_localeOption = new OptionButton { CustomMinimumSize = new Vector2(160, 0) };
			_localeOption.AddItem("简体中文", 0);
			_localeOption.AddItem("English", 1);
			_localeOption.ItemSelected += OnLocaleSelected;
			langRow.AddChild(_localeOption);
		}

		_saveBtn = MakeButton("", () => OnSave?.Invoke());
		box.AddChild(_saveBtn);
		_backupBtn = MakeButton("", () => OnBackup?.Invoke());
		box.AddChild(_backupBtn);
		_quitBtn = MakeButton("", () => OnQuit?.Invoke());
		box.AddChild(_quitBtn);
		_closeBtn = MakeButton("", () => OnClose?.Invoke());
		box.AddChild(_closeBtn);
	}

	private void OnLocaleSelected(long index)
	{
		if (_suppressLocaleUi) return;
		if (_settings == null) return;
		_settings.UiLocale = index == 1 ? I18n.EnglishLocale : I18n.DefaultLocale;
	}

	private void ApplyTexts()
	{
		if (_langLabel != null) _langLabel.Text = I18n.T("menu.lang_label");
		_title.Text = I18n.T("menu.popup_title");
		if (_soundRow != null) _soundRow.Text = I18n.T("menu.sound_fx");
		if (_revealRow != null) _revealRow.Text = I18n.T("menu.reveal_all_recipes");
		if (_localeOption != null && _settings != null)
		{
			_suppressLocaleUi = true;
			_localeOption.Select(_settings.UiLocale == I18n.EnglishLocale ? 1 : 0);
			_suppressLocaleUi = false;
		}

		_saveBtn.Text = I18n.T("menu.save");
		_backupBtn.Text = I18n.T("menu.backup");
		_quitBtn.Text = I18n.T("menu.quit_to_main");
		_closeBtn.Text = I18n.T("menu.close");
	}

	private static Button MakeButton(string text, Action onPressed)
	{
		var button = new Button { Text = text, CustomMinimumSize = new Vector2(0, 34) };
		button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		GameTheme.StyleSidebarButton(button);
		button.Pressed += onPressed;
		return button;
	}
}
