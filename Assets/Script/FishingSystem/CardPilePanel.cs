using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ItemSystem;
using FishCardSystem;

namespace FishingSystem
{
    /// <summary>
    /// 牌堆交互面板预制体控制器
    /// 单击牌堆时由 CardPile 实例化，根据顶牌揭示状态切换 FaceDown/FaceUp 视图
    /// FaceDown：展示卡背 + 揭示按钮；FaceUp：展示卡面 + 捕获按钮
    /// </summary>
    public class CardPilePanel : MonoBehaviour
    {
        #region Fields

        [Header("卡背显示 (FaceDown)")]
        [SerializeField] private GameObject cardBackView;
        [SerializeField] private Image cardBackImage;
        [SerializeField] private Sprite smallBackSprite;
        [SerializeField] private Sprite mediumBackSprite;
        [SerializeField] private Sprite largeBackSprite;

        [Header("卡面显示 (FaceUp)")]
        [SerializeField] private GameObject cardFaceView;
        [SerializeField] private FishCardHolder cardHolder;
        [SerializeField] private GameObject fishCardPrefab;

        [Header("按钮")]
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button revealButton;   // FaceDown 状态下可见
        [SerializeField] private Button captureButton;  // FaceUp 状态下可见
        [SerializeField] private Button abandonButton;  // 仅在刚揭示时可见

        [Header("显示参数")]
        [SerializeField][Range(0.5f, 3f)] private float displayScale = 1.5f;

        private CardPile targetPile;
        private ItemCard displayCard;
        private FishData currentTopCard;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            // 确保 Canvas 排序层高于根 Canvas（150），使面板渲染在牌堆之上
            Canvas selfCanvas = GetComponent<Canvas>();
            if (selfCanvas != null)
            {
                selfCanvas.overrideSorting = true;
                selfCanvas.sortingOrder    = 160;
            }

            // overrideSorting 的嵌套 Canvas 必须有自己的 GraphicRaycaster 才能处理输入和拦截射线
            if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            if (cancelButton  != null) cancelButton.onClick.AddListener(OnCancelClicked);
            if (revealButton  != null) revealButton.onClick.AddListener(OnRevealClicked);
            if (captureButton != null) captureButton.onClick.AddListener(OnCaptureClicked);
            if (abandonButton != null) abandonButton.onClick.AddListener(OnAbandonClicked);
        }

        private void OnDestroy()
        {
            if (cancelButton  != null) cancelButton.onClick.RemoveListener(OnCancelClicked);
            if (revealButton  != null) revealButton.onClick.RemoveListener(OnRevealClicked);
            if (captureButton != null) captureButton.onClick.RemoveListener(OnCaptureClicked);
            if (abandonButton != null) abandonButton.onClick.RemoveListener(OnAbandonClicked);
        }

        #endregion

        #region Public API

        /// <summary>
        /// 打开面板，由 CardPile.OnPointerClick 调用
        /// </summary>
        public void Show(CardPile pile)
        {
            if (pile == null)
            {
                Debug.LogWarning("[CardPilePanel] Show：pile 为空，关闭面板");
                ClosePanel();
                return;
            }

            targetPile     = pile;
            currentTopCard = pile.GetTopCard();

            if (currentTopCard == null)
            {
                Debug.LogWarning("[CardPilePanel] Show：牌堆无顶牌，关闭面板");
                ClosePanel();
                return;
            }

            if (pile.State == PileState.FaceUp)
                ShowFaceUp(currentTopCard, isJustRevealed: false); // 再次打开，不显示放弃
            else
                ShowFaceDown(currentTopCard);
        }

        #endregion

        #region Display

        private void ShowFaceDown(FishData data)
        {
            if (cardBackView != null) cardBackView.SetActive(true);
            if (cardFaceView != null) cardFaceView.SetActive(false);

            if (cardBackImage != null)
            {
                cardBackImage.sprite = GetBackSprite(data.size);
                cardBackImage.color  = Color.white;
            }

            if (cardBackView != null)
                cardBackView.transform.localScale = Vector3.one * displayScale;

            // 未揭示：显示揭示+取消，隐藏捕获+放弃
            SetButtonVisibility(showReveal: true, showCapture: false, showAbandon: false);
        }

        /// <summary>
        /// 显示卡面视图
        /// </summary>
        /// <param name="data">顶牌数据</param>
        /// <param name="isJustRevealed">
        /// true = 本次操作刚揭示（隐藏取消，显示放弃）；
        /// false = 再次打开已揭示的牌堆（显示取消，隐藏放弃）
        /// </param>
        private void ShowFaceUp(FishData data, bool isJustRevealed = false)
        {
            if (cardBackView != null) cardBackView.SetActive(false);
            if (cardFaceView != null) cardFaceView.SetActive(true);

            if (cardFaceView != null)
                cardFaceView.transform.localScale = Vector3.one * displayScale;

            SetButtonVisibility(showReveal: false, showCapture: true, showAbandon: isJustRevealed);
            UpdateCaptureButtonState();

            StartCoroutine(AddCardToHolderNextFrame(data));
        }

