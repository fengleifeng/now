using System.Collections.Generic;

namespace CardSurvival.Data;

/// <summary>
/// 与单局进度相关的统计与成就标记（存档子集）。
/// </summary>
public sealed class PlayerProgression
{
	public Dictionary<string, int> LifetimeHandGains { get; set; } = new();
	public int LifetimeExploreCount { get; set; }
	public List<string> UnlockedAchievementIds { get; set; } = new();

	public void AddLifetimeHand(string cardId, int amount)
	{
		if (amount <= 0) return;
		LifetimeHandGains.TryGetValue(cardId, out var prev);
		LifetimeHandGains[cardId] = prev + amount;
	}

	public void IncrementExplore() => LifetimeExploreCount++;
}
