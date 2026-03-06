# FishCardSystem Unity配置指南

**版本**：1.0  
**创建日期**：2026-02-05

---

## 一、前置准备

### 1.1 依赖检查

- ✅ Unity版本：2022.3及以上
- ✅ TextMeshPro包：已安装
- ✅ DOTween：**必须安装**（Asset Store或Package Manager）
- ✅ EventSystem：场景中需要存在

### 1.2 Tag配置

**必须创建Tag**：
1. Unity菜单 → Edit → Project Settings → Tags and Layers
2. 点击Tags下的"+"按钮
3. 添加新Tag：`Slot`（区分大小写）

---

## 二、创建ScriptableObject资源

### 2.1 CurveParameters资源

1. 在Project窗口中，右键点击 `Assets/Resources/FishCardSystem/` 文件夹
2. 选择 `Create → FishCard → Curve Parameters`
3. 命名为：`CurveParameters`

#### 配置参数

**位置曲线（positioning）**：
- 点击positioning字段，在AnimationCurve编辑器中设置：
  - 第1个关键帧：Time=0, Value=0
  - 第2个关键帧：Time=0.5, Value=1
  - 第3个关键帧：Time=1, Value=0
  - 形状：中间凸起的弧线

**旋转曲线（rotation）**：
- 点击rotation字段，在AnimationCurve编辑器中设置：
  - 第1个关键帧：Time=0, Value=1
  - 第2个关键帧：Time=0.5, Value=0
  - 第3个关键帧：Time=1, Value=-1
  - 形状：两端高中间低

**影响系数**：
- positioningInfluence = 0.02（可调整范围：0.02～0.1）
- rotationInfluence = 1.2（可调整范围：1～10）

---

## 三、创建预制体

### 3.1 FishCardSlot预制体（槽位）

#### 创建步骤

1. Hierarchy中右键 → `Create Empty`，命名为：`FishCardSlot`
2. 选中该物体，在Inspector中：
   - 添加组件：`RectTransform`
   - **设置Tag为：`Slot`**（重要！）
3. 配置RectTransform：
   - Width：100（由布局控制）
   - Height：220
   - Anchor：Stretch（左中右拉伸）
4. 将该物体拖拽到 `Assets/Prefab/FishCardSystem/` 创建预制体
5. 删除Hierarchy中的实例

---

### 3.2 FishCard预制体（逻辑卡）

#### 创建步骤

1. Hierarchy中右键 → `UI → Image`，命名为：`FishCard`
2. 配置RectTransform：
   - Width：110
   - Height：165
   - Anchor：Center
3. 配置Image组件：
   - Color：设置为半透明或纯色（仅用于射线检测）
   - Raycast Target：✅勾选
4. 添加组件：`FishCard`（脚本）
5. 添加组件：`CardFaceController`（脚本）

#### 配置FishCard组件

- **Instantiate Visual**：✅勾选
- **Card Visual Prefab**：稍后拖入FishCardVisual预制体（3.3创建后）
- **Move Speed Limit**：50
- **Selection Offset**：50

#### 创建正反面容器

在FishCard下创建两个空物体：

**FrontFace**：
- GameObject → Create Empty，命名：`FrontFace`
- 初始状态不需要内容（视觉在FishCardVisual中）

**BackFace**：
- GameObject → Create Empty，命名：`BackFace`
- 初始状态不需要内容（视觉在FishCardVisual中）

#### 配置CardFaceController组件

- **Front Face**：拖入FrontFace物体
- **Back Face**：拖入BackFace物体

#### 保存预制体

将FishCard拖拽到 `Assets/Prefab/FishCardSystem/` 创建预制体

---

### 3.3 FishCardVisual预制体（视觉卡）

这是最复杂的预制体，需要仔细配置层级结构。

#### 层级结构

```
FishCardVisual (RectTransform 110×165)
├── ShakeParent
│   ├── Shadow
│   └── TiltParent
│       ├── FrontFace
│       │   ├── Background (Image)
│       │   ├── FishIcon (Image)
│       │   ├── NameText (TextMeshProUGUI)
│       │   ├── ValueText (TextMeshProUGUI)
│       │   ├── DepthText (TextMeshProUGUI)
│       │   ├── StaminaCostText (TextMeshProUGUI)
│       │   ├── TypeText (TextMeshProUGUI)
│       │   ├── SizeText (TextMeshProUGUI)
│       │   └── EffectsText (TextMeshProUGUI)
│       └── BackFace
│           └── BackImage (Image)
```

