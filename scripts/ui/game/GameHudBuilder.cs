using Godot;

namespace CardSurvival.UI.Game;

/// <summary>
/// 负责在代码中搭建游戏主界面 UI 树，与业务逻辑、信号绑定解耦。
/// </summary>
public static class GameHudBuilder
{
	/// <summary>
	/// 在 <paramref name="root"/> 下创建完整 HUD，并返回所有关键子控件的引用。
	/// </summary>
	public static GameHud Build(Control root)
	{
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		root.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

		var bg = new ColorRect { Color = GameTheme.BgDeep };
		bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.AddChild(bg);

		var main = new HBoxContainer();
		main.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		main.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		main.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		main.AddThemeConstantOverride("separation", 0);
		root.AddChild(main);

		var statusPanel = new StatusPanel();
		statusPanel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		main.AddChild(statusPanel);
		main.AddChild(new VSeparator { SelfModulate = GameTheme.Separator });

		var centerShell = new PanelContainer();
		centerShell.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		centerShell.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		GameTheme.ApplyPanelSoft(centerShell, GameTheme.PanelMain);
		main.AddChild(centerShell);

		var center = new VBoxContainer();
		center.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		center.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		center.AddThemeConstantOverride("separation", 0);
		centerShell.AddChild(center);

		var sceneArea = new SceneArea();
		sceneArea.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		center.AddChild(sceneArea);
		center.AddChild(new HSeparator { SelfModulate = GameTheme.Separator });

		var anchoredStrip = new PanelContainer();
		anchoredStrip.Visible = false;
		anchoredStrip.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
		GameTheme.ApplyPanelSoft(anchoredStrip, GameTheme.PanelElevated);
		center.AddChild(anchoredStrip);

		var anchoredMargin = new MarginContainer();
		anchoredMargin.AddThemeConstantOverride("margin_left", 10);
		anchoredMargin.AddThemeConstantOverride("margin_right", 10);
		anchoredMargin.AddThemeConstantOverride("margin_top", 6);
		anchoredMargin.AddThemeConstantOverride("margin_bottom", 6);
		anchoredMargin.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		anchoredMargin.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		anchoredStrip.AddChild(anchoredMargin);

		var anchoredBox = new VBoxContainer();
		anchoredBox.AddThemeConstantOverride("separation", 4);
		anchoredBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		anchoredBox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		anchoredMargin.AddChild(anchoredBox);

		var anchoredTitleLabel = new Label { Text = I18n.T("ui.anchored_strip") };
		GameTheme.StyleSectionLabel(anchoredTitleLabel, GameTheme.TextMuted);
		anchoredBox.AddChild(anchoredTitleLabel);

		var anchoredScroll = new ScrollContainer
		{
			HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
			VerticalScrollMode = ScrollContainer.ScrollMode.Disabled,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			ClipContents = true
		};
		anchoredBox.AddChild(anchoredScroll);

		var anchoredRow = new HBoxContainer();
		anchoredRow.AddThemeConstantOverride("separation", 8);
		anchoredScroll.AddChild(anchoredRow);

		center.AddChild(new HSeparator { SelfModulate = GameTheme.Separator });

		var handArea = new HandArea();
		handArea.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
		center.AddChild(handArea);
		center.AddChild(new HSeparator { SelfModulate = GameTheme.Separator });

		var logShell = new PanelContainer();
		logShell.SizeFlagsVertical = Control.SizeFlags.ShrinkEnd;
		GameTheme.ApplyPanel(logShell, GameTheme.PanelElevated);
		center.AddChild(logShell);

		var logMargin = new MarginContainer();
		logMargin.AddThemeConstantOverride("margin_left", 8);
		logMargin.AddThemeConstantOverride("margin_right", 8);
		logMargin.AddThemeConstantOverride("margin_top", 6);
		logMargin.AddThemeConstantOverride("margin_bottom", 6);
		logMargin.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		logMargin.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		logShell.AddChild(logMargin);

		var logPanel = new RichTextLabel();
		logPanel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		logPanel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		logPanel.BbcodeEnabled = true;
		logPanel.ScrollFollowing = true;
		GameTheme.StyleRichLog(logPanel);
		logMargin.AddChild(logPanel);

		main.AddChild(new VSeparator { SelfModulate = GameTheme.Separator });

		var environmentArea = new EnvironmentArea();
		environmentArea.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		main.AddChild(environmentArea);

		var popupLayer = new CanvasLayer { Layer = 90, Name = "PopupLayer" };
		root.AddChild(popupLayer);

		return new GameHud
		{
			Root = root,
			StatusPanel = statusPanel,
			SceneArea = sceneArea,
			EnvironmentArea = environmentArea,
			HandArea = handArea,
			AnchoredStrip = anchoredStrip,
			AnchoredRow = anchoredRow,
			AnchoredTitleLabel = anchoredTitleLabel,
			LogShell = logShell,
			LogPanel = logPanel,
			PopupLayer = popupLayer
		};
	}
}
