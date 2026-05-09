// scripts/data/DataLoader.cs
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CardSurvival.Data;

public static class DataLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static List<CardData> LoadCards(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<CardData>>(json, Options) ?? new();
    }

    public static List<CombineRule> LoadRules(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<CombineRule>>(json, Options) ?? new();
    }
}
