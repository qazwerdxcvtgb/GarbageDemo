using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 效果基类
    /// 使用 [SerializeReference] 实现多态序列化
    /// </summary>
    [System.Serializable]
    public abstract class EffectBase
    {
        [Tooltip("触发时机")]
        public EffectTrigger trigger = EffectTrigger.OnUse;
        
        /// <summary>
        /// 效果显示名称（用于 Inspector 显示）
        /// </summary>
        public abstract string DisplayName { get; }
        
        /// <summary>
        /// 执行效果
        /// </summary>
        /// <param name="context">效果上下文</param>
        public abstract void Execute(EffectContext context);
        
        /// <summary>
        /// 获取效果描述（用于UI显示和调试）
        /// </summary>
        public virtual string GetDescription() => DisplayName;
    }
}
