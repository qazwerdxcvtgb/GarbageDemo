# 鱼类卡牌系统

> [← 返回索引](INDEX.md)  
> 覆盖脚本：`Script/FishCardSystem/` 下所有脚本

---

## 架构概览

```
FishCard（逻辑卡）                负责输入、状态、位置计算
  └── 实例化 FishCardVisual（视觉卡）  负责所有动画和视觉效果
        └── FishCardFrontDisplay    卡牌 UI 数据绑定

FishCardHolder（卡牌容器）        管理多个 FishCardSlot，处理拖拽排序
VisualCardsHandler                视觉卡全局管理（场景中须存在）
CurveParameters（ScriptableObject）定义手牌弧线曲线
```

---

## 脚本清单

| 脚本 | 路径 | 职责 |
|------|------|------|
| `ItemCard` | `Core/ItemCard.cs` | 逻辑卡基类：输入、状态、位置；含 `isLocked` 锁定字段 |
| `FishCard` | `Core/FishCard.cs` | 逻辑卡：鱼类数据绑定、视觉卡实例化 |
| `TrashCard` | `Core/TrashCard.cs` | 逻辑卡：杂鱼数据绑定 |
| `ConsumableCard` | `Core/ConsumableCard.cs` | 逻辑卡：消耗品数据绑定 |
| `EquipmentCard` | `Core/EquipmentCard.cs` | 逻辑卡：装备数据绑定 |
| `FishCardVisual` | `Visual/FishCardVisual.cs` | 视觉卡：动画、跟随、弧线 |
| `FishCardFrontDisplay` | `Visual/FishCardFrontDisplay.cs` | 卡牌 UI 数据绑定（鱼类） |
| `TrashCardFrontDisplay` | `Visual/TrashCardFrontDisplay.cs` | 卡牌 UI 数据绑定（杂鱼） |
| `ConsumableCardFrontDisplay` | `Visual/ConsumableCardFrontDisplay.cs` | 卡牌 UI 数据绑定（消耗品） |
| `EquipmentCardFrontDisplay` | `Visual/EquipmentCardFrontDisplay.cs` | 卡牌 UI 数据绑定（装备） |
| `CardContextMode` | `Core/CardContextMode.cs` | 枚举：卡牌上下文模式（Hand/Pile/Shop 等） |
| `FishCardHolder` | `Manager/FishCardHolder.cs` | 容器：槽位管理、拖拽排序、悬停压缩；含 `CrossHolderRole` 字段，按角色自动注册到 CrossHolderSystem |
| `CrossHolderSystem` | `Manager/CrossHolderSystem.cs` | 跨 Holder 拖拽全局单例：Source/Target 自动注册、阈值检测、主动归位 |
| `HandPanelUI` | `Manager/HandPanelUI.cs` | 手牌面板 UI 管理：图层、折叠/展开、商店锁定支持 |
| `VisualCardsHandler` | `Manager/VisualCardsHandler.cs` | 视觉卡全局注册管理 |
| `CurveParameters` | `Data/CurveParameters.cs` | 弧线 ScriptableObject |

---

## 核心 API

### FishCard

```csharp
fishCard.Initialize(FishData data)     // 初始化卡牌数据并刷新显示
fishCard.NormalizedPosition()          // 归一化位置（0=最左, 1=最右）
fishCard.ParentIndex()                 // 在父节点中的索引
```

> **生命周期说明**：`Awake()` 负责组件获取和全部 UnityEvent 初始化；`Start()` 负责实例化视觉卡。  
> 外部代码（如 `FishCardHolder.AddCard`）在 `Instantiate` 之后即可安全调用 `AddListener`，无需等待 `Start()`。

**Inspector 关键参数**：

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `cardData` | — | FishData 资源 |
| `instantiateVisual` | true | 自动实例化视觉卡 |
| `cardVisualPrefab` | — | FishCardVisual 预制体 |
| `moveSpeedLimit` | 50 | 拖拽最大速度（世界单位/秒，正交相机下适用） |
| `selectionOffset` | 50 | 选中时上移（画布本地单位，约等于屏幕像素） |

**FishCard 事件**：

| 事件 | 触发时机 |
|------|---------|
| `PointerEnterEvent(card)` | 鼠标进入 |
| `PointerExitEvent(card)` | 鼠标离开 |
| `PointerUpEvent(card, longPress)` | 鼠标抬起 |
| `BeginDragEvent(card)` | 开始拖拽 |
| `EndDragEvent(card)` | 结束拖拽 |
| `SelectEvent(card, selected)` | 选中状态变化 |

### FishCardHolder

```csharp
holder.SetSlotCount(int count)  // 动态调整槽位数量（增/减），由 HandPanelUI 自动调用
holder.AddCard(FishCard card, int slotIndex = -1)   // 添加卡牌到容器
holder.RemoveCard(FishCard card)                     // 从容器移除卡牌
holder.GetCards()                                    // 返回当前卡牌列表副本
```

