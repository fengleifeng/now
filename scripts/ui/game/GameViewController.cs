using System.Linq;
using Godot;

namespace CardSurvival.UI.Game;

/// <summary>
/// 游戏主界面「视图」职责：刷新各面板、写日志、锚定条、响应式布局、成就日志轮询。
/// 不处理弹窗与拖拽规则。
/// </summary>
public sealed class GameViewController
{
	private readonly GameServices _services;
	private readonly GameHud _hud;
	private Action<CardNode>? _onAnchoredCardClicked;
	private bool _needsRefresh;
	private bool _isRefreshing;

	public GameViewController(GameServices services, GameHud hud)
	{
		_services = services;
		_hud = hud;
	}

	/// <summary>设置锚定条卡牌点击回调（在 Input 控制器创建后注入，避免循环依赖）。</summary>
	public void SetAnchoredCardClickHandler(Action<CardNode> handler) =>
		_onAnchoredCardClicked = handler;

	/// <summary>将一条消息追加到底部日志（带当前游戏时间前缀）。</summary>
	public void AppendLog(string message)
	{
		var time = _services.Time.GetTimeDisplay();
		_hud.LogPanel.AppendText($"[color=gray]{time}[/color] {message}\n");
	}

	/// <summary>语言切换时更新锚定条标题等静态文案。</summary>
	public void OnLocaleChanged()
	{
		_hud.AnchoredTitleLabel.Text = I18n.T("ui.anchored_strip");
		RequestRefresh();
	}

	/// <summary>绑定窗口尺寸变化，调整各区域最小高度与侧栏宽度。</summary>
	public void BindResponsiveLayout()
	{
		UiLayout.BindResponsive(_hud.Root, ApplyResponsiveLayout);
	}

	/// <summary>按视口重新计算场景区、手牌、日志、侧栏尺寸。</summary>
	public void ApplyResponsiveLayout()
	{
		UiLayout.ApplyGameLayout(
			_hud.Root,
			_hud.SceneArea,
			_hud.HandArea,
			_hud.LogShell,
			_hud.AnchoredStrip,
			_hud.StatusPanel,
			_hud.EnvironmentArea);
		_hud.SceneArea.ApplyResponsiveHeights();
	}

	/// <summary>标记下一帧需要刷新（合并多次状态变更）。</summary>
	public void RequestRefresh()
	{
		if (_isRefreshing)
		{
			_needsRefresh = true;
			return;
		}
		_needsRefresh = true;
	}

	/// <summary>若标记了刷新，则执行一次全量 UI 同步。</summary>
	public void ProcessFrame()
	{
		PollAchievementLogs();
		if (!_needsRefresh)
			return;
		_needsRefresh = false;
		RefreshAll();
	}

	/// <summary>从成就系统取出待显示日志行。</summary>
	public void PollAchievementLogs()
	{
		while (_services.Achievements.TryDequeueLog(out var line))
			AppendLog(line);
	}

	/// <summary>把手牌、场景、环境、锚定条与当前地图状态同步到 UI。</summary>
	public void RefreshAll()
	{
		if (_isRefreshing)
			return;
		_isRefreshing = true;
		_needsRefresh = false;
		try
		{
			var loc = _services.Map.GetCurrentLocation();
			_hud.HandArea.Refresh(_services.Cards.GetHandForUi());
			_hud.SceneArea.Refresh(
				_services.Cards.GetSceneCards(),
				loc == null ? null : _services.Cards.GetCard(loc.Id),
				loc);
			_hud.EnvironmentArea.Refresh(_services.Map.GetCurrentEnvironmentCards());
			RefreshAnchoredStrip();
		}
		finally
		{
			_isRefreshing = false;
		}
	}

	/// <summary>根据 CardManager 中的锚定卡显示或隐藏锚定条。</summary>
	private void RefreshAnchoredStrip()
	{
		foreach (var ch in _hud.AnchoredRow.GetChildren().ToArray())
		{
			_hud.AnchoredRow.RemoveChild(ch);
			ch.QueueFree();
		}

		var card = _services.Cards.GetAnchoredCard();
		if (card == null)
		{
			_hud.AnchoredStrip.Visible = false;
			return;
		}

		_hud.AnchoredStrip.Visible = true;
		var node = new CardNode();
		node.Setup(card);
		if (_onAnchoredCardClicked != null)
			node.OnCardClicked += _onAnchoredCardClicked;
		_hud.AnchoredRow.AddChild(node);
	}
}
