# 钓鱼系统

> [← 返回索引](INDEX.md)  
> 覆盖脚本：`FishingSystem/FishingTablePanel.cs` · `FishingSystem/CardPileSlot.cs` · `FishingSystem/RevealOverlayPanel.cs`

---

## FishingTablePanel（钓鱼牌桌）

**路径**：`Assets/Script/FishingSystem/FishingTablePanel.cs`  
**模式**：单例 `FishingTablePanel.Instance`（懒加载 FindObjectOfType）  
**命名空间**：`FishingSystem`

### 职责

管理 3×3 九槽位钓鱼桌的初始化和交互，依赖 `ItemPool` 抽取卡牌。

### Inspector 参数

| 参数 | 说明 |
|------|------|
| `pileGridContainer` | 3×3 网格容器 Transform |
| `cardPileSlotPrefab` | CardPileSlot 预制体 |
| `revealOverlay` | RevealOverlayPanel 引用 |
| `playerState` | 玩家 CharacterState 引用 |

### 槽位索引映射

| 槽位索引 | 深度 | poolIndex |
|---------|------|-----------|
| 0、1、2 | Depth1（浅层） | 0、1、2 |
| 3、4、5 | Depth2（中层） | 0、1、2 |
| 6、7、8 | Depth3（深层） | 0、1、2 |

---

## CardPileSlot（槽位控制器）

**路径**：`Assets/Script/FishingSystem/CardPileSlot.cs`  
**命名空间**：`FishingSystem`  
**接口**：`IPointerClickHandler`

### 状态枚举

```csharp
enum PileState { Empty, FaceDown, FaceUp }
```

### Inspector 参数

| 参数 | 说明 |
|------|------|
| `depth` | 槽位深度（FishDepth） |
| `poolIndex` | 子池索引（0-2） |
| `currentState` | 当前状态 |
| `currentCard` | 当前显示的 FishCard |
| `cardSpawnPoint` | 卡牌生成位置 |
| `fishCardPrefab` | FishCard 预制体 |

### API

```csharp
slot.Initialize(FishDepth depth, int poolIndex)  // 初始化槽位（由 FishingTablePanel 调用）
// event Action<CardPileSlot> OnSlotClicked      // 槽位被点击时触发
```

### 工作流

```
Initialize() → 状态 Empty
玩家点击 → OnPointerClick → 触发 OnSlotClicked
FishingTablePanel 接收 → ItemPool 抽牌 → 生成 FishCard
状态：Empty → FaceDown → FaceUp
```

---

## RevealOverlayPanel（翻牌遮罩）

**路径**：`Assets/Script/FishingSystem/RevealOverlayPanel.cs`  
卡牌翻开时显示的遮罩动画面板，由 `FishingTablePanel` 控制显隐。
