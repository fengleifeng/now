// scripts/data/LocationData.cs
using System.Collections.Generic;

namespace CardSurvival.Data;

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
