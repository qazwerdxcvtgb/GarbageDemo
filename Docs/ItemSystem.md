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
| `description` | string | 物品描述（卡面 effectsText 的唯一数据来源，`[TextArea]`） |
| `value` | int | 价值（金币） |
| `weight` | float | 抽取权重（默认 1.0） |
| `category` | ItemCategory | 物品大类 |
| `allowedUseContext` | CardUseContext | 允许使用的行动场合（Flags，默认 All） |

**使用检查 API**：

```csharp
itemData.CanUse(out string reason)   // 虚方法，默认返回 false；FishData/TrashData/ConsumableData 已 override
```

`CanUse` 内部调用 `CheckUseEffects`，依次检查：

1. `DayManager.CurrentPhase == GamePhase.Action`
2. `DayManager.CurrentAction` 与 `allowedUseContext` flags 匹配
3. 存在 trigger 为 `OnUse` 的效果
4. 逐效果 `EffectBase.CanExecute()` 通过

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

### CardUseContext（使用场合，Flags）

**路径**：`Assets/Script/ItemSystem/Data/CardUseContext.cs`

```csharp
[Flags] enum CardUseContext { None = 0, Fishing = 1, Shopping = 2, All = Fishing | Shopping }
```

在卡牌 ScriptableObject 的 Inspector 中通过复选框配置，控制卡牌可在哪些行动阶段被使用。

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
    public virtual (bool canUse, string reason) CanExecute(EffectContext context) => (true, null);
}
```

`CanExecute` 供 `ItemData.CheckUseEffects` 在"使用"前逐效果预检调用。子类可 override 以实现运行时条件检查（如牌堆为空时禁止抽牌）。

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
| `Effect_AllowHangReplace` | 允许更换悬挂（当日有效，详见下文） |
| `Effect_NextFishDiscount` | 下一条鱼体力折扣（消耗品使用，一次性，详见下文） |
| `Effect_PeekPile` | 偷看牌堆顶部 N 张未揭示的牌（详见下文） |
| `Effect_PeekRow` | 偷看一行（同深度 3 个牌堆各 1 张未揭示顶牌，详见下文） |
| `Effect_PeekColumn` | 偷看一列（同序号 3 个牌堆各 1 张未揭示顶牌，详见下文） |
| `Effect_PeekAllPiles` | 偷看全部牌堆（在所有牌堆上叠加浮层展示各堆第一张未揭示牌，详见下文） |
| `Effect_IncreaseDepth` | 增加玩家深度+1（详见下文） |
| `Effect_ModifyGold` | 修改金币（正数增加，负数减少） |
| `Effect_RandomChoice` | 随机选择（从候选效果列表中等概率随机选一个执行，详见下文） |
| `Effect_RemoveOnReveal` | 揭示后无法捕获，自动从牌堆移除（详见下文） |
| `Effect_ConsumeSmallFish` | 捕获时额外消耗手牌中一条随机小型鱼（详见下文） |
| `Effect_Nothing` | 空效果占位，什么都不发生（用于 RandomChoice 候选列表） |
| `Effect_SanityAmplify` | 手牌中持续：疯狂值增加时额外+1（WhileInHand 触发，详见下文） |
| `Effect_RevealCostReduction` | 揭示时降低本鱼卡体力消耗（放弃后失效，详见下文） |
| `Effect_CorruptedFishSanity` | 根据手牌中污秽鱼数量增加疯狂值（不计自身，详见下文） |
| `Effect_ModifyMaxHealth` | 永久修改体力上限（本局有效，正数增加负数减少，详见下文） |
| `Effect_DestroyOnCapture` | 捕获后不入手牌直接销毁（其他捕获效果正常结算，详见下文） |
| `Effect_LoseRandomConsumable` | 随机失去手牌中一张消耗品（无则跳过，不阻止触发） |
| `Effect_RemoveAllPileTops` | 移除所有牌堆顶部卡牌（无论翻开与否，空堆跳过） |
| `Effect_FreeCaptureByHand` | 手牌条件免费捕获（手牌有指定鱼时捕获免费，详见下文） |
| `Effect_FreeCaptureOnSanity` | 疯狂等级免费捕获（疯狂等级精确匹配时捕获免费，详见下文） |
| `Effect_ForceCapture` | 揭示后强制捕获（体力足够时隐藏放弃/取消，必须捕获，详见下文） |
| `Effect_StablePrice` | 标记型：携带此效果的鱼卡售价不受疯狂等级修正影响 |
| `Effect_CorruptedFishDiscount` | 手牌中持续：捕获污秽鱼体力消耗 -N（WhileInHand，多卡叠加，与装备效果叠加） |
| `Effect_ShuffleBackOnAbandon` | 揭示时触发：放弃捕获后鱼卡洗回牌堆随机位置（面朝下），而非留在顶部 FaceUp（详见下文） |
| `Effect_RevealColumn` | 揭示时触发：连锁揭示同列其他牌堆，按队列依次翻牌并触发过滤后的 OnReveal 效果（详见下文） |
| `Effect_CostPerHandFish` | 揭示时触发：手牌中每有一张鱼牌，捕获体力消耗额外 +N（手牌变化实时刷新，详见下文） |
| `Effect_AutoHang` | 商店打开时自动悬挂（手牌中带此效果的鱼自动挂到空槽位，详见下文） |
| `Effect_RemoveRandomSmallFish` | 随机移除手牌中 N 张小型鱼（不足则移除全部，无则跳过，详见下文） |
| `Effect_NoSellNoHang` | 标记型：携带此效果的鱼卡不可出售、不可悬挂（详见下文） |

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
// 钓鱼体力消耗修改链（PassiveEffect 子类订阅，接收当前消耗值与目标鱼数据）
EffectBus.Instance.OnModifyFishingCost += (cost, fishData) => cost - reduction;   // Register
EffectBus.Instance.OnModifyFishingCost -= registeredModifier;                      // Unregister

// FishingTableManager 调用此方法替换原始 staminaCost（下限 0）
int finalCost = EffectBus.Instance.ProcessFishingCost(fish.staminaCost, fishData);

// 捕获完成事件（供有状态效果追踪捕获次数）
EffectBus.Instance.OnFishCaptured += handler;       // 订阅
EffectBus.Instance.NotifyFishCaptured();             // FishingTableManager 捕获成功后调用

// 捕获效果旁路（引用计数，支持多件装备叠加）
EffectBus.Instance.RegisterIgnoreCaptureEffects();   // 装备时 +1
EffectBus.Instance.UnregisterIgnoreCaptureEffects(); // 卸下时 -1
bool skip = EffectBus.Instance.ShouldIgnoreCaptureEffects; // 计数 > 0 时为 true

// 揭示效果旁路（引用计数，支持多件装备叠加）
EffectBus.Instance.RegisterIgnoreRevealEffects();   // 装备时 +1
EffectBus.Instance.UnregisterIgnoreRevealEffects(); // 卸下时 -1
bool skipReveal = EffectBus.Instance.ShouldIgnoreRevealEffects; // 计数 > 0 时为 true

// 每日刷新完成事件（RestoreFullHealth 之后触发，供装备每日体力效果使用）
EffectBus.Instance.OnDayRefreshCompleted += handler;         // 订阅
EffectBus.Instance.NotifyDayRefreshCompleted();               // 触发时机见下方说明

// NotifyDayRefreshCompleted 触发时机（按路径区分）：
// - D1：ExecuteRefreshPhase 末尾立即调用（无装备面板）
// - D2+ 钓鱼：装备面板关闭后由回调触发（确保当天调整的装备生效）
// - D2+ 商店：OnDeclarationChoice 中立即调用（无装备调整机会）

// 悬挂替换许可（由 Effect_AllowHangReplace 激活，每日自动重置）
EffectBus.Instance.EnableHangReplace();                      // 激活许可（消耗品效果调用）
EffectBus.Instance.ResetHangReplace();                       // 重置许可（每日 DayManager.OnDayChanged 自动调用）
bool canReplace = EffectBus.Instance.AllowHangReplace;       // 当前是否允许取下/替换悬挂的鱼

// 一次性钓鱼折扣（由 Effect_NextFishDiscount 激活，捕获后自动消耗，每日重置）
EffectBus.Instance.AddNextFishDiscount(int amount);          // 累加折扣，立即触发 UI 刷新
int discount = EffectBus.Instance.NextFishCostReduction;     // 当前累计折扣值（只读）

// 杂鱼卡选择（由 Effect_TrashCardSelection 激活，引用计数，支持多件装备叠加）
EffectBus.Instance.RegisterTrashSelection();                 // 装备时 +1
EffectBus.Instance.UnregisterTrashSelection();               // 卸下时 -1
bool hasSelection = EffectBus.Instance.TrashSelectionEnabled; // 计数 > 0 时为 true
```

