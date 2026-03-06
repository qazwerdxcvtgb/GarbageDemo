# 工具类

> [← 返回索引](INDEX.md)  
> 覆盖脚本：`Utils/FishPriceCalculator.cs`

---

## FishPriceCalculator

**路径**：`Assets/Script/Utils/FishPriceCalculator.cs`  
**类型**：静态工具类，无需实例化

### 功能

根据当前**世界疯狂等级**（`SanityLevel`）和鱼的**类型**（`FishType`）在基础价值上做价格调整。

### API

```csharp
int price = FishPriceCalculator.CalculatePrice(FishData fish, SanityLevel sanityLevel);
// 返回值：最终售价（≥0）
```

### 价格调整表

| 疯狂等级 | Pure（纯净） | Corrupted（污秽） |
|---------|------------|-----------------|
| Level0（0） | +2 | -2 |
| Level1（1-3） | +1 | -1 |
| Level2（4-6） | +1 | 0 |
| Level3（7-9） | 0 | +1 |
| Level4（10-12） | -1 | +1 |
| Level5（13+） | -2 | +2 |

### 典型用法

```csharp
// FishShopPanel 中计算售价
SanityLevel level = GameManager.Instance.CurrentSanityLevel;
int price = FishPriceCalculator.CalculatePrice(fishData, level);
```