**Inspector 关键参数**：

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `compressionRate` | 0.5 | 悬停压缩强度（0=关闭，1=最大压缩） |
| `cardWidth` | 220 | 卡牌宽度（画布本地单位），用于计算目标间距 |
| `compressionTransition` | 0.2 | 压缩动画时长（秒） |

**悬停压缩效果说明**：

鼠标悬停在某张卡牌时，两侧卡牌向各自边缘方向收紧间距，为悬停卡牌创造展示空间。效果通过移动逻辑卡（`FishCard`）的 `localPosition.x` 实现，视觉卡经由 `SmoothFollow` 自然跟随，交互热区与视觉位置保持同步。

- 最边侧卡牌（索引 0 和 n-1）偏移量自然为 0（锚定），无需特殊处理
- 悬停在最右侧卡牌时不触发压缩（该卡已完全可见）
- 压缩公式：`compressionPerGap = compressionRate × max(0, normalSpacing − (holderWidth − cardWidth) / (n−1))`
  - 左侧组 + 悬停卡（i ≤ h）：`offsetX = −i × compressionPerGap`（悬停卡随左组同步向左，右侧露出完整牌面）
  - 右侧组（i > h）：`offsetX = (n−1−i) × compressionPerGap`
- 拖拽开始时自动重置所有压缩偏移，不影响拖拽排序逻辑

---

### FishCardVisual

```csharp
visual.UpdateCardData(FishData data)  // 更新数据并刷新显示
visual.Swap(float direction)          // 交换动画（正值=顺时针 / 负值=逆时针）
```

**Inspector 关键参数**：

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `followSpeed` | 30 | 跟随逻辑卡速度（10-50） |
| `rotationAmount` | 10 | 手牌扇形最大旋转角 |
| `manualTiltAmount` | 15 | 交互时倾斜角度 |
| `scaleOnHover` | 1.15 | 悬停缩放倍率 |
| `scaleOnSelect` | 1.25 | 选中缩放倍率 |
| `curve` | — | 拖入 CurveParameters 资源 |

---

## Unity 配置摘要

### 必要 Tag
- `Edit → Project Settings → Tags and Layers` 添加 Tag：`Slot`
- 场景中的相机必须设置 Tag 为 `MainCamera`（`Camera.main` 依赖此标签）

### 预制体清单

| 预制体 | 关键要求 | 保存路径 |
|--------|---------|---------|
| `FishCardSlot` | RectTransform，Tag = `Slot` | `Prefab/FishCardSystem/` |
| `FishCard` | Image（RaycastTarget=✓），FishCard 组件 | `Prefab/FishCardSystem/` |
| `FishCardVisual` | FishCardVisual 组件，**根节点不加 Canvas** | `Prefab/FishCardSystem/` |

### FishCardVisual 层级

```
FishCardVisual（110×165，无 Canvas）
└── ShakeParent（Stretch All）
    ├── Shadow（Image，Canvas SortOrder=-1）
    └── TiltParent（Stretch All）
        └── CardFace + FishCardFrontDisplay
```

### CurveParameters 建议关键帧

- **positioning**：(0,0) → (0.5,1) → (1,0)，`positioningInfluence` = 0.02~0.1
- **rotation**：(0,1) → (0.5,0) → (1,-1)，`rotationInfluence` = 1~10

### 场景最小配置

1. **Camera**（Tag = `MainCamera`，**Orthographic**，Size = 5，Position = (0, 0, -5)）
   - 可携带 CinemachineBrain，保留 Cinemachine 能力
2. **Canvas**（**World Space**，1920×1080 参考分辨率）
   - Render Mode：`World Space`
   - Event Camera：绑定场景中的 Main Camera
   - Transform Position：`(0, 0, -4)`
   - Transform Scale：`(0.009259, 0.009259, 0.009259)`（使 1920×1080 画布精确填满正交视野）
   - CanvasScaler：Scale With Screen Size，Reference Resolution 1920×1080
3. `VisualCardsHandler` GameObject（Canvas 内或场景根节点）
4. `FishCardHolder`（Canvas 内，加 Horizontal Layout Group + FishCardHolder 脚本）

---

## 常见问题

| 问题 | 原因 | 解决 |
|------|------|------|
| 视觉卡出现在左下角 | FishCardVisual 根节点带 Canvas 组件 | 移除根节点 Canvas |
| 卡牌无法拖拽 | Image Raycast Target 未勾选 | 勾选 Raycast Target |
| 卡牌无法交换位置 | FishCardSlot Tag 不是 `Slot` | 检查 Tag |
| 弧线不生效 | `curve` 字段为空 | 拖入 CurveParameters 资源 |
| 卡牌锁定后仍可拖拽 | `isLocked` 未设置为 true | 检查 `card.isLocked = true` 是否执行 |

---

## ICardSlot 接口

槽位抽象接口，由所有可接受卡牌拖入的槽位实现（`ShopHangSlot`、`EquipmentSlotUI` 等）。`CrossHolderSystem` 通过此接口操作槽位，无需关心具体类型。

