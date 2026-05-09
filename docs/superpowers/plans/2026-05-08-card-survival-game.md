# 卡牌生存游戏 Sprint 1+2 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 构建卡牌生存游戏可玩核心原型：卡牌展示、拖拽合成、基础生存系统

**Architecture:** 纯 2D UI Control 节点 + Autoload 服务层（CardManager/CombineSystem/PlayerSystem）+ JSON 数据驱动。逻辑与 UI 分离，Autoload 不引用 UI 类型。

**Tech Stack:** Godot 4.6 + C#, System.Text.Json, 纯 Control 节点 UI

---

### Task 1: 项目目录与数据模型

**Files:**
- Create: `scripts/data/CardData.cs`
- Create: `scripts/data/CombineRule.cs`
- Create: `scripts/data/PlayerState.cs`

- [ ] **Step 1: 创建数据模型目录**

```bash
mkdir -p e:/project/now/scripts/data
```

- [ ] **Step 2: 编写 CardData.cs**

```csharp
// scripts/data/CardData.cs
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CardSurvival.Data;

public enum CardType { Resource, Creature, Tool, Building, Status, Event }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CardTag { Wood, Stone, Material, Food, Weapon, Fire, Shelter, Animal, Danger }

public class CardData
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public CardType Type { get; set; }
    public int Stack { get; set; } = 1;
    public int MaxStack { get; set; } = 1;
    public List<CardTag> Tags { get; set; } = new();
    public int Durability { get; set; } = -1;  // -1 = 无限耐久
}
```

- [ ] **Step 3: 编写 CombineRule.cs**

```csharp
// scripts/data/CombineRule.cs
using System.Collections.Generic;

namespace CardSurvival.Data;

public class CombineRule
{
    public string CardA { get; set; } = "";
    public string CardB { get; set; } = "";
    public List<string> Results { get; set; } = new();
    public float Chance { get; set; } = 1.0f;
    public bool MatchByTag { get; set; } = false;
}
```

- [ ] **Step 4: 编写 PlayerState.cs**

```csharp
// scripts/data/PlayerState.cs
namespace CardSurvival.Data;

public class PlayerState
{
    public int Health { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
    public int Hunger { get; set; } = 100;
    public int MaxHunger { get; set; } = 100;
    public float DayProgress { get; set; } = 0f;
}
```

- [ ] **Step 5: 编译验证**

```bash
cd e:/project/now && dotnet build 2>&1
```

Expected: Build succeeded.

- [ ] **Step 6: Commit**

```bash
cd e:/project/now && git add scripts/data/CardData.cs scripts/data/CombineRule.cs scripts/data/PlayerState.cs && git commit -m "feat: add data models (CardData, CombineRule, PlayerState)"
```

---

### Task 2: JSON 数据文件

**Files:**
- Create: `data/cards.json`
- Create: `data/combine_rules.json`

- [ ] **Step 1: 创建 data 目录**

```bash
mkdir -p e:/project/now/data
```

- [ ] **Step 2: 编写 cards.json（12 张 MVP 卡牌）**

```json
[
  {"Id":"wood","Name":"木头","Type":"Resource","Stack":1,"MaxStack":10,"Tags":["Wood","Material"],"Durability":-1},
  {"Id":"stone","Name":"石头","Type":"Resource","Stack":1,"MaxStack":10,"Tags":["Stone","Material"],"Durability":-1},
  {"Id":"berry","Name":"浆果","Type":"Resource","Stack":1,"MaxStack":5,"Tags":["Food"],"Durability":-1},
  {"Id":"rabbit","Name":"兔子","Type":"Creature","Stack":1,"MaxStack":1,"Tags":["Animal","Food"],"Durability":-1},
  {"Id":"wolf","Name":"狼","Type":"Creature","Stack":1,"MaxStack":1,"Tags":["Animal","Danger"],"Durability":-1},
  {"Id":"axe","Name":"石斧","Type":"Tool","Stack":1,"MaxStack":1,"Tags":["Weapon","Tool"],"Durability":5},
  {"Id":"campfire","Name":"火堆","Type":"Building","Stack":1,"MaxStack":1,"Tags":["Fire","Shelter"],"Durability":3},
  {"Id":"trap","Name":"陷阱","Type":"Building","Stack":1,"MaxStack":1,"Tags":["Tool"],"Durability":2},
  {"Id":"meat","Name":"肉","Type":"Resource","Stack":1,"MaxStack":3,"Tags":["Food"],"Durability":-1},
  {"Id":"hide","Name":"兽皮","Type":"Resource","Stack":1,"MaxStack":5,"Tags":["Material"],"Durability":-1},
  {"Id":"injury","Name":"受伤","Type":"Status","Stack":1,"MaxStack":1,"Tags":[],"Durability":-1},
  {"Id":"storm","Name":"暴雨","Type":"Event","Stack":1,"MaxStack":1,"Tags":[],"Durability":-1}
]
```

