# 手牌系统

> [← 返回索引](INDEX.md)  
> 覆盖脚本：`HandSystem/HandManager.cs`

---

## HandManager

**路径**：`Assets/Script/HandSystem/HandManager.cs`  
**模式**：单例 `HandManager.Instance`，`DontDestroyOnLoad`  
**命名空间**：`HandSystem`

### 职责

管理玩家手牌的**数据层**（`List<ItemData>`），不负责 UI 显示。  
UI 由 `HandUIPanel` 订阅 `OnHandChanged` 事件后自行刷新。

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
