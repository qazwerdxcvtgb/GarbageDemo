using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using ItemSystem;
using FishCardSystem;

namespace FishingSystem
{
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

        [Header("面板")]
        [SerializeField] private GameObject cardPilePanelPrefab;

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

        #endregion

        #region Public API

        /// <summary>
        /// 注入卡序，初始化牌堆（自动设为 FaceDown）
        /// </summary>
        public void SetCards(List<FishData> cardList)
        {
            cards = new List<FishData>(cardList);
            currentState = cards.Count > 0 ? PileState.FaceDown : PileState.Empty;
            RefreshDisplay();
        }

        /// <summary>
        /// 获取顶牌（只读，不移除）
        /// </summary>
        public FishData GetTopCard() => cards.Count > 0 ? cards[0] : null;

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
            RefreshDisplay();

            return removed;
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
                    break;

                case PileState.FaceDown:
                    SetCardFaceVisible(false);
                    ShowCardBack();
                    break;

                case PileState.FaceUp:
                    SetCardBackVisible(false);
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
                currentDisplayCard.SetPileMode(true);
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

        private void UpdateThickness()
        {
            if (thicknessDisplay != null)
                thicknessDisplay.UpdateThickness(cards.Count);
        }

        #endregion

        #region Input

        public void OnPointerClick(PointerEventData eventData)
        {
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
