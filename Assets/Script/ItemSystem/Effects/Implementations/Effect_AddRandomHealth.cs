using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 随机增加体力值效果
    /// </summary>
    [System.Serializable]
    public class Effect_AddRandomHealth : EffectBase
    {
        [Tooltip("最小体力增加值")]
        [Min(0)]
        public int minAmount = 1;
        
        [Tooltip("最大体力增加值")]
        [Min(0)]
        public int maxAmount = 10;
        
        public override string DisplayName => "随机增加体力";
        
        public override void Execute(EffectContext context)
        {
            GameObject player = GameObject.Find("player");
            if (player == null)
            {
                Debug.LogWarning("[Effect_AddRandomHealth] 未找到玩家对象");
                return;
            }
            
            CharacterState playerState = player.GetComponent<CharacterState>();
            if (playerState == null)
            {
                Debug.LogError("[Effect_AddRandomHealth] 玩家对象缺少CharacterState组件");
                return;
            }
            
            int randomHealth = Random.Range(minAmount, maxAmount + 1);
            playerState.ModifyHealth(randomHealth);
            
            Debug.Log($"[Effect_AddRandomHealth] 随机增加体力 +{randomHealth} (范围:{minAmount}-{maxAmount})");
        }
        
        public override string GetDescription()
        {
            return $"体力 +{minAmount}~{maxAmount}";
        }
    }
}
