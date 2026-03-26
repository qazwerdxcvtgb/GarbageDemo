using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using HandSystem;

namespace FishCardSystem
{
    /// <summary>
    /// 手牌面板 UI 管理器
    /// 职责：图层管理、手牌区域折叠/展开动画、订阅 HandManager 同步 Holder 槽位数量
    /// 实际手牌数据由 HandManager 管理，视觉卡牌由 FishCardHolder 管理
    /// </summary>
    public class HandPanelUI : MonoBehaviour
    {
        #region Fields

        [Header("图层设置")]
        [SerializeField] private Canvas panelCanvas;
        [SerializeField] private int sortingOrder = 180;

        [Header("Holder 引用")]
        [SerializeField] private FishCardHolder cardHolder;
        [SerializeField] private RectTransform holderRect;

        [Header("折叠/展开动画")]
        [SerializeField] private float hiddenOffsetY = -300f;
        [SerializeField] private float animDuration = 0.35f;
        [SerializeField] private Ease showEase = Ease.OutCubic;
        [SerializeField] private Ease hideEase = Ease.InCubic;
        [SerializeField] private bool startExpanded = true;

        [Header("按钮区域")]
        [SerializeField] private Button toggleButton;
        [SerializeField] private TextMeshProUGUI toggleButtonText;

        [Header("预留按钮（位置固定，初始隐藏）")]
        [SerializeField] private Button[] extraButtons;

        private bool isExpanded;
        private Vector2 expandedPosition;
        private bool isAnimating;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // 使用自身 Canvas 组件设置图层，避免 panelCanvas 字段误绑定根 Canvas 导致层级混乱
            Canvas ownCanvas = GetComponent<Canvas>();
            if (ownCanvas != null)
            {
                ownCanvas.overrideSorting = true;
                ownCanvas.sortingOrder = sortingOrder;
            }

            // 预留按钮默认隐藏
            if (extraButtons != null)
            {
                foreach (var btn in extraButtons)
                {
                    if (btn != null)
                        btn.gameObject.SetActive(false);
                }
            }
        }

        private void Start()
        {
            // 记录展开位置（Awake 后 RectTransform 已就绪）
            if (holderRect != null)
                expandedPosition = holderRect.anchoredPosition;

            // 绑定折叠/展开按钮
            if (toggleButton != null)
                toggleButton.onClick.AddListener(Toggle);

            // 订阅手牌变化事件
            if (HandManager.Instance != null)
                HandManager.Instance.OnHandChanged += OnHandChanged;

            // 初始化展开/收起状态
            isExpanded = startExpanded;
            if (!startExpanded && holderRect != null)
                holderRect.anchoredPosition = expandedPosition + Vector2.up * hiddenOffsetY;

            // 刷新按钮文本
            RefreshToggleText();
        }

        private void OnDestroy()
        {
            if (HandManager.Instance != null)
                HandManager.Instance.OnHandChanged -= OnHandChanged;

            DOTween.Kill(holderRect);
        }

        #endregion

        #region Public API

        /// <summary>
        /// 展开手牌区域
        /// </summary>
        public void Show()
        {
            if (isExpanded || isAnimating || holderRect == null)
                return;

            isExpanded = true;
            isAnimating = true;

            holderRect.DOAnchorPos(expandedPosition, animDuration)
                .SetEase(showEase)
                .OnComplete(() => isAnimating = false);

            RefreshToggleText();
        }

        /// <summary>
        /// 收起手牌区域
        /// </summary>
        public void Hide()
        {
            if (!isExpanded || isAnimating || holderRect == null)
                return;

            isExpanded = false;
            isAnimating = true;

            Vector2 hiddenPosition = expandedPosition + Vector2.up * hiddenOffsetY;
            holderRect.DOAnchorPos(hiddenPosition, animDuration)
                .SetEase(hideEase)
                .OnComplete(() => isAnimating = false);

            RefreshToggleText();
        }

        /// <summary>
        /// 切换展开/收起状态
        /// </summary>
        public void Toggle()
        {
            if (isExpanded)
                Hide();
            else
                Show();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 当 HandManager 手牌变化时，同步 Holder 的槽位数量
        /// </summary>
        private void OnHandChanged()
        {
            if (cardHolder == null || HandManager.Instance == null)
                return;

            int count = HandManager.Instance.GetHandCount();
            cardHolder.SetSlotCount(count);
            RefreshToggleText();
        }

        /// <summary>
        /// 刷新折叠/展开按钮的文本，显示当前手牌数量和状态
        /// </summary>
        private void RefreshToggleText()
        {
            if (toggleButtonText == null)
                return;

            int count = HandManager.Instance != null ? HandManager.Instance.GetHandCount() : 0;
            string stateLabel = isExpanded ? "▼" : "▲";
            toggleButtonText.text = $"手牌 {count}  {stateLabel}";
        }

        #endregion
    }
}
