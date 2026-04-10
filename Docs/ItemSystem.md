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
EffectBus.Instance.NotifyDayRefreshCompleted();               // DayManager.ExecuteRefreshPhase 末尾调用

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