#### 详细创建步骤

##### 1. 创建根节点FishCardVisual

1. Hierarchy中右键 → `Create Empty`，命名：`FishCardVisual`
2. 添加组件：`RectTransform`
3. 配置RectTransform：
   - Width：110
   - Height：165
   - Anchor：Center
4. **不要**在根节点添加 Canvas 组件。视觉卡运行时将作为主 Canvas 的子物体，由主 Canvas 统一渲染；根节点带 Canvas 会形成嵌套 Canvas，导致视觉卡不跟随 Slot 定位（出现在左下角或“跟摄像机”）。
5. 添加组件：`FishCardVisual`（脚本）
6. 添加组件：`CardFaceController`（脚本）

##### 2. 创建ShakeParent

1. 在FishCardVisual下创建空物体：`ShakeParent`
2. RectTransform：
   - Anchor：Stretch All
   - Left/Right/Top/Bottom：0

##### 3. 创建Shadow（阴影）

1. 在ShakeParent下创建：`UI → Image`，命名：`Shadow`
2. RectTransform：
   - Width：110
   - Height：165
   - Anchor：Center
   - Position：(0, -10, 0)（稍微向下偏移）
3. Image组件：
   - Color：黑色，Alpha=0.3（半透明黑）
   - Raycast Target：❌不勾选
4. 添加组件：`Canvas`
   - Override Sorting：✅勾选
   - Sort Order：-1（在卡牌下方）

##### 4. 创建TiltParent

1. 在ShakeParent下创建空物体：`TiltParent`
2. RectTransform：
   - Anchor：Stretch All
   - Left/Right/Top/Bottom：0

##### 5. 创建FrontFace（正面）

1. 在TiltParent下创建空物体：`FrontFace`
2. RectTransform：
   - Anchor：Stretch All
   - Left/Right/Top/Bottom：0
3. 添加组件：`FishCardFrontDisplay`（脚本）

**在FrontFace下创建UI元素**：

**Background（底图）**：
- `UI → Image`，命名：`Background`
- RectTransform：Stretch All，偏移为0
- Image：拖入您准备的卡牌正面底图Sprite

**FishIcon（鱼类图片）**：
- `UI → Image`，命名：`FishIcon`
- RectTransform：根据您的UI布局定位
- 示例：Width=80, Height=80, Anchor=Top Center, Position=(0, -40, 0)
- Image：先留空，运行时会动态设置

**文本字段**（使用TextMeshProUGUI）：

创建以下文本对象，并根据您的UI布局定位：

1. **NameText**：鱼类名称
   - `UI → Text - TextMeshPro`，命名：`NameText`
   - 示例位置：顶部居中

2. **ValueText**：价值
   - `UI → Text - TextMeshPro`，命名：`ValueText`
   - 示例位置：左上角

3. **DepthText**：深度
   - `UI → Text - TextMeshPro`，命名：`DepthText`
   - 示例位置：右上角

4. **StaminaCostText**：消耗体力
   - `UI → Text - TextMeshPro`，命名：`StaminaCostText`
   - 示例位置：左下角

5. **TypeText**：类型（纯净/污秽）
   - `UI → Text - TextMeshPro`，命名：`TypeText`
   - 示例位置：中部

6. **SizeText**：尺寸
   - `UI → Text - TextMeshPro`，命名：`SizeText`
   - 示例位置：中部

7. **EffectsText**：效果描述
   - `UI → Text - TextMeshPro`，命名：`EffectsText`
   - 示例位置：底部
   - 配置：支持多行，Overflow=Ellipsis或Truncate

##### 6. 创建BackFace（背面）

1. 在TiltParent下创建空物体：`BackFace`
2. RectTransform：
   - Anchor：Stretch All
   - Left/Right/Top/Bottom：0
3. 添加组件：`FishCardBackDisplay`（脚本）

**在BackFace下创建**：

**BackImage（背面图片）**：
- `UI → Image`，命名：`BackImage`
- RectTransform：Stretch All，偏移为0
- Image：先拖入一个默认背面图（如Fish_mediumback.png）

##### 7. 配置FishCardVisual组件

选中根节点FishCardVisual，在Inspector中配置：

**视觉组件引用**：
- **Visual Shadow**：拖入Shadow物体
- **Shake Parent**：拖入ShakeParent物体
- **Tilt Parent**：拖入TiltParent物体

