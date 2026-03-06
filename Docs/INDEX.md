# 2DDemo 开发文档索引

> 版本：2.0 | 更新：2026-02-25  
> **使用说明**：先读本文件定位目标，再按链接跳转对应文档，每次只读需要的部分。

---

## 快速检索表

| 我想了解… | 读这个文档 | 定位章节 |
|-----------|-----------|---------|
| 全局疯狂值 / 空牌统计 | [CoreSystem](CoreSystem.md) | GameManager |
| 玩家体力 / 金币 | [CoreSystem](CoreSystem.md) | CharacterState |
| 全局事件类型定义 | [CoreSystem](CoreSystem.md) | GameEvents |
| 玩家移动 / 输入 | [PlayerSystem](PlayerSystem.md) | PlayerMove |
| 场景交互点 | [PlayerSystem](PlayerSystem.md) | InteractionPoint |
| 物品数据结构（鱼/装备/消耗品） | [ItemSystem](ItemSystem.md) | 数据类 |
| 物品效果系统（EffectBase 等） | [ItemSystem](ItemSystem.md) | 效果系统 |
| 物品池（抽取鱼/物品） | [ItemSystem](ItemSystem.md) | 管理器 |
| 手牌数据管理 | [HandSystem](HandSystem.md) | HandManager |
| 鱼类卡牌脚本 API | [FishCardSystem](FishCardSystem.md) | 核心 API |
| 鱼类卡牌 Unity 配置步骤 | [FishCardSystem](FishCardSystem.md) | Unity配置摘要 |
| 钓鱼牌桌（9 槽位） | [FishingSystem](FishingSystem.md) | FishingTablePanel |
| 手牌 UI 面板 | [UISystem](UISystem.md) | HandUIPanel |
| 鱼店 UI 面板 | [UISystem](UISystem.md) | FishShopPanel |
| 体力 / 金币 / 疯狂显示 | [UISystem](UISystem.md) | 状态显示UI |
| 鱼类售价计算 | [Utils](Utils.md) | FishPriceCalculator |

---

## 系统一览

| 文档 | 覆盖系统 | 主要单例 |
|------|---------|---------|
| [CoreSystem](CoreSystem.md) | 全局管理、角色状态、事件定义 | `GameManager.Instance` |
| [PlayerSystem](PlayerSystem.md) | 玩家移动、交互点 | — |
| [ItemSystem](ItemSystem.md) | 物品数据、效果系统、物品池、装备 | `ItemPool.Instance` |
| [HandSystem](HandSystem.md) | 手牌数据管理 | `HandManager.Instance` |
| [FishCardSystem](FishCardSystem.md) | 鱼类卡牌视觉/交互/配置 | — |
| [FishingSystem](FishingSystem.md) | 钓鱼牌桌、槽位、翻牌 | `FishingTablePanel.Instance` |
| [UISystem](UISystem.md) | 所有 UI 面板与控件 | `HandUIPanel.Instance` · `FishShopPanel.Instance` |
| [Utils](Utils.md) | 工具类 | — |

---

## 单例速查

```csharp
GameManager.Instance          // 全局：疯狂值、交互事件
HandManager.Instance          // 手牌数据
ItemPool.Instance             // 物品池抽取
FishingTablePanel.Instance    // 钓鱼牌桌
HandUIPanel.Instance          // 手牌UI（懒加载）
FishShopPanel.Instance        // 鱼店UI（懒加载）
CharacterState                // 挂载在玩家对象上，非全局单例
```

---

## 系统依赖关系

```
GameManager ←── ItemSystem.Effects（ModifySanity / ModifyHealth）
CharacterState ←── ItemSystem.Effects（ModifyHealth）

ItemPool ──→ FishingTablePanel（按深度+池索引抽牌）
FishingTablePanel ──→ HandManager（捕获后 AddCard）
HandManager ──→ HandUIPanel（OnHandChanged 刷新显示）
FishData ──→ FishCardSystem（视觉展示）
FishShopPanel ──→ HandManager + CharacterState（售鱼 → 金币）
FishPriceCalculator ←── GameManager.CurrentSanityLevel
```

---

## 脚本目录结构

```
Assets/Script/
├── GameManager.cs
├── CharacterState.cs
├── GameEvents.cs
├── PlayerMove.cs
├── InputSystem.cs              （Unity自动生成，勿手动修改）
├── InteractionPoint.cs
├── InteractionPoint_1.cs
├── InteractionPoint_FishShop.cs
├── ItemSystem/
│   ├── Data/         ItemData(基类) FishData ConsumableData TrashData EquipmentData ItemEnums
│   ├── Effects/      EffectBase EffectTrigger EffectContext InstantEffect PassiveEffect + Implementations/
│   └── Managers/     ItemPool EquipmentManager
├── HandSystem/
│   └── HandManager.cs
├── FishCardSystem/
│   ├── Core/         FishCard CardFaceController
│   ├── Visual/       FishCardVisual FishCardFrontDisplay FishCardBackDisplay
│   ├── Manager/      FishCardHolder VisualCardsHandler
│   ├── Data/         CurveParameters
│   └── Utility/      ExtensionMethods
├── FishingSystem/
│   ├── FishingTablePanel.cs
│   ├── CardPileSlot.cs
│   └── RevealOverlayPanel.cs
├── UI/               HandUIPanel FishShopPanel HealthDisplayUI GoldDisplayUI SanityDisplayUI 等
└── Utils/
    └── FishPriceCalculator.cs
```
