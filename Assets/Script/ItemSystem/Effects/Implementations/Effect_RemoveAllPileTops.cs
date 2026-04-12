using UnityEngine;
using FishingSystem;

namespace ItemSystem
{
    /// <summary>
    /// 移除所有牌堆顶部的卡牌，无论 FaceDown 还是 FaceUp。
    /// 空牌堆自动跳过。RemoveTopCard 内部负责状态转换和视觉刷新。
    /// </summary>
    [System.Serializable]
    public class Effect_RemoveAllPileTops : EffectBase
    {
        public override string DisplayName => "移除所有牌堆顶牌";

        public override void Execute(EffectContext context)
        {
            if (FishingTableManager.Instance == null)
            {
                Debug.LogWarning("[Effect_RemoveAllPileTops] FishingTableManager 不存在，跳过");
                return;
            }

            var piles = FishingTableManager.Instance.GetAllPiles();
            int removedCount = 0;

            foreach (var pile in piles)
            {
                if (pile.CardCount == 0) continue;

                FishData removed = pile.RemoveTopCard();
                if (removed != null)
                {
                    removedCount++;
                    Debug.Log($"[Effect_RemoveAllPileTops] 移除牌堆顶牌：{removed.itemName}（状态：{pile.State}）");
                }
            }

            Debug.Log($"[Effect_RemoveAllPileTops] 共移除 {removedCount} 张顶牌");
        }

        public override string GetDescription() => "移除所有牌堆顶部的卡牌";
    }
}