- [ ] **Step 3: 编写 combine_rules.json（10 条规则）**

```json
[
  {"CardA":"wood","CardB":"stone","Results":["axe"],"Chance":1.0,"MatchByTag":false},
  {"CardA":"axe","CardB":"rabbit","Results":["meat","hide"],"Chance":0.8,"MatchByTag":false},
  {"CardA":"axe","CardB":"wolf","Results":["meat","hide"],"Chance":0.5,"MatchByTag":false},
  {"CardA":"wood","CardB":"wood","Results":["campfire"],"Chance":1.0,"MatchByTag":false},
  {"CardA":"Material","CardB":"Material","Results":["trap"],"Chance":0.6,"MatchByTag":true},
  {"CardA":"meat","CardB":"campfire","Results":["berry"],"Chance":1.0,"MatchByTag":false},
  {"CardA":"Food","CardB":"campfire","Results":["berry"],"Chance":1.0,"MatchByTag":true},
  {"CardA":"campfire","CardB":"storm","Results":[],"Chance":1.0,"MatchByTag":false},
  {"CardA":"injury","CardB":"Food","Results":[],"Chance":1.0,"MatchByTag":true},
  {"CardA":"wolf","CardB":"trap","Results":["meat","hide"],"Chance":0.9,"MatchByTag":false}
]
```

- [ ] **Step 4: Commit**

```bash
cd e:/project/now && git add data/cards.json data/combine_rules.json && git commit -m "feat: add JSON data files (12 cards, 10 rules)"
```

---

### Task 3: DataLoader（JSON 解析）

**Files:**
- Create: `scripts/data/DataLoader.cs`

- [ ] **Step 1: 编写 DataLoader.cs**

```csharp
// scripts/data/DataLoader.cs
using System;
using System.Collections.Generic;
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
        Converters = { new JsonStringEnumConverter() }
    };

    public static List<CardData> LoadCards(string path)
    {
        try
        {
            var json = File.ReadAllText(ProjectSettings.GlobalizePath(path));
            return JsonSerializer.Deserialize<List<CardData>>(json, Options) ?? new();
        }
        catch (Exception e)
        {
            GD.PrintErr($"Failed to load cards from {path}: {e.Message}");
            return new();
        }
    }

    public static List<CombineRule> LoadRules(string path)
    {
        try
        {
            var json = File.ReadAllText(ProjectSettings.GlobalizePath(path));
            return JsonSerializer.Deserialize<List<CombineRule>>(json, Options) ?? new();
        }
        catch (Exception e)
        {
            GD.PrintErr($"Failed to load rules from {path}: {e.Message}");
            return new();
        }
    }
}
```

- [ ] **Step 2: 编译验证**

```bash
cd e:/project/now && dotnet build 2>&1
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
cd e:/project/now && git add scripts/data/DataLoader.cs && git commit -m "feat: add DataLoader for JSON card/rule parsing"
```

---

### Task 4: CardManager Autoload

**Files:**
- Create: `scripts/autoload/CardManager.cs`

- [ ] **Step 1: 创建 autoload 目录**

```bash
mkdir -p e:/project/now/scripts/autoload
```

- [ ] **Step 2: 编写 CardManager.cs**

