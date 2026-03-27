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
        /// 获取效果描述（纯效果文本，不含触发时机前缀）
        /// </summary>
        public virtual string GetDescription() => DisplayName;

        /// <summary>
        /// 获取带触发时机前缀的完整效果描述，格式："{触发时机}：{效果描述}"
        /// 示例："使用时：疯狂值+1"
        /// </summary>
        public virtual string GetFullDescription()
            => $"{trigger.ToChineseText()}：{GetDescription()}";
    }
}
