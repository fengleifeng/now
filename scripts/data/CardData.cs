// scripts/data/CardData.cs
using System.Text.Json.Serialization;

namespace CardSurvival.Data;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CardType { Resource, Creature, Tool, Weapon, Building, Status, Event, Location, Container, Seed }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CardTag
{
	// 基础材料
	Wood, Stone, Material, Plant,
	// 食物
	Food, Cooked, Raw, Meat, Berry,
	// 工具与武器
	Tool, Weapon, Sharp, Heavy,
	// 生存
	Fire, Shelter, Warm,
	// 动物
	Animal, Danger, Prey, Aquatic,
	// 状态
	Medicine, Heal, Poison, Disease,
	// 建筑
	Building, Permanent, Container,
	// 环境
	Nature, Water, Location,
	// 新增
	Metal, Cloth, Trap, Light, Farm
}

public class CardData
{
	public string Id { get; set; } = "";
	public string Name { get; set; } = "";
	public CardType Type { get; set; }
	public int Stack { get; set; } = 1;
	public int MaxStack { get; set; } = 1;
	public List<CardTag> Tags { get; set; } = new();
	public int Durability { get; set; } = -1;
	public int Weight { get; set; } = 1;
	public string Description { get; set; } = "";
	/// <summary>可选：res:// 下的纹理路径，用于卡牌立绘/图标。</summary>
	public string IconPath { get; set; } = "";
	public bool IsDraggable { get; set; } = true;

	/// <summary>手牌中已暂存于某条配方、不可直接使用或拖动的材料（不入 cards.json）。</summary>
	[System.Text.Json.Serialization.JsonIgnore]
	public bool IsStagedIngredient { get; set; }

	// === 操作耗时（游戏分钟，0 表示使用 game_settings 中 DefaultActionMinutes 对应默认值）===
	public int EatMinutes { get; set; }
	public int DrinkMinutes { get; set; }
	public int UseMinutes { get; set; }
	public int DiscardMinutes { get; set; }
	public int ViewMinutes { get; set; }
	public int PickupSceneMinutes { get; set; }
	public int DropToSceneMinutes { get; set; }
	/// <summary>作为合成/配方产物出现时贡献的耗时（分钟）；多产物时与规则 TimeMinutes 叠加见 CombineSystem。</summary>
	public int CombineMinutes { get; set; }

	// 生存属性
	public int FoodValue { get; set; } = 0;
	public int HealValue { get; set; } = 0;
	public int ThirstValue { get; set; } = 0;   // 新增：解渴值
	/// <summary>进食时增加的蛋白质（0 则按标签启发）。</summary>
	public int ProteinValue { get; set; }
	public int VitaminValue { get; set; }
	public int CarbValue { get; set; }
	/// <summary>进食对体脂倾向的修正，负值偏减脂。</summary>
	public int BodyFatDelta { get; set; }
	public int BurnValue { get; set; } = 0;     // 新增：燃烧值（取暖/烹饪）
	public int ArmorValue { get; set; } = 0;    // 新增：护甲
	public int ToolPower { get; set; } = 0;     // 新增：工具效率等级

	// 使用效果列表 ["warmth+5", "immunity+10", "cure:poison"]
	public List<string> Effects { get; set; } = new();

	// 探索掉落池
	public List<string> ExplorePool { get; set; } = new();
}
