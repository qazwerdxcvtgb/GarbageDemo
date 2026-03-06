using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 杂鱼卡牌数据（放弃钓鱼奖励）
    /// 使用 [SerializeReference] 支持多态效果序列化
    /// </summary>
    [CreateAssetMenu(fileName = "Trash_", menuName = "ItemSystem/Trash", order = 2)]
    public class TrashData : ItemData
    {
        [Header("效果列表")]
        [Tooltip("所有效果（效果自带触发时机）")]
        [SerializeReference]
        public List<EffectBase> effects = new List<EffectBase>();
        
        /// <summary>
        /// 根据触发时机执行效果
        /// </summary>
        public void TriggerEffects(EffectTrigger trigger)
        {
            EffectContext context = new EffectContext();
            GameObject player = GameObject.Find("player");
            if (player != null)
            {
                context.Target = player;
            }
            
            foreach (var effect in effects)
            {
                if (effect != null && effect.trigger == trigger)
                {
                    effect.Execute(context);
                }
            }
        }

        /// <summary>
        /// 触发使用效果
        /// </summary>
        public void TriggerUseEffects()
        {
            Debug.Log($"[TrashData] 触发使用效果：{itemName}");
            TriggerEffects(EffectTrigger.OnUse);
        }
        
        public override string GetItemInfo()
        {
            return $"【{itemName}】\n" +
                   $"类型: 杂鱼\n" +
                   $"价值: {value}\n" +
                   $"使用效果: {GetEffectsDescription()}";
        }
        
        private string GetEffectsDescription()
        {
            if (effects == null || effects.Count == 0) return "无";
            
            var useEffects = effects.Where(e => e != null && e.trigger == EffectTrigger.OnUse);
            if (!useEffects.Any()) return "无";
            
            return string.Join(", ", useEffects.Select(e => e.GetDescription()));
        }
    }
}
