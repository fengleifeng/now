using System.Collections.Generic;
using CardSurvival.Domain;

namespace CardSurvival.UI.Menu;

/// <summary>
/// 特质选择页按钮名与 <see cref="CharacterTrait"/> 的映射，与场景节点名解耦业务枚举。
/// </summary>
public static class TraitSelectionMap
{
	private static readonly Dictionary<string, CharacterTrait> Map = new()
	{
		{ "Trait1", CharacterTrait.Strong },
		{ "Trait2", CharacterTrait.Agile },
		{ "Trait3", CharacterTrait.Wise },
		{ "Trait4", CharacterTrait.Survivalist },
		{ "Trait5", CharacterTrait.Healer },
		{ "Trait6", CharacterTrait.Hunter },
		{ "Trait7", CharacterTrait.Carpenter },
		{ "Trait8", CharacterTrait.Explorer }
	};

	/// <summary>根据场景里特质按钮的 Name 解析对应特质。</summary>
	public static CharacterTrait FromButtonName(StringName buttonName) =>
		Map[buttonName];

	/// <summary>特质对应的 i18n 键后缀（trait.strong 等）。</summary>
	public static string TraitDisplayKey(CharacterTrait trait) =>
		$"trait.{trait.ToString().ToLowerInvariant()}";
}
