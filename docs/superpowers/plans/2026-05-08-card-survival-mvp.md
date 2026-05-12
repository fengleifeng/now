# 卡牌生存游戏 MVP 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 构建可玩核心原型 — 卡牌展示、拖拽、合成系统 + 基础玩家状态

**Architecture:** 混合架构，C# Autoload 服务层（CardManager / CombineSystem / PlayerSystem）处理逻辑，Godot Control 节点（CardNode / HandArea / TableArea / StatusPanel）处理 UI。JSON 驱动卡牌和规则数据。

**Tech Stack:** Godot 4.6 + C#, System.Text.Json, 纯 2D Control UI

---

### Task 1: 项目目录 + 数据模型

**Files:**
- Create: `scripts/data/CardData.cs`
- Create: `scripts/data/CombineRule.cs`
- Create: `scripts/data/PlayerState.cs`

- [ ] **Step 1: 创建目录结构**

```bash
mkdir -p e:/project/now/data
mkdir -p e:/project/now/scripts/data
mkdir -p e:/project/now/scripts/autoload
mkdir -p e:/project/now/scripts/ui
mkdir -p e:/project/now/scenes
```

- [ ] **Step 2: 写 CardData.cs**

```csharp
// scripts/data/CardData.cs
namespace CardSurvival.Data;

public enum CardType { Resource, Creature, Tool, Building, Status, Event }

public enum CardTag { Wood, Stone, Material, Food, Weapon, Fire, Shelter, Animal, Danger }

public class CardData
{
	public string Id { get; set; } = "";
	public string Name { get; set; } = "";
	public CardType Type { get; set; }
	public int Stack { get; set; } = 1;
	public int MaxStack { get; set; } = 1;
	public List<CardTag> Tags { get; set; } = new();
	public int Durability { get; set; } = -1;
}
```

- [ ] **Step 3: 写 CombineRule.cs**

```csharp
// scripts/data/CombineRule.cs
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

- [ ] **Step 4: 写 PlayerState.cs**

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

Run: `dotnet build --project e:/project/now`
Expected: Build succeeds with 0 errors

- [ ] **Step 6: Commit**

```bash
cd e:/project/now
git add scripts/data/CardData.cs scripts/data/CombineRule.cs scripts/data/PlayerState.cs
git commit -m "feat: add data models (CardData, CombineRule, PlayerState)"
```

---

### Task 2: JSON 数据文件

**Files:**
- Create: `data/cards.json`
- Create: `data/combine_rules.json`

- [ ] **Step 1: 写 cards.json（12 张 MVP 卡牌）**

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

- [ ] **Step 2: 写 combine_rules.json（10 条规则）**

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

- [ ] **Step 3: Commit**

```bash
cd e:/project/now
git add data/cards.json data/combine_rules.json
git commit -m "feat: add JSON card definitions and combine rules"
```

---

### Task 3: DataLoader — JSON 解析

**Files:**
- Create: `scripts/data/DataLoader.cs`

- [ ] **Step 1: 写 DataLoader.cs**

```csharp
// scripts/data/DataLoader.cs
using System.Text.Json;
using System.Text.Json.Serialization;

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
		var json = File.ReadAllText(path);
		return JsonSerializer.Deserialize<List<CardData>>(json, Options) ?? new();
	}

	public static List<CombineRule> LoadRules(string path)
	{
		var json = File.ReadAllText(path);
		return JsonSerializer.Deserialize<List<CombineRule>>(json, Options) ?? new();
	}
}
```

- [ ] **Step 2: 编译验证**

Run: `dotnet build --project e:/project/now`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
cd e:/project/now
git add scripts/data/DataLoader.cs
git commit -m "feat: add DataLoader with JSON parsing"
```

---

### Task 4: CardManager Autoload

**Files:**
- Create: `scripts/autoload/CardManager.cs`

- [ ] **Step 1: 写 CardManager.cs**