        /// <summary>
        /// yield return null 等待一帧，确保 FishCardHolder.Start() 已执行完槽位生成，
        /// 再实例化 FishCard 并通过 AddCard 注册到 holder
        /// </summary>
        private IEnumerator AddCardToHolderNextFrame(FishData data)
        {
            yield return null;

            if (fishCardPrefab == null)
            {
                Debug.LogWarning("[CardPilePanel] fishCardPrefab 未设置，无法显示卡面");
                yield break;
            }

            // 优先挂在 cardHolder 下，否则直接挂在 cardFaceView 下
            Transform parent = cardHolder != null
                ? cardHolder.transform
                : cardFaceView?.transform;

            if (parent == null) yield break;

            // 实例化 FishCard 并初始化
            GameObject cardObj = Instantiate(fishCardPrefab, parent);
            displayCard = cardObj.GetComponent<ItemCard>();

            if (displayCard is FishCard fishCard)
            {
                // 视觉卡锚定在 cardFaceView 内（与 FishCardHolder 同父级），便于层级管理和 Mask 剪裁
                fishCard.visualParentOverride = cardFaceView != null ? cardFaceView.transform : transform;
                fishCard.Initialize(data);
                fishCard.SetContextMode(CardContextMode.Pile);
            }

            // 注册到 holder（AddCard 会找到空槽并将卡牌移入，同时绑定事件）
            if (cardHolder != null && displayCard != null)
                cardHolder.AddCard(displayCard);
        }

        private Sprite GetBackSprite(FishSize size)
        {
            switch (size)
            {
                case FishSize.Small:  return smallBackSprite;
                case FishSize.Medium: return mediumBackSprite;
                case FishSize.Large:  return largeBackSprite;
                default:              return mediumBackSprite;
            }
        }

        /// <summary>
        /// 根据玩家当前体力更新捕获按钮的可交互状态：体力不足时置灰
        /// </summary>
        private void UpdateCaptureButtonState()
        {
            if (captureButton == null) return;
            bool canCapture = FishingTableManager.Instance != null
                && FishingTableManager.Instance.CanAffordCapture(currentTopCard);
            captureButton.interactable = canCapture;
        }

        private void SetButtonVisibility(bool showReveal, bool showCapture, bool showAbandon)
        {
            if (revealButton  != null) revealButton.gameObject.SetActive(showReveal);
            if (captureButton != null) captureButton.gameObject.SetActive(showCapture);
            if (abandonButton != null) abandonButton.gameObject.SetActive(showAbandon);
            // 取消按钮与放弃按钮互斥：放弃显示时隐藏取消
            if (cancelButton  != null) cancelButton.gameObject.SetActive(!showAbandon);
        }

        private void ClearDisplayCard()
        {
            if (displayCard != null)
            {
                if (cardHolder != null)
                    cardHolder.RemoveCard(displayCard);

                Destroy(displayCard.gameObject);
                displayCard = null;
            }
        }

        #endregion

        #region Button Handlers

        private void OnRevealClicked()
        {
            if (targetPile == null || currentTopCard == null) return;

            if (FishingTableManager.Instance == null)
            {
                Debug.LogError("[CardPilePanel] FishingTableManager 不存在，无法执行翻牌");
                return;
            }

            bool success = FishingTableManager.Instance.TryReveal(targetPile, currentTopCard);
            if (!success) return;

            targetPile.Reveal();

            ClearDisplayCard();
            // isJustRevealed=true：隐藏取消，显示放弃
            ShowFaceUp(currentTopCard, isJustRevealed: true);
        }

        private void OnCaptureClicked()
        {
            if (targetPile == null || currentTopCard == null) return;

            if (FishingTableManager.Instance == null)
            {
                Debug.LogError("[CardPilePanel] FishingTableManager 不存在，无法执行捕获");
                return;
            }

            // 由 FishingTableManager 负责体力检查、扣除、效果触发、加入手牌、移除顶牌
            bool success = FishingTableManager.Instance.TryCapture(targetPile, currentTopCard);
            if (!success)
            {
                // TODO: 可在此显示"体力不足"UI 提示
                return;
            }

            ClosePanel();
        }

        private void OnAbandonClicked()
        {
            Debug.Log("[CardPilePanel] 放弃捕获，抽取杂鱼卡");
            if (FishingTableManager.Instance != null)
                FishingTableManager.Instance.TryAbandon(targetPile);
            ClosePanel();
        }

        private void OnCancelClicked()
        {
            ClosePanel();
        }

        private void ClosePanel()
        {
            ClearDisplayCard();
            Destroy(gameObject);
        }

        #endregion
    }
}
