using Godot;
using CardSurvival.Data;
using CardSurvival.Game;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CardSurvival;

public partial class CardManager : Node
{
    [Signal] public delegate void OnCardAddedEventHandler(string cardId);
    [Signal] public delegate void OnHandStackGainedEventHandler(string cardId, int amount);
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

    /// <summary>手牌中「待合成」占位卡 Id，非 cards.json 定义。</summary>
    public const string StagedCombineDisplayId = "__staged_combine__";

    private CombineRule? _stagedCombineRule;
    private CardData? _stagedDisplayCard;
    private List<CardData>? _stagedConsumeOrder;
    private CardData? _anchoredFixed;

    // Cache of drawable card IDs to avoid filtering every call
    private List<string>? _cachedDrawableIds;

    public override void _Ready()
    {
        var cardsPath = ProjectSettings.GlobalizePath(ContentPaths.Cards);
        var cards = DataLoader.LoadCards(cardsPath);
        foreach (var c in cards)
            _cardDefs[c.Id] = c;

        var projectsPath = ProjectSettings.GlobalizePath(ContentPaths.Projects);
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

    /// <summary>不含已暂存用于合成的材料（用于判定、选卡、配方列表）。</summary>
    public List<CardData> GetPlayableHand() =>
        _hand.Where(c => !c.IsStagedIngredient).ToList();

    /// <summary>手牌区展示顺序：可用手牌 + 一张「待合成」占位（若有）。</summary>
    public List<CardData> GetHandForUi()
    {
        var list = GetPlayableHand();
        if (_stagedCombineRule != null && _stagedDisplayCard != null)
            list.Add(_stagedDisplayCard);
        return list;
    }

    public static bool IsStagedCombineDisplay(CardData? card) =>
        card != null && card.Id == StagedCombineDisplayId;

    public bool HasStagedRecipe => _stagedCombineRule != null;

    public CombineRule? GetStagedCombineRule() => _stagedCombineRule;

    public CardData? GetAnchoredCard() => _anchoredFixed;

    /// <summary>固定栏（不可拖动的建筑等）；被替换的旧卡会落到当前场景。</summary>
    public void SetAnchoredCard(CardData? incoming)
    {
        if (incoming == null)
        {
            _anchoredFixed = null;
            return;
        }

        var prev = _anchoredFixed;
        _anchoredFixed = incoming;
        if (prev != null)
            GetMapSystem().AddCardToCurrentScene(prev);
    }

    public bool TryStageRecipe(CombineRule rule, CombineSystem combine)
    {
        if (_stagedCombineRule != null) return false;
        var play = GetPlayableHand();
        var picked = combine.SelectCraftConsumablesWithoutLearning(rule, play);
        if (picked == null || picked.Count == 0) return false;

        foreach (var c in picked.Distinct())
        {
            c.IsStagedIngredient = true;
            c.IsDraggable = false;
        }

        _stagedCombineRule = rule;
        _stagedDisplayCard = BuildStagedDisplayCard(rule);
        _stagedConsumeOrder = new List<CardData>(picked);
        return true;
    }

    public void CancelStagedRecipe()
    {
        foreach (var c in _hand.Where(c => c.IsStagedIngredient))
        {
            c.IsStagedIngredient = false;
            if (_cardDefs.TryGetValue(c.Id, out var def))
                c.IsDraggable = def.IsDraggable;
        }

        _stagedCombineRule = null;
        _stagedDisplayCard = null;
        _stagedConsumeOrder = null;
    }

    public void ClearStagedCombineMetaOnly()
    {
        _stagedCombineRule = null;
        _stagedDisplayCard = null;
        _stagedConsumeOrder = null;
    }

    public IReadOnlyList<CardData>? GetStagedConsumeOrder() => _stagedConsumeOrder;

    public List<CardData> GetStagedIngredientCardsSnapshot() =>
        _hand.Where(c => c.IsStagedIngredient).ToList();

    public void AddCardToHand(CardData card)
    {
        var gained = AddToHandWithLimit(card);
        EmitSignal(SignalName.OnCardAdded, card.Id);
        if (gained > 0 && !IsStagedCombineDisplay(card) && !card.IsStagedIngredient)
            EmitSignal(SignalName.OnHandStackGained, card.Id, gained);
    }

    /// <summary>
    /// 从手牌移除一张卡。堆叠卡只减数量，数量归零才移除。
    /// </summary>
    public void RemoveCardFromHand(CardData card)
    {
        if (_hand.Contains(card))
        {
            if (card.Stack > 1)
                card.Stack--;
            else
                _hand.Remove(card);
            EmitSignal(SignalName.OnCardRemoved, card.Id);
            return;
        }

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
        if (_hand.Contains(card))
        {
            if (card.Stack > 1)
                card.Stack--;
            else
                _hand.Remove(card);
            EmitSignal(SignalName.OnCardConsumed, card.Id, reason);
            return;
        }

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
    public bool HasCardInHand(string id) =>
        _hand.Any(c => c.Id == id && !c.IsStagedIngredient);

    /// <summary>
    /// 获取手牌中某张卡的总堆叠数
    /// </summary>
    public int GetCardStackInHand(string id)
    {
        var card = _hand.FirstOrDefault(c => c.Id == id && !c.IsStagedIngredient);
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
        var gained = AddToHandWithLimit(card);
        EmitSignal(SignalName.OnSceneCardsChanged);
        EmitSignal(SignalName.OnCardAdded, card.Id);
        if (gained > 0 && !card.IsStagedIngredient)
            EmitSignal(SignalName.OnHandStackGained, card.Id, gained);
    }

    public void MoveHandCardToScene(CardData card)
    {
        if (card.IsStagedIngredient || IsStagedCombineDisplay(card)) return;
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

    public CardData? FindHandCardByTag(CardTag tag) =>
        _hand.FirstOrDefault(c => c.Tags.Contains(tag) && !c.IsStagedIngredient);

    public List<CardData> GetHandCardsByType(CardType type) =>
        _hand.Where(c => c.Type == type && !c.IsStagedIngredient).ToList();

    // ============================================================
    //  耐久
    // ============================================================

    /// <summary> 手牌中是否存在可磨利（未满耐久）的工具或武器。 </summary>
    public bool CanSharpenToolInHand()
    {
        foreach (var card in _hand.Where(c => !c.IsStagedIngredient))
        {
            if (card.Type != CardType.Tool && card.Type != CardType.Weapon) continue;
            if (card.Durability <= 0) continue;
            if (!_cardDefs.TryGetValue(card.Id, out var def)) continue;
            if (def.Durability <= 0) continue;
            if (card.Durability < def.Durability) return true;
        }
        return false;
    }

    /// <summary> 将第一张未满耐久的工具/武器 +1 耐久（不超过卡表默认上限）。 </summary>
    public bool TrySharpenToolInHand()
    {
        foreach (var card in _hand.Where(c => !c.IsStagedIngredient))
        {
            if (card.Type != CardType.Tool && card.Type != CardType.Weapon) continue;
            if (card.Durability <= 0) continue;
            if (!_cardDefs.TryGetValue(card.Id, out var def)) continue;
            if (def.Durability <= 0) continue;
            if (card.Durability >= def.Durability) continue;
            card.Durability = Math.Min(def.Durability, card.Durability + 1);
            return true;
        }
        return false;
    }

    public void TickDurability()
    {
        var handToRemove = new List<CardData>();
        foreach (var card in _hand.Where(c => !c.IsStagedIngredient))
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
        _stagedCombineRule = null;
        _stagedDisplayCard = null;
        _anchoredFixed = null;
        _stagedConsumeOrder = null;
        EmitSignal(SignalName.OnCardRemoved, "");
    }

    // ============================================================
    //  内部方法
    // ============================================================

    /// <summary>
    /// 将卡加入手牌，同ID自动堆叠。
    /// 超过手牌槽位上限时丢弃最早的非堆叠槽位。
    /// </summary>
    /// <returns>实际进入手牌库存的该卡堆叠增量之和（不含被立刻挤掉的牌）。</returns>
    private int AddToHandWithLimit(CardData card)
    {
        if (card.Stack <= 0)
            return 0;

        var gained = 0;
        while (card.Stack > 0)
        {
            var existing = _hand.FirstOrDefault(c =>
                c.Id == card.Id && c.Stack < c.MaxStack && !c.IsStagedIngredient);
            if (existing != null)
            {
                var space = existing.MaxStack - existing.Stack;
                var add = Math.Min(space, card.Stack);
                existing.Stack += add;
                card.Stack -= add;
                gained += add;
                continue;
            }

            _hand.Add(card);
            gained += card.Stack;
            break;
        }

        while (_hand.Count > MaxHandSlots)
        {
            var discarded = _hand[0];
            _hand.RemoveAt(0);
            EmitSignal(SignalName.OnCardConsumed, discarded.Id, "hand_limit");
        }

        return gained;
    }

    private CardData BuildStagedDisplayCard(CombineRule rule)
    {
        var matText = DescribeMaterialsShort(rule);
        var resText = rule.Results.Count == 0
            ? I18n.T("craft.eliminate")
            : string.Join("、", rule.Results.Select(r => _cardDefs.GetValueOrDefault(r)?.Name ?? r));
        return new CardData
        {
            Id = StagedCombineDisplayId,
            Name = I18n.T("stagedcard.name"),
            Type = CardType.Status,
            Description = I18n.Tf("stagedcard.desc_fmt", matText, resText),
            IsDraggable = false,
            Stack = 1,
            MaxStack = 1
        };
    }

    private string DescribeMaterialsShort(CombineRule rule)
    {
        if (rule.Ingredients.Count > 0)
            return string.Join("、", rule.Ingredients.Select(id => _cardDefs.GetValueOrDefault(id)?.Name ?? id));
        if (rule.MatchByTag)
            return I18n.Tf("staged.tag_pair_fmt", rule.CardA, rule.CardB);
        var na = _cardDefs.GetValueOrDefault(rule.CardA)?.Name ?? rule.CardA;
        var nb = _cardDefs.GetValueOrDefault(rule.CardB)?.Name ?? rule.CardB;
        return $"{na} + {nb}";
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

    public static Godot.Collections.Dictionary<string, Variant> PackCardRuntime(CardData c) =>
        new()
        {
            { "Id", c.Id },
            { "Stack", c.Stack },
            { "Durability", c.Durability }
        };

    public static void ApplyCardRuntime(CardData card, Godot.Collections.Dictionary<string, Variant> d)
    {
        if (d.ContainsKey("Stack")) card.Stack = (int)d["Stack"];
        if (d.ContainsKey("Durability")) card.Durability = (int)d["Durability"];
    }
}
