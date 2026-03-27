using System;
using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 被动效果：钓鱼捕获体力消耗折扣
    /// 装备后每次捕获鱼类时，体力消耗减少 reduction 点（最低消耗为 0）。
    /// 通过订阅 EffectBus.OnModifyFishingCost 实现无侵入式修改。
    /// </summary>
    [CreateAssetMenu(fileName = "Effect_FishingStaminaDiscount", menuName = "ItemSystem/Effects/FishingStaminaDiscount")]
    public class Effect_FishingStaminaDiscount : PassiveEffect
    {
        [Header("折扣配置")]
        [Tooltip("每次钓鱼减少的体力消耗点数（不可小于 0）")]
        [Min(0)]
        public int reduction = 1;

        // 存储已注册的委托引用，确保 Unregister 时能精确移除
        private Func<int, int> registeredModifier;

        public override void Register()
        {
            if (registeredModifier != null)
            {
                Debug.LogWarning($"[Effect_FishingStaminaDiscount] {effectName} 已注册，请勿重复注册");
                return;
            }

            registeredModifier = cost => cost - reduction;
            EffectBus.Instance.OnModifyFishingCost += registeredModifier;
            Debug.Log($"[Effect_FishingStaminaDiscount] 注册：{effectName}，折扣 -{reduction}");
            EffectBus.Instance.NotifyFishingModifierChanged();
        }

        public override void Unregister()
        {
            if (registeredModifier == null)
            {
                Debug.LogWarning($"[Effect_FishingStaminaDiscount] {effectName} 尚未注册，无法注销");
                return;
            }

            EffectBus.Instance.OnModifyFishingCost -= registeredModifier;
            registeredModifier = null;
            Debug.Log($"[Effect_FishingStaminaDiscount] 注销：{effectName}");
            EffectBus.Instance.NotifyFishingModifierChanged();
        }

        public override string GetEffectInfo()
            => $"{effectName}：钓鱼体力消耗 -{reduction}";
    }
}
