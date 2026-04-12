using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using ItemSystem;
using FishCardSystem;

namespace FishingSystem
{
    /// <summary>
    /// 牌堆状态
    /// </summary>
    public enum PileState
    {
        Empty,      // 空牌堆
        FaceDown,   // 卡背朝上（未翻开）
        FaceUp      // 正面朝上（已翻开）
    }

    /// <summary>
    /// 独立牌堆预制体控制器
    /// 自持卡序，管理 FaceDown/FaceUp/Empty 三种显示状态
    /// 交互通过 OnPileClicked 事件上报，不包含游戏逻辑
    /// </summary>
    public class CardPile : MonoBehaviour, IPointerClickHandler
    {
        #region Fields

        [Header("预制体")]
        [SerializeField] private GameObject fishCardPrefab;

        [Header("视觉 - 卡背容器")]
        [SerializeField] private GameObject cardBackContainer;
        [SerializeField] private GameObject smallCardBack;
        [SerializeField] private GameObject mediumCardBack;
        [SerializeField] private GameObject largeCardBack;

        [Header("视觉 - 卡面容器")]
        [SerializeField] private Transform cardFaceContainer;

        [Header("厚度显示")]
        [SerializeField] private PileThicknessDisplay thicknessDisplay;

        [Header("视觉 - 空牌堆")]
        [SerializeField] private GameObject emptyView;

        [Header("面板")]
        [SerializeField] private GameObject cardPilePanelPrefab;

        [Header("深度配置")]
        [Tooltip("该牌堆的深度等级，玩家深度需 >= 此值才能交互")]
        [SerializeField] private FishDepth pileDepth = FishDepth.Depth1;

        [Header("状态（只读调试）")]
        [SerializeField] private PileState currentState = PileState.Empty;

        // 自持卡序
        private List<FishData> cards = new List<FishData>();

        // 当前实例化的展示卡牌
        private FishCard currentDisplayCard;

        #endregion

        #region Events

        /// <summary>
        /// 牌堆被点击时触发，上层控制器订阅此事件处理游戏逻辑
        /// </summary>
        public event Action<CardPile> OnPileClicked;

        #endregion

        #region Properties

        /// <summary>当前状态</summary>
        public PileState State => currentState;

        /// <summary>当前牌堆张数</summary>
        public int CardCount => cards.Count;

        /// <summary>该牌堆的深度等级</summary>
        public FishDepth PileDepth => pileDepth;

        /// <summary>未揭示的卡牌数量（FaceUp 状态下排除已揭示的顶牌）</summary>
        public int UnrevealedCardCount =>
            currentState == PileState.FaceUp ? Mathf.Max(0, cards.Count - 1) : cards.Count;

        /// <summary>
        /// 静态点击拦截器。非 null 时接管 OnPointerClick，跳过深度检查和默认面板。
        /// 使用完毕后调用方须置 null 恢复正常行为。
        /// </summary>
        public static Action<CardPile> ClickInterceptor;

        #endregion

        #region Public API

        /// <summary>
        /// 设置牌堆深度等级（由 FishingTableManager 在初始化时调用）
        /// </summary>
        public void SetDepth(FishDepth depth)
        {
            pileDepth = depth;
        }

        /// <summary>
        /// 注入卡序，初始化牌堆（自动设为 FaceDown）
        /// </summary>
        public void SetCards(List<FishData> cardList)
        {
            cards = new List<FishData>(cardList);
            currentState = cards.Count > 0 ? PileState.FaceDown : PileState.Empty;
            if (currentState == PileState.Empty) OnPileBecameEmpty();
            RefreshDisplay();
        }

        /// <summary>
        /// 获取顶牌（只读，不移除）
        /// </summary>
        public FishData GetTopCard() => cards.Count > 0 ? cards[0] : null;

        /// <summary>
        /// 牌堆变空时触发，预留扩展点（当前为空实现）
        /// </summary>
        protected virtual void OnPileBecameEmpty() { }

        /// <summary>
        /// 翻开顶牌：FaceDown → FaceUp
        /// </summary>
        public void Reveal()
        {
            if (currentState == PileState.FaceDown)
            {
                currentState = PileState.FaceUp;
                RefreshDisplay();
            }
        }

        /// <summary>
        /// 移除顶牌并刷新显示，返回被移除的卡牌数据
        /// </summary>
        public FishData RemoveTopCard()
        {
            if (cards.Count == 0) return null;

            FishData removed = cards[0];
            cards.RemoveAt(0);
            currentState = cards.Count > 0 ? PileState.FaceDown : PileState.Empty;
            if (currentState == PileState.Empty) OnPileBecameEmpty();
            RefreshDisplay();

            return removed;
        }

        /// <summary>
        /// 从顶部移除并返回最多 count 张卡牌。
        /// 牌堆不足时返回实际可用数量。操作完成后自动更新状态和刷新显示。
        /// </summary>
        public List<FishData> DrawTopCards(int count)
        {
            int actual = Mathf.Min(count, cards.Count);
            var result = new List<FishData>(actual);
            for (int i = 0; i < actual; i++)
            {
                result.Add(cards[0]);
                cards.RemoveAt(0);
            }

            currentState = cards.Count > 0 ? PileState.FaceDown : PileState.Empty;
            if (currentState == PileState.Empty) OnPileBecameEmpty();
            RefreshDisplay();

            return result;
        }

