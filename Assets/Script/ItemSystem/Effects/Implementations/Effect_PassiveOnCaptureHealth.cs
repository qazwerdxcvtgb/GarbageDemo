using System;
using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 被动效果：每次成功捕获鱼类时恢复固定体力。
    /// amount 在装备资源上配置，通过普通治疗恢复基础体力（上限 maxHealth）。
    /// </summary>
    [CreateAssetMenu(fileName = "Effect_PassiveOnCaptureHealth", menuName = "ItemSystem/Effects/PassiveOnCaptureHealth")]
    public class Effect_PassiveOnCaptureHealth : PassiveEffect
    {
        [Header("恢复配置")]
        [Tooltip("每次捕获恢复的体力值")]
        [Min(1)]
        public int amount = 2;

        private Action registeredHandler;

        public override void Register()
        {
            if (registeredHandler != null)
            {
                Debug.LogWarning($"[PassiveOnCaptureHealth] {effectName} 已注册，请勿重复注册");
                return;
            }

            registeredHandler = OnFishCaptured;
            EffectBus.Instance.OnFishCaptured += registeredHandler;

            Debug.Log($"[PassiveOnCaptureHealth] 注册：{effectName}，恢复量={amount}");
        }

        public override void Unregister()
        {
            if (registeredHandler == null)
            {
                Debug.LogWarning($"[PassiveOnCaptureHealth] {effectName} 尚未注册，无法注销");
                return;
            }

            if (EffectBus.Instance != null)
                EffectBus.Instance.OnFishCaptured -= registeredHandler;
            registeredHandler = null;

            Debug.Log($"[PassiveOnCaptureHealth] 注销：{effectName}");
        }

        private void OnFishCaptured()
        {
            GameObject player = GameObject.Find("player");
            if (player == null) return;
            CharacterState playerState = player.GetComponent<CharacterState>();
            if (playerState == null) return;

            playerState.ModifyHealth(amount);
            Debug.Log($"[PassiveOnCaptureHealth] {effectName}：捕获恢复体力 +{amount}");
        }

        public override string GetEffectInfo()
        {
            return $"{effectName}：每次捕获恢复体力 +{amount}";
        }
    }
}
