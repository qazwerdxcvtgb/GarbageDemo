using System.Linq;
using UnityEngine;
using HandSystem;

namespace ItemSystem
{
    /// <summary>
    /// 根据手牌中污秽鱼数量增加疯狂值。
    /// 自我排除：OnReveal/OnCapture 时此卡在牌堆中天然不在手牌；
    /// OnUse 时此卡仍在手牌中，自动减 1 排除自身。
    /// </summary>
    [System.Serializable]
    public class Effect_CorruptedFishSanity : EffectBase
    {
        public override string DisplayName => "污秽鱼计数增疯狂";

        public override void Execute(EffectContext context)
        {
            if (HandManager.Instance == null || GameManager.Instance == null) return;

            int count = HandManager.Instance.GetHandCards()
                .OfType<FishData>()
                .Count(f => f.fishType == FishType.Corrupted);

            if (trigger == EffectTrigger.OnUse)
                count--;

            if (count <= 0)
            {
                Debug.Log("[Effect_CorruptedFishSanity] 手牌中无污秽鱼（或已排除自身），跳过");
                return;
            }

            GameManager.Instance.ModifySanity(count);
            Debug.Log($"[Effect_CorruptedFishSanity] 手牌污秽鱼 {count} 条，疯狂值 +{count}");
        }

        public override string GetDescription() => "根据手牌中污秽鱼数量增加疯狂值";
    }
}
