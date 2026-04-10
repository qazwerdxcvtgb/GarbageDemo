using UnityEngine;
using FishingSystem;

namespace ItemSystem
{
    /// <summary>
    /// 偷看牌堆效果：触发后由玩家选择一个牌堆，偷看顶部 N 张未揭示的牌。
    /// N 不包括已揭示的顶牌，不受深度限制。
    /// 偷看期间手牌栏收起锁定、装备栏关闭锁定。
    /// </summary>
    [System.Serializable]
    public class Effect_PeekPile : EffectBase
    {
        [Tooltip("偷看的牌数（不含已揭示的牌）")]
        [Min(1)]
        public int peekCount = 2;

        public override string DisplayName => "偷看牌堆";

        public override void Execute(EffectContext context)
        {
            var handler = PeekPileHandler.Instance;
            if (handler == null)
            {
                Debug.LogError("[Effect_PeekPile] PeekPileHandler 不存在，无法执行偷看");
                return;
            }

            handler.StartPeek(peekCount, PeekPileHandler.PeekMode.Single);
        }

        public override (bool canUse, string reason) CanExecute(EffectContext context)
        {
            if (FishingTableManager.Instance == null)
                return (false, "不在钓鱼场景中");

            var piles = FishingTableManager.Instance.GetAllPiles();
            bool hasUnrevealed = piles.Exists(p => p.UnrevealedCardCount > 0);
            if (!hasUnrevealed)
                return (false, "没有可偷看的牌堆");

            var handler = PeekPileHandler.Instance;
            if (handler == null)
                return (false, "偷看功能不可用");

            if (handler.IsPeeking)
                return (false, "正在偷看中");

            return (true, null);
        }

        public override string GetDescription()
            => $"偷看牌堆顶部 {peekCount} 张未揭示的牌";
    }
}
