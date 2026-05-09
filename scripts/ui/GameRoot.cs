// scripts/ui/GameRoot.cs
using Godot;
using CardSurvival.UI;

namespace CardSurvival;

public partial class GameRoot : Control
{
    private HandArea _handArea = null!;
    private TableArea _tableArea = null!;
    private StatusPanel _statusPanel = null!;
    private Button _drawButton = null!;
    private CardManager _cardManager = null!;
    private CombineSystem _combineSystem = null!;

    public override void _Ready()
    {
        _cardManager = GetNode<CardManager>("/root/CardManager");
        _combineSystem = GetNode<CombineSystem>("/root/CombineSystem");

        SetupUI();

        _cardManager.DrawInitialHand(5);
        RefreshHand();

        ConnectSignals();
    }

    private void SetupUI()
    {
        // Background
        var bg = new ColorRect();
        bg.Color = new Color(0.12f, 0.12f, 0.15f);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 20);
        margin.AddThemeConstantOverride("margin_right", 20);
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddThemeConstantOverride("margin_bottom", 20);
        AddChild(margin);

        var mainVBox = new VBoxContainer();
        mainVBox.AddThemeConstantOverride("separation", 12);
        margin.AddChild(mainVBox);

        // Status bar
        _statusPanel = new StatusPanel();
        mainVBox.AddChild(_statusPanel);

        var separator1 = new HSeparator();
        mainVBox.AddChild(separator1);

        // Table area (card play zone)
        var tableLabel = new Label();
        tableLabel.Text = "== Table ==";
        tableLabel.HorizontalAlignment = HorizontalAlignment.Center;
        tableLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
        mainVBox.AddChild(tableLabel);

        _tableArea = new TableArea();
        _tableArea.CustomMinimumSize = new Vector2(0, 200);
        _tableArea.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _tableArea.SizeFlagsVertical = SizeFlags.ExpandFill;
        mainVBox.AddChild(_tableArea);

        var separator2 = new HSeparator();
        mainVBox.AddChild(separator2);

        // Hand area
        var handLabel = new Label();
        handLabel.Text = "== Hand ==";
        handLabel.HorizontalAlignment = HorizontalAlignment.Center;
        handLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
        mainVBox.AddChild(handLabel);

        _handArea = new HandArea();
        mainVBox.AddChild(_handArea);

        // Draw button
        _drawButton = new Button();
        _drawButton.Text = "Draw";
        _drawButton.Pressed += OnDrawPressed;
        mainVBox.AddChild(_drawButton);
    }

    private void ConnectSignals()
    {
        _cardManager.OnCardAdded += OnCardChanged;
        _cardManager.OnCardRemoved += OnCardChanged;

        _combineSystem.OnCombineSuccess += OnCombineSuccess;
        _combineSystem.OnCombineFail += OnCombineFail;
    }

    private void OnCardChanged(string cardId)
    {
        GD.Print($"[GameRoot] Card changed: {cardId}");
        RefreshHand();
    }

    private void OnDrawPressed()
    {
        _cardManager.DrawCard();
    }

    private void OnCombineSuccess(string a, string b, string[] results)
    {
        GD.Print($"[GameRoot] COMBINE SUCCESS: {a} + {b} -> {string.Join(", ", results)}");
        foreach (var id in results)
        {
            var card = _cardManager.GetCard(id);
            if (card != null)
                _cardManager.AddCardToHand(card);
        }
    }

    private void OnCombineFail(string a, string b)
    {
        GD.Print($"[GameRoot] COMBINE FAILED: {a} + {b}");
    }

    private void RefreshHand()
    {
        _handArea.Refresh(_cardManager.GetHand());
    }
}
