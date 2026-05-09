// scripts/autoload/CombineSystem.cs
using Godot;
using CardSurvival.Data;

namespace CardSurvival;

public partial class CombineSystem : Node
{
    [Signal] public delegate void OnCombineSuccessEventHandler(string cardAId, string cardBId, string[] results);
    [Signal] public delegate void OnCombineFailEventHandler(string cardAId, string cardBId);

    private List<CombineRule> _rules = new();

    public override void _Ready()
    {
        var path = ProjectSettings.GlobalizePath("res://data/combine_rules.json");
        _rules = DataLoader.LoadRules(path);
        GD.Print($"[CombineSystem] Loaded {_rules.Count} rules");
    }

    public bool TryCombine(CardData a, CardData b)
    {
        var rule = FindRule(a, b);
        if (rule == null)
        {
            EmitSignal(SignalName.OnCombineFail, a.Id, b.Id);
            return false;
        }

        if (new Random().NextDouble() > rule.Chance)
        {
            EmitSignal(SignalName.OnCombineFail, a.Id, b.Id);
            return false;
        }

        EmitSignal(SignalName.OnCombineSuccess, a.Id, b.Id, rule.Results.ToArray());
        return true;
    }

    private CombineRule? FindRule(CardData a, CardData b)
    {
        // Priority 1: exact ID match
        foreach (var r in _rules)
        {
            if (r.MatchByTag) continue;
            if (PairMatches(r.CardA, r.CardB, a.Id, b.Id))
                return r;
        }

        // Priority 2: tag match
        foreach (var r in _rules)
        {
            if (!r.MatchByTag) continue;
            if (PairMatchesTags(r.CardA, r.CardB, a.Tags, b.Tags))
                return r;
        }

        return null;
    }

    private static bool PairMatches(string ra, string rb, string idA, string idB)
    {
        return (ra == idA && rb == idB) || (ra == idB && rb == idA);
    }

    private static bool PairMatchesTags(string ra, string rb, List<CardTag> tagsA, List<CardTag> tagsB)
    {
        var flatA = tagsA.Select(t => t.ToString());
        var flatB = tagsB.Select(t => t.ToString());

        var hasA = flatA.Contains(ra) || flatB.Contains(ra);
        var hasB = flatB.Contains(rb) || flatA.Contains(rb);
        return hasA && hasB;
    }
}
