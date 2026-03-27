using System;
using System.Collections.Generic;
using UnityEngine;
using ItemSystem;
using HandSystem;

namespace FishingSystem
{
    /// <summary>
    /// 钓鱼牌桌管理器
    /// 负责从 ItemPool 初始化各 CardPile 的卡序，并作为翻牌/捕获操作的游戏逻辑入口。
    /// 多牌堆操作（全部翻牌、洗牌等）也统一在此实现。
    /// </summary>
    public class FishingTableManager : MonoBehaviour
    {
        #region 单例

        public static FishingTableManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        #endregion

        #region 数据结构

        /// <summary>
        /// 单个牌堆的初始化配置
        /// </summary>
        [Serializable]
        public struct PileConfig
        {
            [Tooltip("对应场景中的 CardPile 实例")]
            public CardPile pile;

            [Tooltip("该牌堆使用的鱼类深度")]
            public FishDepth depth;

            [Tooltip("ItemPool 子池索引（0–2）")]
            [Range(0, 2)]
            public int poolIndex;
        }

        #endregion

        #region Inspector

        [Header("牌堆配置（按顺序对应 3×3 网格，索引 0-8）")]
        [SerializeField] private PileConfig[] pileConfigs = new PileConfig[9];

        [Header("游戏状态")]
        [SerializeField] private CharacterState playerState;

        [Header("调试")]
        [SerializeField] private bool showDebugInfo = true;

        #endregion

        #region 初始化

        private void Start()
        {
            InitializeAllPiles();
        }

        /// <summary>
        /// 从 ItemPool 获取各子池卡序，注入对应 CardPile（深拷贝，与 ItemPool 解耦）
        /// </summary>
        private void InitializeAllPiles()
        {
            if (ItemPool.Instance == null)
            {
                Debug.LogError("[FishingTableManager] ItemPool 不存在，无法初始化牌堆");
                return;
            }

            for (int i = 0; i < pileConfigs.Length; i++)
            {
                PileConfig cfg = pileConfigs[i];
                if (cfg.pile == null)
                {
                    Debug.LogWarning($"[FishingTableManager] pileConfigs[{i}].pile 未指定，跳过");
                    continue;
                }

                List<FishData> source = ItemPool.Instance.GetFragmentedPool(cfg.depth, cfg.poolIndex);
                // 深拷贝：CardPile 独立持有卡序，与 ItemPool 运行时状态完全分离
                cfg.pile.SetCards(new List<FishData>(source));
                cfg.pile.SetDepth(cfg.depth);

                if (showDebugInfo)
                    Debug.Log($"[FishingTableManager] 初始化牌堆[{i}] 深度={cfg.depth} 子池={cfg.poolIndex} 张数={source.Count}");
            }
        }

        #endregion

        #region 游戏逻辑操作（供 CardPilePanel 调用）

        /// <summary>
        /// 尝试翻开顶牌。
        /// 消耗 1 点体力；不足时返回 false，调用方可显示提示。
        /// 注意：此方法只处理体力扣除，不调用 pile.Reveal()，
        /// CardPilePanel 在收到 true 后自行调用 pile.Reveal() 并切换视图。
        /// </summary>
        /// <param name="pile">目标牌堆</param>
        /// <param name="fishData">顶牌数据（用于触发揭示效果）</param>
        /// <returns>是否成功</returns>
        public bool TryReveal(CardPile pile, FishData fishData)
        {
            if (pile == null || fishData == null) return false;

            if (playerState == null)
            {
                Debug.LogError("[FishingTableManager] playerState 未指定");
                return false;
            }

            if (playerState.CurrentHealth < 1)
            {
                if (showDebugInfo)
                    Debug.Log("[FishingTableManager] 体力不足，无法翻牌");
                return false;
            }

            playerState.ModifyHealth(-1);
            fishData.TriggerRevealEffects();

            if (showDebugInfo)
                Debug.Log($"[FishingTableManager] 翻牌成功：{fishData.itemName}，剩余体力={playerState.CurrentHealth}");

            return true;
        }

