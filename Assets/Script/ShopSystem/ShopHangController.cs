using System.Collections.Generic;
using UnityEngine;
using ItemSystem;
using UnityEngine.UI;
using FishCardSystem;
using HandSystem;
using DG.Tweening;

namespace ShopSystem
{
    /// <summary>
    /// 商店悬挂功能控制器
    /// 管理三个 ShopHangSlot，处理以下三种交互场景：
    /// 场景1：手牌鱼卡 → 空槽（正常悬挂）
    /// 场景2：手牌鱼卡 → 已有卡的槽（替换：旧卡回手牌，新卡进槽）
    /// 场景3：槽内卡牌 → 手牌区域（拖出：卡牌归还手牌）
    /// </summary>
    public class ShopHangController : MonoBehaviour
    {
        [Header("槽位引用")]
        [SerializeField] private ShopHangSlot[] hangSlots = new ShopHangSlot[3];

        [Header("依赖引用")]
        [SerializeField] private FishCardHolder handCardHolder;

        [Header("卡牌预制体（用于恢复视觉）")]
        [SerializeField] private GameObject fishCardPrefab;

        [Header("图层设置")]
        [Tooltip("商店面板 Canvas 的 sortingOrder，视觉卡将设置为此值 +1")]
        [SerializeField] private int shopPanelSortingOrder = 165;

        private void Awake()
        {
            if (handCardHolder == null)
            {
                var hpui = FindObjectOfType<HandPanelUI>();
                if (hpui != null)
                    handCardHolder = hpui.CardHolder;
            }

            if (CrossHolderSystem.Instance != null)
            {
                CrossHolderSystem.Instance.OnCardDroppedToSlot.AddListener(OnCrossHolderDrop);
                CrossHolderSystem.Instance.OnCardEjectedToHand.AddListener(OnCardEjectedToHand);
            }
        }

        private void Start()
        {
            // 备用注册：Awake 执行时 CrossHolderSystem 可能尚未初始化（脚本执行顺序不确定）
            // Start() 在所有 Awake() 完成后执行，此时 Instance 已就绪
            if (CrossHolderSystem.Instance != null)
            {
                CrossHolderSystem.Instance.OnCardDroppedToSlot.RemoveListener(OnCrossHolderDrop);
                CrossHolderSystem.Instance.OnCardDroppedToSlot.AddListener(OnCrossHolderDrop);
                CrossHolderSystem.Instance.OnCardEjectedToHand.RemoveListener(OnCardEjectedToHand);
                CrossHolderSystem.Instance.OnCardEjectedToHand.AddListener(OnCardEjectedToHand);
            }

            if (ItemSystem.EffectBus.Instance != null)
                ItemSystem.EffectBus.Instance.OnHangReplaceChanged += OnHangReplaceChanged;
        }

        private void OnDestroy()
        {
            if (CrossHolderSystem.Instance != null)
            {
                CrossHolderSystem.Instance.OnCardDroppedToSlot.RemoveListener(OnCrossHolderDrop);
                CrossHolderSystem.Instance.OnCardEjectedToHand.RemoveListener(OnCardEjectedToHand);
            }

            if (ItemSystem.EffectBus.Instance != null)
                ItemSystem.EffectBus.Instance.OnHangReplaceChanged -= OnHangReplaceChanged;
        }

        #region Public API

        /// <summary>
        /// 商店打开时调用：从 ShopManager 恢复所有悬挂槽的视觉状态
        /// </summary>
        public void RestoreHangState()
        {
            if (ShopManager.Instance == null) return;

            for (int i = 0; i < hangSlots.Length; i++)
            {
                if (hangSlots[i] == null) continue;

                FishData data = ShopManager.Instance.GetHangSlot(i);
                if (data != null)
                    hangSlots[i].RestoreCard(data, fishCardPrefab);
            }
        }

        /// <summary>
        /// 商店关闭时调用：清空所有槽位视觉（数据保留在 ShopManager 中）
        /// </summary>
        public void ClearAllSlotVisuals()
        {
            foreach (var slot in hangSlots)
                slot?.ClearSlot();
        }

        /// <summary>
        /// 获取所有槽位列表（供外部查询）
        /// </summary>
        public IReadOnlyList<ShopHangSlot> GetSlots() => hangSlots;

        #endregion

        #region Drop Handler（场景1 & 场景2）

