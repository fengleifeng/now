using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace CardSurvival.Data;

public static class DataLoader
{
	private static readonly JsonSerializerOptions Options = new()
	{
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true,
		Converters = { new JsonStringEnumConverter() }
	};

	public static List<CardData> LoadCards(string path)
	{
		if (!File.Exists(path)) return new List<CardData>();
		var json = File.ReadAllText(path);
		return JsonSerializer.Deserialize<List<CardData>>(json, Options) ?? new List<CardData>();
	}

	public static List<CombineRule> LoadRules(string path)
	{
		if (!File.Exists(path)) return new List<CombineRule>();
		var json = File.ReadAllText(path);
		return JsonSerializer.Deserialize<List<CombineRule>>(json, Options) ?? new List<CombineRule>();
	}

	public static List<LocationData> LoadLocations(string path)
	{
		if (!File.Exists(path)) return new List<LocationData>();
		var json = File.ReadAllText(path);
		return JsonSerializer.Deserialize<List<LocationData>>(json, Options) ?? new List<LocationData>();
	}

	public static List<ProjectData> LoadProjects(string path)
	{
		if (!File.Exists(path)) return new List<ProjectData>();
		var json = File.ReadAllText(path);
		return JsonSerializer.Deserialize<List<ProjectData>>(json, Options) ?? new List<ProjectData>();
	}

	public static List<EffectData> LoadEffects(string path)
	{
		if (!File.Exists(path)) return new List<EffectData>();
		var json = File.ReadAllText(path);
		return JsonSerializer.Deserialize<List<EffectData>>(json, Options) ?? new List<EffectData>();
	}
}
