using Godot;
using CardSurvival.Game;
using CardSurvival.UI.Game;

namespace CardSurvival.UI;

/// <summary>
/// 游戏主场景根节点：仅负责组装 HUD、会话与各控制器，不包含具体业务与布局细节。
/// </summary>
public partial class GameRoot : Control
{
	private GameServices _services = null!;
	private GameHud _hud = null!;
	private GameViewController _view = null!;
	private GamePopupController _popups = null!;
	private GameInputController _input = null!;
	private CardSurvivalGame _game = null!;

	/// <summary>进入场景：构建 UI、创建游戏会话、连接信号。</summary>
	public override void _Ready()
	{
		_services = GameServices.From(this);
		_hud = GameHudBuilder.Build(this);
		_view = new GameViewController(_services, _hud);

		GamePopupController? popupsRef = null;
		_game = GameSessionBootstrap.Create(_services, _view, () => popupsRef);
		_input = new GameInputController(_services, _hud, _game, _view, () => popupsRef!);
		popupsRef = new GamePopupController(_services, _hud, _game, _view, _input.HandleCardAction);
		_popups = popupsRef;
		_view.SetAnchoredCardClickHandler(_input.OnAnchoredCardClicked);

		GamePanelWiring.Connect(_services, _hud, _game, _view, _input, _popups);
		_ = new CombineSignalBridge(_services, _game, _view);

		_view.BindResponsiveLayout();
		I18n.LocaleChanged += _view.OnLocaleChanged;
		GameSessionBootstrap.StartOrLoad(_services, _game);
		_view.RefreshAll();
	}

	/// <summary>离开场景：取消语言切换订阅。</summary>
	public override void _ExitTree()
	{
		I18n.LocaleChanged -= _view.OnLocaleChanged;
		base._ExitTree();
	}

	/// <summary>每帧：刷新队列与成就日志。</summary>
	public override void _Process(double delta)
	{
		_view.ProcessFrame();
	}
}
