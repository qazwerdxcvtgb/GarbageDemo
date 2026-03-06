using UnityEngine;
using UnityEngine.UI;
using ItemSystem;

namespace FishingSystem
{
    /// <summary>
    /// 钓鱼牌桌主控制器
    /// 管理9个牌堆槽位的初始化和交互
    /// </summary>
    public class FishingTablePanel : MonoBehaviour
    {
        #region 单例模式

        private static FishingTablePanel instance;
        public static FishingTablePanel Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<FishingTablePanel>();
                    if (instance == null)
                    {
                        Debug.LogError("[FishingTablePanel] 场景中未找到FishingTablePanel对象");
                    }
                }
                return instance;
            }
        }

        #endregion

        #region Fields

        [Header("UI引用")]
        [SerializeField] private Transform pileGridContainer;      // 3×3网格容器
        [SerializeField] private GameObject cardPileSlotPrefab;    // 槽位预制体
        [SerializeField] private RevealOverlayPanel revealOverlay; // 揭示遮罩面板

        [Header("组件引用")]
        [SerializeField] private CharacterState playerState;       // 玩家状态

        [Header("调试信息")]
        [SerializeField] private bool showDebugInfo = true;

        // 9个槽位（索引0-8：0-2深度1，3-5深度2，6-8深度3）
        private CardPileSlot[] pileSlots = new CardPileSlot[9];

        // 当前正在操作的槽位
        private CardPileSlot currentSlot;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // 单例初始化
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        private void Start()
        {
            InitializePileSlots();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 初始化9个牌堆槽位
        /// </summary>
        private void InitializePileSlots()
        {
            if (pileGridContainer == null)
            {
                Debug.LogError("[FishingTablePanel] PileGridContainer 未分配！");
                return;
            }

            if (cardPileSlotPrefab == null)
            {
                Debug.LogError("[FishingTablePanel] CardPileSlotPrefab 未分配！");
                return;
            }

            // 创建9个槽位
            for (int i = 0; i < 9; i++)
            {
                // 计算深度和子池索引
                FishDepth depth = GetDepthByIndex(i);
                int poolIndex = i % 3;

                // 实例化槽位
                GameObject slotObj = Instantiate(cardPileSlotPrefab, pileGridContainer);
                slotObj.name = $"PileSlot_{i} (Depth{(int)depth + 1}_Pool{poolIndex})";

                // 获取槽位组件
                CardPileSlot slot = slotObj.GetComponent<CardPileSlot>();
                if (slot == null)
                {
                    Debug.LogError($"[FishingTablePanel] 槽位预制体缺少 CardPileSlot 组件！");
                    continue;
                }

                // 初始化槽位
                slot.Initialize(depth, poolIndex);
                slot.OnSlotClicked += OnPileSlotClicked;

                // 存储到数组
                pileSlots[i] = slot;

                if (showDebugInfo)
                {
                    Debug.Log($"[FishingTablePanel] 槽位 {i} 初始化完成：深度={depth}, 子池={poolIndex}");
                }
            }

            // 初始化遮罩面板事件
            if (revealOverlay != null)
            {
                revealOverlay.OnCaptureClicked += OnCaptureClicked;
                revealOverlay.OnAbandonClicked += OnAbandonClicked;
                revealOverlay.OnCancelClicked += OnCancelClicked;
            }

            Debug.Log("[FishingTablePanel] 所有牌堆槽位初始化完成");
        }

        /// <summary>
        /// 根据槽位索引获取对应的深度
        /// </summary>
        private FishDepth GetDepthByIndex(int index)
        {
            if (index < 3) return FishDepth.Depth1;
            if (index < 6) return FishDepth.Depth2;
            return FishDepth.Depth3;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 牌堆槽位点击回调
        /// </summary>
        private void OnPileSlotClicked(CardPileSlot slot)
        {
            // 空牌堆不可点击
            if (slot.currentState == PileState.Empty)
            {
                Debug.Log("[FishingTablePanel] 点击了空牌堆，忽略");
                return;
            }

            // 检查玩家状态组件
            if (playerState == null)
            {
                Debug.LogError("[FishingTablePanel] PlayerState 未分配！");
                return;
            }

            // 记录当前操作的槽位
            currentSlot = slot;

            if (slot.currentState == PileState.FaceDown)
            {
                // 未翻开：需要消耗体力
                if (playerState.CurrentHealth < 1)
                {
                    Debug.Log("[FishingTablePanel] 体力不足，无法翻牌");
                    // TODO: 可以显示提示UI
                    return;
                }

                // 消耗1点体力
                playerState.ModifyHealth(-1);

                // 翻转到正面
                if (slot.currentCard != null)
                {
                    slot.currentCard.FlipToFront(0.5f);
                }

                // 触发揭示效果
                if (slot.currentCard != null && slot.currentCard.cardData != null)
                {
                    slot.currentCard.cardData.TriggerRevealEffects();
                }

                // 显示遮罩（首次揭示，显示放弃按钮）
                if (revealOverlay != null)
                {
                    revealOverlay.Show(slot, isFirstReveal: true);
                }

                Debug.Log($"[FishingTablePanel] 翻开牌堆：{slot.currentCard.cardData.itemName}");
            }
            else // FaceUp
            {
                // 已翻开：不消耗体力，不触发效果
                // 显示遮罩（非首次，显示取消按钮）
                if (revealOverlay != null)
                {
                    revealOverlay.Show(slot, isFirstReveal: false);
                }

                Debug.Log($"[FishingTablePanel] 查看已翻开的牌：{slot.currentCard.cardData.itemName}");
            }

            // 更新捕获按钮状态
            if (revealOverlay != null)
            {
                revealOverlay.UpdateCaptureButtonState(playerState.CurrentHealth);
            }
        }

        /// <summary>
        /// 捕获按钮点击回调
        /// </summary>
        private void OnCaptureClicked()
        {
            if (currentSlot == null || currentSlot.currentCard == null)
            {
                Debug.LogError("[FishingTablePanel] 当前槽位为空，无法捕获");
                return;
            }

            FishData fishData = currentSlot.currentCard.cardData;

            // 检查体力是否足够
            if (playerState.CurrentHealth < fishData.staminaCost)
            {
                Debug.Log($"[FishingTablePanel] 体力不足，无法捕获（需要{fishData.staminaCost}，当前{playerState.CurrentHealth}）");
                // TODO: 可以显示提示UI
                return;
            }

            // 消耗体力
            playerState.ModifyHealth(-fishData.staminaCost);

            // 触发捕获效果
            fishData.TriggerCaptureEffects();

            // 加入手牌
            if (HandSystem.HandManager.Instance != null)
            {
                HandSystem.HandManager.Instance.AddCard(fishData);
            }

            // 从牌库移除
            ItemPool.Instance.RemoveCard(fishData);

            Debug.Log($"[FishingTablePanel] 捕获成功：{fishData.itemName}");

            // 刷新槽位（显示下一张牌或空）
            currentSlot.RefreshDisplay();

            // 关闭遮罩
            if (revealOverlay != null)
            {
                revealOverlay.Hide();
            }

            currentSlot = null;
        }

        /// <summary>
        /// 放弃按钮点击回调
        /// </summary>
        private void OnAbandonClicked()
        {
            if (currentSlot == null)
            {
                Debug.LogError("[FishingTablePanel] 当前槽位为空，无法放弃");
                return;
            }

            // 标记牌堆为已翻开（正面朝上）
            currentSlot.SetRevealed();

            // 从杂鱼牌库抽一张加入手牌
            ItemData trashCard = ItemPool.Instance.DrawItem(ItemCategory.Trash);
            if (trashCard != null)
            {
                if (HandSystem.HandManager.Instance != null)
                {
                    HandSystem.HandManager.Instance.AddCard(trashCard);
                }
                Debug.Log($"[FishingTablePanel] 放弃捕获，获得杂鱼：{trashCard.itemName}");
            }
            else
            {
                Debug.Log("[FishingTablePanel] 放弃捕获，但杂鱼牌库为空");
            }

            // 关闭遮罩
            if (revealOverlay != null)
            {
                revealOverlay.Hide();
            }

            currentSlot = null;
        }

        /// <summary>
        /// 取消按钮点击回调
        /// </summary>
        private void OnCancelClicked()
        {
            Debug.Log("[FishingTablePanel] 取消操作");

            // 仅关闭遮罩，状态不变
            if (revealOverlay != null)
            {
                revealOverlay.Hide();
            }

            currentSlot = null;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 刷新单个槽位显示
        /// </summary>
        public void RefreshPileSlot(CardPileSlot slot)
        {
            if (slot != null)
            {
                slot.RefreshDisplay();
            }
        }

        /// <summary>
        /// 刷新所有槽位显示
        /// </summary>
        public void RefreshAllPileSlots()
        {
            foreach (var slot in pileSlots)
            {
                if (slot != null)
                {
                    slot.RefreshDisplay();
                }
            }

            Debug.Log("[FishingTablePanel] 所有牌堆槽位已刷新");
        }

        #endregion
    }
}
