# 商店系统

> [← 返回索引](INDEX.md)  
> 覆盖脚本：`ShopSystem/` · `FishCardSystem/Manager/CrossHolderSystem.cs`（全局跨 Holder 系统）

---

## UI 布局

```
┌─────────────────────────────────────────────────┐
│  [商店] Tab            [悬挂] Tab                 │  ← 标签切换栏
├─────────────────────────────────────────────────┤
│                                                 │
│  [购买消耗品  X💰]   [购买装备  X💰]              │
│                                                 │  ← 商店页内容（ShopTabContent）
│  已选总价: XXX                                   │
│  [售卖]                                         │
│                                                 │
├─────────────────────────────────────────────────┤
│   [ 悬挂槽 0 ]    [ 悬挂槽 1 ]    [ 悬挂槽 2 ]    │  ← 悬挂页内容（HangTabContent）
│                                                 │
├─────────────────────────────────────────────────┤
│                   手牌区域                        │  ← 始终可见
└─────────────────────────────────────────────────┘
```

---

## 脚本清单

| 脚本 | 路径 | 职责 |
|------|------|------|
| `ShopPanel` | `ShopSystem/ShopPanel.cs` | 面板主控：开关、标签切换、子控制器协调 |
| `ShopManager` | `ShopSystem/ShopManager.cs` | 单例：消耗品牌序、悬挂槽持久化数据 |
| `ShopSellController` | `ShopSystem/ShopSellController.cs` | 售卖：订阅手牌选中、计算总价、执行售卖 |
| `ShopBuyController` | `ShopSystem/ShopBuyController.cs` | 购买：消耗品按钮逻辑、金币检查 |
| `ShopEquipmentController` | `ShopSystem/ShopEquipmentController.cs` | 购买：装备按钮逻辑、金币检查、价格读取 |
| `ShopHangController` | `ShopSystem/ShopHangController.cs` | 悬挂管理：三槽协调、恢复存档、执行悬挂 |
| `ShopHangSlot` | `ShopSystem/ShopHangSlot.cs` | 单个悬挂槽：接收卡牌、视觉状态、存档恢复 |
| `EquipmentPanel` | `ShopSystem/EquipmentPanel.cs` | 装备面板：开关、双槽管理 |
| `EquipmentSlotUI` | `ShopSystem/EquipmentSlotUI.cs` | 单个装备槽：接收装备卡、实现 ICardSlot |

---

## 一、ShopManager

**模式**：单例，`DontDestroyOnLoad`  
**命名空间**：`ShopSystem`

### 牌序管理

遵循 `FishingTableManager` 的深拷贝模式：调用 `ItemPool.GetCategoryDeck` 获取洗牌后列表，深拷贝后购买时顺序取出并移除。

```csharp
ShopManager.Instance.InitializePools()          // 首次进入商店场景调用（重复调用安全）
ShopManager.Instance.DrawConsumable()           // 顺序抽取消耗品，池空返回 null
ShopManager.Instance.IsConsumablePoolEmpty      // bool 属性
ShopManager.Instance.DrawEquipment()            // 顺序抽取装备，池空返回 null
ShopManager.Instance.PeekEquipment()            // 预览下一张装备（不移除），供价格显示
ShopManager.Instance.IsEquipmentPoolEmpty       // bool 属性
ShopManager.Instance.ResetPools()               // 重置牌序和悬挂槽（由 DayManager 调用）
```

### 悬挂数据

```csharp
ShopManager.Instance.TryHangFish(slotIndex, fish)  // 写入 HangSlots[i]
ShopManager.Instance.GetHangSlot(slotIndex)         // 读取（供恢复视觉）
ShopManager.Instance.GetAllHangSlots()              // FishData[3]，供 GameManager 终局结算
```

---

## 二、ShopPanel

**命名空间**：`ShopSystem`  
**场景层级**：与 `FishingTablePanel` 同级，挂在同一 Canvas 下，二者互斥显示  
**单例**：`ShopPanel.Instance`

### 开关 API

```csharp
ShopPanel.Instance.OpenPanel()   // 打开商店，默认显示商店页
ShopPanel.Instance.ClosePanel()  // 关闭商店，清理视觉和事件
```

> **注意（2026-04-02变更）**：商店面板的关闭按钮已禁用。商店仅通过天数系统的声明阶段（DeclarationPanel）选择"去商店"打开，通过"下一天"按钮退出（DayManager 调用 `ClosePanel()`）。

### 标签切换