```csharp
// scripts/autoload/CardManager.cs
using Godot;
using CardSurvival.Data;

namespace CardSurvival;

public partial class CardManager : Node
{
	[Signal] public delegate void OnCardAddedEventHandler(string cardId);
	[Signal] public delegate void OnCardRemovedEventHandler(string cardId);

	private readonly Dictionary<string, CardData> _cardDefs = new();
	private readonly List<CardData> _hand = new();

	public override void _Ready()
	{
		var cards = DataLoader.LoadCards("res://data/cards.json");
		foreach (var c in cards)
			_cardDefs[c.Id] = c;
		GD.Print($"[CardManager] Loaded {_cardDefs.Count} card definitions");
	}

	public CardData? GetCard(string id) =>
		_cardDefs.TryGetValue(id, out var card) ? card : null;

	public bool HasCard(string id) => _cardDefs.ContainsKey(id);

	public CardData? DrawCard()
	{
		if (_cardDefs.Count == 0) return null;
		var keys = _cardDefs.Keys.ToArray();
		var id = keys[new Random().Next(keys.Length)];
		var template = _cardDefs[id];
		var instance = CloneCard(template);
		_hand.Add(instance);
		EmitSignal(SignalName.OnCardAdded, instance.Id);
		GD.Print($"[CardManager] Drew card: {instance.Name}");
		return instance;
	}

	public void RemoveCardFromHand(CardData card)
	{
		_hand.Remove(card);
		EmitSignal(SignalName.OnCardRemoved, card.Id);
	}

	public List<CardData> GetHand() => _hand;

	public void DrawInitialHand(int count)
	{
		for (int i = 0; i < count; i++)
			DrawCard();
	}

	private static CardData CloneCard(CardData src) => new()
	{
		Id = src.Id,
		Name = src.Name,
		Type = src.Type,
		Stack = src.Stack,
		MaxStack = src.MaxStack,
		Tags = new List<CardTag>(src.Tags),
		Durability = src.Durability
	};
}
```

- [ ] **Step 2: 编译验证**

Run: `dotnet build --project e:/project/now`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
cd e:/project/now
git add scripts/autoload/CardManager.cs
git commit -m "feat: add CardManager autoload"
```

---

### Task 5: PlayerSystem Autoload

**Files:**
- Create: `scripts/autoload/PlayerSystem.cs`

- [ ] **Step 1: 写 PlayerSystem.cs**

```csharp
// scripts/autoload/PlayerSystem.cs
using Godot;
using CardSurvival.Data;

namespace CardSurvival;

public partial class PlayerSystem : Node
{
	[Signal] public delegate void OnPlayerDamagedEventHandler(int amount);
	[Signal] public delegate void OnPlayerHealedEventHandler(int amount);
	[Signal] public delegate void OnPlayerDeathEventHandler();

	public PlayerState State { get; private set; } = new();

	public override void _Ready()
	{
		GD.Print($"[PlayerSystem] HP:{State.Health} Hunger:{State.Hunger}");
	}

	public void TakeDamage(int amount)
	{
		State.Health = Math.Max(0, State.Health - amount);
		EmitSignal(SignalName.OnPlayerDamaged, amount);
		if (State.Health <= 0)
			EmitSignal(SignalName.OnPlayerDeath);
	}

	public void Heal(int amount)
	{
		State.Health = Math.Min(State.MaxHealth, State.Health + amount);
		EmitSignal(SignalName.OnPlayerHealed, amount);
	}

	public void ConsumeHunger(int amount)
	{
		State.Hunger = Math.Max(0, State.Hunger - amount);
		if (State.Hunger <= 0)
			TakeDamage(10);
	}

	public void Eat(int amount)
	{
		State.Hunger = Math.Min(State.MaxHunger, State.Hunger + amount);
	}

	public bool IsDead() => State.Health <= 0;
}
```

- [ ] **Step 2: 编译验证**

Run: `dotnet build --project e:/project/now`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
cd e:/project/now
git add scripts/autoload/PlayerSystem.cs
git commit -m "feat: add PlayerSystem autoload"
```

---

### Task 6: CombineSystem Autoload

**Files:**
- Create: `scripts/autoload/CombineSystem.cs`

- [ ] **Step 1: 写 CombineSystem.cs**

