using System.Collections.Generic;

namespace CardSurvival.Data;

/// <summary>
/// 将手牌中的工具拖到场景/地点栏时触发的「场景互动」规则（如湖泊 + 鱼叉叉鱼）。
/// 数据来自 <c>data/scene_hand_dances.json</c>。
/// </summary>
public sealed class SceneHandDanceRule
{
	public string LocationId { get; set; } = "";
	public List<string> ToolCardIds { get; set; } = new();
	public int EnergyCost { get; set; }
	/// <summary>消耗的游戏内分钟；≤0 时使用 game_settings 的 SceneHandDance 默认。</summary>
	public float TimeMinutes { get; set; }
	public string ResultCardId { get; set; } = "";
	public int ResultCount { get; set; } = 1;
	public float SuccessChance { get; set; } = 1f;
	public int DurabilityCost { get; set; }
	public string LogSuccess { get; set; } = "";
	public string LogFail { get; set; } = "";
	public string LogNoEnergy { get; set; } = "";
}
