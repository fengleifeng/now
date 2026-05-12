using Godot;
using CardSurvival.Data;

namespace CardSurvival.UI;

public partial class CardActionPopup : Control
{
    public event Action<CardData, string>? OnAction;
    public event Action? OnClose;

    private CardData _card = null!;

    public void Setup(CardData card)
    {
        _card = card;
    }

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        BuildUI();
    }

    private void BuildUI()
    {
        // 遮罩 - 点击关闭
        var overlay = new ColorRect { Color = new Color(0, 0, 0, 0.62f) };
        overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        overlay.MouseFilter = MouseFilterEnum.Stop;
        overlay.GuiInput += e =>
        {
            if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
                OnClose?.Invoke();
        };
        AddChild(overlay);

        // 用 CenterContainer 确保面板居中
        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new Panel();
        panel.CustomMinimumSize = new Vector2(340, 280);
        center.AddChild(panel);

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 14);
        margin.AddThemeConstantOverride("margin_right", 14);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        panel.AddChild(margin);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 8);
        margin.AddChild(box);

        // 标题行
        var titleRow = new HBoxContainer();
        titleRow.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        box.AddChild(titleRow);

        var titleLabel = new Label { Text = _card.Name };
        titleLabel.AddThemeFontSizeOverride("font_size", 18);
        titleLabel.AddThemeColorOverride("font_color", Colors.White);
        titleLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        titleRow.AddChild(titleLabel);

        var closeBtn = new Button { Text = "✕", CustomMinimumSize = new Vector2(28, 28) };
        closeBtn.Pressed += () => OnClose?.Invoke();
        titleRow.AddChild(closeBtn);

        // 类型标签
        var typeLabel = new Label
        {
            Text = $"[{GetTypeText(_card.Type)}]  {string.Join(" ", _card.Tags.Select(t => t.ToString()))}"
        };
        typeLabel.AddThemeFontSizeOverride("font_size", 11);
        typeLabel.AddThemeColorOverride("font_color", new Color(0.72f, 0.72f, 0.72f));
        typeLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        box.AddChild(typeLabel);

        box.AddChild(new HSeparator());

        // 描述
        var descLabel = new Label { Text = _card.Description };
        descLabel.AddThemeFontSizeOverride("font_size", 13);
        descLabel.AddThemeColorOverride("font_color", new Color(0.82f, 0.82f, 0.82f));
        descLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        descLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        descLabel.CustomMinimumSize = new Vector2(0, 40);
        box.AddChild(descLabel);

        // 属性信息
        var attrText = GetAttrText();
        if (!string.IsNullOrEmpty(attrText))
        {
            var attrLabel = new Label { Text = attrText };
            attrLabel.AddThemeFontSizeOverride("font_size", 12);
            attrLabel.AddThemeColorOverride("font_color", new Color(1f, 0.86f, 0.45f));
            box.AddChild(attrLabel);
        }

        box.AddChild(new HSeparator());

        // 操作按钮
        var actionRow = new HBoxContainer();
        actionRow.AddThemeConstantOverride("separation", 8);
        actionRow.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        box.AddChild(actionRow);

        AddActionButton(actionRow, "查看", "view");
        if (_card.ThirstValue > 0)
            AddActionButton(actionRow, "饮用", "drink");
        if (_card.FoodValue > 0)
            AddActionButton(actionRow, "食用", "eat");
        if (_card.HealValue > 0 && _card.FoodValue <= 0 && _card.ThirstValue <= 0)
            AddActionButton(actionRow, "使用", "use");

        var spacer = new Control();
        spacer.SizeFlagsVertical = SizeFlags.ExpandFill;
        box.AddChild(spacer);

        // 丢弃按钮
        var bottomRow = new HBoxContainer();
        bottomRow.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        box.AddChild(bottomRow);

        var spacer2 = new Control();
        spacer2.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        bottomRow.AddChild(spacer2);

        var discardBtn = new Button { Text = "丢弃", CustomMinimumSize = new Vector2(80, 28) };
        discardBtn.AddThemeColorOverride("font_color", new Color(0.9f, 0.3f, 0.3f));
        discardBtn.Pressed += () =>
        {
            OnAction?.Invoke(_card, "discard");
            OnClose?.Invoke();
        };
        bottomRow.AddChild(discardBtn);
    }

    private void AddActionButton(HBoxContainer parent, string text, string action)
    {
        var btn = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(70, 30),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        btn.Pressed += () =>
        {
            OnAction?.Invoke(_card, action);
            OnClose?.Invoke();
        };
        parent.AddChild(btn);
    }

    private string GetAttrText()
    {
        var parts = new System.Collections.Generic.List<string>();
        if (_card.FoodValue > 0) parts.Add($"饱食 +{_card.FoodValue}");
        if (_card.ThirstValue > 0) parts.Add($"解渴 +{_card.ThirstValue}");
        if (_card.HealValue > 0) parts.Add($"治疗 +{_card.HealValue}");
        if (_card.Durability > 0) parts.Add($"耐久 {_card.Durability}");
        if (_card.BurnValue > 0) parts.Add($"燃烧 {_card.BurnValue}");
        if (_card.ToolPower > 0) parts.Add($"工具等级 {_card.ToolPower}");
        if (_card.ArmorValue > 0) parts.Add($"护甲 {_card.ArmorValue}");
        return string.Join("  ", parts);
    }

    private static string GetTypeText(CardType type) => type switch
    {
        CardType.Resource => "资源",
        CardType.Creature => "生物",
        CardType.Tool => "工具",
        CardType.Weapon => "武器",
        CardType.Building => "建筑",
        CardType.Status => "状态",
        CardType.Event => "事件",
        CardType.Location => "地点",
        CardType.Container => "容器",
        CardType.Seed => "种子",
        _ => "未知"
    };
}
