/// <summary>
/// 鱼类售价计算工具类
/// 创建日期：2026-01-21
/// 功能：根据世界疯狂等级和鱼类类型计算售价
/// </summary>

using System.Collections.Generic;
using UnityEngine;
using ItemSystem;

/// <summary>
/// 鱼类售价计算静态工具类
/// </summary>
public static class FishPriceCalculator
{
    /// <summary>
    /// 价格调整表：(疯狂等级, 鱼类类型) -> 调整值
    /// </summary>
    private static readonly Dictionary<(SanityLevel, FishType), int> priceAdjustmentTable = new Dictionary<(SanityLevel, FishType), int>
    {
        // 等级0：疯狂值为0
        { (SanityLevel.Level0, FishType.Pure), +2 },
        { (SanityLevel.Level0, FishType.Corrupted), -2 },
        
        // 等级1：疯狂值1-3
        { (SanityLevel.Level1, FishType.Pure), +1 },
        { (SanityLevel.Level1, FishType.Corrupted), -1 },
        
        // 等级2：疯狂值4-6
        { (SanityLevel.Level2, FishType.Pure), +1 },
        { (SanityLevel.Level2, FishType.Corrupted), 0 },
        
        // 等级3：疯狂值7-9
        { (SanityLevel.Level3, FishType.Pure), 0 },
        { (SanityLevel.Level3, FishType.Corrupted), +1 },
        
        // 等级4：疯狂值10-12
        { (SanityLevel.Level4, FishType.Pure), -1 },
        { (SanityLevel.Level4, FishType.Corrupted), +1 },
        
        // 等级5：疯狂值13+
        { (SanityLevel.Level5, FishType.Pure), -2 },
        { (SanityLevel.Level5, FishType.Corrupted), +2 }
    };

    /// <summary>
    /// 计算鱼类卡牌的售价
    /// </summary>
    /// <param name="fish">鱼类数据</param>
    /// <returns>售价（≥0）</returns>
    public static int CalculatePrice(FishData fish)
    {
        if (fish == null)
        {
            Debug.LogError("[FishPriceCalculator] 鱼类数据为空");
            return 0;
        }

        // 1. 获取原始价格
        int basePrice = fish.value;

        // 2. 获取当前疯狂等级
        if (GameManager.Instance == null)
        {
            Debug.LogError("[FishPriceCalculator] GameManager.Instance为空，使用原始价格");
            return Mathf.Max(0, basePrice);
        }

        SanityLevel currentLevel = GameManager.Instance.GetSanityLevel();

        // 3. 根据等级和卡牌类型获取调整值
        int adjustment = GetPriceAdjustment(currentLevel, fish.fishType);

        // 4. 计算最终售价（最低为0）
        int finalPrice = Mathf.Max(0, basePrice + adjustment);

        Debug.Log($"[FishPriceCalculator] {fish.itemName} | 原价:{basePrice} | 等级:{currentLevel} | 类型:{fish.fishType.ToChineseText()} | 调整:{adjustment:+#;-#;0} | 售价:{finalPrice}");

        return finalPrice;
    }

    /// <summary>
    /// 获取售价调整值
    /// </summary>
    /// <param name="level">疯狂等级</param>
    /// <param name="fishType">鱼类类型</param>
    /// <returns>调整值（可为负数）</returns>
    private static int GetPriceAdjustment(SanityLevel level, FishType fishType)
    {
        // 从调整表中查找
        if (priceAdjustmentTable.TryGetValue((level, fishType), out int adjustment))
        {
            return adjustment;
        }

        // 如果未找到（理论上不应该发生）
        Debug.LogWarning($"[FishPriceCalculator] 未找到调整值: 等级={level}, 类型={fishType}");
        return 0;
    }

    /// <summary>
    /// 获取售价调整值（不记录日志，用于UI显示）
    /// </summary>
    /// <param name="fish">鱼类数据</param>
    /// <returns>调整值</returns>
    public static int GetAdjustment(FishData fish)
    {
        if (fish == null || GameManager.Instance == null)
        {
            return 0;
        }

        SanityLevel currentLevel = GameManager.Instance.GetSanityLevel();
        return GetPriceAdjustment(currentLevel, fish.fishType);
    }
}
