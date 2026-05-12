using Godot;

namespace CardSurvival.UI;

public partial class MenuPopup : Control
{
    public event Action? OnSave;
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
        var overlay = new ColorRect { Color = new Color(0, 0, 0, 0.62f) };
        overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        overlay.MouseFilter = MouseFilterEnum.Stop;
        overlay.GuiInput += e =>
        {
            if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
                OnClose?.Invoke();
        };
        AddChild(overlay);

        var panel = new Panel
        {
            AnchorLeft = 0.5f,
            AnchorRight = 0.5f,
            AnchorTop = 0.5f,
            AnchorBottom = 0.5f,
            OffsetLeft = -120,
            OffsetRight = 120,
            OffsetTop = -100,
            OffsetBottom = 100
        };
        AddChild(panel);

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        panel.AddChild(margin);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 8);
        margin.AddChild(box);

        var title = new Label { Text = "菜单", HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 16);
        box.AddChild(title);

        box.AddChild(MakeButton("保存游戏", () => OnSave?.Invoke()));
        box.AddChild(MakeButton("返回主菜单", () => OnQuit?.Invoke()));
        box.AddChild(MakeButton("关闭", () => OnClose?.Invoke()));
    }

    private static Button MakeButton(string text, Action onPressed)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(0, 32) };
        button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        button.Pressed += onPressed;
        return button;
    }
}