```csharp
// scripts/autoload/CardManager.cs
using Godot;
using System.Collections.Generic;
using System.Linq;
using CardSurvival.Data;

namespace CardSurvival;

public partial class CardManager : Node
{
    [Signal] public delegate void OnCardAddedEventHandler(string cardId);
    [Signal] public delegate void OnCardRemovedEventHandler(string cardId);

    private Dictionary<string, CardData> _cardDefs = new();
    private List<CardData> _hand = new();
    private Random _rng = new();

    public override void _Ready()
    {
        _cardDefs.Clear();
        var cards = DataLoader.LoadCards("res://data/cards.json");
        foreach (var card in cards)
            _cardDefs[card.Id] = card;
        GD.Print($"CardManager: Loaded {_cardDefs.Count} card definitions");
    }

    public CardData? GetCard(string id)
    {
        return _cardDefs.TryGetValue(id, out var card) ? card : null;
    }

    public CardData? DrawCard()
    {
        if (_cardDefs.Count == 0) return null;
        var keys = _cardDefs.Keys.ToArray();
        var id = keys[_rng.Next(keys.Length)];
        var template = _cardDefs[id];
        var instance = CloneCard(template);
        AddCardToHand(instance);
        return instance;
    }

    public void AddCardToHand(CardData card)
    {
        _hand.Add(card);
        EmitSignal(SignalName.OnCardAdded, card.Id);
    }

    public void RemoveCardFromHand(CardData card)
    {
        if (_hand.Remove(card))
            EmitSignal(SignalName.OnCardRemoved, card.Id);
    }

    public List<CardData> GetHand() => _hand;

    public void DrawInitialHand(int count)
    {
        for (int i = 0; i < count; i++)
            DrawCard();
    }

    public CardData CloneCard(CardData source)
    {
        return new CardData
        {
            Id = source.Id,
            Name = source.Name,
            Type = source.Type,
            Stack = source.Stack,
            MaxStack = source.MaxStack,
            Tags = new List<CardTag>(source.Tags),
            Durability = source.Durability
        };
    }
}
```

- [ ] **Step 3: 编译验证**

```bash
cd e:/project/now && dotnet build 2>&1
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
cd e:/project/now && git add scripts/autoload/CardManager.cs && git commit -m "feat: add CardManager autoload (card loading, hand management, draw)"
```

---

### Task 5: PlayerSystem Autoload

**Files:**
- Create: `scripts/autoload/PlayerSystem.cs`

- [ ] **Step 1: 编写 PlayerSystem.cs**

```csharp
// scripts/autoload/PlayerSystem.cs
using Godot;
using CardSurvival.Data;

namespace CardSurvival;

public partial class PlayerSystem : Node
{
    [Signal] public delegate void OnPlayerDamagedEventHandler(int amount);
    [Signal] public delegate void OnPlayerDeathEventHandler();
    [Signal] public delegate void OnStatsChangedEventHandler();

    public PlayerState State { get; private set; } = new();

    public override void _Ready()
    {
        State = new PlayerState();
        GD.Print($"PlayerSystem: Initialized (HP:{State.Health} Hunger:{State.Hunger})");
    }

    public void TakeDamage(int amount)
    {
        State.Health = Mathf.Max(0, State.Health - amount);
        EmitSignal(SignalName.OnPlayerDamaged, amount);
        EmitSignal(SignalName.OnStatsChanged);
        if (State.Health <= 0)
        {
            EmitSignal(SignalName.OnPlayerDeath);
            GD.Print("Player died!");
        }
    }

    public void Heal(int amount)
    {
        State.Health = Mathf.Min(State.MaxHealth, State.Health + amount);
        EmitSignal(SignalName.OnStatsChanged);
    }

    public void ConsumeHunger(int amount)
    {
        State.Hunger = Mathf.Max(0, State.Hunger - amount);
        EmitSignal(SignalName.OnStatsChanged);
        if (State.Hunger <= 0)
        {
            TakeDamage(5);
            GD.Print("Starving! Took 5 damage.");
        }
    }

    public bool IsDead() => State.Health <= 0;
}
```

- [ ] **Step 2: 编译验证**

```bash
cd e:/project/now && dotnet build 2>&1
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
cd e:/project/now && git add scripts/autoload/PlayerSystem.cs && git commit -m "feat: add PlayerSystem autoload (HP, hunger, death check)"
```

---

### Task 6: CombineSystem Autoload

**Files:**
- Create: `scripts/autoload/CombineSystem.cs`

- [ ] **Step 1: 编写 CombineSystem.cs**

