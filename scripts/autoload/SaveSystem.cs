using Godot;
using CardSurvival.Data;

namespace CardSurvival;

/// <summary>
/// 独立的存档系统。从 GameRoot 拆分出来。
/// </summary>
public partial class SaveSystem : Node
{
    private const string SaveFileName = "user://save_game.dat";

    public bool HasSaveFile()
    {
        return Godot.FileAccess.FileExists(ProjectSettings.GlobalizePath(SaveFileName));
    }

    public void SaveGame(PlayerState state, CardManager cards, MapSystem map, TimeSystem time)
    {
        var hand = new Godot.Collections.Array();
        foreach (var card in cards.GetHand())
            hand.Add(card.Id);

        var scenes = new Godot.Collections.Dictionary<string, Variant>();
        foreach (var loc in map.GetAllLocations())
        {
            var ids = new Godot.Collections.Array();
            foreach (var card in map.GetSceneCards(loc.Key))
                ids.Add(card.Id);
            scenes[loc.Key] = ids;
        }

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

        var data = new Godot.Collections.Dictionary<string, Variant>
        {
            // 核心属性
            { "Health", state.Health },
            { "Hunger", state.Hunger },
            { "Thirst", state.Thirst },
            { "Energy", state.Energy },
            { "Sanity", state.Sanity },
            { "Immunity", state.Immunity },
            { "Temperature", state.Temperature },
            // 位置与时间
            { "CurrentLocation", map.CurrentLocation },
            { "CurrentDay", time.CurrentDay },
            { "DayProgress", time.DayProgress },
            { "Season", time.CurrentSeason },
            // 卡牌
            { "Hand", hand },
            { "Scenes", scenes },
            // 进度
            { "Projects", projects },
            { "Completed", completed },
            { "Learned", learned },
            // 技能
            { "GatherLevel", state.GatherLevel },
            { "HuntLevel", state.HuntLevel },
            { "CookLevel", state.CookLevel },
            { "BuildLevel", state.BuildLevel },
            { "ExploreLevel", state.ExploreLevel },
            { "FightLevel", state.FightLevel },
        };

        using var file = Godot.FileAccess.Open(
            ProjectSettings.GlobalizePath(SaveFileName),
            Godot.FileAccess.ModeFlags.Write);
        file.StoreString(Json.Stringify(data));
    }

    public bool LoadGame(PlayerState state, CardManager cards, MapSystem map, TimeSystem time)
    {
        var path = ProjectSettings.GlobalizePath(SaveFileName);
        if (!Godot.FileAccess.FileExists(path)) return false;

        using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
        var parsed = Json.ParseString(file.GetAsText());
        if (parsed.VariantType == Variant.Type.Nil) return false;

        var data = new Godot.Collections.Dictionary<string, Variant>((Godot.Collections.Dictionary)parsed);

        cards.ClearRuntimeCards();
        map.ResetState();

        // 核心属性（兼容旧存档）
        state.Health = GetInt(data, "Health", 100);
        state.Hunger = GetInt(data, "Hunger", 100);
        state.Thirst = GetInt(data, "Thirst", 100);
        state.Energy = GetInt(data, "Energy", 100);
        state.Sanity = GetInt(data, "Sanity", 100);
        state.Immunity = GetInt(data, "Immunity", 100);
        state.Temperature = GetInt(data, "Temperature", 25);

        // 时间
        time.CurrentDay = GetInt(data, "CurrentDay", 1);
        time.DayProgress = GetFloat(data, "DayProgress", 0f);
        if (data.ContainsKey("Season"))
            time.CurrentSeason = (string)data["Season"];

        // 位置
        var location = GetString(data, "CurrentLocation", "forest");
        map.MoveToLocation(location);
        state.CurrentLocation = location;

        // 手牌
        if (data.ContainsKey("Hand"))
        {
            foreach (var cardId in (Godot.Collections.Array)data["Hand"])
            {
                var card = cards.CreateCardInstance((string)cardId);
                if (card != null) cards.AddCardToHand(card);
            }
        }

        // 清空并恢复场景卡牌
        foreach (var loc in map.GetAllLocations())
            foreach (var card in map.GetSceneCards(loc.Key).ToList())
                map.RemoveCardFromScene(loc.Key, card);

        if (data.ContainsKey("Scenes"))
        {
            var scenes = (Godot.Collections.Dictionary)data["Scenes"];
            foreach (var kv in scenes)
            {
                var ids = (Godot.Collections.Array)kv.Value;
                foreach (var id in ids)
                {
                    var card = cards.CreateCardInstance((string)id);
                    if (card != null) map.AddCardToScene((string)kv.Key, card);
                }
            }
        }

        // 项目进度
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

        // 已建造
        state.CompletedBuildings.Clear();
        if (data.ContainsKey("Completed"))
            foreach (var id in (Godot.Collections.Array)data["Completed"])
                state.CompletedBuildings.Add((string)id);

        // 已学配方
        state.LearnedRecipes.Clear();
        if (data.ContainsKey("Learned"))
            foreach (var id in (Godot.Collections.Array)data["Learned"])
                state.LearnedRecipes.Add((string)id);

        // 技能等级（兼容旧存档）
        state.GatherLevel = GetInt(data, "GatherLevel", 1);
        state.HuntLevel = GetInt(data, "HuntLevel", 1);
        state.CookLevel = GetInt(data, "CookLevel", 1);
        state.BuildLevel = GetInt(data, "BuildLevel", 1);
        state.ExploreLevel = GetInt(data, "ExploreLevel", 1);
        state.FightLevel = GetInt(data, "FightLevel", 1);

        return true;
    }

    // ===== 安全读取辅助方法 =====

    private static int GetInt(Godot.Collections.Dictionary<string, Variant> data, string key, int defaultValue)
    {
        return data.ContainsKey(key) ? (int)data[key] : defaultValue;
    }

    private static float GetFloat(Godot.Collections.Dictionary<string, Variant> data, string key, float defaultValue)
    {
        return data.ContainsKey(key) ? (float)data[key] : defaultValue;
    }

    private static string GetString(Godot.Collections.Dictionary<string, Variant> data, string key, string defaultValue)
    {
        return data.ContainsKey(key) ? (string)data[key] : defaultValue;
    }
}
