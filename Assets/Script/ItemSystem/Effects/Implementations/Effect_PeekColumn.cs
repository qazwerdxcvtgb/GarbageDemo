using UnityEngine;
using FishingSystem;

namespace ItemSystem
{
    /// <summary>
    /// 偷看一列效果：触发后由玩家选择一个牌堆，偷看该牌堆所在列（同序号）的 3 个牌堆各 1 张未揭示顶牌。
    /// </summary>
    [System.Serializable]
    public class Effect_PeekColumn : EffectBase
    {
        public override string DisplayName => "偷看一列";

        public override void Execute(EffectContext context)
        {
            var handler = PeekPileHandler.Instance;
            if (handler == null)
            {
                Debug.LogError("[Effect_PeekColumn] PeekPileHandler 不存在，无法执行偷看");
                return;
            }

            handler.StartPeek(0, PeekPileHandler.PeekMode.Column);
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
            => "选择牌堆后偷看同一列的3个牌堆各1张顶牌";
    }
}
