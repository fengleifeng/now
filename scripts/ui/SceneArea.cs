using Godot;
using CardSurvival.Data;
using System.Collections.Generic;
using CardSurvival;
using CardSurvival.Game;

namespace CardSurvival.UI;

public partial class SceneArea : PanelContainer
{
    [Signal] public delegate void OnSceneCardClickedEventHandler(CardNode card);
    [Signal] public delegate void OnSceneCardDragEndedEventHandler(CardNode card);
    [Signal] public delegate void OnLocationExploreClickedEventHandler();
    [Signal] public delegate void OnLocationMoveClickedEventHandler(string locationId);

    private HBoxContainer _immovableCards = null!; // 地点、永久建筑（不可移动）
    private HBoxContainer _movableCards = null!;   // 可拾取物品
    private ScrollContainer _immovableScroll = null!;
    private MapSystem _map = null!;
    private TimeSystem _time = null!;
    private PlayerSystem _player = null!;
    private Label _sceneSectionLabel = null!;
    private Label _itemsSectionLabel = null!;

    public override void _Ready()
    {
        var services = GameServices.From(this);
        _map = services.Map;
        _time = services.Time;
        _player = services.Player;

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
        _sceneSectionLabel = new Label();
        GameTheme.StyleSectionLabel(_sceneSectionLabel, GameTheme.AccentScene);
        box.AddChild(_sceneSectionLabel);

        _immovableScroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
            ClipContents = true
        };
        box.AddChild(_immovableScroll);

        _immovableCards = new HBoxContainer();
        _immovableCards.AddThemeConstantOverride("separation", 8);
        _immovableCards.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _immovableScroll.AddChild(_immovableCards);

        box.AddChild(new HSeparator { SelfModulate = GameTheme.Separator });

        // === 第二行：可拾取物品 ===
        _itemsSectionLabel = new Label();
        GameTheme.StyleSectionLabel(_itemsSectionLabel, GameTheme.AccentScene);
        box.AddChild(_itemsSectionLabel);

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

        ApplySectionLabels();
        I18n.LocaleChanged += ApplySectionLabels;
    }

    public override void _ExitTree()
    {
        I18n.LocaleChanged -= ApplySectionLabels;
        base._ExitTree();
    }

    private void ApplySectionLabels()
    {
        _sceneSectionLabel.Text = I18n.T("ui.scene");
        _itemsSectionLabel.Text = I18n.T("ui.scene_items");
    }

    public void ApplyResponsiveHeights()
    {
        _immovableScroll.CustomMinimumSize = new Vector2(0, UiLayout.Scaled(this, 172));
    }

    /// <summary>
    /// 刷新场景卡牌。locationCard = 当前地点卡，sceneCards = 场景中的物品卡。
    /// mapLocation 与当前地点卡对应时，在地点卡下方显示探索/前往（原地点信息栏功能）。
    /// 自动分离不可移动卡牌（Location/Permanent）和可拾取卡牌。
    /// </summary>
    public void Refresh(List<CardData> sceneCards, CardData? locationCard = null, LocationData? mapLocation = null)
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

        // 渲染不可移动行：当前地点卡带操作区，其余仅展示
        foreach (var card in immovable)
        {
            var isCurrentLocation = mapLocation != null
                && card.Type == CardType.Location
                && card.Id == mapLocation.Id;
            if (isCurrentLocation)
                AddLocationCardWithActions(_immovableCards, card, mapLocation!);
            else
                AddCard(_immovableCards, card, false);
        }

        // 渲染可移动行（可交互）
        foreach (var card in movable)
            AddCard(_movableCards, card, true);
    }

    private void AddLocationCardWithActions(HBoxContainer parent, CardData card, LocationData meta)
    {
        var col = new VBoxContainer();
        col.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        col.SizeFlagsVertical = SizeFlags.ShrinkBegin;
        col.AddThemeConstantOverride("separation", 6);

        var title = new Label();
        title.Text = string.IsNullOrEmpty(meta.Icon) ? meta.Name : $"{meta.Icon} {meta.Name}";
        title.AddThemeFontSizeOverride("font_size", 14);
        title.AddThemeColorOverride("font_color", GameTheme.AccentRegion);
        title.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        col.AddChild(title);

        var node = new CardNode();
        node.Setup(card);
        col.AddChild(node);

        var desc = new Label
        {
            Text = meta.Description,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        desc.CustomMinimumSize = new Vector2(220, 0);
        desc.AddThemeFontSizeOverride("font_size", 11);
        desc.AddThemeColorOverride("font_color", GameTheme.TextMuted);
        desc.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        col.AddChild(desc);

        var exploreCost = SurvivalReadModel.GetExploreEnergyCost(
            GameServices.From(this), meta);
        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 8);
        actions.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;

        var explore = new Button { Text = I18n.Tf("ui.explore_energy", exploreCost), CustomMinimumSize = new Vector2(128, 32) };
        GameTheme.StyleExploreButton(explore);
        explore.Pressed += () => EmitSignal(SignalName.OnLocationExploreClicked);
        actions.AddChild(explore);

        foreach (var connectionId in meta.Connections)
        {
            var id = connectionId;
            var name = _map.GetLocation(id)?.Name ?? id;
            var move = new Button { Text = I18n.Tf("ui.go_to", name), CustomMinimumSize = new Vector2(118, 32) };
            GameTheme.StyleMoveButton(move);
            move.Pressed += () => EmitSignal(SignalName.OnLocationMoveClicked, id);
            actions.AddChild(move);
        }

        col.AddChild(actions);
        parent.AddChild(col);
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

    /// <summary>场景手牌互动成功时的轻量「舞动」反馈。</summary>
    public void PlayDancePulse()
    {
        var tw = CreateTween();
        var baseMod = Modulate;
        tw.TweenProperty(this, "modulate", baseMod * new Color(1.12f, 1.18f, 1.08f, 1f), 0.09f).SetTrans(Tween.TransitionType.Sine);
        tw.TweenProperty(this, "modulate", baseMod * new Color(0.94f, 0.96f, 1.04f, 1f), 0.1f).SetTrans(Tween.TransitionType.Sine);
        tw.TweenProperty(this, "modulate", baseMod, 0.14f).SetTrans(Tween.TransitionType.Quad);
    }
}
