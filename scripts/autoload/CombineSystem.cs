using Godot;
using CardSurvival.Data;
using System.Collections.Generic;
using System.Linq;

namespace CardSurvival;

public partial class CombineSystem : Node
{
    [Signal] public delegate void OnCombineSuccessEventHandler(string cardAId, string cardBId, string[] results);
    [Signal] public delegate void OnCombineFailEventHandler(string cardAId, string cardBId);
    [Signal] public delegate void OnMultiCombineSuccessEventHandler(string[] ingredients, string[] results);
    [Signal] public delegate void OnMultiCombineFailEventHandler();
    [Signal] public delegate void OnRecipeLearnedEventHandler(string recipeKey);

    private List<CombineRule> _rules = new();
    private readonly Random _random = new();
    private PlayerSystem? _playerSystem;

    public override void _Ready()
    {
        var path = ProjectSettings.GlobalizePath("res://data/combine_rules.json");
        _rules = DataLoader.LoadRules(path);
        GD.Print($"[CombineSystem] Loaded {_rules.Count} rules");
    }

    private PlayerSystem GetPlayerSystem()
    {
        if (_playerSystem == null)
            _playerSystem = GetNode<PlayerSystem>("/root/PlayerSystem");
        return _playerSystem;
    }

    public bool TryCombine(CardData a, CardData b)
    {
        var rule = FindRule(a, b);
        if (rule == null)
        {
            EmitSignal(SignalName.OnCombineFail, a.Id, b.Id);
            return false;
        }
        if (_random.NextDouble() > GetAdjustedChance(rule))
        {
            EmitSignal(SignalName.OnCombineFail, a.Id, b.Id);
            return false;
        }
        var recipeKey = GetRecipeKey(rule);
        LearnRecipe(recipeKey);
        foreach (var result in rule.Results)
            LearnRecipe(result);
        EmitSignal(SignalName.OnCombineSuccess, a.Id, b.Id, rule.Results.ToArray());
        return true;
    }

    public bool TryMultiCombine(List<CardData> ingredients)
    {
        if (ingredients.Count < 2) return false;
        var rule = FindMultiRule(ingredients);
        if (rule == null)
        {
            EmitSignal(SignalName.OnMultiCombineFail);
            return false;
        }
        if (_random.NextDouble() > GetAdjustedChance(rule))
        {
            EmitSignal(SignalName.OnMultiCombineFail);
            return false;
        }
        var recipeKey = GetRecipeKey(rule);
        LearnRecipe(recipeKey);
        foreach (var result in rule.Results)
            LearnRecipe(result);
        var ingredientIds = ingredients.Select(c => c.Id).ToArray();
        EmitSignal(SignalName.OnMultiCombineSuccess, ingredientIds, rule.Results.ToArray());
        return true;
    }

    public string? PreviewResult(List<CardData> ingredients)
    {
        if (ingredients.Count < 2) return null;
        var rule = FindMultiRule(ingredients);
        if (rule == null) return null;
        return string.Join("+", rule.Results);
    }

    public List<CombineRule> GetRules() => _rules;

    public string GetRecipeKey(CombineRule rule)
    {
        if (rule.Ingredients.Count > 0)
            return string.Join("+", rule.Ingredients.OrderBy(x => x));
        return $"{rule.CardA}+{rule.CardB}";
    }

    public void LearnRecipe(string recipeKey)
    {
        if (!IsRecipeLearned(recipeKey))
        {
            GetPlayerSystem().AddLearnedRecipe(recipeKey);
            EmitSignal(SignalName.OnRecipeLearned, recipeKey);
            GD.Print($"[CombineSystem] Learned recipe: {recipeKey}");
        }
    }

    public bool IsRecipeLearned(string recipeKey) => GetPlayerSystem().IsRecipeLearned(recipeKey);

    // ===== 合成检查（堆叠适配）=====

