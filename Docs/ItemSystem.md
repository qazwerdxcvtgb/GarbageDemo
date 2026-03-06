# 物品系统

> [← 返回索引](INDEX.md)  
> 覆盖脚本：`ItemSystem/Data/` · `ItemSystem/Effects/` · `ItemSystem/Managers/`

---

## 数据类层级

```
ItemData（abstract，ScriptableObject）
├── FishData          鱼类卡牌
├── ConsumableData    消耗品
├── TrashData         杂物
└── EquipmentData     装备
```

---

## ItemData（基类）

**路径**：`Assets/Script/ItemSystem/Data/ItemData.cs`

| 字段 | 类型 | 说明 |
|------|------|------|
| `itemName` | string | 物品名称 |
| `icon` | Sprite | 物品图标 |
| `description` | string | 物品描述 |
| `value` | int | 价值（金币） |
| `weight` | float | 抽取权重（默认 1.0） |
| `category` | ItemCategory | 物品大类 |

---

## FishData

**路径**：`Assets/Script/ItemSystem/Data/FishData.cs`  
**创建**：右键 Project → `ItemSystem/Fish`

| 字段 | 类型 | 说明 |
|------|------|------|
| `depth` | FishDepth | 深度（Depth1/2/3） |
| `size` | FishSize | 体积（Small/Medium/Large） |
| `staminaCost` | int | 消耗体力 |
| `fishType` | FishType | 类型（Pure/Corrupted） |
| `effects` | `List<EffectBase>` | 效果列表（用 `[SerializeReference]`） |

```csharp
fishData.TriggerRevealEffects()    // 触发 OnReveal 效果
fishData.TriggerCaptureEffects()   // 触发 OnCapture 效果
fishData.TriggerUseEffects()       // 触发 OnUse 效果
fishData.TriggerDiscardEffects()   // 触发 OnDiscard 效果
fishData.TriggerEffects(EffectTrigger trigger) // 手动触发指定时机
```

---

## 枚举定义

**路径**：`Assets/Script/ItemSystem/Data/ItemEnums.cs`

```csharp
enum ItemCategory   { Fish, Trash, Consumable, Equipment }
enum FishDepth      { Depth1, Depth2, Depth3 }
enum FishSize       { Small, Medium, Large }
enum FishType       { Pure, Corrupted }
enum EquipmentSlot  { FishingRod, FishingGear }
enum PassiveTrigger { OnFishing, OnCapture, OnUse, OnDamage, Always }
```

中文映射：`ItemEnumsExtensions.cs` 提供 `.ToChineseText()` 扩展方法。

---

## 效果系统

### EffectTrigger（触发时机）

```csharp
enum EffectTrigger { OnReveal, OnCapture, OnUse, OnDiscard, OnEquip, OnUnequip }
```

### EffectBase（基类）

**路径**：`Assets/Script/ItemSystem/Effects/EffectBase.cs`

```csharp
public abstract class EffectBase
{
    public EffectTrigger trigger;
    public abstract string DisplayName { get; }
    public abstract void Execute(EffectContext context);
}
```

### EffectContext

```csharp
public class EffectContext
{
    public GameObject Target;  // 效果目标（通常为玩家 GameObject）
}
```

### 内置效果实现

| 类名 | 功能 |
|------|------|
| `Effect_AddHealth` | 增加体力 |
| `Effect_AddRandomHealth` | 随机增加体力 |
| `Effect_ModifySanity` | 修改疯狂值 |
| `Effect_DrawCards` | 抽取卡牌 |
| `Effect_RandomHealthOrSanity` | 随机修改体力或疯狂值 |

> `InstantEffect`：立即生效效果基类  
> `PassiveEffect`：持久被动效果基类（带 `PassiveTrigger`）

---

## 管理器

### ItemPool

**路径**：`Assets/Script/ItemSystem/Managers/ItemPool.cs`  
**模式**：单例 `ItemPool.Instance`

**内部结构**：
- 鱼类：按 `FishDepth × 3个子池` 分拣
- 非鱼类：按 `ItemCategory` 字典管理

```csharp
ItemPool.Instance.DrawFish(FishDepth depth, int poolIndex) // 按深度+池索引抽鱼
ItemPool.Instance.DrawItem(ItemCategory category)          // 按分类抽物品
```

**钓鱼桌槽位映射**（`FishingTablePanel` 用）：

| 槽位索引 | 深度 | poolIndex |
|---------|------|-----------|
| 0、1、2 | Depth1 | 0、1、2 |
| 3、4、5 | Depth2 | 0、1、2 |
| 6、7、8 | Depth3 | 0、1、2 |

### EquipmentManager

**路径**：`Assets/Script/ItemSystem/Managers/EquipmentManager.cs`  
管理玩家装备槽（`FishingRod` / `FishingGear`）的装备和卸下。