| 标签 | 内容 GameObject | 跨 Holder 拖拽 |
|------|----------------|------------------------------|
| 商店 | `ShopTabContent` | 停用（禁止跨 Holder 拖拽） |
| 悬挂 | `HangTabContent` | 激活 |

### 与 HandPanelUI 联动

- `OpenPanel()` 调用 `HandPanelUI.LockExpanded()`：强制展开手牌，隐藏折叠按钮
- `ClosePanel()` 调用 `HandPanelUI.UnlockExpanded()`：恢复折叠权限和按钮

### Inspector 配置

| 字段 | 说明 |
|------|------|
| `shopTabContent` | 商店页根 GameObject |
| `hangTabContent` | 悬挂页根 GameObject |
| `shopTabButton / hangTabButton` | 标签切换按钮 |
| `closeButton` | 关闭按钮 |
| `sellController / buyController / hangController` | 子控制器引用 |
| `handPanelUI` | 手牌面板引用 |

---

## 三、ShopSellController

**触发条件**：`ShopPanel.OpenPanel()` → `OnShopOpen()`

### 流程

1. 订阅 `HandManager.OnHandChanged`（手牌变化后刷新订阅）
2. 订阅所有 `ItemCard.SelectEvent`
3. 玩家点击卡牌选中/取消 → 重新计算总价 → 更新 `totalPriceText` + `sellButton`
4. 点击"售卖"按钮 → 移除选中卡、增加金币

### 售卖规则

- 所有手牌类型（Fish / Trash / Consumable）均可售卖
- 价格 = `ItemData.value` 累加，不做额外调整
- 已锁定卡牌（`isLocked = true`，即悬挂槽中的鱼牌）不响应选中事件

### Inspector 配置

| 字段 | 说明 |
|------|------|
| `totalPriceText` | 已选总价文本（无选中时隐藏） |
| `sellButton` | 售卖按钮（有选中时可点击） |
| `cardHolder` | 手牌 FishCardHolder 引用 |
| `playerState` | CharacterState（可留空，自动 FindObjectOfType） |

---

## 四、ShopBuyController

**范围**：消耗品购买

### 按钮状态规则

| 条件 | 按钮状态 |
|------|---------|
| 金币不足 | 置灰 |
| 消耗品池耗尽 | 置灰 + "消耗品 已售罄" |
| 正常 | 可点击，显示"购买消耗品 X💰" |

### Inspector 配置

| 字段 | 说明 |
|------|------|
| `buyConsumableButton` | 购买消耗品按钮 |
| `buyConsumableButtonText` | 按钮文本 |
| `consumableCost` | 购买消耗品花费金币（默认 5） |
| `playerState` | CharacterState（可留空，自动 FindObjectOfType） |

---

## 四·五、ShopEquipmentController

**范围**：装备购买，与 `ShopBuyController` 并列挂在 `ShopTabContent` 下

### 价格规则

装备价格优先读取 `EquipmentData.value`；若 value = 0，则使用 Inspector 中配置的 `defaultEquipmentCost`（默认 8）。

### Inspector 配置

| 字段 | 说明 |
|------|------|
| `buyEquipmentButton` | 购买装备按钮 |
| `buyEquipmentButtonText` | 按钮文本 |
| `defaultEquipmentCost` | 默认装备价格（EquipmentData.value=0 时使用） |
| `playerState` | CharacterState（可留空，自动 FindObjectOfType） |

---

## 五、跨 Holder 拖拽（CrossHolderSystem）

### 工作原理

全局单例，挂在场景 Managers 节点上。详见 [FishCardSystem.md → CrossHolderSystem](FishCardSystem.md)。

**与悬挂槽交互的三种场景**：
- 场景1：手牌鱼卡 → 空悬挂槽（正常悬挂）
- 场景2：手牌鱼卡 → 已有卡的悬挂槽（替换：旧卡回手牌，新卡进槽）
- 场景3：悬挂槽内卡 → 手牌区域（拖出：卡牌归还手牌）；拖到其他位置一律归回原槽

### 关键 API

```csharp
// 以下均由框架自动调用，业务代码不需要手动操作
CrossHolderSystem.Instance.RegisterSource(holder)
CrossHolderSystem.Instance.RegisterTarget(rect, slotObject)
CrossHolderSystem.Instance.OnCardDroppedToSlot   // UnityEvent<ItemCard, object>，ShopHangController 订阅
```

### Inspector 配置

| 字段 | 说明 |
|------|------|
| `yThreshold` | 触发跨 Holder 的 Y 轴偏移阈值（世界单位，默认 1.5） |

---

## 六、ShopHangSlot

每个悬挂槽实现 `ICardSlot` 接口，三个实例由 `ShopHangController` 统一管理。

