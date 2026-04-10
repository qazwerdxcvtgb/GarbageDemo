using System;
using UnityEngine;
using DaySystem;

namespace ItemSystem
{
    /// <summary>
    /// 条件筛选维度
    /// </summary>
    public enum FishingDiscountCondition
    {
        All,            // 所有鱼
        ByFishType,     // 按鱼类类型（纯净/污秽）
        ByFishSize,     // 按鱼类体积（小/中/大）
        FirstFishOfDay  // 每天第一条鱼
    }

    /// <summary>
    /// 被动效果：条件钓鱼体力折扣（统一可配置）
    /// 通过 Inspector 选择条件维度和参数，覆盖以下全部场景：
    ///   All            → 所有鱼体力 -N
    ///   ByFishType     → 指定类型（纯净/污秽）体力 -N
    ///   ByFishSize     → 指定体积（小/中/大）体力 -N
    ///   FirstFishOfDay → 每天第一条鱼体力 -N
    /// </summary>
    [CreateAssetMenu(fileName = "Effect_ConditionalFishingDiscount", menuName = "ItemSystem/Effects/ConditionalFishingDiscount")]
    public class Effect_ConditionalFishingDiscount : PassiveEffect
    {
        [Header("条件配置")]
        [Tooltip("折扣生效的条件维度")]
        public FishingDiscountCondition condition = FishingDiscountCondition.All;

        [Header("按类型筛选（仅 ByFishType 时生效）")]
        [Tooltip("目标鱼类类型")]
        public FishType targetFishType;

        [Header("按体积筛选（仅 ByFishSize 时生效）")]
        [Tooltip("目标鱼类体积")]
        public FishSize targetFishSize;

        [Header("折扣配置")]
        [Tooltip("满足条件时减少的体力消耗点数")]
        [Min(0)]
        public int reduction = 1;

        private Func<int, FishData, int> registeredModifier;
        private int capturedCountToday;
        private Action<int> dayChangedHandler;
        private Action fishCapturedHandler;

        public override void Register()
        {
            if (registeredModifier != null)
            {
                Debug.LogWarning($"[ConditionalFishingDiscount] {effectName} 已注册，请勿重复注册");
                return;
            }

            registeredModifier = (cost, fishData) =>
            {
                if (ShouldApply(fishData))
                    return cost - reduction;
                return cost;
            };
            EffectBus.Instance.OnModifyFishingCost += registeredModifier;

            if (condition == FishingDiscountCondition.FirstFishOfDay)
            {
                capturedCountToday = 0;

                dayChangedHandler = _ => capturedCountToday = 0;
                if (DayManager.Instance != null)
                    DayManager.Instance.OnDayChanged += dayChangedHandler;

                fishCapturedHandler = () =>
                {
                    capturedCountToday++;
                    EffectBus.Instance.NotifyFishingModifierChanged();
                };
                EffectBus.Instance.OnFishCaptured += fishCapturedHandler;
            }

            Debug.Log($"[ConditionalFishingDiscount] 注册：{effectName}，条件={condition}，折扣=-{reduction}");
            EffectBus.Instance.NotifyFishingModifierChanged();
        }

        public override void Unregister()
        {
            if (registeredModifier == null)
            {
                Debug.LogWarning($"[ConditionalFishingDiscount] {effectName} 尚未注册，无法注销");
                return;
            }

            EffectBus.Instance.OnModifyFishingCost -= registeredModifier;
            registeredModifier = null;

            if (condition == FishingDiscountCondition.FirstFishOfDay)
            {
                if (DayManager.Instance != null && dayChangedHandler != null)
                    DayManager.Instance.OnDayChanged -= dayChangedHandler;
                dayChangedHandler = null;

                if (EffectBus.Instance != null && fishCapturedHandler != null)
                    EffectBus.Instance.OnFishCaptured -= fishCapturedHandler;
                fishCapturedHandler = null;

                capturedCountToday = 0;
            }

            Debug.Log($"[ConditionalFishingDiscount] 注销：{effectName}");
            EffectBus.Instance.NotifyFishingModifierChanged();
        }

        private bool ShouldApply(FishData fishData)
        {
            switch (condition)
            {
                case FishingDiscountCondition.All:
                    return true;

                case FishingDiscountCondition.ByFishType:
                    return fishData != null && fishData.fishType == targetFishType;

                case FishingDiscountCondition.ByFishSize:
                    return fishData != null && fishData.size == targetFishSize;

                case FishingDiscountCondition.FirstFishOfDay:
                    return capturedCountToday == 0;

                default:
                    return false;
            }
        }

        public override string GetEffectInfo()
        {
            switch (condition)
            {
                case FishingDiscountCondition.All:
                    return $"{effectName}：所有鱼体力消耗 -{reduction}";
                case FishingDiscountCondition.ByFishType:
                    return $"{effectName}：{targetFishType.ToChineseText()}鱼体力消耗 -{reduction}";
                case FishingDiscountCondition.ByFishSize:
                    return $"{effectName}：{targetFishSize.ToChineseText()}鱼体力消耗 -{reduction}";
                case FishingDiscountCondition.FirstFishOfDay:
                    return $"{effectName}：每天第一条鱼体力消耗 -{reduction}";
                default:
                    return $"{effectName}：钓鱼体力消耗 -{reduction}";
            }
        }
    }
}
