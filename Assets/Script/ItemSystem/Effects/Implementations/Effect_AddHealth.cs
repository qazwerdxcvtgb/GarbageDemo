using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 增加固定体力值效果
    /// </summary>
    [System.Serializable]
    public class Effect_AddHealth : EffectBase
    {
        [Tooltip("增加的体力值")]
        [Min(0)]
        public int amount = 5;
        
        public override string DisplayName => "增加体力";
        
        public override void Execute(EffectContext context)
        {
            GameObject player = GameObject.Find("player");
            if (player == null)
            {
                Debug.LogWarning("[Effect_AddHealth] 未找到玩家对象");
                return;
            }
            
            CharacterState playerState = player.GetComponent<CharacterState>();
            if (playerState == null)
            {
                Debug.LogError("[Effect_AddHealth] 玩家对象缺少CharacterState组件");
                return;
            }
            
            playerState.ModifyHealth(amount);
            Debug.Log($"[Effect_AddHealth] 增加体力 +{amount}");
        }
        
        public override string GetDescription()
        {
            return $"体力 +{amount}";
        }
    }
}
