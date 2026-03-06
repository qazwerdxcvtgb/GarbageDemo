# FishCardSystem 参数与方法说明文档

> 版本：1.0  
> 更新日期：2026-02-05  
> 说明：本文档详细说明FishCardSystem中各脚本的参数含义、调整方法和核心功能

---

## 目录

1. [FishCard（逻辑卡）](#fishcard逻辑卡)
2. [FishCardVisual（视觉卡）](#fishcardvisual视觉卡)
3. [FishCardFrontDisplay（正面显示）](#fishcardfrontdisplay正面显示)
4. [FishCardBackDisplay（背面显示）](#fishcardbackdisplay背面显示)
5. [CardFaceController（正反面控制）](#cardfacecontroller正反面控制)
6. [FishCardHolder（卡牌容器）](#fishcardholder卡牌容器)
7. [CurveParameters（弧线参数）](#curveparameters弧线参数)

---

## FishCard（逻辑卡）

### 脚本路径
`Assets/Script/FishCardSystem/Core/FishCard.cs`

### 核心职责
- **数据管理**：绑定`FishData`数据源
- **输入处理**：检测鼠标悬停、点击、拖拽事件
- **状态管理**：跟踪选中、悬停、拖拽状态
- **位置计算**：计算归位位置、规范化位置

### 参数说明

#### 数据参数

| 参数名 | 类型 | 说明 | 默认值 | 调整建议 |
|--------|------|------|--------|----------|
| `cardData` | `FishData` | 卡牌数据源（来自ItemSystem） | null | 从资源中拖入对应的`FishData`资产 |

#### 组件引用

| 参数名 | 类型 | 说明 | 自动获取 |
|--------|------|------|----------|
| `canvas` | `Canvas` | 父级Canvas组件 | ✓ 自动获取 |
| `imageComponent` | `Image` | 卡牌的Image组件 | ✓ 自动获取 |
| `faceController` | `CardFaceController` | 正反面控制器 | ✓ 自动获取 |

#### 视觉设置

| 参数名 | 类型 | 说明 | 默认值 | 调整建议 |
|--------|------|------|--------|----------|
| `instantiateVisual` | `bool` | 是否在Start时实例化视觉卡 | true | 通常保持勾选 |
| `cardVisualPrefab` | `GameObject` | 视觉卡预制体引用 | null | 拖入`FishCardVisual`预制体 |
| `cardVisual` | `FishCardVisual` | 实例化后的视觉卡引用 | null | ✓ 自动生成 |

#### 移动参数

| 参数名 | 类型 | 说明 | 默认值 | 调整建议 |
|--------|------|------|--------|----------|
| `moveSpeedLimit` | `float` | 拖拽时的最大移动速度 | 50f | 增大=拖拽更灵敏，减小=更平滑 |

#### 选中参数

| 参数名 | 类型 | 说明 | 默认值 | 调整建议 |
|--------|------|------|--------|----------|
| `selected` | `bool` | 当前是否被选中 | false | ✓ 运行时自动管理 |
| `selectionOffset` | `float` | 选中时的上移距离（像素） | 50f | 增大=选中时升得更高 |

#### 状态标志

| 参数名 | 类型 | 说明 | 运行时管理 |
|--------|------|------|-----------|
| `isHovering` | `bool` | 当前是否被鼠标悬停 | ✓ 自动管理 |
| `isDragging` | `bool` | 当前是否正在被拖拽 | ✓ 自动管理 |
| `wasDragged` | `bool` | 本次交互是否有过拖拽 | ✓ 自动管理 |

### 事件系统

| 事件名 | 参数 | 触发时机 |
|--------|------|----------|
| `PointerEnterEvent` | `FishCard` | 鼠标进入卡牌区域 |
| `PointerExitEvent` | `FishCard` | 鼠标离开卡牌区域 |
| `PointerDownEvent` | `FishCard` | 鼠标按下卡牌 |
| `PointerUpEvent` | `FishCard, bool longPress` | 鼠标抬起卡牌，longPress表示是否长按 |
| `BeginDragEvent` | `FishCard` | 开始拖拽卡牌 |
| `EndDragEvent` | `FishCard` | 结束拖拽卡牌 |
| `SelectEvent` | `FishCard, bool selected` | 卡牌选中状态改变 |

### 核心方法

#### `NormalizedPosition()`
```csharp
public float NormalizedPosition()
```
- **功能**：计算卡牌在兄弟节点中的归一化位置（0-1）
- **返回值**：`0.0`=最左侧，`1.0`=最右侧
- **用途**：用于弧线排列计算

#### `ParentIndex()`
```csharp
public int ParentIndex()
```
- **功能**：获取卡牌在父节点中的索引
- **返回值**：从0开始的整数索引

#### `SetCardData(FishData data)`
```csharp
public void SetCardData(FishData data)
```
- **功能**：更新卡牌数据并刷新显示
- **参数**：`FishData data` - 新的鱼类数据

---

## FishCardVisual（视觉卡）

### 脚本路径
`Assets/Script/FishCardSystem/Visual/FishCardVisual.cs`

### 核心职责
- **视觉动画**：所有缩放、旋转、倾斜、Punch动画
- **平滑跟随**：跟随逻辑卡的位置
- **弧线排列**：根据`CurveParameters`计算弧线位置
- **翻转动画**：正反面翻转效果

### 参数说明

#### 逻辑卡引用

| 参数名 | 类型 | 说明 | 初始化方式 |
|--------|------|------|-----------|
| `parentCard` | `FishCard` | 对应的逻辑卡引用 | ✓ 通过`Initialize()`自动绑定 |

#### 视觉组件引用

| 参数名 | 类型 | 说明 | 配置方式 |
|--------|------|------|----------|
| `shakeParent` | `Transform` | 用于Punch晃动动画的节点 | 拖入预制体中的`ShakeParent`节点 |
| `tiltParent` | `Transform` | 用于倾斜旋转的节点 | 拖入预制体中的`TiltParent`节点 |

#### 显示模块

| 参数名 | 类型 | 说明 | 配置方式 |
|--------|------|------|----------|
| `frontDisplay` | `FishCardFrontDisplay` | 正面显示控制器 | 拖入`FrontDisplay`脚本组件 |
| `backDisplay` | `FishCardBackDisplay` | 背面显示控制器 | 拖入`BackDisplay`脚本组件 |
| `faceController` | `CardFaceController` | 正反面切换控制器 | 拖入`CardFaceController`脚本组件 |

#### 跟随参数

| 参数名 | 类型 | 说明 | 默认值 | 调整建议 |
|--------|------|------|--------|----------|
| `followSpeed` | `float` | 视觉卡跟随逻辑卡的速度 | 30f | 增大=跟随更快（10-50） |

**效果说明**：
- `10-20`：缓慢跟随，有明显延迟感
- `30`：平衡值，自然的跟随效果
- `40-50`：快速跟随，几乎无延迟

#### 旋转参数

| 参数名 | 类型 | 说明 | 默认值 | 调整建议 |
|--------|------|------|--------|----------|
| `rotationAmount` | `float` | Z轴旋转的最大角度（度） | 10f | 增大=旋转更明显（5-20） |
| `rotationSpeed` | `float` | Z轴旋转的速度 | 12f | 增大=旋转更快（8-20） |
| `autoTiltAmount` | `float` | 自动倾斜的角度（X/Y轴） | 0f | 设为0禁用静态旋转 |
| `manualTiltAmount` | `float` | 手动交互时倾斜角度 | 15f | 增大=交互倾斜更明显（10-30） |
| `tiltSpeed` | `float` | 倾斜的速度 | 10f | 增大=倾斜更快（5-20） |

**效果说明**：
- **Z轴旋转（`rotationAmount` + `rotationSpeed`）**：
  - 卡牌在手牌中排列时的扇形旋转
  - 位置越靠边，旋转角度越大
  
- **X/Y轴倾斜（`manualTiltAmount` + `tiltSpeed`）**：
  - 鼠标悬停、拖拽时的3D倾斜效果
  - 增强卡牌的立体感

- **自动倾斜（`autoTiltAmount`）**：
  - 设为`0`：卡牌保持平面，不会自动旋转
  - 设为`10-30`：卡牌会随时间缓慢晃动（类似呼吸效果）

#### 缩放参数

| 参数名 | 类型 | 说明 | 默认值 | 调整建议 |
|--------|------|------|--------|----------|
| `scaleAnimations` | `bool` | 是否启用缩放动画 | true | 禁用可提升性能 |
| `scaleOnHover` | `float` | 悬停时的缩放倍数 | 1.15f | 增大=悬停更明显（1.1-1.3） |
| `scaleOnSelect` | `float` | 选中时的缩放倍数 | 1.25f | 增大=选中更明显（1.15-1.4） |
| `scaleTransition` | `float` | 缩放过渡时间（秒） | 0.15f | 减小=更快，增大=更柔和 |
| `scaleEase` | `Ease` | 缩放缓动曲线 | `OutBack` | 更换为其他DOTween曲线 |

**缩放阶段**：
1. **正常**：缩放 = `1.0`
2. **悬停**：缩放 = `scaleOnHover`（1.15）
3. **按下**：缩放 = `scaleOnSelect`（1.25）

#### 选中Punch参数

| 参数名 | 类型 | 说明 | 默认值 | 调整建议 |
|--------|------|------|--------|----------|
| `selectPunchAmount` | `float` | 选中时的弹跳角度 | 20f | 增大=弹跳更夸张（10-40） |

**效果说明**：
- 点击卡牌时，`ShakeParent`会产生Z轴旋转弹跳
- 配合缩放动画，增强反馈感

#### 悬停Punch参数

| 参数名 | 类型 | 说明 | 默认值 | 调整建议 |
|--------|------|------|--------|----------|
| `hoverPunchAngle` | `float` | 悬停时的弹跳角度 | 5f | 增大=弹跳更明显（3-15） |
| `hoverTransition` | `float` | 悬停Punch的持续时间 | 0.15f | 增大=动画更慢 |

**效果说明**：
- 鼠标进入卡牌时触发
- 轻微的Z轴旋转弹跳，增强交互反馈

#### 交换参数

| 参数名 | 类型 | 说明 | 默认值 | 调整建议 |
|--------|------|------|--------|----------|
| `swapAnimations` | `bool` | 是否启用交换动画 | true | 禁用可提升性能 |
| `swapRotationAngle` | `float` | 交换时的旋转角度 | 30f | 增大=旋转更明显（20-50） |
| `swapTransition` | `float` | 交换动画持续时间 | 0.15f | 增大=动画更慢 |
| `swapVibrato` | `int` | 交换Punch的震动次数 | 5 | 增大=震动更多次（3-10） |

**效果说明**：
- 拖拽卡牌替换其他卡牌位置时触发
- `direction`参数决定旋转方向（1或-1）

#### 翻转参数

| 参数名 | 类型 | 说明 | 默认值 | 调整建议 |
|--------|------|------|--------|----------|
| `flipCurve` | `AnimationCurve` | 翻转动画的缓动曲线 | `EaseInOut(0,0,1,1)` | 在Inspector中调整曲线形状 |

**效果说明**：
- 控制翻转时Y轴旋转的速度变化
- 默认曲线：开始慢→中间快→结束慢

#### 弧线参数

| 参数名 | 类型 | 说明 | 默认值 | 调整建议 |
|--------|------|------|--------|----------|
| `curve` | `CurveParameters` | 弧线参数资产 | null | 拖入创建的`CurveParameters`资产 |

**效果说明**：
- 控制卡牌在手牌中的弧形排列
- 详见[CurveParameters章节](#curveparameters弧线参数)

### 核心方法

#### `Initialize(FishCard target)`
```csharp
public void Initialize(FishCard target)
```
- **功能**：初始化视觉卡，绑定逻辑卡并订阅事件
- **参数**：`target` - 对应的逻辑卡
- **调用时机**：由`FishCard.Start()`自动调用

#### `FlipToFront(float duration)`
```csharp
public void FlipToFront(float duration)
```
- **功能**：翻转卡牌到正面
- **参数**：`duration` - 翻转动画持续时间（秒）

#### `FlipToBack(float duration)`
```csharp
public void FlipToBack(float duration)
```
- **功能**：翻转卡牌到背面
- **参数**：`duration` - 翻转动画持续时间（秒）

#### `UpdateCardData(FishData data)`
```csharp
public void UpdateCardData(FishData data)
```
- **功能**：更新卡牌数据并刷新显示
- **参数**：`data` - 新的鱼类数据

#### `PlaySwapAnimation(int direction)`
```csharp
public void PlaySwapAnimation(int direction)
```
- **功能**：播放交换位置动画
- **参数**：`direction` - 旋转方向（1=顺时针，-1=逆时针）

### Update循环（每帧执行）

| 方法名 | 功能 | 执行顺序 |
|--------|------|----------|
| `HandPositioning()` | 计算弧线位置和旋转偏移 | 1 |
| `SmoothFollow()` | 平滑跟随逻辑卡位置 | 2 |
| `FollowRotation()` | 计算并应用Z轴旋转 | 3 |
| `CardTilt()` | 计算并应用X/Y轴倾斜 | 4 |

---

## FishCardFrontDisplay（正面显示）

### 脚本路径
`Assets/Script/FishCardSystem/Visual/FishCardFrontDisplay.cs`

### 核心职责
- **UI元素绑定**：管理正面所有UI组件引用
- **数据显示**：根据`FishData`更新UI文本和图片

### 参数说明

#### UI组件引用

| 参数名 | 类型 | 说明 | 配置方式 |
|--------|------|------|----------|
| `fishNameText` | `TMP_Text` | 鱼类名称文本 | 拖入对应TextMeshPro组件 |
| `fishValueText` | `TMP_Text` | 价值文本 | 拖入对应TextMeshPro组件 |
| `fishDepthText` | `TMP_Text` | 深度文本 | 拖入对应TextMeshPro组件 |
| `fishStaminaCostText` | `TMP_Text` | 体力消耗文本 | 拖入对应TextMeshPro组件 |
| `fishTypeText` | `TMP_Text` | 类型文本 | 拖入对应TextMeshPro组件 |
| `fishSizeText` | `TMP_Text` | 尺寸文本 | 拖入对应TextMeshPro组件 |
| `fishEffectText` | `TMP_Text` | 效果描述文本 | 拖入对应TextMeshPro组件 |
| `fishImage` | `Image` | 鱼类图片 | 拖入对应Image组件 |

### 核心方法

#### `UpdateDisplay(FishData data)`
```csharp
public void UpdateDisplay(FishData data)
```
- **功能**：根据`FishData`更新所有UI元素
- **参数**：`data` - 鱼类数据
- **更新内容**：
  - 名称：`data.name`
  - 价值：`data.value`
  - 深度：转换为中文（"浅层"/"中层"/"深层"）
  - 体力消耗：`data.staminaCost`
  - 类型：转换为中文（"普通"/"稀有"/"传说"）
  - 尺寸：转换为中文（"小型"/"中型"/"大型"）
  - 效果：`data.effectDescription`
  - 图片：`data.itemSprite`

### 文本转换逻辑

#### 深度转换（`GetDepthText`）
- `FishDepth.Shallow` → "浅层"
- `FishDepth.Medium` → "中层"
- `FishDepth.Deep` → "深层"

#### 类型转换（`GetTypeText`）
- `FishType.Common` → "普通"
- `FishType.Rare` → "稀有"
- `FishType.Legendary` → "传说"

#### 尺寸转换（`GetSizeText`）
- `FishSize.Small` → "小型"
- `FishSize.Medium` → "中型"
- `FishSize.Large` → "大型"

---

## FishCardBackDisplay（背面显示）

### 脚本路径
`Assets/Script/FishCardSystem/Visual/FishCardBackDisplay.cs`

### 核心职责
- **背面图片管理**：根据`FishSize`切换不同的背面样式

### 参数说明

#### 背面样式配置

| 参数名 | 类型 | 说明 | 配置方式 |
|--------|------|------|----------|
| `backImage` | `Image` | 背面Image组件 | 拖入背面的Image组件 |
| `smallBackSprite` | `Sprite` | 小型鱼背面图片 | 拖入小型鱼背面美术资源 |
| `mediumBackSprite` | `Sprite` | 中型鱼背面图片 | 拖入中型鱼背面美术资源 |
| `largeBackSprite` | `Sprite` | 大型鱼背面图片 | 拖入大型鱼背面美术资源 |

### 核心方法

#### `UpdateDisplay(FishData data)`
```csharp
public void UpdateDisplay(FishData data)
```
- **功能**：根据`FishSize`切换背面图片
- **参数**：`data` - 鱼类数据
- **切换逻辑**：
  - `FishSize.Small` → 显示`smallBackSprite`
  - `FishSize.Medium` → 显示`mediumBackSprite`
  - `FishSize.Large` → 显示`largeBackSprite`

---

## CardFaceController（正反面控制）

### 脚本路径
`Assets/Script/FishCardSystem/Core/CardFaceController.cs`

### 核心职责
- **正反面切换**：管理`frontFace`和`backFace`的显示/隐藏
- **状态跟踪**：记录当前显示正面还是背面

### 参数说明

#### 正反面引用

| 参数名 | 类型 | 说明 | 配置方式 |
|--------|------|------|----------|
| `frontFace` | `GameObject` | 正面GameObject | 拖入包含正面UI的GameObject |
| `backFace` | `GameObject` | 背面GameObject | 拖入包含背面UI的GameObject |

### 核心方法

#### `ShowFront()`
```csharp
public void ShowFront()
```
- **功能**：显示正面，隐藏背面
- **效果**：`frontFace.SetActive(true)`, `backFace.SetActive(false)`

#### `ShowBack()`
```csharp
public void ShowBack()
```
- **功能**：显示背面，隐藏正面
- **效果**：`frontFace.SetActive(false)`, `backFace.SetActive(true)`

#### `SetFaceVisible(bool showFront, bool immediate)`
```csharp
public void SetFaceVisible(bool showFront, bool immediate)
```
- **功能**：设置正反面可见性
- **参数**：
  - `showFront` - 是否显示正面
  - `immediate` - 是否立即切换（不带动画）

#### `IsFrontVisible` 属性
```csharp
public bool IsFrontVisible { get; }
```
- **功能**：获取当前是否显示正面
- **返回值**：`true`=显示正面，`false`=显示背面

---

## FishCardHolder（卡牌容器）

### 脚本路径
`Assets/Script/FishCardSystem/Manager/FishCardHolder.cs`

### 核心职责
- **卡槽管理**：管理`FishCardSlot`子节点
- **拖拽排序**：支持拖拽卡牌交换位置
- **归位动画**：卡牌返回槽位的平滑动画
- **弧线布局**：自动应用`CurveParameters`的弧线效果

### 参数说明

#### 弧线参数

| 参数名 | 类型 | 说明 | 默认值 | 调整建议 |
|--------|------|------|--------|----------|
| `curve` | `CurveParameters` | 弧线参数资产 | null | 拖入创建的`CurveParameters`资产 |

#### 返回动画参数

| 参数名 | 类型 | 说明 | 默认值 | 调整建议 |
|--------|------|------|--------|----------|
| `returnSpeed` | `float` | 返回槽位的速度 | 15f | 增大=返回更快（10-30） |
| `returnRotationSpeed` | `float` | 返回时旋转的速度 | 5f | 增大=旋转恢复更快（3-10） |

**效果说明**：
- **returnSpeed**：拖拽结束后，卡牌移动回槽位的速度
- **returnRotationSpeed**：拖拽结束后，卡牌旋转恢复到0的速度

### 核心方法

#### `AddCard(FishCard card)`
```csharp
public void AddCard(FishCard card)
```
- **功能**：添加卡牌到容器中（未实现，需手动添加子节点）
- **参数**：`card` - 要添加的卡牌

#### `RemoveCard(FishCard card)`
```csharp
public void RemoveCard(FishCard card)
```
- **功能**：从容器中移除卡牌（未实现，需手动删除子节点）
- **参数**：`card` - 要移除的卡牌

### 事件订阅（自动管理）

| 事件 | 处理方法 | 功能 |
|------|----------|------|
| `BeginDragEvent` | `OnCardBeginDrag` | 卡牌开始拖拽 |
| `EndDragEvent` | `OnCardEndDrag` | 卡牌结束拖拽，处理排序和归位 |

---

## CurveParameters（弧线参数）

### 脚本路径
`Assets/Script/FishCardSystem/Data/CurveParameters.cs`

### 核心职责
- **弧线定义**：通过`AnimationCurve`定义卡牌排列的弧形轨迹
- **参数存储**：使用`ScriptableObject`存储可复用的弧线配置

### 参数说明

#### 位置曲线

| 参数名 | 类型 | 说明 | 建议值 |
|--------|------|------|--------|
| `positioning` | `AnimationCurve` | 控制卡牌Y轴偏移的曲线 | (0,0), (0.5,1), (1,0) |
| `positioningInfluence` | `float` | 位置影响系数（像素） | 0.02～0.1 |

**效果说明**：
- **positioning曲线**：
  - X轴：卡牌在手牌中的归一化位置（0=最左，1=最右）
  - Y轴：曲线高度（0-1）
  - 建议形状：中间凸起的抛物线
  
- **positioningInfluence**：
  - 控制弧线的高度（单位：Canvas像素）
  - `0.05`：轻微弧线（约5% Canvas高度）
  - `0.1`：明显弧线（约10% Canvas高度）

**配置示例**：
```
曲线关键帧：
- (0, 0)     最左侧卡牌，Y偏移=0
- (0.5, 1)   中间卡牌，Y偏移最大
- (1, 0)     最右侧卡牌，Y偏移=0

Influence = 0.1：
中间卡牌会向上偏移 Canvas高度 * 0.1
```

#### 旋转曲线

| 参数名 | 类型 | 说明 | 建议值 |
|--------|------|------|--------|
| `rotation` | `AnimationCurve` | 控制卡牌Z轴旋转的曲线 | (0,1), (0.5,0), (1,-1) |
| `rotationInfluence` | `float` | 旋转影响系数（度） | 1～10 |

**效果说明**：
- **rotation曲线**：
  - X轴：卡牌在手牌中的归一化位置
  - Y轴：旋转系数（-1到1）
  - 建议形状：线性递减（左正右负）
  
- **rotationInfluence**：
  - 控制旋转的最大角度（单位：度）
  - `5`：轻微旋转
  - `10`：明显旋转（类似扇形效果）

**配置示例**：
```
曲线关键帧：
- (0, 1)     最左侧卡牌，旋转系数=1
- (0.5, 0)   中间卡牌，旋转系数=0
- (1, -1)    最右侧卡牌，旋转系数=-1

Influence = 10：
最左侧卡牌：旋转 +10°（逆时针）
中间卡牌：旋转 0°（不旋转）
最右侧卡牌：旋转 -10°（顺时针）
```

### 创建步骤

1. 右键点击`Project`窗口
2. 选择`Create > FishCard > Curve Parameters`
3. 命名（如`DefaultCurveParameters`）
4. 在Inspector中调整曲线和影响系数
5. 拖入`FishCardHolder`和`FishCardVisual`的`curve`字段

---

## 快速调参指南

### 场景1：卡牌跟随太慢/太快

**调整脚本**：`FishCardVisual`  
**调整参数**：`followSpeed`

| 问题 | 当前值 | 建议值 |
|------|--------|--------|
| 跟随太慢，有拖尾 | 30 | 增大到 40-50 |
| 跟随太快，不够平滑 | 30 | 减小到 15-25 |

---

### 场景2：卡牌旋转/晃动太明显

**调整脚本**：`FishCardVisual`  
**调整参数**：

| 参数名 | 当前值 | 建议调整 |
|--------|--------|----------|
| `autoTiltAmount` | 0f | 保持0（禁用静态旋转） |
| `manualTiltAmount` | 15f | 减小到 8-12 |
| `rotationAmount` | 10f | 减小到 5-8 |

---

### 场景3：卡牌缩放效果不明显

**调整脚本**：`FishCardVisual`  
**调整参数**：

| 参数名 | 当前值 | 建议调整 |
|--------|--------|----------|
| `scaleOnHover` | 1.15f | 增大到 1.2-1.25 |
| `scaleOnSelect` | 1.25f | 增大到 1.3-1.35 |
| `scaleTransition` | 0.15f | 保持或减小到 0.1 |

---

### 场景4：弧线效果太夸张/不明显

**调整脚本**：`CurveParameters`资产  
**调整参数**：

| 问题 | 当前值 | 建议调整 |
|------|--------|----------|
| 弧线太高 | `positioningInfluence = 0.1` | 减小到 0.05 |
| 弧线太平 | `positioningInfluence = 0.05` | 增大到 0.1-0.15 |
| 旋转太大 | `rotationInfluence = 10` | 减小到 5-8 |
| 旋转太小 | `rotationInfluence = 5` | 增大到 10-15 |

---

### 场景5：拖拽返回太慢

**调整脚本**：`FishCardHolder`  
**调整参数**：

| 参数名 | 当前值 | 建议调整 |
|--------|--------|----------|
| `returnSpeed` | 15f | 增大到 20-30 |
| `returnRotationSpeed` | 5f | 增大到 8-12 |

---

## 常见问题

### Q1：卡牌点击没有反应？
**检查清单**：
1. `FishCard`的`Image`组件是否勾选了`Raycast Target`
2. 场景中是否有`EventSystem`
3. `Canvas`的`Graphic Raycaster`是否启用

### Q2：视觉卡没有实例化？
**检查清单**：
1. `FishCard.instantiateVisual`是否勾选
2. `FishCard.cardVisualPrefab`是否拖入了预制体
3. 场景中是否有`VisualCardsHandler`对象

### Q3：弧线效果不生效？
**检查清单**：
1. `FishCardVisual.curve`是否拖入了`CurveParameters`资产
2. `FishCardHolder.curve`是否拖入了同样的资产
3. `CurveParameters`的曲线是否正确配置

### Q4：卡牌数据不显示？
**检查清单**：
1. `FishCard.cardData`是否拖入了有效的`FishData`
2. `FishCardFrontDisplay`的所有UI组件引用是否正确
3. `FishData`中的字段是否有值

### Q5：翻转动画不流畅？
**解决方案**：
1. 调整`FishCardVisual.flipCurve`曲线为更平滑的形状
2. 增大翻转持续时间（调用`FlipToFront`/`FlipToBack`时传入更大的`duration`）

---

## 附录：DOTween缓动曲线参考

`FishCardVisual.scaleEase`可选值：

| 缓动类型 | 效果 | 适用场景 |
|----------|------|----------|
| `Linear` | 线性匀速 | 简单动画 |
| `OutQuad` | 快速开始，缓慢结束 | 自然减速 |
| `OutBack` | 超过目标后回弹（**当前使用**） | 弹性反馈 |
| `OutElastic` | 弹簧效果 | 夸张动画 |
| `InOutCubic` | 慢-快-慢 | 平滑过渡 |

---

## 相关文档

- [FishCardSystem Unity配置指南](./FishCardSystem Unity配置指南.md)
- [开发文档 - FishCardSystem章节](../开发文档.md#六fishcardsystem---鱼类卡牌系统)

---

**文档结束**
