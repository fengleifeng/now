using System.Collections.Generic;
using System.Linq;
using CardSurvival.Data;

namespace CardSurvival.Game;

/// <summary>
/// 场景手牌互动规则表（数据 + 查询），与具体生存结算分离。
/// </summary>
public sealed class SceneHandDanceCatalog
{
	private readonly IReadOnlyList<SceneHandDanceRule> _rules;

	private SceneHandDanceCatalog(IReadOnlyList<SceneHandDanceRule> rules) => _rules = rules;

	public static SceneHandDanceCatalog Load(string absolutePath) =>
		new(DataLoader.LoadSceneHandDances(absolutePath));

	public SceneHandDanceRule? Match(string locationId, string toolCardId) =>
		_rules.FirstOrDefault(r => r.LocationId == locationId && r.ToolCardIds.Contains(toolCardId));
}
