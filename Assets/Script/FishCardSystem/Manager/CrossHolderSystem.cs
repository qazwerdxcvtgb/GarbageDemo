using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

namespace FishCardSystem
{
    /// <summary>
    /// 跨 Holder 拖拽全局系统（单例）
    /// 支持双向拖拽：手牌 Holder → 槽位，以及槽位 → 手牌区域。
    ///
    /// 工作原理：
    /// - Source Holder（FishCardHolder，role=Source）在 Start() 自动注册，OnDestroy 自动注销
    /// - Target 槽位（实现 ICardSlot 的组件）在 OnEnable/OnDisable 自动注册/注销
    /// - 槽位持有的卡牌通过 RegisterSlotCard 注册后可被拖拽，但只能回到手牌区域或原槽位（禁止槽间转移）
    /// - 手牌区域通过 RegisterHandZone 注册，槽位卡拖拽到此区域时触发 OnCardEjectedToHand
    ///
    /// 三种交互场景：
    /// 场景1：手牌卡 → 空槽     → OnCardDroppedToSlot（IsOccupied=false）
    /// 场景2：手牌卡 → 已有卡槽 → OnCardDroppedToSlot（IsOccupied=true，由控制器执行替换）
    /// 场景3：槽位卡 → 手牌区域 → OnCardEjectedToHand
    ///        槽位卡 → 其他位置 → 归回原槽（禁止槽间转移）
    ///
    /// 执行顺序约定：
    /// FishCardHolder.BindCardEvents 先于 CrossHolderSystem.SubscribeSourceCard 订阅 EndDragEvent，
    /// 因此 FishCardHolder.OnCardEndDrag 先执行（触发错误归位动画），
    /// CrossHolderSystem.OnCardEndDrag 后执行，调用 RejoinAndReturn 通过 DOKill 覆盖错误动画。
    /// </summary>
    public class CrossHolderSystem : MonoBehaviour
    {
        public static CrossHolderSystem Instance { get; private set; }

        [Header("触发阈值")]
        [Tooltip("Y 轴世界坐标偏移超过此值时进入跨 Holder 状态（世界单位，典型值 1.5~3）")]
        [SerializeField] private float yThreshold = 1.5f;

        /// <summary>手牌卡成功落入目标槽位时触发（场景1、场景2）</summary>
        [HideInInspector] public UnityEvent<ItemCard, ICardSlot> OnCardDroppedToSlot
            = new UnityEvent<ItemCard, ICardSlot>();

        /// <summary>槽位卡拖拽到手牌区域时触发（场景3）</summary>
        [HideInInspector] public UnityEvent<ItemCard, ICardSlot> OnCardEjectedToHand
            = new UnityEvent<ItemCard, ICardSlot>();

        // 手牌 Source Holders
        private readonly List<FishCardHolder> sourceHolders = new List<FishCardHolder>();

        // Target 槽位列表（ICardSlot 实现）
        private readonly List<RectTransform> targetRects  = new List<RectTransform>();
        private readonly List<ICardSlot>     targetSlots  = new List<ICardSlot>();

        // 槽位卡追踪：记录来自槽位的卡牌及其原始槽位
        private readonly Dictionary<ItemCard, ICardSlot> slotCardOrigins = new Dictionary<ItemCard, ICardSlot>();

        // 手牌区域矩形（用于槽位卡的"归还手牌"检测）
        private RectTransform handZoneRect;

        private ItemCard trackedCard;
        private Vector3  dragStartWorldPos;
        private bool     isCrossing;
        private bool     wasCrossing;

        private bool HasActiveTargets => targetRects.Count > 0 || handZoneRect != null;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (!HasActiveTargets || trackedCard == null || !trackedCard.isDragging) return;

            float yDelta = trackedCard.transform.position.y - dragStartWorldPos.y;
            bool newIsCrossing = Mathf.Abs(yDelta) > yThreshold;

            if (newIsCrossing != isCrossing)
            {
                isCrossing = newIsCrossing;
                if (isCrossing)
                    NotifyCrossBegin(trackedCard);
                else
                    NotifyCrossEnd(trackedCard);
            }

            wasCrossing = isCrossing;
        }

        #endregion

        #region Source Registration（手牌 Holder）

        /// <summary>
        /// 注册 Source Holder 并订阅其当前所有卡牌。
        /// 由 FishCardHolder.Start() 调用（在 BindCardEvents 之后，保证订阅顺序靠后）。
        /// </summary>
        public void RegisterSource(FishCardHolder holder)
        {
            if (holder == null || sourceHolders.Contains(holder)) return;
            sourceHolders.Add(holder);
            foreach (var card in holder.GetCards())
                SubscribeSourceCard(card);
        }

