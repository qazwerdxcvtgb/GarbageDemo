using UnityEngine;
using static UnityEditor.Timeline.TimelinePlaybackControls;

namespace ItemSystem
{
    /// <summary>
    /// 常驻被动效果基类（装备生效时持续触发）
    /// </summary>
    public abstract class PassiveEffect : EffectData
    {
        [Header("触发条件")]
        [Tooltip("触发时机")]
        public PassiveTrigger trigger;
        
        /// <summary>
        /// 注册效果（装备装备时调用）
        /// </summary>
        public abstract void Register();
        
        /// <summary>
        /// 注销效果（卸下装备时调用）
        /// </summary>
        public abstract void Unregister();
        
        /// <summary>
        /// 即时执行（被动效果通常不需要主动执行）
        /// </summary>
        public override void Execute(EffectContext context)
        {
            Debug.LogWarning($"[PassiveEffect] 被动效果不应主动执行：{effectName}");
        }
    }
}