### Effect_ConditionalFishingDiscount

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_ConditionalFishingDiscount.cs`  
**创建**：右键 Project → `ItemSystem/Effects/ConditionalFishingDiscount`

继承 `PassiveEffect`，通过 Inspector 配置条件维度和参数，统一实现所有钓鱼体力折扣场景。

**条件枚举** `FishingDiscountCondition`（定义在同文件中）：

| 值 | 说明 |
|----|------|
| `All` | 所有鱼（无条件） |
| `ByFishType` | 按鱼类类型（Pure/Corrupted） |
| `ByFishSize` | 按鱼类体积（Small/Medium/Large） |
| `FirstFishOfDay` | 每天第一条鱼（当天捕获后失效，次日重置） |

**Inspector 字段**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `condition` | FishingDiscountCondition | 条件维度 |
| `targetFishType` | FishType | 目标鱼类型（仅 ByFishType 时生效） |
| `targetFishSize` | FishSize | 目标鱼体积（仅 ByFishSize 时生效） |
| `reduction` | int | 满足条件时减少的体力消耗点数 |

**预设配置参考**：

| 技能 | condition | targetFishType | targetFishSize | reduction |
|------|-----------|---------------|---------------|-----------|
| 所有鱼体力-1 | All | — | — | 1 |
| 污秽鱼体力-1 | ByFishType | Corrupted | — | 1 |
| 纯净鱼体力-1 | ByFishType | Pure | — | 1 |
| 大型鱼体力-1 | ByFishSize | — | Large | 1 |
| 中型鱼体力-1 | ByFishSize | — | Medium | 1 |
| 小型鱼体力-1 | ByFishSize | — | Small | 1 |
| 每天第一条鱼体力-2 | FirstFishOfDay | — | — | 2 |

### Effect_IgnoreCaptureEffect

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_IgnoreCaptureEffect.cs`  
**创建**：右键 Project → `ItemSystem/Effects/IgnoreCaptureEffect`

继承 `PassiveEffect`，装备后捕获鱼类时跳过鱼卡自身的 OnCapture 效果。通过 `EffectBus` 旁路计数器实现，无额外配置字段。

### Effect_IgnoreRevealEffect

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_IgnoreRevealEffect.cs`  
**创建**：右键 Project → `ItemSystem/Effects/IgnoreRevealEffect`

继承 `PassiveEffect`，装备后翻牌揭示鱼类时跳过鱼卡自身的 OnReveal 效果。通过 `EffectBus` 旁路计数器实现，无额外配置字段。

### Effect_PassiveDailyHealth

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_PassiveDailyHealth.cs`  
**创建**：右键 Project → `ItemSystem/Effects/PassiveDailyHealth`

继承 `PassiveEffect`，每日刷新完成后（`EffectBus.OnDayRefreshCompleted`）随机补充装备临时体力。

| 字段 | 类型 | 说明 |
|------|------|------|
| `minAmount` | int | 每日随机最小值（含，可为负） |
| `maxAmount` | int | 每日随机最大值（含，可为负） |

**逻辑**：
- 随机结果 ≥ 0 → 叠加到 `CharacterState.SetBonusHealth`（装备体力槽）
- 随机结果 < 0 → 直接扣除基础体力（`ModifyHealth`）

**预设配置参考**：

| 技能 | minAmount | maxAmount |
|------|-----------|-----------|
| 每天补充5~6体力 | 5 | 6 |
| 每天补充3~4体力 | 3 | 4 |
| 每天补充1~2体力 | 1 | 2 |
| 每天补充-9~+9体力 | -9 | 9 |
| 每天补充-6~+6体力 | -6 | 6 |

