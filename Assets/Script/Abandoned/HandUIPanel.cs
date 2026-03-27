/// <summary>
/// 手牌UI面板控制器
/// 管理手牌显示、动画和交互逻辑
/// 创建日期：2026-01-21
/// </summary>

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using ItemSystem;
using HandSystem;

namespace UISystem
{
    /// <summary>
    /// 手牌UI面板控制器
    /// 负责显示手牌列表、处理选中和食用逻辑
    /// </summary>
    [System.Obsolete("此脚本已废弃，不再使用。保留仅供历史参考。")]
    public class HandUIPanel : MonoBehaviour
    {
        #region 单例

        private static HandUIPanel instance;

        /// <summary>
        /// 单例访问点（懒加载）
        /// </summary>
        public static HandUIPanel Instance
        {
            get
            {
                if (instance == null)
                {
                    // 参数true表示包括未激活的对象
                    instance = FindObjectOfType<HandUIPanel>(true);
                    if (instance == null)
                    {
                        Debug.LogError("[HandUIPanel] 场景中未找到HandUIPanel对象");
                    }
                }
                return instance;
            }
        }

        #endregion

        #region UI组件引用

        [Header("手牌按钮")]
        [Tooltip("左下角的手牌按钮")]
        public Button handButton;

        [Tooltip("手牌按钮上的文本组件")]
        public TextMeshProUGUI handButtonText;

        [Tooltip("手牌按钮的RectTransform")]
        public RectTransform handButtonRect;

        [Header("手牌列表面板")]
        [Tooltip("手牌列表面板根对象")]
        public GameObject handListPanel;

        [Tooltip("手牌列表面板的RectTransform")]
        public RectTransform handListPanelRect;

        [Tooltip("ScrollView的Content（用于放置手牌卡片）")]
        public Transform cardContainer;

        [Tooltip("使用按钮")]
        public Button consumeButton;

        [Tooltip("背景遮罩（用于检测外部点击）")]
        public Button backgroundBlocker;

        [Tooltip("背景遮罩的CanvasGroup")]
        public CanvasGroup backgroundBlockerCanvasGroup;

        [Header("手牌卡片预制体")]
        [Tooltip("手牌卡片按钮预制体")]
        public GameObject handCardButtonPrefab;

        [Header("动画参数")]
        [Tooltip("动画时长（秒）")]
        public float animationDuration = 0.3f;

        [Tooltip("面板高度（像素）")]
        public float panelHeight = 420f;

        [Header("位置参数（可在Inspector中调整）")]
        [Tooltip("手牌按钮初始Y坐标（关闭状态）")]
        public float handButtonInitialY = 0f;

        [Tooltip("手牌按钮目标Y坐标（打开状态，通常等于面板高度）")]
        public float handButtonTargetY = 420f;

        [Tooltip("手牌列表面板初始Y坐标（关闭状态，隐藏在屏幕下方）")]
        public float handListPanelInitialY = -420f;

        [Tooltip("手牌列表面板目标Y坐标（打开状态）")]
        public float handListPanelTargetY = 0f;

        #endregion

        #region 数据

        /// <summary>
        /// 面板是否打开
        /// </summary>
        public bool isOpen { get; private set; } = false;

        /// <summary>
        /// 当前选中的手牌卡片按钮
        /// </summary>
        private HandCardButton selectedCardButton;

        /// <summary>
        /// 当前生成的所有手牌卡片按钮
        /// </summary>
        private List<HandCardButton> cardButtons = new List<HandCardButton>();

        /// <summary>
        /// 动画是否正在播放
        /// </summary>
        private bool isAnimating = false;

        #endregion

        #region 初始化

        private void Awake()
        {
            // 单例检查
            if (instance != null && instance != this)
            {
                Debug.LogWarning("[HandUIPanel] 场景中存在多个HandUIPanel实例，销毁重复实例");
                Destroy(gameObject);
                return;
            }
            instance = this;

            // 添加手牌按钮点击事件
            if (handButton != null)
            {
                handButton.onClick.AddListener(TogglePanel);
            }

            // 添加使用按钮点击事件
            if (consumeButton != null)
            {
                consumeButton.onClick.AddListener(OnConsumeButtonClicked);
                consumeButton.interactable = false; // 初始禁用
            }

            // 添加背景遮罩点击事件（点击外部关闭）
            if (backgroundBlocker != null)
            {
                backgroundBlocker.onClick.AddListener(ClosePanel);
            }

            // 初始化面板位置（隐藏状态）
            InitializePanelPositions();
        }

