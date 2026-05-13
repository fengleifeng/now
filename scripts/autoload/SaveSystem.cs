using Godot;
using CardSurvival.Data;
using CardSurvival.UI;
using CardSurvival;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// 存档：快速槽 <c>user://save_game.dat</c>、每局每日自动备份（最多 10 份）、用户备份目录无上限。
/// </summary>
public partial class SaveSystem : Node
{
    public const int SaveFormatVersion = 2;
    public const int MaxDailyBackupsPerSession = 10;

    private const string QuickSavePath = "user://save_game.dat";
    private const string SessionsRoot = "user://saves/sessions";
    private const string UserBackupsRoot = "user://saves/user";

    private string _sessionId = "";

    public override void _Ready()
    {
        EnsureDirectoryTree(SessionsRoot);
        EnsureDirectoryTree(UserBackupsRoot);
        var timeSystem = GetNodeOrNull<TimeSystem>("/root/TimeSystem");
        if (timeSystem != null)
            timeSystem.Connect(TimeSystem.SignalName.OnDayChanged,
                Callable.From((int day) => OnGameDayChangedBackup(day)));
    }

    private void OnGameDayChangedBackup(int day)
    {
        if (string.IsNullOrEmpty(_sessionId)) return;
        try
        {
            var cards = GetNode<CardManager>("/root/CardManager");
            var map = GetNode<MapSystem>("/root/MapSystem");
            var time = GetNode<TimeSystem>("/root/TimeSystem");
            var player = GetNode<PlayerSystem>("/root/PlayerSystem");
            var effects = GetNode<EffectSystem>("/root/EffectSystem");
            var data = BuildSaveData(player.State, cards, map, time, player, effects);
            var json = Json.Stringify(data);
            var folder = $"{SessionsRoot}/{_sessionId}";
            EnsureDirectoryTree(folder);
            var rel = $"{folder}/day_{day.ToString("D6", CultureInfo.InvariantCulture)}.json";
            TryWriteAtomicUserPath(rel, json);
            PruneDailyBackups(folder, MaxDailyBackupsPerSession);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[SaveSystem] 每日备份失败: {e.Message}");
        }
    }

    public void BeginNewPlaySession() =>
        _sessionId = Guid.NewGuid().ToString("N");

    public bool HasSaveFile() =>
        FileAccess.FileExists(ProjectSettings.GlobalizePath(QuickSavePath));

    public void SaveGame(PlayerState state, CardManager cards, MapSystem map, TimeSystem time, PlayerSystem player, EffectSystem effects)
    {
        if (string.IsNullOrEmpty(_sessionId))
            _sessionId = Guid.NewGuid().ToString("N");
        var data = BuildSaveData(state, cards, map, time, player, effects);
        TryWriteAtomicUserPath(QuickSavePath, Json.Stringify(data));
    }

    /// <summary>写入用户备份目录（文件名带时间戳，数量无上限）。</summary>
    public void SaveUserBackup(PlayerState state, CardManager cards, MapSystem map, TimeSystem time, PlayerSystem player, EffectSystem effects, string? label = null)
    {
        if (string.IsNullOrEmpty(_sessionId))
            _sessionId = Guid.NewGuid().ToString("N");
        var data = BuildSaveData(state, cards, map, time, player, effects);
        var safe = SanitizeFileLabel(label);
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var name = string.IsNullOrEmpty(safe) ? $"backup_{stamp}.json" : $"backup_{stamp}_{safe}.json";
        TryWriteAtomicUserPath($"{UserBackupsRoot}/{name}", Json.Stringify(data));
    }

    public bool LoadGame(PlayerState state, CardManager cards, MapSystem map, TimeSystem time, PlayerSystem player, EffectSystem effects)
    {
        var path = ProjectSettings.GlobalizePath(QuickSavePath);
        if (!FileAccess.FileExists(path)) return false;

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        var parsed = Json.ParseString(file.GetAsText());
        if (parsed.VariantType != Variant.Type.Dictionary) return false;

        var data = new Godot.Collections.Dictionary<string, Variant>((Godot.Collections.Dictionary)parsed);
        ApplySaveData(data, state, cards, map, time, player, effects);
        return true;
    }