```csharp
// scripts/autoload/CombineSystem.cs
using Godot;
using System.Collections.Generic;
using System.Linq;
using CardSurvival.Data;

namespace CardSurvival;

public partial class CombineSystem : Node
{
    [Signal] public delegate void OnCombineSuccessEventHandler(string cardAId, string cardBId, string[] results);
    [Signal] public delegate void OnCombineFailEventHandler(string cardAId, string cardBId);

    private List<CombineRule> _rules = new();
    private Random _rng = new();

    public override void _Ready()
    {
        _rules = DataLoader.LoadRules("res://data/combine_rules.json");
        GD.Print($"CombineSystem: Loaded {_rules.Count} rules");
    }

    public bool TryCombine(CardData a, CardData b)
    {
        if (a == null || b == null) return false;

        var rule = FindRule(a.Id, b.Id, a.Tags, b.Tags);
        if (rule == null)
        {
            GD.Print($"Combine: {a.Name} + {b.Name} → No match");
            EmitSignal(SignalName.OnCombineFail, a.Id, b.Id);
            return false;
        }

        if (_rng.NextDouble() > rule.Chance)
        {
            GD.Print($"Combine: {a.Name} + {b.Name} → Failed ({rule.Chance:P0} chance)");
            EmitSignal(SignalName.OnCombineFail, a.Id, b.Id);
            return false;
        }

        GD.Print($"Combine: {a.Name} + {b.Name} → {string.Join(", ", rule.Results)}");
        EmitSignal(SignalName.OnCombineSuccess, a.Id, b.Id, rule.Results.ToArray());
        return true;
    }

    private CombineRule? FindRule(string idA, string idB, List<CardTag> tagsA, List<CardTag> tagsB)
    {
        // 1. Exact ID match (order-independent)
        foreach (var rule in _rules)
        {
            if (rule.MatchByTag) continue;
            if (MatchPair(rule.CardA, rule.CardB, idA, idB))
                return rule;
        }

        // 2. Tag match (order-independent)
        foreach (var rule in _rules)
        {
            if (!rule.MatchByTag) continue;
            if (MatchByTags(rule.CardA, rule.CardB, tagsA, tagsB))
                return rule;
        }

        return null;
    }

    private static bool MatchPair(string a, string b, string idA, string idB)
    {
        return (a == idA && b == idB) || (a == idB && b == idA);
    }

    private static bool MatchByTags(string tagA, string tagB, List<CardTag> tagsA, List<CardTag> tagsB)
    {
        var tagsAStr = tagsA.Select(t => t.ToString()).ToHashSet();
        var tagsBStr = tagsB.Select(t => t.ToString()).ToHashSet();
        var allTags = tagsAStr.Concat(tagsBStr).ToHashSet();
        return allTags.Contains(tagA) && allTags.Contains(tagB);
    }
}
```

- [ ] **Step 2: 编译验证**

```bash
cd e:/project/now && dotnet build 2>&1
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
cd e:/project/now && git add scripts/autoload/CombineSystem.cs && git commit -m "feat: add CombineSystem autoload (rule matching, ID + Tag modes)"
```

---

### Task 7: 注册 Autoload 到 project.godot

**Files:**
- Modify: `project.godot`

- [ ] **Step 1: 在 project.godot 末尾追加 Autoload 配置**

Read the current file, then add these lines at the end:

```ini
[autoload]

CardManager="*res://scripts/autoload/CardManager.cs"
CombineSystem="*res://scripts/autoload/CombineSystem.cs"
PlayerSystem="*res://scripts/autoload/PlayerSystem.cs"
```

- [ ] **Step 2: 编译验证**

```bash
cd e:/project/now && dotnet build 2>&1
```

Expected: Build succeeded. The autoloads will now be available as `/root/CardManager`, `/root/CombineSystem`, `/root/PlayerSystem`.

- [ ] **Step 3: Commit**

```bash
cd e:/project/now && git add project.godot && git commit -m "feat: register CardManager, CombineSystem, PlayerSystem as autoloads"
```

---

### Task 8: CardNode 卡牌 UI

**Files:**
- Create: `scripts/ui/CardNode.cs`

- [ ] **Step 1: 创建 ui 目录**

```bash
mkdir -p e:/project/now/scripts/ui
```

- [ ] **Step 2: 编写 CardNode.cs**

