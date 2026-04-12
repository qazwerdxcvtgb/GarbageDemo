using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 永久修改体力上限（本局有效，ResetState 时恢复初始值）。
    /// 正数增加，负数减少。
    /// </summary>
    [System.Serializable]
    public class Effect_ModifyMaxHealth : EffectBase
    {
        [Tooltip("体力上限变化量（正数增加，负数减少）")]
        public int amount = 1;

        public override string DisplayName => "修改体力上限";

        public override void Execute(EffectContext context)
        {
            GameObject player = GameObject.Find("player");
            if (player == null)
            {
                Debug.LogWarning("[Effect_ModifyMaxHealth] 未找到玩家对象");
                return;
            }

            CharacterState playerState = player.GetComponent<CharacterState>();
            if (playerState == null)
            {
                Debug.LogError("[Effect_ModifyMaxHealth] 玩家对象缺少CharacterState组件");
                return;
            }

            playerState.ModifyMaxHealth(amount);

            string changeText = amount >= 0 ? $"+{amount}" : $"{amount}";
            Debug.Log($"[Effect_ModifyMaxHealth] 体力上限 {changeText}，当前上限={playerState.MaxHealth}");
        }

        public override string GetDescription()
        {
            string sign = amount >= 0 ? "+" : "";
            return $"体力上限 {sign}{amount}";
        }
    }
}
