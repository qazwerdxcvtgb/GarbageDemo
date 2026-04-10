# 天数系统

> [← 返回索引](INDEX.md)  
> 覆盖脚本：`DaySystem/DayManager.cs` · `DaySystem/DeclarationPanel.cs` · `DaySystem/DayEndPanel.cs` · `DaySystem/GameOverPanel.cs` · `DaySystem/DayDisplayUI.cs`

---

## 概述

天数系统管理一局游戏的完整生命周期：从星期一（D1）到星期六（D6），共 6 天。每天经过 Start → Refresh → Declaration → Action 四个阶段。D6 行动结束后进入 GameOver。

---

## 每日流程

```
D1(星期一): 跳过Start → Refresh(恢复体力) → 跳过Declaration → 自动选择钓鱼 → Action(行动)
D2-D6:     Start(结算) → Refresh(恢复体力) → Declaration(选择) → Action(行动)
D6结束:    → GameOver(游戏结束面板)
```

> **D1 简化说明**：D1 玩家金币=0、手牌=0，自动跳过声明阶段和钓鱼准备（装备调整），直接进入钓鱼行动。由 `DayManager.EnterFishingDirectly()` 实现。

### Start 阶段（D2-D6，系统自动执行）

按顺序执行：
1. 天数 +1
2. 丢弃钓鱼桌上所有 FaceUp 状态的牌堆顶牌（不加入手牌）
3. 每日效果：

| 天数 | 效果 |
|------|------|
| D3 | 玩家获得 1 张杂鱼牌 |
| D4 | 玩家获得 3 金币 |
| D5 | 玩家获得 1 张杂鱼牌 |
| D6 | 玩家体力上限 +3 |

4. 深度回退：Depth3→Depth2, Depth2→Depth1, Depth1 不变

### Refresh 阶段（每天执行，系统自动）

将当前体力恢复为最大值。

### Declaration 阶段（等待玩家选择）

显示 DeclarationPanel，玩家选择"去商店"或"去钓鱼"。

### Action 阶段（玩家自由行动）

- 选择商店：自动打开 ShopPanel
- 选择钓鱼：正常钓鱼流程
- 显示"下一天"按钮（D6 显示为"结束"）

### DayEnd 阶段（D1-D5，过渡面板）

玩家按"下一天"后弹出 DayEndPanel（日终结算面板），玩家点击关闭后进入下一天的 Start 阶段。D6 不弹出此面板，直接进入 GameOver。

---

## DayManager

**路径**：`Assets/Script/DaySystem/DayManager.cs`  
**命名空间**：`DaySystem`  
**模式**：单例 `DayManager.Instance`，`DontDestroyOnLoad`

### 属性

```csharp
DayManager.Instance.CurrentDay      // int, 当前天数 1-6
DayManager.Instance.CurrentPhase    // GamePhase 枚举
DayManager.Instance.CurrentAction   // DayAction 枚举（Fishing / Shopping）
DayManager.GetDayName(int day)      // 静态方法，返回"星期X"
DayManager.MAX_DAY                  // 常量 6
```

### 方法

```csharp
DayManager.Instance.StartGame()              // 开始新游戏（从D1）
DayManager.Instance.OnNextDayClicked()       // 下一天按钮回调（Editor绑定）
DayManager.Instance.OnDayEndPanelClosed()    // 日终面板关闭回调
DayManager.Instance.OnDeclarationChoice(DayAction) // 声明选择回调
DayManager.Instance.ResetGame()              // 重置并重新开始
```

### 事件

| 事件 | 类型 | 说明 |
|------|------|------|
| `OnDayChanged` | `Action<int>` | 天数变化（参数：新天数 1-6） |
| `OnPhaseChanged` | `Action<GamePhase>` | 游戏阶段变化 |

### Inspector 配置

| 字段 | 说明 |
|------|------|
| `playerState` | CharacterState 引用 |
| `declarationPanel` | DeclarationPanel 引用 |
| `dayEndPanel` | DayEndPanel 引用 |
| `gameOverPanel` | GameOverPanel 引用 |
| `nextDayButton` | 下一天按钮 Button |
| `nextDayButtonText` | 按钮文本 Text |

---

## DeclarationPanel

**路径**：`Assets/Script/DaySystem/DeclarationPanel.cs`  
**命名空间**：`DaySystem`

全屏遮罩 + 两个按钮（去商店 / 去钓鱼）。

### API

```csharp
declarationPanel.Show(int currentDay)   // 显示面板
declarationPanel.Hide()                 // 隐藏面板
```

