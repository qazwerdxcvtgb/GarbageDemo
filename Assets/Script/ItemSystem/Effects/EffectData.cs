using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 效果数据基类
    /// </summary>
    public abstract class EffectData : ScriptableObject
    {
        [Header("效果信息")]
        [Tooltip("效果名称")]
        public string effectName;
        
        [Tooltip("效果描述")]
        [TextArea(2, 4)]
        public string effectDescription;
        
        /// <summary>
        /// 执行效果（子类实现）
        /// </summary>
        //public abstract void ExecutetrAnsformable(int amount);
        public abstract void Execute(EffectContext context);
        /// <summary>
        /// 获取效果信息
        /// </summary>
        public virtual string GetEffectInfo()
        {
            return $"{effectName}: {effectDescription}";
        }
    }
}
