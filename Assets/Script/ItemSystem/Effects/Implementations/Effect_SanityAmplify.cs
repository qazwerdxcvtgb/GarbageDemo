using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 手牌中持续效果：每次疯狂值增加时额外 +1。
    /// 通过 EffectBus 引用计数实现，多张卡叠加。
    /// </summary>
    [System.Serializable]
    public class Effect_SanityAmplify : EffectBase
    {
        public override string DisplayName => "疯狂值增幅";

        public override void Activate(EffectContext context)
        {
            EffectBus.Instance.RegisterSanityAmplify();
        }

        public override void Deactivate(EffectContext context)
        {
            EffectBus.Instance.UnregisterSanityAmplify();
        }

        public override void Execute(EffectContext context)
        {
            Debug.LogWarning("[Effect_SanityAmplify] WhileInHand 效果不应通过 Execute 调用");
        }

        public override string GetDescription() => "手牌中：疯狂值增加时额外+1";
    }
}
