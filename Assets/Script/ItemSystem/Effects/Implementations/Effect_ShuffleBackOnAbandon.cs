using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 揭示时触发：若玩家放弃捕获，鱼卡从牌堆顶部移除并随机插回牌堆（面朝下），
    /// 而非留在顶部保持 FaceUp。通过 EffectBus 标记-消费模式实现。
    /// </summary>
    [System.Serializable]
    public class Effect_ShuffleBackOnAbandon : EffectBase
    {
        public override string DisplayName => "放弃后洗回";

        public override void Execute(EffectContext context)
        {
            EffectBus.Instance.SetPendingShuffleBackOnAbandon();
        }

        public override string GetDescription() => "放弃捕获时洗回牌堆随机位置";
    }
}
