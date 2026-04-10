using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 修改玩家金币效果（正数增加，负数减少）
    /// </summary>
    [System.Serializable]
    public class Effect_ModifyGold : EffectBase
    {
        [Tooltip("金币变化量（正数增加，负数减少）")]
        public int amount = 1;

        public override string DisplayName => "修改金币";

        public override void Execute(EffectContext context)
        {
            GameObject player = GameObject.Find("player");
            if (player == null)
            {
                Debug.LogWarning("[Effect_ModifyGold] 未找到玩家对象");
                return;
            }

            CharacterState playerState = player.GetComponent<CharacterState>();
            if (playerState == null)
            {
                Debug.LogError("[Effect_ModifyGold] 玩家对象缺少CharacterState组件");
                return;
            }

            playerState.ModifyGold(amount);

            string changeText = amount >= 0 ? $"+{amount}" : $"{amount}";
            Debug.Log($"[Effect_ModifyGold] 金币 {changeText}");
        }

        public override string GetDescription()
        {
            string sign = amount >= 0 ? "+" : "";
            return $"金币 {sign}{amount}";
        }
    }
}
