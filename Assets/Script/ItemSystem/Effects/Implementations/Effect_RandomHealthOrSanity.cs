using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 随机补充体力或改变疯狂值效果
    /// 50%概率执行其中一种效果
    /// </summary>
    [System.Serializable]
    public class Effect_RandomHealthOrSanity : EffectBase
    {
        [Tooltip("补充的体力值")]
        [Min(0)]
        public int healthAmount = 5;
        
        [Tooltip("疯狂值变化量（正数增加，负数减少）")]
        public int sanityAmount = 3;
        
        public override string DisplayName => "随机补充体力或改变疯狂值";
        
        public override void Execute(EffectContext context)
        {
            // 50% 概率选择其中一个效果
            bool chooseHealth = Random.Range(0, 2) == 0;
            
            if (chooseHealth)
            {
                // 执行增加体力效果
                GameObject player = GameObject.Find("player");
                if (player == null)
                {
                    Debug.LogWarning("[Effect_RandomHealthOrSanity] 未找到玩家对象");
                    return;
                }
                
                CharacterState playerState = player.GetComponent<CharacterState>();
                if (playerState == null)
                {
                    Debug.LogError("[Effect_RandomHealthOrSanity] 玩家对象缺少CharacterState组件");
                    return;
                }
                
                playerState.ModifyHealth(healthAmount);
                Debug.Log($"[Effect_RandomHealthOrSanity] 随机选择：增加体力 +{healthAmount}");
            }
            else
            {
                // 执行修改疯狂值效果
                if (GameManager.Instance == null)
                {
                    Debug.LogError("[Effect_RandomHealthOrSanity] GameManager不存在");
                    return;
                }
                
                GameManager.Instance.ModifySanity(sanityAmount);
                
                string changeText = sanityAmount >= 0 ? $"+{sanityAmount}" : $"{sanityAmount}";
                Debug.Log($"[Effect_RandomHealthOrSanity] 随机选择：疯狂值 {changeText}");
            }
        }
        
        public override string GetDescription()
        {
            string sanitySign = sanityAmount >= 0 ? "+" : "";
            return $"体力 +{healthAmount} 或 疯狂值 {sanitySign}{sanityAmount}";
        }
    }
}
