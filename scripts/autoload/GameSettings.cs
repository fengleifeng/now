using System;
using System.Collections.Generic;
using System.Text.Json;
using CardSurvival.Data;
using CardSurvival.Game;
using Godot;

/// <summary>
/// 全局游戏配置：默认读取 <c>res://data/game_settings.json</c>，若存在 <c>user://game_settings.json</c> 则覆盖。
/// 菜单内修改会写入 user 路径。
/// </summary>
public partial class GameSettings : Node
{
	private const string ResSettingsPath = "res://data/game_settings.json";
	private const string UserSettingsPath = "user://game_settings.json";

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true,
		WriteIndented = true
	};

	private GameSettingsData _data = new();
	private readonly Dictionary<string, float> _defaultActionMinutes = new();
	private AudioStreamPlayer? _player;
	private readonly System.Collections.Generic.Dictionary<CardUiSoundKind, AudioStreamWav> _streams = new();

	public bool CardActionSoundsEnabled
	{
		get => _data.CardActionSoundsEnabled;
		set
		{
			_data.CardActionSoundsEnabled = value;
			Save();
		}
	}

	public float CardActionSoundVolumeDb
	{
		get => _data.CardActionSoundVolumeDb;
		set
		{
			_data.CardActionSoundVolumeDb = value;
			Save();
		}
	}

	public override void _Ready()
	{
		Reload();
		_player = new AudioStreamPlayer
		{
			Name = "CardUiSounds",
			Bus = "Master",
			MaxPolyphony = 4
		};
		AddChild(_player);
		BuildStreams();
	}

	/// <summary>重新从磁盘加载（主菜单进入时可调用以应用手动改过的 JSON）。</summary>
	public void Reload()
	{
		try
		{
			if (FileAccess.FileExists(UserSettingsPath))
				_data = DeserializeOrDefault(ReadAllText(UserSettingsPath));
			else if (FileAccess.FileExists(ResSettingsPath))
				_data = DeserializeOrDefault(ReadAllText(ResSettingsPath));
			else
				_data = new GameSettingsData();
		}
		catch (Exception e)
		{
			GD.PrintErr($"[GameSettings] 加载失败，使用内置默认: {e.Message}");
			_data = new GameSettingsData();
		}

		RebuildDefaultActionMinutes();
	}

	/// <summary>某类操作在卡牌未单独配置时的默认耗时（游戏分钟）。</summary>
	public float GetDefaultActionMinutes(string key) =>
		_defaultActionMinutes.TryGetValue(key, out var v) ? v : 10f;

	/// <summary>按操作类型与卡牌可选字段解析耗时（分钟）。</summary>
	public float ResolveActionMinutes(string key, CardData card) => key switch
	{
		"Eat" => card.EatMinutes > 0 ? card.EatMinutes : GetDefaultActionMinutes("Eat"),
		"Drink" => card.DrinkMinutes > 0 ? card.DrinkMinutes : GetDefaultActionMinutes("Drink"),
		"Use" => card.UseMinutes > 0 ? card.UseMinutes : GetDefaultActionMinutes("Use"),
		"Discard" => card.DiscardMinutes > 0 ? card.DiscardMinutes : GetDefaultActionMinutes("Discard"),
		"View" => card.ViewMinutes > 0 ? card.ViewMinutes : GetDefaultActionMinutes("View"),
		"PickupScene" => card.PickupSceneMinutes > 0 ? card.PickupSceneMinutes : GetDefaultActionMinutes("PickupScene"),
		"DropToScene" => card.DropToSceneMinutes > 0 ? card.DropToSceneMinutes : GetDefaultActionMinutes("DropToScene"),
		_ => GetDefaultActionMinutes(key)
	};

	public void AdvanceTimeForActionMinutes(TimeSystem time, string key, CardData? card = null)
	{
		var minutes = card == null ? GetDefaultActionMinutes(key) : ResolveActionMinutes(key, card);
		time.AdvanceTime(SurviveTime.MinutesToDayFraction(minutes));
	}

	private void RebuildDefaultActionMinutes()
	{
		_defaultActionMinutes.Clear();
		foreach (var kv in BuiltinDefaultActionMinutes)
			_defaultActionMinutes[kv.Key] = kv.Value;
		if (_data.DefaultActionMinutes != null)
		{
			foreach (var kv in _data.DefaultActionMinutes)
			{
				if (kv.Value >= 0f)
					_defaultActionMinutes[kv.Key] = kv.Value;
			}
		}
	}

	private static readonly Dictionary<string, float> BuiltinDefaultActionMinutes = new()
	{
		["Eat"] = 10f,
		["Drink"] = 6f,
		["Use"] = 12f,
		["Discard"] = 4f,
		["View"] = 3f,
		["CombineHand"] = 15f,
		["MultiCombine"] = 15f,
		["CraftRecipe"] = 15f,
		["PickupScene"] = 10f,
		["DropToScene"] = 10f,
		["BuildProjectStep"] = 20f,
		["CombineFail"] = 5f
	};

	public void Save()
	{
		try
		{
			var json = JsonSerializer.Serialize(_data, JsonOptions);
			using var f = FileAccess.Open(UserSettingsPath, FileAccess.ModeFlags.Write);
			if (f.IsOpen())
				f.StoreString(json);
		}
		catch (Exception e)
		{
			GD.PrintErr($"[GameSettings] 保存失败: {e.Message}");
		}
	}

	public void PlayCardUiSound(CardUiSoundKind kind)
	{
		if (!CardActionSoundsEnabled || _player == null) return;
		if (!_streams.TryGetValue(kind, out var stream)) return;
		_player.Stream = stream;
		_player.VolumeDb = CardActionSoundVolumeDb;
		_player.Play();
	}

	private static string ReadAllText(string path)
	{
		using var f = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		return f.IsOpen() ? f.GetAsString() : "{}";
	}

	private static GameSettingsData DeserializeOrDefault(string json)
	{
		try
		{
			return JsonSerializer.Deserialize<GameSettingsData>(json, JsonOptions) ?? new GameSettingsData();
		}
		catch
		{
			return new GameSettingsData();
		}
	}

	private void BuildStreams()
	{
		_streams[CardUiSoundKind.Tap] = CreateTone(620, 0.035, 2600);
		_streams[CardUiSoundKind.Pickup] = CreateTone(880, 0.045, 3200);
		_streams[CardUiSoundKind.Drop] = CreateTone(380, 0.05, 2400);
		_streams[CardUiSoundKind.CombineSuccess] = CreateTone(1040, 0.055, 3400);
		_streams[CardUiSoundKind.CombineFail] = CreateTone(180, 0.12, 2200, 90);
		_streams[CardUiSoundKind.Use] = CreateTone(760, 0.04, 2800);
		_streams[CardUiSoundKind.Discard] = CreateTone(320, 0.06, 2000);
	}

	private static AudioStreamWav CreateTone(double startHz, double durationSec, short amplitude, double? endHz = null)
	{
		const int rate = 22050;
		var samples = Math.Max(8, (int)(rate * durationSec));
		var data = new byte[samples * 2];
		var end = endHz ?? startHz;
		var sampleDur = (double)samples / rate;
		for (int i = 0; i < samples; i++)
		{
			var t = (double)i / rate;
			var hz = startHz + (end - startHz) * (sampleDur <= 0 ? 0 : t / sampleDur);
			var env = Math.Min(1.0, i / (rate * 0.004)) * Math.Min(1.0, (samples - i) / (double)Math.Max(1, samples / 4));
			var s = (short)(Math.Sin(t * 2 * Math.PI * hz) * amplitude * env);
			data[i * 2] = (byte)(s & 0xff);
			data[i * 2 + 1] = (byte)((s >> 8) & 0xff);
		}

		var wav = new AudioStreamWav
		{
			Format = AudioStreamWav.FormatEnum.Format16Bits,
			MixRate = rate,
			Data = data
		};
		return wav;
	}
}

public sealed class GameSettingsData
{
	public bool CardActionSoundsEnabled { get; set; } = true;
	public float CardActionSoundVolumeDb { get; set; } = -8f;
	/// <summary>各类操作默认耗时（游戏分钟）；1 日 = 1440 分钟。单张卡可用 EatMinutes 等字段覆盖。</summary>
	public Dictionary<string, float>? DefaultActionMinutes { get; set; }
}

public enum CardUiSoundKind
{
	Tap,
	Pickup,
	Drop,
	CombineSuccess,
	CombineFail,
	Use,
	Discard
}