### Effect_PassiveOnCaptureHealth

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_PassiveOnCaptureHealth.cs`  
**创建**：右键 Project → `ItemSystem/Effects/PassiveOnCaptureHealth`

继承 `PassiveEffect`，每次成功捕获鱼类时（`EffectBus.OnFishCaptured`）恢复固定体力。通过普通治疗恢复基础体力，上限 `maxHealth`。

| 字段 | 类型 | 说明 |
|------|------|------|
| `amount` | int | 每次捕获恢复的体力值 |

**预设配置参考**：

| 技能 | amount |
|------|--------|
| 捕获恢复2体力 | 2 |

### Effect_TrashCardSelection

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_TrashCardSelection.cs`  
**创建**：右键 Project → `ItemSystem/Effects/TrashCardSelection`

继承 `PassiveEffect`，装备后放弃捕获获取杂鱼卡时，从杂鱼牌库抽取3张展示选择面板（3选1）。玩家选择1张加入手牌，未选中的归还牌库并洗牌。通过 `EffectBus` 引用计数实现，无额外配置字段。

**注意**：天数推进自动获取的杂鱼卡（D3/D5）不触发此被动，仅在 `FishingTableManager.TryAbandon`（放弃捕获）时生效。

**边界情况**：
- 杂鱼牌库 < 2 张：跳过选择，走原始单张抽取逻辑
- 杂鱼牌库恰好 2 张：显示2选1
- 多件装备叠加：引用计数 ≥ 1 即视为启用，行为不变

**Unity 配置**：需要在 `FishingTableManager` Inspector 上配置 `cardSelectionPanelPrefab`（`Assets/Prefab/FishCardSystem/CardSelectionPanel.prefab`）

### Effect_AllowHangReplace

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_AllowHangReplace.cs`

继承 `EffectBase`，使用时（`OnUse`）激活 `EffectBus.AllowHangReplace`，允许当日内取下和替换悬挂槽中的鱼。进入下一天时由 `EffectBus` 订阅 `DayManager.OnDayChanged` 自动重置。

- 仅影响悬挂系统，不影响装备系统（装备锁定由 `EquipmentPanel.AllowRemoveAndReplace` 独立控制）
- 挂载在 `ConsumableData` 的 `effects` 列表中，`trigger = OnUse`
- 建议 `allowedUseContext = Shopping`（悬挂操作在商店中进行）

### Effect_NextFishDiscount

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_NextFishDiscount.cs`

继承 `EffectBase`，使用时（`OnUse`）为下一条捕获的鱼减少体力消耗。折扣通过 `EffectBus` 一次性折扣机制实现，捕获一条鱼后自动清零，每日重置时也会清零。

| 字段 | 类型 | 说明 |
|------|------|------|
| `reduction` | int | 减少的体力消耗点数（默认 4，最小 1） |

**特性**：
- 多次使用可叠加累计（使用两次 reduction=4 → 下一条鱼体力 -8）
- 捕获一条鱼后折扣自动清零（仅影响下一条鱼）
- 最终体力消耗不会低于 0（由 `ProcessFishingCost` 下限保证）
- 使用后立即刷新牌堆和钓鱼面板的体力消耗显示
- 每日重置时未消耗的折扣自动清零
- 挂载在 `ConsumableData` 的 `effects` 列表中，`trigger = OnUse`
- 建议 `allowedUseContext = Fishing`（钓鱼时使用）

**EffectBus API**：

```csharp
EffectBus.Instance.AddNextFishDiscount(int amount)  // 累加一次性折扣，立即触发 UI 刷新
EffectBus.Instance.NextFishCostReduction             // 当前累计折扣值（只读）
```

### Effect_PeekPile

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_PeekPile.cs`

继承 `EffectBase`，使用时（`OnUse`）触发偷看牌堆流程：收起手牌栏、关闭装备栏、玩家选择牌堆后展示顶部 N 张未揭示的牌。

| 字段 | 类型 | 说明 |
|------|------|------|
| `peekCount` | int | 偷看的牌数（不含已揭示的牌） |

**流程**：`Execute` → `PeekPileHandler.StartPeek(peekCount, PeekMode.Single)` → 锁定 UI → 玩家点击牌堆 → `CardPile.PeekTopCards` 只读获取 → `CardSelectionPanel.Open(maxSelect=0, slotLabels)` 展示（标签显示 "第N张"）→ 取消关闭 → 恢复 UI

**CanExecute 检查**：
- `FishingTableManager` 存在（在钓鱼场景中）
- 至少一个牌堆有未揭示的卡牌
- `PeekPileHandler` 存在且不在偷看中

**建议配置**：`trigger = OnUse`，卡牌 `allowedUseContext = Fishing`

### Effect_PeekRow

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_PeekRow.cs`

继承 `EffectBase`，使用时（`OnUse`）触发偷看一行流程：收起手牌栏、关闭装备栏、玩家选择牌堆后偷看该牌堆所在行（同深度）的 3 个牌堆各 1 张未揭示顶牌。

**流程**：`Execute` → `PeekPileHandler.StartPeek(0, PeekMode.Row)` → 锁定 UI → 玩家点击牌堆 → `GetPileConfig` 反查深度 → `GetPilesByDepth` 获取同行牌堆 → 每堆 `PeekTopCards(1)` → `CardSelectionPanel.Open(maxSelect=0, slotLabels)` 展示（标签显示 "牌堆 N"）→ 取消关闭 → 恢复 UI

**CanExecute 检查**：
- `FishingTableManager` 存在（在钓鱼场景中）
- 至少一个牌堆有未揭示的卡牌
- `PeekPileHandler` 存在且不在偷看中

**建议配置**：`trigger = OnUse`，挂载在 `ConsumableData` 上，`allowedUseContext = Fishing`

