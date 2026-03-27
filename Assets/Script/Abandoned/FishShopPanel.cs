/// <summary>
/// 鱼店面板控制器
/// 创建日期：2026-01-21
/// 功能：管理鱼店面板UI、处理鱼类售卖逻辑
/// </summary>

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ItemSystem;
using HandSystem;

namespace UISystem
{
    /// <summary>
    /// 鱼店面板控制器（单例）
    /// </summary>
    [System.Obsolete("此脚本已废弃，不再使用。保留仅供历史参考。")]
    public class FishShopPanel : MonoBehaviour
    {
        #region 单例

        private static FishShopPanel instance;

        /// <summary>
        /// 单例访问点（懒加载）
        /// </summary>
        public static FishShopPanel Instance
        {
            get
            {
                if (instance == null)
                {
                    // 参数true表示包括未激活的对象
                    instance = FindObjectOfType<FishShopPanel>(true);
                    if (instance == null)
                    {
                        Debug.LogError("[FishShopPanel] 场景中未找到FishShopPanel对象");
                    }
                    else
                    {
                        Debug.Log("[FishShopPanel] 懒加载成功找到FishShopPanel实例");
                    }
                }
                return instance;
            }
        }

        #endregion

        #region UI组件引用

        [Header("UI组件")]
        [Tooltip("面板根对象")]
        public GameObject panelRoot;

        [Tooltip("卡片容器（ScrollView的Content）")]
        public Transform cardContainer;

        [Tooltip("拖拽放置区域（ScrollView的Viewport）")]
        public RectTransform dropZone;

        [Tooltip("售卖按钮")]
        public Button sellButton;

        [Tooltip("关闭按钮")]
        public Button closeButton;

        [Tooltip("背景遮罩按钮")]
        public Button backgroundBlocker;

        [Tooltip("售卖总价文本")]
        public TextMeshProUGUI totalPriceText;

        [Tooltip("疯狂值增加文本")]
        public TextMeshProUGUI sanityIncreaseText;

        [Header("预制体")]
        [Tooltip("鱼店卡牌槽预制体")]
        public GameObject fishShopCardSlotPrefab;

        #endregion

        #region 数据

        private List<FishData> fishInShop = new List<FishData>();
        private int totalPrice = 0;
        private int sanityIncrease = 0;
        private bool isInitialized = false;

        /// <summary>
        /// 玩家移动控制器引用
        /// </summary>
        private PlayerMove playerMove;

        #endregion

        #region Unity生命周期

        void Awake()
        {
            // 单例检查
            if (instance != null && instance != this)
            {
                Debug.LogWarning("[FishShopPanel] 场景中存在多个FishShopPanel实例，销毁重复实例");
                Destroy(gameObject);
                return;
            }
            instance = this;

            // 初始化
            InitializeUI();
        }

