using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ItemSystem;
using FishCardSystem;

namespace FishingSystem
{
    /// <summary>
    /// 揭示遮罩层控制器
    /// 显示放大的卡牌详情和操作按钮
    /// </summary>
    public class RevealOverlayPanel : MonoBehaviour
    {
        #region Fields

        [Header("UI组件")]
        [SerializeField] private GameObject overlayRoot;         // 遮罩根对象
        [SerializeField] private Image backgroundMask;           // 半透明背景
        [SerializeField] private Transform cardDisplayArea;      // 卡牌放大显示区域
        [SerializeField] private Button captureButton;           // 捕获按钮
        [SerializeField] private Button abandonButton;           // 放弃按钮
        [SerializeField] private Button cancelButton;            // 取消按钮
        [SerializeField] private TextMeshProUGUI captureButtonText;  // 捕获按钮文本
        [SerializeField] private TextMeshProUGUI staminaCostText;    // 体力消耗显示

        [Header("预制体")]
        [SerializeField] private GameObject displayCardPrefab;   // 放大显示用的卡牌预制体

        [Header("显示设置")]
        [SerializeField] private float displayCardScale = 1.5f;  // 放大卡牌的缩放

        [Header("调试")]
        [SerializeField] private bool showDebugInfo = true;

        // 当前状态
        private CardPileSlot currentSlot;        // 当前操作的槽位
        private FishCard displayCard;            // 放大显示的卡牌实例
        private bool isFirstReveal;              // 是否首次揭示（决定显示放弃还是取消）

        #endregion

        #region Events

        /// <summary>
        /// 捕获按钮点击事件
        /// </summary>
        public event System.Action OnCaptureClicked;

        /// <summary>
        /// 放弃按钮点击事件
        /// </summary>
        public event System.Action OnAbandonClicked;

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        public event System.Action OnCancelClicked;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // 绑定按钮事件
            if (captureButton != null)
            {
                captureButton.onClick.AddListener(OnCaptureButtonClicked);
            }

            if (abandonButton != null)
            {
                abandonButton.onClick.AddListener(OnAbandonButtonClicked);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelButtonClicked);
            }

            // 初始隐藏
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(false);
            }
        }

        #endregion

        #region Display Management

        /// <summary>
        /// 显示遮罩
        /// </summary>
        /// <param name="slot">操作的槽位</param>
        /// <param name="isFirstReveal">是否首次揭示</param>
        public void Show(CardPileSlot slot, bool isFirstReveal)
        {
            if (slot == null || slot.currentCard == null)
            {
                Debug.LogError("[RevealOverlayPanel] 槽位或卡牌为空，无法显示遮罩");
                return;
            }

            currentSlot = slot;
            this.isFirstReveal = isFirstReveal;

            // 创建放大显示的卡牌
            CreateDisplayCard(slot.currentCard.cardData);

            // 根据是否首次揭示显示不同按钮
            if (abandonButton != null)
            {
                abandonButton.gameObject.SetActive(isFirstReveal);
            }

            if (cancelButton != null)
            {
                cancelButton.gameObject.SetActive(!isFirstReveal);
            }

            // 更新体力消耗显示
            if (staminaCostText != null)
            {
                staminaCostText.text = $"消耗体力: {slot.currentCard.cardData.staminaCost}";
            }

            // 显示遮罩
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(true);
            }

            if (showDebugInfo)
            {
                Debug.Log($"[RevealOverlayPanel] 显示遮罩：{slot.currentCard.cardData.itemName}, 首次揭示={isFirstReveal}");
            }
        }

        /// <summary>
        /// 隐藏遮罩
        /// </summary>
        public void Hide()
        {
            // 销毁放大卡牌
            DestroyDisplayCard();

            // 隐藏遮罩
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(false);
            }

            currentSlot = null;

            if (showDebugInfo)
            {
                Debug.Log("[RevealOverlayPanel] 隐藏遮罩");
            }
        }

        /// <summary>
        /// 更新捕获按钮状态
        /// </summary>
        /// <param name="playerStamina">玩家当前体力</param>
        public void UpdateCaptureButtonState(int playerStamina)
        {
            if (captureButton == null || currentSlot == null || currentSlot.currentCard == null)
            {
                return;
            }

            int requiredStamina = currentSlot.currentCard.cardData.staminaCost;
            bool canCapture = playerStamina >= requiredStamina;

            // 更新按钮可交互状态
            captureButton.interactable = canCapture;

            // 更新按钮文本
            if (captureButtonText != null)
            {
                if (canCapture)
                {
                    captureButtonText.text = "捕获";
                }
                else
                {
                    captureButtonText.text = $"捕获 (体力不足)";
                }
            }
        }

        #endregion

        #region Card Display

        /// <summary>
        /// 创建放大显示的卡牌
        /// </summary>
        private void CreateDisplayCard(FishData data)
        {
            if (displayCardPrefab == null || cardDisplayArea == null)
            {
                Debug.LogError("[RevealOverlayPanel] DisplayCardPrefab 或 CardDisplayArea 未分配");
                return;
            }

            // 销毁旧卡牌
            DestroyDisplayCard();

            // 实例化新卡牌
            GameObject cardObj = Instantiate(displayCardPrefab, cardDisplayArea);
            cardObj.name = $"DisplayCard_{data.itemName}";

            displayCard = cardObj.GetComponent<FishCard>();
            if (displayCard == null)
            {
                Debug.LogError("[RevealOverlayPanel] DisplayCardPrefab 缺少 FishCard 组件");
                Destroy(cardObj);
                return;
            }

            // 初始化卡牌
            displayCard.Initialize(data);

            // 设置为牌堆模式（禁用交互）
            displayCard.SetPileMode(true);

            // 设置缩放
            cardObj.transform.localScale = Vector3.one * displayCardScale;

            // 重置位置
            cardObj.transform.localPosition = Vector3.zero;
        }

        /// <summary>
        /// 销毁放大显示的卡牌
        /// </summary>
        private void DestroyDisplayCard()
        {
            if (displayCard != null)
            {
                Destroy(displayCard.gameObject);
                displayCard = null;
            }
        }

        #endregion

        #region Button Callbacks

        private void OnCaptureButtonClicked()
        {
            OnCaptureClicked?.Invoke();
        }

        private void OnAbandonButtonClicked()
        {
            OnAbandonClicked?.Invoke();
        }

        private void OnCancelButtonClicked()
        {
            OnCancelClicked?.Invoke();
        }

        #endregion
    }
}
