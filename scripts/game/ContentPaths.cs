namespace CardSurvival.Game;

/// <summary>
/// 内置数据资源路径（<c>res://</c>）；加载时用 Godot 的 <c>ProjectSettings.GlobalizePath</c> 转成绝对路径。
/// </summary>
public static class ContentPaths
{
	public const string Achievements = "res://data/achievements.json";
	public const string SceneHandDances = "res://data/scene_hand_dances.json";
	public const string CombineRules = "res://data/combine_rules.json";
	public const string Cards = "res://data/cards.json";
	public const string Projects = "res://data/projects.json";
	public const string Locations = "res://data/locations.json";
	public const string Effects = "res://data/effects.json";
	public const string GameSettingsDefault = "res://data/game_settings.json";
	public const string GameStrings = "res://locale/game.tsv";
}
