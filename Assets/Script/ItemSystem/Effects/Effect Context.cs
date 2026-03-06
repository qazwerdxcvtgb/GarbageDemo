using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 效果执行时的上下文数据包
    /// </summary>
    public class EffectContext
    {
        // 必须参数：谁是目标
        public GameObject Target;

        // 可选参数：施法者是谁（反伤、吸血可能需要）
        public GameObject Source;

        // 核心数值：可以是伤害值、治疗量、持续时间等
        // 如果某个效果不需要数值，调用者传 0 即可，接收者也会忽略它
        public int NumericValue;
        
        // 范围随机数值（可选）
        public int MaxValue;
        public int MinValue;

        //目标类别（用于传递要抽什么类型的牌）
        public ItemCategory TargetCategory;

        // 构造函数方便创建
        public EffectContext() { }

        public EffectContext(int value = 0)
        {
            NumericValue = value;
        }
        public EffectContext(int maxValue = 0, int minValue = 0)
        {
            MaxValue = maxValue;
            MinValue = minValue;
        }
        public EffectContext(GameObject target, int value = 0, GameObject source = null, int maxValue = 0, int minValue = 0)
        {
            Target = target;
            NumericValue = value;
            Source = source;
            MaxValue = maxValue;
            MinValue = minValue;
        }
        public EffectContext(ItemCategory itemCategory, int value = 1)
        {
            NumericValue = value;
            TargetCategory = itemCategory;
        }
    }
}
