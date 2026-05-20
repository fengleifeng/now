using System;
using Godot;

namespace CardSurvival.UI;

/// <summary>
/// 视口自适应工具：以 1280×720 为设计基准，按窗口大小缩放控件并限制弹窗不超出屏幕。
/// </summary>
public static class UiLayout
{
	/// <summary>UI 设计稿参考分辨率。</summary>
	public static readonly Vector2 DesignSize = new(1280, 720);

	/// <summary>获取当前视口像素尺寸。</summary>
	public static Vector2 ViewportSize(Node from) => from.GetViewportRect().Size;

	/// <summary>
	/// 计算内容缩放系数：取宽高相对设计稿比例的较小值，限制在 0.55～1.0。
	/// </summary>
	public static float ContentScale(Node from)
	{
		var vp = ViewportSize(from);
		if (vp.X < 8f || vp.Y < 8f)
			return 1f;
		return Mathf.Clamp(Mathf.Min(vp.X / DesignSize.X, vp.Y / DesignSize.Y), 0.55f, 1f);
	}

	/// <summary>将设计稿像素换算为当前视口下的整型像素。</summary>
	public static int Scaled(Node from, float designPixels) =>
		Mathf.Max(1, Mathf.RoundToInt(designPixels * ContentScale(from)));

	/// <summary>将设计稿二维尺寸换算为当前视口尺寸。</summary>
	public static Vector2 ScaledSize(Node from, Vector2 design) =>
		new(Scaled(from, design.X), Scaled(from, design.Y));

	/// <summary>
	/// 设置面板最小尺寸：先按设计尺寸缩放，再限制不超过视口减去边距。
	/// </summary>
	public static void ClampPanelMinSize(Control panel, Vector2 designSize, float margin = 20f)
	{
		var vp = ViewportSize(panel);
		var maxW = Mathf.Max(96f, vp.X - margin * 2f);
		var maxH = Mathf.Max(96f, vp.Y - margin * 2f);
		var scaled = ScaledSize(panel, designSize);
		panel.CustomMinimumSize = new Vector2(
			Mathf.Min(scaled.X, maxW),
			Mathf.Min(scaled.Y, maxH));
	}

	/// <summary>
	/// 在控件尺寸变化时重复调用布局回调（立即执行一次）。
	/// </summary>
	public static void BindResponsive(Control root, Action onLayout)
	{
		if (!root.IsConnected(Control.SignalName.Resized, Callable.From(onLayout)))
			root.Resized += onLayout;
		onLayout();
	}

	/// <summary>
	/// 游戏主界面三区（场景/手牌/日志）与左右侧栏的响应式最小尺寸与可见性。
	/// </summary>
	public static void ApplyGameLayout(
		Node from,
		Control sceneArea,
		Control handArea,
		Control logShell,
		Control? anchoredStrip,
		Control statusPanel,
		Control environmentArea)
	{
		var vp = ViewportSize(from);

		sceneArea.CustomMinimumSize = new Vector2(0, Scaled(from, 288));
		handArea.CustomMinimumSize = new Vector2(0, Scaled(from, 168));
		logShell.CustomMinimumSize = new Vector2(0, Scaled(from, 86));
		if (anchoredStrip != null)
			anchoredStrip.CustomMinimumSize = new Vector2(0, Scaled(from, 108));

		var sideW = Mathf.Clamp((int)(vp.X * 0.155f), Scaled(from, 148), Scaled(from, 220));
		statusPanel.CustomMinimumSize = new Vector2(sideW, 0);
		environmentArea.CustomMinimumSize = new Vector2(sideW, 0);

		environmentArea.Visible = vp.X >= 820f;
		statusPanel.Visible = vp.X >= 520f;
	}

	/// <summary>特质网格：窄屏单列，否则双列。</summary>
	public static void ApplyTraitGrid(GridContainer grid, Node from) =>
		grid.Columns = ViewportSize(from).X < 560f ? 1 : 2;
}
