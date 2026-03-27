using UnityEngine;
using ItemSystem;

namespace FishCardSystem
{
    /// <summary>
    /// 卡牌槽位接口
    /// 由所有可接受卡牌拖入的槽位实现（ShopHangSlot、EquipmentSlotUI 等）。
    /// CrossHolderSystem 通过此接口操作槽位，无需关心具体槽位类型。
    /// </summary>
    public interface ICardSlot
    {
        /// <summary>槽位是否已有卡牌</summary>
        bool IsOccupied { get; }

        /// <summary>当前槽位持有的卡牌（空槽时为 null）</summary>
        ItemCard OccupiedCard { get; }

        /// <summary>
        /// 判断指定卡牌是否可以被此槽位接受（类型校验）
        /// </summary>
        bool CanAccept(ItemCard card);

        /// <summary>
        /// 槽位接管卡牌。
        /// 调用方需确保槽位已空（替换场景下先调用 ReleaseCard）。
        /// </summary>
        void AcceptCard(ItemCard card);

        /// <summary>
        /// 槽位放弃当前持有的卡牌，不销毁卡牌，由调用方负责后续处理。
        /// </summary>
        void ReleaseCard();

        /// <summary>
        /// 获取槽位的 RectTransform（用于 CrossHolderSystem 注册和命中检测）
        /// </summary>
        RectTransform GetSlotRect();
    }
}
