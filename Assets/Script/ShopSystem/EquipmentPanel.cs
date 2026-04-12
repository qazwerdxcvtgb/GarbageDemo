using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using FishCardSystem;
using HandSystem;
using ItemSystem;

/// <summary>
/// 装备面板主控
/// 独立面板，通过事件呼出/关闭（由场景中按钮的 EquipmentPanelToggleEvent 触发）。
/// 持有鱼竿槽和渔轮槽两个 EquipmentSlotUI，处理以下场景：
/// 场景1：手牌装备卡 → 空槽（直接装备）
/// 场景2：手牌装备卡 → 已有卡的槽（替换：旧卡回手牌，新卡装备）— 仅钓鱼准备模式
/// 场景3：槽内装备卡 → 手牌区域（卸下：卡牌归还手牌）— 仅钓鱼准备模式
/// 面板打开时强制展开手牌，关闭时恢复。
///
/// 锁定机制：
/// - 默认锁定：装备可装入空槽，但不可取下或替换
/// - 钓鱼准备模式（OpenPanelForFishing）：完全解锁，可自由装备/取下/替换
/// </summary>
public class EquipmentPanel : MonoBehaviour
{
    public static EquipmentPanel Instance { get; private set; }

    [Header("面板根节点")]
    [SerializeField] private GameObject panelRoot;

    [Header("装备槽位")]
    [SerializeField] private EquipmentSlotUI rodSlot;
    [SerializeField] private EquipmentSlotUI gearSlot;

    [Header("关闭按钮")]
    [SerializeField] private Button closeButton;

    [Header("钓鱼准备模式")]
    [Tooltip("确认按钮，钓鱼准备模式下替代关闭按钮")]
    [SerializeField] private Button confirmButton;
    [Tooltip("全屏遮罩，钓鱼准备模式下覆盖钓鱼页面以做视觉区分")]
    [SerializeField] private GameObject fishingOverlay;

    [Header("依赖引用")]
    [SerializeField] private FishCardSystem.HandPanelUI handPanelUI;

    [Header("图层设置")]
    [Tooltip("装备面板 Canvas 的 sortingOrder，视觉卡将设置为此值 +1")]
    [SerializeField] private int panelSortingOrder = 170;

    [Header("动画参数")]
    [SerializeField] private float animDuration = 0.3f;
    [SerializeField] private Ease  animEase     = Ease.OutCubic;

    private RectTransform panelRect;
    private Vector2       openPosition;
    private bool isOpen;

    /// <summary>是否允许取下和替换装备（仅钓鱼准备模式为 true）</summary>
    private bool allowRemoveAndReplace;

    /// <summary>外部锁定关闭状态（偷看牌堆等效果使用时禁止打开面板）</summary>
    private bool isOpenLocked;

    /// <summary>钓鱼准备模式关闭后的回调（装备确认后触发每日刷新等）</summary>
    private System.Action onFishingConfirmedCallback;

    /// <summary>面板下所有装备槽位（Awake 中自动收集，不依赖 Inspector 赋值）</summary>
    private EquipmentSlotUI[] equipSlots;