```csharp
// scripts/autoload/CombineSystem.cs
using Godot;
using CardSurvival.Data;

namespace CardSurvival;

public partial class CombineSystem : Node
{
	[Signal] public delegate void OnCombineSuccessEventHandler(string cardAId, string cardBId, string[] results);
	[Signal] public delegate void OnCombineFailEventHandler(string cardAId, string cardBId);

	private List<CombineRule> _rules = new();

	public override void _Ready()
	{
		_rules = DataLoader.LoadRules("res://data/combine_rules.json");
		GD.Print($"[CombineSystem] Loaded {_rules.Count} rules");
	}

	public bool TryCombine(CardData a, CardData b)
	{
		var rule = FindRule(a, b);
		if (rule == null)
		{
			EmitSignal(SignalName.OnCombineFail, a.Id, b.Id);
			return false;
		}

		if (new Random().NextDouble() > rule.Chance)
		{
			EmitSignal(SignalName.OnCombineFail, a.Id, b.Id);
			return false;
		}

		EmitSignal(SignalName.OnCombineSuccess, a.Id, b.Id, rule.Results.ToArray());
		return true;
	}

	private CombineRule? FindRule(CardData a, CardData b)
	{
		// Priority 1: exact ID match
		foreach (var r in _rules)
		{
			if (r.MatchByTag) continue;
			if (PairMatches(r.CardA, r.CardB, a.Id, b.Id))
				return r;
		}

		// Priority 2: tag match
		foreach (var r in _rules)
		{
			if (!r.MatchByTag) continue;
			if (PairMatchesTags(r.CardA, r.CardB, a.Tags, b.Tags))
				return r;
		}

		return null;
	}

	private static bool PairMatches(string ra, string rb, string idA, string idB)
	{
		return (ra == idA && rb == idB) || (ra == idB && rb == idA);
	}

	private static bool PairMatchesTags(string ra, string rb, List<CardTag> tagsA, List<CardTag> tagsB)
	{
		var flatA = tagsA.Select(t => t.ToString());
		var flatB = tagsB.Select(t => t.ToString());

		var hasA = flatA.Contains(ra) || flatB.Contains(ra);
		var hasB = flatB.Contains(rb) || flatA.Contains(rb);
		return hasA && hasB;
	}
}
```

- [ ] **Step 2: 编译验证**

Run: `dotnet build --project e:/project/now`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
cd e:/project/now
git add scripts/autoload/CombineSystem.cs
git commit -m "feat: add CombineSystem autoload"
```

---

### Task 7: 注册 Autoload + project.godot 配置

**Files:**
- Modify: `project.godot`

- [ ] **Step 1: 确保 .csproj 存在**

Run: `ls e:/project/now/now.csproj 2>/dev/null || echo "NEEDS REGEN"`
If the .csproj file is missing, open the project in Godot editor once to regenerate it.

- [ ] **Step 2: 在 project.godot 添加 Autoload 注册**

在 `project.godot` 末尾追加以下内容：

```ini
[autoload]

CardManager="*res://scripts/autoload/CardManager.cs"
CombineSystem="*res://scripts/autoload/CombineSystem.cs"
PlayerSystem="*res://scripts/autoload/PlayerSystem.cs"
```

- [ ] **Step 3: 编译验证**

Run: `dotnet build --project e:/project/now`
Expected: Build succeeds with 0 errors

- [ ] **Step 4: Commit**

```bash
cd e:/project/now
git add project.godot
git commit -m "feat: register autoloads in project.godot"
```

---

### Task 8: CardNode — 单张卡牌 Control

**Files:**
- Create: `scripts/ui/CardNode.cs`

- [ ] **Step 1: 写 CardNode.cs**

```csharp
// scripts/ui/CardNode.cs
using Godot;
using CardSurvival.Data;

namespace CardSurvival.UI;

public partial class CardNode : Control
{
	public CardData Data { get; private set; } = null!;

	private ColorRect _bg = null!;
	private Label _nameLabel = null!;
	private Label _typeLabel = null!;
	private Panel _panel = null!;

	public override void _Ready()
	{
		CustomMinimumSize = new Vector2(110, 70);
		MouseFilter = MouseFilterEnum.Stop;
		MouseDefaultCursorShape = CursorShape.PointingHand;

		_panel = new Panel();
		_panel.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(_panel);

		_bg = new ColorRect();
		_bg.SetAnchorsPreset(LayoutPreset.FullRect);
		_panel.AddChild(_bg);

		var vbox = new VBoxContainer();
		vbox.SetAnchorsPreset(LayoutPreset.FullRect);
		vbox.AddThemeConstantOverride("separation", 2);
		_panel.AddChild(vbox);

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 6);
		margin.AddThemeConstantOverride("margin_right", 6);
		margin.AddThemeConstantOverride("margin_top", 6);
		margin.AddThemeConstantOverride("margin_bottom", 6);
		margin.AddChild(vbox);
		_panel.AddChild(margin);

