using Godot;

namespace CardSurvival.UI;

public partial class MenuPopup : Control
{
	public event Action? OnSave;
	public event Action? OnBackup;
	public event Action? OnQuit;
	public event Action? OnClose;

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		BuildUI();
	}

	private void BuildUI()
	{
		ModalUi.AddDimOverlay(this, () => OnClose?.Invoke());
		var center = ModalUi.AddCenterLayer(this);

		var shell = new PanelContainer();
		shell.CustomMinimumSize = new Vector2(300, 320);
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

		var title = new Label { Text = "菜单", HorizontalAlignment = HorizontalAlignment.Center };
		title.AddThemeFontSizeOverride("font_size", 18);
		title.AddThemeColorOverride("font_color", GameTheme.TextPrimary);
		box.AddChild(title);

		var settings = GetNodeOrNull<GameSettings>("/root/GameSettings");
		if (settings != null)
		{
			var soundRow = new CheckBox
			{
				Text = "卡牌操作音效",
				ButtonPressed = settings.CardActionSoundsEnabled
			};
			soundRow.AddThemeFontSizeOverride("font_size", 13);
			soundRow.AddThemeColorOverride("font_color", GameTheme.TextPrimary);
			soundRow.Toggled += pressed => { settings.CardActionSoundsEnabled = pressed; };
			box.AddChild(soundRow);
		}

		box.AddChild(MakeButton("保存游戏", () => OnSave?.Invoke()));
		box.AddChild(MakeButton("备份存档（无上限）", () => OnBackup?.Invoke()));
		box.AddChild(MakeButton("返回主菜单", () => OnQuit?.Invoke()));
		box.AddChild(MakeButton("关闭", () => OnClose?.Invoke()));
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
