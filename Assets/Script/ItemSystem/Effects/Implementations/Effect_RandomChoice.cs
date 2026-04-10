using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 随机选择效果：从候选效果列表中等概率随机选一个执行。
    /// 候选列表在 Inspector 中配置，可复用任意已有的 EffectBase 子类。
    /// </summary>
    [System.Serializable]
    public class Effect_RandomChoice : EffectBase
    {
        [Tooltip("候选效果列表，执行时等概率随机选一个触发")]
        [SerializeReference]
        public List<EffectBase> candidates = new List<EffectBase>();

        public override string DisplayName => "随机选择";

        public override void Execute(EffectContext context)
        {
            var valid = candidates.Where(e => e != null).ToList();
            if (valid.Count == 0)
            {
                Debug.LogWarning("[Effect_RandomChoice] 候选效果列表为空，跳过执行");
                return;
            }

            var chosen = valid[Random.Range(0, valid.Count)];
            chosen.Execute(context);
            Debug.Log($"[Effect_RandomChoice] 随机选中：{chosen.DisplayName} — {chosen.GetDescription()}");
        }

        public override string GetDescription() => "";
    }
}