### Effect_PeekColumn

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_PeekColumn.cs`

继承 `EffectBase`，使用时（`OnUse`）触发偷看一列流程：收起手牌栏、关闭装备栏、玩家选择牌堆后偷看该牌堆所在列（同序号）的 3 个牌堆各 1 张未揭示顶牌。

**流程**：`Execute` → `PeekPileHandler.StartPeek(0, PeekMode.Column)` → 锁定 UI → 玩家点击牌堆 → `GetPileConfig` 反查序号 → `GetPilesByPoolIndex` 获取同列牌堆 → 每堆 `PeekTopCards(1)` → `CardSelectionPanel.Open(maxSelect=0, slotLabels)` 展示（标签显示 "深度 X"）→ 取消关闭 → 恢复 UI

**CanExecute 检查**：
- `FishingTableManager` 存在（在钓鱼场景中）
- 至少一个牌堆有未揭示的卡牌
- `PeekPileHandler` 存在且不在偷看中

**建议配置**：`trigger = OnUse`，挂载在 `ConsumableData` 上，`allowedUseContext = Fishing`

### Effect_PeekAllPiles

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_PeekAllPiles.cs`

继承 `EffectBase`，使用时（`OnUse`）触发全局偷看流程：收起手牌栏、关闭装备栏，直接在所有牌堆上方叠加浮层，展示各堆第一张未揭示的牌。无需玩家选择牌堆。

**流程**：`Execute` → `PeekPileHandler.StartPeek(1, PeekMode.All)` → 锁定 UI → `ClickInterceptor` 拦截所有牌堆点击 → 遍历 9 堆各 `PeekTopCards(1, skipRevealed: true)` → 有结果的堆实例化 FishCard 浮层叠加显示 → 显示退出按钮 → 玩家点击退出 → 销毁浮层 → 恢复 UI

**CanExecute 检查**：
- `FishingTableManager` 存在（在钓鱼场景中）
- 至少一个牌堆有未揭示的卡牌
- `PeekPileHandler` 存在且不在偷看中

**建议配置**：`trigger = OnUse`，挂载在 `ConsumableData` 上，`allowedUseContext = Fishing`

### Effect_IncreaseDepth

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_IncreaseDepth.cs`

继承 `EffectBase`，使用时（`OnUse`）将玩家深度+1（Depth1→Depth2→Depth3）。不消耗体力，体力成本由卡牌自身价值机制决定。

**逻辑**：
- Depth1 → Depth2，Depth2 → Depth3
- 已在 Depth3 时 `CanExecute` 返回 `(false, "已在最深层")`，阻止卡牌使用
- 通过 `CharacterState.SetDepth()` 触发 `OnDepthChanged` 事件，DepthIndicatorUI 自动响应动画

**建议配置**：`trigger = OnUse`，挂载在 `ConsumableData` 或 `TrashData` 的 `effects` 列表中

### Effect_RandomChoice

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_RandomChoice.cs`

继承 `EffectBase`，泛用随机效果容器。内部持有一个 `[SerializeReference] List<EffectBase> candidates` 候选列表，执行时等概率随机选一个触发。候选项可复用任意已有的 EffectBase 子类（如 `Effect_AddHealth`、`Effect_ModifySanity`、`Effect_ModifyGold` 等）。

**Inspector 配置方式**：在物品的 effects 列表中添加"随机选择"效果，展开后在 `candidates` 子列表中添加具体的候选效果并分别配置参数。

**建议配置**：`trigger = OnUse`，挂载在 `ConsumableData` 或 `TrashData` 的 `effects` 列表中

### Effect_RemoveOnReveal

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_RemoveOnReveal.cs`

继承 `EffectBase`，揭示时（`OnReveal`）通过 `EffectBus` 设置一次性信号。`CardPilePanel` 在翻牌后检测信号，自动移除顶牌、置灰捕获按钮、隐藏放弃按钮，玩家只能关闭面板。

**行为**：
- 揭示时触发，与同卡上的其他 OnReveal 效果一起执行
- 所有揭示效果结算完成后，卡牌从牌堆移除
- 玩家不可捕获（按钮置灰）、不可放弃（按钮隐藏）、不获得杂鱼卡
- 卡牌仍会在面板中展示卡面，玩家可查看后关闭

**EffectBus API**：

```csharp
EffectBus.Instance.SetPendingRemoveOnReveal()      // 效果执行时设置标记
EffectBus.Instance.ConsumePendingRemoveOnReveal()   // CardPilePanel 消费标记（读后清除）
```

**建议配置**：`trigger = OnReveal`，挂载在 `FishData` 的 `effects` 列表中

---

### Effect_ConsumeSmallFish

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_ConsumeSmallFish.cs`

继承 `EffectBase`，捕获时（`OnCapture`）额外随机消耗手牌中一条小型鱼。手牌中无小型鱼时不消耗，不阻止捕获。体力消耗不变。

**行为**：
- 捕获时触发，属于 `TriggerCaptureEffects()` 阶段
- 从 `HandManager.GetHandCards()` 中筛选 `FishData.size == FishSize.Small` 的卡牌
- 随机选择一条进行消耗；无小型鱼则跳过
- 时序：先通过 `HandPanelUI.CardHolder.RemoveCardAndCollapse` 移除视觉卡（含 slot），再通过 `HandManager.RemoveCard` 移除数据层（与 UseCardHandler 模式一致）

**执行时序**（在 `TryCapture` 流程中的位置）：

```
扣体力 → TriggerCaptureEffects() → AddCard(fishData) → RemoveTopCard
               ↑ 效果在此执行，被捕获的鱼尚未加入手牌，不会误消耗
```

**建议配置**：`trigger = OnCapture`，挂载在 `FishData` 的 `effects` 列表中

---

### Effect_SanityAmplify

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_SanityAmplify.cs`

继承 `EffectBase`，使用 `WhileInHand` 触发时机。卡牌在手牌中时，每次疯狂值增加额外 +1。

**行为**：
- 卡牌进入手牌时 `Activate` → `EffectBus.RegisterSanityAmplify()`（计数器 +1）
- 卡牌离开手牌时 `Deactivate` → `EffectBus.UnregisterSanityAmplify()`（计数器 -1）
- `GameManager.ModifySanity(amount)` 通过 `EffectBus.ProcessSanityChange(amount)` 处理，仅在 `amount > 0` 时叠加增幅
- 多张同效果卡牌叠加（引用计数）

**WhileInHand 触发基础设施**（首个此类效果，建立了以下机制）：
- `EffectTrigger.WhileInHand`：新增触发时机枚举值
- `EffectBase.Activate(ctx)` / `Deactivate(ctx)`：新增虚方法，供 WhileInHand 效果 override
- `ItemData.GetEffects()`：新增虚方法，`FishData`/`TrashData`/`ConsumableData` override 返回各自 effects 列表
- `HandManager`：在 `AddCard`/`AddCardData`/`RemoveCard`/`ClearHand` 中自动扫描 WhileInHand 效果并调用 Activate/Deactivate

**边界场景**：
- 拖拽到悬挂槽（经 `HandManager.RemoveCard`）→ 效果取消
- 拖拽失败回到手牌 → 数据未变动，效果保留
- 悬挂被替换回手牌（经 `HandManager.AddCardData`）→ 效果重新激活

**建议配置**：`trigger = WhileInHand`，挂载在 `FishData` 的 `effects` 列表中

---

### Effect_RevealCostReduction

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_RevealCostReduction.cs`

