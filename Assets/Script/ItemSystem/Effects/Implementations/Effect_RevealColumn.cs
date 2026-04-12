using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 揭示时触发：连锁揭示同列（同 poolIndex）其他牌堆。
    /// 通过 EffectBus 标记-消费模式，由 FishingTableManager 协程按队列依次翻牌
    /// 并触发过滤后的 OnReveal 效果（跳过标记型/折扣型效果和自身以防递归）。
    /// </summary>
    [System.Serializable]
    public class Effect_RevealColumn : EffectBase
    {
        public override string DisplayName => "连锁揭示本列";

        public override void Execute(EffectContext context)
        {
            EffectBus.Instance.SetPendingRevealColumn();
        }

        public override string GetDescription() => "揭示时同时揭示本列其他卡牌";
    }
}
