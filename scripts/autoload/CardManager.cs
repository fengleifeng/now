// scripts/autoload/CardManager.cs
using Godot;
using CardSurvival.Data;

namespace CardSurvival;

public partial class CardManager : Node
{
    [Signal] public delegate void OnCardAddedEventHandler(string cardId);
    [Signal] public delegate void OnCardRemovedEventHandler(string cardId);

    private readonly Dictionary<string, CardData> _cardDefs = new();
    private readonly List<CardData> _hand = new();

    public override void _Ready()
    {
        var path = ProjectSettings.GlobalizePath("res://data/cards.json");
        var cards = DataLoader.LoadCards(path);
        foreach (var c in cards)
            _cardDefs[c.Id] = c;
        GD.Print($"[CardManager] Loaded {_cardDefs.Count} card definitions");
    }

    public CardData? GetCard(string id) =>
        _cardDefs.TryGetValue(id, out var card) ? card : null;

    public bool HasCard(string id) => _cardDefs.ContainsKey(id);

    public CardData? DrawCard()
    {
        if (_cardDefs.Count == 0) return null;
        var keys = _cardDefs.Keys.ToArray();
        var id = keys[new Random().Next(keys.Length)];
        var template = _cardDefs[id];
        var instance = CloneCard(template);
        _hand.Add(instance);
        EmitSignal(SignalName.OnCardAdded, instance.Id);
        GD.Print($"[CardManager] Drew card: {instance.Name}");
        return instance;
    }

    public void AddCardToHand(CardData card)
    {
        _hand.Add(card);
        EmitSignal(SignalName.OnCardAdded, card.Id);
        GD.Print($"[CardManager] Added card to hand: {card.Name}");
    }

    public void RemoveCardFromHand(CardData card)
    {
        _hand.Remove(card);
        EmitSignal(SignalName.OnCardRemoved, card.Id);
    }

    public List<CardData> GetHand() => _hand;

    public void DrawInitialHand(int count)
    {
        for (int i = 0; i < count; i++)
            DrawCard();
    }

    private static CardData CloneCard(CardData src) => new()
    {
        Id = src.Id,
        Name = src.Name,
        Type = src.Type,
        Stack = src.Stack,
        MaxStack = src.MaxStack,
        Tags = new List<CardTag>(src.Tags),
        Durability = src.Durability
    };
}
