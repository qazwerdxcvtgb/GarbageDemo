using System;
using UnityEngine;

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

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        #region 钓鱼体力消耗修改链

        /// <summary>
        /// 钓鱼捕获体力消耗修改链。
        /// 每个订阅者接收当前消耗值，返回修改后的值（可减少也可增加）。
        /// 注册示例：EffectBus.Instance.OnModifyFishingCost += cost => cost - reduction;
        /// </summary>
        public event Func<int, int> OnModifyFishingCost;

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
        /// <returns>经过所有被动效果修改后的最终消耗值（≥ 0）</returns>
        public int ProcessFishingCost(int baseCost)
        {
            int result = baseCost;
            if (OnModifyFishingCost != null)
            {
                foreach (Delegate d in OnModifyFishingCost.GetInvocationList())
                {
                    if (d is Func<int, int> modifier)
                        result = modifier(result);
                }
            }
            return Mathf.Max(0, result);
        }

        #endregion
    }
}
