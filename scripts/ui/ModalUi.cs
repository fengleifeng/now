using Godot;

namespace CardSurvival.UI;

/// <summary>
/// 全屏模态框通用外壳：半透明遮罩 + 居中容器，与具体弹窗内容解耦。
/// </summary>
public static class ModalUi
{
	/// <summary>默认遮罩颜色（半透明黑）。</summary>
	public static readonly Color DimColor = new(0, 0, 0, 0.62f);

	/// <summary>
	/// 添加可点击的暗色全屏遮罩；左键点击时调用 <paramref name="onClickBackground"/>。
	/// </summary>
	public static ColorRect AddDimOverlay(Control modalRoot, System.Action onClickBackground)
	{
		var overlay = new ColorRect { Color = DimColor };
		overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		overlay.MouseFilter = Control.MouseFilterEnum.Stop;
		overlay.GuiInput += e =>
		{
			if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
				onClickBackground();
		};
		modalRoot.AddChild(overlay);
		return overlay;
	}

	/// <summary>添加全屏居中容器，用于放置弹窗面板。</summary>
	public static CenterContainer AddCenterLayer(Control modalRoot)
	{
		var center = new CenterContainer();
		center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		center.MouseFilter = Control.MouseFilterEnum.Ignore;
		modalRoot.AddChild(center);
		return center;
	}
}
