using System.Collections.Generic;
using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 商店打开时自动悬挂：ShopHangController 主动检查手牌中的 FishData 是否携带此效果，
    /// 若有空悬挂槽则自动将该鱼卡从手牌移至悬挂槽。不通过 TriggerEffects 触发。
    /// 每张卡仅自动悬挂一次，返回手牌后不再触发。
    /// </summary>
    [System.Serializable]
    public class Effect_AutoHang : EffectBase
    {
        private static readonly HashSet<FishData> autoHungCards = new HashSet<FishData>();

        public override string DisplayName => "自动悬挂";

        public override void Execute(EffectContext context)
        {
            Debug.Log("[Effect_AutoHang] 此效果由 ShopHangController 主动检查，Execute 不执行操作");
        }

        public override string GetDescription()
        {
            return "商店打开时自动悬挂到空槽位（仅一次）";
        }

        /// <summary>
        /// 该鱼卡是否已被自动悬挂过（本局内）
        /// </summary>
        public static bool HasBeenAutoHung(FishData fish) => autoHungCards.Contains(fish);

        /// <summary>
        /// 标记该鱼卡已被自动悬挂
        /// </summary>
        public static void MarkAutoHung(FishData fish) => autoHungCards.Add(fish);

        /// <summary>
        /// 重置自动悬挂记录（游戏重置时调用）
        /// </summary>
        public static void ResetAutoHungState() => autoHungCards.Clear();
    }
}