        private void OnCrossHolderDrop(ItemCard card, ICardSlot targetSlot)
        {
            if (!(targetSlot is ShopHangSlot hangSlot)) return;

            int slotIndex = System.Array.IndexOf(hangSlots, hangSlot);
            if (slotIndex < 0)
            {
                Debug.LogWarning("[ShopHangController] 无法找到目标槽位索引");
                return;
            }

            // 场景2：槽位已有卡牌 → 先将旧卡归还手牌
            if (hangSlot.IsOccupied)
            {
                EjectCardToHand(hangSlot.OccupiedCard, hangSlot, slotIndex);
            }

            // 场景1 / 场景2 后续：执行悬挂
            ExecuteHang(card, hangSlot, slotIndex);
        }

        private void ExecuteHang(ItemCard card, ShopHangSlot slot, int slotIndex)
        {
            FishData fishData = card.cardData as FishData;
            if (fishData == null)
            {
                Debug.LogWarning("[ShopHangController] 目标卡牌不含 FishData，取消悬挂");
                return;
            }

            // 写入持久化数据（支持覆盖）
            ShopManager.Instance.TryHangFish(slotIndex, fishData);

            Transform cardSlot = card.transform.parent;

            // 先将卡牌迁移到槽位，确保逻辑卡始终在 Canvas 树内，避免因暂时脱离 Canvas 导致视觉卡闪烁
            slot.AcceptCard(card);

            // 接管后再 detach 旧容器（此时 card 已不在 cardSlot 下，SetSlotCount 不会误删）
            if (cardSlot != null && cardSlot != handCardHolder?.transform)
                cardSlot.SetParent(null);

            handCardHolder?.RemoveCard(card);
            HandManager.Instance?.RemoveCard(fishData);

            // 仿照 EquipmentPanel.ExecuteEquip 在 RemoveCard 后重新注册拖拽监听，
            // 防止 UnsubscribeSourceCard 导致 FishCardVisual.EndDrag 被跳过（canvas 层级卡在 200）
            CrossHolderSystem.Instance?.RegisterSlotCard(card, slot);

            // 设置视觉卡归属层级（ShopPanel sortingOrder + 1）
            card.SetVisualHomeSortingOrder(shopPanelSortingOrder + 1);

            if (cardSlot != null)
                Destroy(cardSlot.gameObject);

            Debug.Log($"[ShopHangController] 悬挂成功：{fishData.itemName} → 槽位 {slotIndex}");
        }

        #endregion

        #region Eject Handler（场景3）

        private void OnCardEjectedToHand(ItemCard card, ICardSlot sourceSlot)
        {
            if (!(sourceSlot is ShopHangSlot hangSlot)) return;

            int slotIndex = System.Array.IndexOf(hangSlots, hangSlot);
            if (slotIndex < 0) return;

            if (ItemSystem.EffectBus.Instance == null || !ItemSystem.EffectBus.Instance.AllowHangReplace)
            {
                card.transform.DOKill();
                card.transform.DOLocalMove(Vector3.zero, 0.2f).SetEase(Ease.OutBack);
                return;
            }

            EjectCardToHand(card, hangSlot, slotIndex);
        }

        /// <summary>
        /// 将槽位卡牌归还手牌（场景2旧卡清场 和 场景3 均调用此方法）
        /// </summary>
        private void EjectCardToHand(ItemCard card, ShopHangSlot slot, int slotIndex)
        {
            FishData fishData = card.cardData as FishData;

            card.isLocked = false;

            // 从槽位释放卡牌（清除 CrossHolderSystem 注册）
            slot.ReleaseCard();

            // 清空持久化数据
            ShopManager.Instance?.TryHangFish(slotIndex, null);

            // 归还手牌数据层（仅数据+OnHandChanged，不触发 OnCardAdded，避免 HandPanelUI 重复创建视觉卡）
            HandManager.Instance?.AddCardData(fishData);

            // 归还手牌视觉层（含排序动画）
            if (handCardHolder != null)
                handCardHolder.AddCard(card);

            // 归还手牌后重置视觉卡层级（继承 VisualCardsHandler 默认层）
            card.SetVisualHomeSortingOrder(0);

            Debug.Log($"[ShopHangController] 槽位 {slotIndex} 卡牌归还手牌：{fishData?.itemName}");
        }

        #endregion

        #region Hang Replace State

        /// <summary>
        /// 响应 EffectBus.OnHangReplaceChanged，更新所有已悬挂卡牌的 isLocked 状态
        /// </summary>
        private void OnHangReplaceChanged(bool allowed)
        {
            foreach (var slot in hangSlots)
            {
                if (slot != null && slot.IsOccupied)
                    slot.OccupiedCard.isLocked = !allowed;
            }
        }

        #endregion
    }
}