**显示模块**：
- **Front Display**：拖入FrontFace上的FishCardFrontDisplay组件
- **Back Display**：拖入BackFace上的FishCardBackDisplay组件
- **Face Controller**：拖入根节点上的CardFaceController组件

**跟随参数**：
- Follow Speed：30

**旋转参数**：
- Rotation Amount：20
- Rotation Speed：20
- Auto Tilt Amount：30
- Manual Tilt Amount：20
- Tilt Speed：20

**缩放参数**：
- Scale Animations：✅勾选
- Scale On Hover：1.15
- Scale On Select：1.25
- Scale Transition：0.15
- Scale Ease：OutBack

**选中Punch参数**：
- Select Punch Amount：20

**悬停Punch参数**：
- Hover Punch Angle：5
- Hover Transition：0.15

**交换参数**：
- Swap Animations：✅勾选
- Swap Rotation Angle：30
- Swap Transition：0.15
- Swap Vibrato：5

**翻转参数**：
- Flip Duration：0.5
- Flip Curve：默认EaseInOut曲线

**弧线参数**：
- **Curve**：拖入之前创建的CurveParameters资源

##### 8. 配置FishCardFrontDisplay组件

选中FrontFace物体，配置FishCardFrontDisplay组件：

- **Background Image**：拖入Background的Image组件
- **Fish Icon**：拖入FishIcon的Image组件
- **Name Text**：拖入NameText的TextMeshProUGUI组件
- **Value Text**：拖入ValueText的TextMeshProUGUI组件
- **Depth Text**：拖入DepthText的TextMeshProUGUI组件
- **Stamina Cost Text**：拖入StaminaCostText的TextMeshProUGUI组件
- **Type Text**：拖入TypeText的TextMeshProUGUI组件
- **Size Text**：拖入SizeText的TextMeshProUGUI组件
- **Effects Text**：拖入EffectsText的TextMeshProUGUI组件

##### 9. 配置FishCardBackDisplay组件

选中BackFace物体，配置FishCardBackDisplay组件：

- **Back Image**：拖入BackImage的Image组件
- **Small Back Sprite**：拖入 `Assets/Resources/Card/Fish_minback.png`
- **Medium Back Sprite**：拖入 `Assets/Resources/Card/Fish_mediumback.png`
- **Large Back Sprite**：拖入 `Assets/Resources/Card/Fish_maxback.png`

##### 10. 配置CardFaceController组件

选中根节点FishCardVisual，配置CardFaceController组件：

- **Front Face**：拖入FrontFace物体
- **Back Face**：拖入BackFace物体

##### 11. 保存预制体

将FishCardVisual拖拽到 `Assets/Prefab/FishCardSystem/` 创建预制体

##### 12. 关联FishCard预制体

回到FishCard预制体：
- 打开FishCard预制体
- 在FishCard组件中，将**Card Visual Prefab**字段拖入FishCardVisual预制体
- 保存

---

## 四、场景配置

### 4.1 创建测试场景

#### 创建Canvas

1. Hierarchy中右键 → `UI → Canvas`
2. Canvas配置：
   - Render Mode：Screen Space - Overlay（或Camera，根据需求）
   - Canvas Scaler：Scale With Screen Size
   - Reference Resolution：1920×1080

#### 创建EventSystem

如果场景中没有EventSystem，会自动创建。确保存在。

#### 创建VisualCardsHandler

1. Hierarchy中右键 → `Create Empty`，命名：`VisualHandler`
2. 添加组件：`VisualCardsHandler`（脚本）
3. 将该物体放在Canvas下或场景根节点

#### 创建FishCardHolder容器

1. 在Canvas下创建：`UI → Panel`或空物体，命名：`FishCardHolder`
2. 添加组件：`Horizontal Layout Group`（推荐）
   - Child Alignment：Middle Center
   - Spacing：10
   - Child Force Expand：Width和Height都勾选
3. 添加组件：`FishCardHolder`（脚本）
4. 配置FishCardHolder组件：
   - **Slot Prefab**：拖入FishCardSlot预制体
   - **Cards To Spawn**：7（生成7个槽位）
   - **Tween Card Return**：✅勾选

#### 创建测试卡牌

为了测试，手动在某个槽位下创建卡牌：

1. 运行场景，会自动生成7个FishCardSlot
2. 停止运行
3. 在某个FishCardSlot下，拖入FishCard预制体实例
4. 选中该FishCard实例，在Inspector中：
   - **Card Data**：拖入一个FishData资源（需要您先创建）
5. 运行场景测试

---

## 五、测试与验证

### 5.1 基础功能测试

