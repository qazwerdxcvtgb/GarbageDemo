# 钓鱼系统

> [← 返回索引](INDEX.md)  
> 覆盖脚本：`FishingSystem/CardPile.cs` · `FishingSystem/CardPilePanel.cs` · `FishingSystem/PileThicknessDisplay.cs` · `FishingSystem/CardPileTest.cs` · `FishingSystem/FishingTablePanel.cs` · `FishingSystem/CardPileSlot.cs` · `FishingSystem/RevealOverlayPanel.cs`

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

---

## CardPile（独立牌堆预制体）

**路径**：`Assets/Script/FishingSystem/CardPile.cs`  
**命名空间**：`FishingSystem`  
**接口**：`IPointerClickHandler`

### 职责

自持卡序的独立牌堆单元，管理三种显示状态，交互通过事件上报，不含游戏逻辑。

### 预制体层级结构

```
CardPile（CardPile.cs + Image 透明遮罩，RaycastTarget=true）
├── ThicknessContainer（PileThicknessDisplay.cs）
│   ├── EdgeLayer_1（Image，最近底部）
│   ├── EdgeLayer_2
│   └── EdgeLayer_N
├── CardBackContainer（FaceDown 时激活）
│   ├── SmallCardBack（Image）
│   ├── MediumCardBack（Image）
│   └── LargeCardBack（Image）
└── CardFaceContainer（FaceUp 时激活，FishCard 实例化到此）
```

### Inspector 参数

| 参数 | 说明 |
|------|------|
| `fishCardPrefab` | FishCard 预制体（FaceUp 时实例化） |
| `cardBackContainer` | 卡背总容器 GameObject |
| `smallCardBack` | Small 尺寸卡背 |
| `mediumCardBack` | Medium 尺寸卡背 |
| `largeCardBack` | Large 尺寸卡背 |
| `cardFaceContainer` | 卡面生成的父 Transform |
| `thicknessDisplay` | PileThicknessDisplay 组件引用 |
| `emptyView` | 空牌堆视觉容器（Empty 状态时显示） |
| `cardPilePanelPrefab` | 点击牌堆时实例化的 CardPilePanel 预制体 |

### 状态枚举（复用 PileState）

```csharp
enum PileState { Empty, FaceDown, FaceUp }
```

### API

```csharp
pile.SetCards(List<FishData> list)   // 注入卡序，自动进入 FaceDown；若列表为空则进入 Empty
pile.Reveal()                        // FaceDown → FaceUp
pile.RemoveTopCard()                 // 移除顶牌并刷新，返回 FishData；耗尽则进入 Empty
pile.GetTopCard()                    // 只读取顶牌
pile.CardCount                       // 当前张数
pile.State                           // 当前 PileState
// event Action<CardPile> OnPileClicked  // 点击时触发
// protected virtual void OnPileBecameEmpty()  // 牌堆变空时触发（可在子类中重写）
```

### 状态流转

```
SetCards(list) → FaceDown（显示对应尺寸卡背）
             └→ Empty（list 为空，显示 emptyView，触发 OnPileBecameEmpty）
Reveal()       → FaceUp（显示 FishCardVisual 卡面）
RemoveTopCard()→ FaceDown（显示新顶牌卡背）
             └→ Empty（牌堆耗尽，显示 emptyView，触发 OnPileBecameEmpty）
```

---

## CardPilePanel（牌堆交互面板）

**路径**：`Assets/Script/FishingSystem/CardPilePanel.cs`  
**命名空间**：`FishingSystem`

### 职责

单击 `CardPile` 时由其实例化并显示，根据顶牌揭示状态切换 FaceDown / FaceUp 视图，提供揭示、捕获、取消三种操作。

### Inspector 参数

| 参数 | 说明 |
|------|------|
| `cardBackView` | FaceDown 视图根 GameObject |
| `cardBackImage` | 卡背图 Image 组件 |
| `smallBackSprite` | Small 尺寸卡背 Sprite |
| `mediumBackSprite` | Medium 尺寸卡背 Sprite |
| `largeBackSprite` | Large 尺寸卡背 Sprite |
| `cardFaceView` | FaceUp 视图根 GameObject |
| `cardHolder` | 装载 FishCard 的 FishCardHolder（需配 slotPrefab、cardsToSpawn=1） |
| `fishCardPrefab` | FishCard 预制体，FaceUp 时实例化 |
| `cancelButton` | 取消按钮（始终可见） |
| `revealButton` | 揭示按钮（仅 FaceDown 时可见） |
| `captureButton` | 捕获按钮（仅 FaceUp 时可见） |
| `displayScale` | 放大倍率，默认 1.5（Range 0.5–3） |

