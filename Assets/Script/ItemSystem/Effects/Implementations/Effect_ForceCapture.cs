using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 揭示时触发：如果玩家体力足以捕获，则强制捕获（隐藏放弃和取消按钮）。
    /// 体力不足时回退为正常流程。
    /// 通过 EffectBus.pendingForceCapture 标记实现，CardPilePanel 在揭示后消费标记。
    /// </summary>
    [System.Serializable]
    public class Effect_ForceCapture : EffectBase
    {
        public override string DisplayName => "强制捕获";

        public override void Execute(EffectContext context)
        {
            EffectBus.Instance.SetPendingForceCapture();
            Debug.Log("[Effect_ForceCapture] 强制捕获标记已设置");
        }

        public override string GetDescription()
        {
            return "揭示后若体力足够则必须捕获";
        }
    }
}
