using Godot;
using CardSurvival.Data;
using System.Collections.Generic;
using System.Linq;

namespace CardSurvival;

public partial class CardManager : Node
{
    [Signal] public delegate void OnCardAddedEventHandler(string cardId);
    [Signal] public delegate void OnCardRemovedEventHandler(string cardId);
    [Signal] public delegate void OnCardConsumedEventHandler(string cardId, string reason);
    [Signal] public delegate void OnSceneCardsChangedEventHandler();

    private readonly Dictionary<string, CardData> _cardDefs = new();
    private readonly Dictionary<string, ProjectData> _projectDefs = new();
    private readonly List<CardData> _hand = new();
    private readonly List<CardData> _tableCards = new();
    private readonly Random _random = new();
    private const int MaxHandSlots = 8; // 最大手牌槽位数（堆叠算1槽）
    private MapSystem? _mapSystem;

    // Cache of drawable card IDs to avoid filtering every call
    private List<string>? _cachedDrawableIds;

    public override void _Ready()
    {
        var cardsPath = ProjectSettings.GlobalizePath("res://data/cards.json");
        var cards = DataLoader.LoadCards(cardsPath);
        foreach (var c in cards)
            _cardDefs[c.Id] = c;

        var projectsPath = ProjectSettings.GlobalizePath("res://data/projects.json");
        var projects = DataLoader.LoadProjects(projectsPath);
        foreach (var p in projects)
            _projectDefs[p.Id] = p;

        BuildDrawableCache();

        GD.Print($"[CardManager] Loaded {_cardDefs.Count} card definitions, {_projectDefs.Count} project definitions");
    }

    public void BuildDrawableCache()
    {
        _cachedDrawableIds = _cardDefs
            .Where(kv => kv.Value.Type != CardType.Location
                && kv.Value.Type != CardType.Event
                && !kv.Value.Tags.Contains(CardTag.Nature))
            .Select(kv => kv.Key)
            .ToList();
    }

    private MapSystem GetMapSystem()
    {
        if (_mapSystem == null)
            _mapSystem = GetNode<MapSystem>("/root/MapSystem");
        return _mapSystem;
    }

    public CardData? GetCard(string id) =>
        _cardDefs.TryGetValue(id, out var card) ? card : null;

    public bool HasCard(string id) => _cardDefs.ContainsKey(id);

    public ProjectData? GetProject(string id) =>
        _projectDefs.TryGetValue(id, out var proj) ? proj : null;

    public IEnumerable<ProjectData> GetAllProjects() => _projectDefs.Values;

    public CardData? CreateCardInstance(string id)
    {
        if (!_cardDefs.TryGetValue(id, out var template))
            return null;
        return CloneCard(template);
    }

    // ============================================================
    //  抽卡
    // ============================================================

    public CardData? DrawCard()
    {
        if (_cachedDrawableIds == null || _cachedDrawableIds.Count == 0)
            BuildDrawableCache();
        if (_cachedDrawableIds!.Count == 0) return null;
        var id = _cachedDrawableIds[_random.Next(_cachedDrawableIds.Count)];
        var template = _cardDefs[id];
        var instance = CloneCard(template);
        GetMapSystem().AddCardToCurrentScene(instance);
        EmitSignal(SignalName.OnSceneCardsChanged);
        return instance;
    }

    public CardData? DrawCardFromLocationToHand(string locationId)
    {
        var location = GetMapSystem().GetLocation(locationId);
        if (location == null || location.ExplorePool.Count == 0)
            return DrawCard();

        var id = location.ExplorePool[_random.Next(location.ExplorePool.Count)];
        var template = _cardDefs.GetValueOrDefault(id);
        if (template == null)
            return DrawCard();

        var instance = CloneCard(template);
        GetMapSystem().AddCardToCurrentScene(instance);
        EmitSignal(SignalName.OnSceneCardsChanged);
        return instance;
    }

