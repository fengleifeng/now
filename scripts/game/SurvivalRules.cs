using System;
using CardSurvival.Data;

namespace CardSurvival.Game;

/// <summary>
/// 纯规则：根据「行为」计算精力/时间代价与休息收益，不依赖 Godot 节点。
/// 设计原则：玩家的每个有意义行为都应消耗或恢复可感知的生存状态。
/// </summary>
public static class SurvivalRules
{
	/// <summary>饱食、口渴过低时无法安心休息。</summary>
	public static bool CanRest(PlayerState state) =>
		state.Energy < state.MaxEnergy && state.Hunger >= 10 && state.Thirst >= 10;

	/// <summary>
	/// 探索精力消耗：地点配置 ExploreCost × 天气 × 敏捷特质(ExploreSpeed) × 探索技能。
	/// </summary>
	public static int GetExploreEnergyCost(
		LocationData location,
		WeatherType weather,
		PlayerState state,
		float exploreSkillBonus)
	{
		var cost = (float)Math.Max(1, location.ExploreCost);
		if (weather == WeatherType.Foggy)
			cost *= 1.5f;
		if (state.ExploreSpeed > 0.01f)
			cost /= state.ExploreSpeed;
		cost /= Math.Max(1f, exploreSkillBonus);
		return Math.Max(1, (int)Math.Round(cost));
	}

	/// <summary>移动基础精力（再乘体脂与特质移动系数）。</summary>
	public static int GetMoveEnergyCost(PlayerState state) =>
		Math.Max(1, (int)MathF.Round(15f * state.MoveEnergyCostMultiplier * GetBodyFatMoveMultiplier(state)));

	private static float GetBodyFatMoveMultiplier(PlayerState state)
	{
		var x = Math.Clamp(state.BodyFatIndex - 22, 0, 45);
		return 1f + x * 0.0035f;
	}

	/// <summary>
	/// 休息恢复的精力：庇护等级 × 时段恢复倍率（TimeSystem）× 夜晚加成。
	/// </summary>
	public static int GetRestEnergyGain(
		int shelterTierEnergy,
		bool isNight,
		float timeOfDayRestoreRate)
	{
		var gain = (int)Math.Round(shelterTierEnergy * timeOfDayRestoreRate);
		if (isNight)
			gain = (int)Math.Round(gain * 1.5f);
		return Math.Max(1, gain);
	}

	/// <summary>庇护建筑对应的休息精力基数。</summary>
	public static int GetShelterRestEnergy(
		bool stoneHouse,
		bool house,
		bool tentOrCard) => stoneHouse ? 60 : house ? 45 : tentOrCard ? 30 : 20;

	/// <summary>对应 i18n shelter.* 键。</summary>
	public static string GetShelterMessageKey(bool stoneHouse, bool house, bool tentOrCard) =>
		stoneHouse ? "shelter.stone_house" : house ? "shelter.house" : tentOrCard ? "shelter.tent" : "shelter.wild";
}
