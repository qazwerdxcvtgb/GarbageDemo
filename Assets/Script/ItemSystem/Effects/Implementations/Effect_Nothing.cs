using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 空效果占位：什么都不发生。
    /// 用于 Effect_RandomChoice 候选列表，表示"正常捕获，无额外效果"。
    /// </summary>
    [System.Serializable]
    public class Effect_Nothing : EffectBase
    {
        public override string DisplayName => "无事发生";

        public override void Execute(EffectContext context)
        {
            Debug.Log("[Effect_Nothing] 无事发生");
        }

        public override string GetDescription() => "无事发生";
    }
}
