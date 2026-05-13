using System.Text.Json.Serialization;

namespace CardSurvival.Data;

/// <summary>成就条件类型，与 <c>achievements.json</c> 中 <c>ConditionType</c> 字符串对应。</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AchievementConditionKind
{
	LifetimeHandGain,
	LifetimeExplore
}
