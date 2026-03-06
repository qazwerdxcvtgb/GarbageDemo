using UnityEngine;
using UnityEngine.EventSystems;
using ItemSystem;
using FishCardSystem;

namespace FishingSystem
{
    /// <summary>
    /// 牌堆状态枚举
    /// </summary>
    public enum PileState
    {
        Empty,      // 空牌堆
        FaceDown,   // 卡背朝上（未翻开）
        FaceUp      // 正面朝上（已翻开）
    }

    /// <summary>
    /// 单个牌堆槽位控制器
    /// 管理槽位对应的深度、子池索引、当前卡牌和状态
    /// </summary>
    public class CardPileSlot : MonoBehaviour, IPointerClickHandler
    {
        #region Fields

        [Header("牌堆配置")]
        public FishDepth depth;              // 深度
        public int poolIndex;                // 子池索引（0-2）

        [Header("状态")]
        public PileState currentState = PileState.Empty;

        [Header("引用")]
        public FishCard currentCard;         // 当前显示的卡牌
        public Transform cardSpawnPoint;     // 卡牌生成位置

        [Header("预制体")]
        [SerializeField] private GameObject fishCardPrefab;  // FishCard预制体

        [Header("调试")]
        [SerializeField] private bool showDebugInfo = true;

        #endregion

        #region Events

        /// <summary>
        /// 槽位点击事件
        /// </summary>
        public event System.Action<CardPileSlot> OnSlotClicked;

        #endregion

        #region Initialization

        /// <summary>
        /// 初始化槽位
        /// </summary>
        public void Initialize(FishDepth depth, int poolIndex)
        {
            this.depth = depth;
            this.poolIndex = poolIndex;

            // 如果没有指定卡牌生成位置，使用自身
            if (cardSpawnPoint == null)
            {
                cardSpawnPoint = transform;
            }

            // 刷新显示
            RefreshDisplay();
        }

        #endregion

        #region Display Management

        /// <summary>
        /// 刷新显示（从ItemPool获取顶牌）
        /// </summary>
        public void RefreshDisplay()
        {
            // 清除旧卡牌
            ClearSlot();

            // 从ItemPool获取顶牌
            FishData topCard = ItemPool.Instance.PeekTopCard(depth, poolIndex);

            if (topCard == null)
            {
                // 牌堆为空
                currentState = PileState.Empty;
                if (showDebugInfo)
                {
                    Debug.Log($"[CardPileSlot] 牌堆为空：深度={depth}, 子池={poolIndex}");
                }
                return;
            }

            // 实例化FishCard
            if (fishCardPrefab == null)
            {
                Debug.LogError($"[CardPileSlot] FishCardPrefab 未分配！");
                return;
            }

            GameObject cardObj = Instantiate(fishCardPrefab, cardSpawnPoint);
            cardObj.name = $"FishCard_{topCard.itemName}";

            currentCard = cardObj.GetComponent<FishCard>();
            if (currentCard == null)
            {
                Debug.LogError($"[CardPileSlot] 预制体缺少 FishCard 组件！");
                Destroy(cardObj);
                return;
            }

            // 初始化卡牌数据
            currentCard.Initialize(topCard);

            // 设置为牌堆模式（禁用拖拽等手牌功能）
            currentCard.SetPileMode(true);

            // 根据当前状态显示正面或背面
            if (currentState == PileState.FaceUp)
            {
                // 已翻开：显示正面
                currentCard.FlipToFront(0f);
            }
            else
            {
                // 未翻开：显示背面
                currentState = PileState.FaceDown;
                currentCard.FlipToBack(0f);
            }

            if (showDebugInfo)
            {
                Debug.Log($"[CardPileSlot] 显示卡牌：{topCard.itemName}, 状态={currentState}");
            }
        }

        /// <summary>
        /// 标记为已翻开（正面朝上）
        /// </summary>
        public void SetRevealed()
        {
            currentState = PileState.FaceUp;

            if (showDebugInfo)
            {
                Debug.Log($"[CardPileSlot] 标记为已翻开：深度={depth}, 子池={poolIndex}");
            }
        }

        /// <summary>
        /// 清空槽位
        /// </summary>
        public void ClearSlot()
        {
            if (currentCard != null)
            {
                Destroy(currentCard.gameObject);
                currentCard = null;
            }
        }

        #endregion

        #region Input Handling

        /// <summary>
        /// 点击处理
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            // 触发点击事件
            OnSlotClicked?.Invoke(this);
        }

        #endregion

        #region Debug

        private void OnValidate()
        {
            // 在编辑器中修改时更新名称
            if (cardSpawnPoint != null)
            {
                gameObject.name = $"PileSlot (Depth{(int)depth + 1}_Pool{poolIndex})";
            }
        }

        #endregion
    }
}
