using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

namespace FishCardSystem
{
    /// <summary>
    /// 定义 FishCardHolder 在跨 Holder 拖拽系统中的角色
    /// </summary>
    public enum CrossHolderRole
    {
        None,   // 不参与跨 Holder 拖拽
        Source, // 可从此 Holder 拖出（手牌 Holder）
        Target, // 可接受从 Source 拖入（悬挂槽等）
        Both    // 双向（预留）
    }

    /// <summary>
    /// 手牌卡牌容器管理器（支持所有 ItemCard 子类）
    /// 负责：槽位生成、卡牌事件绑定、拖拽排序、返回动画、悬停压缩效果
    ///
    /// 跨 Holder 浮动槽位机制：
    /// 当 CrossHolderSystem 检测到卡牌超越 Y 阈值时，调用 BeginCrossFloat：
    ///   - 原始槽位被禁用（HorizontalLayoutGroup 自动合并间距）
    ///   - 卡牌临时挂载到 Canvas 根节点下的 floatSlotGO（保持世界坐标）
    /// 当卡牌回落或拖拽结束时，调用 EndCrossFloat 或 RejoinAndReturn 恢复。
    /// </summary>
    public class FishCardHolder : MonoBehaviour
    {
        [Header("槽位设置")]
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private int cardsToSpawn = 7;

        [Header("跨 Holder 拖拽")]
        [SerializeField] public CrossHolderRole crossHolderRole = CrossHolderRole.None;

        [Header("动画设置")]
        [SerializeField] private bool tweenCardReturn = true;

        [Header("悬停压缩效果")]
        [SerializeField][Range(0f, 1f)] private float compressionRate = 0.5f;
        [SerializeField] private float cardWidth = 220f;
        [SerializeField] private float compressionTransition = 0.2f;

        private RectTransform rect;
        private List<ItemCard> cards;
        private ItemCard selectedCard;
        private ItemCard hoveredCard;
        private bool isCrossing;
        private Dictionary<ItemCard, Tween> compressionTweens = new Dictionary<ItemCard, Tween>();

        // 浮动状态：key=浮动中的卡牌，value=(原始槽位, 临时float GO)
        private readonly Dictionary<ItemCard, (Transform originalSlot, GameObject floatSlotGO)>
            floatingCards = new Dictionary<ItemCard, (Transform, GameObject)>();

        // 当前是否有卡牌处于浮动状态，用于暂停排序逻辑
        private bool isCardFloating;

        private void Start()
        {
            rect  = GetComponent<RectTransform>();
            cards = new List<ItemCard>();

            for (int i = 0; i < cardsToSpawn; i++)
                Instantiate(slotPrefab, transform);

            cards = GetComponentsInChildren<ItemCard>().ToList();

            foreach (var card in cards)
                BindCardEvents(card);

            // BindCardEvents 完成后注册，保证 CrossHolderSystem 的 EndDragEvent 订阅顺序靠后
            if (IsSourceRole())
                CrossHolderSystem.Instance?.RegisterSource(this);

            StartCoroutine(DelayedUpdateIndices());
        }

        private void OnDestroy()
        {
            if (IsSourceRole())
                CrossHolderSystem.Instance?.UnregisterSource(this);
        }

        private bool IsSourceRole() =>
            crossHolderRole == CrossHolderRole.Source || crossHolderRole == CrossHolderRole.Both;

        private IEnumerator DelayedUpdateIndices()
        {
            yield return new WaitForSeconds(0.1f);
            foreach (var card in cards)
            {
                if (card.cardVisual != null)
                    card.cardVisual.UpdateIndex();
            }
        }

        private void Update()
        {
            // isCardFloating: 有卡牌处于浮动状态时暂停排序，避免 ParentIndex() 逻辑出错
            if (selectedCard == null || isCrossing || isCardFloating) return;

            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == selectedCard) continue;

