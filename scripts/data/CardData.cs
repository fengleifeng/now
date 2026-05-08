// scripts/data/CardData.cs
using System.Text.Json.Serialization;

namespace CardSurvival.Data;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CardType { Resource, Creature, Tool, Building, Status, Event }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CardTag { Wood, Stone, Material, Food, Weapon, Fire, Shelter, Animal, Danger }

public class CardData
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public CardType Type { get; set; }
    public int Stack { get; set; } = 1;
    public int MaxStack { get; set; } = 1;
    public List<CardTag> Tags { get; set; } = new();
    public int Durability { get; set; } = -1; // -1 = infinite durability
}