### 槽位状态

| 状态 | 视觉 |
|------|------|
| 空 | `emptyVisual` 显示（虚线边框 + 提示文字） |
| 已占用 | FishCard + FishCardVisual（完整动画） |

### 悬挂锁定

`CanAccept` 除类型校验外还检查 `EffectBus.AllowHangReplace`：已占用且未激活悬挂替换许可时拒绝替换，空槽始终允许悬挂。该锁定与装备锁定互相独立。

### 关键 API

```csharp
slot.AcceptCard(ItemCard card)                     // 接管卡牌（替换时由控制器先 ReleaseCard）
slot.ReleaseCard()                                 // 放弃卡牌（不销毁）
slot.RestoreCard(FishData data, GameObject prefab) // 从存档恢复视觉（不触及 HandManager）
slot.ClearSlot()                                   // 清空视觉（ClosePanel 时调用）
slot.IsOccupied                                    // bool 属性
slot.OccupiedCard                                  // ItemCard 属性
slot.CanAccept(ItemCard card)                      // 接受 FishData；已占用且未解锁时返回 false
```

---

## 七、ShopHangController

### 悬挂锁定机制

默认状态下，已悬挂的鱼不可被取下或替换。通过 `EffectBus.AllowHangReplace` 控制：

- **锁定（默认）**：`AllowHangReplace = false`
  - 空槽可以接受新鱼卡悬挂
  - 已占用槽拒绝替换（`ShopHangSlot.CanAccept` 返回 false）
  - 拖出已占用槽的卡牌时动画归回原槽
- **解锁**：使用携带 `Effect_AllowHangReplace` 效果的消耗品后激活，当日有效
  - 允许替换已占用槽和拖出归还手牌
  - 进入下一天时由 `EffectBus` 订阅 `DayManager.OnDayChanged` 自动重置

> 悬挂锁定与装备锁定（`EquipmentPanel.AllowRemoveAndReplace`）互相独立，互不影响。

### 悬挂流程（场景1 & 场景2）

1. 用户切换到悬挂页 → `HangTabContent.SetActive(true)` → `ShopHangSlot.OnEnable` 注册 Target → CrossHolderSystem 自动激活
2. 用户将 FishCard 向上拖拽超过阈值并释放
3. `CrossHolderSystem.OnCardDroppedToSlot` 触发
4. `ShopHangController` 执行：
   - **场景2（槽位有卡，需 AllowHangReplace = true）**：先调用 `EjectCardToHand` 将旧卡归还手牌
   - 继续执行正常悬挂：写入 `ShopManager`、从手牌移除、`AcceptCard()`

### 拖出归还流程（场景3，需 AllowHangReplace = true）

1. 槽位卡向手牌区域拖拽（向下过阈值）
2. `CrossHolderSystem.OnCardEjectedToHand` 触发
3. 若 `AllowHangReplace = false`：卡牌动画归回原槽，不执行后续操作
4. 若 `AllowHangReplace = true`：`ShopHangController.EjectCardToHand` 执行：
   - `slot.ReleaseCard()` → `ShopManager.TryHangFish(index, null)` → `HandManager.AddCard()` → `handHolder.AddCard()`

### Inspector 配置

| 字段 | 说明 |
|------|------|
| `hangSlots[3]` | 三个 ShopHangSlot 引用 |
| `handCardHolder` | 手牌 FishCardHolder（可留空，自动从 HandPanelUI 查找） |
| `fishCardPrefab` | FishCard 预制体（存档恢复时实例化） |

---

## 八、HandPanelUI 新增 API

> 商店系统对 `HandPanelUI` 的扩展，路径：`FishCardSystem/Manager/HandPanelUI.cs`

```csharp
HandPanelUI.LockExpanded()    // 强制展开，隐藏 ToggleButton，禁止折叠
HandPanelUI.UnlockExpanded()  // 恢复 ToggleButton，解除锁定
HandPanelUI.CardHolder        // 公开 FishCardHolder 引用（只读属性）
```

---

## 九、ItemCard 扩展

> 路径：`FishCardSystem/Core/ItemCard.cs`

新增字段：

```csharp
public bool isLocked = false;
// true 时：屏蔽所有输入事件（Pointer / Drag / Select）
// 本版本暂不添加锁定视觉（遮罩/图标），预留后续实现
```

---

## 十、FishCardHolder 扩展

> 路径：`FishCardSystem/Manager/FishCardHolder.cs`

新增枚举和字段：

```csharp
public enum CrossHolderRole { None, Source, Target, Both }
[SerializeField] public CrossHolderRole crossHolderRole = CrossHolderRole.None;
```