```csharp
// scripts/ui/CardNode.cs
using Godot;
using CardSurvival.Data;

namespace CardSurvival.UI;

public partial class CardNode : Control
{
    public CardData Data { get; private set; }

    private ColorRect _background;
    private Label _nameLabel;
    private Label _infoLabel;

    private static readonly Color[] TypeColors = new[]
    {
        new(0.4f, 0.3f, 0.2f),  // Resource - brown
        new(0.3f, 0.5f, 0.3f),  // Creature - green
        new(0.4f, 0.4f, 0.5f),  // Tool - steel blue
        new(0.5f, 0.4f, 0.2f),  // Building - tan
        new(0.6f, 0.2f, 0.2f),  // Status - red
        new(0.2f, 0.2f, 0.6f),  // Event - dark blue
    };

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(100, 60);
        MouseFilter = MouseFilterEnum.Stop;

        _background = new ColorRect();
        _background.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_background);

        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(LayoutPreset.FullRect);
        vbox.AddThemeConstantOverride("separation", 2);
        AddChild(vbox);

        _nameLabel = new Label();
        _nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _nameLabel.AddThemeFontSizeOverride("font_size", 14);
        vbox.AddChild(_nameLabel);

        _infoLabel = new Label();
        _infoLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _infoLabel.AddThemeFontSizeOverride("font_size", 10);
        vbox.AddChild(_infoLabel);
    }

    public void Setup(CardData data)
    {
        Data = data;
        _nameLabel.Text = data.Name;
        _infoLabel.Text = $"[{data.Type}]";

        var colorIdx = (int)data.Type;
        _background.Color = colorIdx < TypeColors.Length ? TypeColors[colorIdx] : new Color(0.3f, 0.3f, 0.3f);

        if (data.Durability > 0)
            _infoLabel.Text += $" Dur:{data.Durability}";
        if (data.MaxStack > 1)
            _infoLabel.Text += $" x{data.Stack}/{data.MaxStack}";
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        var preview = new Label();
        preview.Text = Data.Name;
        preview.AddThemeFontSizeOverride("font_size", 12);
        SetDragPreview(preview);
        Modulate = new Color(1, 1, 1, 0.5f);
        return this;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationDragEnd)
            Modulate = new Color(1, 1, 1, 1);
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        var other = data.As<CardNode>();
        return other != null && other != this;
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        var other = data.As<CardNode>();
        if (other == null || other == this) return;

        var combineSystem = GetNode<CombineSystem>("/root/CombineSystem");
        combineSystem.TryCombine(Data, other.Data);
    }
}
```

- [ ] **Step 3: 编译验证**

```bash
cd e:/project/now && dotnet build 2>&1
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
cd e:/project/now && git add scripts/ui/CardNode.cs && git commit -m "feat: add CardNode UI (display, drag/drop, type colors)"
```

---

### Task 9: HandArea 手牌区 + TableArea 桌面区

**Files:**
- Create: `scripts/ui/HandArea.cs`
- Create: `scripts/ui/TableArea.cs`

- [ ] **Step 1: 编写 HandArea.cs**

```csharp
// scripts/ui/HandArea.cs
using Godot;
using System.Linq;

namespace CardSurvival.UI;

public partial class HandArea : HBoxContainer
{
    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 4);
        var cardManager = GetNode<CardManager>("/root/CardManager");

        cardManager.OnCardAdded += (id) => CallDeferred(nameof(Refresh));
        cardManager.OnCardRemoved += (id) => CallDeferred(nameof(Refresh));
    }

    private void Refresh()
    {
        foreach (var child in GetChildren().OfType<CardNode>().ToList())
            child.QueueFree();

        var cardManager = GetNode<CardManager>("/root/CardManager");
        foreach (var card in cardManager.GetHand())
        {
            var cardNode = new CardNode();
            cardNode.Setup(card);
            AddChild(cardNode);
        }
    }
}
```

- [ ] **Step 2: 编写 TableArea.cs**

