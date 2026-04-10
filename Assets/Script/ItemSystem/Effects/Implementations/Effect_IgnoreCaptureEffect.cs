using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 被动效果：免疫捕获效果
    /// 装备后，捕获鱼类时跳过鱼卡自身的 OnCapture 效果（正面和负面均不触发）。
    /// 通过 EffectBus 旁路计数器实现，支持多件装备叠加。
    /// </summary>
    [CreateAssetMenu(fileName = "Effect_IgnoreCaptureEffect", menuName = "ItemSystem/Effects/IgnoreCaptureEffect")]
    public class Effect_IgnoreCaptureEffect : PassiveEffect
    {
        private bool isRegistered;

        public override void Register()
        {
            if (isRegistered)
            {
                Debug.LogWarning($"[IgnoreCaptureEffect] {effectName} 已注册，请勿重复注册");
                return;
            }

            EffectBus.Instance.RegisterIgnoreCaptureEffects();
            isRegistered = true;
            Debug.Log($"[IgnoreCaptureEffect] 注册：{effectName}");
        }

        public override void Unregister()
        {
            if (!isRegistered)
            {
                Debug.LogWarning($"[IgnoreCaptureEffect] {effectName} 尚未注册，无法注销");
                return;
            }

            EffectBus.Instance.UnregisterIgnoreCaptureEffects();
            isRegistered = false;
            Debug.Log($"[IgnoreCaptureEffect] 注销：{effectName}");
        }

        public override string GetEffectInfo()
            => $"{effectName}：捕获时免疫鱼卡效果";
    }
}
