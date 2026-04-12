using UnityEngine;
using HandSystem;

namespace ItemSystem
{
    /// <summary>
    /// 揭示时触发：注册 OnHandChanged → OnFishingModifierChanged 桥接。
    /// ProcessFishingCost 通过 GetHandFishCount 实时统计手牌中鱼卡数量，增加额外体力消耗。
    /// 放弃后再次打开面板仍然生效（桥接不随面板关闭清除）。
    /// </summary>
    [System.Serializable]
    public class Effect_CostPerHandFish : EffectBase
    {
        [Tooltip("每张手牌鱼卡增加的额外体力消耗")]
        public int costPerFish = 1;

        public override string DisplayName => "手牌鱼数额外消耗";

        public override void Execute(EffectContext context)
        {
            EffectBus.Instance.RegisterHandFishCostSource();
            Debug.Log("[Effect_CostPerHandFish] 桥接已注册");
        }

        /// <summary>
        /// 统计手牌中鱼卡数量并乘以 costPerFish，返回额外消耗值。
        /// 由 EffectBus.CheckHandFishCostIncrease 在每次 ProcessFishingCost 时调用。
        /// </summary>
        public int GetHandFishCount()
        {
            if (HandManager.Instance == null) return 0;
            int count = 0;
            foreach (var card in HandManager.Instance.GetHandCards())
            {
                if (card is FishData)
                    count++;
            }
            return count * costPerFish;
        }

        public override string GetDescription()
            => $"手牌中每有一张鱼牌，捕获体力消耗+{costPerFish}";
    }
}
