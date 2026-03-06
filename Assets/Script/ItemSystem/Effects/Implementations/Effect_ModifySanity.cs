using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 修改世界疯狂值效果
    /// </summary>
    [System.Serializable]
    public class Effect_ModifySanity : EffectBase
    {
        [Tooltip("疯狂值变化量（正数增加，负数减少）")]
        public int amount = 10;
        
        public override string DisplayName => "修改疯狂值";
        
        public override void Execute(EffectContext context)
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("[Effect_ModifySanity] GameManager不存在");
                return;
            }
            
            GameManager.Instance.ModifySanity(amount);
            
            string changeText = amount >= 0 ? $"+{amount}" : $"{amount}";
            Debug.Log($"[Effect_ModifySanity] 疯狂值 {changeText}");
        }
        
        public override string GetDescription()
        {
            string sign = amount >= 0 ? "+" : "";
            return $"疯狂值 {sign}{amount}";
        }
    }
}
