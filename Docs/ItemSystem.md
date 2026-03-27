# 物品系统

> [← 返回索引](INDEX.md)  
> 覆盖脚本：`ItemSystem/Data/` · `ItemSystem/Effects/` · `ItemSystem/Managers/`

---

## 数据类层级

```
ItemData（abstract，ScriptableObject）
├── FishData          鱼类卡牌
├── ConsumableData    消耗品
├── TrashData         杂鱼（放弃钓鱼时获得，可使用效果）
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

### EffectData（效果数据基类）

**路径**：`Assets/Script/ItemSystem/Effects/EffectData.cs`

抽象 ScriptableObject 基类，用于以资产方式定义效果（被动效果模式）。子类 `PassiveEffect` 用于装备的持续效果。

```csharp
public abstract class EffectData : ScriptableObject
{
    public abstract void Apply(EffectContext context);
    public abstract void Remove(EffectContext context);
}
```

> `PassiveEffect`：继承 `EffectData`，持久被动效果基类（带 `PassiveTrigger`）

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

### EffectBus

**路径**：`Assets/Script/ItemSystem/Effects/EffectBus.cs`  
**模式**：单例（自动创建 DontDestroyOnLoad）

全局被动效果事件总线，被动效果通过 Register/Unregister 接入，使用方无需关心哪些效果已激活。

```csharp
// 钓鱼体力消耗修改链（PassiveEffect 子类订阅）
EffectBus.Instance.OnModifyFishingCost += cost => cost - reduction;   // Register
EffectBus.Instance.OnModifyFishingCost -= registeredModifier;          // Unregister

// FishingTableManager 调用此方法替换原始 staminaCost（下限 0）
int finalCost = EffectBus.Instance.ProcessFishingCost(fish.staminaCost);
```

### Effect_FishingStaminaDiscount

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_FishingStaminaDiscount.cs`  
**创建**：右键 Project → `ItemSystem/Effects/FishingStaminaDiscount`

继承 `PassiveEffect`，装备时每次钓鱼消耗减少 `reduction` 点（`[Min(0)]`，下限 0）。

---

### EquipmentManager

**路径**：`Assets/Script/ItemSystem/Managers/EquipmentManager.cs`  
管理玩家装备槽（`FishingRod` / `FishingGear`）的装备和卸下。

```csharp
// 新增事件（供 UI 层订阅刷新显示）
EquipmentManager.Instance.OnEquipped    // Action<EquipmentData>
EquipmentManager.Instance.OnUnequipped  // Action<EquipmentData>
```

---

## TrashData（杂鱼卡）

**路径**：`Assets/Script/ItemSystem/Data/TrashData.cs`  
**创建**：右键 Project → `ItemSystem/Trash`  
**资源目录**：`Resources/Items/Trash/`

| 字段 | 类型 | 说明 |
|------|------|------|
| `effects` | `List<EffectBase>` | 效果列表（`[SerializeReference]`） |

```csharp
trashData.TriggerUseEffects()   // 触发 OnUse 效果（手牌中点击使用）
trashData.TriggerEffects(EffectTrigger trigger)
trashData.GetItemInfo()         // 返回类型/价值/效果信息字符串
```

**获取来源**：钓鱼面板揭示鱼类后点击"放弃" → `FishingTableManager.TryAbandon()` → `ItemPool.DrawItem(ItemCategory.Trash)`

**手牌集成**：`HandPanelUI.OnCardAdded` 识别 `TrashData` → 实例化 `TrashCard.prefab` → 加入 `FishCardHolder`

---

## ConsumableData（消耗品卡）

**路径**：`Assets/Script/ItemSystem/Data/ConsumableData.cs`  
**创建**：右键 Project → `ItemSystem/Consumable`  
**资源目录**：`Resources/Items/Consumable/`（待规划）

| 字段 | 类型 | 说明 |
|------|------|------|
| `effects` | `List<EffectBase>` | 效果列表（`[SerializeReference]`） |

```csharp
consumableData.TriggerUseEffects()   // 触发 OnUse 效果
consumableData.TriggerEffects(EffectTrigger trigger)
consumableData.GetItemInfo()         // 返回类型/价值/效果信息字符串
```

**获取来源**：暂未实现，预留扩展。

**手牌集成**：`HandPanelUI.OnCardAdded` 识别 `ConsumableData` → 实例化 `ConsumableCard.prefab` → 加入 `FishCardHolder`