        private void Start()
        {
            // 订阅HandManager事件（在Start中确保HandManager已初始化）
            if (HandManager.Instance != null)
            {
                HandManager.Instance.OnHandChanged += UpdateHandCount;
                Debug.Log("[HandUIPanel] 成功订阅HandManager事件");
            }
            else
            {
                Debug.LogError("[HandUIPanel] HandManager.Instance为空，无法订阅事件");
            }

            // 初始化手牌数量显示
            UpdateHandCount();
        }

        private void OnDestroy()
        {
            // 清理单例引用
            if (instance == this)
            {
                instance = null;
            }

            // 取消订阅HandManager事件
            if (HandManager.Instance != null)
            {
                HandManager.Instance.OnHandChanged -= UpdateHandCount;
            }

            // 停止所有动画
            DOTween.Kill(handButtonRect);
            DOTween.Kill(handListPanelRect);
            DOTween.Kill(backgroundBlockerCanvasGroup);
        }

        private void Update()
        {
            // 按ESC键关闭面板
            if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                ClosePanel();
            }
        }

        /// <summary>
        /// 初始化面板位置
        /// </summary>
        private void InitializePanelPositions()
        {
            if (handButtonRect != null)
            {
                handButtonRect.anchoredPosition = new Vector2(handButtonRect.anchoredPosition.x, handButtonInitialY);
            }

            if (handListPanelRect != null)
            {
                handListPanelRect.anchoredPosition = new Vector2(handListPanelRect.anchoredPosition.x, handListPanelInitialY);
            }

            if (handListPanel != null)
            {
                handListPanel.SetActive(false);
            }

            if (backgroundBlocker != null)
            {
                backgroundBlocker.gameObject.SetActive(false);
            }

            if (backgroundBlockerCanvasGroup != null)
            {
                backgroundBlockerCanvasGroup.alpha = 0;
            }
        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 设置背景遮罩的激活状态（用于其他面板控制）
        /// </summary>
        /// <param name="active">是否激活</param>
        public void SetBackgroundBlockerActive(bool active)
        {
            if (backgroundBlocker != null)
            {
                backgroundBlocker.gameObject.SetActive(active);
            }

            if (backgroundBlockerCanvasGroup != null)
            {
                backgroundBlockerCanvasGroup.alpha = active ? 0.8f : 0f;
            }
        }

        /// <summary>
        /// 切换面板开关状态
        /// </summary>
        public void TogglePanel()
        {
            if (isAnimating)
            {
                Debug.Log("[HandUIPanel] 动画正在播放，忽略操作");
                return;
            }

            if (isOpen)
            {
                ClosePanel();
            }
            else
            {
                OpenPanel();
            }
        }

        /// <summary>
        /// 打开手牌列表面板
        /// </summary>
        public void OpenPanel()
        {
            if (isOpen || isAnimating)
            {
                return;
            }

            Debug.Log("[HandUIPanel] 打开手牌面板");

            // 刷新手牌列表
            RefreshHandCards();

            // 订阅手牌变化事件（面板打开时实时刷新）
            if (HandManager.Instance != null)
            {
                HandManager.Instance.OnHandChanged += RefreshHandCards;
                Debug.Log("[HandUIPanel] 订阅手牌变化事件以实时刷新");
            }

            // 显示面板和背景遮罩
            if (handListPanel != null)
            {
                handListPanel.SetActive(true);
            }

            if (backgroundBlocker != null)
            {
                backgroundBlocker.gameObject.SetActive(true);
            }

            // 播放打开动画
            isAnimating = true;

            // 手牌按钮上移
            if (handButtonRect != null)
            {
                handButtonRect.DOAnchorPosY(handButtonTargetY, animationDuration).SetEase(Ease.OutCubic);
            }

            // 手牌列表面板上移
            if (handListPanelRect != null)
            {
                handListPanelRect.DOAnchorPosY(handListPanelTargetY, animationDuration).SetEase(Ease.OutCubic);
            }

            // 背景遮罩保持完全透明（不需要淡入动画）
            if (backgroundBlockerCanvasGroup != null)
            {
                backgroundBlockerCanvasGroup.alpha = 0;
            }

            // 等待动画完成
            if (handListPanelRect != null)
            {
                handListPanelRect.DOAnchorPosY(handListPanelTargetY, animationDuration).SetEase(Ease.OutCubic).OnComplete(() =>
                {
                    isAnimating = false;
                    isOpen = true;
                });
            }
            else
            {
                isAnimating = false;
                isOpen = true;
            }
        }

        /// <summary>
        /// 关闭手牌列表面板
        /// </summary>
        public void ClosePanel()
        {
            if (!isOpen || isAnimating)
            {
                return;
            }

            // 检查鱼店面板是否打开，如果打开则阻止关闭手牌面板
            if (FishShopPanel.Instance != null && 
                FishShopPanel.Instance.panelRoot != null && 
                FishShopPanel.Instance.panelRoot.activeSelf)
            {
                Debug.Log("[HandUIPanel] 鱼店面板打开中，无法关闭手牌面板");
                return;
            }

            Debug.Log("[HandUIPanel] 关闭手牌面板");

            // 取消订阅手牌变化事件
            if (HandManager.Instance != null)
            {
                HandManager.Instance.OnHandChanged -= RefreshHandCards;
                Debug.Log("[HandUIPanel] 取消订阅手牌变化事件");
            }

            // 播放关闭动画
            isAnimating = true;

            // 手牌按钮下移
            if (handButtonRect != null)
            {
                handButtonRect.DOAnchorPosY(handButtonInitialY, animationDuration).SetEase(Ease.InCubic);
            }

            // 手牌列表面板下移
            if (handListPanelRect != null)
            {
                handListPanelRect.DOAnchorPosY(handListPanelInitialY, animationDuration).SetEase(Ease.InCubic).OnComplete(() =>
                {
                    // 动画完成后隐藏面板
                    if (handListPanel != null)
                    {
                        handListPanel.SetActive(false);
                    }

                    if (backgroundBlocker != null)
                    {
                        backgroundBlocker.gameObject.SetActive(false);
                    }

                    isAnimating = false;
                    isOpen = false;

                    // 清理选中状态
                    ClearSelection();
                });
            }
            else
            {
                if (handListPanel != null)
                {
                    handListPanel.SetActive(false);
                }

                if (backgroundBlocker != null)
                {
                    backgroundBlocker.gameObject.SetActive(false);
                }

                isAnimating = false;
                isOpen = false;
                ClearSelection();
            }
        }

        /// <summary>
        /// 手牌卡片按钮点击回调
        /// </summary>
        public void OnHandCardButtonClicked(HandCardButton clickedButton)
        {
            // 取消之前的选中状态
            if (selectedCardButton != null)
            {
                selectedCardButton.SetSelected(false);
            }

            // 如果点击的是已选中的按钮，取消选中
            if (selectedCardButton == clickedButton)
            {
                selectedCardButton = null;
            }
            else
            {
                // 选中新按钮
                selectedCardButton = clickedButton;
                selectedCardButton.SetSelected(true);
            }

            // 更新食用按钮状态
            UpdateConsumeButtonState();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 更新手牌数量显示
        /// </summary>
        private void UpdateHandCount()
        {
            if (handButtonText == null)
            {
                Debug.LogWarning("[HandUIPanel] handButtonText为空，无法更新手牌数量显示");
                return;
            }

            if (HandManager.Instance == null)
            {
                Debug.LogWarning("[HandUIPanel] HandManager.Instance为空，无法获取手牌数量");
                handButtonText.text = "手牌（?）";
                return;
            }

            int count = HandManager.Instance.GetHandCount();
            handButtonText.text = $"手牌  {count}";
            Debug.Log($"[HandUIPanel] 更新手牌数量显示: {count}");
        }

        /// <summary>
        /// 刷新手牌列表
        /// </summary>
        private void RefreshHandCards()
        {
            // 清空现有的手牌卡片按钮
            ClearCardButtons();

            // 获取当前手牌
            if (HandManager.Instance == null)
            {
                Debug.LogError("[HandUIPanel] HandManager.Instance为空");
                return;
            }

            List<ItemData> handCards = HandManager.Instance.GetHandCards();

            if (handCards == null || handCards.Count == 0)
            {
                Debug.Log("[HandUIPanel] 手牌为空");
                return;
            }

            // 生成手牌卡片按钮
            foreach (ItemData item in handCards)
            {
                if (item != null && handCardButtonPrefab != null && cardContainer != null)
                {
                    GameObject cardObj = Instantiate(handCardButtonPrefab, cardContainer);
                    HandCardButton cardButton = cardObj.GetComponent<HandCardButton>();

                    if (cardButton != null)
                    {
                        cardButton.SetCardData(item, this);
                        cardButtons.Add(cardButton);
                    }
                }
            }

            Debug.Log($"[HandUIPanel] 刷新手牌列表，共{handCards.Count}张卡牌");
        }

        /// <summary>
        /// 清空所有手牌卡片按钮
        /// </summary>
        private void ClearCardButtons()
        {
            foreach (var cardButton in cardButtons)
            {
                if (cardButton != null)
                {
                    Destroy(cardButton.gameObject);
                }
            }

            cardButtons.Clear();
        }

        /// <summary>
        /// 清除选中状态
        /// </summary>
        private void ClearSelection()
        {
            if (selectedCardButton != null)
            {
                selectedCardButton.SetSelected(false);
                selectedCardButton = null;
            }

            UpdateConsumeButtonState();
        }

        /// <summary>
        /// 更新使用按钮的可用状态
        /// </summary>
        private void UpdateConsumeButtonState()
        {
            if (consumeButton != null)
            {
                consumeButton.interactable = (selectedCardButton != null);
            }
        }

        /// <summary>
        /// 使用按钮点击事件（处理不同类型物品）
        /// </summary>
        private void OnConsumeButtonClicked()
        {
            if (selectedCardButton == null)
            {
                Debug.LogWarning("[HandUIPanel] 未选中任何物品，无法使用");
                return;
            }

            ItemData selectedItem = selectedCardButton.GetCardData();

            if (selectedItem == null)
            {
                Debug.LogWarning("[HandUIPanel] 选中的物品数据为空");
                return;
            }

            Debug.Log($"[HandUIPanel] 使用物品: {selectedItem.itemName}");

            // 根据物品类型执行不同操作
            if (selectedItem is FishData fish)
            {
                // 1. 触发鱼类使用效果
                fish.TriggerUseEffects();

                // 2. 从HandManager移除卡牌
                HandManager.Instance.RemoveCard(selectedItem);
            }
            else if (selectedItem is TrashData trash)
            {
                // 1. 触发杂鱼使用效果
                trash.TriggerUseEffects();

                // 2. 从HandManager移除卡牌
                HandManager.Instance.RemoveCard(selectedItem);
            }
            else if (selectedItem is ConsumableData consumable)
            {
                // 1. 触发消耗品使用效果
                consumable.TriggerUseEffects();

                // 2. 从HandManager移除卡牌
                HandManager.Instance.RemoveCard(selectedItem);
            }
            else if (selectedItem is EquipmentData equipment)
            {
                // 装备：装备到对应槽位
                bool success = EquipmentManager.Instance.Equip(equipment);
                
                if (success)
                {
                    // 装备成功，从手牌移除
                    HandManager.Instance.RemoveCard(selectedItem);
                }
                else
                {
                    Debug.LogWarning($"[HandUIPanel] 装备失败: {equipment.itemName}");
                }
            }
            else
            {
                Debug.LogWarning($"[HandUIPanel] 未知的物品类型: {selectedItem.GetType()}");
                return;
            }

            // 3. 刷新UI（会自动销毁对应的按钮）
            RefreshHandCards();

            // 4. 清除选中状态
            ClearSelection();

            Debug.Log($"[HandUIPanel] 食用完成，剩余手牌: {HandManager.Instance.GetHandCount()}");
        }

        #endregion
    }
}
