# 核心系统

> [← 返回索引](INDEX.md)  
> 覆盖脚本：`GameManager.cs` · `CharacterState.cs` · `GameEvents.cs`

---

## GameManager

**路径**：`Assets/Script/GameManager.cs`  
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

### 事件

| 事件 | 类型 | 说明 |
|------|------|------|
| `OnInteractionPressed` | `UnityEvent` | 玩家按下交互键 |
| `OnSanityChanged` | `IntValueChangedEvent` | 疯狂值变化 |
| `OnSanityLevelChanged` | `SanityLevelChangedEvent` | 疯狂等级变化 |

---

## CharacterState

**路径**：`Assets/Script/CharacterState.cs`  
**挂载**：玩家 GameObject，**非全局单例**，需持有引用

### 属性

```csharp
characterState.MaxHealth      // 最大体力（默认 10）
characterState.CurrentHealth  // 当前体力（范围 0~MaxHealth）
characterState.GoldAmount     // 金币数量（≥0）
```

### 方法

```csharp
characterState.ModifyHealth(int)     // 修改体力（正增负减，自动 Clamp）
characterState.ModifyMaxHealth(int)  // 修改最大体力
characterState.ModifyGold(int)       // 修改金币（正增负减）
characterState.RestoreFullHealth()   // 满体力恢复
characterState.HasEnoughGold(int)    // 检查金币是否足够
characterState.IsDead()              // 体力是否为 0
characterState.IsHealthFull()        // 体力是否已满
```

### 事件

| 事件 | 说明 |
|------|------|
| `OnHealthChanged` | 当前体力变化 |
| `OnMaxHealthChanged` | 最大体力变化 |
| `OnGoldChanged` | 金币变化 |

---

## GameEvents

**路径**：`Assets/Script/GameEvents.cs`  
全局事件类型定义，其他脚本共用这些类型。

```csharp
// 整数值变化事件（体力 / 金币 / 疯狂值等通用）
public class IntValueChangedEvent : UnityEvent<int> { }

// 疯狂等级变化事件
public class SanityLevelChangedEvent : UnityEvent<SanityLevel> { }

// 疯狂等级枚举（详见 GameManager 章节的等级对照表）
public enum SanityLevel { Level0, Level1, Level2, Level3, Level4, Level5 }
```
