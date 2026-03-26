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

        [Header("显示参数")]
        [SerializeField][Range(0.5f, 3f)] private float displayScale = 1.5f;

        private CardPile targetPile;
        private FishCard displayCard;
        private FishData currentTopCard;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            if (cancelButton  != null) cancelButton.onClick.AddListener(OnCancelClicked);
            if (revealButton  != null) revealButton.onClick.AddListener(OnRevealClicked);
            if (captureButton != null) captureButton.onClick.AddListener(OnCaptureClicked);
        }

        private void OnDestroy()
        {
            if (cancelButton  != null) cancelButton.onClick.RemoveListener(OnCancelClicked);
            if (revealButton  != null) revealButton.onClick.RemoveListener(OnRevealClicked);
            if (captureButton != null) captureButton.onClick.RemoveListener(OnCaptureClicked);
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
                ShowFaceUp(currentTopCard);
            else
                ShowFaceDown(currentTopCard);
        }

        #endregion

        #region Display

        private void ShowFaceDown(FishData data)
        {
            if (cardBackView != null) cardBackView.SetActive(true);
            if (cardFaceView != null) cardFaceView.SetActive(false);

            // 按鱼类尺寸选取卡背 Sprite，并确保 alpha 可见
            if (cardBackImage != null)
            {
                cardBackImage.sprite = GetBackSprite(data.size);
                cardBackImage.color  = Color.white;
            }

            // 对卡背视图整体放大
            if (cardBackView != null)
                cardBackView.transform.localScale = Vector3.one * displayScale;

            SetButtonVisibility(showReveal: true, showCapture: false);
        }

        private void ShowFaceUp(FishData data)
        {
            if (cardBackView != null) cardBackView.SetActive(false);
            if (cardFaceView != null) cardFaceView.SetActive(true);

            // 对卡面容器整体放大
            if (cardFaceView != null)
                cardFaceView.transform.localScale = Vector3.one * displayScale;

            SetButtonVisibility(showReveal: false, showCapture: true);

            // 等待 FishCardHolder.Start() 完成槽位生成后再添加卡牌
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
            displayCard = cardObj.GetComponent<FishCard>();

            if (displayCard != null)
            {
                displayCard.Initialize(data);
                displayCard.SetPileMode(true);
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

        private void SetButtonVisibility(bool showReveal, bool showCapture)
        {
            if (revealButton  != null) revealButton.gameObject.SetActive(showReveal);
            if (captureButton != null) captureButton.gameObject.SetActive(showCapture);
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

            // 触发揭示效果
            currentTopCard.TriggerRevealEffects();

            // 通知牌堆翻牌（FaceDown → FaceUp），更新牌堆本体视觉
            targetPile.Reveal();

            // 切换面板视图至卡面
            ClearDisplayCard();
            ShowFaceUp(currentTopCard);
        }

        private void OnCaptureClicked()
        {
            if (currentTopCard == null) return;

            // 触发捕获效果
            currentTopCard.TriggerCaptureEffects();

            // TODO: 加入手牌（占位，待手牌系统接入）
            // 示例：HandCardHolder.Instance.AddCard(currentTopCard);

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
