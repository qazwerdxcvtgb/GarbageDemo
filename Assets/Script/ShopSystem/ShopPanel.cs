using UnityEngine;
using UnityEngine.UI;
using HandSystem;
using FishCardSystem;

namespace ShopSystem
{
    /// <summary>
    /// 商店面板主控制器
    /// 负责：面板开关、标签切换（商店页 / 悬挂页）、协调各子控制器、与 HandPanelUI 联动。
    ///
    /// 跨 Holder 拖拽激活逻辑已移至 CrossHolderSystem（全局单例）+
    /// ShopHangSlot（OnEnable/OnDisable 自动注册/注销 Target）。
    /// ShopPanel 无需再手动管理 CrossHolderDragHandler 的激活状态。
    /// </summary>
    public class ShopPanel : MonoBehaviour
    {
        public static ShopPanel Instance { get; private set; }

        #region Inspector Fields

        [Header("面板根节点")]
        [SerializeField] private GameObject panelRoot;

        [Header("标签页内容")]
        [SerializeField] private GameObject shopTabContent;  // 商店页（买卖）
        [SerializeField] private GameObject hangTabContent;  // 悬挂页

        [Header("标签按钮")]
        [SerializeField] private Button shopTabButton;
        [SerializeField] private Button hangTabButton;

        [Header("关闭按钮")]
        [SerializeField] private Button closeButton;

        [Header("子控制器引用")]
        [SerializeField] private ShopSellController sellController;
        [SerializeField] private ShopBuyController  buyController;
        [SerializeField] private ShopHangController hangController;

        [Header("手牌面板引用")]
        [SerializeField] private HandPanelUI handPanelUI;

        [Header("打开商店时隐藏的对象")]
        [Tooltip("商店开启时需要隐藏的牌堆容器（如 FishingTablePanel 根节点）")]
        [SerializeField] private GameObject[] hideOnOpen;

        [Header("图层设置")]
        [Tooltip("商店面板 Canvas 的 sortingOrder（需与 ShopHangController.shopPanelSortingOrder 保持一致）")]
        [SerializeField] private int panelSortingOrder = 165;

        #endregion

        private bool isOpen;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (panelRoot != null)
                panelRoot.SetActive(false);

            // 确保面板具有独立 Canvas，使其渲染在牌堆面板(160)之上、装备面板(170)之下
            Canvas selfCanvas = GetComponent<Canvas>();
            if (selfCanvas == null)
                selfCanvas = gameObject.AddComponent<Canvas>();
            selfCanvas.overrideSorting = true;
            selfCanvas.sortingOrder    = panelSortingOrder;

            if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        private void Start()
        {
            if (shopTabButton != null)
                shopTabButton.onClick.AddListener(() => SwitchTab(false));
            if (hangTabButton != null)
                hangTabButton.onClick.AddListener(() => SwitchTab(true));
            // 商店关闭按钮已禁用：商店仅通过 DayManager 的"下一天"按钮退出
            // closeButton 保留 Inspector 引用但不绑定事件
            if (closeButton != null)
                closeButton.gameObject.SetActive(false);

            // 确保商店管理器牌序已初始化
            if (ShopManager.Instance != null)
                ShopManager.Instance.InitializePools();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #endregion

        #region Public API

        public void OpenPanel()
        {
            if (isOpen) return;
            isOpen = true;

            // 1. 显示面板
            if (panelRoot != null)
                panelRoot.SetActive(true);

            // 2. 手牌面板：强制展开、隐藏折叠按钮
            if (handPanelUI != null)
                handPanelUI.LockExpanded();

            // 隐藏牌堆等场景对象
            foreach (var obj in hideOnOpen)
                obj?.SetActive(false);

            // 3. 默认显示商店页
            SwitchTab(false);

            // 4. 通知子控制器
            sellController?.OnShopOpen();
            hangController?.RestoreHangState();
            buyController?.RefreshButtonState();

            Debug.Log("[ShopPanel] 商店面板已打开");
        }

        public void ClosePanel()
        {
            if (!isOpen) return;
            isOpen = false;

            // 1. 通知子控制器清理
            sellController?.OnShopClose();
            hangController?.ClearAllSlotVisuals();

            // 2. 恢复手牌面板
            if (handPanelUI != null)
                handPanelUI.UnlockExpanded();

            // 恢复牌堆等场景对象
            foreach (var obj in hideOnOpen)
                obj?.SetActive(true);

            // 3. 隐藏面板（HangTabContent 随 panelRoot 隐藏 → ShopHangSlot.OnDisable 自动注销 Target）
            if (panelRoot != null)
                panelRoot.SetActive(false);

            Debug.Log("[ShopPanel] 商店面板已关闭");
        }

        #endregion

        #region Tab Switching

        /// <summary>
        /// 切换标签页。
        /// 跨 Holder 激活状态由 HangTabContent 的 SetActive 驱动：
        /// 显示时 ShopHangSlot.OnEnable 注册 Target → CrossHolderSystem 自动激活；
        /// 隐藏时 ShopHangSlot.OnDisable 注销 Target → CrossHolderSystem 自动停用。
        /// </summary>
        /// <param name="showHang">true = 悬挂页，false = 商店页</param>
        private void SwitchTab(bool showHang)
        {
            if (shopTabContent != null)
                shopTabContent.SetActive(!showHang);
            if (hangTabContent != null)
                hangTabContent.SetActive(showHang);

            Debug.Log($"[ShopPanel] 切换到{(showHang ? "悬挂" : "商店")}页");
        }

        #endregion
    }
}
