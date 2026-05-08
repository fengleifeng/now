---
title: 一纸策划搞定卡牌生存游戏：从核心循环到 Godot 架构
slug: card-survival-fantasy-forest-game-design
summary: 从拖拽卡牌到合成生存，一份可落地的卡牌生存游戏 Godot + C# 完整策划方案
description: 涵盖核心循环、卡牌系统、拖拽组合、生存时间系统、事件探索等完整系统设计，包含 Godot 节点架构、C# 代码示例、JSON 配置方案，以及 MVP 范围和 Sprint 开发拆分。强调全面数据驱动，避免硬编码逻辑。
---

## 一、项目定位

### 1. 游戏类型

- 生存 + 卡牌驱动 + 资源管理
- 单人 / 轻 Roguelike（可扩展种子世界）

### 2. 核心玩法一句话

> **拖拽卡牌 → 触发合成/事件 → 维持生存 → 探索森林 → 解锁新卡组**

## 二、Core Loop（核心循环）

**抽卡 → 拖拽组合 → 触发结果 → 消耗/产出资源 → 存活 or 死亡 → 解锁新内容**

示例流程：

1. 抽到「树枝」
2. 拖到「石头」→ 合成「工具」
3. 工具 +「野兽」→ 战斗 / 资源掉落
4. 获得「肉」「皮」
5. 吃肉维持生命
6. 夜晚来临 → 消耗火堆 / 死亡判定

## 三、核心系统设计

### 1. 卡牌系统（核心）

**卡牌类型：**

| 类型 | 示例 |
|------|------|
| 资源卡 | 木头、石头、食物 |
| 生物卡 | 兔子、狼 |
| 工具卡 | 斧头、火堆 |
| 建筑卡 | 营地、陷阱 |
| 状态卡 | 受伤、中毒 |
| 事件卡 | 暴雨、夜晚 |

**卡牌数据结构（C#）：**

```csharp
public class CardData
{
    public string Id;
    public string Name;
    public CardType Type;
    public int Stack;
    public int MaxStack;
    public List<CardTag> Tags;
    public CardEffect OnUse;
    public CardEffect OnCombine;
}
```

### 2. 拖拽组合系统（关键）

类似原作的灵魂系统。

**规则：**

- 卡牌 A 拖到卡牌 B → 触发组合判断
- 支持：规则匹配、条件触发、多结果概率

**合成规则示例（JSON）：**

```json
{
  "input": ["Wood", "Stone"],
  "output": ["Tool"],
  "chance": 1.0
}
```

**C# 结构：**

```csharp
public class CombineRule
{
    public string CardA;
    public string CardB;
    public List<string> Results;
    public float Chance;
}
```

### 3. 生存系统

**玩家属性：**

```csharp
public class PlayerState
{
    public int Health;
    public int Hunger;
    public int Energy;
    public int Sanity;
}
```

**机制：**

- 饥饿 → 掉血
- 夜晚 → 高风险
- 不同天气 → Buff / Debuff

### 4. 时间系统

Tick 制（类似放置游戏）：

```csharp
public float DayProgress; // 0~1
```

**时间影响：**

| 时段 | 影响 |
|------|------|
| 白天 | 探索安全 |
| 夜晚 | 野兽增强 |
| 雨天 | 火堆效率下降 |

### 5. 事件系统

动态事件驱动：

```csharp
public class GameEvent
{
    public string Id;
    public Func<bool> Condition;
    public Action Execute;
}
```

示例：

- 低生命 → 触发"虚弱"
- 夜晚 + 无火 → "野兽袭击"

### 6. 探索系统（扩展核心）

**区域卡（Map 作为卡牌）：** 森林 / 湖泊 / 洞穴

拖人类卡 → 区域卡，结果：

- 获得资源
- 遭遇事件
- 新卡解锁

## 四、Godot 架构设计

### 1. 节点结构

```
GameRoot
 ├── CardManager
 ├── CombineSystem
 ├── PlayerSystem
 ├── TimeSystem
 ├── UI
 └── EventSystem
```

### 2. 卡牌节点（Godot）

```csharp
public partial class CardNode : Control
{
    public CardData Data;

    public override void _Ready()
    {
        // 初始化UI
    }

    public void OnDragStart() { }

    public void OnDrop(CardNode target)
    {
        CombineSystem.TryCombine(this, target);
    }
}
```

### 3. Combine System

```csharp
public class CombineSystem
{
    public static void TryCombine(CardNode a, CardNode b)
    {
        var rule = FindRule(a.Data.Id, b.Data.Id);
        if (rule != null)
        {
            Execute(rule, a, b);
        }
    }
}
```

### 4. 数据驱动（强烈建议）

> **全部用 JSON / Scriptable 数据，避免硬编码逻辑。**

数据目录：

```
/data/cards.json
/data/combine_rules.json
/data/events.json
```

## 五、UI 设计

### 1. 主界面

- 卡牌桌（Grid）
- 手卡区
- 状态栏（HP / 饥饿）
- 时间条

### 2. 交互

- 拖拽卡牌
- Hover 显示提示
- **合成动画（很重要）**

## 六、成长与解锁

### 1. 解锁机制

- 生存天数
- 完成特定组合
- 探索到区域

### 2. Meta 进度

- 解锁新卡池
- 新地图
- 新规则

## 七、MVP 范围（优先实现）

**必做：**

- 卡牌拖拽
- 合成系统
- 10~20 张卡
- 10 条合成规则
- 基础生存系统

**先不要做：**

- 多地图
- 复杂 AI
- 网络

## 八、开发拆分

### Sprint 1（基础）

- 卡牌展示
- 拖拽系统
- 卡牌数据结构

### Sprint 2（核心玩法）

- Combine 系统
- 基础规则
- 玩家状态

### Sprint 3（可玩性）

- 时间系统
- 简单事件
- UI 完善

### Sprint 4（扩展）

- 探索
- 更多卡牌
- 平衡调整

## 九、难点提醒

### 1. 合成规则爆炸

> **用 Tag 替代硬编码。**

```json
"tags": ["wood", "material"]
```

规则写成：`["material", "stone"] → tool`

### 2. 卡牌状态复杂

卡牌必须支持：

- Stack（堆叠）
- Durability（耐久）
- 状态附加（Buff / Debuff）

### 3. 可扩展性

> **一切数据驱动！！！**

## 十、下一步建议

可继续落地到代码级：

- Godot C# 完整项目结构模板
- 卡牌拖拽完整实现
- 合成系统可运行 Demo
- JSON 配置样例（直接用）
- 类似原版 UI 复刻