### Inspector 配置

| 字段 | 说明 |
|------|------|
| `panelRoot` | 面板根节点 |
| `shopButton` | "去商店"按钮 |
| `fishingButton` | "去钓鱼"按钮 |
| `dayInfoText` | 显示当天星期信息的 Text |

---

## DayEndPanel

**路径**：`Assets/Script/DaySystem/DayEndPanel.cs`  
**命名空间**：`DaySystem`

日终结算面板，D1-D5 结束时弹出。当前仅包含关闭按钮，内容区预留。

### API

```csharp
dayEndPanel.Show(int currentDay)   // 显示面板
dayEndPanel.Hide()                 // 隐藏面板
```

### Inspector 配置

| 字段 | 说明 |
|------|------|
| `panelRoot` | 面板根节点 |
| `closeButton` | 关闭按钮 |
| `dayInfoText` | 信息文本（预留） |

---

## GameOverPanel

**路径**：`Assets/Script/DaySystem/GameOverPanel.cs`  
**命名空间**：`DaySystem`

游戏结束面板，D6 结束后显示，仅一个重新开始按钮。

### API

```csharp
gameOverPanel.Show()   // 显示面板
gameOverPanel.Hide()   // 隐藏面板
```

### Inspector 配置

| 字段 | 说明 |
|------|------|
| `panelRoot` | 面板根节点 |
| `restartButton` | 重新开始按钮 |

---

## DayDisplayUI

**路径**：`Assets/Script/DaySystem/DayDisplayUI.cs`  
**命名空间**：`DaySystem`

订阅 `DayManager.OnDayChanged` 事件，在 Text 上显示当前"星期X"。

### Inspector 配置

| 字段 | 说明 |
|------|------|
| `dayText` | 显示天数的 Text 组件 |
| `displayFormat` | 格式字符串，默认 `"{0}"`，`{0}` 替换为星期名 |

---

## 游戏重置流程

`DayManager.ResetGame()` 执行顺序：

```
1. ItemPool.Instance.ReshuffleAll()        // 重新洗牌所有卡池
2. GameManager.Instance.ResetAll()         // 重置疯狂值/空牌统计
3. HandManager.Instance.ClearHand()        // 清空手牌
4. ShopManager.Instance.ResetPools()       // 重置商店牌序和悬挂槽
5. playerState.ResetState()                // 重置角色属性
6. SceneManager.LoadScene(当前场景)        // 重载场景
7. DayManager.StartGame()                  // 场景加载后重新开始D1
```

### 各系统新增的重置方法

| 方法 | 所在脚本 | 重置内容 |
|------|---------|---------|
| `GameManager.ResetAll()` | `GameManager.cs` | 疯狂值归零、空牌统计归零 |
| `CharacterState.ResetState()` | `CharacterState.cs` | 最大体力恢复初始值、当前体力满值、金币归零、深度Depth1 |
| `ShopManager.ResetPools()` | `ShopManager.cs` | 消耗品/装备牌序清空、悬挂槽清空、标记未初始化 |
| `ItemPool.ReshuffleAll()` | `ItemPool.cs` | 重新加载并洗牌所有卡池 |

---

## 每日不重置的内容

- 手牌列表（跨天保持）
- 金币（不归零）
- 最大体力（不重置回初始值）
- 牌堆卡序（不重新洗牌，仅移除FaceUp顶牌）
- 商店牌序（继续顺序抽取）
- 商店悬挂槽
- 疯狂值/疯狂等级
- 装备栏

---

## Unity Editor 配置（脚本编译后）

### 最小配置

1. 新建 `DayManager` GameObject（建议挂在 Managers 父节点），添加 `DayManager.cs`
2. 配置 `playerState`（拖入玩家对象的 CharacterState）
3. Canvas 下创建以下面板，分别挂载对应脚本：
   - `DeclarationPanel`：panelRoot + 两个按钮 + dayInfoText
   - `DayEndPanel`：panelRoot + closeButton + dayInfoText
   - `GameOverPanel`：panelRoot + restartButton
4. 创建"下一天"按钮，将 `nextDayButton` 和 `nextDayButtonText` 引用拖入 DayManager
5. （可选）创建 DayDisplayUI，挂载到 Text 组件所在对象

### 商店系统变更

- ShopPanel 的关闭按钮已禁用（`closeButton.gameObject.SetActive(false)`）
- 商店仅通过 DeclarationPanel 选择"去商店"打开
- 商店通过"下一天"按钮退出（DayManager 调用 `ShopPanel.ClosePanel()`）
