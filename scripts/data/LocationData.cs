// scripts/data/LocationData.cs
using System;
using System.Collections.Generic;

namespace CardSurvival.Data;

/// <summary>
/// 某地区一种环境资源（树木、岩石等）：上限、初始数量、每日再生（基础 × 倍率）均由 <c>locations.json</c> 配置。
/// </summary>
public class LocationEnvironmentResource
{
	public string CardId { get; set; } = "";

	/// <summary>该地区该资源叠加上限（张独立卡牌实例数）。</summary>
	public int Max { get; set; }

	/// <summary>新游戏时的数量；未配置时与 <see cref="Max"/> 相同。</summary>
	public int? Initial { get; set; }

	/// <summary>每日再生基数，与 <see cref="RegenMultiplier"/> 相乘为实际日产出（可小数，累积满 1 生成一张）。</summary>
	public float RegenPerDay { get; set; }

	/// <summary>再生倍率（地区/资源调参）。</summary>
	public float RegenMultiplier { get; set; } = 1f;

	public int GetInitialCount()
	{
		var cap = Math.Max(0, Max);
		if (!Initial.HasValue) return cap;
		return Math.Clamp(Initial.Value, 0, cap);
	}

	public float GetEffectiveRegenPerDay() => Math.Max(0f, RegenPerDay) * Math.Max(0f, RegenMultiplier);
}

public class LocationData
{
	public string Id { get; set; } = "";
	public string Name { get; set; } = "";
	public string Icon { get; set; } = "";
	public string Description { get; set; } = "";

	// 默认探索池（任何季节）
	public List<string> ExplorePool { get; set; } = new();

	// 新增：季节性探索池
	public List<string> SpringPool { get; set; } = new();
	public List<string> SummerPool { get; set; } = new();
	public List<string> AutumnPool { get; set; } = new();
	public List<string> WinterPool { get; set; } = new();

	public List<string> Connections { get; set; } = new();
	public string SpecialEffect { get; set; } = "";

	// 新增：探索消耗（精力基数）
	public int ExploreCost { get; set; } = 10;

	/// <summary>环境区资源：种类、上限、初始量、再生（见 <see cref="LocationEnvironmentResource"/>）。</summary>
	public List<LocationEnvironmentResource> EnvironmentResources { get; set; } = new();

	/// <summary>
	/// 根据季节获取探索池
	/// </summary>
	public List<string> GetExplorePoolForSeason(string season)
	{
		var pool = season switch
		{
			"spring" => SpringPool.Count > 0 ? SpringPool : ExplorePool,
			"summer" => SummerPool.Count > 0 ? SummerPool : ExplorePool,
			"autumn" => AutumnPool.Count > 0 ? AutumnPool : ExplorePool,
			"winter" => WinterPool.Count > 0 ? WinterPool : ExplorePool,
			_ => ExplorePool
		};
		if (pool.Count == 0) pool = ExplorePool;
		return pool;
	}
}
