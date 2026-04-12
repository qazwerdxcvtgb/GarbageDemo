using System;
using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 手牌中持续效果：捕获污秽鱼时体力消耗减少。
    /// 通过 EffectBus.OnModifyFishingCost 修改链实现，多张卡和装备效果天然叠加。
    /// </summary>
    [System.Serializable]
    public class Effect_CorruptedFishDiscount : EffectBase
    {
        [Tooltip("减少的体力消耗点数")]
        public int reduction = 1;

        [NonSerialized]
        private Func<int, FishData, int> registeredModifier;

        public override string DisplayName => "污秽鱼体力折扣";

        public override void Activate(EffectContext context)
        {
            registeredModifier = (cost, fish) =>
            {
                if (fish != null && fish.fishType == FishType.Corrupted)
                    return cost - reduction;
                return cost;
            };
            EffectBus.Instance.OnModifyFishingCost += registeredModifier;
            EffectBus.Instance.NotifyFishingModifierChanged();
        }

        public override void Deactivate(EffectContext context)
        {
            if (registeredModifier != null)
            {
                EffectBus.Instance.OnModifyFishingCost -= registeredModifier;
                registeredModifier = null;
                EffectBus.Instance.NotifyFishingModifierChanged();
            }
        }

        public override void Execute(EffectContext context)
        {
            Debug.LogWarning("[Effect_CorruptedFishDiscount] WhileInHand 效果不应通过 Execute 调用");
        }

        public override string GetDescription() => $"手牌中：捕获污秽鱼体力消耗-{reduction}";
    }
}
