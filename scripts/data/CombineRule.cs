// scripts/data/CombineRule.cs
namespace CardSurvival.Data;

public class CombineRule
{
    public string CardA { get; set; } = "";
    public string CardB { get; set; } = "";
    public List<string> Results { get; set; } = new();
    public float Chance { get; set; } = 1.0f;
    public bool MatchByTag { get; set; } = false;
}
