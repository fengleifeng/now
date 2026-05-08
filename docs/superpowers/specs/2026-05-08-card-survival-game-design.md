# 卡牌生存游戏 — 技术设计文档

**日期**: 2026-05-08
**范围**: Sprint 1+2（可玩核心原型）
**技术栈**: Godot 4.6 + C#，纯 2D UI
**数据方案**: JSON 配置 + Godot Resource

---

## 一、架构概览

混合架构：游戏逻辑在 C# Autoload 服务层，UI 层用 Control 节点动态生成。

```
GameRoot (Node2D)
 ├── CardManager (Autoload)      — 卡牌数据管理
 ├── CombineSystem (Autoload)    — 合成规则匹配
 ├── PlayerSystem (Autoload)     — 玩家状态
 ├── GameUI (Scene)              — UI 布局
 │    ├── HandArea               — 手牌区（自动排列）
 │    ├── TableArea              — 卡牌桌（自由拖放区）
 │    └── StatusPanel            — HP/饥饿/时间状态栏
 └── CardNode (动态生成)          — 单张卡牌 Control 节点
```

## 二、文件结构

```
now/
├── data/
│   ├── cards.json
│   └── combine_rules.json
├── scripts/
│   ├── autoload/
│   │   ├── CardManager.cs
│   │   ├── CombineSystem.cs
│   │   └── PlayerSystem.cs
│   ├── data/
│   │   ├── CardData.cs
│   │   ├── CombineRule.cs
│   │   ├── PlayerState.cs
│   │   └── DataLoader.cs
│   └── ui/
│       ├── CardNode.cs
│       ├── HandArea.cs
│       ├── TableArea.cs
│       └── StatusPanel.cs
├── scenes/
│   └── GameRoot.tscn
├── docs/
│   ├── 策划.md
│   └── superpowers/specs/
│       └── 2026-05-08-card-survival-game-design.md
└── project.godot
```

## 三、数据模型

### CardData

```csharp
public enum CardType { Resource, Creature, Tool, Building, Status, Event }
public enum CardTag { Wood, Stone, Material, Food, Weapon, Fire, Shelter, Animal, Danger }

public class CardData
{
    public string Id;
    public string Name;
    public CardType Type;
    public int Stack;
    public int MaxStack;
    public List<CardTag> Tags;
    public int Durability;        // -1 = 无限耐久
}
```

### CombineRule

```csharp
public class CombineRule
{
    public string CardA;          // 卡牌 ID 或 Tag 名
    public string CardB;
    public List<string> Results;  // 产出卡牌 ID 列表
    public float Chance;          // 0~1
    public bool MatchByTag;       // true=Tag匹配, false=精确ID匹配
}
```

### PlayerState

```csharp
public class PlayerState
{
    public int Health;
    public int MaxHealth;
    public int Hunger;
    public int MaxHunger;
    public float DayProgress;     // 0~1
}
```

## 四、Autoload 服务

### CardManager

- **职责**: 卡牌数据加载、手牌管理、抽牌/弃牌
- **依赖**: DataLoader
- **信号**: OnCardAdded, OnCardRemoved
- **方法**: LoadCards(), DrawCard(), AddCardToHand(), RemoveCardFromHand(), GetCard(string id)

### CombineSystem

- **职责**: 接收拖拽事件 → 匹配规则 → 执行合成结果
- **依赖**: CardManager（只读）, PlayerSystem（只读）
- **信号**: OnCombineSuccess, OnCombineFail
- **方法**: TryCombine(CardNode a, CardNode b)
  - 取出两个卡牌的 ID 和 Tags
  - 遍历规则表，先精确匹配，再 Tag 匹配
  - 命中后按 Chance 概率判定
  - 成功 → 移除输入卡、生成输出卡、发信号
  - 失败 → 发 OnCombineFail 信号

### PlayerSystem

- **职责**: 维护玩家状态、饥饿/生命消耗、死亡判定
- **依赖**: 无
- **信号**: OnPlayerDamaged, OnPlayerDeath
- **方法**: TakeDamage(), Heal(), ConsumeHunger(), TickDayProgress(), IsDead()

## 五、UI 节点

### CardNode

- 继承 Control，动态生成
- `_GetDragData()` 开始拖拽，设置预览
- `_CanDropData()` 判断目标是否是另一张 CardNode
- `_DropData()` 调用 `CombineSystem.TryCombine(this, target)`
- Hover 时显示 Tooltip（名称、类型、Tags）

### HandArea

- 继承 GridContainer 或 HBoxContainer
- 监听 CardManager 信号 → 重建子 CardNode
- 每张卡牌可拖出

### TableArea

- 继承 Control（自由布局）
- 接收手牌区拖入的卡牌
- 两张卡牌重叠 → 触发合成判断

### StatusPanel

- 继承 HBoxContainer
- 每帧从 PlayerSystem 取值刷新 HP 条、饥饿条、时间进度

## 六、数据流

```
JSON → DataLoader → CardManager（卡牌缓存）
                        ↓
                   HandArea ← 抽牌
                        ↓
             玩家拖拽 CardNode A → CardNode B
                        ↓
                CombineSystem.TryCombine()
                  ↙              ↘
           规则匹配成功         规则不匹配
             ↓                    ↓
     CardManager 移除旧卡    OnCombineFail
     CardManager 生成新卡       ↓
             ↓              播放失败动画
         UI 刷新
             ↓
      PlayerSystem 更新
```

## 七、JSON 配置

### cards.json（12 张 MVP 卡牌）

木头、石头、浆果、兔子、狼、石斧、火堆、陷阱、肉、兽皮、受伤、暴雨

### combine_rules.json（10 条规则）

涵盖：树枝+石头→石斧、斧头+兔子→肉+皮、斧头+狼→肉+皮、树枝+树枝→火堆、材料+材料→陷阱、肉+火堆→熟食、食物+火堆→熟食、火堆+暴雨→灭火、受伤+食物→治愈、狼+陷阱→肉+皮

## 八、设计原则

- **数据驱动**: 加新卡牌/新规则只改 JSON，零 C# 代码
- **Tag 匹配**: `MatchByTag: true` 让一条规则覆盖多张同类卡牌，避免规则爆炸
- **逻辑与 UI 分离**: Autoload 不引用 UI 类型，CardNode 只做表现和输入
- **Godot 原生**: 拖拽用内置 `_GetDragData/_DropData`，通信用 `[Signal]`
