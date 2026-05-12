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

        GD.Print($"[MapSystem] Loaded {_locations.Count} locations");
    }

    private void InitializeEnvironmentItems()
    {
        foreach (var items in _environmentItems.Values)
            items.Clear();

        var cardManager = GetNode<CardManager>("/root/CardManager");

        if (_environmentItems.ContainsKey("forest"))
        {
            var forestEnv = _environmentItems["forest"];
            AddEnvCard(forestEnv, cardManager, "tree", 3);
            AddEnvCard(forestEnv, cardManager, "bush", 2);
            AddEnvCard(forestEnv, cardManager, "rock", 1);
        }

        if (_environmentItems.ContainsKey("lake"))
        {
            var lakeEnv = _environmentItems["lake"];
            AddEnvCard(lakeEnv, cardManager, "rock", 2);
            AddEnvCard(lakeEnv, cardManager, "bush", 1);
        }

        if (_environmentItems.ContainsKey("mountain"))
        {
            var mountainEnv = _environmentItems["mountain"];
            AddEnvCard(mountainEnv, cardManager, "rock", 3);
            AddEnvCard(mountainEnv, cardManager, "tree", 1);
        }

        if (_environmentItems.ContainsKey("plains"))
        {
            var plainsEnv = _environmentItems["plains"];
            AddEnvCard(plainsEnv, cardManager, "bush", 3);
        }

        if (_environmentItems.ContainsKey("cave"))
        {
            var caveEnv = _environmentItems["cave"];
            AddEnvCard(caveEnv, cardManager, "rock", 2);
        }

        _environmentInitialized = true;
    }

    private void EnsureEnvironmentInitialized()
    {
        if (_environmentInitialized) return;
        InitializeEnvironmentItems();
    }

    private void AddEnvCard(List<CardData> envList, CardManager cardManager, string cardId, int count)
    {
        for (int i = 0; i < count; i++)
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

    public void AddCardToScene(string locationId, CardData card)
    {
        if (!_sceneItems.ContainsKey(locationId))
            _sceneItems[locationId] = new List<CardData>();
        _sceneItems[locationId].Add(card);
        EmitSignal(SignalName.OnSceneItemsChanged, locationId);
    }

    public void AddCardToCurrentScene(CardData card)
    {
        AddCardToScene(CurrentLocation, card);
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
        EnsureEnvironmentInitialized();
        EmitSignal(SignalName.OnLocationChanged, CurrentLocation);
        EmitSignal(SignalName.OnSceneItemsChanged, CurrentLocation);
    }
}
