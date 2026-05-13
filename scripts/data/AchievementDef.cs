using System.Collections.Generic;

namespace CardSurvival.Data;

/// <summary>
/// 成就定义（<c>data/achievements.json</c>）。达成后解锁若干配方键（如 <c>wood+stone</c> 或产物 Id <c>axe</c>）。
/// </summary>
public sealed class AchievementDef
{
	public string Id { get; set; } = "";
	public string Name { get; set; } = "";
	public string Description { get; set; } = "";
	public AchievementConditionKind ConditionType { get; set; }
	public string CardId { get; set; } = "";
	public int RequiredCount { get; set; }
	public List<string> UnlockRecipeKeys { get; set; } = new();
}