    private Godot.Collections.Dictionary<string, Variant> BuildSaveData(
        PlayerState state,
        CardManager cards,
        MapSystem map,
        TimeSystem time,
        PlayerSystem player,
        EffectSystem effects)
    {
        var handDetailed = new Godot.Collections.Array();
        foreach (var card in cards.GetHand())
            handDetailed.Add(CardManager.PackCardRuntime(card));

        var scenesDetailed = new Godot.Collections.Dictionary<string, Variant>();
        foreach (var loc in map.GetAllLocations())
        {
            var arr = new Godot.Collections.Array();
            foreach (var card in map.GetSceneCards(loc.Key))
                arr.Add(CardManager.PackCardRuntime(card));
            scenesDetailed[loc.Key] = arr;
        }

        var envDetailed = new Godot.Collections.Dictionary<string, Variant>();
        foreach (var loc in map.GetAllLocations())
        {
            var arr = new Godot.Collections.Array();
            foreach (var card in map.GetEnvironmentCards(loc.Key))
                arr.Add(CardManager.PackCardRuntime(card));
            envDetailed[loc.Key] = arr;
        }

        var regen = new Godot.Collections.Dictionary<string, Variant>();
        foreach (var kv in map.ExportEnvRegenForSave())
            regen[kv.Key] = kv.Value;

        var projects = new Godot.Collections.Array();
        foreach (var project in state.ActiveProjects)
        {
            projects.Add(new Godot.Collections.Dictionary<string, Variant>
            {
                { "Id", project.Id },
                { "Progress", project.Progress },
                { "Required", project.Required }
            });
        }

        var completed = new Godot.Collections.Array();
        foreach (var id in state.CompletedBuildings)
            completed.Add(id);

        var learned = new Godot.Collections.Array();
        foreach (var id in state.LearnedRecipes)
            learned.Add(id);

        var traits = new Godot.Collections.Array();
        foreach (var t in player.Traits)
            traits.Add(t.ToString());

        return new Godot.Collections.Dictionary<string, Variant>
        {
            { "SaveFormatVersion", SaveFormatVersion },
            { "SessionId", _sessionId },
            { "Health", state.Health },
            { "MaxHealth", state.MaxHealth },
            { "Hunger", state.Hunger },
            { "MaxHunger", state.MaxHunger },
            { "Thirst", state.Thirst },
            { "MaxThirst", state.MaxThirst },
            { "Energy", state.Energy },
            { "MaxEnergy", state.MaxEnergy },
            { "Sanity", state.Sanity },
            { "MaxSanity", state.MaxSanity },
            { "Immunity", state.Immunity },
            { "MaxImmunity", state.MaxImmunity },
            { "MaxWeight", state.MaxWeight },
            { "Temperature", state.Temperature },
            { "MinTemperature", state.MinTemperature },
            { "MaxTemperature", state.MaxTemperature },
            { "HungerRate", state.HungerRate },
            { "ThirstRate", state.ThirstRate },
            { "HealBonus", state.HealBonus },
            { "CombineBonus", state.CombineBonus },
            { "HuntBonus", state.HuntBonus },
            { "ExploreSpeed", state.ExploreSpeed },
            { "BuildSpeed", state.BuildSpeed },
            { "DiscoverBonus", state.DiscoverBonus },
            { "MoveEnergyCostMultiplier", state.MoveEnergyCostMultiplier },
            { "SanityDrainRate", state.SanityDrainRate },
            { "CurrentLocation", map.CurrentLocation },
            { "CurrentDay", time.CurrentDay },
            { "DayProgress", time.DayProgress },
            { "Season", time.CurrentSeason },
            { "Weather", (int)time.CurrentWeather },
            { "HandDetailed", handDetailed },
            { "ScenesDetailed", scenesDetailed },
            { "EnvironmentsDetailed", envDetailed },
            { "EnvRegen", regen },
            { "ActiveEffects", effects.SerializeActiveEffects() },
            { "Projects", projects },
            { "Completed", completed },
            { "Learned", learned },
            { "GatherLevel", state.GatherLevel },
            { "HuntLevel", state.HuntLevel },
            { "CookLevel", state.CookLevel },
            { "BuildLevel", state.BuildLevel },
            { "ExploreLevel", state.ExploreLevel },
            { "FightLevel", state.FightLevel },
            { "BodyFatIndex", state.BodyFatIndex },
            { "Protein", state.Protein },
            { "Vitamins", state.Vitamins },
            { "Carbohydrate", state.Carbohydrate },
            { "Traits", traits }
        };
    }