继承 `EffectBase`，揭示时（`OnReveal`）通过 `EffectBus.SetRevealCostReduction(amount)` 设置临时体力折扣。`ProcessFishingCost` 计算最终消耗时自动扣减。`CardPilePanel.ClosePanel` 关闭面板时自动清除。

**行为**：
- 揭示时触发，降低本鱼卡的捕获体力消耗 N 点（`amount` 可配置，默认 1）
- 捕获时折扣通过 `ProcessFishingCost` 生效，按折后体力扣除
- 放弃后折扣清除；再次打开牌堆时卡牌已为 FaceUp，不触发 OnReveal，显示原价
- 取消关闭面板同样清除折扣

**EffectBus API**：

```csharp
EffectBus.Instance.SetRevealCostReduction(amount)   // 效果执行时设置临时折扣
EffectBus.Instance.ClearRevealCostReduction()        // CardPilePanel.ClosePanel 清除
```

**UI 表现**：
- 卡面体力数字通过 `FishCardFrontDisplay.UpdateStaminaCostDisplay` 自动刷新（订阅 `OnFishingModifierChanged`），显示折后值并变色
- 捕获按钮可用状态通过 `CanAffordCapture` → `ProcessFishingCost` 自动反映折后体力

**建议配置**：`trigger = OnReveal`，挂载在 `FishData` 的 `effects` 列表中

---

### Effect_CorruptedFishSanity

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_CorruptedFishSanity.cs`

继承 `EffectBase`，触发时统计手牌中 `FishData.fishType == FishType.Corrupted` 的数量，增加等量疯狂值。

**行为**：
- 触发时机由 `trigger` 字段配置（OnReveal / OnCapture / OnUse 均可）
- 统计 `HandManager.GetHandCards()` 中污秽鱼数量
- 此卡本身不计入统计：
  - OnReveal / OnCapture：此卡在牌堆中，天然不在手牌
  - OnUse：此卡仍在手牌中，通过 `trigger == OnUse` 判断自动减 1
- 污秽鱼数量为 0 时跳过（不调用 ModifySanity）

**建议配置**：挂载在 `FishData` 的 `effects` 列表中，`trigger` 按需设置

---

### Effect_ModifyMaxHealth

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_ModifyMaxHealth.cs`

继承 `EffectBase`，调用 `CharacterState.ModifyMaxHealth(amount)` 永久修改体力上限。

**行为**：
- `amount` 可配置（正数增加，负数减少，默认 1）
- 本局游戏内永久生效
- `CharacterState.ResetState()` 时自动恢复到 `initialMaxHealth`（Awake 记录的初始值）
- 若当前体力超过新上限，自动调整为新上限

**建议配置**：`trigger` 按需设置，挂载在任意卡牌的 `effects` 列表中

---

### Effect_DestroyOnCapture

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_DestroyOnCapture.cs`

继承 `EffectBase`，捕获时（`OnCapture`）通过 `EffectBus` 一次性标记通知 `FishingTableManager.TryCapture` 跳过 `AddCard`。

**行为**：
- 捕获时触发，与其他 OnCapture 效果一起在 `TriggerCaptureEffects()` 阶段执行
- 其他捕获效果正常结算（如 Effect_ConsumeSmallFish、Effect_ModifyGold 等）
- 效果执行后设置 `EffectBus.SetPendingDestroyOnCapture()` 标记
- `TryCapture` 在效果结算后检查标记，若为 true 则跳过 `HandManager.AddCard`
- 牌堆 `RemoveTopCard` 仍正常执行，`NotifyFishCaptured` 仍正常触发

**EffectBus API**：

```csharp
EffectBus.Instance.SetPendingDestroyOnCapture()      // 效果执行时设置标记
EffectBus.Instance.ConsumePendingDestroyOnCapture()   // TryCapture 消费标记（读后清除）
```

**建议配置**：`trigger = OnCapture`，挂载在 `FishData` 的 `effects` 列表中

---

### Effect_LoseRandomConsumable

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_LoseRandomConsumable.cs`

继承 `EffectBase`，随机移除手牌中一张消耗品。手牌无消耗品时静默跳过，不阻止效果触发（`CanExecute` 始终为 true）。

**行为**：
- 从 `HandManager.GetHandCards()` 筛选 `ConsumableData` 类型卡牌
- 无消耗品时打印日志并 return，不影响其他效果或操作
- 有消耗品时随机选择一张，先移除视觉卡（`HandPanelUI.CardHolder.RemoveCardAndCollapse`），再移除数据层（`HandManager.RemoveCard`）
- 时序与 `UseCardHandler` / `Effect_ConsumeSmallFish` 一致

**建议配置**：可搭配任意 `trigger`，常见搭配 `OnCapture` / `OnReveal` / `OnUse`

---

### Effect_RemoveAllPileTops

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_RemoveAllPileTops.cs`

继承 `EffectBase`，移除所有牌堆（3×3 网格）顶部的卡牌，无论 FaceDown 还是 FaceUp。空牌堆自动跳过。

**行为**：
- 通过 `FishingTableManager.Instance.GetAllPiles()` 获取所有非 null 牌堆
- 对每个 `CardCount > 0` 的牌堆调用 `RemoveTopCard()`
- `RemoveTopCard` 内部自动处理状态转换（FaceUp/FaceDown → FaceDown/Empty）和视觉刷新
- 被移除的卡牌不进入手牌，直接丢弃
- 日志打印每张被移除卡牌的名称和移除总数

**建议配置**：可搭配任意 `trigger`，常见搭配 `OnUse` / `OnCapture` / `OnReveal`

---

### Effect_FreeCaptureByHand

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_FreeCaptureByHand.cs`

