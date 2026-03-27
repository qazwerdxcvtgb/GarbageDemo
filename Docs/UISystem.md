# UI 系统

> [← 返回索引](INDEX.md)  
> 覆盖脚本：`Script/UI/` 目录下所有脚本

---

## 概述

UI 系统负责 HUD 状态显示和深度指示。仅包含轻量级的显示组件，通过事件订阅自动更新，无需手动调用。

装备面板（EquipmentPanel、EquipmentSlotUI）归属于 [ShopSystem](ShopSystem.md)。  
手牌面板（HandPanelUI）归属于 [FishCardSystem](FishCardSystem.md)。

---

## 脚本清单

| 脚本 | 类型 | 职责 |
|------|------|------|
| `HealthDisplayUI` | MonoBehaviour | 体力条显示（Slider + 数值文本） |
| `GoldDisplayUI` | MonoBehaviour | 金币数量显示 |
| `SanityDisplayUI` | MonoBehaviour | 疯狂值显示（数值 + 等级文本） |
| `DepthIndicatorUI` | MonoBehaviour | 玩家深度指示图标，随深度变化位置对齐 |

---

## API 速查

### HealthDisplayUI

**路径**：`Assets/Script/UI/HealthDisplayUI.cs`  
**订阅事件**：`CharacterState.OnHealthChanged` / `OnMaxHealthChanged`  
**组件要求**：`Slider` + `TMP_Text`

自动订阅角色体力变化事件，更新 Slider 和文本显示。无需手动调用。

### GoldDisplayUI

**路径**：`Assets/Script/UI/GoldDisplayUI.cs`  
**订阅事件**：`CharacterState.OnGoldChanged`  
**组件要求**：`TMP_Text`

自动订阅角色金币变化事件，更新文本显示。

### SanityDisplayUI

**路径**：`Assets/Script/UI/SanityDisplayUI.cs`  
**订阅事件**：`GameManager.OnSanityChanged` / `OnSanityLevelChanged`  
**组件要求**：`TMP_Text`

自动订阅疯狂值变化事件，更新数值和等级文本显示。

### DepthIndicatorUI

**路径**：`Assets/Script/UI/DepthIndicatorUI.cs`  
**命名空间**：`UISystem`  
**订阅事件**：`CharacterState.OnDepthChanged`

在固定大小的容器内，根据玩家当前深度调整图标的垂直对齐位置：

| 深度 | 锚点 | 对齐 |
|------|------|------|
| Depth1 | 置顶 | 图标顶对齐容器 |
| Depth2 | 居中 | 图标垂直居中 |
| Depth3 | 置底 | 图标底对齐容器 |

**Inspector 参数**：

| 参数 | 说明 |
|------|------|
| `playerState` | 玩家 CharacterState 组件引用 |
| `depth1Sprite` / `depth2Sprite` / `depth3Sprite` | 各深度对应图标（可选） |

---

## 依赖关系

- **依赖**：Core（CharacterState、GameManager 提供事件源）
- **被依赖**：无（纯显示组件）
