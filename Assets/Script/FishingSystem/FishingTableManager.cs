using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ItemSystem;
using HandSystem;
using ShopSystem;
using FishCardSystem;

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

        [Header("选择面板")]
        [Tooltip("CardSelectionPanel 预制体，供杂鱼三选一被动使用")]
        [SerializeField] private GameObject cardSelectionPanelPrefab;

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

            if (!ItemSystem.EffectBus.Instance.ShouldIgnoreRevealEffects)
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

            // 通过 EffectBus 计算最终体力消耗（被动效果可根据鱼属性条件减少消耗，下限为 0）
            int finalCost = ItemSystem.EffectBus.Instance.ProcessFishingCost(fishData.staminaCost, fishData);

            if (playerState.CurrentHealth < finalCost)
            {
                if (showDebugInfo)
                    Debug.Log($"[FishingTableManager] 体力不足，无法捕获（需要 {finalCost}，当前 {playerState.CurrentHealth}）");
                return false;
            }

            playerState.ModifyHealth(-finalCost);

            // 先从牌堆移除被捕获的牌，使捕获效果看到的牌堆状态是"捕获后"的
            // （如 Effect_RemoveAllPileTops 需要操作的是被捕获牌下方的卡牌）
            pile.RemoveTopCard();

            if (!ItemSystem.EffectBus.Instance.ShouldIgnoreCaptureEffects)
                fishData.TriggerCaptureEffects();

            bool destroyOnCapture = ItemSystem.EffectBus.Instance.ConsumePendingDestroyOnCapture();
            if (!destroyOnCapture && HandManager.Instance != null)
                HandManager.Instance.AddCard(fishData);

            ItemSystem.EffectBus.Instance.NotifyFishCaptured();

            if (fishData.effects != null)
            {
                foreach (var e in fishData.effects)
                {
                    if (e is ItemSystem.Effect_FreeCaptureByHand)
                    {
                        ItemSystem.EffectBus.Instance.UnregisterFreeCaptureSource();
                    }
                    else if (e is ItemSystem.Effect_FreeCaptureOnSanity)
                    {
                        ItemSystem.EffectBus.Instance.UnregisterSanityFreeCaptureSource();
                    }
                    else if (e is ItemSystem.Effect_CostPerHandFish)
                    {
                        ItemSystem.EffectBus.Instance.UnregisterHandFishCostSource();
                    }
                }
            }

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
            int finalCost = ItemSystem.EffectBus.Instance.ProcessFishingCost(data.staminaCost, data);
            return playerState.CurrentHealth >= finalCost;
        }

        /// <summary>
        /// 放弃捕获：从杂鱼牌库抽取卡牌加入手牌，不修改牌堆状态。
        /// 若装备被动启用了杂鱼选择，则抽取3张展示选择面板（3选1）。
        /// 若杂鱼牌库为空，则静默跳过（面板仍正常关闭）。
        /// </summary>
        /// <param name="pile">当前操作的牌堆（仅用于日志记录，逻辑不依赖）</param>
        public void TryAbandon(CardPile pile)
        {
            if (ShopManager.Instance == null) return;

            if (ItemSystem.EffectBus.Instance.TrashSelectionEnabled)
            {
                var drawnItems = ShopManager.Instance.DrawTopItems(ItemCategory.Trash, 3);

                if (drawnItems.Count >= 2)
                {
                    OpenTrashSelection(drawnItems);
                    return;
                }

                // 不足2张无法有效选择，归还已抽卡走正常逻辑
                if (drawnItems.Count > 0)
                    ShopManager.Instance.ReturnToTop(ItemCategory.Trash, drawnItems);
            }

            TrashData trash = ShopManager.Instance.DrawTrash();

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

        /// <summary>
        /// 打开杂鱼卡选择面板（3选1），由装备被动 Effect_TrashCardSelection 驱动。
        /// 选中卡加入手牌，未选中卡归还杂鱼牌库并洗牌。
        /// </summary>
        private void OpenTrashSelection(List<ItemData> cards)
        {
            if (cardSelectionPanelPrefab == null)
            {
                Debug.LogError("[FishingTableManager] cardSelectionPanelPrefab 未配置，无法打开杂鱼选择面板");
                // 回退：取第一张入手牌，其余归还
                if (cards.Count > 0)
                {
                    HandManager.Instance?.AddCard(cards[0]);
                    if (cards.Count > 1)
                    {
                        ShopManager.Instance?.ReturnToTop(ItemCategory.Trash, cards.GetRange(1, cards.Count - 1));
                        ShopManager.Instance?.ShuffleTrashPool();
                    }
                }
                return;
            }

            // 查找 UI Canvas 作为面板父节点
            Canvas rootCanvas = null;
            if (pileConfigs.Length > 0 && pileConfigs[0].pile != null)
                rootCanvas = pileConfigs[0].pile.GetComponentInParent<Canvas>();
            if (rootCanvas == null)
                rootCanvas = FindObjectOfType<Canvas>();

            Transform panelParent = rootCanvas != null ? rootCanvas.transform : transform;

            GameObject panelObj = Instantiate(cardSelectionPanelPrefab, panelParent);
            CardSelectionPanel panel = panelObj.GetComponentInChildren<CardSelectionPanel>(true);

            if (panel == null)
            {
                Debug.LogError("[FishingTableManager] cardSelectionPanelPrefab 中未找到 CardSelectionPanel 组件");
                Destroy(panelObj);
                if (cards.Count > 0)
                {
                    HandManager.Instance?.AddCard(cards[0]);
                    if (cards.Count > 1)
                    {
                        ShopManager.Instance?.ReturnToTop(ItemCategory.Trash, cards.GetRange(1, cards.Count - 1));
                        ShopManager.Instance?.ShuffleTrashPool();
                    }
                }
                return;
            }

            if (showDebugInfo)
                Debug.Log($"[FishingTableManager] 杂鱼选择面板已打开，展示 {cards.Count} 张");

            panel.Open(cards, 1, (selected, rejected) =>
            {
                foreach (var card in selected)
                {
                    HandManager.Instance?.AddCard(card);
                    if (showDebugInfo)
                        Debug.Log($"[FishingTableManager] 杂鱼选择：选中 {card.itemName}");
                }

                if (rejected.Count > 0)
                {
                    ShopManager.Instance?.ReturnToTop(ItemCategory.Trash, rejected);
                    ShopManager.Instance?.ShuffleTrashPool();

                    if (showDebugInfo)
                        Debug.Log($"[FishingTableManager] 杂鱼选择：{rejected.Count} 张归还牌库并洗牌");
                }

                Destroy(panelObj);
            });
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

        /// <summary>
        /// 根据 CardPile 实例反查其配置（深度和序号），未找到返回 null
        /// </summary>
        public (FishDepth depth, int poolIndex)? GetPileConfig(CardPile pile)
        {
            foreach (var cfg in pileConfigs)
            {
                if (cfg.pile == pile)
                    return (cfg.depth, cfg.poolIndex);
            }
            return null;
        }

        #endregion

        #region 按深度/序号查询与复合抽取

        /// <summary>
        /// 按深度+序号精确查找单个 CardPile
        /// </summary>
        public CardPile GetPile(FishDepth depth, int poolIndex)
        {
            foreach (var cfg in pileConfigs)
            {
                if (cfg.pile != null && cfg.depth == depth && cfg.poolIndex == poolIndex)
                    return cfg.pile;
            }
            return null;
        }

        /// <summary>
        /// 获取某深度下全部牌堆（最多3个）
        /// </summary>
        public List<CardPile> GetPilesByDepth(FishDepth depth)
        {
            var result = new List<CardPile>();
            foreach (var cfg in pileConfigs)
            {
                if (cfg.pile != null && cfg.depth == depth)
                    result.Add(cfg.pile);
            }
            return result;
        }

        /// <summary>
        /// 获取某序号跨所有深度的牌堆（最多3个）
        /// </summary>
        public List<CardPile> GetPilesByPoolIndex(int poolIndex)
        {
            var result = new List<CardPile>();
            foreach (var cfg in pileConfigs)
            {
                if (cfg.pile != null && cfg.poolIndex == poolIndex)
                    result.Add(cfg.pile);
            }
            return result;
        }

        /// <summary>
        /// 按深度横抽：从指定深度的各牌堆各抽取1张顶牌。
        /// 返回带源追踪的列表，供调用方在回调中归还被拒绝的卡牌。
        /// </summary>
        public List<(FishData card, CardPile source)> DrawOneFromEachAtDepth(FishDepth depth)
        {
            var result = new List<(FishData, CardPile)>();
            foreach (var cfg in pileConfigs)
            {
                if (cfg.pile != null && cfg.depth == depth && cfg.pile.CardCount > 0)
                {
                    FishData top = cfg.pile.RemoveTopCard();
                    if (top != null) result.Add((top, cfg.pile));
                }
            }

            if (showDebugInfo)
                Debug.Log($"[FishingTableManager] 按深度横抽 {depth}：抽取 {result.Count} 张");
            return result;
        }

        /// <summary>
        /// 按序号纵抽：从3个深度的同序号牌堆各抽取1张顶牌。
        /// 返回带源追踪的列表，供调用方在回调中归还被拒绝的卡牌。
        /// </summary>
        public List<(FishData card, CardPile source)> DrawOneFromEachAtPoolIndex(int poolIndex)
        {
            var result = new List<(FishData, CardPile)>();
            foreach (var cfg in pileConfigs)
            {
                if (cfg.pile != null && cfg.poolIndex == poolIndex && cfg.pile.CardCount > 0)
                {
                    FishData top = cfg.pile.RemoveTopCard();
                    if (top != null) result.Add((top, cfg.pile));
                }
            }

            if (showDebugInfo)
                Debug.Log($"[FishingTableManager] 按序号纵抽 poolIndex={poolIndex}：抽取 {result.Count} 张");
            return result;
        }

        #endregion

        #region 连锁揭示本列

        /// <summary>
        /// 启动同列连锁揭示协程。
        /// 查找 sourcePile 所在列（同 poolIndex）的其他 FaceDown 牌堆，依次翻面并触发过滤后的 OnReveal 效果。
        /// </summary>
        public void StartColumnReveal(CardPile sourcePile)
        {
            var config = GetPileConfig(sourcePile);
            if (config == null) return;

            var columnPiles = GetPilesByPoolIndex(config.Value.poolIndex);
            var targets = new List<CardPile>();
            foreach (var p in columnPiles)
            {
                if (p != sourcePile && p.State == PileState.FaceDown && p.CardCount > 0)
                    targets.Add(p);
            }

            if (targets.Count == 0) return;
            StartCoroutine(ColumnRevealCoroutine(targets));
        }

        private IEnumerator ColumnRevealCoroutine(List<CardPile> targets)
        {
            const float delay = 0.4f;

            foreach (var pile in targets)
            {
                if (pile == null || pile.State != PileState.FaceDown || pile.CardCount == 0)
                    continue;

                pile.Reveal();

                FishData topCard = pile.GetTopCard();
                if (topCard != null && !EffectBus.Instance.ShouldIgnoreRevealEffects)
                    TriggerChainRevealEffects(topCard);

                yield return new WaitForSeconds(delay);
            }

            if (showDebugInfo)
                Debug.Log("[FishingTableManager] 连锁揭示本列完成");
        }

        /// <summary>
        /// 触发连锁揭示的 OnReveal 效果（过滤掉需要面板消费的标记型/折扣型效果，防止递归）。
        /// </summary>
        private void TriggerChainRevealEffects(FishData fishData)
        {
            if (fishData.effects == null) return;

            var context = new EffectContext
            {
                Target = GameObject.Find("player")
            };

            foreach (var effect in fishData.effects)
            {
                if (effect == null || effect.trigger != EffectTrigger.OnReveal) continue;

                if (effect is Effect_RemoveOnReveal) continue;
                if (effect is Effect_ForceCapture) continue;
                if (effect is Effect_ShuffleBackOnAbandon) continue;
                if (effect is Effect_RevealCostReduction) continue;
                if (effect is Effect_RevealColumn) continue;

                effect.Execute(context);

                if (showDebugInfo)
                    Debug.Log($"[FishingTableManager] 连锁揭示效果：{fishData.itemName} → {effect.DisplayName}");
            }
        }

        #endregion
    }
}