    public CardData? ExploreLocation(string locationId)
    {
        var location = GetMapSystem().GetLocation(locationId);
        if (location == null || location.ExplorePool.Count == 0)
            return DrawCard();

        var id = location.ExplorePool[_random.Next(location.ExplorePool.Count)];
        var template = _cardDefs.GetValueOrDefault(id);
        if (template == null)
            return DrawCard();

        var instance = CloneCard(template);
        GetMapSystem().AddCardToCurrentScene(instance);
        EmitSignal(SignalName.OnSceneCardsChanged);
        return instance;
    }

    // ============================================================
    //  手牌操作（堆叠感知）
    // ============================================================

    /// <summary> 手牌实际占用槽位数（堆叠算1槽） </summary>
    public int GetHandSlotCount() => _hand.Count;

    /// <summary> 获取手牌原始数据（已堆叠） </summary>
    public List<CardData> GetHand() => _hand;

    public void AddCardToHand(CardData card)
    {
        AddToHandWithLimit(card);
        EmitSignal(SignalName.OnCardAdded, card.Id);
    }

    /// <summary>
    /// 从手牌移除一张卡。堆叠卡只减数量，数量归零才移除。
    /// </summary>
    public void RemoveCardFromHand(CardData card)
    {
        var existing = _hand.FirstOrDefault(c => c.Id == card.Id);
        if (existing == null) return;

        if (existing.Stack > 1)
        {
            existing.Stack--;
        }
        else
        {
            _hand.Remove(existing);
        }
        EmitSignal(SignalName.OnCardRemoved, card.Id);
    }

    /// <summary>
    /// 消耗一张卡（移除并触发消耗信号）。
    /// </summary>
    public void ConsumeCardFromHand(CardData card, string reason)
    {
        var existing = _hand.FirstOrDefault(c => c.Id == card.Id);
        if (existing == null) return;

        if (existing.Stack > 1)
        {
            existing.Stack--;
        }
        else
        {
            _hand.Remove(existing);
        }
        EmitSignal(SignalName.OnCardConsumed, card.Id, reason);
    }

    /// <summary>
    /// 检查手牌中是否有指定ID的卡（堆叠卡也返回true）
    /// </summary>
    public bool HasCardInHand(string id) => _hand.Any(c => c.Id == id);

    /// <summary>
    /// 获取手牌中某张卡的总堆叠数
    /// </summary>
    public int GetCardStackInHand(string id)
    {
        var card = _hand.FirstOrDefault(c => c.Id == id);
        return card?.Stack ?? 0;
    }

    public void DrawInitialHand(int count)
    {
        for (int i = 0; i < count; i++)
            DrawCardFromLocationToHand(GetMapSystem().CurrentLocation);
    }

    // ============================================================
    //  桌面（保留但未深度使用）
    // ============================================================

    public void AddCardToTable(CardData card)
    {
        _tableCards.Add(card);
        EmitSignal(SignalName.OnCardAdded, card.Id);
    }

    public void RemoveCardFromTable(CardData card)
    {
        _tableCards.Remove(card);
        EmitSignal(SignalName.OnCardRemoved, card.Id);
    }

    public List<CardData> GetTableCards() => _tableCards;

    // ============================================================
    //  场景卡牌
    // ============================================================

    public List<CardData> GetSceneCards()
    {
        return GetMapSystem().GetCurrentSceneCards();
    }

    public void AddCardToScene(CardData card)
    {
        GetMapSystem().AddCardToCurrentScene(card);
        EmitSignal(SignalName.OnSceneCardsChanged);
    }

    public void RemoveCardFromScene(CardData card)
    {
        GetMapSystem().RemoveCardFromCurrentScene(card);
        EmitSignal(SignalName.OnSceneCardsChanged);
    }

    public void MoveSceneCardToHand(CardData card)
    {
        GetMapSystem().RemoveCardFromCurrentScene(card);
        AddToHandWithLimit(card);
        EmitSignal(SignalName.OnSceneCardsChanged);
        EmitSignal(SignalName.OnCardAdded, card.Id);
    }

