using System;
using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 被动效果：每日刷新时获得随机范围的装备临时体力。
    /// minAmount / maxAmount 在装备资源上配置，支持负值范围（如 -9~+9）。
    /// 正值 → 设置装备体力槽（SetBonusHealth，可叠加多件装备）。
    /// 负值 → 直接扣除基础体力（ModifyHealth）。
    /// </summary>
    [CreateAssetMenu(fileName = "Effect_PassiveDailyHealth", menuName = "ItemSystem/Effects/PassiveDailyHealth")]
    public class Effect_PassiveDailyHealth : PassiveEffect
    {
        [Header("体力范围配置")]
        [Tooltip("每日随机补充的最小值（含），可为负")]
        public int minAmount = 1;

        [Tooltip("每日随机补充的最大值（含），可为负")]
        public int maxAmount = 2;

        private Action registeredHandler;

        public override void Register()
        {
            if (registeredHandler != null)
            {
                Debug.LogWarning($"[PassiveDailyHealth] {effectName} 已注册，请勿重复注册");
                return;
            }

            registeredHandler = OnDayRefresh;
            EffectBus.Instance.OnDayRefreshCompleted += registeredHandler;

            Debug.Log($"[PassiveDailyHealth] 注册：{effectName}，范围=[{minAmount}, {maxAmount}]");
        }

        public override void Unregister()
        {
            if (registeredHandler == null)
            {
                Debug.LogWarning($"[PassiveDailyHealth] {effectName} 尚未注册，无法注销");
                return;
            }

            if (EffectBus.Instance != null)
                EffectBus.Instance.OnDayRefreshCompleted -= registeredHandler;
            registeredHandler = null;

            Debug.Log($"[PassiveDailyHealth] 注销：{effectName}");
        }

        private void OnDayRefresh()
        {
            int roll = UnityEngine.Random.Range(minAmount, maxAmount + 1);

            GameObject player = GameObject.Find("player");
            if (player == null) return;
            CharacterState playerState = player.GetComponent<CharacterState>();
            if (playerState == null) return;

            if (roll >= 0)
            {
                playerState.SetBonusHealth(playerState.BonusHealth + roll);
                Debug.Log($"[PassiveDailyHealth] {effectName}：装备体力 +{roll}（当前装备体力={playerState.BonusHealth}）");
            }
            else
            {
                playerState.ModifyHealth(roll);
                Debug.Log($"[PassiveDailyHealth] {effectName}：基础体力 {roll}");
            }
        }

        public override string GetEffectInfo()
        {
            if (minAmount >= 0)
                return $"{effectName}：每日装备体力 +{minAmount}~+{maxAmount}";
            if (maxAmount <= 0)
                return $"{effectName}：每日体力 {minAmount}~{maxAmount}";
            return $"{effectName}：每日体力 {minAmount:+#;-#;0}~{maxAmount:+#;-#;0}";
        }
    }
}
