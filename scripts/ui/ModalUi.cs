using Godot;

namespace CardSurvival.UI;

/// <summary>
/// 全屏弹窗通用结构：底层半透明遮罩 + 全屏 <see cref="CenterContainer"/>，保证内容始终在视口中央。
/// </summary>
public static class ModalUi
{
	public static readonly Color DimColor = new(0, 0, 0, 0.62f);

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

	public static CenterContainer AddCenterLayer(Control modalRoot)
	{
		var center = new CenterContainer();
		center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		center.MouseFilter = Control.MouseFilterEnum.Ignore;
		modalRoot.AddChild(center);
		return center;
	}
}
