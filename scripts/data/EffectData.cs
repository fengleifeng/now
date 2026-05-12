// scripts/data/EffectData.cs
using System.Text.Json.Serialization;

namespace CardSurvival.Data;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EffectType
{
    Disease,    // 疾病 - 负面，持续
    Buff,       // 增益 - 正面，持续
    Injury,     // 外伤 - 负面，持续
    Instant     // 瞬间效果
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EffectTarget
{
    Health, Hunger, Thirst, Energy, Sanity, Immunity, Temperature
}

public class EffectData
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public EffectType Type { get; set; } = EffectType.Disease;
    public bool IsVisible { get; set; } = true;  // 是否显示在UI上

    /// <summary>
    /// 每回合效果列表。格式: "target:value" 如 "Health:-5", "HungerRate:*2"
    /// target 取值: Health/Hunger/Thirst/Energy/Sanity/Immunity/Temperature
    /// value 格式: +N / -N / *N  (加减或乘)
    /// </summary>
    public List<string> PerTurnEffects { get; set; } = new();

    /// <summary>
    /// 施加时立即生效的效果（同上格式）
    /// </summary>
    public List<string> OnApplyEffects { get; set; } = new();

    /// <summary>
    /// 移除时触发 格式同上
    /// </summary>
    public List<string> OnRemoveEffects { get; set; } = new();

    public int DefaultDuration { get; set; } = 5;     // 默认持续回合数
    public string CuredByTag { get; set; } = "";     // 被什么标签的卡治愈（如 Medicine）
    public string CuredByCard { get; set; } = "";    // 或被什么具体卡治愈

    /// <summary>
    /// 获得途径: 事件触发条件
    /// </summary>
    public string TriggerCondition { get; set; } = "";
}

public static class EffectExtensions
{
    /// <summary>
    /// 解析效果字符串 "Health:-5" → (EffectTarget.Health, -5, false)
    /// 支持格式: "Health:+5", "HungerRate:*2", "Immunity:-10"
    /// </summary>
    public static (EffectTarget target, float value, bool isMultiply) ParseEffect(string effectStr)
    {
        var parts = effectStr.Split(':');
        if (parts.Length != 2) return (EffectTarget.Health, 0, false);

        var target = parts[0] switch
        {
            "Health" => EffectTarget.Health,
            "Hunger" => EffectTarget.Hunger,
            "Thirst" => EffectTarget.Thirst,
            "Energy" => EffectTarget.Energy,
            "Sanity" => EffectTarget.Sanity,
            "Immunity" => EffectTarget.Immunity,
            "Temperature" => EffectTarget.Temperature,
            "HungerRate" => EffectTarget.Hunger,
            "ThirstRate" => EffectTarget.Thirst,
            _ => EffectTarget.Health
        };

        var valStr = parts[1];
        if (valStr.StartsWith('*'))
        {
            return (target, float.Parse(valStr[1..]), true);
        }

        return (target, float.Parse(valStr), false);
    }
}
