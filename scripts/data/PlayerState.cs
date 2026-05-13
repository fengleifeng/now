// scripts/data/PlayerState.cs
using System.Collections.Generic;

namespace CardSurvival.Data;

public class PlayerState
{
	// === 核心生存属性 ===
	public int Health { get; set; } = 100;
	public int MaxHealth { get; set; } = 100;
	public int Hunger { get; set; } = 100;
	public int MaxHunger { get; set; } = 100;
	public int Thirst { get; set; } = 100;        // 新增：口渴
	public int MaxThirst { get; set; } = 100;     // 新增
	public int Energy { get; set; } = 100;
	public int MaxEnergy { get; set; } = 100;
	public int Sanity { get; set; } = 100;
	public int MaxSanity { get; set; } = 100;
	public int Immunity { get; set; } = 100;      // 新增：免疫力
	public int MaxImmunity { get; set; } = 100;   // 新增

	// === 营养与体成分（0–100，后续角色/特质可改初值与成长）===
	/// <summary>体脂倾向，高值偏肥胖，影响移动精力消耗等。</summary>
	public int BodyFatIndex { get; set; } = 22;
	/// <summary>蛋白质储备，长期过低影响免疫与恢复。</summary>
	public int Protein { get; set; } = 80;
	/// <summary>维生素储备。</summary>
	public int Vitamins { get; set; } = 80;
	/// <summary>碳水化合物（能量底物）。</summary>
	public int Carbohydrate { get; set; } = 72;

	// === 环境 ===
	public int Temperature { get; set; } = 25;
	public int MinTemperature { get; set; } = -10;
	public int MaxTemperature { get; set; } = 40;

	// === 修饰器 ===
	public float HungerRate { get; set; } = 1.0f;
	public float ThirstRate { get; set; } = 1.0f; // 新增：口渴速率
	public float HealBonus { get; set; } = 0f;
	public float CombineBonus { get; set; } = 0f;
	public float HuntBonus { get; set; } = 0f;
	public float ExploreSpeed { get; set; } = 1.0f;
	public float BuildSpeed { get; set; } = 1.0f;
	public float DiscoverBonus { get; set; } = 0f;
	public float MoveEnergyCostMultiplier { get; set; } = 1.0f;
	public float SanityDrainRate { get; set; } = 1.0f; // 新增：精神消耗速率

	public int MaxWeight { get; set; } = 50;

	// === 位置 ===
	public string CurrentLocation { get; set; } = "forest";

	// === 进度 ===
	public List<string> LearnedRecipes { get; set; } = new();
	public List<ProjectState> ActiveProjects { get; set; } = new();
	public List<string> CompletedBuildings { get; set; } = new();
	public PlayerProgression Progression { get; set; } = new();

	// === 技能等级（新增） ===
	public int GatherLevel { get; set; } = 1;     // 采集
	public int HuntLevel { get; set; } = 1;       // 狩猎
	public int CookLevel { get; set; } = 1;       // 烹饪
	public int BuildLevel { get; set; } = 1;      // 建造
	public int ExploreLevel { get; set; } = 1;    // 探索
	public int FightLevel { get; set; } = 1;      // 战斗
}

/// <summary>
/// 玩家身上的活跃状态/疾病效果
/// </summary>
public class ActiveEffect
{
	public string EffectId { get; set; } = "";    // 对应 EffectData.Id
	public int RemainingTurns { get; set; } = 1;  // 剩余持续回合数（-1=无限）
	public int Intensity { get; set; } = 1;       // 强度等级
}