继承 `EffectBase`，`trigger = OnReveal`。当手牌中存在符合条件的鱼时，该鱼卡的捕获费用变为 0。

**与 Effect_RevealCostReduction 的区别**：
- RevealCostReduction：OnReveal 设固定折扣值，存在 EffectBus，面板关闭清除，放弃后消失
- FreeCaptureByHand：条件始终在鱼卡数据上，每次 ProcessFishingCost 调用时实时评估，放弃后再打开仍然生效

**条件系统**（`FreeCaptureCondition` 枚举，可扩展）：

| 枚举值 | 含义 |
|--------|------|
| `BySize` | 手牌中有指定体积（`targetSize`）的鱼 |
| `ByNameContains` | 手牌中有名称包含指定文本（`nameKeyword`）的鱼 |

**行为**：
- `Execute`（OnReveal）：调用 `EffectBus.RegisterFreeCaptureSource()` 启动 OnHandChanged → OnFishingModifierChanged 桥接
- `CheckHandCondition()`：遍历 `HandManager.GetHandCards()` 筛选 `FishData` 并按条件匹配，返回 `bool`
- `ProcessFishingCost` 在计算链最前面直接检查 `fishData.effects` 中是否有此效果并调用 `CheckHandCondition()`，满足则返回 0

**UI 更新机制**：
- 牌堆 FaceUp 卡面：`FishCardFrontDisplay`（`CardContextMode.Pile` 时自动启用）已订阅 `OnFishingModifierChanged`，手牌变化时自动刷新费用显示（免费时变绿色显示 0）
- 面板捕获按钮：`CardPilePanel.ShowFaceUp` 订阅 `OnFishingModifierChanged` → `RefreshCaptureButton` → `UpdateCaptureButtonState`
- 清理：`TryCapture` 中对含此效果的鱼卡调用 `UnregisterFreeCaptureSource`，引用计数归零时取消 OnHandChanged 订阅

**建议配置**：`trigger = OnReveal`，挂载在 `FishData` 的 `effects` 列表中

---

### Effect_FreeCaptureOnSanity

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_FreeCaptureOnSanity.cs`

继承 `EffectBase`。当玩家疯狂等级 **恰好等于** 配置等级时，该鱼卡的捕获费用变为 0。

**与 Effect_FreeCaptureByHand 的区别**：
- FreeCaptureByHand：条件基于手牌鱼类构成，桥接 `OnHandChanged` → `OnFishingModifierChanged`
- FreeCaptureOnSanity：条件基于 `GameManager.CurrentSanityLevel` 精确匹配，桥接 `OnSanityLevelChanged` → `OnFishingModifierChanged`

**可配置字段**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `requiredLevel` | `SanityLevel` | 触发免费捕获的疯狂等级（默认 `Level2`，Inspector 可调） |

**行为**：
- `Execute`（OnReveal）：调用 `EffectBus.RegisterSanityFreeCaptureSource()` 启动 OnSanityLevelChanged → OnFishingModifierChanged 桥接
- `CheckSanityCondition()`：返回 `GameManager.Instance.CurrentSanityLevel == requiredLevel`
- `ProcessFishingCost` 在 `CheckFreeCaptureByHand` 之后、链式计算之前，检查 `fishData.effects` 中是否有此效果并调用 `CheckSanityCondition()`，满足则返回 0

**UI 更新机制**：
- 牌堆 FaceUp 卡面：`FishCardFrontDisplay`（`CardContextMode.Pile` 时自动启用）已订阅 `OnFishingModifierChanged`，疯狂等级变化时自动刷新费用显示
- 面板捕获按钮：`CardPilePanel.ShowFaceUp` 订阅 `OnFishingModifierChanged` → `RefreshCaptureButton` → `UpdateCaptureButtonState`
- 清理：`TryCapture` 中对含此效果的鱼卡调用 `UnregisterSanityFreeCaptureSource`，引用计数归零时取消 OnSanityLevelChanged 订阅

**与其他免费捕获效果的关系**：与 `Effect_FreeCaptureByHand` 为 OR 关系——任一条件满足即免费

**注意事项**：
- 使用 `==` 精确匹配疯狂等级，不是 `>=`（仅在指定等级时免费）
- 放弃捕获后卡牌留在牌堆顶 FaceUp，疯狂等级变化时费用会动态刷新
- 再次打开面板时重新订阅 `OnFishingModifierChanged`，实时响应疯狂等级变化

**建议配置**：`trigger = OnReveal`，挂载在 `FishData` 的 `effects` 列表中

---

### Effect_ForceCapture

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_ForceCapture.cs`

继承 `EffectBase`，`trigger = OnReveal`。揭示后如果玩家体力足以捕获，则强制捕获——隐藏放弃和取消按钮，玩家只能点击捕获。

**实现模式**：沿用 `pendingRemoveOnReveal` 的 Set/Consume 模式：
- `Execute`（OnReveal）：调用 `EffectBus.SetPendingForceCapture()`
- `CardPilePanel.OnRevealClicked`：消费标记后，若 `CanAffordCapture` 为 true，调用 `ShowFaceUpForced`（仅显示捕获按钮）

**按钮状态**：

| 条件 | capture | abandon | cancel |
|------|---------|---------|--------|
| 强制捕获 + 体力足够 | 显示 | 隐藏 | 隐藏 |
| 强制捕获 + 体力不足 | 显示（置灰） | 显示 | 隐藏 |
| 无强制捕获（正常揭示） | 显示 | 显示 | 隐藏 |

**EffectBus API**：
```csharp
EffectBus.Instance.SetPendingForceCapture()      // 效果执行时设置标记
EffectBus.Instance.ConsumePendingForceCapture()   // CardPilePanel 消费标记（读后清除）
```

**注意事项**：
- 标记一次性消费，再次打开已揭示牌堆时不再强制（标记已被消费，且再次打开不触发 OnReveal）
- 与 `autoRemove` 的优先级：`autoRemove` 先检查，已移除则不走强制捕获
- `revealCostReduction` 等折扣效果在 `ProcessFishingCost` 中计算，`CanAffordCapture` 已包含

