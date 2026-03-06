# 鱼类卡牌系统

> [← 返回索引](INDEX.md)  
> 覆盖脚本：`FishCardSystem/` 下所有脚本  
> 旧版详细文档：[参数说明](Archive/FishCardSystem%20参数与方法说明.md) · [Unity配置](Archive/FishCardSystem%20Unity配置指南.md)

---

## 架构概览

```
FishCard（逻辑卡）                负责输入、状态、位置计算
  └── 实例化 FishCardVisual（视觉卡）  负责所有动画和视觉效果
        ├── FishCardFrontDisplay    正面 UI 数据绑定
        ├── FishCardBackDisplay     背面图片（按 FishSize 切换）
        └── CardFaceController      正反面 GameObject 显隐

FishCardHolder（卡牌容器）        管理多个 FishCardSlot，处理拖拽排序
VisualCardsHandler                视觉卡全局管理（场景中须存在）
CurveParameters（ScriptableObject）定义手牌弧线曲线
```

---

## 脚本清单

| 脚本 | 路径 | 职责 |
|------|------|------|
| `FishCard` | `Core/FishCard.cs` | 逻辑卡：输入、状态、位置 |
| `CardFaceController` | `Core/CardFaceController.cs` | 正反面 GameObject 显隐 |
| `FishCardVisual` | `Visual/FishCardVisual.cs` | 视觉卡：动画、跟随、弧线 |
| `FishCardFrontDisplay` | `Visual/FishCardFrontDisplay.cs` | 正面 UI 数据绑定 |
| `FishCardBackDisplay` | `Visual/FishCardBackDisplay.cs` | 背面图片（按鱼的 Size） |
| `FishCardHolder` | `Manager/FishCardHolder.cs` | 容器：槽位管理、拖拽排序 |
| `VisualCardsHandler` | `Manager/VisualCardsHandler.cs` | 视觉卡全局注册管理 |
| `CurveParameters` | `Data/CurveParameters.cs` | 弧线 ScriptableObject |

---

## 核心 API

### FishCard

```csharp
fishCard.SetCardData(FishData data)   // 更新卡牌数据并刷新显示
fishCard.NormalizedPosition()          // 归一化位置（0=最左, 1=最右）
fishCard.ParentIndex()                 // 在父节点中的索引
```

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

### FishCardVisual

```csharp
visual.FlipToFront(float duration)    // 翻转到正面
visual.FlipToBack(float duration)     // 翻转到背面
visual.UpdateCardData(FishData data)  // 更新数据并刷新显示
visual.PlaySwapAnimation(int dir)     // 交换动画（1=顺时针 / -1=逆时针）
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

### CardFaceController

```csharp
controller.ShowFront()
controller.ShowBack()
controller.SetFaceVisible(bool showFront, bool immediate)
controller.IsFrontVisible   // 只读属性
```

---

## Unity 配置摘要

### 必要 Tag
- `Edit → Project Settings → Tags and Layers` 添加 Tag：`Slot`
- 场景中的相机必须设置 Tag 为 `MainCamera`（`Camera.main` 依赖此标签）

### 预制体清单

| 预制体 | 关键要求 | 保存路径 |
|--------|---------|---------|
| `FishCardSlot` | RectTransform，Tag = `Slot` | `Prefab/FishCardSystem/` |
| `FishCard` | Image（RaycastTarget=✓），FishCard，CardFaceController | `Prefab/FishCardSystem/` |
| `FishCardVisual` | FishCardVisual，CardFaceController，**根节点不加 Canvas** | `Prefab/FishCardSystem/` |

### FishCardVisual 层级

```
FishCardVisual（110×165，无 Canvas）
└── ShakeParent（Stretch All）
    ├── Shadow（Image，Canvas SortOrder=-1）
    └── TiltParent（Stretch All）
        ├── FrontFace + FishCardFrontDisplay
        └── BackFace + FishCardBackDisplay
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
| 数据不显示 | FishCardFrontDisplay 引用未绑定 | 检查各 TMP / Image 引用 |
| 拖拽报 NullReferenceException | 相机 Tag 不是 `MainCamera` | 将相机 Tag 改为 `MainCamera` |
| 拖拽坐标完全错乱 | Camera 为透视模式或 Canvas 非 World Space | 相机改 Orthographic Size=5，Canvas 改 World Space |