    public void MoveHandCardToScene(CardData card)
    {
        // 堆叠处理：从堆叠中取出1个放场景
        var existing = _hand.FirstOrDefault(c => c.Id == card.Id);
        if (existing == null) return;

        var sceneInstance = CloneCard(_cardDefs[card.Id]);
        if (existing.Stack > 1)
        {
            existing.Stack--;
        }
        else
        {
            _hand.Remove(existing);
        }
        GetMapSystem().AddCardToCurrentScene(sceneInstance);
        EmitSignal(SignalName.OnCardRemoved, card.Id);
        EmitSignal(SignalName.OnSceneCardsChanged);
    }

    // ============================================================
    //  搜索与查询
    // ============================================================

    public CardData? FindHandCardByTag(CardTag tag)
    {
        return _hand.FirstOrDefault(c => c.Tags.Contains(tag));
    }

    public List<CardData> GetHandCardsByType(CardType type)
    {
        return _hand.Where(c => c.Type == type).ToList();
    }

    // ============================================================
    //  耐久
    // ============================================================

    public void TickDurability()
    {
        var handToRemove = new List<CardData>();
        foreach (var card in _hand)
        {
            if (card.Durability > 0)
            {
                card.Durability--;
                if (card.Durability <= 0)
                    handToRemove.Add(card);
            }
        }
        foreach (var card in handToRemove)
            ConsumeCardFromHand(card, "durability");

        var currentScene = GetMapSystem().GetCurrentSceneCards();
        var sceneToRemove = new List<CardData>();
        foreach (var card in currentScene)
        {
            if (card.Durability > 0)
            {
                card.Durability--;
                if (card.Durability <= 0)
                    sceneToRemove.Add(card);
            }
        }
        foreach (var card in sceneToRemove)
        {
            GetMapSystem().RemoveCardFromCurrentScene(card);
            EmitSignal(SignalName.OnCardConsumed, card.Id, "durability");
        }
        if (sceneToRemove.Count > 0)
            EmitSignal(SignalName.OnSceneCardsChanged);
    }

    public void ClearRuntimeCards()
    {
        _hand.Clear();
        _tableCards.Clear();
        EmitSignal(SignalName.OnCardRemoved, "");
    }

    // ============================================================
    //  内部方法
    // ============================================================

    /// <summary>
    /// 将卡加入手牌，同ID自动堆叠。
    /// 超过手牌槽位上限时丢弃最早的非堆叠槽位。
    /// </summary>
    private void AddToHandWithLimit(CardData card)
    {
        // 尝试堆叠到现有卡上
        var existing = _hand.FirstOrDefault(c => c.Id == card.Id && c.Stack < c.MaxStack);
        if (existing != null)
        {
            var space = existing.MaxStack - existing.Stack;
            var toAdd = card.Stack;
            if (toAdd <= space)
            {
                existing.Stack += toAdd;
                return;
            }
            else
            {
                existing.Stack = existing.MaxStack;
                card.Stack = toAdd - space; // 剩余部分，继续添加
            }
        }

        // 没有可堆叠的槽位 → 放入新槽位
        _hand.Add(card);

        // 超限丢弃
        while (_hand.Count > MaxHandSlots)
        {
            var discarded = _hand[0];
            _hand.RemoveAt(0);
            EmitSignal(SignalName.OnCardConsumed, discarded.Id, "hand_limit");
        }
    }

    private static CardData CloneCard(CardData src) => new()
    {
        Id = src.Id,
        Name = src.Name,
        Type = src.Type,
        Stack = 1, // 新实例初始为1
        MaxStack = src.MaxStack,
        Tags = new List<CardTag>(src.Tags),
        Durability = src.Durability,
        Weight = src.Weight,
        Description = src.Description,
        IsDraggable = src.IsDraggable,
        FoodValue = src.FoodValue,
        HealValue = src.HealValue,
        ThirstValue = src.ThirstValue,
        BurnValue = src.BurnValue,
        ArmorValue = src.ArmorValue,
        ToolPower = src.ToolPower,
        Effects = src.Effects != null ? new List<string>(src.Effects) : new List<string>(),
        ExplorePool = src.ExplorePool != null ? new List<string>(src.ExplorePool) : new List<string>()
    };
}
