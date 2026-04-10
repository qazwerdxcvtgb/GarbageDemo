using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 被动效果：免疫揭示效果
    /// 装备后，翻牌揭示鱼类时跳过鱼卡自身的 OnReveal 效果（正面和负面均不触发）。
    /// 通过 EffectBus 旁路计数器实现，支持多件装备叠加。
    /// </summary>
    [CreateAssetMenu(fileName = "Effect_IgnoreRevealEffect", menuName = "ItemSystem/Effects/IgnoreRevealEffect")]
    public class Effect_IgnoreRevealEffect : PassiveEffect
    {
        private bool isRegistered;

        public override void Register()
        {
            if (isRegistered)
            {
                Debug.LogWarning($"[IgnoreRevealEffect] {effectName} 已注册，请勿重复注册");
                return;
            }

            EffectBus.Instance.RegisterIgnoreRevealEffects();
            isRegistered = true;
            Debug.Log($"[IgnoreRevealEffect] 注册：{effectName}");
        }

        public override void Unregister()
        {
            if (!isRegistered)
            {
                Debug.LogWarning($"[IgnoreRevealEffect] {effectName} 尚未注册，无法注销");
                return;
            }

            EffectBus.Instance.UnregisterIgnoreRevealEffects();
            isRegistered = false;
            Debug.Log($"[IgnoreRevealEffect] 注销：{effectName}");
        }

        public override string GetEffectInfo()
            => $"{effectName}：揭示时免疫鱼卡效果";
    }
}