		_nameLabel = new Label();
		_nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(_nameLabel);

		_typeLabel = new Label();
		_typeLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_typeLabel.AddThemeFontSizeOverride("font_size", 10);
		vbox.AddChild(_typeLabel);
	}

	public void Setup(CardData data)
	{
		Data = data;
		_nameLabel.Text = data.Name;
		_typeLabel.Text = $"[{data.Type}] x{data.Stack}";

		_bg.Color = data.Type switch
		{
			CardType.Resource => new Color(0.45f, 0.35f, 0.2f),
			CardType.Creature => new Color(0.25f, 0.5f, 0.25f),
			CardType.Tool => new Color(0.35f, 0.35f, 0.55f),
			CardType.Building => new Color(0.5f, 0.4f, 0.25f),
			CardType.Status => new Color(0.55f, 0.2f, 0.2f),
			CardType.Event => new Color(0.2f, 0.2f, 0.55f),
			_ => new Color(0.3f, 0.3f, 0.3f)
		};

		Name = data.Id;
	}

	public override Variant _GetDragData(Vector2 atPosition)
	{
		var preview = new Label();
		preview.Text = Data.Name;
		preview.Modulate = new Color(1, 1, 1, 0.7f);
		SetDragPreview(preview);
		return this;
	}

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		var node = data.As<CardNode>();
		return node != null && node != this;
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		var other = data.As<CardNode>();
		if (other == null || other == this) return;

		var system = GetNode<CombineSystem>("/root/CombineSystem");
		var manager = GetNode<CardManager>("/root/CardManager");

		if (system.TryCombine(Data, other.Data))
		{
			manager.RemoveCardFromHand(Data);
			manager.RemoveCardFromHand(other.Data);
			QueueFree();
			other.QueueFree();
		}
		else
		{
			GD.Print($"[CardNode] Combine failed: {Data.Name} + {other.Data.Name}");
		}
	}
}
```

- [ ] **Step 2: 编译验证**

Run: `dotnet build --project e:/project/now`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
cd e:/project/now
git add scripts/ui/CardNode.cs
git commit -m "feat: add CardNode with drag-and-drop"
```

---

### Task 9: HandArea + TableArea UI

**Files:**
- Create: `scripts/ui/HandArea.cs`
- Create: `scripts/ui/TableArea.cs`

- [ ] **Step 1: 写 HandArea.cs**

```csharp
// scripts/ui/HandArea.cs
using Godot;
using CardSurvival.Data;

namespace CardSurvival.UI;

public partial class HandArea : HBoxContainer
{
	public override void _Ready()
	{
		Alignment = AlignmentMode.Center;
		AddThemeConstantOverride("separation", 8);
	}

	public void Refresh(List<CardData> hand)
	{
		foreach (var child in GetChildren())
			child.QueueFree();

		foreach (var card in hand)
		{
			var cardNode = new CardNode();
			cardNode.Setup(card);
			AddChild(cardNode);
		}
	}
}
```

- [ ] **Step 2: 写 TableArea.cs**

```csharp
// scripts/ui/TableArea.cs
using Godot;

namespace CardSurvival.UI;

public partial class TableArea : Control
{
	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Pass;
		GD.Print("[TableArea] Ready");
	}
}
```

Note: TableArea 本身不处理放置逻辑 — CardNode 的 `_DropData` 直接调用 CombineSystem。TableArea 仅作为卡牌可以自由拖放的空间。

- [ ] **Step 3: 编译验证**

Run: `dotnet build --project e:/project/now`
Expected: Build succeeds

- [ ] **Step 4: Commit**

```bash
cd e:/project/now
git add scripts/ui/HandArea.cs scripts/ui/TableArea.cs
git commit -m "feat: add HandArea and TableArea UI"
```

---

### Task 10: StatusPanel — 玩家状态栏

**Files:**
- Create: `scripts/ui/StatusPanel.cs`

- [ ] **Step 1: 写 StatusPanel.cs**

