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
            int result = baseCost;
            if (OnModifyFishingCost != null)
            {
                foreach (Delegate d in OnModifyFishingCost.GetInvocationList())
                {
                    if (d is Func<int, FishData, int> modifier)
                        result = modifier(result, fishData);
                }
            }
            if (nextFishCostReduction > 0)
                result -= nextFishCostReduction;
            return Mathf.Max(0, result);
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

        #region 每日重置

        private void ResetDailyFlags()
        {
            ResetHangReplace();
            ResetNextFishDiscount();
        }

        #endregion
    }
}
