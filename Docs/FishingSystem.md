# 钓鱼系统

> [← 返回索引](INDEX.md)  
> 覆盖脚本：`FishingSystem/FishingTableManager.cs` · `FishingSystem/CardPile.cs` · `FishingSystem/CardPilePanel.cs` · `FishingSystem/PileThicknessDisplay.cs` · `FishingSystem/PeekPileHandler.cs`

---

## 架构概览

```
ItemPool（数据层）
  │  Start 时一次性读取初始卡序
  ▼
FishingTableManager（逻辑层，单例）
  │  SetCards() 注入各 CardPile，持有 CharacterState 引用
  ├─► CardPile × 9（视图层，自持卡序）
  │     │  OnPointerClick → 实例化 CardPilePanel
  │     ▼
  │   CardPilePanel（交互层，按需实例化）
  │     │  TryReveal / TryCapture
  └─────┘  回调 FishingTableManager
```

### 数据分层原则

| 层级 | 脚本 | 职责 |
|------|------|------|
| 数据层 | `ItemPool` | 从 Resources 加载所有 FishData，完成分池打乱，提供初始卡序；保管非鱼类 Deck（Trash/Consumable/Equipment）的随机抽取 |
| 钓鱼逻辑 | `FishingTableManager` | 翻牌/捕获/放弃 逻辑入口；`TryAbandon` 从杂鱼牌库抽取一张加入手牌 |
| 逻辑层 | `FishingTableManager` | 初始化 9 个 CardPile；体力检查与扣除；捕获流程（加入手牌）；多牌堆操作入口 |
| 视图层 | `CardPile` | 自持 `List<FishData>`，管理 FaceDown / FaceUp / Empty 三态显示；与 ItemPool 解耦 |
| 交互层 | `CardPilePanel` | 点击牌堆时实例化，处理翻牌/捕获/取消 UI；调用 `FishingTableManager` 执行游戏逻辑 |

---

## FishingTableManager（钓鱼牌桌管理器）

**路径**：`Assets/Script/FishingSystem/FishingTableManager.cs`  
**模式**：单例 `FishingTableManager.Instance`  
**命名空间**：`FishingSystem`

### 职责

- 游戏启动时从 `ItemPool.GetFragmentedPool()` 读取初始卡序，深拷贝后注入各 `CardPile.SetCards()`
- 作为翻牌（体力-1）、捕获（体力-staminaCost + 加入手牌）的唯一游戏逻辑入口
- 提供多牌堆批量操作方法

### Inspector 参数

| 参数 | 说明 |
|------|------|
| `pileConfigs` | 长度 9 的 PileConfig 数组，每项绑定一个 CardPile 实例及其 depth、poolIndex |
| `playerState` | 玩家 `CharacterState` 引用 |
| `showDebugInfo` | 是否输出调试日志 |

### PileConfig 结构体

```csharp
[Serializable]
public struct PileConfig
{
    public CardPile pile;       // 对应场景中的 CardPile 实例
    public FishDepth depth;     // 使用的鱼类深度
    [Range(0,2)] public int poolIndex; // ItemPool 子池索引（0-2）
}
```

### 槽位索引建议

| 索引 | depth | poolIndex |
|------|-------|-----------|
| 0、1、2 | Depth1（浅层） | 0、1、2 |
| 3、4、5 | Depth2（中层） | 0、1、2 |
| 6、7、8 | Depth3（深层） | 0、1、2 |

### API

```csharp
// 游戏逻辑（供 CardPilePanel 调用）
bool TryReveal(CardPile pile, FishData data)   // 检查体力≥1 → 扣体力 → TriggerRevealEffects()
bool TryCapture(CardPile pile, FishData data)  // 检查体力≥staminaCost → 扣体力 → TriggerCaptureEffects()
                                               //   → HandManager.AddCard() → pile.RemoveTopCard()
bool CanPlayerAccessPile(CardPile pile)        // 检查玩家深度是否允许与该牌堆交互

// 多牌堆操作
void RevealAllPiles()       // 翻开所有 FaceDown 牌堆（不消耗体力）
void ResetAllPiles()        // 重新从 ItemPool 读取并重置所有牌堆卡序（ContextMenu 可调试）
CardPile GetPile(int index) // 按索引获取 CardPile
List<CardPile> GetAllPiles()// 获取所有非空 CardPile 列表
```

---

## PileState 枚举

定义位置已迁移至 `CardPile.cs`（`FishingSystem` 命名空间内）。

```csharp
public enum PileState { Empty, FaceDown, FaceUp }
```

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
│   └── （边缘层由脚本自动生成，无需手动建）
├── CardBackContainer（FaceDown 时激活）
│   ├── SmallCardBack（Image）
│   ├── MediumCardBack（Image）
│   └── LargeCardBack（Image）
├── CardFaceContainer（FaceUp 时激活，FishCard 实例化到此）
└── EmptyView（Empty 时激活，显示空牌堆图案）
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
| `pileDepth` | 该牌堆深度等级（由 FishingTableManager 初始化时写入，也可在 Inspector 手动配置） |