    private void ApplySaveData(
        Godot.Collections.Dictionary<string, Variant> data,
        PlayerState state,
        CardManager cards,
        MapSystem map,
        TimeSystem time,
        PlayerSystem player,
        EffectSystem effects)
    {
        cards.ClearRuntimeCards();
        map.PrepareWorldForSaveLoad();
        effects.ClearAll();

        var version = GetInt(data, "SaveFormatVersion", 1);
        _sessionId = data.ContainsKey("SessionId") ? (string)data["SessionId"] : Guid.NewGuid().ToString("N");

        var traits = new List<CharacterTrait>();
        if (data.ContainsKey("Traits"))
        {
            foreach (var v in (Godot.Collections.Array)data["Traits"])
            {
                var s = (string)v;
                if (Enum.TryParse<CharacterTrait>(s, out var tr))
                    traits.Add(tr);
            }
        }

        player.SetTraits(traits);

        state.Health = GetInt(data, "Health", 100);
        state.MaxHealth = GetInt(data, "MaxHealth", state.MaxHealth);
        state.Hunger = GetInt(data, "Hunger", 100);
        state.MaxHunger = GetInt(data, "MaxHunger", state.MaxHunger);
        state.Thirst = GetInt(data, "Thirst", 100);
        state.MaxThirst = GetInt(data, "MaxThirst", state.MaxThirst);
        state.Energy = GetInt(data, "Energy", 100);
        state.MaxEnergy = GetInt(data, "MaxEnergy", state.MaxEnergy);
        state.Sanity = GetInt(data, "Sanity", 100);
        state.MaxSanity = GetInt(data, "MaxSanity", state.MaxSanity);
        state.Immunity = GetInt(data, "Immunity", 100);
        state.MaxImmunity = GetInt(data, "MaxImmunity", state.MaxImmunity);
        state.MaxWeight = GetInt(data, "MaxWeight", state.MaxWeight);
        state.Temperature = GetInt(data, "Temperature", 25);
        state.MinTemperature = GetInt(data, "MinTemperature", state.MinTemperature);
        state.MaxTemperature = GetInt(data, "MaxTemperature", state.MaxTemperature);

        if (data.ContainsKey("HungerRate")) state.HungerRate = GetFloat(data, "HungerRate", 1f);
        if (data.ContainsKey("ThirstRate")) state.ThirstRate = GetFloat(data, "ThirstRate", 1f);
        if (data.ContainsKey("HealBonus")) state.HealBonus = GetFloat(data, "HealBonus", 0f);
        if (data.ContainsKey("CombineBonus")) state.CombineBonus = GetFloat(data, "CombineBonus", 0f);
        if (data.ContainsKey("HuntBonus")) state.HuntBonus = GetFloat(data, "HuntBonus", 0f);
        if (data.ContainsKey("ExploreSpeed")) state.ExploreSpeed = GetFloat(data, "ExploreSpeed", 1f);
        if (data.ContainsKey("BuildSpeed")) state.BuildSpeed = GetFloat(data, "BuildSpeed", 1f);
        if (data.ContainsKey("DiscoverBonus")) state.DiscoverBonus = GetFloat(data, "DiscoverBonus", 0f);
        if (data.ContainsKey("MoveEnergyCostMultiplier"))
            state.MoveEnergyCostMultiplier = GetFloat(data, "MoveEnergyCostMultiplier", 1f);
        if (data.ContainsKey("SanityDrainRate")) state.SanityDrainRate = GetFloat(data, "SanityDrainRate", 1f);

        var season = GetString(data, "Season", "spring");
        var weather = data.ContainsKey("Weather")
            ? (WeatherType)Math.Clamp((int)data["Weather"], 0, (int)WeatherType.Foggy)
            : WeatherType.Sunny;
        time.ApplyLoadedTimeAndWeather(GetFloat(data, "DayProgress", 0f), GetInt(data, "CurrentDay", 1), season, weather);

        var location = GetString(data, "CurrentLocation", "forest");
        map.MoveToLocation(location);
        state.CurrentLocation = location;

        if (version >= 2 && data.ContainsKey("HandDetailed"))
            LoadHandDetailed(cards, (Godot.Collections.Array)data["HandDetailed"]);
        else if (data.ContainsKey("Hand"))
            LoadHandLegacy(cards, (Godot.Collections.Array)data["Hand"]);

        if (version >= 2 && data.ContainsKey("ScenesDetailed"))
            LoadScenesDetailed(cards, map, (Godot.Collections.Dictionary)data["ScenesDetailed"]);
        else if (data.ContainsKey("Scenes"))
            LoadScenesLegacy(cards, map, (Godot.Collections.Dictionary)data["Scenes"]);

        if (version >= 2 && data.ContainsKey("EnvironmentsDetailed"))
            LoadEnvironmentsDetailed(cards, map, (Godot.Collections.Dictionary)data["EnvironmentsDetailed"]);
        else
            map.RestoreDefaultEnvironmentIfEmpty();

        if (version >= 2 && data.ContainsKey("EnvRegen"))
            map.ImportEnvRegenFromSave((Godot.Collections.Dictionary)data["EnvRegen"]);

        if (version >= 2 && data.ContainsKey("ActiveEffects"))
            effects.DeserializeActiveEffects((Godot.Collections.Array)data["ActiveEffects"]);

        state.ActiveProjects.Clear();
        if (data.ContainsKey("Projects"))
        {
            foreach (var value in (Godot.Collections.Array)data["Projects"])
            {
                var item = new Godot.Collections.Dictionary<string, Variant>((Godot.Collections.Dictionary)value);
                state.ActiveProjects.Add(new ProjectState
                {
                    Id = (string)item["Id"],
                    Progress = (int)item["Progress"],
                    Required = (int)item["Required"]
                });
            }
        }

        state.CompletedBuildings.Clear();
        if (data.ContainsKey("Completed"))
            foreach (var id in (Godot.Collections.Array)data["Completed"])
                state.CompletedBuildings.Add((string)id);

        state.LearnedRecipes.Clear();
        if (data.ContainsKey("Learned"))
            foreach (var id in (Godot.Collections.Array)data["Learned"])
                state.LearnedRecipes.Add((string)id);

        state.GatherLevel = GetInt(data, "GatherLevel", 1);
        state.HuntLevel = GetInt(data, "HuntLevel", 1);
        state.CookLevel = GetInt(data, "CookLevel", 1);
        state.BuildLevel = GetInt(data, "BuildLevel", 1);
        state.ExploreLevel = GetInt(data, "ExploreLevel", 1);
        state.FightLevel = GetInt(data, "FightLevel", 1);

        state.BodyFatIndex = GetInt(data, "BodyFatIndex", 22);
        state.Protein = GetInt(data, "Protein", 80);
        state.Vitamins = GetInt(data, "Vitamins", 80);
        state.Carbohydrate = GetInt(data, "Carbohydrate", 72);

        map.EmitSceneItemsChangedAllLocations();
    }

