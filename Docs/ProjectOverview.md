# 项目总览

## 基本信息

- **项目名称**：GarbageDemo
- **类型**：Unity 2D 钓鱼卡牌游戏 Demo
- **引擎**：Unity（URP 2D）
- **语言**：C#
- **依赖库**：DOTween（动画）、TextMeshPro（文本渲染）

## 核心玩法

玩家在有限天数内通过**钓鱼**获取卡牌（鱼、垃圾、消耗品、装备），在**商店**出售鱼类换取金币、购买消耗品和装备，管理**体力**和**疯狂值**两项核心资源，争取在 6 天内存活。

玩法循环：**宣言 → 钓鱼 → 商店 → 结算 → 下一天**

## 场景

当前仅有 `Assets/Scenes/UI.unity` 单场景，包含所有 UI 面板和游戏逻辑。

## 系统架构

| 系统 | 职责 | 关键单例 |
|------|------|----------|
| Core | 全局状态（体力/金币/疯狂值）、事件定义 | `GameManager.Instance` |
| DaySystem | 天数循环、阶段切换、结算面板 | `DayManager.Instance` |
| FishCardSystem | 卡牌实体（拖拽、视觉、槽位管理） | `CrossHolderSystem.Instance` |
| FishingSystem | 钓鱼牌堆、抽卡、捕获/放弃逻辑 | `FishingTableManager.Instance` |
| HandSystem | 手牌数据管理（增删查） | `HandManager.Instance` |
| ItemSystem | 物品数据定义、效果系统、物品池 | `ItemPool.Instance`, `EquipmentManager.Instance` |
| ShopSystem | 商店买卖、悬挂鱼、装备面板 | `ShopManager.Instance` |
| UI | HUD 状态显示（体力/金币/疯狂值/深度） | 无单例 |

## 数据流

```
ScriptableObject 数据（Resources/Items/）
    ↓ Resources.LoadAll（由 ItemPool 加载）
运行时 ItemData 实例
    ↓ HandManager 管理手牌列表
    ↓ HandPanelUI 创建卡牌实体（Instantiate）
FishCard / TrashCard / ConsumableCard / EquipmentCard（场景中的 GameObject）
    ↓ FishCardVisual + FrontDisplay 渲染卡面
UI 展示与交互
```

## 脚本目录结构

```
Assets/Script/
├── Core/           (3)  GameManager, CharacterState, GameEvents
├── DaySystem/      (5)  天数管理与结算面板
├── FishCardSystem/ (18) 卡牌核心（Core/Data/Manager/Utility/Visual）
├── FishingSystem/  (4)  钓鱼牌堆与面板
├── HandSystem/     (1)  HandManager
├── ItemSystem/     (22) 数据定义/效果/管理器（Data/Editor/Effects/Managers）
├── ShopSystem/     (9)  商店控制器与装备面板
├── UI/             (4)  HUD 状态显示
├── Test/           (1)  CardSystemTester
└── Abandoned/      (16) 废弃脚本（已标注 [Obsolete]）
```

## 文档入口

详细文档见 [INDEX.md](INDEX.md)，文档规范见 [DocStandards.md](DocStandards.md)。