```csharp
// scripts/ui/TableArea.cs
using Godot;
using System.Collections.Generic;

namespace CardSurvival.UI;

public partial class TableArea : Control
{
    private List<CardNode> _cards = new();

    public override void _Ready()
    {
        var combineSystem = GetNode<CombineSystem>("/root/CombineSystem");
        var cardManager = GetNode<CardManager>("/root/CardManager");

        combineSystem.OnCombineSuccess += (aId, bId, results) => OnCombineSuccess(aId, bId, results);
        combineSystem.OnCombineFail += (aId, bId) => OnCombineFail(aId, bId);

        CustomMinimumSize = new Vector2(400, 200);
    }

    private void OnCombineSuccess(string aId, string bId, string[] results)
    {
        var cardManager = GetNode<CardManager>("/root/CardManager");

        // Remove input cards from hand
        RemoveCardById(aId);
        RemoveCardById(bId);

        // Add result cards to hand
        foreach (var resultId in results)
        {
            var template = cardManager.GetCard(resultId);
            if (template != null)
                cardManager.AddCardToHand(cardManager.CloneCard(template));
        }

        GD.Print($"Combine SUCCESS: {aId} + {bId} → [{string.Join(", ", results)}]");
    }

    private void OnCombineFail(string aId, string bId)
    {
        GD.Print($"Combine FAILED: {aId} + {bId}");
    }

    private void RemoveCardById(string id)
    {
        var cardManager = GetNode<CardManager>("/root/CardManager");
        var hand = cardManager.GetHand();
        var card = hand.Find(c => c.Id == id);
        if (card != null)
            cardManager.RemoveCardFromHand(card);
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        return data.As<CardNode>() != null;
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        var cardNode = data.As<CardNode>();
        if (cardNode == null) return;

        // Position the card where it was dropped
        if (cardNode.GetParent() != this)
        {
            cardNode.GetParent()?.RemoveChild(cardNode);
            AddChild(cardNode);
        }
        cardNode.Position = atPosition - cardNode.Size / 2;
        _cards.Add(cardNode);
    }
}
```

- [ ] **Step 3: 编译验证**

```bash
cd e:/project/now && dotnet build 2>&1
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
cd e:/project/now && git add scripts/ui/HandArea.cs scripts/ui/TableArea.cs && git commit -m "feat: add HandArea and TableArea UI containers"
```

---

### Task 10: StatusPanel 状态栏

**Files:**
- Create: `scripts/ui/StatusPanel.cs`

- [ ] **Step 1: 编写 StatusPanel.cs**

```csharp
// scripts/ui/StatusPanel.cs
using Godot;

namespace CardSurvival.UI;

public partial class StatusPanel : HBoxContainer
{
    private ProgressBar _healthBar;
    private ProgressBar _hungerBar;
    private Label _healthLabel;
    private Label _hungerLabel;
    private Button _drawButton;
    private Button _eatButton;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 8);

        // Health bar
        var healthGroup = new VBoxContainer();
        _healthLabel = new Label();
        _healthLabel.Text = "HP: 100/100";
        healthGroup.AddChild(_healthLabel);
        _healthBar = new ProgressBar();
        _healthBar.MinValue = 0;
        _healthBar.MaxValue = 100;
        _healthBar.Value = 100;
        _healthBar.CustomMinimumSize = new Vector2(120, 0);
        healthGroup.AddChild(_healthBar);
        AddChild(healthGroup);

        // Hunger bar
        var hungerGroup = new VBoxContainer();
        _hungerLabel = new Label();
        _hungerLabel.Text = "Hunger: 100/100";
        hungerGroup.AddChild(_hungerLabel);
        _hungerBar = new ProgressBar();
        _hungerBar.MinValue = 0;
        _hungerBar.MaxValue = 100;
        _hungerBar.Value = 100;
        _hungerBar.CustomMinimumSize = new Vector2(120, 0);
        hungerGroup.AddChild(_hungerBar);
        AddChild(hungerGroup);

        // Draw button
        _drawButton = new Button();
        _drawButton.Text = "抽牌";
        _drawButton.Pressed += OnDrawPressed;
        AddChild(_drawButton);

        // Eat button（手动扣饥饿测试）
        _eatButton = new Button();
        _eatButton.Text = "消耗饥饿";
        _eatButton.Pressed += OnEatPressed;
        AddChild(_eatButton);

        var playerSystem = GetNode<PlayerSystem>("/root/PlayerSystem");
        playerSystem.OnStatsChanged += Refresh;
    }

    private void OnDrawPressed()
    {
        var cardManager = GetNode<CardManager>("/root/CardManager");
        cardManager.DrawCard();
    }

    private void OnEatPressed()
    {
        var playerSystem = GetNode<PlayerSystem>("/root/PlayerSystem");
        playerSystem.ConsumeHunger(10);
    }

    private void Refresh()
    {
        var playerSystem = GetNode<PlayerSystem>("/root/PlayerSystem");
        var state = playerSystem.State;
        _healthLabel.Text = $"HP: {state.Health}/{state.MaxHealth}";
        _healthBar.Value = state.Health;
        _hungerLabel.Text = $"Hunger: {state.Hunger}/{state.MaxHunger}";
        _hungerBar.Value = state.Hunger;
    }
}
```

