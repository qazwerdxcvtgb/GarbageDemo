using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 增加玩家所在深度+1（Depth1→Depth2→Depth3，已在 Depth3 时禁止使用）
    /// </summary>
    [System.Serializable]
    public class Effect_IncreaseDepth : EffectBase
    {
        public override string DisplayName => "增加深度";

        public override void Execute(EffectContext context)
        {
            GameObject player = GameObject.Find("player");
            if (player == null)
            {
                Debug.LogWarning("[Effect_IncreaseDepth] 未找到玩家对象");
                return;
            }

            CharacterState playerState = player.GetComponent<CharacterState>();
            if (playerState == null)
            {
                Debug.LogError("[Effect_IncreaseDepth] 玩家对象缺少CharacterState组件");
                return;
            }

            FishDepth current = playerState.CurrentDepth;
            if (current == FishDepth.Depth3)
            {
                Debug.LogWarning("[Effect_IncreaseDepth] 已在最深层，无法继续下潜");
                return;
            }

            FishDepth next = current == FishDepth.Depth1 ? FishDepth.Depth2 : FishDepth.Depth3;
            playerState.SetDepth(next);
            Debug.Log($"[Effect_IncreaseDepth] 深度变更：{current} → {next}");
        }

        public override (bool canUse, string reason) CanExecute(EffectContext context)
        {
            GameObject player = GameObject.Find("player");
            if (player == null)
                return (false, "未找到玩家对象");

            CharacterState playerState = player.GetComponent<CharacterState>();
            if (playerState == null)
                return (false, "玩家缺少状态组件");

            if (playerState.CurrentDepth == FishDepth.Depth3)
                return (false, "已在最深层");

            return (true, null);
        }

        public override string GetDescription() => "深度 +1";
    }
}