    /// <summary>供 EquipmentSlotUI.CanAccept 读取锁定状态</summary>
    public bool AllowRemoveAndReplace => allowRemoveAndReplace;

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);

        panelRect    = panelRoot?.GetComponent<RectTransform>();
        openPosition = panelRect != null ? panelRect.anchoredPosition : Vector2.zero;

        if (panelRoot != null)
            panelRoot.SetActive(false);
        if (fishingOverlay != null)
            fishingOverlay.SetActive(false);
        if (confirmButton != null)
            confirmButton.gameObject.SetActive(false);

        // 确保面板具有独立 Canvas，使其渲染在手牌面板之下、商店面板之上
        Canvas selfCanvas = GetComponent<Canvas>();
        if (selfCanvas == null)
            selfCanvas = gameObject.AddComponent<Canvas>();
        selfCanvas.overrideSorting = true;
        selfCanvas.sortingOrder    = panelSortingOrder;

        if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        equipSlots = GetComponentsInChildren<EquipmentSlotUI>(true);
    }

    private void Start()
    {
        if (handPanelUI == null)
            handPanelUI = FindObjectOfType<FishCardSystem.HandPanelUI>();

        if (CrossHolderSystem.Instance != null)
        {
            CrossHolderSystem.Instance.OnCardDroppedToSlot.AddListener(OnCardDroppedToSlot);
            CrossHolderSystem.Instance.OnCardEjectedToHand.AddListener(OnCardEjectedToHand);
        }
    }

    private void OnDestroy()
    {
        if (CrossHolderSystem.Instance != null)
        {
            CrossHolderSystem.Instance.OnCardDroppedToSlot.RemoveListener(OnCardDroppedToSlot);
            CrossHolderSystem.Instance.OnCardEjectedToHand.RemoveListener(OnCardEjectedToHand);
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// 打开装备面板（普通模式：仅允许装入空槽，不可取下/替换）
    /// </summary>
    public void OpenPanel()
    {
        if (isOpen || isOpenLocked) return;
        isOpen = true;

        // 普通模式：显示关闭按钮，隐藏确认按钮和遮罩
        if (closeButton != null)
            closeButton.gameObject.SetActive(true);
        if (confirmButton != null)
            confirmButton.gameObject.SetActive(false);
        if (fishingOverlay != null)
            fishingOverlay.SetActive(false);

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (panelRect != null)
        {
            panelRect.DOKill();
            float offscreenX = openPosition.x + panelRect.rect.width;
            panelRect.anchoredPosition = new Vector2(offscreenX, openPosition.y);
            panelRect.DOAnchorPos(openPosition, animDuration).SetEase(animEase);
        }

        handPanelUI?.LockExpanded();

        Debug.Log("[EquipmentPanel] 面板打开（普通模式）");
    }

    /// <summary>
    /// 以钓鱼准备模式打开装备面板（完全解锁：可装备/取下/替换）。
    /// 由 DayManager.OnDeclarationChoice(Fishing) 调用。
    /// 显示全屏遮罩和确认按钮，保留关闭按钮。
    /// </summary>
    /// <param name="onConfirmed">面板关闭时的回调，用于延迟触发装备每日刷新效果</param>
    public void OpenPanelForFishing(System.Action onConfirmed = null)
    {
        if (isOpenLocked) return;

        onFishingConfirmedCallback = onConfirmed;

        allowRemoveAndReplace = true;

        // 钓鱼准备模式：显示确认按钮和遮罩，保留关闭按钮
        if (confirmButton != null)
            confirmButton.gameObject.SetActive(true);
        if (fishingOverlay != null)
            fishingOverlay.SetActive(true);

        // 解锁已有槽位卡的拖拽
        SetSlotCardsLocked(false);

        if (!isOpen)
        {
            isOpen = true;

            if (panelRoot != null)
                panelRoot.SetActive(true);

            if (panelRect != null)
            {
                panelRect.DOKill();
                float offscreenX = openPosition.x + panelRect.rect.width;
                panelRect.anchoredPosition = new Vector2(offscreenX, openPosition.y);
                panelRect.DOAnchorPos(openPosition, animDuration).SetEase(animEase);
            }

            handPanelUI?.LockExpanded();
        }

        Debug.Log("[EquipmentPanel] 面板打开（钓鱼准备模式 - 解锁取下/替换）");
    }

    /// <summary>
    /// 关闭装备面板（同时重置锁定状态）
    /// </summary>
    public void ClosePanel()
    {
        if (!isOpen) return;
        isOpen = false;

        bool wasFishingMode = allowRemoveAndReplace;
        allowRemoveAndReplace = false;

        // 关闭时锁定槽位卡的拖拽
        SetSlotCardsLocked(true);

        if (fishingOverlay != null)
            fishingOverlay.SetActive(false);

        if (panelRect != null)
        {
            panelRect.DOKill();
            float offscreenX = openPosition.x + panelRect.rect.width;
            panelRect.DOAnchorPos(new Vector2(offscreenX, openPosition.y), animDuration)
                     .SetEase(animEase)
                     .OnComplete(() =>
                     {
                         if (panelRoot != null)
                             panelRoot.SetActive(false);
                     });
        }
        else if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        handPanelUI?.UnlockExpanded();

        if (wasFishingMode)
        {
            onFishingConfirmedCallback?.Invoke();
            onFishingConfirmedCallback = null;
        }

        Debug.Log("[EquipmentPanel] 面板关闭");
    }

    /// <summary>
    /// 钓鱼准备模式确认按钮回调
    /// </summary>
    private void OnConfirmClicked()
    {
        ClosePanel();
    }

    /// <summary>
    /// 切换装备面板开关状态（供外部按钮直接绑定，始终以普通模式打开）
    /// </summary>
    public void TogglePanel()
    {
        if (isOpen)
            ClosePanel();
        else
            OpenPanel();
    }

    /// <summary>
    /// 锁定关闭状态（偷看牌堆时调用）：若面板已打开则关闭，并禁止打开
    /// </summary>
    public void LockClosed()
    {
        isOpenLocked = true;
        if (isOpen) ClosePanel();
    }

    /// <summary>
    /// 解除关闭锁定（偷看结束时调用）
    /// </summary>
    public void UnlockClosed()
    {
        isOpenLocked = false;
    }

    #endregion

    #region Slot Card Lock

    /// <summary>
    /// 设置所有装备槽位中卡牌的 isLocked 状态，控制是否可被拖拽
    /// </summary>
    private void SetSlotCardsLocked(bool locked)
    {
        if (equipSlots == null) return;
        foreach (var slot in equipSlots)
        {
            if (slot != null && slot.IsOccupied)
                slot.OccupiedCard.isLocked = locked;
        }
    }

    #endregion

    #region Drop Handler（场景1 & 场景2）

    private void OnCardDroppedToSlot(ItemCard card, ICardSlot targetSlot)
    {
        if (!(targetSlot is EquipmentSlotUI equipSlot)) return;
        // 通过层级归属判断，不依赖 Inspector 引用，避免未赋值时失效
        if (!equipSlot.transform.IsChildOf(transform)) return;

        // 场景2：槽位已有装备 → 先将旧卡归还手牌
        if (equipSlot.IsOccupied)
        {
            EjectCardToHand(equipSlot.OccupiedCard, equipSlot);
        }

        // 场景1 / 场景2 后续：执行装备
        ExecuteEquip(card, equipSlot);
    }

    private void ExecuteEquip(ItemCard card, EquipmentSlotUI slot)
    {
        EquipmentData equipData = card.cardData as EquipmentData;
        if (equipData == null)
        {
            Debug.LogWarning("[EquipmentPanel] 目标卡牌不含 EquipmentData，取消装备");
            return;
        }

        FishCardHolder handHolder = handPanelUI?.CardHolder;

        // 保存旧容器引用（AcceptCard 执行后 card.transform.parent 会变更）
        Transform oldContainer = card.transform.parent;

        // 修复闪烁：先接管卡牌（card 立即切换父节点到槽位，始终留在 Canvas 层级），
        // 再将旧容器从 Canvas 层级中摘除，防止 card 随 oldContainer 短暂脱离 Canvas 导致 SmoothFollow 读到错误坐标
        slot.AcceptCard(card);

        if (oldContainer != null && oldContainer != handHolder?.transform)
            oldContainer.SetParent(null);

        // 数据层清理（此时 card 已不在 FishCardHolder 层级，SetSlotCount 不会干扰）
        // 注意：handHolder.RemoveCard 内部会调用 UnsubscribeSourceCard，会清除 AcceptCard 中注册的拖拽监听，
        // 因此需要在清理完成后重新注册，确保槽位卡的拖拽事件有效。
        handHolder?.RemoveCard(card);
        HandManager.Instance?.RemoveCard(equipData);

        // 手牌清理后重新注册槽位拖拽监听（防止 UnsubscribeSourceCard 清除了 AcceptCard 的注册）
        CrossHolderSystem.Instance?.RegisterSlotCard(card, slot);

        // 设置视觉卡归属层级（装备面板 sortingOrder + 1）
        card.SetVisualHomeSortingOrder(panelSortingOrder + 1);

        // 非解锁模式下锁定卡牌拖拽
        card.isLocked = !allowRemoveAndReplace;

        if (oldContainer != null)
            Destroy(oldContainer.gameObject);

        Debug.Log($"[EquipmentPanel] 装备成功：{equipData.itemName}");
    }

    #endregion

    #region Eject Handler（场景3）

    private void OnCardEjectedToHand(ItemCard card, ICardSlot sourceSlot)
    {
        if (!(sourceSlot is EquipmentSlotUI equipSlot)) return;
        if (!equipSlot.transform.IsChildOf(transform)) return;

        if (!allowRemoveAndReplace)
        {
            card.transform.DOKill();
            card.transform.DOLocalMove(Vector3.zero, 0.2f).SetEase(Ease.OutBack);
            return;
        }

        EjectCardToHand(card, equipSlot);
    }

    /// <summary>
    /// 将装备槽位中的卡牌归还手牌（场景2旧卡清场 和 场景3 均调用此方法）
    /// </summary>
    private void EjectCardToHand(ItemCard card, EquipmentSlotUI slot)
    {
        EquipmentData equipData = card.cardData as EquipmentData;

        card.isLocked = false;

        // 从槽位释放（含 EquipmentManager.Unequip + CrossHolderSystem 注销）
        slot.ReleaseCard();

        // 归还手牌数据层（仅数据+OnHandChanged，不触发 OnCardAdded，避免 HandPanelUI 重复创建视觉卡）
        HandManager.Instance?.AddCardData(equipData);

        // 归还手牌视觉层：将已有视觉卡直接放回 FishCardHolder
        FishCardHolder handHolder = handPanelUI?.CardHolder;
        if (handHolder != null)
            handHolder.AddCard(card);

        // 归还手牌后重置视觉卡层级（继承 VisualCardsHandler 默认层）
        card.SetVisualHomeSortingOrder(0);

        Debug.Log($"[EquipmentPanel] 装备卡归还手牌：{equipData?.itemName}");
    }

    #endregion
}
