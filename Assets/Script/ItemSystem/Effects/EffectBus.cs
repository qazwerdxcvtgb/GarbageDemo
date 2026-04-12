using System;
using UnityEngine;
using DaySystem;

namespace ItemSystem
{
    /// <summary>
    /// 全局被动效果事件总线（单例）
    /// 为游戏行为提供可订阅的修改链，被动效果通过 Register/Unregister 接入。
    /// 使用方只需调用对应的 Process 方法，无需关心哪些效果已激活。
    /// </summary>
    public class EffectBus : MonoBehaviour
    {
        private static EffectBus instance;
        public static EffectBus Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<EffectBus>();
                    if (instance == null)
                    {
                        var go = new GameObject("EffectBus");
                        instance = go.AddComponent<EffectBus>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (DayManager.Instance != null)
                DayManager.Instance.OnDayChanged += _ => ResetDailyFlags();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        #region 钓鱼体力消耗修改链

        /// <summary>
        /// 钓鱼捕获体力消耗修改链。
        /// 每个订阅者接收 (当前消耗值, 目标鱼数据)，返回修改后的值。
        /// 注册示例：EffectBus.Instance.OnModifyFishingCost += (cost, fish) => cost - reduction;
        /// </summary>
        public event Func<int, FishData, int> OnModifyFishingCost;

        /// <summary>
        /// 当钓鱼体力修改器发生变更（注册/注销）时触发，通知 UI 刷新显示
        /// </summary>
        public event Action OnFishingModifierChanged;

        /// <summary>
        /// 通知所有订阅者钓鱼体力修改器已变更
        /// </summary>
        public void NotifyFishingModifierChanged() => OnFishingModifierChanged?.Invoke();

        /// <summary>
        /// 计算最终钓鱼体力消耗。
        /// FishingTableManager.TryCapture 调用此方法替换原始 staminaCost。
        /// 最终结果下限为 0，不允许负值。
        /// </summary>
        /// <param name="baseCost">鱼卡原始 staminaCost</param>
        /// <param name="fishData">目标鱼数据，供条件效果判断鱼类属性</param>
        /// <returns>经过所有被动效果修改后的最终消耗值（≥ 0）</returns>
        public int ProcessFishingCost(int baseCost, FishData fishData)
        {
            if (fishData != null && CheckFreeCaptureByHand(fishData))
                return 0;
            if (fishData != null && CheckFreeCaptureOnSanity(fishData))
                return 0;

            int result = baseCost;

            if (fishData != null)
                result += CheckHandFishCostIncrease(fishData);

            if (OnModifyFishingCost != null)
            {
                foreach (Delegate d in OnModifyFishingCost.GetInvocationList())
                {
                    if (d is Func<int, FishData, int> modifier)
                        result = modifier(result, fishData);
                }
            }
            if (revealCostReduction > 0)
                result -= revealCostReduction;
            if (nextFishCostReduction > 0)
                result -= nextFishCostReduction;
            return Mathf.Max(0, result);
        }

        /// <summary>
        /// 检查 fishData 是否携带 Effect_FreeCaptureByHand 且手牌条件满足。
        /// 每次 ProcessFishingCost 调用时实时评估，无缓存。
        /// </summary>
        private bool CheckFreeCaptureByHand(FishData fishData)
        {
            if (fishData.effects == null) return false;
            foreach (var effect in fishData.effects)
            {
                if (effect is Effect_FreeCaptureByHand fc && fc.CheckHandCondition())
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 检查 fishData 是否携带 Effect_FreeCaptureOnSanity 且当前疯狂等级匹配。
        /// 每次 ProcessFishingCost 调用时实时评估，无缓存。
        /// </summary>
        private bool CheckFreeCaptureOnSanity(FishData fishData)
        {
            if (fishData.effects == null) return false;
            foreach (var effect in fishData.effects)
            {
                if (effect is Effect_FreeCaptureOnSanity fs && fs.CheckSanityCondition())
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 检查 fishData 是否携带 Effect_CostPerHandFish，返回手牌鱼卡带来的额外消耗总计。
        /// 每次 ProcessFishingCost 调用时实时评估，无缓存。
        /// </summary>
        private int CheckHandFishCostIncrease(FishData fishData)
        {
            if (fishData.effects == null) return 0;
            int total = 0;
            bool found = false;
            foreach (var effect in fishData.effects)
            {
                if (effect is Effect_CostPerHandFish cpf)
                {
                    total += cpf.GetHandFishCount();
                    found = true;
                }
            }
            if (found && handFishCostSourceCount == 0)
                RegisterHandFishCostSource();
            return total;
        }

        #endregion

        #region 钓鱼捕获事件

        /// <summary>
        /// 鱼类捕获完成时触发，供有状态的被动效果（如"每天第一条鱼"）追踪捕获次数
        /// </summary>
        public event Action OnFishCaptured;

        /// <summary>
        /// 通知所有订阅者一次捕获已完成
        /// </summary>
        public void NotifyFishCaptured()
        {
            OnFishCaptured?.Invoke();
            ConsumeNextFishDiscount();
        }

        #endregion

        #region 捕获效果旁路

        private int ignoreCaptureEffectsCount = 0;

        /// <summary>
        /// 是否应跳过鱼卡的捕获效果（OnCapture 触发的所有 EffectBase）
        /// </summary>
        public bool ShouldIgnoreCaptureEffects => ignoreCaptureEffectsCount > 0;

        /// <summary>
        /// 注册一个"忽略捕获效果"来源（装备时调用）。
        /// 使用引用计数，支持多件装备同时持有此效果。
        /// </summary>
        public void RegisterIgnoreCaptureEffects()
        {
            ignoreCaptureEffectsCount++;
            NotifyFishingModifierChanged();
        }

        /// <summary>
        /// 注销一个"忽略捕获效果"来源（卸下时调用）。
        /// 计数器下限为 0，防止异常场景下变为负数。
        /// </summary>
        public void UnregisterIgnoreCaptureEffects()
        {
            ignoreCaptureEffectsCount = Mathf.Max(0, ignoreCaptureEffectsCount - 1);
            NotifyFishingModifierChanged();
        }

        #endregion

        #region 揭示效果旁路

        private int ignoreRevealEffectsCount = 0;

        /// <summary>
        /// 是否应跳过鱼卡的揭示效果（OnReveal 触发的所有 EffectBase）
        /// </summary>
        public bool ShouldIgnoreRevealEffects => ignoreRevealEffectsCount > 0;

        /// <summary>
        /// 注册一个"忽略揭示效果"来源（装备时调用）。
        /// 使用引用计数，支持多件装备同时持有此效果。
        /// </summary>
        public void RegisterIgnoreRevealEffects()
        {
            ignoreRevealEffectsCount++;
            NotifyFishingModifierChanged();
        }

        /// <summary>
        /// 注销一个"忽略揭示效果"来源（卸下时调用）。
        /// 计数器下限为 0，防止异常场景下变为负数。
        /// </summary>
        public void UnregisterIgnoreRevealEffects()
        {
            ignoreRevealEffectsCount = Mathf.Max(0, ignoreRevealEffectsCount - 1);
            NotifyFishingModifierChanged();
        }

        #endregion

        #region 杂鱼卡选择（装备被动）

        private int trashSelectionCount = 0;

        /// <summary>
        /// 是否启用杂鱼卡三选一（放弃捕获时从杂鱼牌库抽多张供玩家选择）。
        /// 通过引用计数实现，支持多件装备同时持有此效果。
        /// </summary>
        public bool TrashSelectionEnabled => trashSelectionCount > 0;

        /// <summary>
        /// 注册一个"杂鱼卡选择"来源（装备时调用）
        /// </summary>
        public void RegisterTrashSelection()
        {
            trashSelectionCount++;
            Debug.Log($"[EffectBus] 杂鱼选择已注册，当前计数={trashSelectionCount}");
        }

        /// <summary>
        /// 注销一个"杂鱼卡选择"来源（卸下时调用）
        /// </summary>
        public void UnregisterTrashSelection()
        {
            trashSelectionCount = Mathf.Max(0, trashSelectionCount - 1);
            Debug.Log($"[EffectBus] 杂鱼选择已注销，当前计数={trashSelectionCount}");
        }

        #endregion

        #region 每日刷新完成事件

        /// <summary>
        /// 每日刷新阶段完成后触发（RestoreFullHealth 之后），
        /// 装备每日体力效果在此时机给予 bonusHealth。
        /// </summary>
        public event Action OnDayRefreshCompleted;

        /// <summary>
        /// 通知所有订阅者每日刷新已完成
        /// </summary>
        public void NotifyDayRefreshCompleted() => OnDayRefreshCompleted?.Invoke();

        #endregion

        #region 悬挂替换许可

        private bool allowHangReplace;

        /// <summary>
        /// 当前是否允许取下/替换悬挂的鱼（由 Effect_AllowHangReplace 激活，每日自动重置）
        /// </summary>
        public bool AllowHangReplace => allowHangReplace;

        /// <summary>
        /// 悬挂替换许可状态变化事件（参数 true = 允许替换，false = 锁定）。
        /// ShopHangController 订阅此事件以同步更新已悬挂卡牌的 isLocked 状态。
        /// </summary>
        public event Action<bool> OnHangReplaceChanged;

        /// <summary>
        /// 激活悬挂替换许可（消耗品效果调用，当日有效）
        /// </summary>
        public void EnableHangReplace()
        {
            allowHangReplace = true;
            OnHangReplaceChanged?.Invoke(true);
            Debug.Log("[EffectBus] 悬挂替换许可已激活（当日有效）");
        }

        /// <summary>
        /// 重置悬挂替换许可（每日开始时自动调用）
        /// </summary>
        public void ResetHangReplace()
        {
            if (allowHangReplace)
            {
                allowHangReplace = false;
                OnHangReplaceChanged?.Invoke(false);
                Debug.Log("[EffectBus] 悬挂替换许可已重置");
            }
        }

        #endregion

        #region 揭示体力折扣（鱼卡 OnReveal 效果）

        private int revealCostReduction = 0;

        /// <summary>
        /// 设置揭示体力折扣（由 Effect_RevealCostReduction 在 OnReveal 时调用）。
        /// CardPilePanel 关闭时自动清除。
        /// </summary>
        public void SetRevealCostReduction(int amount)
        {
            revealCostReduction = amount;
            Debug.Log($"[EffectBus] 揭示体力折扣已设置：-{amount}");
            NotifyFishingModifierChanged();
        }

        /// <summary>
        /// 清除揭示体力折扣（CardPilePanel.ClosePanel 调用）。
        /// </summary>
        public void ClearRevealCostReduction()
        {
            if (revealCostReduction > 0)
            {
                Debug.Log($"[EffectBus] 揭示体力折扣已清除（原值：-{revealCostReduction}）");
                revealCostReduction = 0;
                NotifyFishingModifierChanged();
            }
        }

        #endregion

        #region 一次性钓鱼折扣（消耗品效果）

        private int nextFishCostReduction = 0;

        /// <summary>
        /// 当前一次性钓鱼体力折扣累计值（只读）
        /// </summary>
        public int NextFishCostReduction => nextFishCostReduction;

        /// <summary>
        /// 增加一次性钓鱼体力折扣（消耗品使用时调用）。
        /// 可叠加累计，捕获下一条鱼后自动清零，每日重置时也会清零。
        /// </summary>
        public void AddNextFishDiscount(int amount)
        {
            nextFishCostReduction += amount;
            Debug.Log($"[EffectBus] 一次性钓鱼折扣 +{amount}，当前累计={nextFishCostReduction}");
            NotifyFishingModifierChanged();
        }

        /// <summary>
        /// 消耗一次性折扣（捕获完成后自动调用）
        /// </summary>
        private void ConsumeNextFishDiscount()
        {
            if (nextFishCostReduction > 0)
            {
                Debug.Log($"[EffectBus] 一次性钓鱼折扣已消耗：{nextFishCostReduction}");
                nextFishCostReduction = 0;
                NotifyFishingModifierChanged();
            }
        }

        /// <summary>
        /// 重置一次性折扣（每日重置时调用）
        /// </summary>
        private void ResetNextFishDiscount()
        {
            if (nextFishCostReduction > 0)
            {
                nextFishCostReduction = 0;
                Debug.Log("[EffectBus] 一次性钓鱼折扣已重置（每日）");
            }
        }

        #endregion

        #region 揭示后自动移除

        private bool pendingRemoveOnReveal = false;

        /// <summary>
        /// 标记当前揭示的鱼卡需要自动移除（由 Effect_RemoveOnReveal 调用）。
        /// CardPilePanel 在 TryReveal 返回后通过 Consume 读取并清除此标记。
        /// </summary>
        public void SetPendingRemoveOnReveal()
        {
            pendingRemoveOnReveal = true;
            Debug.Log("[EffectBus] 揭示后自动移除标记已设置");
        }

        /// <summary>
        /// 消费揭示后自动移除标记。返回 true 表示需要自动移除，标记同时被清除。
        /// </summary>
        public bool ConsumePendingRemoveOnReveal()
        {
            if (!pendingRemoveOnReveal) return false;
            pendingRemoveOnReveal = false;
            return true;
        }

        #endregion

        #region 捕获后销毁（不入手牌）

        private bool pendingDestroyOnCapture = false;

        /// <summary>
        /// 标记当前捕获的鱼卡不加入手牌，直接销毁（由 Effect_DestroyOnCapture 调用）。
        /// FishingTableManager.TryCapture 在 TriggerCaptureEffects 后通过 Consume 读取并清除此标记。
        /// </summary>
        public void SetPendingDestroyOnCapture()
        {
            pendingDestroyOnCapture = true;
            Debug.Log("[EffectBus] 捕获后销毁标记已设置");
        }

        /// <summary>
        /// 消费捕获后销毁标记。返回 true 表示跳过 AddCard，标记同时被清除。
        /// </summary>
        public bool ConsumePendingDestroyOnCapture()
        {
            if (!pendingDestroyOnCapture) return false;
            pendingDestroyOnCapture = false;
            return true;
        }

        #endregion

        #region 揭示后强制捕获

        private bool pendingForceCapture = false;

        /// <summary>
        /// 标记当前揭示的鱼卡需要强制捕获（由 Effect_ForceCapture 调用）。
        /// CardPilePanel 在 OnRevealClicked 成功后通过 Consume 读取并清除此标记。
        /// </summary>
        public void SetPendingForceCapture()
        {
            pendingForceCapture = true;
            Debug.Log("[EffectBus] 强制捕获标记已设置");
        }

        /// <summary>
        /// 消费强制捕获标记。返回 true 表示需要强制捕获，标记同时被清除。
        /// </summary>
        public bool ConsumePendingForceCapture()
        {
            if (!pendingForceCapture) return false;
            pendingForceCapture = false;
            return true;
        }

        #endregion

        #region 放弃后随机洗回

        private bool pendingShuffleBackOnAbandon = false;

        /// <summary>
        /// 标记当前揭示的鱼卡在放弃捕获时应洗回牌堆随机位置（由 Effect_ShuffleBackOnAbandon 调用）。
        /// CardPilePanel 在 OnAbandonClicked 时通过 Consume 读取并清除此标记。
        /// </summary>
        public void SetPendingShuffleBackOnAbandon()
        {
            pendingShuffleBackOnAbandon = true;
            Debug.Log("[EffectBus] 放弃后洗回标记已设置");
        }

        /// <summary>
        /// 消费放弃后洗回标记。返回 true 表示需要洗回，标记同时被清除。
        /// </summary>
        public bool ConsumePendingShuffleBackOnAbandon()
        {
            if (!pendingShuffleBackOnAbandon) return false;
            pendingShuffleBackOnAbandon = false;
            return true;
        }

        #endregion

        #region 连锁揭示本列

        private bool pendingRevealColumn = false;

        /// <summary>
        /// 标记当前揭示的鱼卡需要连锁揭示同列其他牌堆（由 Effect_RevealColumn 调用）。
        /// CardPilePanel 在 OnRevealClicked 末尾通过 Consume 读取并清除此标记。
        /// </summary>
        public void SetPendingRevealColumn()
        {
            pendingRevealColumn = true;
            Debug.Log("[EffectBus] 连锁揭示本列标记已设置");
        }

        /// <summary>
        /// 消费连锁揭示标记。返回 true 表示需要连锁揭示，标记同时被清除。
        /// </summary>
        public bool ConsumePendingRevealColumn()
        {
            if (!pendingRevealColumn) return false;
            pendingRevealColumn = false;
            return true;
        }

        #endregion

        #region 疯狂值增幅（手牌被动）

        private int sanityAmplifyCount = 0;

        /// <summary>
        /// 注册一个"疯狂值增幅"来源（手牌中持续效果激活时调用）。
        /// 引用计数，多张卡叠加。
        /// </summary>
        public void RegisterSanityAmplify()
        {
            sanityAmplifyCount++;
            Debug.Log($"[EffectBus] 疯狂值增幅已注册，当前计数={sanityAmplifyCount}");
        }

        /// <summary>
        /// 注销一个"疯狂值增幅"来源（手牌中持续效果停用时调用）。
        /// </summary>
        public void UnregisterSanityAmplify()
        {
            sanityAmplifyCount = Mathf.Max(0, sanityAmplifyCount - 1);
            Debug.Log($"[EffectBus] 疯狂值增幅已注销，当前计数={sanityAmplifyCount}");
        }

        /// <summary>
        /// 处理疯狂值变化量。仅在增加（amount > 0）时叠加增幅。
        /// GameManager.ModifySanity 调用此方法获取最终变化量。
        /// </summary>
        public int ProcessSanityChange(int amount)
        {
            if (amount > 0 && sanityAmplifyCount > 0)
                amount += sanityAmplifyCount;
            return amount;
        }

        #endregion

        #region 手牌鱼卡额外消耗（OnHandChanged 桥接）

        private int handFishCostSourceCount = 0;
        private System.Action handChangedForHandFishCost;

        /// <summary>
        /// 注册一个手牌鱼卡额外消耗来源（OnReveal 时由 Effect_CostPerHandFish 调用）。
        /// 引用计数 0→1 时订阅 HandManager.OnHandChanged，使手牌变化触发 NotifyFishingModifierChanged，
        /// 从而刷新牌堆卡面费用显示和面板捕获按钮。
        /// </summary>
        public void RegisterHandFishCostSource()
        {
            handFishCostSourceCount++;
            if (handFishCostSourceCount == 1)
            {
                handChangedForHandFishCost = () => NotifyFishingModifierChanged();
                if (HandSystem.HandManager.Instance != null)
                    HandSystem.HandManager.Instance.OnHandChanged += handChangedForHandFishCost;
            }
            NotifyFishingModifierChanged();
            Debug.Log($"[EffectBus] 手牌鱼卡额外消耗来源已注册，当前计数={handFishCostSourceCount}");
        }

        /// <summary>
        /// 注销一个手牌鱼卡额外消耗来源（卡牌被捕获时调用）。
        /// 引用计数 1→0 时取消订阅 HandManager.OnHandChanged。
        /// </summary>
        public void UnregisterHandFishCostSource()
        {
            handFishCostSourceCount = Mathf.Max(0, handFishCostSourceCount - 1);
            if (handFishCostSourceCount == 0 && handChangedForHandFishCost != null)
            {
                if (HandSystem.HandManager.Instance != null)
                    HandSystem.HandManager.Instance.OnHandChanged -= handChangedForHandFishCost;
                handChangedForHandFishCost = null;
            }
            NotifyFishingModifierChanged();
            Debug.Log($"[EffectBus] 手牌鱼卡额外消耗来源已注销，当前计数={handFishCostSourceCount}");
        }

        #endregion

        #region 手牌条件免费捕获（OnHandChanged 桥接）

        private int freeCaptureSourceCount = 0;
        private System.Action handChangedForFreeCapture;

        /// <summary>
        /// 注册一个免费捕获来源（OnReveal 时由 Effect_FreeCaptureByHand 调用）。
        /// 引用计数 0→1 时订阅 HandManager.OnHandChanged，使手牌变化触发 NotifyFishingModifierChanged，
        /// 从而刷新牌堆卡面费用显示和面板捕获按钮。
        /// </summary>
        public void RegisterFreeCaptureSource()
        {
            freeCaptureSourceCount++;
            if (freeCaptureSourceCount == 1)
            {
                handChangedForFreeCapture = () => NotifyFishingModifierChanged();
                if (HandSystem.HandManager.Instance != null)
                    HandSystem.HandManager.Instance.OnHandChanged += handChangedForFreeCapture;
            }
            NotifyFishingModifierChanged();
            Debug.Log($"[EffectBus] 免费捕获来源已注册，当前计数={freeCaptureSourceCount}");
        }

        /// <summary>
        /// 注销一个免费捕获来源（卡牌从牌堆移除时调用）。
        /// 引用计数 1→0 时取消订阅 HandManager.OnHandChanged。
        /// </summary>
        public void UnregisterFreeCaptureSource()
        {
            freeCaptureSourceCount = Mathf.Max(0, freeCaptureSourceCount - 1);
            if (freeCaptureSourceCount == 0 && handChangedForFreeCapture != null)
            {
                if (HandSystem.HandManager.Instance != null)
                    HandSystem.HandManager.Instance.OnHandChanged -= handChangedForFreeCapture;
                handChangedForFreeCapture = null;
            }
            NotifyFishingModifierChanged();
            Debug.Log($"[EffectBus] 免费捕获来源已注销，当前计数={freeCaptureSourceCount}");
        }

        #endregion

        #region 疯狂等级免费捕获（OnSanityLevelChanged 桥接）

        private int sanityCaptureSourceCount = 0;
        private UnityEngine.Events.UnityAction<SanityLevel> sanityChangedForFreeCapture;

        /// <summary>
        /// 注册一个疯狂等级免费捕获来源（OnReveal 时由 Effect_FreeCaptureOnSanity 调用）。
        /// 引用计数 0→1 时订阅 GameManager.OnSanityLevelChanged，使疯狂等级变化触发 NotifyFishingModifierChanged，
        /// 从而刷新牌堆卡面费用显示和面板捕获按钮。
        /// </summary>
        public void RegisterSanityFreeCaptureSource()
        {
            sanityCaptureSourceCount++;
            if (sanityCaptureSourceCount == 1)
            {
                sanityChangedForFreeCapture = _ => NotifyFishingModifierChanged();
                if (GameManager.Instance != null)
                    GameManager.Instance.OnSanityLevelChanged.AddListener(sanityChangedForFreeCapture);
            }
            NotifyFishingModifierChanged();
            Debug.Log($"[EffectBus] 疯狂等级免费捕获来源已注册，当前计数={sanityCaptureSourceCount}");
        }

        /// <summary>
        /// 注销一个疯狂等级免费捕获来源（卡牌被捕获/移除时调用）。
        /// 引用计数 1→0 时取消订阅 GameManager.OnSanityLevelChanged。
        /// </summary>
        public void UnregisterSanityFreeCaptureSource()
        {
            sanityCaptureSourceCount = Mathf.Max(0, sanityCaptureSourceCount - 1);
            if (sanityCaptureSourceCount == 0 && sanityChangedForFreeCapture != null)
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.OnSanityLevelChanged.RemoveListener(sanityChangedForFreeCapture);
                sanityChangedForFreeCapture = null;
            }
            NotifyFishingModifierChanged();
            Debug.Log($"[EffectBus] 疯狂等级免费捕获来源已注销，当前计数={sanityCaptureSourceCount}");
        }

        #endregion

        #region 每日重置

        private void ResetDailyFlags()
        {
            ResetHangReplace();
            ResetNextFishDiscount();
        }

        #endregion
    }
}
