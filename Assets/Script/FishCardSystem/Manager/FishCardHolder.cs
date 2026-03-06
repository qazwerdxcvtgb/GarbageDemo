using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

namespace FishCardSystem
{
    /// <summary>
    /// 鱼类卡牌容器管理器
    /// 负责：槽位生成、卡牌事件绑定、拖拽排序、返回动画
    /// </summary>
    public class FishCardHolder : MonoBehaviour
    {
        [Header("槽位设置")]
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private int cardsToSpawn = 7;

        [Header("动画设置")]
        [SerializeField] private bool tweenCardReturn = true;

        private RectTransform rect;
        private List<FishCard> cards;
        private FishCard selectedCard;
        private FishCard hoveredCard;
        private bool isCrossing;

        private void Start()
        {
            rect = GetComponent<RectTransform>();
            cards = new List<FishCard>();

            // 生成槽位
            for (int i = 0; i < cardsToSpawn; i++)
            {
                Instantiate(slotPrefab, transform);
            }

            // 收集所有卡牌
            cards = GetComponentsInChildren<FishCard>().ToList();

            // 绑定事件
            foreach (var card in cards)
            {
                card.PointerEnterEvent.AddListener(OnCardPointerEnter);
                card.PointerExitEvent.AddListener(OnCardPointerExit);
                card.BeginDragEvent.AddListener(OnCardBeginDrag);
                card.EndDragEvent.AddListener(OnCardEndDrag);
            }

            // 延迟更新视觉卡索引
            StartCoroutine(DelayedUpdateIndices());
        }

        private IEnumerator DelayedUpdateIndices()
        {
            yield return new WaitForSeconds(0.1f);

            foreach (var card in cards)
            {
                if (card.cardVisual != null)
                {
                    card.cardVisual.UpdateIndex();
                }
            }
        }

        private void Update()
        {
            // 拖拽排序逻辑
            if (selectedCard == null || isCrossing)
                return;

            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == selectedCard)
                    continue;

                // 向右拖拽，且越过了右边的卡
                if (selectedCard.transform.position.x > cards[i].transform.position.x &&
                    selectedCard.ParentIndex() < cards[i].ParentIndex())
                {
                    Swap(i);
                    break;
                }
                // 向左拖拽，且越过了左边的卡
                else if (selectedCard.transform.position.x < cards[i].transform.position.x &&
                         selectedCard.ParentIndex() > cards[i].ParentIndex())
                {
                    Swap(i);
                    break;
                }
            }
        }

        private void OnCardPointerEnter(FishCard card)
        {
            hoveredCard = card;
        }

        private void OnCardPointerExit(FishCard card)
        {
            if (hoveredCard == card)
            {
                hoveredCard = null;
            }
        }

        private void OnCardBeginDrag(FishCard card)
        {
            selectedCard = card;
        }

        private void OnCardEndDrag(FishCard card)
        {
            if (selectedCard == card)
            {
                // 返回动画
                Vector3 targetLocalPos = card.selected ?
                    new Vector3(0, card.selectionOffset, 0) : Vector3.zero;

                float duration = tweenCardReturn ? 0.15f : 0f;
                card.transform.DOLocalMove(targetLocalPos, duration).SetEase(Ease.OutBack);

                // 微调RectTransform触发布局更新
                rect.sizeDelta += Vector2.one * 0.01f;
                rect.sizeDelta -= Vector2.one * 0.01f;

                selectedCard = null;
            }
        }

        private void Swap(int index)
        {
            isCrossing = true;

            // 记录交换前的父节点
            Transform selectedParent = selectedCard.transform.parent;
            Transform targetParent = cards[index].transform.parent;

            // 目标卡归位到 selectedCard 的原槽位
            cards[index].transform.SetParent(selectedParent);
            cards[index].transform.localPosition = cards[index].selected ?
                new Vector3(0, cards[index].selectionOffset, 0) : Vector3.zero;

            // 拖拽卡换到目标卡的原槽位，不重置 localPosition，保持跟随鼠标
            selectedCard.transform.SetParent(targetParent);

            // 触发交换动画
            if (cards[index].cardVisual != null)
            {
                bool swapIsRight = cards[index].ParentIndex() > selectedCard.ParentIndex();
                cards[index].cardVisual.Swap(swapIsRight ? -1 : 1);
            }

            // 更新所有卡牌的视觉索引
            foreach (var card in cards)
            {
                if (card.cardVisual != null)
                {
                    card.cardVisual.UpdateIndex();
                }
            }

            isCrossing = false;
        }

        /// <summary>
        /// 获取容器中的所有卡牌
        /// </summary>
        public List<FishCard> GetCards()
        {
            return new List<FishCard>(cards);
        }

        /// <summary>
        /// 添加卡牌到容器
        /// </summary>
        public void AddCard(FishCard card, int slotIndex = -1)
        {
            if (slotIndex < 0 || slotIndex >= transform.childCount)
            {
                // 添加到第一个空槽位
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform slot = transform.GetChild(i);
                    if (slot.childCount == 0)
                    {
                        card.transform.SetParent(slot);
                        card.transform.localPosition = Vector3.zero;
                        break;
                    }
                }
            }
            else
            {
                Transform slot = transform.GetChild(slotIndex);
                card.transform.SetParent(slot);
                card.transform.localPosition = Vector3.zero;
            }

            if (!cards.Contains(card))
            {
                cards.Add(card);

                // 绑定事件
                card.PointerEnterEvent.AddListener(OnCardPointerEnter);
                card.PointerExitEvent.AddListener(OnCardPointerExit);
                card.BeginDragEvent.AddListener(OnCardBeginDrag);
                card.EndDragEvent.AddListener(OnCardEndDrag);
            }
        }

        /// <summary>
        /// 从容器移除卡牌
        /// </summary>
        public void RemoveCard(FishCard card)
        {
            if (cards.Contains(card))
            {
                cards.Remove(card);

                // 取消事件绑定
                card.PointerEnterEvent.RemoveListener(OnCardPointerEnter);
                card.PointerExitEvent.RemoveListener(OnCardPointerExit);
                card.BeginDragEvent.RemoveListener(OnCardBeginDrag);
                card.EndDragEvent.RemoveListener(OnCardEndDrag);
            }
        }
    }
}
