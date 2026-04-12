using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 标记效果：携带此效果的鱼卡不可被出售，不可被悬挂。
    /// Execute 不执行操作，由 ShopHangSlot.CanAccept 和 ShopSellController 主动检查。
    /// </summary>
    [System.Serializable]
    public class Effect_NoSellNoHang : EffectBase
    {
        public override string DisplayName => "不可交易";

        public override void Execute(EffectContext context)
        {
            Debug.Log("[Effect_NoSellNoHang] 标记效果，Execute 不执行操作");
        }

        public override string GetDescription()
        {
            return "该卡牌不可出售，不可悬挂";
        }

        /// <summary>
        /// 检查指定鱼卡是否携带 Effect_NoSellNoHang 效果
        /// </summary>
        public static bool HasEffect(FishData fish)
        {
            if (fish == null || fish.effects == null) return false;
            foreach (var effect in fish.effects)
            {
                if (effect is Effect_NoSellNoHang)
                    return true;
            }
            return false;
        }
    }
}
