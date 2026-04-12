using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 揭示后自动移除效果。
    /// 挂载在 FishData 上，trigger = OnReveal。
    /// 揭示时设置 EffectBus 信号，CardPilePanel 检测后移除顶牌并禁用捕获。
    /// </summary>
    [System.Serializable]
    public class Effect_RemoveOnReveal : EffectBase
    {
        public override string DisplayName => "揭示后移除";

        public override void Execute(EffectContext context)
        {
            EffectBus.Instance.SetPendingRemoveOnReveal();
            Debug.Log("[Effect_RemoveOnReveal] 此卡揭示后将被自动移除，无法捕获");
        }

        public override string GetDescription() => "揭示后无法捕获，自动从牌堆移除";
    }
}
