using Godot;
using CardSurvival.Data;
using System.Collections.Generic;
using System.Linq;

namespace CardSurvival;

public partial class MapSystem : Node
{
    [Signal] public delegate void OnLocationChangedEventHandler(string locationId);
    [Signal] public delegate void OnSceneItemsChangedEventHandler(string locationId);

    private readonly Dictionary<string, LocationData> _locations = new();
    private readonly Dictionary<string, List<CardData>> _sceneItems = new();
    private readonly Dictionary<string, List<CardData>> _environmentItems = new();
    private readonly Dictionary<string, float> _envRegenCarry = new();
    private bool _environmentInitialized;
    public string CurrentLocation { get; private set; } = "forest";

    public override void _Ready()
    {
        var path = ProjectSettings.GlobalizePath("res://data/locations.json");
        var locations = DataLoader.LoadLocations(path);
        foreach (var loc in locations)
        {
            _locations[loc.Id] = loc;
            _sceneItems[loc.Id] = new List<CardData>();
            _environmentItems[loc.Id] = new List<CardData>();
        }

        var time = GetNodeOrNull<TimeSystem>("/root/TimeSystem");
        if (time != null)
            time.Connect(TimeSystem.SignalName.OnDayChanged, Callable.From((int _) => TickEnvironmentRegeneration()));

        GD.Print($"[MapSystem] Loaded {_locations.Count} locations");
    }

    /// <summary>每个游戏日结束时为各地区环境资源补再生（受上限与 JSON 中 RegenPerDay×RegenMultiplier 约束）。</summary>
    public void TickEnvironmentRegeneration()
    {
        EnsureEnvironmentInitialized();
        var cardManager = GetNode<CardManager>("/root/CardManager");
        var anySpawned = false;

        foreach (var loc in _locations.Values)
        {
            var rules = DeduplicateEnvRules(loc);
            if (rules.Count == 0) continue;
            if (!_environmentItems.TryGetValue(loc.Id, out var envList)) continue;

            foreach (var rule in rules)
            {
                var effective = rule.GetEffectiveRegenPerDay();
                if (effective <= 0f) continue;

                var count = envList.Count(c => c.Id == rule.CardId);
                var room = rule.Max - count;
                if (room <= 0) continue;

                var key = RegenKey(loc.Id, rule.CardId);
                var carry = _envRegenCarry.GetValueOrDefault(key, 0f) + effective;
                var spawn = (int)System.Math.Floor(carry);
                if (spawn > room) spawn = room;
                if (spawn <= 0)
                {
                    _envRegenCarry[key] = carry;
                    continue;
                }

                carry -= spawn;
                _envRegenCarry[key] = carry;
                for (var i = 0; i < spawn; i++)
                {
                    var card = cardManager.CreateCardInstance(rule.CardId);
                    if (card != null)
                        envList.Add(card);
                }

                anySpawned = true;
            }
        }

        if (anySpawned)
            EmitSignal(SignalName.OnSceneItemsChanged, CurrentLocation);
    }

    private static string RegenKey(string locationId, string cardId) => $"{locationId}|{cardId}";

    private static List<LocationEnvironmentResource> DeduplicateEnvRules(LocationData loc)
    {
        var list = loc.EnvironmentResources;
        if (list == null || list.Count == 0)
            return new List<LocationEnvironmentResource>();

        return list
            .Where(r => !string.IsNullOrWhiteSpace(r.CardId))
            .GroupBy(r => r.CardId)
            .Select(g =>
            {
                if (g.Count() > 1)
                    GD.PushWarning($"[MapSystem] 地区 {loc.Id} 中资源 {g.Key} 在 EnvironmentResources 中重复，已采用最后一条配置。");
                return g.Last();
            })
            .ToList();
    }

    private void InitializeEnvironmentItems()
    {
        foreach (var items in _environmentItems.Values)
            items.Clear();
        _envRegenCarry.Clear();

        var cardManager = GetNode<CardManager>("/root/CardManager");

        foreach (var loc in _locations.Values)
        {
            if (!_environmentItems.TryGetValue(loc.Id, out var envList)) continue;
            foreach (var rule in DeduplicateEnvRules(loc))
            {
                var n = rule.GetInitialCount();
                AddEnvCard(envList, cardManager, rule.CardId, n);
            }
        }

        _environmentInitialized = true;
    }

    /// <summary>读档前清空场景与环境实例（不填充默认环境）。</summary>
    public void PrepareWorldForSaveLoad()
    {
        foreach (var items in _sceneItems.Values)
            items.Clear();
        foreach (var items in _environmentItems.Values)
            items.Clear();
        _envRegenCarry.Clear();
        _environmentInitialized = true;
    }