```csharp
bool IsOccupied { get; }
ItemCard OccupiedCard { get; }
bool CanAccept(ItemCard card);        // 类型校验（ShopHangSlot 只接受 FishData，EquipmentSlotUI 只接受对应类型装备）
void AcceptCard(ItemCard card);       // 接管卡牌，调用方保证槽位已空
void ReleaseCard();                   // 放弃卡牌，不销毁，由调用方接管
RectTransform GetSlotRect();
```

---

## ItemCard 锁定机制

`ItemCard.isLocked = true` 时屏蔽以下所有输入：`OnPointerEnter / Exit / Down / Up / OnBeginDrag`  
**注意**：槽位持有的卡牌现在不使用 `isLocked` 管理拖拽，而是通过 `CrossHolderSystem.RegisterSlotCard()` 注册拖拽监听；`isLocked` 仅用于手牌 UI 场景（防止误触消耗按钮等）。

---

## CrossHolderRole 枚举

定义 `FishCardHolder` 在跨 Holder 拖拽系统中的角色：

| 枚举值 | 说明 |
|--------|------|
| `None` | 不参与跨 Holder 拖拽（默认） |
| `Source` | 可从此 Holder 拖出（手牌 Holder） |
| `Target` | 可接受拖入（预留） |
| `Both` | 双向（预留，暂未使用） |

`FishCardHolder` 在 `Start()` 中自动向 `CrossHolderSystem` 注册（`role == Source`），在 `OnDestroy()` 中自动注销。  
`AddCard` / `RemoveCard` 时同步更新 `CrossHolderSystem` 的卡牌订阅，无需外部手动调用。

---

## CrossHolderSystem（双向拖拽框架）

全局跨 Holder 拖拽系统，挂在场景 Managers 节点上。支持三种交互场景：

- **场景1**：手牌卡 → 空槽 → `OnCardDroppedToSlot`（`IsOccupied=false`）
- **场景2**：手牌卡 → 已有卡的槽 → `OnCardDroppedToSlot`（`IsOccupied=true`，控制器执行替换）
- **场景3**：槽位卡 → 手牌区域 → `OnCardEjectedToHand`；槽位卡落其他位置一律归回原槽

```csharp
// Source Holder 自动注册（由 FishCardHolder 调用）
CrossHolderSystem.Instance.RegisterSource(FishCardHolder holder)
CrossHolderSystem.Instance.UnregisterSource(FishCardHolder holder)
CrossHolderSystem.Instance.SubscribeSourceCard(ItemCard card)
CrossHolderSystem.Instance.UnsubscribeSourceCard(ItemCard card)

// Target 槽位自动注册（由实现 ICardSlot 的组件在 OnEnable/OnDisable 调用）
CrossHolderSystem.Instance.RegisterTarget(RectTransform rect, ICardSlot slot)
CrossHolderSystem.Instance.UnregisterTarget(RectTransform rect)

// 槽位卡注册（由槽位 AcceptCard/ReleaseCard 调用）
CrossHolderSystem.Instance.RegisterSlotCard(ItemCard card, ICardSlot sourceSlot)
CrossHolderSystem.Instance.UnregisterSlotCard(ItemCard card)

// 手牌区域注册（由 HandPanelUI 在 Start/OnDestroy 调用）
CrossHolderSystem.Instance.RegisterHandZone(RectTransform rect)
CrossHolderSystem.Instance.UnregisterHandZone()

// 事件
CrossHolderSystem.Instance.OnCardDroppedToSlot   // UnityEvent<ItemCard, ICardSlot>（场景1、2）
CrossHolderSystem.Instance.OnCardEjectedToHand   // UnityEvent<ItemCard, ICardSlot>（场景3）
```

**Inspector 配置**：

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `yThreshold` | 1.5 | 触发跨 Holder 的 Y 轴偏移阈值（世界单位） |

**激活机制**：`HasActiveTargets`（至少一个 Target 注册或 handZoneRect 不为空）时才处理拖拽，否则透明放行。

---

## 常见问题（补充）

| 问题 | 原因 | 解决 |
|------|------|------|
| 数据不显示 | FishCardFrontDisplay 引用未绑定 | 检查 FishCardVisual 中的 frontDisplay 及 TMP / Image 引用 |
| 拖拽报 NullReferenceException | 相机 Tag 不是 `MainCamera` | 将相机 Tag 改为 `MainCamera` |
| 拖拽坐标完全错乱 | Camera 为透视模式或 Canvas 非 World Space | 相机改 Orthographic Size=5，Canvas 改 World Space |
| 拖拽后卡牌不回位 / 换序无效 | 卡牌未通过 `FishCardHolder.AddCard()` 注册 | 手动调用 `AddCard()` 注册卡牌 |
| 视觉卡渲染层级混乱 | 视觉卡挂在 Canvas 下而非 VisualCardsHandler 下 | 确保场景中存在 `VisualCardsHandler` |
| AddCard 后事件无响应报空引用 | FishCard 事件在 `Start()` 中初始化（旧版本） | 确认使用最新版 FishCard.cs（事件在 `Awake()` 中初始化） |
