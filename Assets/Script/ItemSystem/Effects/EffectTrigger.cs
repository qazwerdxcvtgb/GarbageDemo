using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 效果触发时机枚举
    /// </summary>
    public enum EffectTrigger
    {
        [InspectorName("揭示时")]
        OnReveal,
        
        [InspectorName("捕获时")]
        OnCapture,
        
        [InspectorName("使用时")]
        OnUse,
        
        [InspectorName("丢弃时")]
        OnDiscard,
        
        [InspectorName("装备时")]
        OnEquip,
        
        [InspectorName("卸下时")]
        OnUnequip,
        
        [InspectorName("手牌中持续")]
        WhileInHand
    }
}