```csharp
// scripts/ui/StatusPanel.cs
using Godot;

namespace CardSurvival.UI;

public partial class StatusPanel : HBoxContainer
{
	private ProgressBar _hpBar = null!;
	private ProgressBar _hungerBar = null!;
	private Label _hpLabel = null!;
	private Label _hungerLabel = null!;
	private PlayerSystem _player = null!;

	public override void _Ready()
	{
		Alignment = AlignmentMode.Center;
		AddThemeConstantOverride("separation", 16);

		_player = GetNode<PlayerSystem>("/root/PlayerSystem");

		var hpGroup = new VBoxContainer();
		AddChild(hpGroup);

		_hpLabel = new Label();
		_hpLabel.Text = "HP";
		_hpLabel.HorizontalAlignment = HorizontalAlignment.Center;
		hpGroup.AddChild(_hpLabel);

		_hpBar = new ProgressBar();
		_hpBar.MinValue = 0;
		_hpBar.MaxValue = _player.State.MaxHealth;
		_hpBar.CustomMinimumSize = new Vector2(140, 22);
		hpGroup.AddChild(_hpBar);

		var hungerGroup = new VBoxContainer();
		AddChild(hungerGroup);

		_hungerLabel = new Label();
		_hungerLabel.Text = "Hunger";
		_hungerLabel.HorizontalAlignment = HorizontalAlignment.Center;
		hungerGroup.AddChild(_hungerLabel);

		_hungerBar = new ProgressBar();
		_hungerBar.MinValue = 0;
		_hungerBar.MaxValue = _player.State.MaxHunger;
		_hungerBar.CustomMinimumSize = new Vector2(140, 22);
		hungerGroup.AddChild(_hungerBar);

		_player.OnPlayerDamaged += OnDamaged;
		_player.OnPlayerHealed += OnHealed;
	}

	public override void _Process(double delta)
	{
		_hpBar.Value = _player.State.Health;
		_hungerBar.Value = _player.State.Hunger;
		_hpLabel.Text = $"HP {_player.State.Health}/{_player.State.MaxHealth}";
		_hungerLabel.Text = $"Hunger {_player.State.Hunger}/{_player.State.MaxHunger}";
	}

	private void OnDamaged(int amount)
	{
		GD.Print($"[StatusPanel] Player damaged: {amount}");
	}

	private void OnHealed(int amount)
	{
		GD.Print($"[StatusPanel] Player healed: {amount}");
	}
}
```

- [ ] **Step 2: 编译验证**

Run: `dotnet build --project e:/project/now`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
cd e:/project/now
git add scripts/ui/StatusPanel.cs
git commit -m "feat: add StatusPanel with HP and hunger bars"
```

---

### Task 11: GameRoot 场景 + 主脚本

**Files:**
- Create: `scenes/GameRoot.tscn`
- Create: `scripts/ui/GameRoot.cs`

- [ ] **Step 1: 写 GameRoot.cs**

```csharp
// scripts/ui/GameRoot.cs
using Godot;
using CardSurvival.UI;

namespace CardSurvival;

public partial class GameRoot : Control
{
	private HandArea _handArea = null!;
	private TableArea _tableArea = null!;
	private StatusPanel _statusPanel = null!;
	private Button _drawButton = null!;
	private CardManager _cardManager = null!;
	private CombineSystem _combineSystem = null!;

	public override void _Ready()
	{
		_cardManager = GetNode<CardManager>("/root/CardManager");
		_combineSystem = GetNode<CombineSystem>("/root/CombineSystem");

		SetupUI();
		ConnectSignals();

		_cardManager.DrawInitialHand(5);
		RefreshHand();
	}

	private void SetupUI()
	{
		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 20);
		margin.AddThemeConstantOverride("margin_right", 20);
		margin.AddThemeConstantOverride("margin_top", 20);
		margin.AddThemeConstantOverride("margin_bottom", 20);
		AddChild(margin);

		var mainVBox = new VBoxContainer();
		mainVBox.AddThemeConstantOverride("separation", 12);
		margin.AddChild(mainVBox);

		// Title / status
		_statusPanel = new StatusPanel();
		mainVBox.AddChild(_statusPanel);

		var separator1 = new HSeparator();
		mainVBox.AddChild(separator1);

