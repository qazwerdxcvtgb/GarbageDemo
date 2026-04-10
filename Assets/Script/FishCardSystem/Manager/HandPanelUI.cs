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
        [SerializeField] private GameObject fishCardPrefab;
        [SerializeField] private GameObject trashCardPrefab;
        [SerializeField] private GameObject consumableCardPrefab;
        [SerializeField] private GameObject equipmentCardPrefab;

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
        private bool lockedExpanded;
        private bool lockedCollapsed;

        // 供外部系统（如商店）访问 Holder 引用
        public FishCardHolder CardHolder => cardHolder;

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
            {
                HandManager.Instance.OnHandChanged += OnHandChanged;
                HandManager.Instance.OnCardAdded   += OnCardAdded;
            }

            // 注册手牌区域为 CrossHolderSystem 的手牌归还区域
            // holderRect 作为槽位卡"拖回手牌"的命中检测矩形
            if (holderRect != null)
                CrossHolderSystem.Instance?.RegisterHandZone(holderRect);

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
            {
                HandManager.Instance.OnHandChanged -= OnHandChanged;
                HandManager.Instance.OnCardAdded   -= OnCardAdded;
            }

            CrossHolderSystem.Instance?.UnregisterHandZone();
            DOTween.Kill(holderRect);
        }

        #endregion

        #region Public API

        /// <summary>
        /// 展开手牌区域
        /// </summary>
        public void Show()
        {
            if (isExpanded || isAnimating || holderRect == null || lockedCollapsed)
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
            if (lockedExpanded || lockedCollapsed) return;
            if (isExpanded)
                Hide();
            else
                Show();
        }

        /// <summary>
        /// 锁定展开状态（商店打开时调用）：强制展开、隐藏 ToggleButton、禁止折叠
        /// </summary>
        public void LockExpanded()
        {
            lockedExpanded = true;
            if (toggleButton != null)
                toggleButton.gameObject.SetActive(false);
            Show();
        }

        /// <summary>
        /// 解除锁定（商店关闭时调用）：恢复 ToggleButton 显示
        /// </summary>
        public void UnlockExpanded()
        {
            lockedExpanded = false;
            if (toggleButton != null)
                toggleButton.gameObject.SetActive(true);
        }

        /// <summary>
        /// 锁定收起状态（偷看牌堆时调用）：强制收起、隐藏 ToggleButton、禁止展开
        /// </summary>
        public void LockCollapsed()
        {
            lockedCollapsed = true;
            if (toggleButton != null)
                toggleButton.gameObject.SetActive(false);
            Hide();
        }

        /// <summary>
        /// 解除收起锁定（偷看结束时调用）：恢复 ToggleButton 显示
        /// </summary>
        public void UnlockCollapsed()
        {
            lockedCollapsed = false;
            if (toggleButton != null)
                toggleButton.gameObject.SetActive(true);
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
        /// 当 HandManager 新增卡牌时，在 FishCardHolder 中实例化对应的视觉卡并自动展开手牌面板。
        /// 此事件在 OnHandChanged（槽位扩容）之后触发，确保有空槽可用。
        /// </summary>
        private void OnCardAdded(ItemSystem.ItemData item)
        {
            if (cardHolder == null) return;

            if (item is ItemSystem.FishData fishData && fishCardPrefab != null)
            {
                GameObject cardObj = Instantiate(fishCardPrefab);
                FishCard fishCard  = cardObj.GetComponent<FishCard>();
                if (fishCard == null) { Destroy(cardObj); return; }

                fishCard.Initialize(fishData);
                fishCard.SetContextMode(CardContextMode.Hand);
                cardHolder.AddCard(fishCard);
                Show();
            }
            else if (item is ItemSystem.TrashData trashData && trashCardPrefab != null)
            {
                GameObject cardObj  = Instantiate(trashCardPrefab);
                TrashCard trashCard = cardObj.GetComponent<TrashCard>();
                if (trashCard == null) { Destroy(cardObj); return; }

                trashCard.Initialize(trashData);
                cardHolder.AddCard(trashCard);
                Show();
            }
            else if (item is ItemSystem.ConsumableData consumableData && consumableCardPrefab != null)
            {
                var cardObj       = Instantiate(consumableCardPrefab);
                var consumableCard = cardObj.GetComponent<ConsumableCard>();
                if (consumableCard == null) { Destroy(cardObj); return; }

                consumableCard.Initialize(consumableData);
                cardHolder.AddCard(consumableCard);
                Show();
            }
            else if (item is ItemSystem.EquipmentData equipmentData && equipmentCardPrefab != null)
            {
                var cardObj       = Instantiate(equipmentCardPrefab);
                var equipmentCard = cardObj.GetComponent<EquipmentCard>();
                if (equipmentCard == null) { Destroy(cardObj); return; }

                equipmentCard.Initialize(equipmentData);
                cardHolder.AddCard(equipmentCard);
                Show();
            }
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