    /// <summary>存档未含环境时，按 locations.json 恢复默认环境。</summary>
    public void RestoreDefaultEnvironmentIfEmpty()
    {
        if (_environmentItems.Values.Any(v => v.Count > 0)) return;
        _environmentInitialized = false;
        EnsureEnvironmentInitialized();
    }

    public void ImportEnvRegenFromSave(Godot.Collections.Dictionary? data)
    {
        _envRegenCarry.Clear();
        if (data == null) return;
        foreach (var kv in data)
            _envRegenCarry[kv.Key.ToString()] = (float)kv.Value;
    }

    public Dictionary<string, float> ExportEnvRegenForSave() => new(_envRegenCarry);

    /// <summary>读档时写入环境卡（不触发默认环境生成）。</summary>
    public void AddEnvironmentCardForSave(string locationId, CardData card)
    {
        if (!_environmentItems.TryGetValue(locationId, out var list))
            return;
        list.Add(card);
    }

    private void EnsureEnvironmentInitialized()
    {
        if (_environmentInitialized) return;
        InitializeEnvironmentItems();
    }

    private void AddEnvCard(List<CardData> envList, CardManager cardManager, string cardId, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var card = cardManager.CreateCardInstance(cardId);
            if (card != null)
                envList.Add(card);
        }
    }

    public void AddCompletedBuildingToEnvironment(string buildingId)
    {
        EnsureEnvironmentInitialized();
        var cardManager = GetNode<CardManager>("/root/CardManager");
        var card = cardManager.CreateCardInstance(buildingId);
        if (card != null)
        {
            _environmentItems[CurrentLocation].Add(card);
            EmitSignal(SignalName.OnSceneItemsChanged, CurrentLocation);
        }
    }

    public void RemoveBuildingFromEnvironment(string buildingId)
    {
        var items = _environmentItems[CurrentLocation];
        var building = items.FirstOrDefault(c => c.Id == buildingId);
        if (building != null)
        {
            items.Remove(building);
            EmitSignal(SignalName.OnSceneItemsChanged, CurrentLocation);
        }
    }

    public LocationData? GetLocation(string id)
    {
        return _locations.TryGetValue(id, out var loc) ? loc : null;
    }

    public List<string> GetConnections(string id)
    {
        var loc = GetLocation(id);
        return loc?.Connections ?? new List<string>();
    }

    public LocationData? GetCurrentLocation()
    {
        return GetLocation(CurrentLocation);
    }

    public void MoveToLocation(string id)
    {
        if (!_locations.ContainsKey(id)) return;
        if (CurrentLocation == id) return;
        CurrentLocation = id;
        EmitSignal(SignalName.OnLocationChanged, id);
        GD.Print($"[MapSystem] Moved to {id}");
    }

    public List<CardData> GetSceneCards(string locationId)
    {
        if (_sceneItems.TryGetValue(locationId, out var items))
            return items;
        return new List<CardData>();
    }

    public List<CardData> GetCurrentSceneCards()
    {
        return GetSceneCards(CurrentLocation);
    }

    public List<CardData> GetEnvironmentCards(string locationId)
    {
        EnsureEnvironmentInitialized();
        if (_environmentItems.TryGetValue(locationId, out var items))
            return items;
        return new List<CardData>();
    }

    public List<CardData> GetCurrentEnvironmentCards()
    {
        return GetEnvironmentCards(CurrentLocation);
    }

    public void AddCardToScene(string locationId, CardData card, bool emitChanged = true)
    {
        if (!_sceneItems.ContainsKey(locationId))
            _sceneItems[locationId] = new List<CardData>();
        _sceneItems[locationId].Add(card);
        if (emitChanged)
            EmitSignal(SignalName.OnSceneItemsChanged, locationId);
    }

    public void AddCardToCurrentScene(CardData card)
    {
        AddCardToScene(CurrentLocation, card);
    }

    public void EmitSceneItemsChangedAllLocations()
    {
        foreach (var id in _locations.Keys)
            EmitSignal(SignalName.OnSceneItemsChanged, id);
    }

    public void RemoveCardFromScene(string locationId, CardData card)
    {
        if (_sceneItems.TryGetValue(locationId, out var items))
        {
            items.Remove(card);
            EmitSignal(SignalName.OnSceneItemsChanged, locationId);
        }
    }

    public void RemoveCardFromCurrentScene(CardData card)
    {
        RemoveCardFromScene(CurrentLocation, card);
    }

    public Dictionary<string, LocationData> GetAllLocations()
    {
        return _locations;
    }

    public void ResetState()
    {
        CurrentLocation = "forest";
        foreach (var items in _sceneItems.Values)
            items.Clear();
        _environmentInitialized = false;
        _envRegenCarry.Clear();
        EnsureEnvironmentInitialized();
        EmitSignal(SignalName.OnLocationChanged, CurrentLocation);
        EmitSignal(SignalName.OnSceneItemsChanged, CurrentLocation);
    }
}