        /// <summary>
        /// 注销 Source Holder 并取消订阅其所有卡牌。
        /// 由 FishCardHolder.OnDestroy() 调用。
        /// </summary>
        public void UnregisterSource(FishCardHolder holder)
        {
            if (holder == null || !sourceHolders.Contains(holder)) return;
            foreach (var card in holder.GetCards())
                UnsubscribeSourceCard(card);
            sourceHolders.Remove(holder);
        }

        /// <summary>
        /// 订阅单张手牌卡牌（FishCardHolder.AddCard 后调用）。
        /// </summary>
        public void SubscribeSourceCard(ItemCard card)
        {
            if (card == null) return;
            card.BeginDragEvent.RemoveListener(OnCardBeginDrag);
            card.EndDragEvent.RemoveListener(OnCardEndDrag);
            card.BeginDragEvent.AddListener(OnCardBeginDrag);
            card.EndDragEvent.AddListener(OnCardEndDrag);
        }

        /// <summary>
        /// 取消订阅单张手牌卡牌（FishCardHolder.RemoveCard 后调用）。
        /// </summary>
        public void UnsubscribeSourceCard(ItemCard card)
        {
            if (card == null) return;
            card.BeginDragEvent.RemoveListener(OnCardBeginDrag);
            card.EndDragEvent.RemoveListener(OnCardEndDrag);
        }

        #endregion

        #region Slot Card Registration（槽位持有的卡牌）

        /// <summary>
        /// 注册来自槽位的卡牌，使其可被拖拽（场景3：槽 → 手牌区域）。
        /// 由槽位的 AcceptCard() 调用。
        /// </summary>
        public void RegisterSlotCard(ItemCard card, ICardSlot sourceSlot)
        {
            if (card == null || sourceSlot == null) return;

            slotCardOrigins[card] = sourceSlot;
            card.BeginDragEvent.RemoveListener(OnCardBeginDrag);
            card.EndDragEvent.RemoveListener(OnCardEndDrag);
            card.BeginDragEvent.AddListener(OnCardBeginDrag);
            card.EndDragEvent.AddListener(OnCardEndDrag);
        }

        /// <summary>
        /// 注销槽位卡牌的拖拽监听（槽位 ReleaseCard 或卡牌被销毁时调用）。
        /// </summary>
        public void UnregisterSlotCard(ItemCard card)
        {
            if (card == null) return;
            slotCardOrigins.Remove(card);
            card.BeginDragEvent.RemoveListener(OnCardBeginDrag);
            card.EndDragEvent.RemoveListener(OnCardEndDrag);
        }

        #endregion

        #region Target Registration（槽位 Target）

        /// <summary>
        /// 注册 Target 槽位（实现 ICardSlot 的组件在 OnEnable 调用）。
        /// </summary>
        public void RegisterTarget(RectTransform slotRect, ICardSlot slot)
        {
            if (slotRect == null || targetRects.Contains(slotRect)) return;
            targetRects.Add(slotRect);
            targetSlots.Add(slot);
        }

        /// <summary>
        /// 注销 Target 槽位（OnDisable 调用）。
        /// </summary>
        public void UnregisterTarget(RectTransform slotRect)
        {
            int index = targetRects.IndexOf(slotRect);
            if (index < 0) return;
            targetRects.RemoveAt(index);
            targetSlots.RemoveAt(index);

            if (trackedCard != null && !HasActiveTargets)
            {
                if (isCrossing)
                {
                    FishCardHolder holder = FindSourceHolder(trackedCard);
                    if (holder != null)
                        holder.RejoinAndReturn(trackedCard);
                    else
                        trackedCard.ReturnToSlot();
                }
                ResetTracking();
            }
        }

        /// <summary>
        /// 注册手牌区域矩形，供槽位卡"归还手牌"时的命中检测使用。
        /// 由 HandPanelUI 或对应 Holder 在 Start() 调用。
        /// </summary>
        public void RegisterHandZone(RectTransform rect)
        {
            handZoneRect = rect;
        }

        /// <summary>
        /// 注销手牌区域矩形。
        /// </summary>
        public void UnregisterHandZone()
        {
            handZoneRect = null;
        }

        #endregion

        #region Cross Notification

