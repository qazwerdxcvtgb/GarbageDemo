using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 鱼类卡牌数据
    /// </summary>
    [CreateAssetMenu(fileName = "Fish_", menuName = "ItemSystem/Fish", order = 1)]
    public class FishData : ItemData
    {
        [Header("鱼类特有属性")]
        [Tooltip("深度等级")]
        public FishDepth depth;
        
        [Tooltip("体积大小")]
        public FishSize size;
        
        [Tooltip("消耗体力")]
        public int staminaCost;
        
        [Tooltip("鱼类类型（纯净/污秽）")]
        public FishType fishType;
        
        [Header("效果列表")]
        [Tooltip("效果列表（每个效果自带触发时机）")]
        [SerializeReference]
        public List<EffectBase> effects = new List<EffectBase>();
        
        /// <summary>
        /// 触发指定时机的所有效果
        /// </summary>
        /// <param name="trigger">触发时机</param>
        public void TriggerEffects(EffectTrigger trigger)
        {
            var context = new EffectContext
            {
                Target = GameObject.Find("player")
            };
            
            Debug.Log($"[FishData] 触发 {trigger} 效果：{itemName}");
            
            foreach (var effect in effects)
            {
                if (effect != null && effect.trigger == trigger)
                {
                    effect.Execute(context);
                }
            }
        }
        
        /// <summary>
        /// 触发揭示效果
        /// </summary>
        public void TriggerRevealEffects() => TriggerEffects(EffectTrigger.OnReveal);
        
        /// <summary>
        /// 触发捕获效果
        /// </summary>
        public void TriggerCaptureEffects() => TriggerEffects(EffectTrigger.OnCapture);
        
        /// <summary>
        /// 触发使用效果
        /// </summary>
        public void TriggerUseEffects() => TriggerEffects(EffectTrigger.OnUse);
        
        /// <summary>
        /// 触发丢弃效果
        /// </summary>
        public void TriggerDiscardEffects() => TriggerEffects(EffectTrigger.OnDiscard);
        
        public override bool CanUse(out string reason) => CheckUseEffects(effects, out reason);
        
        public override string GetItemInfo()
        {
            var effectDescs = effects
                .Where(e => e != null)
                .Select(e => e.GetFullDescription());

            return $"【{itemName}】\n" +
                   $"深度: {depth.ToChineseText()}\n" +
                   $"体积: {size.ToChineseText()}\n" +
                   $"价值: {value}\n" +
                   $"消耗: {staminaCost}\n" +
                   $"类型: {fishType.ToChineseText()}\n" +
                   $"效果:\n  {string.Join("\n  ", effectDescs)}";
        }
        
        public override string GetBriefInfo()
        {
            var effectCount = effects?.Count ?? 0;
            return $"{itemName} ({fishType.ToChineseText()}) - {effectCount}个效果";
        }
    }
}
