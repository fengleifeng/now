using System;
using System.Collections.Generic;
using CardSurvival.Data;

namespace CardSurvival.Game;

/// <summary>
/// 成就条件判定（纯函数，无 Godot 依赖）。
/// </summary>
public static class AchievementConditionEvaluator
{
	public static bool IsSatisfied(AchievementDef def, PlayerProgression progression) =>
		def.ConditionType switch
		{
			AchievementConditionKind.LifetimeHandGain =>
				progression.LifetimeHandGains.GetValueOrDefault(def.CardId) >= def.RequiredCount,
			AchievementConditionKind.LifetimeExplore =>
				progression.LifetimeExploreCount >= def.RequiredCount,
			_ => false
		};
}

/// <summary>
/// 成就解锁与日志队列（由 <c>AchievementSystem</c> 接线到存档与 <c>CombineSystem.LearnRecipe</c>）。
/// </summary>
public sealed class AchievementEngine
{
	private readonly IReadOnlyList<AchievementDef> _catalog;
	private readonly Queue<string> _logQueue = new();

	public AchievementEngine(IReadOnlyList<AchievementDef> catalog) =>
		_catalog = catalog;

	public bool TryDequeueLog(out string bbcodeLine)
	{
		if (_logQueue.Count == 0)
		{
			bbcodeLine = "";
			return false;
		}

		bbcodeLine = _logQueue.Dequeue();
		return true;
	}

	public void NotifyHandGained(PlayerState state, string cardId, int amount, Action<string> learnRecipe)
	{
		if (amount <= 0) return;
		state.Progression.AddLifetimeHand(cardId, amount);
		Reevaluate(state, learnRecipe);
	}

	public void Reevaluate(PlayerState state, Action<string> learnRecipe)
	{
		foreach (var def in _catalog)
		{
			if (string.IsNullOrEmpty(def.Id)) continue;
			if (state.Progression.UnlockedAchievementIds.Contains(def.Id)) continue;
			if (!AchievementConditionEvaluator.IsSatisfied(def, state.Progression)) continue;
			Unlock(state, def, learnRecipe);
		}
	}

	private void Unlock(PlayerState state, AchievementDef def, Action<string> learnRecipe)
	{
		if (state.Progression.UnlockedAchievementIds.Contains(def.Id)) return;
		state.Progression.UnlockedAchievementIds.Add(def.Id);
		foreach (var key in def.UnlockRecipeKeys)
		{
			if (string.IsNullOrEmpty(key)) continue;
			learnRecipe(key);
		}

		var title = string.IsNullOrEmpty(def.Name) ? def.Id : def.Name;
		var desc = string.IsNullOrEmpty(def.Description) ? "" : $" {def.Description}";
		_logQueue.Enqueue($"[color=gold]成就：{title}[/color]{desc}");
		if (def.UnlockRecipeKeys.Count > 0)
			_logQueue.Enqueue(
				$"[color=cyan]已解锁合成：{string.Join("、", def.UnlockRecipeKeys)}[/color]");
	}
}