### API

```csharp
pile.SetCards(List<FishData> list)   // 注入卡序，自动进入 FaceDown；空列表则进入 Empty
pile.Reveal()                        // FaceDown → FaceUp
pile.RemoveTopCard()                 // 移除顶牌并刷新，返回 FishData；耗尽则进入 Empty
pile.GetTopCard()                    // 只读取顶牌（不移除）
pile.CardCount                       // 当前张数
pile.State                           // 当前 PileState
// event Action<CardPile> OnPileClicked          // 点击时触发（上层可订阅）
// protected virtual void OnPileBecameEmpty()    // 牌堆变空时触发（可在子类中重写）
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

单击 `CardPile` 时由其实例化并显示，根据顶牌揭示状态切换 FaceDown / FaceUp 视图，提供揭示、捕获、取消三种操作。游戏逻辑（体力扣除、手牌添加）委托给 `FishingTableManager`。

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
  │     揭示按钮点击 → FishingTableManager.TryReveal() → pile.Reveal() → ShowFaceUp()
  └─ pile.State == FaceUp  → ShowFaceUp()  → 展示 FishCard + 捕获按钮
        捕获按钮点击 → FishingTableManager.TryCapture() → ClosePanel()
        取消按钮点击 → ClosePanel() → Destroy(gameObject)
```

### 预制体搭建说明

**建议路径**：`Assets/Prefab/FishCardSystem/CardPilePanel.prefab`

```
CardPilePanel               (RectTransform, Canvas[overrideSorting=true, order=160], CardPilePanel)
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

> **注意**：FishCardHolder 的 `slotPrefab` 必须指向 `FishCardSlot` 预制体，`cardsToSpawn` 设为 1。  
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

## PeekPileHandler（偷看牌堆流程管理器）

**路径**：`Assets/Script/FishingSystem/PeekPileHandler.cs`  
**模式**：场景级单例 `PeekPileHandler.Instance`  
**命名空间**：`FishingSystem`

### 职责

管理偷看效果的完整 UI 流程：锁定面板 → 展示偷看结果 → 恢复。

### PeekMode 枚举

| 值 | 说明 |
|----|------|
| `Single` | 玩家选择一个牌堆，偷看顶部 N 张未揭示的牌 |
| `Row` | 玩家选择一个牌堆，偷看同行（同深度）3 个牌堆各 1 张未揭示顶牌 |
| `Column` | 玩家选择一个牌堆，偷看同列（同序号）3 个牌堆各 1 张未揭示顶牌 |
| `All` | 直接在所有 9 个牌堆上方叠加浮层，展示各堆第一张未揭示的牌 |

### Inspector 参数

| 参数 | 说明 |
|------|------|
| `handPanelUI` | HandPanelUI 引用（偷看时收起手牌栏） |
| `selectionPanelPrefab` | CardSelectionPanel 预制体（Single/Row/Column 模式用） |
| `promptRoot` | 提示 UI 根节点（Single/Row/Column 模式显示选择提示） |
| `promptText` | 提示文本 TMP 组件 |
| `fishCardPrefab` | FishCard 预制体（All 模式用于创建浮层卡牌） |
| `exitPeekButton` | 退出偷看按钮（All 模式专用，默认隐藏） |
| `overlaySortingOrder` | 浮层 Canvas 排序层级（默认 165，高于牌堆低于手牌栏） |

### API

```csharp
handler.StartPeek(int count, PeekMode mode)  // 开始偷看流程
handler.IsPeeking                             // 是否正在偷看中（只读）
```

### All 模式流程

```
Effect_PeekAllPiles.Execute()
  → StartPeek(1, PeekMode.All)
    → 锁定手牌栏/装备栏
    → ClickInterceptor = 空操作（拦截牌堆点击，阻止 CardPilePanel 弹出）
    → 遍历 9 堆，各调用 PeekTopCards(1, skipRevealed: true)
    → 有未揭示牌的堆：实例化 FishCard 浮层叠加到牌堆上方
    → 显示退出按钮

退出按钮点击
  → 销毁所有浮层 → 清除 ClickInterceptor → 解锁手牌栏/装备栏 → 隐藏退出按钮
```

### 预制体搭建说明（退出按钮）

在 `PeekPileHandler` 所在 Canvas 下添加退出按钮节点，将 Button 组件拖至 `exitPeekButton` 字段。按钮事件由代码自动绑定，无需 Inspector 手动配置 onClick。  
退出按钮在 Awake 时自动隐藏，仅 All 模式偷看期间激活。
