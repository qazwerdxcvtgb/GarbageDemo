# FishCardSystem - 鱼类卡牌系统

**版本**：1.0  
**创建日期**：2026-02-05

---

## 快速开始

### 1. 检查依赖

- ✅ DOTween（必须安装）
- ✅ TextMeshPro
- ✅ ItemSystem（FishData）

### 2. 创建Tag

Unity菜单 → Edit → Project Settings → Tags and Layers → 添加Tag：`Slot`

### 3. 创建资源

在Project窗口中：
- 右键 `Assets/Resources/FishCardSystem/`
- Create → FishCard → Curve Parameters
- 配置曲线参数（详见配置指南）

### 4. 配置场景

1. 创建VisualHandler（挂载VisualCardsHandler）
2. 创建FishCardHolder（挂载FishCardHolder + 配置slotPrefab）
3. 创建预制体（详见配置指南）

---

## 文档索引

- **完整配置指南**：`Docs/FishCardSystem Unity配置指南.md`
- **系统文档**：`开发文档.md` - 六、FishCardSystem

---

## 脚本列表

### 核心层（逻辑卡）
- `Core/FishCard.cs` - 卡牌逻辑控制器（Awake 初始化事件，Start 实例化视觉卡）

### 视觉层（视觉卡）
- `Visual/FishCardVisual.cs` - 卡牌视觉控制器
- `Visual/FishCardFrontDisplay.cs` - 卡牌数据显示模块

### 测试辅助
- `CardSystemTester.cs` - 场景测试脚本，按槽位数量自动生成卡牌

### 管理层
- `Manager/VisualCardsHandler.cs` - 视觉卡管理器
- `Manager/FishCardHolder.cs` - 卡牌容器管理器

### 工具层
- `Utility/ExtensionMethods.cs` - 工具方法
- `Data/CurveParameters.cs` - 弧线参数ScriptableObject

---

## 特性

- ✅ 逻辑与视觉分离架构
- ✅ Balatro风格交互（拖拽、选中、悬停）
- ✅ DOTween动画系统
- ✅ 弧线排布
- ✅ 拖拽排序
- ✅ 模块化设计

---

## 使用示例

```csharp
using FishCardSystem;
using ItemSystem;

// 初始化卡牌
FishCard card = GetComponent<FishCard>();
card.Initialize(fishData);

// 订阅事件
card.SelectEvent.AddListener((c, selected) => {
    Debug.Log($"卡牌选中：{selected}");
});
```

---

## 注意事项

1. **Tag配置**：必须创建"Slot" Tag
2. **DOTween**：必须安装DOTween插件
3. **预制体结构**：层级结构必须正确
4. **CurveParameters**：需要正确配置AnimationCurve

---

**更多信息请参考完整配置指南**
