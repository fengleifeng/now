using Godot;
using CardSurvival.Data;
using CardSurvival.Game;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CardSurvival;

/// <summary>
/// 状态与疾病管理系统。管理所有 ActiveEffect 的添加、移除、每回合结算。
/// </summary>
public partial class EffectSystem : Node
{
    [Signal] public delegate void OnEffectAddedEventHandler(string effectId, int intensity);
    [Signal] public delegate void OnEffectRemovedEventHandler(string effectId);
    [Signal] public delegate void OnEffectTickedEventHandler(string effectId, int remainingTurns);

    private readonly List<ActiveEffect> _activeEffects = new();
    private readonly Dictionary<string, EffectData> _effectDefs = new();
    private PlayerSystem? _playerSystem;

    public override void _Ready()
    {
        var path = ProjectSettings.GlobalizePath(ContentPaths.Effects);
        var effects = DataLoader.LoadEffects(path);
        foreach (var e in effects)
            _effectDefs[e.Id] = e;

        GD.Print($"[EffectSystem] Loaded {_effectDefs.Count} effect definitions");
    }

    private PlayerSystem GetPlayerSystem()
    {
        if (_playerSystem == null)
            _playerSystem = GetNode<PlayerSystem>("/root/PlayerSystem");
        return _playerSystem;
    }

    /// <summary> 获取所有活跃效果 </summary>
    public List<ActiveEffect> GetActiveEffects() => _activeEffects.ToList();

    /// <summary> 获取玩家身上的疾病 </summary>
    public List<ActiveEffect> GetDiseases() =>
        _activeEffects.Where(e => GetDefinition(e.EffectId)?.Type == EffectType.Disease).ToList();

    /// <summary> 获取玩家身上的增益 </summary>
    public List<ActiveEffect> GetBuffs() =>
        _activeEffects.Where(e => GetDefinition(e.EffectId)?.Type == EffectType.Buff).ToList();

    /// <summary> 是否有指定效果 </summary>
    public bool HasEffect(string effectId) =>
        _activeEffects.Any(e => e.EffectId == effectId);

    /// <summary> 获取效果定义 </summary>
    public EffectData? GetDefinition(string effectId) =>
        _effectDefs.TryGetValue(effectId, out var def) ? def : null;

    /// <summary>
    /// 卡牌瞬间效果（cards.json 的 Effects 字段，如 sanity+5、cure:poison）。
    /// </summary>
    public void ApplyInstantCardEffects(IEnumerable<string> rawEffects)
    {
        var normalized = new List<string>();
        foreach (var raw in rawEffects)
        {
            if (raw.StartsWith("cure:", StringComparison.OrdinalIgnoreCase))
            {
                var id = raw["cure:".Length..].Trim();
                if (!string.IsNullOrEmpty(id))
                    RemoveEffect(id);
                continue;
            }

            var fmt = CardInstantEffectApplicator.NormalizeEffectString(raw);
            if (fmt != null)
                normalized.Add(fmt);
        }

        if (normalized.Count > 0)
            ApplyEffectStrings(normalized, 1);
    }

    /// <summary> 添加一个效果到玩家身上 </summary>
    public void AddEffect(string effectId, int intensity = 1, int? duration = null)
    {
        if (!_effectDefs.TryGetValue(effectId, out var def)) return;

        // 已有相同效果 → 刷新或叠加
        var existing = _activeEffects.FirstOrDefault(e => e.EffectId == effectId);
        if (existing != null)
        {
            existing.RemainingTurns = duration ?? def.DefaultDuration;
            existing.Intensity = Math.Min(existing.Intensity + intensity, 5);
            return;
        }

        var effect = new ActiveEffect
        {
            EffectId = effectId,
            RemainingTurns = duration ?? def.DefaultDuration,
            Intensity = intensity
        };
        _activeEffects.Add(effect);

        // 应用即时效果
        ApplyEffectStrings(def.OnApplyEffects, intensity);
        EmitSignal(SignalName.OnEffectAdded, effectId, intensity);
        GD.Print($"[EffectSystem] Added effect: {effectId} (intensity={intensity})");
    }

    /// <summary> 移除玩家身上第一个疾病效果（若无则 false）。用于夜仪等主动技能。 </summary>
    public bool TryRemoveFirstDisease()
    {
        var disease = GetDiseases().FirstOrDefault();
        if (disease == null) return false;
        RemoveEffect(disease.EffectId);
        return true;
    }

    /// <summary> 移除指定效果 </summary>
    public void RemoveEffect(string effectId)
    {
        var effect = _activeEffects.FirstOrDefault(e => e.EffectId == effectId);
        if (effect == null) return;

        var def = GetDefinition(effectId);
        if (def != null)
            ApplyEffectStrings(def.OnRemoveEffects, effect.Intensity);

        _activeEffects.Remove(effect);
        EmitSignal(SignalName.OnEffectRemoved, effectId);
        GD.Print($"[EffectSystem] Removed effect: {effectId}");
    }

