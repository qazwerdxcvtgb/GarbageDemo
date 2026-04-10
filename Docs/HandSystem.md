# 手牌系统

> [← 返回索引](INDEX.md)  
> 覆盖脚本：`HandSystem/HandManager.cs` · `FishCardSystem/Manager/UseCardHandler.cs`

---

## HandManager

**路径**：`Assets/Script/HandSystem/HandManager.cs`  
**模式**：单例 `HandManager.Instance`，`DontDestroyOnLoad`  
**命名空间**：`HandSystem`

### 职责

管理玩家手牌的**数据层**（`List<ItemData>`），不负责 UI 显示。  
UI 由 `HandPanelUI`（FishCardSystem）订阅 `OnHandChanged` 事件后自行刷新。

### API

```csharp
HandManager.Instance.AddCard(ItemData card)          // 添加卡牌到手牌
HandManager.Instance.RemoveCard(ItemData card)        // 移除卡牌（返回 bool：是否成功）
HandManager.Instance.GetHandCards()                   // 返回手牌列表副本（List<ItemData>）
HandManager.Instance.GetHandCount()                   // 手牌数量
HandManager.Instance.ContainsCard(ItemData card)      // 是否包含指定卡牌
HandManager.Instance.ClearHand()                      // 清空所有手牌
```

### 事件

```csharp
HandManager.Instance.OnHandChanged  // Action，手牌变化时触发
```

### 典型用法

```csharp
// 钓鱼系统捕获后添加
HandManager.Instance.AddCard(fishData);

// UI 层订阅刷新
HandManager.Instance.OnHandChanged += RefreshDisplay;

// 鱼店售出后移除
HandManager.Instance.RemoveCard(selectedFish);
```

> **注意**：`GetHandCards()` 返回副本，直接修改不影响原始数据。

---

## UseCardHandler（手牌使用按钮）

**路径**：`Assets/Script/FishCardSystem/Manager/UseCardHandler.cs`  
**命名空间**：`FishCardSystem`

### 职责

管理手牌面板的"使用"按钮，实现卡牌单选、可用性检查、效果触发与移除。

### 行为

- **单选强制**：同一时刻只允许一张卡牌处于选中状态。选中新卡时自动取消上一张的选中。
- **装备卡排除**：`EquipmentCard` 被选中时不显示使用按钮（装备仅通过拖拽操作）。
- **可用性检查**：非装备卡选中时调用 `ItemData.CanUse(out reason)` 判断是否可用，据此控制按钮 `interactable`。
- **使用执行**：点击按钮 → `TriggerUseEffects()` → `HandManager.RemoveCard()` → `FishCardHolder.RemoveCardAndCollapse()` → 销毁卡牌 GameObject。

### Inspector 字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `cardHolder` | FishCardHolder | 手牌容器引用 |
| `useButton` | Button | "使用"按钮 |
| `reasonText` | TextMeshProUGUI | 不可用原因文本（可选） |

### 使用流程

```
选中卡牌 → CanUse? → 是 → 按钮可点击 → 点击 → 触发效果 → 移除卡牌
                   → 否 → 按钮灰置（interactable = false）
```

---

## 商店场景下的手牌行为

商店打开时，`ShopPanel` 会锁定 `HandPanelUI` 的展开状态：

- 调用 `HandPanelUI.LockExpanded()`：强制展开手牌，隐藏折叠按钮
- 关闭商店时调用 `HandPanelUI.UnlockExpanded()`：恢复折叠权限和按钮

偷看牌堆时，`PeekPileHandler` 会锁定 `HandPanelUI` 的收起状态：

- 调用 `HandPanelUI.LockCollapsed()`：强制收起手牌，隐藏折叠按钮，禁止 Show/Toggle
- 偷看结束时调用 `HandPanelUI.UnlockCollapsed()`：恢复折叠权限和按钮

售卖手牌流程（`ShopSellController`）：

```
FishCardHolder.RemoveCard(card)       → 解除 Holder 绑定
Destroy(card.gameObject)              → 销毁逻辑卡（触发 FishCardVisual 销毁）
HandManager.RemoveCard(cardData)      → 数据层移除 → OnHandChanged → SetSlotCount 收缩槽位
CharacterState.ModifyGold(total)      → 结算金币
```

