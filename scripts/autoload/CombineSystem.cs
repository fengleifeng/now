using Godot;
using CardSurvival.Data;
using CardSurvival.Game;
using System.Collections.Generic;
using System.Linq;

namespace CardSurvival;

public partial class CombineSystem : Node
{
    [Signal] public delegate void OnCombineSuccessEventHandler(string cardAId, string cardBId, string[] results, double synthesisMinutes);
    [Signal] public delegate void OnCombineFailEventHandler(string cardAId, string cardBId);
    [Signal] public delegate void OnMultiCombineSuccessEventHandler(string[] ingredients, string[] results, double synthesisMinutes);
    [Signal] public delegate void OnMultiCombineFailEventHandler();
    [Signal] public delegate void OnRecipeLearnedEventHandler(string recipeKey);

    private const char IngredientKeySep = '\u001e';

    private List<CombineRule> _rules = new();
    private readonly Dictionary<string, List<CombineRule>> _pairNonTagBySortedKey = new();
    private readonly List<CombineRule> _pairTagRules = new();
    private readonly Dictionary<string, CombineRule> _multiByIngredientKey = new();
    private readonly List<CombineRule> _multiTagRules = new();

    private readonly Random _random = new();
    private PlayerSystem? _playerSystem;
    private CardManager? _cardManager;
    private GameSettings? _gameSettings;

    public override void _Ready()
    {
        var path = ProjectSettings.GlobalizePath(ContentPaths.CombineRules);
        _rules = DataLoader.LoadRules(path);
        _cardManager = GetNodeOrNull<CardManager>("/root/CardManager");
        _gameSettings = GetNodeOrNull<GameSettings>("/root/GameSettings");
        RebuildRuleIndices();
        GD.Print($"[CombineSystem] Loaded {_rules.Count} rules (indexed: pairId={_pairNonTagBySortedKey.Count}, pairTag={_pairTagRules.Count}, multi={_multiByIngredientKey.Count})");
    }

    private void RebuildRuleIndices()
    {
        _pairNonTagBySortedKey.Clear();
        _pairTagRules.Clear();
        _multiByIngredientKey.Clear();
        _multiTagRules.Clear();

        foreach (var r in _rules)
        {
            if (r.Ingredients.Count > 0)
            {
                if (r.MatchByTag)
                    _multiTagRules.Add(r);
                else
                {
                    var key = BuildSortedIngredientKey(r.Ingredients);
                    if (_multiByIngredientKey.ContainsKey(key))
                        GD.PushWarning($"[CombineSystem] 多卡配方键重复，保留先出现的规则: {key}");
                    else
                        _multiByIngredientKey[key] = r;
                }
                continue;
            }

            if (r.MatchByTag)
                _pairTagRules.Add(r);
            else
            {
                var key = OrderedPairKey(r.CardA, r.CardB);
                if (!_pairNonTagBySortedKey.TryGetValue(key, out var list))
                {
                    list = new List<CombineRule>();
                    _pairNonTagBySortedKey[key] = list;
                }

                list.Add(r);
            }
        }
    }

    private static string OrderedPairKey(string a, string b) =>
        string.CompareOrdinal(a, b) <= 0 ? $"{a}{IngredientKeySep}{b}" : $"{b}{IngredientKeySep}{a}";

    private static string BuildSortedIngredientKey(IReadOnlyList<string> ids) =>
        string.Join(IngredientKeySep, ids.OrderBy(x => x));

    private PlayerSystem GetPlayerSystem()
    {
        if (_playerSystem == null)
            _playerSystem = GetNode<PlayerSystem>("/root/PlayerSystem");
        return _playerSystem;
    }

    public bool IsRecipeUnlockedForAttempt(CombineRule rule) =>
        RecipeUnlockPolicy.AllowsHandCombine(_gameSettings, GetPlayerSystem().State, GetRecipeKey(rule));

    public bool TryCombine(CardData a, CardData b)
    {
        var rule = FindRule(a, b);
        if (rule == null)
        {
            EmitSignal(SignalName.OnCombineFail, a.Id, b.Id);
            return false;
        }

        if (!IsRecipeUnlockedForAttempt(rule))
        {
            EmitSignal(SignalName.OnCombineFail, a.Id, b.Id);
            return false;
        }

        if (_random.NextDouble() > GetAdjustedChance(rule))
        {
            EmitSignal(SignalName.OnCombineFail, a.Id, b.Id);
            return false;
        }

        var results = rule.Results.ToArray();
        EmitSignal(SignalName.OnCombineSuccess, a.Id, b.Id, results, (double)GetSynthesisMinutes(rule));
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

        if (!IsRecipeUnlockedForAttempt(rule))
        {
            EmitSignal(SignalName.OnMultiCombineFail);
            return false;
        }

        if (_random.NextDouble() > GetAdjustedChance(rule))
        {
            EmitSignal(SignalName.OnMultiCombineFail);
            return false;
        }

        var ingredientIds = ingredients.Select(c => c.Id).ToArray();
        var results = rule.Results.ToArray();
        EmitSignal(SignalName.OnMultiCombineSuccess, ingredientIds, results, (double)GetSynthesisMinutes(rule));
        return true;
    }

    public string? PreviewResult(List<CardData> ingredients)
    {
        if (ingredients.Count < 2) return null;
        var rule = FindMultiRule(ingredients);
        return rule == null ? null : string.Join("+", rule.Results);
    }

    public List<CombineRule> GetRules() => _rules;

