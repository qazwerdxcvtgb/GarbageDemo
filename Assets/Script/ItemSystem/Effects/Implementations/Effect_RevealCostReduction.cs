using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 揭示时降低本鱼卡的捕获体力消耗。
    /// 通过 EffectBus.SetRevealCostReduction 设置临时折扣，
    /// CardPilePanel 关闭时自动清除。放弃捕获后再次打开不触发 OnReveal，折扣不再生效。
    /// </summary>
    [System.Serializable]
    public class Effect_RevealCostReduction : EffectBase
    {
        [Tooltip("体力消耗降低量")]
        public int amount = 1;

        public override string DisplayName => "揭示降低体力";

        public override void Execute(EffectContext context)
        {
            EffectBus.Instance.SetRevealCostReduction(amount);
            Debug.Log($"[Effect_RevealCostReduction] 揭示体力折扣 -{amount}");
        }

        public override string GetDescription()
        {
            return $"揭示时体力消耗-{amount}";
        }
    }
}
