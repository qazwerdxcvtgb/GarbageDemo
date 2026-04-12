using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 标记型效果：携带此效果的鱼卡售价不受疯狂等级修正影响。
    /// ShopSellController.GetAdjustedCardValue 通过 is Effect_StablePrice 检测跳过修正。
    /// </summary>
    [System.Serializable]
    public class Effect_StablePrice : EffectBase
    {
        public override string DisplayName => "稳定售价";

        public override void Execute(EffectContext context) { }

        public override string GetDescription() => "售价不受疯狂等级影响";
    }
}
