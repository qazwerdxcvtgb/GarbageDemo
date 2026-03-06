using UnityEngine;
using HandSystem;

namespace ItemSystem
{
    /// <summary>
    /// 抽牌效果：从指定类型的牌堆抽取N张牌加入手牌
    /// </summary>
    [System.Serializable]
    public class Effect_DrawCards : EffectBase
    {
        [Tooltip("抽取的卡牌数量")]
        [Min(1)]
        public int count = 1;
        
        [Tooltip("目标物品类型")]
        public ItemCategory category = ItemCategory.Fish;
        
        public override string DisplayName => "抽取卡牌";
        
        public override void Execute(EffectContext context)
        {
            if (count <= 0) return;

            // 检查必要单例
            if (ItemPool.Instance == null || HandManager.Instance == null)
            {
                Debug.LogError("[Effect_DrawCards] ItemPool 或 HandManager 缺失！");
                return;
            }

            Debug.Log($"[Effect_DrawCards] 开始抽取 {count} 张 {category}...");

            // 循环抽牌
            for (int i = 0; i < count; i++)
            {
                ItemData drawnItem = ItemPool.Instance.DrawItem(category);

                if (drawnItem != null)
                {
                    HandManager.Instance.AddCard(drawnItem);
                }
                else
                {
                    Debug.LogWarning($"[Effect_DrawCards] {category} 牌堆已空，无法继续抽取。");
                    break;
                }
            }
        }
        
        public override string GetDescription()
        {
            return $"抽取 {count}张 {category.ToChineseText()}";
        }
    }
}