- [ ] **Step 2: 编译验证**

```bash
cd e:/project/now && dotnet build 2>&1
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
cd e:/project/now && git add scripts/ui/StatusPanel.cs && git commit -m "feat: add StatusPanel (HP bar, hunger bar, draw/eat buttons)"
```

---

### Task 11: GameRoot 主场景

**Files:**
- Create: `scenes/GameRoot.tscn`
- Create: `scripts/GameRoot.cs`

- [ ] **Step 1: 创建 scenes 目录**

```bash
mkdir -p e:/project/now/scenes
```

- [ ] **Step 2: 编写 GameRoot.cs**

```csharp
// scripts/GameRoot.cs
using Godot;
using CardSurvival.UI;

namespace CardSurvival;

public partial class GameRoot : Control
{
    public override void _Ready()
    {
        GD.Print("GameRoot: Starting...");

        // Layout
        var mainVBox = new VBoxContainer();
        mainVBox.SetAnchorsPreset(LayoutPreset.FullRect);
        mainVBox.AddThemeConstantOverride("separation", 4);
        AddChild(mainVBox);

        // Status panel at top
        var statusPanel = new StatusPanel();
        mainVBox.AddChild(statusPanel);

        // Table area (drop zone) in middle
        var tableArea = new TableArea();
        tableArea.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        mainVBox.AddChild(tableArea);

        // Hand area at bottom
        var handArea = new HandArea();
        mainVBox.AddChild(handArea);

        // Draw initial hand
        var cardManager = GetNode<CardManager>("/root/CardManager");
        cardManager.DrawInitialHand(5);
    }
}
```

- [ ] **Step 3: 创建 GameRoot.tscn 场景文件（用 Write 工具写入以下内容）**

```
[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://scripts/GameRoot.cs" id="1_root"]

[node name="GameRoot" type="Control"]
layout_mode = 3
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
script = ExtResource("1_root")
```

- [ ] **Step 4: 设置 GameRoot 为项目主场景**

在 project.godot 的 `[application]` 部分：

```ini
[application]

config/name="Card Survival"
config/features=PackedStringArray("4.6", "Forward Plus")
config/icon="res://icon.svg"
run/main_scene="res://scenes/GameRoot.tscn"
```

- [ ] **Step 5: 编译验证**

```bash
cd e:/project/now && dotnet build 2>&1
```

Expected: Build succeeded.

- [ ] **Step 6: Commit**

```bash
cd e:/project/now && git add scripts/GameRoot.cs scenes/GameRoot.tscn project.godot && git commit -m "feat: add GameRoot scene (main UI layout, hand draw, wire everything)"
```

---

### Task 12: 运行验证

- [ ] **Step 1: 启动 Godot 编辑器打开项目**

```bash
cd e:/project/now && echo "Open project.godot in Godot Editor and press F5 to run."
```

Expected 验证清单：
1. 游戏窗口打开，顶部显示 HP/Hunger 状态条
2. 底部显示 5 张初始手牌（彩色卡牌，显示名称和类型）
3. 点击"抽牌"按钮，手牌区新增一张卡牌
4. 拖拽一张卡牌到另一张卡牌上 —— 若匹配规则，两张卡消失并产出新卡
5. 拖拽不匹配的卡牌 —— 控制台输出 "No match"
6. 把卡牌拖到桌面中间区域 —— 卡牌留在桌面上
7. 点击"消耗饥饿"按钮 —— 饥饿条减少
8. 饥饿归零后继续点 —— HP 开始减少
9. HP 归零 —— 控制台输出 "Player died!"

- [ ] **Step 2: 验证数据驱动**

编辑 `data/cards.json`，添加一张新卡牌：
```json
{"Id":"gold","Name":"金块","Type":"Resource","Stack":1,"MaxStack":3,"Tags":["Material"],"Durability":-1}
```

重新运行游戏，抽牌 —— 新卡牌出现在手牌中。（无需改任何 C# 代码）

- [ ] **Step 3: Commit（如有 micro-fix）**

```bash
cd e:/project/now && git status
```
