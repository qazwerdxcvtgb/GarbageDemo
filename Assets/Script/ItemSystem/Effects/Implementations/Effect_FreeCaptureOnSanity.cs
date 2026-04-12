using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 当玩家疯狂等级恰好等于配置等级时，该鱼卡捕获免费（费用为 0）。
    /// 条件始终在鱼卡数据上，由 EffectBus.ProcessFishingCost 直接检查，不通过 Execute 触发。
    /// </summary>
    [System.Serializable]
    public class Effect_FreeCaptureOnSanity : EffectBase
    {
        [Header("条件配置")]
        [Tooltip("触发免费捕获的疯狂等级（精确匹配）")]
        public SanityLevel requiredLevel = SanityLevel.Level2;

        public override string DisplayName => "疯狂等级免费捕获";

        public override void Execute(EffectContext context)
        {
            EffectBus.Instance.RegisterSanityFreeCaptureSource();
            Debug.Log($"[Effect_FreeCaptureOnSanity] 疯狂等级桥接已注册，requiredLevel={requiredLevel}");
        }

        /// <summary>
        /// 检查当前疯狂等级是否满足免费捕获条件。
        /// 由 EffectBus.ProcessFishingCost 在每次费用计算时直接调用。
        /// </summary>
        public bool CheckSanityCondition()
        {
            if (GameManager.Instance == null) return false;
            return GameManager.Instance.CurrentSanityLevel == requiredLevel;
        }

        public override string GetDescription()
        {
            return $"疯狂等级为{(int)requiredLevel}时免费捕获";
        }
    }
}