运行场景，测试以下功能：

- ✅ **卡牌生成**：7个槽位自动生成
- ✅ **数据显示**：卡牌正面显示FishData的所有信息
- ✅ **背面显示**：根据鱼类尺寸显示不同背面图片
- ✅ **悬停效果**：鼠标悬停时卡牌缩放和Punch旋转
- ✅ **点击选中**：短按选中，卡牌向上偏移
- ✅ **拖拽**：可以拖拽卡牌移动
- ✅ **拖拽排序**：拖拽卡牌越过其他卡时交换位置
- ✅ **弧线排布**：多张卡牌时呈弧线排列
- ✅ **倾斜效果**：卡牌有自动和手动倾斜
- ✅ **翻转动画**：调用FlipToFront()时卡牌翻转

### 5.2 常见问题排查

**问题1：卡牌无法拖拽**
- 检查Canvas是否有GraphicRaycaster组件
- 检查场景中是否有EventSystem
- 检查FishCard上的Image组件的Raycast Target是否勾选

**问题2：卡牌无法交换位置**
- 检查FishCardSlot的Tag是否设置为"Slot"
- 检查FishCardHolder组件的配置

**问题3：卡牌翻转后看不到正面**
- 检查CardFaceController的Front Face和Back Face引用
- 检查FishCardVisual的Face Controller引用

**问题4：卡牌动画不生效**
- 检查DOTween是否正确安装
- 检查Console是否有DOTween相关错误

**问题5：卡牌数据不显示**
- 检查FishCardFrontDisplay的所有UI组件引用
- 检查FishData是否正确赋值
- 检查Console是否有空引用错误

**问题6：卡牌不跟随 Slot 定位，出现在屏幕左下角或“跟着摄像机”**
- **原因**：FishCardVisual 预制体根节点上若带有 **Canvas** 组件（无论 Overlay 或 World Space），会形成嵌套 Canvas，导致视觉卡在独立渲染/坐标空间中，不跟随逻辑卡（Slot）位置。
- **解决**：从 FishCardVisual 预制体**根节点移除 Canvas 组件**。视觉卡运行时作为主 Canvas 的子物体，由主 Canvas 统一渲染即可，无需自带 Canvas。
- 同时确认主 Canvas 为 **Screen Space - Overlay**，CenterSlot 的 RectTransform 锚点居中、Pos 为 (0,0,0)。

---

## 六、使用示例代码

### 6.1 创建并初始化卡牌

```csharp
using UnityEngine;
using FishCardSystem;
using ItemSystem;

public class TestCardSpawner : MonoBehaviour
{
    public GameObject fishCardPrefab;
    public FishData testFishData;
    public Transform parentSlot;

    void Start()
    {
        // 实例化卡牌
        GameObject cardObj = Instantiate(fishCardPrefab, parentSlot);
        FishCard card = cardObj.GetComponent<FishCard>();
        
        // 初始化数据
        card.Initialize(testFishData);
        
        // 翻转到正面
        card.FlipToFront(0.5f);
    }
}
```

### 6.2 动态添加卡牌到容器

```csharp
using UnityEngine;
using FishCardSystem;
using ItemSystem;

public class TestCardManager : MonoBehaviour
{
    public FishCardHolder cardHolder;
    public GameObject fishCardPrefab;
    public FishData fishData;

    public void AddNewCard()
    {
        // 实例化卡牌
        GameObject cardObj = Instantiate(fishCardPrefab);
        FishCard card = cardObj.GetComponent<FishCard>();
        
        // 初始化数据
        card.Initialize(fishData);
        
        // 添加到容器
        cardHolder.AddCard(card);
    }
}
```

---

## 七、性能优化建议

1. **对象池**：大量卡牌创建/销毁时建议使用对象池
2. **Canvas分离**：将视觉卡的Canvas设置为独立Canvas减少重绘
3. **LOD**：远离视口的卡牌可以降低更新频率
4. **动画优化**：限制同时播放的DOTween动画数量

---

## 八、扩展指南

### 8.1 自定义卡牌样式

修改FishCardVisual预制体的UI布局和样式即可。

### 8.2 添加新的动画效果

在FishCardVisual.cs中添加新的动画方法，使用DOTween实现。

### 8.3 支持其他类型卡牌

1. 创建新的数据类继承ItemData
2. 创建对应的FrontDisplay组件
3. 修改FishCard支持多种数据类型

---

**配置完成后，您的鱼类卡牌系统就可以正常工作了！**
