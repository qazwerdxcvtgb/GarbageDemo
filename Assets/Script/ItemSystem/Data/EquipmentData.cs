using System.Collections.Generic;
using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 装备卡牌数据
    /// </summary>
    [CreateAssetMenu(fileName = "Equipment_", menuName = "ItemSystem/Equipment", order = 4)]
    public class EquipmentData : ItemData
    {
        [Header("装备特有属性")]
        [Tooltip("装备槽位（鱼竿/渔具）")]
        public EquipmentSlot slot;
        
        [Header("常驻效果配置")]
        [Tooltip("被动效果列表（装备后持续生效）")]
        public List<PassiveEffect> passiveEffects = new List<PassiveEffect>();
        
        /// <summary>
        /// 装备时触发（注册被动效果）
        /// </summary>
        public void OnEquip()
        {
            Debug.Log($"[EquipmentData] 装备：{itemName}");
            foreach (var effect in passiveEffects)
            {
                if (effect != null)
                {
                    effect.Register();
                }
            }
        }
        
        /// <summary>
        /// 卸下时触发（注销被动效果）
        /// </summary>
        public void OnUnequip()
        {
            Debug.Log($"[EquipmentData] 卸下：{itemName}");
            foreach (var effect in passiveEffects)
            {
                if (effect != null)
                {
                    effect.Unregister();
                }
            }
        }
        
        public override string GetItemInfo()
        {
            return $"【{itemName}】\n" +
                   $"类型: 装备\n" +
                   $"槽位: {slot.ToChineseText()}\n" +
                   $"价值: {value}\n" +
                   $"被动效果: {GetEffectNames(passiveEffects)}";
        }
        
        private string GetEffectNames(List<PassiveEffect> effects)
        {
            if (effects == null || effects.Count == 0) return "无";
            return string.Join(", ", effects.ConvertAll(e => e != null ? e.effectName : "空"));
        }
    }
}