**建议配置**：`trigger = OnReveal`，挂载在 `FishData` 的 `effects` 列表中

---

### Effect_ShuffleBackOnAbandon

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_ShuffleBackOnAbandon.cs`

继承 `EffectBase`，`trigger = OnReveal`。揭示后若玩家放弃捕获，鱼卡从牌堆顶部移除并随机插回牌堆任意位置（面朝下），而非留在顶部保持 FaceUp。

**实现模式**：沿用 `pendingRemoveOnReveal` / `pendingForceCapture` 的 Set/Consume 标记模式：
- `Execute`（OnReveal）：调用 `EffectBus.SetPendingShuffleBackOnAbandon()`
- `CardPilePanel.OnAbandonClicked`：消费标记后，调用 `targetPile.RemoveTopCard()` + `targetPile.InsertCardAtRandom(card)`

**牌堆新增方法**：

```csharp
CardPile.InsertCardAtRandom(FishData card)  // 随机位置插入（含顶部和底部），状态强制 FaceDown
```

**EffectBus API**：

```csharp
EffectBus.Instance.SetPendingShuffleBackOnAbandon()      // 效果执行时设置标记
EffectBus.Instance.ConsumePendingShuffleBackOnAbandon()   // CardPilePanel 消费标记（读后清除）
```

**边界情况**：
- **玩家选择捕获**：标记未被消费，不影响正常捕获流程
- **牌堆只剩一张卡**：`RemoveTopCard` 后牌堆为空，`InsertCardAtRandom` 插入后恢复为 1 张 FaceDown
- **与 Effect_ForceCapture 共存**：ForceCapture 隐藏放弃按钮，玩家无法放弃，标记自然不会被消费

**建议配置**：`trigger = OnReveal`，挂载在 `FishData` 的 `effects` 列表中

---

### Effect_RevealColumn

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_RevealColumn.cs`

继承 `EffectBase`，`trigger = OnReveal`。揭示后连锁揭示同列（同 `poolIndex`）其他牌堆，不消耗体力、不弹面板，按队列依次翻牌并触发过滤后的 OnReveal 效果。

**实现模式**：沿用 Set/Consume 标记模式：
- `Execute`（OnReveal）：调用 `EffectBus.SetPendingRevealColumn()`
- `CardPilePanel.OnRevealClicked`：消费标记后调用 `FishingTableManager.StartColumnReveal(targetPile)`
- `FishingTableManager` 通过协程 `ColumnRevealCoroutine` 按队列处理，每张卡之间间隔 0.4 秒

**效果过滤规则**（连锁揭示触发的 OnReveal 效果）：

| 处理方式 | 效果类型 |
|----------|---------|
| 跳过 | `Effect_RemoveOnReveal`、`Effect_ForceCapture`、`Effect_ShuffleBackOnAbandon`、`Effect_RevealCostReduction`、`Effect_RevealColumn`（防递归） |
| 正常执行 | `Effect_FreeCaptureByHand`、`Effect_FreeCaptureOnSanity`、`Effect_CorruptedFishSanity`、`Effect_ModifySanity` 等所有其他 OnReveal 效果 |

**EffectBus API**：

```csharp
EffectBus.Instance.SetPendingRevealColumn()      // 效果执行时设置标记
EffectBus.Instance.ConsumePendingRevealColumn()   // CardPilePanel 消费标记（读后清除）
```

**FishingTableManager API**：

```csharp
FishingTableManager.Instance.StartColumnReveal(CardPile sourcePile)  // 启动连锁揭示协程
```

**边界情况**：
- **同列全部已 FaceUp 或 Empty**：不启动协程
- **同列包含超出玩家深度的牌堆**：仍然揭示（效果驱动，不受深度限制）
- **连锁揭示的卡也带 Effect_RevealColumn**：被过滤跳过，不会递归
- **连锁揭示的卡带 Effect_FreeCaptureByHand**：正常注册，玩家后续手动点击该牌堆时免费捕获生效
- **协程运行期间玩家操作原始卡牌面板**：互不干扰，协程在 FishingTableManager 上独立运行

**建议配置**：`trigger = OnReveal`，挂载在 `FishData` 的 `effects` 列表中

---

### Effect_CostPerHandFish

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_CostPerHandFish.cs`

继承 `EffectBase`，`trigger = OnReveal`。揭示后根据手牌中鱼卡数量增加额外捕获体力消耗。手牌变化时费用实时刷新。

**与 Effect_FreeCaptureByHand 的区别**：
- FreeCaptureByHand：条件满足时费用变 0（免费捕获）
- CostPerHandFish：按手牌鱼卡数量**增加**消耗，可被折扣链抵消

**可配置字段**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `costPerFish` | int | 每张手牌鱼卡增加的额外体力消耗（默认 1） |

**行为**：
- `Execute`（OnReveal）：调用 `EffectBus.RegisterHandFishCostSource()` 启动 OnHandChanged → OnFishingModifierChanged 桥接
- `GetHandFishCount()`：遍历 `HandManager.GetHandCards()` 统计 `FishData` 数量，返回 `count * costPerFish`
- `ProcessFishingCost` 在 baseCost 之后、OnModifyFishingCost 链之前，检查 `fishData.effects` 中是否有此效果并调用 `GetHandFishCount()` 累加
- 额外消耗加入后，后续折扣链（装备被动、揭示折扣、一次性折扣）可正常抵消，最终 `Mathf.Max(0, result)` 保证不低于 0

**EffectBus API**：

```csharp
EffectBus.Instance.RegisterHandFishCostSource()    // 效果执行时注册桥接
EffectBus.Instance.UnregisterHandFishCostSource()   // 捕获成功后注销桥接
```

**UI 更新机制**：
- 牌堆 FaceUp 卡面：`FishCardFrontDisplay` 已订阅 `OnFishingModifierChanged`，手牌变化时自动刷新费用显示
- 面板捕获按钮：`CardPilePanel.ShowFaceUp` 订阅 `OnFishingModifierChanged` → `RefreshCaptureButton` → `UpdateCaptureButtonState`
- 清理：`TryCapture` 中对含此效果的鱼卡调用 `UnregisterHandFishCostSource`

**与免费捕获效果的关系**：ProcessFishingCost 先检查免费条件（提前 return 0），免费时不会走到额外消耗逻辑

**建议配置**：`trigger = OnReveal`，挂载在 `FishData` 的 `effects` 列表中

---

### Effect_AutoHang

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_AutoHang.cs`