### API

```csharp
panel.Show(CardPile pile)   // 打开面板，由 CardPile.OnPointerClick 调用
```

### 状态流转

```
Show(pile)
  ├─ pile.State == FaceDown → ShowFaceDown() → 展示卡背 + 揭示按钮
  │     揭示按钮点击 → TriggerRevealEffects() + pile.Reveal() → ShowFaceUp()
  └─ pile.State == FaceUp  → ShowFaceUp()  → 展示 FishCard + 捕获按钮
        捕获按钮点击 → TriggerCaptureEffects() + ClosePanel()
        取消按钮点击 → ClosePanel() → Destroy(gameObject)
```

### 预制体搭建说明

**建议路径**：`Assets/Prefab/FishCardSystem/CardPilePanel.prefab`

```
CardPilePanel               (RectTransform, Canvas[overrideSorting], CardPilePanel)
├── Mask                    (Image 半透明遮罩，Button → 点击遮罩等同取消)
└── PanelRoot               (RectTransform 居中内容区)
    ├── CardDisplayArea     (RectTransform)
    │   ├── CardBackView    (RectTransform - FaceDown 视图根)
    │   │   └── CardBackImage (Image - 卡背图)
    │   └── CardFaceView    (RectTransform - FaceUp 视图根)
    │       └── FishCardHolder (FishCardHolder，slotPrefab=FishCardSlot，cardsToSpawn=1)
    └── ButtonArea          (HorizontalLayoutGroup)
        ├── CancelButton    (Button)
        ├── RevealButton    (Button - FaceDown 时激活)
        └── CaptureButton   (Button - FaceUp 时激活)
```

> **注意**：FishCardHolder 的 `slotPrefab` 必须指向 `FishCardSlot` 预制体，`cardsToSpawn` 设为 1，使 Start() 时自动生成一个空槽供 AddCard() 使用。  
> 预制体完成后将其拖至每个 `CardPile` 组件的 `cardPilePanelPrefab` 字段。

---

## PileThicknessDisplay（牌堆厚度视觉）

**路径**：`Assets/Script/FishingSystem/PileThicknessDisplay.cs`  
**命名空间**：`FishingSystem`

边缘层在 `Awake` 时**自动生成**，预制体的 `ThicknessContainer` 下无需手动创建子节点。

### Inspector 参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `maxLayers` | 8 | 最多生成的边缘层数（厚度上限） |
| `layerSprite` | 空 | 边缘层 Sprite，留空则纯色 |
| `layerColor` | 浅灰 | 边缘层颜色 |
| `layerSize` | (220, 6) | 每层宽高，宽度建议与卡牌一致 |
| `layerOffset` | (1, -4) | 每层相对上一层的偏移，向右下堆叠产生立体感 |
| `cardsPerLayer` | 2 | 每 N 张卡显示一层，调整厚度增长速率 |

### API

```csharp
display.UpdateThickness(int cardCount)  // 由 CardPile 自动调用
```

---

## CardPileTest（牌堆测试初始化）

**路径**：`Assets/Script/FishingSystem/CardPileTest.cs`  
**命名空间**：`FishingSystem`

测试脚本，从 `ItemPool` 取指定深度和子池的卡序注入 `CardPile`。

### Inspector 参数

| 参数 | 说明 |
|------|------|
| `targetPile` | 目标 CardPile |
| `depth` | FishDepth（Depth1/2/3） |
| `poolIndex` | 子池索引（0-2） |
| `cardCountLimit` | 取卡上限，0 表示不限 |
| `autoInitOnStart` | 是否 Start 时自动初始化 |

### 调试方法

```csharp
test.InitializePile()   // 重新初始化
test.DebugReveal()      // 翻开顶牌（等同 pile.Reveal()）
test.DebugRemoveTop()   // 移除顶牌（等同 pile.RemoveTopCard()）
```

---

## RevealOverlayPanel（翻牌遮罩）

**路径**：`Assets/Script/FishingSystem/RevealOverlayPanel.cs`  
卡牌翻开时显示的遮罩动画面板，由 `FishingTablePanel` 控制显隐。