手牌 Holder 应设置为 `Source`，悬挂槽（若使用 FishCardHolder 实现）设置为 `Target`。

---

---

## 十一（新）、装备面板（EquipmentPanel）

**路径**：`Assets/Script/ShopSystem/EquipmentPanel.cs`  
**模式**：单例 `EquipmentPanel.Instance`

独立面板，通过事件或按钮调用 `TogglePanel()` 打开/关闭。面板中包含两个 `EquipmentSlotUI`。

### 装备锁定机制

默认状态下，装备可装入空槽但不可取下或替换。通过 `AllowRemoveAndReplace` 属性控制：

- **普通模式（默认）**：`AllowRemoveAndReplace = false`
  - 空槽可以接受装备卡（`EquipmentSlotUI.CanAccept` 通过）
  - 已占用槽拒绝替换（`CanAccept` 返回 false）
  - 拖出已占用槽的卡牌时动画归回原槽
  - 显示关闭按钮，隐藏确认按钮和遮罩
- **钓鱼准备模式**：`AllowRemoveAndReplace = true`
  - 玩家在声明阶段选择"去钓鱼"时由 `DayManager` 调用 `OpenPanelForFishing()` 触发
  - 允许装备/取下/替换所有操作
  - 显示确认按钮和全屏遮罩，隐藏关闭按钮
  - 点击确认或关闭面板后自动重置为锁定状态

### 开关 API

```csharp
EquipmentPanel.Instance.OpenPanel()             // 普通模式：仅空槽可装备
EquipmentPanel.Instance.OpenPanelForFishing()   // 钓鱼准备模式：完全解锁
EquipmentPanel.Instance.ClosePanel()            // 关闭并重置为锁定状态
EquipmentPanel.Instance.TogglePanel()           // 绑定按钮 onClick（始终普通模式）
EquipmentPanel.Instance.AllowRemoveAndReplace   // bool 只读属性，供 EquipmentSlotUI 读取
```

### Inspector 配置

| 字段 | 说明 |
|------|------|
| `panelRoot` | 面板根节点 GameObject |
| `rodSlot` | 鱼竿 EquipmentSlotUI |
| `gearSlot` | 渔轮 EquipmentSlotUI |
| `closeButton` | 关闭按钮（普通模式显示） |
| `confirmButton` | 确认按钮（钓鱼准备模式显示） |
| `fishingOverlay` | 全屏遮罩 GameObject（钓鱼准备模式显示，覆盖钓鱼页面） |
| `handPanelUI` | HandPanelUI 引用（可留空，自动查找） |

### EquipmentSlotUI Inspector 配置

| 字段 | 说明 |
|------|------|
| `allowedSlot` | `EquipmentSlot.FishingRod` 或 `FishingGear` |
| `emptyVisual` | 空槽时显示的 GameObject |
| `occupiedOverlay` | 已占用时的覆盖层（可选） |
| `borderImage` | 边框 Image（悬停高亮用） |

---

## 十二、Unity Editor 配置（脚本编译后）

### 商店场景最小配置

1. 新建 `ShopManager` GameObject（`DontDestroyOnLoad` 单例，也可挂在 Managers 父节点）
2. 新建 `CrossHolderSystem` GameObject（挂载 `CrossHolderSystem.cs`，建议放在 Managers 父节点）
3. Canvas 下新建 `ShopPanel` GameObject，挂载 `ShopPanel.cs`，配置所有 SerializeField 引用
4. `ShopPanel` 子节点：
   - `ShopTabContent`（含 `ShopSellController`、`ShopBuyController`、`ShopEquipmentController` 组件）
   - `HangTabContent`（含 `ShopHangController` 组件及三个 `ShopHangSlot`）
5. `HandPanelUI` 上的 `FishCardHolder` 的 `crossHolderRole` 设为 `Source`

### 装备面板最小配置

1. Canvas 下新建 `EquipmentPanel` GameObject，挂载 `EquipmentPanel.cs`
2. 子节点 `RodSlot`：挂 `EquipmentSlotUI`，`allowedSlot = FishingRod`
3. 子节点 `GearSlot`：挂 `EquipmentSlotUI`，`allowedSlot = FishingGear`
4. 场景中需有 `EquipmentManager` GameObject（`DontDestroyOnLoad`）
5. 场景中需有 `EffectBus` GameObject，或让 `EffectBus` 自动创建（访问 `Instance` 时自动生成）
6. `HandPanelUI` 的 `equipmentCardPrefab` 字段绑定装备卡预制体
