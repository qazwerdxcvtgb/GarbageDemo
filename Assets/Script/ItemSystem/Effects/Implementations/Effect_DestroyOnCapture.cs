using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 捕获时触发：其他捕获效果正常结算，但该鱼卡不加入手牌，直接销毁。
    /// 通过 EffectBus 一次性标记通知 FishingTableManager.TryCapture 跳过 AddCard。
    /// </summary>
    [System.Serializable]
    public class Effect_DestroyOnCapture : EffectBase
    {
        public override string DisplayName => "捕获后销毁";

        public override void Execute(EffectContext context)
        {
            EffectBus.Instance.SetPendingDestroyOnCapture();
            Debug.Log("[Effect_DestroyOnCapture] 此卡捕获后将不加入手牌，直接销毁");
        }

        public override string GetDescription() => "捕获后不入手牌，直接销毁";
    }
}