        /// <summary>
        /// 将卡牌列表插入牌堆顶部（保持传入顺序）。
        /// 例如传入 [A, B, C]，结果牌堆顶部为 A, B, C, ...原有牌...
        /// 操作完成后自动更新状态和刷新显示。
        /// </summary>
        public void InsertCardsAtTop(List<FishData> newCards)
        {
            if (newCards == null || newCards.Count == 0) return;

            cards.InsertRange(0, newCards);
            currentState = PileState.FaceDown;
            RefreshDisplay();
        }

        /// <summary>
        /// 将卡牌插入牌堆的随机位置（含顶部和底部），状态强制为 FaceDown。
        /// 用于放弃捕获后将鱼卡洗回牌堆。
        /// </summary>
        public void InsertCardAtRandom(FishData card)
        {
            if (card == null) return;
            int index = UnityEngine.Random.Range(0, cards.Count + 1);
            cards.Insert(index, card);
            currentState = PileState.FaceDown;
            RefreshDisplay();
        }

        /// <summary>
        /// 偷看顶部卡牌（只读，不移除不改变状态）。
        /// skipRevealed=true 时跳过已揭示的顶牌（FaceUp 状态下从第二张开始）。
        /// </summary>
        public List<FishData> PeekTopCards(int count, bool skipRevealed = true)
        {
            int startIndex = (skipRevealed && currentState == PileState.FaceUp) ? 1 : 0;
            int available = cards.Count - startIndex;
            int actual = Mathf.Min(count, Mathf.Max(0, available));
            if (actual <= 0) return new List<FishData>();
            return new List<FishData>(cards.GetRange(startIndex, actual));
        }

        /// <summary>
        /// 强制刷新所有视觉显示
        /// </summary>
        public void RefreshDisplay()
        {
            ClearDisplayCard();
            UpdateThickness();

            switch (currentState)
            {
                case PileState.Empty:
                    SetCardBackVisible(false);
                    SetCardFaceVisible(false);
                    SetEmptyViewVisible(true);
                    break;

                case PileState.FaceDown:
                    SetCardFaceVisible(false);
                    SetEmptyViewVisible(false);
                    ShowCardBack();
                    break;

                case PileState.FaceUp:
                    SetCardBackVisible(false);
                    SetEmptyViewVisible(false);
                    ShowCardFace();
                    break;
            }
        }

        #endregion

        #region Private Display

        private void ShowCardBack()
        {
            FishData top = GetTopCard();
            if (top == null) return;

            SetCardBackVisible(true);

            if (smallCardBack != null)  smallCardBack.SetActive(top.size == FishSize.Small);
            if (mediumCardBack != null) mediumCardBack.SetActive(top.size == FishSize.Medium);
            if (largeCardBack != null)  largeCardBack.SetActive(top.size == FishSize.Large);
        }

        private void ShowCardFace()
        {
            FishData top = GetTopCard();
            if (top == null || fishCardPrefab == null || cardFaceContainer == null) return;

            SetCardFaceVisible(true);

            GameObject cardObj = Instantiate(fishCardPrefab, cardFaceContainer);
            cardObj.transform.localPosition = Vector3.zero;
            cardObj.transform.localScale = Vector3.one;

            currentDisplayCard = cardObj.GetComponent<FishCard>();
            if (currentDisplayCard != null)
            {
                // 视觉卡锚定在牌堆内部，不进入 VisualCardsHandler，保证层级在面板之下
                currentDisplayCard.visualParentOverride = cardFaceContainer;
                currentDisplayCard.Initialize(top);
                currentDisplayCard.SetContextMode(CardContextMode.Pile);
            }
        }

        private void ClearDisplayCard()
        {
            if (currentDisplayCard != null)
            {
                Destroy(currentDisplayCard.gameObject);
                currentDisplayCard = null;
            }
        }

        private void SetCardBackVisible(bool visible)
        {
            if (cardBackContainer != null)
                cardBackContainer.SetActive(visible);
        }

        private void SetCardFaceVisible(bool visible)
        {
            if (cardFaceContainer != null)
                cardFaceContainer.gameObject.SetActive(visible);
        }

        private void SetEmptyViewVisible(bool visible)
        {
            if (emptyView != null) emptyView.SetActive(visible);
        }

        private void UpdateThickness()
        {
            if (thicknessDisplay != null)
                thicknessDisplay.UpdateThickness(cards.Count);
        }

        #endregion

        #region Input

        public void OnPointerClick(PointerEventData eventData)
        {
            if (ClickInterceptor != null)
            {
                ClickInterceptor.Invoke(this);
                return;
            }

            // 玩家深度不足时拦截点击，不触发任何交互
            if (FishingTableManager.Instance != null && !FishingTableManager.Instance.CanPlayerAccessPile(this))
            {
                Debug.Log($"[CardPile] 玩家深度不足，无法与深度 {pileDepth} 的牌堆交互");
                return;
            }

            OnPileClicked?.Invoke(this);

            // 若配置了交互面板预制体且牌堆非空，则实例化面板并显示
            if (cardPilePanelPrefab == null || cards.Count == 0) return;

            Canvas rootCanvas = GetComponentInParent<Canvas>();
            Transform panelParent = rootCanvas != null ? rootCanvas.transform : transform.parent;

            CardPilePanel panel = Instantiate(cardPilePanelPrefab, panelParent)
                                      .GetComponentInChildren<CardPilePanel>(true);
            if (panel != null)
                panel.Show(this);
        }

        #endregion
    }
}