悬挂鱼牌流程（`ShopHangController`）：

```
ShopManager.TryHangFish(index, data) → 持久化存储
HandManager.RemoveCard(data)          → 数据层移除 → OnHandChanged → 槽位收缩
FishCardHolder.RemoveCard(card)       → 解除 Holder 绑定
ShopHangSlot.HangCard(card)          → 卡牌归位到悬挂槽，isLocked = true
```

---

## CardSelectionPanel（卡牌选择面板）

**路径**：`Assets/Script/FishCardSystem/Manager/CardSelectionPanel.cs`  
**命名空间**：`FishCardSystem`  
**面板层级**：`sortingOrder = 175`（高于 EquipmentPanel(170)，低于 HandPanel(180)）

### 职责

可配置的卡牌选择面板预制体。源无关设计：接收卡牌列表并通过回调返回选择结果，牌库操作由调用方负责。

### API

```csharp
public delegate void SelectionCallback(List<ItemData> selected, List<ItemData> rejected);

// 打开选择面板
// offeredCards: 已由调用方从牌库中抽出的卡牌列表
// maxSelectCount: 最大可选数量；0 表示无需选择（按钮显示"取消"）
// onComplete: 回调，返回选中和未选中的卡牌列表
panel.Open(List<ItemData> offeredCards, int maxSelectCount, SelectionCallback onComplete);

panel.IsOpen  // bool，面板是否处于打开状态
```

### 选择行为

- `maxSelectCount == 0`：按钮显示"取消"，始终可点击；点击后回调 `(空列表, 全部卡牌)`
- `maxSelectCount > 0`：按钮显示"确认"；已选数量 < max 时按钮置灰
- 超选处理：FIFO 取消最早选中的卡牌（`LinkedList` 维护顺序）
- 确认后销毁所有卡牌实例，隐藏面板，调用回调

### Inspector 字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `sortingOrder` | int | Canvas 排序层级（默认 175） |
| `panelRoot` | GameObject | 面板根节点（显示/隐藏） |
| `slotContainer` | Transform | SelectionSlot 父容器 |
| `selectionSlotPrefab` | GameObject | SelectionSlot 预制体 |
| `confirmButton` | Button | 确认/取消按钮 |
| `buttonText` | TextMeshProUGUI | 按钮文本 |
| `fishCardPrefab` | GameObject | 鱼类卡牌预制体 |
| `trashCardPrefab` | GameObject | 杂鱼卡牌预制体 |
| `consumableCardPrefab` | GameObject | 消耗品卡牌预制体 |
| `equipmentCardPrefab` | GameObject | 装备卡牌预制体 |

### SelectionSlot（辅助组件）

**路径**：`Assets/Script/FishCardSystem/Manager/SelectionSlot.cs`

包裹单张卡牌的容器组件，管理选中高亮和点击交互。

```csharp
slot.Setup(ItemCard card, ItemData data)  // 绑定卡牌
slot.SetSelected(bool selected)           // 控制选中高亮框
slot.Cleanup()                            // 销毁持有的卡牌
slot.IsSelected                           // 当前选中状态
slot.CardData                             // 当前卡牌数据
```

### 调用方示例

```csharp
// 从 CardPile 抽3张选1张
List<FishData> drawn = cardPile.DrawTopCards(3);
panel.Open(drawn.Cast<ItemData>().ToList(), 1, (selected, rejected) => {
    foreach (var card in selected)
        HandManager.Instance.AddCard(card);
    cardPile.InsertCardsAtTop(rejected.Cast<FishData>().ToList());
});

// 按深度横抽（多源追踪）
var drawn = FishingTableManager.Instance.DrawOneFromEachAtDepth(FishDepth.Depth1);
panel.Open(drawn.Select(x => (ItemData)x.card).ToList(), 1, (selected, rejected) => {
    foreach (var card in selected)
        HandManager.Instance.AddCard(card);
    foreach (var rej in rejected)
    {
        var source = drawn.First(x => x.card == rej).source;
        source.InsertCardsAtTop(new List<FishData> { rej as FishData });
    }
});
```
