using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HandSystem;

namespace ShopSystem
{
    /// <summary>
    /// 商店购买功能控制器（本版本仅实现消耗品购买）
    /// </summary>
    public class ShopBuyController : MonoBehaviour
    {
        [Header("购买消耗品")]
        [SerializeField] private Button buyConsumableButton;
        [SerializeField] private TextMeshProUGUI buyConsumableButtonText;
        [SerializeField] private int consumableCost = 5;

        [Header("依赖引用")]
        [SerializeField] private CharacterState playerState;

        private void Awake()
        {
            if (playerState == null)
                playerState = FindObjectOfType<CharacterState>();

            // 订阅金币变化事件，金币变化时实时刷新按钮状态
            if (playerState != null)
                playerState.OnGoldChanged.AddListener(OnGoldChanged);
        }

        private void Start()
        {
            if (buyConsumableButton != null)
                buyConsumableButton.onClick.AddListener(OnBuyConsumableClicked);
        }

        private void OnDestroy()
        {
            if (buyConsumableButton != null)
                buyConsumableButton.onClick.RemoveListener(OnBuyConsumableClicked);

            if (playerState != null)
                playerState.OnGoldChanged.RemoveListener(OnGoldChanged);
        }

        private void OnGoldChanged(int newGold)
        {
            RefreshConsumableButton();
        }

        #region Public API

        /// <summary>
        /// 商店打开时调用：根据当前金币和牌池状态刷新按钮
        /// </summary>
        public void RefreshButtonState()
        {
            RefreshConsumableButton();
        }

        #endregion

        #region Buy Logic

        private void OnBuyConsumableClicked()
        {
            if (ShopManager.Instance == null || playerState == null) return;

            if (!playerState.HasEnoughGold(consumableCost))
            {
                Debug.Log("[ShopBuyController] 金币不足，无法购买消耗品");
                return;
            }

            var consumableData = ShopManager.Instance.DrawConsumable();
            if (consumableData == null)
            {
                RefreshConsumableButton(); // 更新为"已售罄"
                return;
            }

            playerState.ModifyGold(-consumableCost);
            HandManager.Instance?.AddCard(consumableData);

            Debug.Log($"[ShopBuyController] 购买消耗品：{consumableData.itemName}，花费 {consumableCost} 金币");

            RefreshConsumableButton();
        }

        private void RefreshConsumableButton()
        {
            if (buyConsumableButton == null) return;

            if (ShopManager.Instance != null && ShopManager.Instance.IsConsumablePoolEmpty)
            {
                buyConsumableButton.interactable = false;
                if (buyConsumableButtonText != null)
                    buyConsumableButtonText.text = "消耗品 已售罄";
                return;
            }

            bool canAfford = playerState != null && playerState.HasEnoughGold(consumableCost);
            buyConsumableButton.interactable = canAfford;

            if (buyConsumableButtonText != null)
                buyConsumableButtonText.text = $"购买消耗品  {consumableCost}金币";
        }

        #endregion
    }
}
