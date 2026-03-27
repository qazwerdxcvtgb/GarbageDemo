using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using ItemSystem;
using FishCardSystem;

namespace ShopSystem
{
    /// <summary>
    /// 商店悬挂槽
    /// 单个槽位组件，实现 ICardSlot 接口。
    /// 支持接受、替换（由 ShopHangController 控制）、释放卡牌，以及从存档恢复视觉状态。
    /// </summary>
    public class ShopHangSlot : MonoBehaviour, ICardSlot
    {
        [Header("视觉")]
        [SerializeField] private GameObject emptyVisual;
        [SerializeField] private GameObject occupiedOverlay;
        [SerializeField] private Image borderImage;
        [SerializeField] private Color normalBorderColor  = new Color(1f, 1f, 1f, 0.3f);
        [SerializeField] private Color hoverBorderColor   = Color.white;

        private ItemCard hungCard;

        #region ICardSlot 实现

        public bool IsOccupied => hungCard != null;
        public ItemCard OccupiedCard => hungCard;

        /// <summary>
        /// 悬挂槽仅接受 FishData 类型卡牌
        /// </summary>
        public bool CanAccept(ItemCard card)
        {
            return card != null && card.cardData is FishData;
        }

        /// <summary>
        /// 槽位接管卡牌（场景1：空槽接受；场景2：由控制器先 ReleaseCard 后调用此方法）。
        /// 不含 IsOccupied 守卫，由调用方保证槽位已空。
        /// 同时向 CrossHolderSystem 注册槽位卡，使其可被拖拽出槽。
        /// </summary>
        public void AcceptCard(ItemCard card)
        {
            if (card == null) return;

            card.transform.DOKill();
            hungCard = card;
            card.transform.SetParent(this.transform, true);
            card.transform.DOLocalMove(Vector3.zero, 0.2f).SetEase(Ease.OutBack);
            card.transform.localScale = Vector3.one;

            // 注册为槽位卡，使其可被拖拽出槽（场景3）
            CrossHolderSystem.Instance?.RegisterSlotCard(card, this);

            RefreshVisual();
            Debug.Log($"[ShopHangSlot] 接受卡牌：{card.cardData?.itemName}");
        }

        /// <summary>
        /// 槽位放弃当前持有的卡牌，不销毁卡牌，由调用方负责后续处理。
        /// 注销 CrossHolderSystem 中的槽位卡注册。
        /// </summary>
        public void ReleaseCard()
        {
            if (hungCard == null) return;

            CrossHolderSystem.Instance?.UnregisterSlotCard(hungCard);
            hungCard.transform.SetParent(null, true);
            hungCard = null;

            RefreshVisual();
            Debug.Log($"[ShopHangSlot] 释放卡牌");
        }

        public RectTransform GetSlotRect()
        {
            return GetComponent<RectTransform>();
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            RefreshVisual();
        }

        private void OnEnable()
        {
            var slotRect = GetComponent<RectTransform>();
            if (slotRect != null)
                CrossHolderSystem.Instance?.RegisterTarget(slotRect, this);
        }

        private void OnDisable()
        {
            var slotRect = GetComponent<RectTransform>();
            if (slotRect != null)
                CrossHolderSystem.Instance?.UnregisterTarget(slotRect);
        }

        #endregion

        #region Public API

        [Header("图层设置")]
        [Tooltip("商店面板 Canvas 的 sortingOrder，与 ShopHangController.shopPanelSortingOrder 保持一致")]
        [SerializeField] private int shopPanelSortingOrder = 165;

        /// <summary>
        /// 从 ShopManager 存档中恢复视觉（仅在 ShopHangSlot 内部实例化，不触及 HandManager）
        /// </summary>
        public void RestoreCard(FishData data, GameObject fishCardPrefab)
        {
            if (data == null || fishCardPrefab == null) return;

            var cardObj  = Instantiate(fishCardPrefab, this.transform);
            var fishCard = cardObj.GetComponent<FishCard>();
            if (fishCard == null)
            {
                Destroy(cardObj);
                return;
            }

            fishCard.Initialize(data);
            fishCard.SetContextMode(CardContextMode.Hang);
            cardObj.transform.localPosition = Vector3.zero;
            cardObj.transform.localScale    = Vector3.one;

            hungCard = fishCard;

            // 恢复后也注册为槽位卡，以支持拖出功能
            CrossHolderSystem.Instance?.RegisterSlotCard(fishCard, this);

            // 恢复时同步设置视觉卡层级（与拖拽悬挂路径保持一致）
            // 注：FishCard.Start 在下一帧执行，此处延迟一帧确保 cardVisual 已初始化
            StartCoroutine(SetVisualSortingNextFrame(fishCard));

            RefreshVisual();
            Debug.Log($"[ShopHangSlot] 恢复悬挂卡牌：{data.itemName}");
        }

        private System.Collections.IEnumerator SetVisualSortingNextFrame(ItemCard card)
        {
            yield return null;
            card.SetVisualHomeSortingOrder(shopPanelSortingOrder + 1);
        }

        /// <summary>
        /// 清空槽位（商店面板关闭时调用，销毁逻辑卡和视觉卡）
        /// </summary>
        public void ClearSlot()
        {
            if (hungCard != null)
            {
                CrossHolderSystem.Instance?.UnregisterSlotCard(hungCard);
                Destroy(hungCard.gameObject);
                hungCard = null;
            }
            RefreshVisual();
        }

        /// <summary>
        /// 设置悬停高亮状态
        /// </summary>
        public void SetHoverHighlight(bool highlight)
        {
            if (borderImage == null) return;
            borderImage.color = highlight ? hoverBorderColor : normalBorderColor;
        }

        #endregion

        #region Private

        private void RefreshVisual()
        {
            if (emptyVisual != null)
                emptyVisual.SetActive(!IsOccupied);
            if (occupiedOverlay != null)
                occupiedOverlay.SetActive(IsOccupied);
        }

        #endregion
    }
}