    /// <summary> 用卡牌尝试治疗疾病 </summary>
    /// <returns>被治愈的效果ID列表</returns>
    public List<string> TryCureWithCard(CardData card)
    {
        var cured = new List<string>();
        foreach (var effect in _activeEffects.ToList())
        {
            var def = GetDefinition(effect.EffectId);
            if (def == null) continue;

            var curedByTag = !string.IsNullOrEmpty(def.CuredByTag)
                && card.Tags.Any(t => t.ToString() == def.CuredByTag);
            var curedByCard = !string.IsNullOrEmpty(def.CuredByCard)
                && card.Id == def.CuredByCard;

            if (curedByTag || curedByCard)
            {
                RemoveEffect(effect.EffectId);
                cured.Add(effect.EffectId);
            }
        }
        return cured;
    }

    /// <summary> 每回合结算（推进所有效果的持续时间和每回合效果） </summary>
    public void TickEffects()
    {
        var expired = new List<ActiveEffect>();

        foreach (var effect in _activeEffects)
        {
            var def = GetDefinition(effect.EffectId);
            if (def == null) continue;

            // 应用每回合效果
            ApplyEffectStrings(def.PerTurnEffects, effect.Intensity);

            // 减少剩余回合
            if (effect.RemainingTurns > 0)
            {
                effect.RemainingTurns--;
                if (effect.RemainingTurns <= 0)
                    expired.Add(effect);
            }

            EmitSignal(SignalName.OnEffectTicked, effect.EffectId, effect.RemainingTurns);
        }

        // 移除到期的效果
        foreach (var effect in expired)
        {
            var def = GetDefinition(effect.EffectId);
            if (def != null)
                ApplyEffectStrings(def.OnRemoveEffects, effect.Intensity);
            _activeEffects.Remove(effect);
            EmitSignal(SignalName.OnEffectRemoved, effect.EffectId);
        }
    }

    /// <summary> 触发随机疾病（根据免疫力概率） </summary>
    public void TryTriggerDisease(string diseaseId, float baseChance)
    {
        var player = GetPlayerSystem();
        var immunityFactor = player.State.Immunity / 100f; // 0~1
        var adjustedChance = baseChance * (1f - immunityFactor * 0.7f);
        if (GD.Randf() < adjustedChance)
            AddEffect(diseaseId);
    }

    /// <summary> 清除所有效果 </summary>
    public void ClearAll()
    {
        _activeEffects.Clear();
    }

    public Godot.Collections.Array SerializeActiveEffects()
    {
        var arr = new Godot.Collections.Array();
        foreach (var e in _activeEffects)
        {
            arr.Add(new Godot.Collections.Dictionary<string, Variant>
            {
                { "EffectId", e.EffectId },
                { "RemainingTurns", e.RemainingTurns },
                { "Intensity", e.Intensity }
            });
        }
        return arr;
    }

    public void DeserializeActiveEffects(Godot.Collections.Array? data)
    {
        _activeEffects.Clear();
        if (data == null) return;
        foreach (var item in data)
        {
            if (item.VariantType != Variant.Type.Dictionary) continue;
            var d = new Godot.Collections.Dictionary<string, Variant>((Godot.Collections.Dictionary)item);
            if (!d.ContainsKey("EffectId")) continue;
            _activeEffects.Add(new ActiveEffect
            {
                EffectId = (string)d["EffectId"],
                RemainingTurns = d.ContainsKey("RemainingTurns") ? (int)d["RemainingTurns"] : 1,
                Intensity = d.ContainsKey("Intensity") ? (int)d["Intensity"] : 1
            });
        }
    }

    // ====================================

    private void ApplyEffectStrings(List<string> effectStrings, int intensity)
    {
        var player = GetPlayerSystem();
        foreach (var effectStr in effectStrings)
        {
            var (target, value, isMultiply) = EffectExtensions.ParseEffect(effectStr);
            var scaledValue = value * intensity;

            switch (target)
            {
                case EffectTarget.Health:
                    if (isMultiply)
                        player.State.Health = (int)(player.State.Health * scaledValue);
                    else
                        player.TakeDamage(-(int)scaledValue); // negative = heal
                    break;
                case EffectTarget.Hunger when effectStr.Contains("HungerRate"):
                    player.State.HungerRate = isMultiply
                        ? player.State.HungerRate * scaledValue
                        : player.State.HungerRate + scaledValue;
                    break;
                case EffectTarget.Hunger:
                    if (scaledValue < 0)
                        player.ConsumeHunger((int)-scaledValue);
                    else
                        player.Eat((int)scaledValue);
                    break;
                case EffectTarget.Thirst when effectStr.Contains("ThirstRate"):
                    player.State.ThirstRate = isMultiply
                        ? player.State.ThirstRate * scaledValue
                        : player.State.ThirstRate + scaledValue;
                    break;
                case EffectTarget.Thirst:
                    player.UpdateThirst((int)scaledValue);
                    break;
                case EffectTarget.Energy:
                    if (scaledValue < 0)
                        player.ConsumeEnergy((int)-scaledValue);
                    else
                        player.RestoreEnergy((int)scaledValue);
                    break;
                case EffectTarget.Sanity:
                    player.UpdateSanity((int)scaledValue);
                    break;
                case EffectTarget.Immunity:
                    player.UpdateImmunity((int)scaledValue);
                    break;
                case EffectTarget.Temperature:
                    // 最小值 -10, 最大值 40
                    player.State.Temperature = Math.Clamp(
                        player.State.Temperature + (int)scaledValue,
                        player.State.MinTemperature,
                        player.State.MaxTemperature);
                    break;
            }
        }
    }
}