        void OnDestroy()
        {
            // 清理单例引用
            if (instance == this)
            {
                instance = null;
            }

            // 移除按钮事件监听
            if (sellButton != null)
            {
                sellButton.onClick.RemoveAllListeners();
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
            }

            if (backgroundBlocker != null)
            {
                backgroundBlocker.onClick.RemoveAllListeners();
            }
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化UI组件
        /// </summary>
        private void InitializeUI()
        {
            // 避免重复初始化
            if (isInitialized)
            {
                return;
            }

            // 初始化列表（已在字段声明时初始化，这里仅作保险）
            if (fishInShop == null)
            {
                fishInShop = new List<FishData>();
            }

            // 绑定按钮事件
            if (sellButton != null)
            {
                sellButton.onClick.RemoveAllListeners();
                sellButton.onClick.AddListener(OnSellButtonClicked);
            }
            else
            {
                Debug.LogWarning("[FishShopPanel] sellButton未配置");
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(OnCloseButtonClicked);
            }
            else
            {
                Debug.LogWarning("[FishShopPanel] closeButton未配置");
            }

            if (backgroundBlocker != null)
            {
                backgroundBlocker.onClick.RemoveAllListeners();
                backgroundBlocker.onClick.AddListener(OnCloseButtonClicked);
            }
            else
            {
                Debug.LogWarning("[FishShopPanel] backgroundBlocker未配置");
            }

            // 初始隐藏面板
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
            else
            {
                Debug.LogError("[FishShopPanel] panelRoot未配置");
            }

            isInitialized = true;
            Debug.Log("[FishShopPanel] 初始化完成");
        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 打开鱼店面板
        /// </summary>
        public void OpenPanel()
        {
            // 确保已初始化（针对懒加载场景，Awake可能未执行）
            if (!isInitialized)
            {
                Debug.Log("[FishShopPanel] 懒加载模式，执行初始化");
                InitializeUI();
            }

            // 依赖检查
            if (HandManager.Instance == null)
            {
                Debug.LogError("[FishShopPanel] HandManager.Instance为空，无法打开面板");
                return;
            }

            if (HandUIPanel.Instance == null)
            {
                Debug.LogError("[FishShopPanel] HandUIPanel.Instance为空，无法打开面板");
                return;
            }

            if (panelRoot == null)
            {
                Debug.LogError("[FishShopPanel] panelRoot未配置");
                return;
            }

            // 查找玩家对象并获取PlayerMove组件
            GameObject player = GameObject.Find("player");
            if (player == null)
            {
                Debug.LogError("[FishShopPanel] 未找到玩家对象（名称应为'player'）");
                return;
            }

            playerMove = player.GetComponent<PlayerMove>();
            if (playerMove == null)
            {
                Debug.LogError("[FishShopPanel] 玩家对象缺少PlayerMove组件");
                return;
            }

            // 清空数据
            fishInShop.Clear();
            totalPrice = 0;
            sanityIncrease = 0;

            // 清空UI
            if (cardContainer != null)
            {
                foreach (Transform child in cardContainer)
                {
                    Destroy(child.gameObject);
                }
            }

            // 更新UI显示
            UpdateUI();

            // 禁用玩家移动和交互（在确认面板能够打开后）
            playerMove.SetInputEnabled(false);

            // 显示面板
            panelRoot.SetActive(true);
            if (backgroundBlocker != null)
            {
                backgroundBlocker.gameObject.SetActive(true);
            }

            // 打开手牌面板
            HandUIPanel.Instance.OpenPanel();

            // 禁用手牌面板的背景遮罩（避免遮挡鱼店面板）
            HandUIPanel.Instance.SetBackgroundBlockerActive(false);

            Debug.Log("[FishShopPanel] 鱼店面板已打开");
        }

        /// <summary>
        /// 关闭鱼店面板
        /// </summary>
        public void ClosePanel()
        {
            // 将鱼店中的所有卡牌返还到手牌
            if (fishInShop.Count > 0)
            {
                Debug.Log($"[FishShopPanel] 关闭面板，返还 {fishInShop.Count} 张卡牌到手牌");

                // 将所有卡牌添加回HandManager
                if (HandSystem.HandManager.Instance != null)
                {
                    foreach (FishData fish in fishInShop)
                    {
                        if (fish != null)
                        {
                            HandSystem.HandManager.Instance.AddCard(fish);
                            Debug.Log($"[FishShopPanel] 返还卡牌到手牌: {fish.itemName}");
                        }
                    }
                }
                else
                {
                    Debug.LogError("[FishShopPanel] HandManager.Instance为空，无法返还卡牌");
                }
            }

            // 清空数据
            fishInShop.Clear();
            totalPrice = 0;
            sanityIncrease = 0;

            // 清空UI
            if (cardContainer != null)
            {
                foreach (Transform child in cardContainer)
                {
                    Destroy(child.gameObject);
                }
            }

            // 隐藏面板
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            if (backgroundBlocker != null)
            {
                backgroundBlocker.gameObject.SetActive(false);
            }

            // 关闭手牌面板
            if (HandUIPanel.Instance != null)
            {
                HandUIPanel.Instance.ClosePanel();
            }

            // 恢复玩家移动和交互
            if (playerMove != null)
            {
                playerMove.SetInputEnabled(true);
                playerMove = null;
            }

            Debug.Log("[FishShopPanel] 鱼店面板已关闭");
        }

        /// <summary>
        /// 添加鱼类卡牌到鱼店
        /// </summary>
        /// <param name="item">物品数据</param>
        public void AddFish(ItemData item)
        {
            // 参数检查
            if (item == null)
            {
                Debug.LogWarning("[FishShopPanel] 物品数据为空");
                return;
            }

            // 类型检查：只能添加鱼类
            if (!(item is FishData fish))
            {
                Debug.LogWarning($"[FishShopPanel] 只能添加鱼类，当前物品类型: {item.category.ToChineseText()}");
                return;
            }

            // 添加到列表
            fishInShop.Add(fish);

            // 动态生成卡牌槽
            if (fishShopCardSlotPrefab != null && cardContainer != null)
            {
                GameObject slotObj = Instantiate(fishShopCardSlotPrefab, cardContainer);
                FishShopCardSlot slot = slotObj.GetComponent<FishShopCardSlot>();
                if (slot != null)
                {
                    slot.SetCardData(fish, this);

                    // 确保有CanvasGroup组件（拖拽需要）
                    if (slotObj.GetComponent<CanvasGroup>() == null)
                    {
                        slotObj.AddComponent<CanvasGroup>();
                    }

                    // 添加拖拽组件（允许拖回手牌）
                    if (slotObj.GetComponent<DraggableFishShopCard>() == null)
                    {
                        slotObj.AddComponent<DraggableFishShopCard>();
                    }
                }
                else
                {
                    Debug.LogError("[FishShopPanel] FishShopCardSlot组件未找到");
                }
            }
            else
            {
                Debug.LogError("[FishShopPanel] fishShopCardSlotPrefab或cardContainer未配置");
            }

            // 重新计算总价和疯狂值
            CalculateTotalPriceAndSanity();

            Debug.Log($"[FishShopPanel] 添加鱼类: {fish.itemName}");
        }

        /// <summary>
        /// 从鱼店移除鱼类卡牌
        /// </summary>
        /// <param name="fish">鱼类数据</param>
        public void RemoveFish(FishData fish)
        {
            if (fish == null)
            {
                Debug.LogWarning("[FishShopPanel] 鱼类数据为空");
                return;
            }

            if (!fishInShop.Contains(fish))
            {
                Debug.LogWarning($"[FishShopPanel] 鱼店中不包含此鱼类: {fish.itemName}");
                return;
            }

            // 从列表移除
            fishInShop.Remove(fish);

            // 查找并销毁对应的卡牌槽
            if (cardContainer != null)
            {
                foreach (Transform child in cardContainer)
                {
                    FishShopCardSlot slot = child.GetComponent<FishShopCardSlot>();
                    if (slot != null && slot.GetCardData() == fish)
                    {
                        Destroy(child.gameObject);
                        break;
                    }
                }
            }

            // 重新计算总价和疯狂值
            CalculateTotalPriceAndSanity();

            Debug.Log($"[FishShopPanel] 移除鱼类: {fish.itemName}");
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 计算售卖总价和疯狂值增加
        /// </summary>
        private void CalculateTotalPriceAndSanity()
        {
            // 重置
            totalPrice = 0;
            sanityIncrease = 0;

            // 遍历计算
            foreach (var fish in fishInShop)
            {
                // 计算售价
                int price = FishPriceCalculator.CalculatePrice(fish);
                totalPrice += price;

                // 统计污秽鱼数量
                if (fish.fishType == FishType.Corrupted)
                {
                    sanityIncrease++;
                }
            }

            // 更新UI
            UpdateUI();

            Debug.Log($"[FishShopPanel] 售卖总价: {totalPrice}金币, 疯狂值+{sanityIncrease}, 鱼类数量: {fishInShop.Count}");
        }

        /// <summary>
        /// 更新UI显示
        /// </summary>
        private void UpdateUI()
        {
            // 更新总价文本
            if (totalPriceText != null)
            {
                totalPriceText.text = $"售卖总价: {totalPrice}金币";
            }

            // 更新疯狂值文本
            if (sanityIncreaseText != null)
            {
                if (sanityIncrease > 0)
                {
                    sanityIncreaseText.text = $"疯狂值+{sanityIncrease}";
                    sanityIncreaseText.color = Color.red;
                    sanityIncreaseText.gameObject.SetActive(true);
                }
                else
                {
                    sanityIncreaseText.gameObject.SetActive(false);
                }
            }

            // 更新售卖按钮状态
            if (sellButton != null)
            {
                sellButton.interactable = (fishInShop.Count > 0);
            }
        }

        /// <summary>
        /// 售卖按钮点击
        /// </summary>
        private void OnSellButtonClicked()
        {
            // 检查是否有鱼类
            if (fishInShop.Count == 0)
            {
                Debug.LogWarning("[FishShopPanel] 鱼店中没有鱼类，无法售卖");
                return;
            }

            // 查找玩家对象
            GameObject player = GameObject.Find("player");
            if (player == null)
            {
                Debug.LogError("[FishShopPanel] 未找到玩家对象（名称应为'player'）");
                return;
            }

            // 获取角色状态组件
            CharacterState playerState = player.GetComponent<CharacterState>();
            if (playerState == null)
            {
                Debug.LogError("[FishShopPanel] 玩家对象缺少CharacterState组件");
                return;
            }

            // 增加金币
            playerState.ModifyGold(totalPrice);

            // 增加疯狂值
            if (sanityIncrease > 0)
            {
                GameManager.Instance.ModifySanity(sanityIncrease);
            }

            Debug.Log($"[FishShopPanel] 售卖完成: +{totalPrice}金币, +{sanityIncrease}疯狂值, 移除{fishInShop.Count}张鱼类卡牌");

            // 清空鱼店数据（注意：卡牌在拖拽时已从HandManager移除，这里不需要再次移除）
            int soldCount = fishInShop.Count;
            fishInShop.Clear();
            totalPrice = 0;
            sanityIncrease = 0;

            // 清空UI
            if (cardContainer != null)
            {
                foreach (Transform child in cardContainer)
                {
                    Destroy(child.gameObject);
                }
            }

            // 隐藏面板
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            if (backgroundBlocker != null)
            {
                backgroundBlocker.gameObject.SetActive(false);
            }

            // 关闭手牌面板
            if (HandUIPanel.Instance != null)
            {
                HandUIPanel.Instance.ClosePanel();
            }

            // 恢复玩家移动和交互
            if (playerMove != null)
            {
                playerMove.SetInputEnabled(true);
                playerMove = null;
            }

            Debug.Log($"[FishShopPanel] 售卖完成，面板已关闭，已售卖{soldCount}张卡牌");
        }

        /// <summary>
        /// 关闭按钮点击
        /// </summary>
        private void OnCloseButtonClicked()
        {
            Debug.Log("[FishShopPanel] 关闭面板，鱼类卡牌返还到手牌中");

            // 直接关闭面板（卡牌本来就在HandManager中，无需返还）
            ClosePanel();
        }

        #endregion
    }
}
