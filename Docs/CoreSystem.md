# 核心系统

> [← 返回索引](INDEX.md)  
> 覆盖脚本：`Script/Core/` 目录下所有脚本

---

## GameManager

**路径**：`Assets/Script/Core/GameManager.cs`  
**模式**：单例 `GameManager.Instance`，`DontDestroyOnLoad`

### 疯狂值

| 等级 | 疯狂值范围 |
|------|-----------|
| Level0 | 0 |
| Level1 | 1–3 |
| Level2 | 4–6 |
| Level3 | 7–9 |
| Level4 | 10–12 |
| Level5 | 13+ |

```csharp
GameManager.Instance.SanityValue           // 读取当前疯狂值
GameManager.Instance.CurrentSanityLevel    // 读取当前疯狂等级（SanityLevel 枚举）
GameManager.Instance.ModifySanity(int)     // 修改疯狂值（正增负减）
GameManager.Instance.SetSanity(int)        // 直接设置疯狂值
GameManager.Instance.ResetSanity()         // 重置为 0
GameManager.Instance.CalculateSanityLevel(int) // 根据数值计算等级
```

### 交互事件

```csharp
GameManager.Instance.TriggerInteractionPressed()   // 触发交互按钮事件
GameManager.Instance.OnInteractionPressed          // UnityEvent，其他系统可订阅
```

### 空牌统计

```csharp
GameManager.Instance.RecordEmptyCardDrawn(int count) // 记录空牌数量
GameManager.Instance.EmptyCardDrawnCount             // 读取累计空牌数
GameManager.Instance.ResetEmptyCardCount()           // 重置统计
```

### 游戏重置

```csharp
GameManager.Instance.ResetAll()   // 重置疯狂值归零 + 空牌统计归零（由 DayManager 调用）
```

### 事件

| 事件 | 类型 | 说明 |
|------|------|------|
| `OnInteractionPressed` | `UnityEvent` | 玩家按下交互键 |
| `OnSanityChanged` | `IntValueChangedEvent` | 疯狂值变化 |
| `OnSanityLevelChanged` | `SanityLevelChangedEvent` | 疯狂等级变化 |

---

## CharacterState

**路径**：`Assets/Script/Core/CharacterState.cs`  
**挂载**：玩家 GameObject，**非全局单例**，需持有引用

### 属性

```csharp
characterState.MaxHealth      // 最大体力（默认 10）
characterState.CurrentHealth  // 当前总体力 = 基础体力 + 装备临时体力（getter 返回 base + bonus）
characterState.BaseHealth     // 基础体力（只读，不含装备临时体力）
characterState.BonusHealth    // 装备临时体力（只读，受伤优先扣除，每日刷新重置）
characterState.GoldAmount     // 金币数量（≥0）
characterState.CurrentDepth   // 玩家当前深度（FishDepth 枚举，默认 Depth1）
```

> **装备临时体力**：装备被动效果可在每日刷新后给予 `bonusHealth`，使总体力超过 `maxHealth`。
> 受伤时优先扣除装备体力，治疗仅恢复基础体力（上限 `maxHealth`），`RestoreFullHealth` 会清零装备体力。

### 方法

```csharp
characterState.ModifyHealth(int)          // 修改体力（受伤先扣 bonus 再扣 base；治疗仅恢复 base，上限 maxHealth）
characterState.ModifyMaxHealth(int)       // 修改最大体力
characterState.ModifyGold(int)            // 修改金币（正增负减）
characterState.RestoreFullHealth()        // 基础体力恢复至满值，装备临时体力清零
characterState.SetBonusHealth(int)        // 设置装备临时体力（负值截断为 0，触发 OnBonusHealthChanged + OnHealthChanged）
characterState.HasEnoughGold(int)         // 检查金币是否足够
characterState.IsDead()                   // 总体力是否为 0
characterState.IsHealthFull()             // 基础体力是否已满（不含装备体力）
characterState.SetDepth(FishDepth)        // 设置玩家深度等级
characterState.CanAccessDepth(FishDepth)  // 检查是否可访问指定深度（currentDepth >= pileDepth）
characterState.ResetState()              // 重置到初始值（bonusHealth 归零，由 DayManager 调用）
```

### 事件

| 事件 | 说明 |
|------|------|
| `OnHealthChanged` | 总体力（base + bonus）变化 |
| `OnMaxHealthChanged` | 最大体力变化 |
| `OnBonusHealthChanged` | 装备临时体力变化 |
| `OnGoldChanged` | 金币变化 |
| `OnDepthChanged` | 玩家深度变化，参数类型 `FishDepth` |

---

## GameEvents

**路径**：`Assets/Script/Core/GameEvents.cs`  
全局事件类型定义，其他脚本共用这些类型。

```csharp
// 整数值变化事件（体力 / 金币 / 疯狂值等通用）
public class IntValueChangedEvent : UnityEvent<int> { }

// 疯狂等级变化事件
public class SanityLevelChangedEvent : UnityEvent<SanityLevel> { }

// 玩家深度变化事件
public class FishDepthChangedEvent : UnityEvent<FishDepth> { }

// 疯狂等级枚举（详见 GameManager 章节的等级对照表）
public enum SanityLevel { Level0, Level1, Level2, Level3, Level4, Level5 }

// 游戏阶段枚举（天数系统使用）
public enum GamePhase { DayStart, Refresh, Declaration, Action, DayEnd, GameOver }

// 天数变化事件
public class DayChangedEvent : UnityEvent<int> { }

// 游戏阶段变化事件
public class GamePhaseChangedEvent : UnityEvent<GamePhase> { }

// 装备面板开关事件
public class EquipmentPanelToggleEvent : UnityEvent { }
```
