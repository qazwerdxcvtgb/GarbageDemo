# 玩家系统

> [← 返回索引](INDEX.md)  
> 覆盖脚本：`PlayerMove.cs` · `InteractionPoint.cs` · `InteractionPoint_1.cs` · `InteractionPoint_FishShop.cs`

---

## PlayerMove

**路径**：`Assets/Script/PlayerMove.cs`  
**依赖**：`Rigidbody2D`、`Animator`、Unity 新版 InputSystem

### 参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `moveSpeed` | 3f | 移动速度 |

### Animator 参数要求

| 参数名 | 类型 | 说明 |
|--------|------|------|
| `Horizontal` | float | 水平输入值 |
| `Vertical` | float | 垂直输入值 |
| `Speed` | float | 移动速度 |
| `isInteraction` | trigger | 交互触发器 |

### 输入映射

- **移动**：WASD 或手柄左摇杆
- **交互**：F 键或手柄 X 键 → 调用 `GameManager.Instance.TriggerInteractionPressed()`

---

## InteractionPoint（基类）

**路径**：`Assets/Script/InteractionPoint.cs`  
**依赖**：`Collider2D`（勾选 Is Trigger）、`Animator`、玩家 Tag = `"Player"`

### 工作原理

玩家进入 / 离开 Collider2D 触发范围时，设置 Animator 的 `isNear` 布尔参数控制提示图标显示。

### 子类扩展模板

```csharp
protected override void OnTriggerEnter2D(Collider2D other)
{
    base.OnTriggerEnter2D(other);  // 播放靠近动画
    if (other.CompareTag("Player"))
        GameManager.Instance.OnInteractionPressed.AddListener(OnPlayerInteract);
}

protected override void OnTriggerExit2D(Collider2D other)
{
    base.OnTriggerExit2D(other);   // 停止靠近动画
    if (other.CompareTag("Player"))
        GameManager.Instance.OnInteractionPressed.RemoveListener(OnPlayerInteract);
}
```

---

## InteractionPoint_1

**路径**：`Assets/Script/InteractionPoint_1.cs`  
**功能**：玩家交互时打开 `CardSelectionPanel`（卡片选择面板）

| Inspector 参数 | 说明 |
|----------------|------|
| `cardSelectionPanel` | 拖入 CardSelectionPanel 引用 |
| `drawCardDepth` | 本交互点对应的抽牌深度（FishDepth） |

---

## InteractionPoint_FishShop

**路径**：`Assets/Script/InteractionPoint_FishShop.cs`  
**功能**：玩家交互时打开 `FishShopPanel`（鱼店面板）  
无额外参数，依赖 `GameManager.Instance.OnInteractionPressed` 事件。