继承 `EffectBase`。不通过 `TriggerEffects` 触发，由 `ShopHangController.AutoHangEligibleCards()` 主动检查。

**触发时机**：商店面板打开时（`ShopPanel.OpenPanel` → `AutoHangEligibleCards` → `RestoreHangState`）。

**行为**：
1. 遍历 `HandManager.GetHandCards()` 中的 `FishData`
2. 检查 `fishData.effects` 中是否存在 `Effect_AutoHang`
3. 从 `ShopManager.GetAllHangSlots()` 查找空槽（`null`）
4. 匹配成功：`ShopManager.TryHangFish(slotIndex, fishData)` + `HandManager.RemoveCard(fishData)`
5. 后续 `RestoreHangState()` 统一恢复视觉

**调用链**：
```
ShopPanel.OpenPanel()
  → hangController.AutoHangEligibleCards()   // 数据层：自动悬挂
  → hangController.RestoreHangState()        // 视觉层：恢复所有悬挂卡
```

**注意事项**：
- 多张带此效果的鱼卡按手牌顺序依次填充空槽，槽满后剩余留在手牌
- 自动悬挂仅操作数据层，手牌视觉通过 `OnHandChanged` 自动刷新
- 已占用的槽不会被替换（仅填充空槽）
- 不依赖 `CanAccept` 检查（`CanAccept` 用于拖拽判断，自动悬挂直接操作数据层）

**建议配置**：trigger 无特殊要求，挂载在 `FishData` 的 `effects` 列表中

---

### Effect_RemoveRandomSmallFish

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_RemoveRandomSmallFish.cs`

继承 `EffectBase`，随机移除手牌中 N 张小型鱼。手牌中小型鱼不足 N 张时移除全部，无小型鱼则静默跳过，不阻止效果触发（`CanExecute` 始终为 true）。

| 字段 | 类型 | 说明 |
|------|------|------|
| `count` | int | 移除的小型鱼数量（默认 1，最小 1） |

**行为**：
- 从 `HandManager.GetHandCards()` 筛选 `FishData.size == FishSize.Small` 的卡牌
- 实际移除数量为 `min(count, 可用小型鱼数量)`
- 逐张随机选择并移除，先移除视觉卡（`HandPanelUI.CardHolder.RemoveCardAndCollapse`），再移除数据层（`HandManager.RemoveCard`）
- 时序与 `UseCardHandler` / `Effect_LoseRandomConsumable` 一致

**建议配置**：可搭配任意 `trigger`，常见搭配 `OnCapture` / `OnReveal` / `OnUse`

---

### Effect_NoSellNoHang

**路径**：`Assets/Script/ItemSystem/Effects/Implementations/Effect_NoSellNoHang.cs`

继承 `EffectBase`。标记效果，`Execute` 不执行操作。由商店出售和悬挂系统主动检查。

**拦截点**：

| 系统 | 检查位置 | 行为 |
|------|----------|------|
| 悬挂 | `ShopHangSlot.CanAccept(ItemCard)` | 检测到此效果 → 返回 `false`，卡牌无法进入悬挂槽 |
| 出售 | `ShopSellController.OnCardSelected(ItemCard, bool)` | 检测到此效果 → 调用 `Deselect()`，阻止卡牌被选中出售 |

**静态辅助方法**：
```csharp
public static bool HasEffect(FishData fish)
```
遍历 `fish.effects` 检查是否包含 `Effect_NoSellNoHang` 类型实例。

**注意事项**：
- 不影响卡牌的捕获、使用、丢弃等其他行为
- 悬挂拦截覆盖手动拖拽和槽位替换（均经过 `CanAccept`）
- `AutoHangEligibleCards` 不受影响（仅检查 `Effect_AutoHang`，两个效果不应同时存在）

**建议配置**：trigger 无特殊要求，挂载在 `FishData` 的 `effects` 列表中

---

### EquipmentManager

**路径**：`Assets/Script/ItemSystem/Managers/EquipmentManager.cs`  
管理玩家装备槽（`FishingRod` / `FishingGear`）的装备和卸下。

```csharp
// 新增事件（供 UI 层订阅刷新显示）
EquipmentManager.Instance.OnEquipped    // Action<EquipmentData>
EquipmentManager.Instance.OnUnequipped  // Action<EquipmentData>
```

### ShopManager 牌库管理

**路径**：`Assets/Script/ShopSystem/ShopManager.cs`  
**模式**：单例 `ShopManager.Instance`，`DontDestroyOnLoad`

ShopManager 从 `ItemPool` 深拷贝三类非鱼牌库，运行时与 ItemPool 完全解耦：

| 牌池 | 类型 | 说明 |
|------|------|------|
| `consumablePool` | `List<ConsumableData>` | 消耗品牌序 |
| `equipmentPool` | `List<EquipmentData>` | 装备牌序 |
| `trashPool` | `List<TrashData>` | 杂鱼牌序 |

```csharp
ShopManager.Instance.DrawConsumable()    // 顺序抽取一张消耗品
ShopManager.Instance.DrawEquipment()     // 顺序抽取一张装备
ShopManager.Instance.DrawTrash()         // 顺序抽取一张杂鱼

// 批量抽取（供 CardSelectionPanel 调用方使用）
ShopManager.Instance.DrawTopItems(ItemCategory category, int count)  // 从指定类型牌堆顶部取n张
ShopManager.Instance.ReturnToTop(ItemCategory category, List<ItemData> cards)  // 将卡牌插回牌堆顶部

// 杂鱼牌池洗牌（Fisher-Yates，供杂鱼选择归还后调用）
ShopManager.Instance.ShuffleTrashPool()  // 对 trashPool 执行随机洗牌
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

**获取来源**：钓鱼面板揭示鱼类后点击"放弃" → `FishingTableManager.TryAbandon()` → `ShopManager.Instance.DrawTrash()`

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
