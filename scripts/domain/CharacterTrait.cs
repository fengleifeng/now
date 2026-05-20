namespace CardSurvival.Domain;

/// <summary>
/// 开局可选角色特质。与 UI 无关，供 PlayerSystem、存档与特质选择页共用。
/// </summary>
public enum CharacterTrait
{
	Strong,
	Agile,
	Wise,
	Survivalist,
	Healer,
	Hunter,
	Carpenter,
	Explorer
}
