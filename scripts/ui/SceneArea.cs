using Godot;
using CardSurvival.Data;
using System.Collections.Generic;

namespace CardSurvival.UI;

public partial class SceneArea : PanelContainer
{
    [Signal] public delegate void OnSceneCardClickedEventHandler(CardNode card);
    [Signal] public delegate void OnSceneCardDragEndedEventHandler(CardNode card);

    private HBoxContainer _immovableCards = null!; // 地点、永久建筑（不可移动）
    private HBoxContainer _movableCards = null!;   // 可拾取物品

    public override void _Ready()
    {
        GameTheme.ApplyPanelSoft(this, GameTheme.PanelMain);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_top", 6);
        margin.AddThemeConstantOverride("margin_bottom", 6);
        margin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        margin.SizeFlagsVertical = SizeFlags.ExpandFill;
        AddChild(margin);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 4);
        box.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        box.SizeFlagsVertical = SizeFlags.ExpandFill;
        margin.AddChild(box);

        // === 第一行：不可移动卡牌（地点、永久建筑） ===
        var immovableLabel = new Label { Text = "场景" };
        GameTheme.StyleSectionLabel(immovableLabel, GameTheme.AccentScene);
        box.AddChild(immovableLabel);

        var immovableScroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
            CustomMinimumSize = new Vector2(0, 108),
            ClipContents = true
        };
        box.AddChild(immovableScroll);

        _immovableCards = new HBoxContainer();
        _immovableCards.AddThemeConstantOverride("separation", 8);
        _immovableCards.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        immovableScroll.AddChild(_immovableCards);

        box.AddChild(new HSeparator { SelfModulate = GameTheme.Separator });

        // === 第二行：可拾取物品 ===
        var movableLabel = new Label { Text = "物品" };
        GameTheme.StyleSectionLabel(movableLabel, GameTheme.AccentScene);
        box.AddChild(movableLabel);

        var movableScroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            ClipContents = true
        };
        box.AddChild(movableScroll);

        _movableCards = new HBoxContainer();
        _movableCards.AddThemeConstantOverride("separation", 8);
        _movableCards.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        movableScroll.AddChild(_movableCards);
    }

    /// <summary>
    /// 刷新场景卡牌。locationCard = 当前地点卡，sceneCards = 场景中的物品卡。
    /// 自动分离不可移动卡牌（Location/Permanent）和可拾取卡牌。
    /// </summary>
    public void Refresh(List<CardData> sceneCards, CardData? locationCard = null)
    {
        // 清空
        ClearChildren(_immovableCards);
        ClearChildren(_movableCards);

        // 收集所有卡牌
        var allCards = new List<CardData>();
        if (locationCard != null)
            allCards.Add(locationCard);
        allCards.AddRange(sceneCards);

        // 分离不可移动和可移动
        var immovable = new List<CardData>();
        var movable = new List<CardData>();

        foreach (var card in allCards)
        {
            var isImmovable = card.Type == CardType.Location
                || card.Tags.Contains(CardTag.Permanent);
            if (isImmovable)
                immovable.Add(card);
            else
                movable.Add(card);
        }

        // 渲染不可移动行（非交互）
        foreach (var card in immovable)
            AddCard(_immovableCards, card, false);

        // 渲染可移动行（可交互）
        foreach (var card in movable)
            AddCard(_movableCards, card, true);
    }

    private void AddCard(HBoxContainer container, CardData card, bool interactive)
    {
        var node = new CardNode();
        node.Setup(card);
        if (interactive)
        {
            node.OnCardClicked += c => EmitSignal(SignalName.OnSceneCardClicked, c);
            node.OnCardDragEnded += c => EmitSignal(SignalName.OnSceneCardDragEnded, c);
        }
        container.AddChild(node);
    }

    private static void ClearChildren(Node node)
    {
        foreach (var child in node.GetChildren().ToArray())
        {
            node.RemoveChild(child);
            child.QueueFree();
        }
    }
}
