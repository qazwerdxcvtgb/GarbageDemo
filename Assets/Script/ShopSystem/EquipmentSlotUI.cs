using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using ItemSystem;
using FishCardSystem;

/// <summary>
/// 装备槽位 UI 组件
/// 实现 ICardSlot 接口，接受对应类型的装备卡（鱼竿 或 渔轮）。
/// 在 OnEnable/OnDisable 时自动向 CrossHolderSystem 注册/注销 Target。
/// 接受卡牌时调用 EquipmentManager.Equip()；释放时调用 EquipmentManager.Unequip()。
/// </summary>
public class EquipmentSlotUI : MonoBehaviour, ICardSlot
{
    [Header("槽位类型")]
    [Tooltip("此槽位接受的装备类型（鱼竿 或 渔轮）")]
    [SerializeField] private EquipmentSlot allowedSlot;

    [Header("视觉")]
    [SerializeField] private GameObject emptyVisual;
    [SerializeField] private GameObject occupiedOverlay;
    [SerializeField] private Image borderImage;
    [SerializeField] private Color normalBorderColor = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] private Color hoverBorderColor  = Color.white;

    private ItemCard equippedCard;

    #region ICardSlot 实现

    public bool IsOccupied => equippedCard != null;
    public ItemCard OccupiedCard => equippedCard;

    /// <summary>
    /// 仅接受对应 EquipmentSlot 类型的装备卡
    /// </summary>
    public bool CanAccept(ItemCard card)
    {
        if (card == null) return false;
        return card.cardData is EquipmentData ed && ed.slot == allowedSlot;
    }

    /// <summary>
    /// 槽位接管装备卡，触发装备效果。
    /// 调用方保证槽位已空（替换场景下先调用 ReleaseCard）。
    /// </summary>
    public void AcceptCard(ItemCard card)
    {
        if (card == null) return;

        card.transform.DOKill();
        equippedCard = card;
        card.transform.SetParent(this.transform, true);
        card.transform.DOLocalMove(Vector3.zero, 0.2f).SetEase(Ease.OutBack);
        card.transform.localScale = Vector3.one;

        // 注册为槽位卡，使其可被拖拽出槽
        CrossHolderSystem.Instance?.RegisterSlotCard(card, this);

        // 触发装备效果
        EquipmentData equipData = card.cardData as EquipmentData;
        if (equipData != null)
            EquipmentManager.Instance?.Equip(equipData);

        RefreshVisual();
        Debug.Log($"[EquipmentSlotUI] 装备：{card.cardData?.itemName}（槽位：{allowedSlot}）");
    }

    /// <summary>
    /// 槽位放弃当前持有的装备卡，注销效果，由调用方负责后续处理。
    /// </summary>
    public void ReleaseCard()
    {
        if (equippedCard == null) return;

        CrossHolderSystem.Instance?.UnregisterSlotCard(equippedCard);

        // 注销装备效果
        EquipmentData equipData = equippedCard.cardData as EquipmentData;
        if (equipData != null)
            EquipmentManager.Instance?.Unequip(allowedSlot);

        equippedCard.transform.SetParent(null, true);
        equippedCard = null;

        RefreshVisual();
        Debug.Log($"[EquipmentSlotUI] 释放装备（槽位：{allowedSlot}）");
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

    /// <summary>
    /// 设置悬停高亮
    /// </summary>
    public void SetHoverHighlight(bool highlight)
    {
        if (borderImage == null) return;
        borderImage.color = highlight ? hoverBorderColor : normalBorderColor;
    }

    /// <summary>
    /// 清空槽位（面板关闭时调用，销毁卡牌但不触发 Unequip 效果，效果在 ReleaseCard 时已处理）
    /// </summary>
    public void ClearSlot()
    {
        if (equippedCard != null)
        {
            CrossHolderSystem.Instance?.UnregisterSlotCard(equippedCard);
            Destroy(equippedCard.gameObject);
            equippedCard = null;
        }
        RefreshVisual();
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