    private static void LoadHandDetailed(CardManager cards, Godot.Collections.Array arr)
    {
        foreach (var item in arr)
        {
            if (item.VariantType != Variant.Type.Dictionary) continue;
            var d = new Godot.Collections.Dictionary<string, Variant>((Godot.Collections.Dictionary)item);
            if (!d.ContainsKey("Id")) continue;
            var card = cards.CreateCardInstance((string)d["Id"]);
            if (card == null) continue;
            CardManager.ApplyCardRuntime(card, d);
            cards.AddCardToHand(card);
        }
    }

    private static void LoadHandLegacy(CardManager cards, Godot.Collections.Array hand)
    {
        foreach (var cardId in hand)
        {
            var card = cards.CreateCardInstance((string)cardId);
            if (card != null) cards.AddCardToHand(card);
        }
    }

    private static void LoadScenesDetailed(CardManager cards, MapSystem map, Godot.Collections.Dictionary scenes)
    {
        foreach (var kv in scenes)
        {
            var locId = kv.Key.ToString();
            if (kv.Value.VariantType != Variant.Type.Array) continue;
            foreach (var item in (Godot.Collections.Array)kv.Value)
            {
                if (item.VariantType != Variant.Type.Dictionary) continue;
                var d = new Godot.Collections.Dictionary<string, Variant>((Godot.Collections.Dictionary)item);
                if (!d.ContainsKey("Id")) continue;
                var card = cards.CreateCardInstance((string)d["Id"]);
                if (card == null) continue;
                CardManager.ApplyCardRuntime(card, d);
                map.AddCardToScene(locId, card, false);
            }
        }
    }