		// Table area (card play zone)
		var tableLabel = new Label();
		tableLabel.Text = "== 卡牌桌 ==";
		tableLabel.HorizontalAlignment = HorizontalAlignment.Center;
		mainVBox.AddChild(tableLabel);

		_tableArea = new TableArea();
		_tableArea.CustomMinimumSize = new Vector2(0, 200);
		_tableArea.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		_tableArea.SizeFlagsVertical = SizeFlags.ExpandFill;
		mainVBox.AddChild(_tableArea);

		var separator2 = new HSeparator();
		mainVBox.AddChild(separator2);

		// Hand area
		var handLabel = new Label();
		handLabel.Text = "== 手牌 ==";
		handLabel.HorizontalAlignment = HorizontalAlignment.Center;
		mainVBox.AddChild(handLabel);

		_handArea = new HandArea();
		mainVBox.AddChild(_handArea);

		// Draw button
		_drawButton = new Button();
		_drawButton.Text = "抽牌";
		_drawButton.Pressed += OnDrawPressed;
		mainVBox.AddChild(_drawButton);
	}

	private void ConnectSignals()
	{
		_cardManager.OnCardAdded += OnCardChanged;
		_cardManager.OnCardRemoved += OnCardChanged;

		_combineSystem.OnCombineSuccess += OnCombineSuccess;
		_combineSystem.OnCombineFail += OnCombineFail;
	}

	private void OnCardChanged(string cardId)
	{
		GD.Print($"[GameRoot] Card changed: {cardId}");
		RefreshHand();
	}

	private void OnDrawPressed()
	{
		_cardManager.DrawCard();
	}

	private void OnCombineSuccess(string a, string b, string[] results)
	{
		GD.Print($"[GameRoot] COMBINE SUCCESS: {a} + {b} -> {string.Join(", ", results)}");
		foreach (var id in results)
		{
			var card = _cardManager.GetCard(id);
			if (card != null)
				_cardManager.AddCardToHand(card);
		}
	}

	private void OnCombineFail(string a, string b)
	{
		GD.Print($"[GameRoot] COMBINE FAILED: {a} + {b}");
	}

	private void RefreshHand()
	{
		_handArea.Refresh(_cardManager.GetHand());
	}
}
```

- [ ] **Step 2: 创建 GameRoot.tscn 场景**

在 Godot 编辑器中：
1. 新建 Scene → 根节点选 `Control`
2. 将根节点命名为 `GameRoot`
3. 在根节点上附加脚本 `res://scripts/ui/GameRoot.cs`
4. 保存场景到 `res://scenes/GameRoot.tscn`

（注：如无编辑器，可用文本创建 .tscn 文件）

- [ ] **Step 3: 编译验证**

Run: `dotnet build --project e:/project/now`
Expected: Build succeeds

- [ ] **Step 4: Commit**

```bash
cd e:/project/now
git add scripts/ui/GameRoot.cs scenes/GameRoot.tscn
git commit -m "feat: add GameRoot scene with full UI layout"
```

---

### Task 12: 集成验证 — 运行游戏

- [ ] **Step 1: 编译并确认无错误**

```bash
dotnet build --project e:/project/now
```

Expected output: `Build succeeded. 0 Error(s)`

- [ ] **Step 2: 手动运行游戏验证**

用 Godot 编辑器打开 `project.godot`，按 F5 运行，验证以下功能：

| 检查项 | 预期行为 |
|--------|----------|
| 主界面加载 | 看到状态栏 + 卡牌桌 + 手牌区 + 抽牌按钮 |
| 手牌显示 | 启动时自动发 5 张卡牌 |
| 拖拽卡牌 | 可以从手牌区拖拽卡牌 |
| 合成成功 | 将「木头」拖到「石头」→ 手牌区两张消失，生成「石斧」 |
| 合成失败 | 两张不匹配的卡牌拖拽 → 打印 "COMBINE FAILED" |
| 抽牌按钮 | 点击"抽牌"按钮 → 手牌区新增一张卡 |
| 状态栏 | HP 100/100, Hunger 100/100 持续显示 |

- [ ] **Step 3: Commit**

```bash
cd e:/project/now
git add -A
git commit -m "chore: integration verification, finalize MVP"
```
