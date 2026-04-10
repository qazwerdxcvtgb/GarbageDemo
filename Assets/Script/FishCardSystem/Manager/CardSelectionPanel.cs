using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ItemSystem;

namespace FishCardSystem
{
    /// <summary>
    /// 可配置的卡牌选择面板（预制体）。
    /// 源无关设计：接收卡牌列表并通过回调返回选择结果，牌库操作由调用方负责。
    /// </summary>
    public class CardSelectionPanel : MonoBehaviour
    {
        #region Inspector

        [Header("图层设置")]
        [SerializeField] private int sortingOrder = 175;

        [Header("面板结构")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform slotContainer;
        [SerializeField] private GameObject selectionSlotPrefab;

        [Header("按钮")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private TextMeshProUGUI buttonText;

        [Header("卡牌预制体")]
        [SerializeField] private GameObject fishCardPrefab;
        [SerializeField] private GameObject trashCardPrefab;
        [SerializeField] private GameObject consumableCardPrefab;
        [SerializeField] private GameObject equipmentCardPrefab;

        #endregion

        #region Types

        public delegate void SelectionCallback(List<ItemData> selected, List<ItemData> rejected);

        #endregion

        #region Runtime State

        private readonly List<SelectionSlot> activeSlots = new List<SelectionSlot>();
        private readonly LinkedList<SelectionSlot> selectionOrder = new LinkedList<SelectionSlot>();

        private int maxSelectCount;
        private SelectionCallback onComplete;
        private bool isOpen;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Canvas panelCanvas = GetComponent<Canvas>();
            if (panelCanvas == null)
                panelCanvas = gameObject.AddComponent<Canvas>();

            panelCanvas.overrideSorting = true;
            panelCanvas.sortingOrder = sortingOrder;

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmClicked);

            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(OnConfirmClicked);
        }

        #endregion

        #region Public API

        /// <summary>
        /// 打开选择面板。
        /// </summary>
        /// <param name="offeredCards">展示的卡牌列表（已由调用方从牌库中抽出）</param>
        /// <param name="maxSelect">最大可选数量；0 表示无需选择（按钮显示"取消"）</param>
        /// <param name="callback">选择完成回调，返回选中和未选中的卡牌列表</param>
        /// <param name="slotLabels">每个槽位的信息标签文本（可选，null 时全部隐藏）</param>
        public void Open(List<ItemData> offeredCards, int maxSelect,
                         SelectionCallback callback, List<string> slotLabels = null)
        {
            if (offeredCards == null || offeredCards.Count == 0)
            {
                Debug.LogWarning("[CardSelectionPanel] 传入的卡牌列表为空");
                callback?.Invoke(new List<ItemData>(), new List<ItemData>());
                return;
            }

            ClearSlots();

            maxSelectCount = maxSelect;
            onComplete = callback;
            isOpen = true;

            if (panelRoot != null)
                panelRoot.SetActive(true);

            for (int i = 0; i < offeredCards.Count; i++)
            {
                if (offeredCards[i] == null) continue;
                string label = (slotLabels != null && i < slotLabels.Count) ? slotLabels[i] : null;
                CreateSlot(offeredCards[i], label);
            }

            if (maxSelectCount == 0)
            {
                if (buttonText != null) buttonText.text = "取消";
                if (confirmButton != null) confirmButton.interactable = true;
            }
            else
            {
                if (buttonText != null) buttonText.text = "确认";
                UpdateConfirmState();
            }

            Debug.Log($"[CardSelectionPanel] 面板已打开：展示 {activeSlots.Count} 张卡牌，需选择 {maxSelectCount} 张");
        }

        /// <summary>
        /// 面板是否处于打开状态
        /// </summary>
        public bool IsOpen => isOpen;

        #endregion

        #region Slot Management

        private void CreateSlot(ItemData data, string label = null)
        {
            if (selectionSlotPrefab == null || slotContainer == null) return;

            GameObject slotObj = Instantiate(selectionSlotPrefab, slotContainer);
            SelectionSlot slot = slotObj.GetComponent<SelectionSlot>();
            if (slot == null)
            {
                Destroy(slotObj);
                return;
            }

            GameObject cardPrefab = GetCardPrefab(data);
            if (cardPrefab == null)
            {
                Debug.LogWarning($"[CardSelectionPanel] 找不到 {data.category} 类型的卡牌预制体");
                Destroy(slotObj);
                return;
            }

            Transform container = slot.GetCardContainer();
            GameObject cardObj = Instantiate(cardPrefab, container);
            cardObj.transform.localPosition = Vector3.zero;
            cardObj.transform.localScale = Vector3.one;

            ItemCard itemCard = cardObj.GetComponent<ItemCard>();
            if (itemCard == null)
            {
                Destroy(cardObj);
                Destroy(slotObj);
                return;
            }

            itemCard.visualParentOverride = container;
            itemCard.Initialize(data);
            itemCard.SetContextMode(CardContextMode.Pile);

            slot.Setup(itemCard, data);
            slot.SetInfoText(label);
            slot.OnClicked += OnSlotClicked;
            activeSlots.Add(slot);
        }

        private void ClearSlots()
        {
            foreach (var slot in activeSlots)
            {
                if (slot != null)
                {
                    slot.OnClicked -= OnSlotClicked;
                    slot.Cleanup();
                    Destroy(slot.gameObject);
                }
            }
            activeSlots.Clear();
            selectionOrder.Clear();
        }

        private GameObject GetCardPrefab(ItemData data)
        {
            if (data is FishData)        return fishCardPrefab;
            if (data is TrashData)       return trashCardPrefab;
            if (data is ConsumableData)  return consumableCardPrefab;
            if (data is EquipmentData)   return equipmentCardPrefab;
            return null;
        }

        #endregion

        #region Selection Logic

        private void OnSlotClicked(SelectionSlot slot)
        {
            if (!isOpen || maxSelectCount == 0) return;

            if (slot.IsSelected)
            {
                slot.SetSelected(false);
                selectionOrder.Remove(slot);
            }
            else
            {
                slot.SetSelected(true);
                selectionOrder.AddLast(slot);

                while (selectionOrder.Count > maxSelectCount)
                {
                    SelectionSlot oldest = selectionOrder.First.Value;
                    oldest.SetSelected(false);
                    selectionOrder.RemoveFirst();
                }
            }

            UpdateConfirmState();
        }

        private void UpdateConfirmState()
        {
            if (confirmButton == null) return;
            confirmButton.interactable = selectionOrder.Count >= maxSelectCount;
        }

        #endregion

        #region Confirm / Cancel

        private void OnConfirmClicked()
        {
            if (!isOpen) return;

            var selected = new List<ItemData>();
            var rejected = new List<ItemData>();

            foreach (var slot in activeSlots)
            {
                if (slot == null) continue;
                if (slot.IsSelected)
                    selected.Add(slot.CardData);
                else
                    rejected.Add(slot.CardData);
            }

            ClosePanel();

            onComplete?.Invoke(selected, rejected);
        }

        private void ClosePanel()
        {
            isOpen = false;
            ClearSlots();

            if (panelRoot != null)
                panelRoot.SetActive(false);

            Debug.Log("[CardSelectionPanel] 面板已关闭");
        }

        #endregion
    }
}