        private void NotifyCrossBegin(ItemCard card)
        {
            // 槽位卡拖拽时没有对应的 FishCardHolder，不需要触发 BeginCrossFloat
            if (slotCardOrigins.ContainsKey(card)) return;
            FishCardHolder holder = FindSourceHolder(card);
            holder?.BeginCrossFloat(card);
        }

        private void NotifyCrossEnd(ItemCard card)
        {
            if (slotCardOrigins.ContainsKey(card)) return;
            FishCardHolder holder = FindSourceHolder(card);
            holder?.EndCrossFloat(card);
        }

        private FishCardHolder FindSourceHolder(ItemCard card)
        {
            foreach (var holder in sourceHolders)
            {
                if (holder.GetCards().Contains(card))
                    return holder;
            }
            return null;
        }

        #endregion

        #region Drag Event Handlers

        private void OnCardBeginDrag(ItemCard card)
        {
            if (!HasActiveTargets) return;

            trackedCard       = card;
            dragStartWorldPos = card.transform.position;
            isCrossing        = false;
            wasCrossing       = false;
        }

        private void OnCardEndDrag(ItemCard card)
        {
            if (card != trackedCard)
            {
                if (card == trackedCard) ResetTracking();
                return;
            }

            bool isSlotCard = slotCardOrigins.TryGetValue(card, out ICardSlot sourceSlot);

            // 槽位卡不依赖 isCrossing（Y 阈值），直接按落点决策
            if (isSlotCard)
            {
                HandleSlotCardEndDrag(card, sourceSlot);
                ResetTracking();
                return;
            }

            // 手牌卡：需要 isCrossing 才能触发跨 Holder 流程
            if (!HasActiveTargets || !isCrossing)
            {
                ResetTracking();
                return;
            }

            HandleHandCardEndDrag(card);
            ResetTracking();
        }

        /// <summary>
        /// 处理来自槽位的卡牌拖拽结束（场景3）。
        /// 规则：落点超出源槽位范围 + 落在手牌区域 → 归还手牌；其他情况归回原槽。
        /// </summary>
        private void HandleSlotCardEndDrag(ItemCard card, ICardSlot sourceSlot)
        {
            RectTransform slotRect  = sourceSlot?.GetSlotRect();
            bool outsideSlot = slotRect == null || !IsPointInRect(card.transform.position, slotRect);
            bool insideHand  = handZoneRect != null && IsPointInRect(card.transform.position, handZoneRect);

            if (outsideSlot && insideHand)
            {
                // 落点超出槽位 + 落在手牌区域 → 触发归还手牌
                OnCardEjectedToHand.Invoke(card, sourceSlot);
            }
            else
            {
                // 落点在槽内，或未落入手牌区域 → 归回原槽
                sourceSlot.AcceptCard(card);
            }
        }

        /// <summary>
        /// 处理来自手牌 Holder 的卡牌拖拽结束（场景1、场景2）。
        /// </summary>
        private void HandleHandCardEndDrag(ItemCard card)
        {
            ICardSlot hitSlot = FindSlotAtCardPosition(card);

            if (hitSlot != null)
            {
                // 命中槽位（已占用或空）→ 触发落槽事件，由控制器决定处理方式
                OnCardDroppedToSlot.Invoke(card, hitSlot);
            }
            else
            {
                // 未命中任何槽位 → 归回手牌 Holder
                FishCardHolder holder = FindSourceHolder(card);
                if (holder != null)
                    holder.RejoinAndReturn(card);
                else
                    card.ReturnToSlot();
            }
        }

        private void ResetTracking()
        {
            trackedCard = null;
            isCrossing  = false;
            wasCrossing = false;
        }

        #endregion

        #region Hit Detection

        /// <summary>
        /// 在已注册的 Target 槽位中查找命中的槽位，并验证类型兼容性。
        /// </summary>
        private ICardSlot FindSlotAtCardPosition(ItemCard card)
        {
            for (int i = 0; i < targetRects.Count; i++)
            {
                RectTransform rect = targetRects[i];
                if (rect == null) continue;

                if (IsPointInRect(card.transform.position, rect))
                {
                    ICardSlot slot = targetSlots[i];
                    if (slot.CanAccept(card))
                        return slot;
                }
            }
            return null;
        }

        private bool IsPointInRect(Vector3 worldPos, RectTransform rect)
        {
            Camera cam = Camera.main;
            if (cam == null) return false;
            Vector2 screenPos = cam.WorldToScreenPoint(worldPos);
            return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, cam);
        }

        #endregion
    }
}
