using Godot;

namespace CardSurvival.UI.Game;

/// <summary>
/// 游戏主界面 HUD 节点引用集合，由 <see cref="GameHudBuilder"/> 构建后交给各控制器使用。
/// </summary>
public sealed class GameHud
{
	/// <summary>根控件（GameRoot），用于响应式布局绑定。</summary>
	public required Control Root { get; init; }

	/// <summary>左侧状态栏。</summary>
	public required StatusPanel StatusPanel { get; init; }

	/// <summary>中间场景区。</summary>
	public required SceneArea SceneArea { get; init; }

	/// <summary>右侧环境物品栏。</summary>
	public required EnvironmentArea EnvironmentArea { get; init; }

	/// <summary>手牌区。</summary>
	public required HandArea HandArea { get; init; }

	/// <summary>固定展示卡（锚定条）外层面板。</summary>
	public required PanelContainer AnchoredStrip { get; init; }

	/// <summary>锚定条内卡牌横向容器。</summary>
	public required HBoxContainer AnchoredRow { get; init; }

	/// <summary>锚定条标题。</summary>
	public required Label AnchoredTitleLabel { get; init; }

	/// <summary>底部日志外层面板。</summary>
	public required PanelContainer LogShell { get; init; }

	/// <summary>底部 BBCode 日志文本。</summary>
	public required RichTextLabel LogPanel { get; init; }

	/// <summary>弹窗层（CanvasLayer）。</summary>
	public required CanvasLayer PopupLayer { get; init; }
}
