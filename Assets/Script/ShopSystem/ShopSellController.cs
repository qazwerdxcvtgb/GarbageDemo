using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ItemSystem;
using FishCardSystem;
using HandSystem;

namespace ShopSystem
{
    /// <summary>
    /// 商店售卖功能控制器
    /// 商店打开时订阅手牌选中事件，实时计算选中牌总价，执行售卖操作。
    /// </summary>
    public class ShopSellController : MonoBehaviour
    {
        [Header("UI 引用")]
        [SerializeField] private TextMeshProUGUI totalPriceText;
        [SerializeField] private Button sellButton;

        [Header("依赖引用")]
        [SerializeField] private FishCardHolder cardHolder;
        [SerializeField] private CharacterState playerState;

        private bool isShopOpen;

        #region Lifecycle

        private void Awake()
        {
            if (playerState == null)
                playerState = FindObjectOfType<CharacterState>();
        }

        private void OnDestroy()
        {
            OnShopClose();
        }

        #endregion

        #region Public API

        /// <summary>
        /// 商店打开时调用：订阅手牌选中事件，刷新 UI
        /// </summary>
        public void OnShopOpen()
        {
            isShopOpen = true;

            if (sellButton != null)
                sellButton.onClick.AddListener(ExecuteSell);

            if (HandManager.Instance != null)
                HandManager.Instance.OnHandChanged += OnHandChangedRefresh;

            if (GameManager.Instance != null)
                GameManager.Instance.OnSanityLevelChanged.AddListener(OnSanityLevelChangedRefresh);

            RefreshSubscriptions();
            RecalculateTotalPrice();
        }

        /// <summary>
        /// 商店关闭时调用：取消所有选中状态，注销事件监听
        /// </summary>
        public void OnShopClose()
        {
            isShopOpen = false;

            if (sellButton != null)
                sellButton.onClick.RemoveListener(ExecuteSell);

            if (HandManager.Instance != null)
                HandManager.Instance.OnHandChanged -= OnHandChangedRefresh;

            if (GameManager.Instance != null)
                GameManager.Instance.OnSanityLevelChanged.RemoveListener(OnSanityLevelChangedRefresh);

            // 取消所有手牌选中状态
            if (cardHolder != null)
            {
                foreach (var card in cardHolder.GetCards())
                {
                    if (card.selected) card.Deselect();
                    card.SelectEvent.RemoveListener(OnCardSelected);
                }
            }

            UpdateSellUI(0);
        }

        #endregion

        #region Subscription Management

        private void OnHandChangedRefresh()
        {
            if (!isActiveAndEnabled) return;
            StartCoroutine(RefreshNextFrame());
        }

        private IEnumerator RefreshNextFrame()
        {
            yield return null;
            RefreshSubscriptions();
            RecalculateTotalPrice();
        }

        private void RefreshSubscriptions()
        {
            if (cardHolder == null) return;
            foreach (var card in cardHolder.GetCards())
            {
                card.SelectEvent.RemoveListener(OnCardSelected);
                card.SelectEvent.AddListener(OnCardSelected);
            }
        }

        private void OnCardSelected(ItemCard card, bool selected)
        {
            if (!isShopOpen) return;
            RecalculateTotalPrice();
        }

        private void OnSanityLevelChangedRefresh(SanityLevel _)
        {
            if (!isShopOpen) return;
            RecalculateTotalPrice();
        }

        #endregion

        #region Price Calculation

        /// <summary>
        /// 计算单张卡牌经疯狂等级修正后的售价
        /// 仅 FishData 受疯狂等级影响，其他类型返回基础价值
        /// </summary>
        private int GetAdjustedCardValue(ItemCard card)
        {
            if (card.cardData == null) return 0;

            int baseValue = card.cardData.value;

            if (card.cardData is FishData fishData && GameManager.Instance != null)
            {
                bool isStable = fishData.effects != null
                    && fishData.effects.Exists(e => e is Effect_StablePrice);
                if (isStable) return baseValue;

                int modifier = GameManager.Instance.GetSanityGoldModifier(fishData.fishType);
                return Mathf.Max(0, baseValue + modifier);
            }

            return baseValue;
        }

        private void RecalculateTotalPrice()
        {
            if (cardHolder == null) { UpdateSellUI(0); return; }

            int total = cardHolder.GetCards()
                .Where(c => c.selected && c.cardData != null && IsSellable(c))
                .Sum(c => GetAdjustedCardValue(c));

            UpdateSellUI(total);
        }

        private bool IsSellable(ItemCard card)
        {
            return !(card.cardData is FishData fish && Effect_NoSellNoHang.HasEffect(fish));
        }

        private void UpdateSellUI(int total)
        {
            if (totalPriceText != null)
            {
                if (total > 0)
                {
                    totalPriceText.text      = $"已选总价：{total} 金币";
                    totalPriceText.gameObject.SetActive(true);
                }
                else
                {
                    totalPriceText.gameObject.SetActive(false);
                }
            }

            if (sellButton != null)
                sellButton.interactable = total > 0;
        }

        #endregion

        #region Sell Execution

        private void ExecuteSell()
        {
            if (cardHolder == null || playerState == null) return;

            List<ItemCard> selected = cardHolder.GetCards()
                .Where(c => c.selected && IsSellable(c))
                .ToList();

            if (selected.Count == 0) return;

            int total = selected.Sum(c => GetAdjustedCardValue(c));

            foreach (var card in selected)
            {
                // 先捕获数据引用，RemoveCardAndCollapse 会销毁卡牌 GO
                ItemData data = card.cardData;

                card.SelectEvent.RemoveListener(OnCardSelected);

                // 移除卡牌并同时销毁其槽位，防止 SetSlotCount 从末尾错误删除有效卡牌
                cardHolder.RemoveCardAndCollapse(card);

                // 从 HandManager 数据层移除（触发 OnHandChanged → SetSlotCount，此时 childCount 已正确）
                if (data != null)
                    HandManager.Instance?.RemoveCard(data);
            }

            playerState.ModifyGold(total);
            Debug.Log($"[ShopSellController] 售卖 {selected.Count} 张卡牌，获得 {total} 金币");

            RecalculateTotalPrice();
        }

        #endregion
    }
}
