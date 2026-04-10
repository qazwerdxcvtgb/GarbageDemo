# 开发文档索引

> 更新：2026-04-10  
> **使用说明**：新 agent 先读 [ProjectOverview](ProjectOverview.md) 了解项目全貌，再按本索引查找具体系统文档。  
> **文档规范**：修改文档时务必遵循 [DocStandards](DocStandards.md)。

---

## 快速检索表

| 我想了解… | 读这个文档 | 定位章节 |
|-----------|-----------|---------|
| 项目整体概况 | [ProjectOverview](ProjectOverview.md) | 全文 |
| 文档编写规范 | [DocStandards](DocStandards.md) | 全文 |
| 天数循环 / 每日结算 / 重置 | [DaySystem](DaySystem.md) | DayManager |
| 声明阶段 / 游戏结束 | [DaySystem](DaySystem.md) | DeclarationPanel / GameOverPanel |
| 全局疯狂值 / 空牌统计 | [CoreSystem](CoreSystem.md) | GameManager |
| 玩家体力 / 金币 / 深度 | [CoreSystem](CoreSystem.md) | CharacterState |
| 全局事件类型定义 | [CoreSystem](CoreSystem.md) | GameEvents |
| 物品数据结构（鱼/装备/消耗品） | [ItemSystem](ItemSystem.md) | 数据类 |
| 物品效果系统（EffectBase 等） | [ItemSystem](ItemSystem.md) | 效果系统 |
| 钓鱼体力条件折扣 | [ItemSystem](ItemSystem.md) | Effect_ConditionalFishingDiscount |
| 每日装备体力补充 | [ItemSystem](ItemSystem.md) | Effect_PassiveDailyHealth |
| 捕获恢复体力 | [ItemSystem](ItemSystem.md) | Effect_PassiveOnCaptureHealth |
| 揭示时免疫鱼卡效果 | [ItemSystem](ItemSystem.md) | Effect_IgnoreRevealEffect |
| 偷看牌堆效果 | [ItemSystem](ItemSystem.md) | Effect_PeekPile |
| 偷看一行效果 | [ItemSystem](ItemSystem.md) | Effect_PeekRow |
| 偷看一列效果 | [ItemSystem](ItemSystem.md) | Effect_PeekColumn |
| 增加玩家深度效果 | [ItemSystem](ItemSystem.md) | Effect_IncreaseDepth |
| 修改金币效果 | [ItemSystem](ItemSystem.md) | Effect_ModifyGold |
| 随机选择效果（效果组合） | [ItemSystem](ItemSystem.md) | Effect_RandomChoice |
| 装备临时体力机制 | [CoreSystem](CoreSystem.md) | CharacterState |
| 物品池（抽取鱼/物品） | [ItemSystem](ItemSystem.md) | ItemPool |
| 手牌数据管理 | [HandSystem](HandSystem.md) | HandManager |
| 手牌使用按钮 / 单选 / 可用性检查 | [HandSystem](HandSystem.md) | UseCardHandler |
| 卡牌选择面板（多选/回调） | [HandSystem](HandSystem.md) | CardSelectionPanel |
| ShopManager 牌库管理（杂鱼/消耗/装备池） | [ItemSystem](ItemSystem.md) | ShopManager 牌库管理 |
| 卡牌使用场合配置（CardUseContext） | [ItemSystem](ItemSystem.md) | 枚举定义 |
| 卡牌脚本 API / 拖拽 | [FishCardSystem](FishCardSystem.md) | 核心 API |
| 卡牌 Unity 配置 | [FishCardSystem](FishCardSystem.md) | Unity 配置摘要 |
| 跨 Holder 拖拽 / 锁定 | [FishCardSystem](FishCardSystem.md) | CrossHolderSystem |
| 钓鱼牌桌（9 槽位） | [FishingSystem](FishingSystem.md) | FishingTableManager |
| 体力 / 金币 / 疯狂显示 | [UISystem](UISystem.md) | 状态显示 UI |
| 深度指示器 | [UISystem](UISystem.md) | DepthIndicatorUI |
| 商店面板（售卖/购买/悬挂） | [ShopSystem](ShopSystem.md) | 全文 |
| 装备面板 / 装备槽 | [ShopSystem](ShopSystem.md) | EquipmentPanel |

---

## 系统一览

| 文档 | 覆盖系统 | 主要单例 |
|------|---------|---------|
| [CoreSystem](CoreSystem.md) | 全局管理、角色状态、事件定义 | `GameManager.Instance` |
| [DaySystem](DaySystem.md) | 天数循环、声明选择、日终结算 | `DayManager.Instance` |
| [FishCardSystem](FishCardSystem.md) | 卡牌实体、视觉、拖拽、槽位 | `CrossHolderSystem.Instance` |
| [FishingSystem](FishingSystem.md) | 钓鱼牌桌、牌堆、翻牌交互 | `FishingTableManager.Instance` |
| [HandSystem](HandSystem.md) | 手牌数据管理 | `HandManager.Instance` |
| [ItemSystem](ItemSystem.md) | 物品数据、效果系统、物品池、装备管理 | `ItemPool.Instance`、`EquipmentManager.Instance` |
| [ShopSystem](ShopSystem.md) | 商店买卖、悬挂鱼、装备面板 | `ShopManager.Instance`、`ShopPanel.Instance` |
| [UISystem](UISystem.md) | HUD 状态显示 | — |

