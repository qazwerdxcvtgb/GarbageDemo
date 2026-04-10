using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ItemSystem;
using HandSystem;

namespace FishCardSystem
{
    /// <summary>
    /// 手牌"使用"按钮处理器
    /// 职责：强制单选、可用性检查（CanUse）、触发效果并移除卡牌
    /// 独立组件，通过 Inspector 引用 FishCardHolder 和 Button，不修改 HandPanelUI
    /// </summary>
    public class UseCardHandler : MonoBehaviour
    {
        #region Inspector Fields

        [Header("引用")]
        [SerializeField] private FishCardHolder cardHolder;
        [SerializeField] private Button useButton;
        [SerializeField] private TextMeshProUGUI reasonText;

        #endregion

        #region Private State

        private ItemCard currentSelected;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (useButton != null)
            {
                useButton.gameObject.SetActive(false);
                useButton.onClick.AddListener(OnUseButtonClicked);
            }

            StartCoroutine(DeferredInit());
        }

        private System.Collections.IEnumerator DeferredInit()
        {
            yield return null;

            while (HandManager.Instance == null)
                yield return null;

            if (cardHolder != null)
            {
                var existingCards = cardHolder.GetCards();
                foreach (var card in existingCards)
                    SubscribeCard(card);
            }

            HandManager.Instance.OnCardAdded += OnHandCardAdded;
        }

        private void OnDestroy()
        {
            if (useButton != null)
                useButton.onClick.RemoveListener(OnUseButtonClicked);

            if (HandManager.Instance != null)
                HandManager.Instance.OnCardAdded -= OnHandCardAdded;
        }

        #endregion

        #region Card Event Subscription

        private void SubscribeCard(ItemCard card)
        {
            if (card == null) return;
            card.SelectEvent.AddListener(OnCardSelectChanged);
        }

        private void UnsubscribeCard(ItemCard card)
        {
            if (card == null) return;
            card.SelectEvent.RemoveListener(OnCardSelectChanged);
        }

        /// <summary>
        /// HandManager 新增卡牌时，HandPanelUI 会实例化 ItemCard 并加入 FishCardHolder。
        /// 延迟一帧后对新卡补订阅 SelectEvent（确保 AddCard 流程完成）。
        /// </summary>
        private void OnHandCardAdded(ItemData item)
        {
            StartCoroutine(SubscribeNewCardsNextFrame());
        }

        private System.Collections.IEnumerator SubscribeNewCardsNextFrame()
        {
            yield return null;
            if (cardHolder == null) yield break;

            foreach (var card in cardHolder.GetCards())
            {
                card.SelectEvent.RemoveListener(OnCardSelectChanged);
                card.SelectEvent.AddListener(OnCardSelectChanged);
            }
        }

        #endregion

        #region Selection & Usability

        private void OnCardSelectChanged(ItemCard card, bool isSelected)
        {
            if (isSelected)
            {
                if (currentSelected != null && currentSelected != card)
                    currentSelected.Deselect();

                currentSelected = card;
                RefreshButton();
            }
            else
            {
                if (card == currentSelected)
                {
                    currentSelected = null;
                    HideButton();
                }
            }
        }

        private void RefreshButton()
        {
            if (useButton == null || currentSelected == null)
            {
                HideButton();
                return;
            }

            if (currentSelected.cardData is EquipmentData)
            {
                HideButton();
                return;
            }

            useButton.gameObject.SetActive(true);

            bool canUse = currentSelected.cardData.CanUse(out string reason);
            useButton.interactable = canUse;

            if (reasonText != null)
                reasonText.text = canUse ? "" : reason;
        }

        private void HideButton()
        {
            if (useButton != null)
                useButton.gameObject.SetActive(false);

            if (reasonText != null)
                reasonText.text = "";
        }

        #endregion

        #region Use Execution

        private void OnUseButtonClicked()
        {
            if (currentSelected == null) return;

            ItemData data = currentSelected.cardData;
            if (data == null || data is EquipmentData) return;

            if (!data.CanUse(out _)) return;

            if (data is FishData fish)
                fish.TriggerUseEffects();
            else if (data is TrashData trash)
                trash.TriggerUseEffects();
            else if (data is ConsumableData consumable)
                consumable.TriggerUseEffects();

            ItemCard cardToRemove = currentSelected;
            currentSelected = null;
            HideButton();

            if (cardHolder != null)
            {
                UnsubscribeCard(cardToRemove);
                cardHolder.RemoveCardAndCollapse(cardToRemove);
            }

            if (data != null)
                HandManager.Instance?.RemoveCard(data);
        }

        #endregion
    }
}
