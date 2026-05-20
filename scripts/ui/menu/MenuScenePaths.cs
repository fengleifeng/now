using Godot;

namespace CardSurvival.UI.Menu;

/// <summary>
/// 主菜单与特质页场景树节点路径解析，兼容旧版 .tscn 结构。
/// </summary>
public static class MenuScenePaths
{
	/// <summary>解析主菜单内容 VBox（含标题与按钮）。</summary>
	public static VBoxContainer ResolveMainMenuVBox(Node root)
	{
		if (root.HasNode("CenterRoot/SafeMargin/Center/MenuPanel/MainContainer"))
			return root.GetNode<VBoxContainer>("CenterRoot/SafeMargin/Center/MenuPanel/MainContainer");
		if (root.HasNode("CenterRoot/MenuPanel/MainContainer"))
			return root.GetNode<VBoxContainer>("CenterRoot/MenuPanel/MainContainer");
		return root.GetNode<VBoxContainer>("CenterRoot/MainContainer");
	}

	/// <summary>尝试获取主菜单外层面板。</summary>
	public static bool TryGetMainMenuPanel(Node root, out PanelContainer panel)
	{
		foreach (var path in new[] { "CenterRoot/SafeMargin/Center/MenuPanel", "CenterRoot/MenuPanel" })
		{
			if (root.HasNode(path) && root.GetNode(path) is PanelContainer p)
			{
				panel = p;
				return true;
			}
		}

		panel = null!;
		return false;
	}

	/// <summary>解析特质页主内容 VBox。</summary>
	public static VBoxContainer ResolveCharacterMainVBox(Node root)
	{
		if (root.HasNode("CenterRoot/SafeMargin/Center/MenuPanel/Scroll/MainContainer"))
			return root.GetNode<VBoxContainer>("CenterRoot/SafeMargin/Center/MenuPanel/Scroll/MainContainer");
		return root.GetNode<VBoxContainer>("CenterRoot/MenuPanel/MainContainer");
	}

	/// <summary>解析特质页外层面板。</summary>
	public static PanelContainer ResolveCharacterMenuPanel(Node root) =>
		root.HasNode("CenterRoot/SafeMargin/Center/MenuPanel")
			? root.GetNode<PanelContainer>("CenterRoot/SafeMargin/Center/MenuPanel")
			: root.GetNode<PanelContainer>("CenterRoot/MenuPanel");
}
