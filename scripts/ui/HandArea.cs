using Godot;
using CardSurvival.Data;

namespace CardSurvival.UI;

public partial class HandArea : PanelContainer
{
    [Signal] public delegate void OnHandCardClickedEventHandler(CardNode card);
    [Signal] public delegate void OnHandCardDragEndedEventHandler(CardNode card);

    private HBoxContainer _cards = null!;

    public override void _Ready()
    {
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_top", 6);
        margin.AddThemeConstantOverride("margin_bottom", 6);
        margin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        margin.SizeFlagsVertical = SizeFlags.ExpandFill;
        AddChild(margin);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 5);
        box.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        box.SizeFlagsVertical = SizeFlags.ExpandFill;
        margin.AddChild(box);

        var label = new Label { Text = "手牌" };
        label.AddThemeFontSizeOverride("font_size", 12);
        box.AddChild(label);

        var scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        box.AddChild(scroll);

        _cards = new HBoxContainer();
        _cards.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(_cards);
    }

    public void Refresh(List<CardData> hand)
    {
        foreach (var child in _cards.GetChildren().ToArray())
            {
                _cards.RemoveChild(child);
                child.QueueFree();
            }

        foreach (var card in hand)
        {
            var node = new CardNode();
            node.Setup(card);
            node.OnCardClicked += c => EmitSignal(SignalName.OnHandCardClicked, c);
            node.OnCardDragEnded += c => EmitSignal(SignalName.OnHandCardDragEnded, c);
            _cards.AddChild(node);
        }
    }
}
