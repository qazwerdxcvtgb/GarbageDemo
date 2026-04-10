using UnityEngine;
using FishingSystem;

namespace ItemSystem
{
    /// <summary>
    /// 全局偷看效果：使用后直接在每个牌堆上方叠加浮层，展示各堆第一张未揭示的牌。
    /// 已揭示（FaceUp）的牌堆显示其下一张未揭示牌；空堆或全部已揭示的堆不显示浮层。
    /// 偷看期间手牌栏收起锁定、装备栏关闭锁定、牌堆点击被拦截。
    /// </summary>
    [System.Serializable]
    public class Effect_PeekAllPiles : EffectBase
    {
        public override string DisplayName => "偷看全部牌堆";

        public override void Execute(EffectContext context)
        {
            var handler = PeekPileHandler.Instance;
            if (handler == null)
            {
                Debug.LogError("[Effect_PeekAllPiles] PeekPileHandler 不存在，无法执行偷看");
                return;
            }

            handler.StartPeek(1, PeekPileHandler.PeekMode.All);
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
            => "偷看所有牌堆中第一张未揭示的牌";
    }
}