                if (selectedCard.transform.position.x > cards[i].transform.position.x &&
                    selectedCard.ParentIndex() < cards[i].ParentIndex())
                {
                    Swap(i);
                    break;
                }
                else if (selectedCard.transform.position.x < cards[i].transform.position.x &&
                         selectedCard.ParentIndex() > cards[i].ParentIndex())
                {
                    Swap(i);
                    break;
                }
            }
        }

        #region Card Event Callbacks

        private void OnCardPointerEnter(ItemCard card)
        {
            hoveredCard = card;
            ApplyHoverCompression(card);
        }

        private void OnCardPointerExit(ItemCard card)
        {
            if (hoveredCard == card)
            {
                hoveredCard = null;
                ResetCompression();
            }
        }

        private void OnCardBeginDrag(ItemCard card)
        {
            selectedCard = card;
            ResetCompression();
        }

        private void OnCardEndDrag(ItemCard card)
        {
            if (selectedCard != card) return;

            Vector3 targetLocalPos = card.selected
                ? new Vector3(0, card.selectionOffset, 0)
                : Vector3.zero;

            float duration = tweenCardReturn ? 0.15f : 0f;
            card.transform.DOLocalMove(targetLocalPos, duration).SetEase(Ease.OutBack);

            rect.sizeDelta += Vector2.one * 0.01f;
            rect.sizeDelta -= Vector2.one * 0.01f;

            selectedCard = null;
        }

        #endregion

        #region Public API

        /// <summary>
        /// 获取容器中的所有卡牌
        /// </summary>
        public List<ItemCard> GetCards() => new List<ItemCard>(cards);

        /// <summary>
        /// 添加卡牌到容器（自动寻找第一个空槽位）
        /// </summary>
        public void AddCard(ItemCard card, int slotIndex = -1)
        {
            if (slotIndex < 0 || slotIndex >= transform.childCount)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform slot = transform.GetChild(i);
                    // 跳过被禁用的槽位（BeginCrossFloat 期间 originalSlot 会被 SetActive(false)）
                    // 否则替换场景下旧卡会被放入正在浮动的新卡的 originalSlot，随后被 RemoveCard 销毁
                    if (slot.childCount == 0 && slot.gameObject.activeSelf)
                    {
                        card.transform.SetParent(slot, false);
                        card.transform.localPosition = Vector3.zero;
                        card.transform.localScale    = Vector3.one;
                        break;
                    }
                }
            }
            else
            {
                Transform slot = transform.GetChild(slotIndex);
                card.transform.SetParent(slot, false);
                card.transform.localPosition = Vector3.zero;
                card.transform.localScale    = Vector3.one;
            }

            if (!cards.Contains(card))
            {
                cards.Add(card);
                BindCardEvents(card);
                // BindCardEvents 之后订阅，保证 CrossHolderSystem 处理顺序靠后
                if (IsSourceRole())
                    CrossHolderSystem.Instance?.SubscribeSourceCard(card);
            }
        }

        /// <summary>
        /// 从容器移除卡牌（仅解除绑定，槽位保留）。
        /// 若卡牌正处于浮动状态，同时销毁其原始（disabled）槽位；
        /// floatSlotGO 由调用方（ExecuteHang）负责销毁。
        /// </summary>
        public void RemoveCard(ItemCard card)
        {
            if (!cards.Contains(card)) return;

            // 浮动状态清理：销毁被禁用的原始槽位
            if (floatingCards.TryGetValue(card, out var entry))
            {
                floatingCards.Remove(card);
                if (entry.originalSlot != null)
                {
                    entry.originalSlot.SetParent(null);
                    Destroy(entry.originalSlot.gameObject);
                }
                // entry.floatSlotGO 由 ExecuteHang 的 Destroy(cardSlot.gameObject) 销毁
                RefreshFloatingFlag();
            }

            cards.Remove(card);
            UnbindCardEvents(card);
            if (IsSourceRole())
                CrossHolderSystem.Instance?.UnsubscribeSourceCard(card);
        }

        /// <summary>
        /// 从容器移除卡牌并销毁其槽位（售卖等永久移除时使用）。
        /// 若卡牌正处于浮动状态，同时清理浮动相关 GO。
        /// </summary>
        public void RemoveCardAndCollapse(ItemCard card)
        {
            if (!cards.Contains(card)) return;

            // 浮动状态清理：一并销毁原始槽位和 floatSlotGO
            if (floatingCards.TryGetValue(card, out var entry))
            {
                floatingCards.Remove(card);
                if (entry.originalSlot != null)
                {
                    entry.originalSlot.SetParent(null);
                    Destroy(entry.originalSlot.gameObject);
                }
                if (entry.floatSlotGO != null)
                    Destroy(entry.floatSlotGO);
                RefreshFloatingFlag();

                cards.Remove(card);
                UnbindCardEvents(card);
                if (IsSourceRole())
                    CrossHolderSystem.Instance?.UnsubscribeSourceCard(card);
                return;
            }

            Transform slot = card.transform.parent;
            cards.Remove(card);
            UnbindCardEvents(card);
            if (IsSourceRole())
                CrossHolderSystem.Instance?.UnsubscribeSourceCard(card);
            if (slot != null && slot != transform)
            {
                slot.SetParent(null);
                Destroy(slot.gameObject);
            }
        }

        /// <summary>
        /// 动态调整槽位数量
        /// </summary>
        public void SetSlotCount(int count)
        {
            count = Mathf.Max(0, count);

            int currentChildCount = transform.childCount;
            for (int i = currentChildCount - 1; i >= count; i--)
            {
                Transform slot = transform.GetChild(i);
                ItemCard card  = slot.GetComponentInChildren<ItemCard>();
                if (card != null) RemoveCard(card);
                Destroy(slot.gameObject);
            }

            while (transform.childCount < count)
                Instantiate(slotPrefab, transform);
        }

        #endregion

        #region Cross-Holder Float API

        /// <summary>
        /// 卡牌超过 Y 阈值时由 CrossHolderSystem 调用。
        /// 禁用卡牌原槽位（HorizontalLayoutGroup 自动合并间距），
        /// 将卡牌挂载到 Canvas 根节点下的临时 floatSlotGO（保持世界坐标）。
        /// </summary>
        public void BeginCrossFloat(ItemCard card)
        {
            if (!cards.Contains(card)) return;
            if (floatingCards.ContainsKey(card)) return; // 已在浮动中，忽略

            Transform originalSlot = card.transform.parent;
            if (originalSlot == null || !originalSlot.CompareTag("Slot")) return;

            // 创建临时 floatSlotGO，挂在根 Canvas 下，避免被 LayoutGroup 影响
            // 必须使用 rootCanvas，防止被挂到子级 Canvas（如 HandPanel 自带的 Canvas）下导致定位偏移
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null) canvas = canvas.rootCanvas;
            Transform floatParent = canvas != null ? canvas.transform : null;

            // 使用 RectTransform 创建 floatSlotGO，避免普通 Transform 挂到 Canvas 下时
            // local position 归零（屏幕中心）导致卡牌在重定父级时闪现到屏幕中央
            GameObject floatSlotGO = new GameObject("_FloatSlot_" + card.name);
            RectTransform floatRect = floatSlotGO.AddComponent<RectTransform>();
            floatRect.SetParent(floatParent, false);

            // 先将 floatSlotGO 对齐到卡牌当前世界坐标，再挂卡牌
            // 保证父节点已在正确位置，卡牌以 localPosition=zero 挂入后不会出现一帧位置跳变
            floatRect.position = card.transform.position;
            card.transform.SetParent(floatRect, false);
            card.transform.localPosition = Vector3.zero;

            // 禁用原槽位 → HorizontalLayoutGroup 自动合并
            originalSlot.gameObject.SetActive(false);

            floatingCards[card] = (originalSlot, floatSlotGO);
            isCardFloating = true;

            // 强制 Layout 刷新
            rect.sizeDelta += Vector2.one * 0.01f;
            rect.sizeDelta -= Vector2.one * 0.01f;
        }

        /// <summary>
        /// 卡牌 Y 回落阈值以内时由 CrossHolderSystem 调用。
        /// 恢复原槽位，将卡牌归还（保持世界坐标），销毁 floatSlotGO。
        /// </summary>
        public void EndCrossFloat(ItemCard card)
        {
            if (!floatingCards.TryGetValue(card, out var entry)) return;

            // 恢复原槽位
            entry.originalSlot.gameObject.SetActive(true);

            // 卡牌迁回原槽位，保持世界坐标（拖拽继续跟随鼠标）
            card.transform.SetParent(entry.originalSlot, true);

            // 销毁临时 floatSlotGO
            Destroy(entry.floatSlotGO);

            floatingCards.Remove(card);
            RefreshFloatingFlag();

            // 强制 Layout 刷新
            rect.sizeDelta += Vector2.one * 0.01f;
            rect.sizeDelta -= Vector2.one * 0.01f;
        }

        /// <summary>
        /// 非法落点时调用：先恢复槽位，再触发归位动画。
        /// 覆盖 FishCardHolder.OnCardEndDrag 因 parent 错误而启动的 DOLocalMove。
        /// </summary>
        public void RejoinAndReturn(ItemCard card)
        {
            EndCrossFloat(card);
            card.ReturnToSlot();
        }

        #endregion

        #region Private Helpers

        private void BindCardEvents(ItemCard card)
        {
            card.PointerEnterEvent.AddListener(OnCardPointerEnter);
            card.PointerExitEvent.AddListener(OnCardPointerExit);
            card.BeginDragEvent.AddListener(OnCardBeginDrag);
            card.EndDragEvent.AddListener(OnCardEndDrag);
        }

        private void UnbindCardEvents(ItemCard card)
        {
            card.PointerEnterEvent.RemoveListener(OnCardPointerEnter);
            card.PointerExitEvent.RemoveListener(OnCardPointerExit);
            card.BeginDragEvent.RemoveListener(OnCardBeginDrag);
            card.EndDragEvent.RemoveListener(OnCardEndDrag);
        }

        /// <summary>根据 floatingCards 是否为空同步 isCardFloating 标志。</summary>
        private void RefreshFloatingFlag()
        {
            isCardFloating = floatingCards.Count > 0;
        }

        private void Swap(int index)
        {
            isCrossing = true;

            Transform selectedParent = selectedCard.transform.parent;
            Transform targetParent   = cards[index].transform.parent;

            cards[index].transform.SetParent(selectedParent);
            cards[index].transform.localPosition = cards[index].selected
                ? new Vector3(0, cards[index].selectionOffset, 0)
                : Vector3.zero;

            selectedCard.transform.SetParent(targetParent);

            if (cards[index].cardVisual != null)
            {
                bool swapIsRight = cards[index].ParentIndex() > selectedCard.ParentIndex();
                cards[index].cardVisual.Swap(swapIsRight ? -1 : 1);
            }

            foreach (var card in cards)
            {
                if (card.cardVisual != null)
                    card.cardVisual.UpdateIndex();
            }

            isCrossing = false;
        }

        private void ApplyHoverCompression(ItemCard hoveredCard)
        {
            int n = cards.Count;
            if (n <= 1 || compressionRate <= 0f) return;

            int h = hoveredCard.ParentIndex();
            if (h == n - 1) { ResetCompression(); return; }

            float leftEdgeX    = transform.GetChild(0).localPosition.x;
            float rightEdgeX   = transform.GetChild(n - 1).localPosition.x;
            float normalSpacing = (n > 1) ? (rightEdgeX - leftEdgeX) / (n - 1) : 0f;

            float holderWidth  = rect.rect.width;
            float targetSpacing = (holderWidth - cardWidth) / (n - 1);
            float compressionPerGap = compressionRate * Mathf.Max(0f, normalSpacing - targetSpacing);

            foreach (var card in cards)
            {
                if (card == null) continue;
                int   i = card.ParentIndex();
                float offsetX = i <= h
                    ? -i * compressionPerGap
                    : (n - 1 - i) * compressionPerGap;
                SetCompressionTween(card, offsetX);
            }
        }

        private void ResetCompression()
        {
            foreach (var card in cards)
            {
                if (card != null) SetCompressionTween(card, 0f);
            }
        }

        private void SetCompressionTween(ItemCard card, float targetOffsetX)
        {
            if (compressionTweens.TryGetValue(card, out Tween existing))
                existing?.Kill();

            compressionTweens[card] = card.transform
                .DOLocalMoveX(targetOffsetX, compressionTransition)
                .SetEase(Ease.OutBack);
        }

        #endregion
    }
}