    private static void LoadScenesLegacy(CardManager cards, MapSystem map, Godot.Collections.Dictionary scenes)
    {
        foreach (var kv in scenes)
        {
            var ids = (Godot.Collections.Array)kv.Value;
            foreach (var id in ids)
            {
                var card = cards.CreateCardInstance((string)id);
                if (card != null) map.AddCardToScene((string)kv.Key, card, false);
            }
        }
    }

    private static void LoadEnvironmentsDetailed(CardManager cards, MapSystem map, Godot.Collections.Dictionary env)
    {
        foreach (var kv in env)
        {
            var locId = kv.Key.ToString();
            if (kv.Value.VariantType != Variant.Type.Array) continue;
            foreach (var item in (Godot.Collections.Array)kv.Value)
            {
                if (item.VariantType != Variant.Type.Dictionary) continue;
                var d = new Godot.Collections.Dictionary<string, Variant>((Godot.Collections.Dictionary)item);
                if (!d.ContainsKey("Id")) continue;
                var card = cards.CreateCardInstance((string)d["Id"]);
                if (card == null) continue;
                CardManager.ApplyCardRuntime(card, d);
                map.AddEnvironmentCardForSave(locId, card);
            }
        }
    }

    private static void EnsureDirectoryTree(string userPath)
    {
        var abs = ProjectSettings.GlobalizePath(userPath);
        if (DirAccess.DirExistsAbsolute(abs)) return;
        DirAccess.MakeDirRecursiveAbsolute(abs);
    }

    private static bool TryWriteAtomicUserPath(string userPath, string content)
    {
        var final = ProjectSettings.GlobalizePath(userPath);
        var tmp = final + ".tmp";
        {
            using var w = FileAccess.Open(tmp, FileAccess.ModeFlags.Write);
            if (!w.IsOpen()) return false;
            w.StoreString(content);
        }

        if (FileAccess.FileExists(final))
            DirAccess.RemoveAbsolute(final);
        return DirAccess.RenameAbsolute(tmp, final) == Error.Ok;
    }

    private static int ParseDayBackupFileName(string fileName)
    {
        // day_000042.json
        const string prefix = "day_";
        if (!fileName.StartsWith(prefix, StringComparison.Ordinal) || !fileName.EndsWith(".json", StringComparison.Ordinal))
            return -1;
        var inner = fileName.Substring(prefix.Length, fileName.Length - prefix.Length - ".json".Length);
        return int.TryParse(inner, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : -1;
    }

    private static void PruneDailyBackups(string userFolder, int maxKeep)
    {
        var abs = ProjectSettings.GlobalizePath(userFolder);
        if (!DirAccess.DirExistsAbsolute(abs)) return;
        using var dir = DirAccess.Open(abs);
        if (dir == null) return;
        dir.ListDirBegin();
        var files = new List<(int Day, string Name)>();
        while (true)
        {
            var n = dir.GetNext();
            if (string.IsNullOrEmpty(n)) break;
            var day = ParseDayBackupFileName(n);
            if (day < 0) continue;
            files.Add((day, n));
        }

        dir.ListDirEnd();
        files.Sort((a, b) => a.Day.CompareTo(b.Day));
        while (files.Count > maxKeep)
        {
            var victim = files[0].Name;
            files.RemoveAt(0);
            DirAccess.RemoveAbsolute($"{abs}/{victim}");
        }
    }

    private static string SanitizeFileLabel(string? label)
    {
        if (string.IsNullOrWhiteSpace(label)) return "";
        var sb = new StringBuilder(label.Length);
        foreach (var ch in label.Trim())
        {
            if (char.IsLetterOrDigit(ch) || ch is '_' or '-')
                sb.Append(ch);
            if (sb.Length >= 32) break;
        }

        return sb.ToString();
    }

    private static int GetInt(Godot.Collections.Dictionary<string, Variant> data, string key, int defaultValue) =>
        data.ContainsKey(key) ? (int)data[key] : defaultValue;

    private static float GetFloat(Godot.Collections.Dictionary<string, Variant> data, string key, float defaultValue) =>
        data.ContainsKey(key) ? (float)data[key] : defaultValue;

    private static string GetString(Godot.Collections.Dictionary<string, Variant> data, string key, string defaultValue) =>
        data.ContainsKey(key) ? (string)data[key] : defaultValue;
}