---

## 单例速查

```csharp
GameManager.Instance           // 全局：疯狂值、交互事件
DayManager.Instance            // 天数循环（DontDestroyOnLoad）
HandManager.Instance           // 手牌数据（DontDestroyOnLoad）
ItemPool.Instance              // 物品池抽取（DontDestroyOnLoad）
EquipmentManager.Instance      // 装备管理（DontDestroyOnLoad）
FishingTableManager.Instance   // 钓鱼牌桌
ShopManager.Instance           // 商店数据（DontDestroyOnLoad）
ShopPanel.Instance             // 商店面板 UI
CrossHolderSystem.Instance     // 跨 Holder 拖拽
EquipmentPanel.Instance        // 装备面板 UI
CharacterState                 // 挂载在玩家对象上，非全局单例
```

---

## 脚本目录结构

```
Assets/Script/
├── Core/               (3)  GameManager, CharacterState, GameEvents
├── DaySystem/          (5)  DayManager, DeclarationPanel, DayEndPanel, GameOverPanel, DayDisplayUI
├── FishCardSystem/     (21)
│   ├── Core/           (6)  ItemCard, FishCard, TrashCard, ConsumableCard, EquipmentCard, CardContextMode
│   ├── Data/           (1)  CurveParameters
│   ├── Manager/        (8)  CrossHolderSystem, FishCardHolder, HandPanelUI, ICardSlot,
│   │                         UseCardHandler, VisualCardsHandler, CardSelectionPanel, SelectionSlot
│   ├── Utility/        (1)  ExtensionMethods
│   └── Visual/         (5)  FishCardVisual, FishCardFrontDisplay, TrashCardFrontDisplay,
│                             ConsumableCardFrontDisplay, EquipmentCardFrontDisplay
├── FishingSystem/      (5)  FishingTableManager, CardPile, CardPilePanel, PileThicknessDisplay, PeekPileHandler
├── HandSystem/         (1)  HandManager
├── ItemSystem/         (25)
│   ├── Data/           (8)  ItemData, FishData, TrashData, ConsumableData, EquipmentData,
│   │                         ItemEnums, ItemEnumsExtensions, CardUseContext
│   ├── Editor/         (1)  EffectBaseDrawer
│   ├── Effects/        (6)  EffectBase, EffectBus, EffectContext, EffectData, EffectTrigger, PassiveEffect
│   │   └── Implementations/ (13) Effect_AddHealth, Effect_AddRandomHealth,
│   │                              Effect_ConditionalFishingDiscount, Effect_DrawCards,
│   │                              Effect_IgnoreCaptureEffect, Effect_IgnoreRevealEffect,
│   │                              Effect_ModifySanity, Effect_PassiveDailyHealth,
│   │                              Effect_PassiveOnCaptureHealth, Effect_PeekPile,
│   │                              Effect_PeekRow, Effect_PeekColumn,
│   │                              Effect_RandomHealthOrSanity
│   └── Managers/       (2)  ItemPool, EquipmentManager
├── ShopSystem/         (9)  ShopManager, ShopPanel, ShopSellController, ShopBuyController,
│                             ShopEquipmentController, ShopHangController, ShopHangSlot,
│                             EquipmentPanel, EquipmentSlotUI
├── UI/                 (4)  HealthDisplayUI, GoldDisplayUI, SanityDisplayUI, DepthIndicatorUI
├── Test/               (1)  CardSystemTester（从 Resources 加载物品数据的测试工具）
└── Abandoned/          (16) 废弃脚本（已标注 [Obsolete]，不再使用）
```

---

## 系统依赖关系

```
DayManager ──→ CharacterState（体力恢复、深度回退）
DayManager ──→ FishingTableManager（丢弃揭示牌）
DayManager ──→ ShopPanel（声明阶段开商店）
DayManager ──→ ItemPool / HandManager / GameManager / ShopManager（重置）

GameManager ←── ItemSystem.Effects（ModifySanity）
CharacterState ←── ItemSystem.Effects（ModifyHealth）

ItemPool ──→ FishingTableManager（按深度抽牌）
ItemPool ──→ ShopManager（消耗品/装备/杂鱼牌序）
FishingTableManager ──→ HandManager（捕获后 AddCard）
FishingTableManager ──→ ShopManager（放弃时 DrawTrash）
HandManager ──→ HandPanelUI（OnHandChanged 刷新）

ShopPanel ──→ ShopSellController + ShopBuyController + ShopHangController
ShopSellController ──→ HandManager + CharacterState
ShopHangController ──→ ShopManager + HandManager + CrossHolderSystem
EquipmentPanel ──→ EquipmentManager + CrossHolderSystem

FishCardSystem ──→ ItemSystem（数据类型）
UISystem ──→ Core（事件订阅）
```