        /// <summary>
        /// 尝试捕获顶牌。
        /// 消耗 fishData.staminaCost 点体力，将卡牌加入手牌，并从牌堆移除顶牌。
        /// 不足时返回 false，调用方可显示提示。
        /// </summary>
        /// <param name="pile">目标牌堆</param>
        /// <param name="fishData">顶牌数据</param>
        /// <returns>是否成功</returns>
        public bool TryCapture(CardPile pile, FishData fishData)
        {
            if (pile == null || fishData == null) return false;

            if (playerState == null)
            {
                Debug.LogError("[FishingTableManager] playerState 未指定");
                return false;
            }

            // 通过 EffectBus 计算最终体力消耗（被动效果可减少消耗，下限为 0）
            int finalCost = ItemSystem.EffectBus.Instance.ProcessFishingCost(fishData.staminaCost);

            if (playerState.CurrentHealth < finalCost)
            {
                if (showDebugInfo)
                    Debug.Log($"[FishingTableManager] 体力不足，无法捕获（需要 {finalCost}，当前 {playerState.CurrentHealth}）");
                return false;
            }

            playerState.ModifyHealth(-finalCost);
            fishData.TriggerCaptureEffects();

            if (HandManager.Instance != null)
                HandManager.Instance.AddCard(fishData);

            pile.RemoveTopCard();

            if (showDebugInfo)
                Debug.Log($"[FishingTableManager] 捕获成功：{fishData.itemName}，剩余体力={playerState.CurrentHealth}");

            return true;
        }

        /// <summary>
        /// 查询玩家当前体力是否足以捕获指定卡牌，供 CardPilePanel 用于按钮置灰判断。
        /// 使用 EffectBus 计算最终消耗，与 TryCapture 保持一致。
        /// </summary>
        public bool CanAffordCapture(FishData data)
        {
            if (playerState == null || data == null) return false;
            int finalCost = ItemSystem.EffectBus.Instance.ProcessFishingCost(data.staminaCost);
            return playerState.CurrentHealth >= finalCost;
        }

        /// <summary>
        /// 放弃捕获：从杂鱼牌库抽取一张加入手牌，不修改牌堆状态。
        /// 若杂鱼牌库为空，则静默跳过（面板仍正常关闭）。
        /// </summary>
        /// <param name="pile">当前操作的牌堆（仅用于日志记录，逻辑不依赖）</param>
        public void TryAbandon(CardPile pile)
        {
            ItemSystem.TrashData trash =
                ItemPool.Instance?.DrawItem(ItemSystem.ItemCategory.Trash) as ItemSystem.TrashData;

            if (trash != null)
            {
                HandManager.Instance?.AddCard(trash);
                if (showDebugInfo)
                    Debug.Log($"[FishingTableManager] 放弃捕获，获得杂鱼卡：{trash.itemName}");
            }
            else
            {
                if (showDebugInfo)
                    Debug.Log("[FishingTableManager] 放弃捕获，杂鱼牌库为空，未获得卡牌");
            }
        }

        #endregion

        #region 多牌堆操作

        /// <summary>
        /// 翻开所有处于 FaceDown 状态的牌堆（不消耗体力，适合事件/效果触发）
        /// </summary>
        public void RevealAllPiles()
        {
            foreach (var cfg in pileConfigs)
            {
                if (cfg.pile != null && cfg.pile.State == PileState.FaceDown)
                    cfg.pile.Reveal();
            }

            if (showDebugInfo)
                Debug.Log("[FishingTableManager] RevealAllPiles 执行完毕");
        }

        /// <summary>
        /// 重新初始化所有牌堆（重置卡序），从 ItemPool 重新读取
        /// </summary>
        [ContextMenu("调试：重置所有牌堆")]
        public void ResetAllPiles()
        {
            InitializeAllPiles();
        }

        /// <summary>
        /// 获取指定索引的 CardPile（供外部效果脚本访问）
        /// </summary>
        public CardPile GetPile(int index)
        {
            if (index < 0 || index >= pileConfigs.Length) return null;
            return pileConfigs[index].pile;
        }

        /// <summary>
        /// 获取所有非空 CardPile 的列表
        /// </summary>
        public List<CardPile> GetAllPiles()
        {
            var result = new List<CardPile>();
            foreach (var cfg in pileConfigs)
            {
                if (cfg.pile != null)
                    result.Add(cfg.pile);
            }
            return result;
        }

        /// <summary>
        /// 检查玩家当前深度是否允许与指定牌堆交互。
        /// playerState 未设置时默认允许，保证向后兼容。
        /// </summary>
        /// <param name="pile">目标牌堆</param>
        /// <returns>玩家深度 >= 牌堆深度时返回 true</returns>
        public bool CanPlayerAccessPile(CardPile pile)
        {
            if (playerState == null || pile == null) return true;
            return playerState.CanAccessDepth(pile.PileDepth);
        }

        #endregion
    }
}