    public bool CanCraftRecipe(CombineRule rule, List<CardData> hand)
    {
        if (rule.Ingredients.Count > 0)
        {
            var available = new Dictionary<string, int>();
            foreach (var card in hand)
            {
                available.TryGetValue(card.Id, out var count);
                available[card.Id] = count + card.Stack;
            }
            foreach (var id in rule.Ingredients)
            {
                if (!available.TryGetValue(id, out var c) || c <= 0) return false;
                available[id] = c - 1;
            }
            return true;
        }
        if (hand.Count < 2) return false;
        if (rule.MatchByTag)
        {
            var a = hand.FirstOrDefault(c => c.Tags.Any(t => t.ToString() == rule.CardA));
            if (a == null) return false;
            return hand.Any(c => c != a && c.Tags.Any(t => t.ToString() == rule.CardB));
        }
        var f = hand.FirstOrDefault(c => c.Id == rule.CardA);
        if (f == null) return false;
        if (rule.CardA == rule.CardB) return f.Stack >= 2;
        return hand.Any(c => c != f && c.Id == rule.CardB);
    }

    public List<CardData> CraftRecipe(CombineRule rule, List<CardData> hand)
    {
        var consumed = new List<CardData>();
        if (rule.Ingredients.Count > 0)
        {
            var list = hand.ToList();
            foreach (var id in rule.Ingredients)
            {
                var m = list.FirstOrDefault(c => c.Id == id);
                if (m != null) { consumed.Add(m); list.Remove(m); }
            }
        }
        else
        {
            CardData? a = null, b = null;
            if (rule.MatchByTag)
            {
                a = hand.FirstOrDefault(c => c.Tags.Any(t => t.ToString() == rule.CardA));
                b = hand.FirstOrDefault(c => c.Tags.Any(t => t.ToString() == rule.CardB) && c != a);
            }
            else
            {
                a = hand.FirstOrDefault(c => c.Id == rule.CardA);
                b = rule.CardA == rule.CardB ? a : hand.FirstOrDefault(c => c.Id == rule.CardB && c != a);
            }
            if (a != null) consumed.Add(a);
            if (b != null) consumed.Add(b);
        }
        LearnRecipe(GetRecipeKey(rule));
        foreach (var r in rule.Results) LearnRecipe(r);
        return consumed;
    }

    private float GetAdjustedChance(CombineRule rule)
    {
        return Mathf.Clamp(rule.Chance + GetPlayerSystem().State.CombineBonus, 0f, 1f);
    }

    private CombineRule? FindRule(CardData a, CardData b)
    {
        foreach (var r in _rules)
        {
            if (r.Ingredients.Count > 0) continue;
            if (!r.MatchByTag) { if (PairMatches(r.CardA, r.CardB, a.Id, b.Id)) return r; }
            else { if (PairMatchesTags(r.CardA, r.CardB, a.Tags, b.Tags)) return r; }
        }
        return null;
    }

    private CombineRule? FindMultiRule(List<CardData> ingredients)
    {
        var ids = ingredients.Select(c => c.Id).OrderBy(x => x).ToList();
        var tags = ingredients.SelectMany(c => c.Tags).Select(t => t.ToString()).ToList();
        foreach (var r in _rules)
        {
            if (r.Ingredients.Count == 0 || r.Ingredients.Count != ingredients.Count) continue;
            var ri = r.Ingredients.OrderBy(x => x).ToList();
            if (!r.MatchByTag) { if (ids.SequenceEqual(ri)) return r; }
            else
            {
                var rt = ri.ToHashSet(); var pt = tags.ToHashSet();
                if (rt.SetEquals(pt)) return r;
            }
        }
        return ingredients.Count == 2 ? FindRule(ingredients[0], ingredients[1]) : null;
    }

    private static bool PairMatches(string ra, string rb, string a, string b) =>
        (ra == a && rb == b) || (ra == b && rb == a);

    private static bool PairMatchesTags(string ra, string rb, List<CardTag> ta, List<CardTag> tb)
    {
        var sa = ta.Select(t => t.ToString()).ToHashSet();
        var sb = tb.Select(t => t.ToString()).ToHashSet();
        return (sa.Contains(ra) && sb.Contains(rb)) || (sb.Contains(ra) && sa.Contains(rb));
    }
}
