// scripts/ui/CardNode.cs
using Godot;
using CardSurvival.Data;

namespace CardSurvival.UI;

public partial class CardNode : Control
{
    public CardData Data { get; private set; } = null!;

    private ColorRect _bg = null!;
    private Label _nameLabel = null!;
    private Label _typeLabel = null!;
    private Panel _panel = null!;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(110, 70);
        MouseFilter = MouseFilterEnum.Stop;
        MouseDefaultCursorShape = CursorShape.PointingHand;

        _panel = new Panel();
        _panel.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_panel);

        _bg = new ColorRect();
        _bg.SetAnchorsPreset(LayoutPreset.FullRect);
        _panel.AddChild(_bg);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 2);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 6);
        margin.AddThemeConstantOverride("margin_right", 6);
        margin.AddThemeConstantOverride("margin_top", 6);
        margin.AddThemeConstantOverride("margin_bottom", 6);
        _panel.AddChild(margin);
        margin.AddChild(vbox);

        _nameLabel = new Label();
        _nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(_nameLabel);

        _typeLabel = new Label();
        _typeLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _typeLabel.AddThemeFontSizeOverride("font_size", 10);
        vbox.AddChild(_typeLabel);
    }

    public void Setup(CardData data)
    {
        Data = data;
        _nameLabel.Text = data.Name;
        _typeLabel.Text = $"[{data.Type}] x{data.Stack}";

        _bg.Color = data.Type switch
        {
            CardType.Resource => new Color(0.45f, 0.35f, 0.2f),
            CardType.Creature => new Color(0.25f, 0.5f, 0.25f),
            CardType.Tool => new Color(0.35f, 0.35f, 0.55f),
            CardType.Building => new Color(0.5f, 0.4f, 0.25f),
            CardType.Status => new Color(0.55f, 0.2f, 0.2f),
            CardType.Event => new Color(0.2f, 0.2f, 0.55f),
            _ => new Color(0.3f, 0.3f, 0.3f)
        };

        Name = data.Id;
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        var preview = new Label();
        preview.Text = Data.Name;
        preview.Modulate = new Color(1, 1, 1, 0.7f);
        SetDragPreview(preview);
        return this;
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        var node = data.As<CardNode>();
        return node != null && node != this;
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        var other = data.As<CardNode>();
        if (other == null || other == this) return;

        var system = GetNode<CombineSystem>("/root/CombineSystem");
        var manager = GetNode<CardManager>("/root/CardManager");

        if (system.TryCombine(Data, other.Data))
        {
            manager.RemoveCardFromHand(Data);
            manager.RemoveCardFromHand(other.Data);
            QueueFree();
            other.QueueFree();
        }
        else
        {
            GD.Print($"[CardNode] Combine failed: {Data.Name} + {other.Data.Name}");
        }
    }
}
