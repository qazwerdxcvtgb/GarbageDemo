using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HandSystem;
using ItemSystem;

namespace ShopSystem
{
    /// <summary>
    /// 商店装备购买控制器
    /// 与 ShopBuyController（消耗品购买）并列挂在 ShopTabContent 下，职责独立。
    /// 从 ShopManager 装备牌序中顺序抽取装备并加入手牌，消耗对应金币。
    /// 装备价格优先读取 EquipmentData.value（在 Inspector 中配置），不足时使用本控制器的默认价格。
    /// </summary>
    public class ShopEquipmentController : MonoBehaviour
    {
        [Header("购买装备")]
        [SerializeField] private Button buyEquipmentButton;
        [SerializeField] private TextMeshProUGUI buyEquipmentButtonText;
        [Tooltip("装备购买价格（当装备数据 value=0 时使用此默认值）")]
        [SerializeField] private int defaultEquipmentCost = 8;

        [Header("依赖引用")]
        [SerializeField] private CharacterState playerState;

        private void Awake()
        {
            if (playerState == null)
                playerState = FindObjectOfType<CharacterState>();

            if (playerState != null)
                playerState.OnGoldChanged.AddListener(OnGoldChanged);
        }

        private void Start()
        {
            if (buyEquipmentButton != null)
                buyEquipmentButton.onClick.AddListener(OnBuyEquipmentClicked);

            RefreshButton();
        }

        private void OnDestroy()
        {
            if (buyEquipmentButton != null)
                buyEquipmentButton.onClick.RemoveListener(OnBuyEquipmentClicked);

            if (playerState != null)
                playerState.OnGoldChanged.RemoveListener(OnGoldChanged);
        }

        private void OnGoldChanged(int newGold)
        {
            RefreshButton();
        }

        #region Public API

        /// <summary>
        /// 商店打开时调用，刷新按钮状态
        /// </summary>
        public void RefreshButtonState()
        {
            RefreshButton();
        }

        #endregion

        #region Buy Logic

        private void OnBuyEquipmentClicked()
        {
            if (ShopManager.Instance == null || playerState == null) return;

            EquipmentData preview = ShopManager.Instance.PeekEquipment();
            int cost = GetCost(preview);

            if (!playerState.HasEnoughGold(cost))
            {
                Debug.Log("[ShopEquipmentController] 金币不足，无法购买装备");
                return;
            }

            EquipmentData drawn = ShopManager.Instance.DrawEquipment();
            if (drawn == null)
            {
                RefreshButton();
                return;
            }

            playerState.ModifyGold(-cost);
            HandManager.Instance?.AddCard(drawn);

            Debug.Log($"[ShopEquipmentController] 购买装备：{drawn.itemName}，花费 {cost} 金币");
            RefreshButton();
        }

        private void RefreshButton()
        {
            if (buyEquipmentButton == null) return;

            if (ShopManager.Instance != null && ShopManager.Instance.IsEquipmentPoolEmpty)
            {
                buyEquipmentButton.interactable = false;
                if (buyEquipmentButtonText != null)
                    buyEquipmentButtonText.text = "装备 已售罄";
                return;
            }

            EquipmentData preview = ShopManager.Instance?.PeekEquipment();
            int cost = GetCost(preview);
            bool canAfford = playerState != null && playerState.HasEnoughGold(cost);

            buyEquipmentButton.interactable = canAfford;
            if (buyEquipmentButtonText != null)
                buyEquipmentButtonText.text = $"购买装备  {cost}金币";
        }

        private int GetCost(EquipmentData data)
        {
            if (data != null && data.value > 0)
                return data.value;
            return defaultEquipmentCost;
        }

        #endregion
    }
}
