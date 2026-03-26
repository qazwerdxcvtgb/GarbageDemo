# UI 系统

> [← 返回索引](INDEX.md)  
> 覆盖脚本：`UI/` 目录下所有脚本  
> **命名空间**：`UISystem`

---

## HandPanelUI（手牌面板 UI 管理器）

**路径**：`Assets/Script/FishCardSystem/Manager/HandPanelUI.cs`  
**命名空间**：`FishCardSystem`

### 职责
- 设置 HandPanel 的 Canvas 图层（`overrideSorting = true`，默认 `sortingOrder = 100`）
- 提供手牌区域（`FishCardHolder`）折叠/展开按钮及 DOTween 动画
- 订阅 `HandManager.OnHandChanged`，自动调用 `FishCardHolder.SetSlotCount()` 同步槽位数
- 按钮文本实时显示当前手牌数量和展开状态

### Inspector 参数

| 参数 | 说明 |
|------|------|
| `panelCanvas` | HandPanel 根节点上的 Canvas |
| `sortingOrder` | 图层排序值（默认 100，高于 CardPilePanel 的 50，低于拖拽手牌的 200） |
| `cardHolder` | 子节点 FishCardHolder 组件 |
| `holderRect` | FishCardHolder 的 RectTransform |
| `hiddenOffsetY` | 收起时的 Y 轴偏移量（负值=向下隐藏，按实际布局调整） |
| `animDuration` | 动画时长（秒） |
| `startExpanded` | 初始是否展开 |
| `toggleButton` | 折叠/展开按钮 |
| `toggleButtonText` | 按钮文本（TMP），显示「手牌 N ▼/▲」 |
| `extraButtons` | 预留按钮数组（位置固定，默认隐藏） |

### API

```csharp
panel.Show()    // 展开手牌区域
panel.Hide()    // 收起手牌区域
panel.Toggle()  // 切换展开/收起（绑定到 toggleButton）
```

### 图层关系

```
拖拽手牌 FishCardVisual（sortingOrder 200）  ← BeginDrag 时临时置顶
HandPanel Canvas（sortingOrder 100）          ← HandPanelUI 设置
CardPilePanel Canvas（sortingOrder 50）       ← CardPilePanel 已有设置
CardPile（无 overrideSorting）               ← 最低
```

---

## HandUIPanel（手牌 UI 面板）

**路径**：`Assets/Script/UI/HandUIPanel.cs`  
**模式**：单例 `HandUIPanel.Instance`（懒加载，含未激活对象搜索）

### 职责
- 显示 `HandManager` 中的手牌列表
- 处理卡牌选中和使用（食用）逻辑
- 订阅 `HandManager.OnHandChanged` 自动刷新

### 关键调用

```csharp
HandUIPanel.Instance.ShowPanel()   // 显示面板
HandUIPanel.Instance.HidePanel()   // 隐藏面板
// 面板自动订阅 HandManager.OnHandChanged 刷新
```

**关联组件**：`HandCardButton`（手牌按钮）、`DraggableHandCard`（可拖拽手牌）

---

## FishShopPanel（鱼店面板）

**路径**：`Assets/Script/UI/FishShopPanel.cs`  
**模式**：单例 `FishShopPanel.Instance`（懒加载）

### 职责
- 展示玩家手牌中的可售鱼类
- 调用 `FishPriceCalculator` 计算售价
- 出售后：`HandManager.RemoveCard()` + `CharacterState.ModifyGold()`

### 关键调用

```csharp
FishShopPanel.Instance.ShowPanel()   // 显示鱼店面板
FishShopPanel.Instance.HidePanel()   // 隐藏鱼店面板
```

**关联组件**：`FishShopCardSlot`（可售卡槽）、`DraggableFishShopCard`（拖拽售卡）

---

## 状态显示 UI

三个独立组件，挂载在 Canvas 对应 GameObject 上，通过事件订阅自动更新，**无需手动调用**。

| 脚本 | 订阅事件 | 组件要求 |
|------|---------|---------|
| `HealthDisplayUI` | `CharacterState.OnHealthChanged` / `OnMaxHealthChanged` | `Slider` + `TMP_Text` |
| `GoldDisplayUI` | `CharacterState.OnGoldChanged` | `TMP_Text` |
| `SanityDisplayUI` | `GameManager.OnSanityChanged` / `OnSanityLevelChanged` | `TMP_Text` |

---

## CardSelectionPanel（卡片选择面板）

**路径**：`Assets/Script/UI/CardSelectionPanel.cs`  
**触发**：玩家靠近 `InteractionPoint_1` 并按交互键后弹出，展示从牌池抽取的卡牌供选择。

---

## 其他 UI 组件

| 脚本 | 功能 |
|------|------|
| `CardButton` | 通用卡牌按钮基类 |
| `HandCardButton` | 手牌列表按钮（继承 CardButton） |
| `DraggableHandCard` | 可拖拽手牌控件 |
| `FishShopCardSlot` | 鱼店卡片槽位 |
| `DraggableFishShopCard` | 鱼店可拖拽卡片 |
