using System.Linq;
using Godot;
using CardSurvival.Data;

namespace CardSurvival.UI;

public partial class EnvironmentArea : PanelContainer
{
    private VBoxContainer _items = null!;

    public override void _Ready()
    {
        GameTheme.ApplyPanelSoft(this, GameTheme.PanelMain);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        margin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        margin.SizeFlagsVertical = SizeFlags.ExpandFill;
        AddChild(margin);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 6);
        box.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        box.SizeFlagsVertical = SizeFlags.ExpandFill;
        margin.AddChild(box);

        var title = new Label { Text = "环境" };
        GameTheme.StyleSectionLabel(title, GameTheme.TextMuted);
        box.AddChild(title);

        var scroll = new ScrollContainer
        {
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            ClipContents = true
        };
        box.AddChild(scroll);

        _items = new VBoxContainer();
        _items.AddThemeConstantOverride("separation", 6);
        _items.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(_items);
    }

    public void Refresh(List<CardData> environmentCards)
    {
        foreach (var child in _items.GetChildren())
            child.QueueFree();

        foreach (var group in environmentCards.GroupBy(c => c.Id))
            _items.AddChild(CreateRow(group.First(), group.Count()));
    }

    private static Control CreateRow(CardData card, int count)
    {
        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(0, 42);
        panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_top", 4);
        margin.AddThemeConstantOverride("margin_bottom", 4);
        panel.AddChild(margin);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);
        margin.AddChild(row);

        var color = new ColorRect
        {
            Color = GetTypeColor(card.Type),
            CustomMinimumSize = new Vector2(8, 0)
        };
        row.AddChild(color);

        var text = new Label();
        text.Text = count > 1 ? $"{card.Name} x{count}\n{GetTypeText(card.Type)}" : $"{card.Name}\n{GetTypeText(card.Type)}";
        text.AddThemeFontSizeOverride("font_size", 11);
        text.AddThemeColorOverride("font_color", GameTheme.TextPrimary);
        text.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(text);

        return panel;
    }

    private static string GetTypeText(CardType type) => type switch
    {
        CardType.Resource => "资源",
        CardType.Creature => "生物",
        CardType.Tool => "工具",
        CardType.Building => "建筑",
        CardType.Status => "状态",
        CardType.Event => "事件",
        CardType.Location => "地点",
        _ => "未知"
    };

    private static Color GetTypeColor(CardType type) => type switch
    {
        CardType.Resource => new Color(0.42f, 0.33f, 0.2f),
        CardType.Creature => new Color(0.25f, 0.45f, 0.24f),
        CardType.Tool => new Color(0.32f, 0.34f, 0.55f),
        CardType.Building => new Color(0.48f, 0.38f, 0.23f),
        CardType.Status => new Color(0.55f, 0.2f, 0.2f),
        CardType.Event => new Color(0.2f, 0.25f, 0.5f),
        CardType.Location => new Color(0.2f, 0.43f, 0.34f),
        _ => new Color(0.3f, 0.3f, 0.3f)
    };
}
