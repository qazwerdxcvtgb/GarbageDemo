using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FishCardSystem
{
    /// <summary>
    /// [DEPRECATED] 跨 Holder 拖拽协调器（面板级，已废弃）
    ///
    /// 此类已被 CrossHolderSystem（全局单例）替代，不再使用。
    /// 保留文件仅用于过渡期参考，后续可安全删除。
    ///
    /// 迁移说明：
    /// - Source Holder 自动注册：FishCardHolder 按 CrossHolderRole 在 Start() 中调用 CrossHolderSystem.RegisterSource
    /// - Target 自动激活：ShopHangSlot 在 OnEnable/OnDisable 中调用 CrossHolderSystem.RegisterTarget/UnregisterTarget
    /// - 事件订阅：ShopHangController 直接订阅 CrossHolderSystem.Instance.OnCardDroppedToSlot
    /// - ShopPanel 无需再管理跨 Holder 的激活状态
    /// </summary>
    [System.Obsolete("已被 CrossHolderSystem 替代，请勿在新场景中使用此组件。")]
    public class CrossHolderDragHandler : MonoBehaviour
    {
        [Header("触发阈值")]
        [Tooltip("Y 轴世界坐标偏移超过此值时进入跨 Holder 状态（世界单位，典型值 1.5~3）")]
        [SerializeField] private float yThreshold = 1.5f;

        // 触发跨 Holder 后，卡牌成功落入目标槽位时触发
        [HideInInspector] public UnityEvent<ItemCard, object> OnCardDroppedToSlot
            = new UnityEvent<ItemCard, object>();

        private FishCardHolder sourceHolder;
        private List<RectTransform> targetSlotRects = new List<RectTransform>();
        private List<object> targetSlots           = new List<object>();

        private ItemCard trackedCard;
        private Vector3  dragStartWorldPos;
        private bool     isCrossing;
        private bool     isActive;

        #region Activation

        public void SetActive(bool active)
        {
            isActive = active;
            if (!active)
            {
                trackedCard = null;
                isCrossing  = false;
            }
        }

        public bool IsActive => isActive;
        public int GetTargetSlotCount() => targetSlotRects.Count;

        #endregion

        #region Registration

        /// <summary>
        /// 注册 Source Holder（手牌 Holder）
        /// </summary>
        public void SetSourceHolder(FishCardHolder holder)
        {
            if (sourceHolder != null)
                UnsubscribeAllFromHolder(sourceHolder);

            sourceHolder = holder;
        }

        /// <summary>
        /// 注册 Target 槽位（ShopHangSlot 等）
        /// object 类型保持通用，落点检测结果通过 OnCardDroppedToSlot 传回
        /// </summary>
        public void RegisterTargetSlot(RectTransform slotRect, object slotObject)
        {
            if (!targetSlotRects.Contains(slotRect))
            {
                targetSlotRects.Add(slotRect);
                targetSlots.Add(slotObject);
            }
        }

        public void ClearTargetSlots()
        {
            targetSlotRects.Clear();
            targetSlots.Clear();
        }

        /// <summary>
        /// 刷新对 Source Holder 所有卡牌的事件订阅（新卡加入后调用）
        /// </summary>
        public void RefreshSubscriptions()
        {
            if (sourceHolder == null) return;

            foreach (var card in sourceHolder.GetCards())
            {
                card.BeginDragEvent.RemoveListener(OnCardBeginDrag);
                card.EndDragEvent.RemoveListener(OnCardEndDrag);
                card.BeginDragEvent.AddListener(OnCardBeginDrag);
                card.EndDragEvent.AddListener(OnCardEndDrag);
            }
        }

        /// <summary>
        /// 订阅单张新加入卡牌的事件（供外部在 AddCard 后调用）
        /// </summary>
        public void SubscribeCard(ItemCard card)
        {
            card.BeginDragEvent.RemoveListener(OnCardBeginDrag);
            card.EndDragEvent.RemoveListener(OnCardEndDrag);
            card.BeginDragEvent.AddListener(OnCardBeginDrag);
            card.EndDragEvent.AddListener(OnCardEndDrag);
        }

        private void UnsubscribeAllFromHolder(FishCardHolder holder)
        {
            foreach (var card in holder.GetCards())
            {
                card.BeginDragEvent.RemoveListener(OnCardBeginDrag);
                card.EndDragEvent.RemoveListener(OnCardEndDrag);
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (!isActive || trackedCard == null || !trackedCard.isDragging) return;

            float yDelta = trackedCard.transform.position.y - dragStartWorldPos.y;
            isCrossing = Mathf.Abs(yDelta) > yThreshold;
        }

        private void OnDestroy()
        {
            if (sourceHolder != null)
                UnsubscribeAllFromHolder(sourceHolder);
        }

        #endregion

        #region Drag Event Handlers

        private void OnCardBeginDrag(ItemCard card)
        {
            if (!isActive) return;
            trackedCard       = card;
            dragStartWorldPos = card.transform.position;
            isCrossing        = false;
        }

        private void OnCardEndDrag(ItemCard card)
        {
            if (!isActive || card != trackedCard)
            {
                if (card == trackedCard) ResetTracking();
                return;
            }

            if (!isCrossing)
            {
                ResetTracking();
                return;
            }

            // 只允许 FishCard 进入悬挂槽
            if (!(card is FishCard))
            {
                ResetTracking();
                return;
            }

            object hitSlot = FindSlotAtCardPosition(card);
            if (hitSlot != null)
                OnCardDroppedToSlot.Invoke(card, hitSlot);
            // else: 卡牌通过 FishCardHolder.OnCardEndDrag 自然回到原槽位

            ResetTracking();
        }

        private void ResetTracking()
        {
            trackedCard = null;
            isCrossing  = false;
        }

        #endregion

        #region Hit Detection

        private object FindSlotAtCardPosition(ItemCard card)
        {
            Camera cam = Camera.main;
            if (cam == null) return null;

            Vector2 screenPos = cam.WorldToScreenPoint(card.transform.position);

            for (int i = 0; i < targetSlotRects.Count; i++)
            {
                RectTransform slotRect = targetSlotRects[i];
                if (slotRect == null) continue;

                if (RectTransformUtility.RectangleContainsScreenPoint(slotRect, screenPos, cam))
                    return targetSlots[i];
            }

            return null;
        }

        #endregion
    }
}
