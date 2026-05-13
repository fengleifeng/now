using System.Linq;
using CardSurvival.Data;

namespace CardSurvival.Game;

/// <summary>
/// 合成配方在「手牌尝试合成」与「合成界面展示」上的解锁策略，与成就/设置解耦。
/// </summary>
public static class RecipeUnlockPolicy
{
	public static bool RevealAllRecipes(GameSettings? settings) =>
		settings != null && settings.RevealAllRecipesInCraftUi;

	/// <summary>手牌两卡/多卡合成是否允许尝试（未开全显时需已学配方键）。</summary>
	public static bool AllowsHandCombine(GameSettings? settings, PlayerState player, string recipeKey) =>
		RevealAllRecipes(settings) || player.LearnedRecipes.Contains(recipeKey);

	/// <summary>合成弹窗列表是否显示该条规则（全显 / 已学键 / 已学任一产物 Id）。</summary>
	public static bool IsVisibleInCraftList(GameSettings? settings, PlayerState player, CombineRule rule,
		string recipeKey) =>
		RevealAllRecipes(settings)
		|| player.LearnedRecipes.Contains(recipeKey)
		|| rule.Results.Any(r => player.LearnedRecipes.Contains(r));
}
