using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ItemSystem;

namespace FishCardSystem
{
    /// <summary>
    /// 选择面板中的单卡槽位容器。
    /// 包裹一张逻辑卡（ItemCard），管理选中高亮和点击交互。
    /// 不修改现有 ItemCard / FishCardVisual 代码。
    /// </summary>
    public class SelectionSlot : MonoBehaviour
    {
        [Header("结构")]
        [SerializeField] private Transform cardContainer;
        [SerializeField] private GameObject selectedFrame;
        [SerializeField] private Button clickArea;

        [Header("信息标签")]
        [SerializeField] private TextMeshProUGUI infoLabel;

        private ItemCard card;
        private ItemData cardData;
        private bool isSelected;

        public event Action<SelectionSlot> OnClicked;

        public ItemCard Card => card;
        public ItemData CardData => cardData;
        public bool IsSelected => isSelected;

        private void Awake()
        {
            if (selectedFrame != null)
                selectedFrame.SetActive(false);

            if (infoLabel != null)
                infoLabel.text = "";

            if (clickArea != null)
                clickArea.onClick.AddListener(() => OnClicked?.Invoke(this));
        }

        private void OnDestroy()
        {
            if (clickArea != null)
                clickArea.onClick.RemoveAllListeners();
        }

        /// <summary>
        /// 获取卡牌实例化父节点（同时作为 visualParentOverride）
        /// </summary>
        public Transform GetCardContainer() => cardContainer;

        /// <summary>
        /// 绑定逻辑卡引用和数据
        /// </summary>
        public void Setup(ItemCard itemCard, ItemData data)
        {
            card = itemCard;
            cardData = data;
        }

        /// <summary>
        /// 设置选中状态，控制高亮框的显隐
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            if (selectedFrame != null)
                selectedFrame.SetActive(selected);
        }

        /// <summary>
        /// 设置信息标签文本。传入 null 或空字符串时隐藏标签。
        /// </summary>
        public void SetInfoText(string text)
        {
            if (infoLabel == null) return;
            infoLabel.text = string.IsNullOrEmpty(text) ? "" : text;
        }

        /// <summary>
        /// 清理并销毁持有的逻辑卡（视觉卡会在逻辑卡 OnDestroy 中自动销毁）
        /// </summary>
        public void Cleanup()
        {
            if (card != null)
            {
                Destroy(card.gameObject);
                card = null;
            }
            cardData = null;
            isSelected = false;
        }
    }
}
