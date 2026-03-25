using System.Collections.Generic;
using UnityEngine;
using ItemSystem;

namespace FishingSystem
{
    /// <summary>
    /// 牌堆测试初始化脚本
    /// 根据指定深度和子池索引从 ItemPool 取卡序，注入目标 CardPile 完成初始化
    /// 用于单独测试牌堆预制体的显示和交互
    /// </summary>
    public class CardPileTest : MonoBehaviour
    {
        [Header("目标牌堆")]
        [SerializeField] private CardPile targetPile;

        [Header("初始化参数")]
        [SerializeField] private FishDepth depth = FishDepth.Depth1;
        [SerializeField][Range(0, 2)] private int poolIndex = 0;
        [Tooltip("限制取卡张数，0 表示取全部")]
        [SerializeField][Min(0)] private int cardCountLimit = 0;

        [Header("运行时操作（调试用）")]
        [Tooltip("Start 时自动初始化")]
        [SerializeField] private bool autoInitOnStart = true;

        [Header("调试信息")]
        [SerializeField] private bool showDebugInfo = true;

        private void Start()
        {
            if (autoInitOnStart)
                InitializePile();
        }

        /// <summary>
        /// 初始化牌堆（可从外部或 Inspector 按钮调用）
        /// </summary>
        public void InitializePile()
        {
            if (targetPile == null)
            {
                Debug.LogError("[CardPileTest] targetPile 未指定，请在 Inspector 中拖入 CardPile 对象");
                return;
            }

            if (ItemPool.Instance == null)
            {
                Debug.LogError("[CardPileTest] ItemPool 不存在，请确保场景中有 ItemPool 对象");
                return;
            }

            List<FishData> pool = ItemPool.Instance.GetFragmentedPool(depth, poolIndex);

            if (pool == null || pool.Count == 0)
            {
                Debug.LogWarning($"[CardPileTest] 深度 {depth} 子池 {poolIndex} 为空，请检查 Resources/Items/Fish 资源");
                targetPile.SetCards(new List<FishData>());
                return;
            }

            // 截取指定张数
            int takeCount = (cardCountLimit > 0 && cardCountLimit <= pool.Count)
                ? cardCountLimit
                : pool.Count;

            List<FishData> cards = pool.GetRange(0, takeCount);
            targetPile.SetCards(cards);

            if (showDebugInfo)
            {
                Debug.Log($"[CardPileTest] 初始化完成 → 深度:{depth}  子池:{poolIndex}  张数:{cards.Count}");
                for (int i = 0; i < cards.Count; i++)
                {
                    Debug.Log($"  [{i}] {cards[i].itemName}  尺寸:{cards[i].size}");
                }
            }
        }

        /// <summary>
        /// 翻开顶牌（测试用）
        /// </summary>
        [ContextMenu("调试：翻开顶牌 (Reveal)")]
        public void DebugReveal()
        {
            if (targetPile == null) return;
            targetPile.Reveal();
            if (showDebugInfo)
                Debug.Log($"[CardPileTest] Reveal → 状态:{targetPile.State}  顶牌:{targetPile.GetTopCard()?.itemName ?? "无"}");
        }

        /// <summary>
        /// 移除顶牌（测试用）
        /// </summary>
        [ContextMenu("调试：移除顶牌 (RemoveTop)")]
        public void DebugRemoveTop()
        {
            if (targetPile == null) return;
            FishData removed = targetPile.RemoveTopCard();
            if (showDebugInfo)
                Debug.Log($"[CardPileTest] RemoveTop → 移除:{removed?.itemName ?? "无"}  剩余:{targetPile.CardCount}张");
        }
    }
}