    /// <summary>
    /// 合成耗时（分钟）：优先规则 <see cref="CombineRule.TimeMinutes"/>，否则为各产物卡 <see cref="CardData.CombineMinutes"/> 之和，
    /// 均为 0 时用全局默认（多卡为 MultiCombine，否则 CombineHand）。
    /// </summary>
    public float GetSynthesisMinutes(CombineRule rule)
    {
        if (rule.TimeMinutes > 0f)
            return rule.TimeMinutes;

        var cm = _cardManager ?? GetNodeOrNull<CardManager>("/root/CardManager");
        float sum = 0f;
        foreach (var id in rule.Results)
        {
            var t = cm?.GetCard(id);
            if (t is { CombineMinutes: > 0 })
                sum += t.CombineMinutes;
        }

        if (sum > 0f)
            return sum;

        var gs = _gameSettings ?? GetNodeOrNull<GameSettings>("/root/GameSettings");
        var key = rule.Ingredients.Count > 0 ? "MultiCombine" : "CombineHand";
        return gs?.GetDefaultActionMinutes(key) ?? 15f;
    }

    public string GetRecipeKey(CombineRule rule) =>
        rule.Ingredients.Count > 0
            ? string.Join("+", rule.Ingredients.OrderBy(x => x))
            : $"{rule.CardA}+{rule.CardB}";

    public void LearnRecipe(string recipeKey)
    {
        if (IsRecipeLearned(recipeKey)) return;
        GetPlayerSystem().AddLearnedRecipe(recipeKey);
        EmitSignal(SignalName.OnRecipeLearned, recipeKey);
        GD.Print($"[CombineSystem] Learned recipe: {recipeKey}");
    }

    public bool IsRecipeLearned(string recipeKey) => GetPlayerSystem().IsRecipeLearned(recipeKey);

    public bool CanCraftRecipe(CombineRule rule, List<CardData> hand)
    {
        if (rule.Ingredients.Count > 0)
            return HasIngredientsInHand(rule.Ingredients, hand);

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
                if (m == null) continue;
                consumed.Add(m);
                list.Remove(m);
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

        return consumed;
    }

    /// <summary>仅记录配方与产物键，在真正合成成功时调用。</summary>
    public void LearnRuleAndResults(CombineRule rule)
    {
        LearnRecipe(GetRecipeKey(rule));
        foreach (var result in rule.Results)
            LearnRecipe(result);
    }

    /// <summary>选出将消耗的卡牌实例，不学习配方（用于手牌暂存）。</summary>
    public List<CardData>? SelectCraftConsumablesWithoutLearning(CombineRule rule, List<CardData> hand)
    {
        if (!CanCraftRecipe(rule, hand)) return null;
        var consumed = new List<CardData>();
        if (rule.Ingredients.Count > 0)
        {
            var list = hand.ToList();
            foreach (var id in rule.Ingredients)
            {
                var m = list.FirstOrDefault(c => c.Id == id);
                if (m == null) continue;
                consumed.Add(m);
                list.Remove(m);
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

        return consumed;
    }

    private static bool HasIngredientsInHand(IReadOnlyList<string> required, List<CardData> hand)
    {
        var available = new Dictionary<string, int>();
        foreach (var card in hand)
        {
            available.TryGetValue(card.Id, out var count);
            available[card.Id] = count + card.Stack;
        }

        foreach (var id in required)
        {
            if (!available.TryGetValue(id, out var c) || c <= 0) return false;
            available[id] = c - 1;
        }

        return true;
    }

    private float GetAdjustedChance(CombineRule rule) =>
        Mathf.Clamp(rule.Chance + GetPlayerSystem().State.CombineBonus, 0f, 1f);

    private CombineRule? FindRule(CardData a, CardData b)
    {
        var key = OrderedPairKey(a.Id, b.Id);
        if (_pairNonTagBySortedKey.TryGetValue(key, out var list))
        {
            foreach (var r in list)
            {
                if (PairMatches(r.CardA, r.CardB, a.Id, b.Id))
                    return r;
            }
        }

        foreach (var r in _pairTagRules)
        {
            if (PairMatchesTags(r.CardA, r.CardB, a.Tags, b.Tags))
                return r;
        }

        return null;
    }

    private CombineRule? FindMultiRule(List<CardData> ingredients)
    {
        var n = ingredients.Count;
        if (n < 2) return null;

        var idKey = BuildSortedIngredientKey(ingredients.Select(c => c.Id).ToList());
        if (_multiByIngredientKey.TryGetValue(idKey, out var byId) && byId.Ingredients.Count == n && !byId.MatchByTag)
            return byId;

        foreach (var r in _multiTagRules)
        {
            if (r.Ingredients.Count != n || !r.MatchByTag) continue;
            var ri = r.Ingredients.OrderBy(x => x).ToList();
            var tagSet = new HashSet<string>(ingredients.SelectMany(c => c.Tags).Select(t => t.ToString()));
            if (ri.ToHashSet().SetEquals(tagSet))
                return r;
        }

        return n == 2 ? FindRule(ingredients[0], ingredients[1]) : null;
    }

    private static bool PairMatches(string ra, string rb, string a, string b) =>
        (ra == a && rb == b) || (ra == b && rb == a);

    private static bool PairMatchesTags(string ra, string rb, List<CardTag> ta, List<CardTag> tb)
    {
        var aHasRa = false;
        var aHasRb = false;
        var bHasRa = false;
        var bHasRb = false;
        foreach (var t in ta)
        {
            var s = t.ToString();
            if (s == ra) aHasRa = true;
            else if (s == rb) aHasRb = true;
        }

        foreach (var t in tb)
        {
            var s = t.ToString();
            if (s == ra) bHasRa = true;
            else if (s == rb) bHasRb = true;
        }

        return (aHasRa && bHasRb) || (bHasRa && aHasRb);
    }
}
