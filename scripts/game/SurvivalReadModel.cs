using CardSurvival.Data;

namespace CardSurvival.Game;

/// <summary>
/// 只读生存状态查询，供 UI 显示代价、禁用按钮与提示，不修改玩家数据。
/// </summary>
public static class SurvivalReadModel
{
	/// <summary>当前地点探索所需精力（与 CardSurvivalGame.Explore 一致）。</summary>
	public static int GetExploreEnergyCost(GameServices services, LocationData location) =>
		SurvivalRules.GetExploreEnergyCost(
			location,
			services.Time.CurrentWeather,
			services.Player.State,
			services.Player.GetSkillBonus("explore"));

	/// <summary>是否满足休息条件。</summary>
	public static bool CanRest(PlayerSystem player) => SurvivalRules.CanRest(player.State);

	/// <summary>是否应显示夜仪按钮（与游戏规则一致）。</summary>
	public static bool ShouldOfferNightRitual(GameServices services)
	{
		if (!services.Time.IsNight())
			return false;
		if (!services.Cards.HasCardInHand("campfire") || !services.Cards.HasCardInHand("herb"))
			return false;
		var hasDisease = services.Effects.GetDiseases().Count > 0;
		return hasDisease || services.Player.State.Sanity < 50;
	}
}
