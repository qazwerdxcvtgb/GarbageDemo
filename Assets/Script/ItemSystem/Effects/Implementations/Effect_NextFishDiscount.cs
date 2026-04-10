using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 使用效果：下一条鱼体力折扣
    /// 消耗品使用后，下一条捕获的鱼体力消耗减少指定点数（不低于0）。
    /// 可叠加累计，捕获一条鱼后自动清零，每日重置时也会清零。
    /// 挂载在 ConsumableData 的 effects 列表中，trigger = OnUse。
    /// </summary>
    [System.Serializable]
    public class Effect_NextFishDiscount : EffectBase
    {
        [Tooltip("减少的体力消耗点数")]
        [Min(1)]
        public int reduction = 4;

        public override string DisplayName => "下一条鱼体力折扣";

        public override void Execute(EffectContext context)
        {
            EffectBus.Instance.AddNextFishDiscount(reduction);
        }

        public override string GetDescription()
            => $"下一条捕获的鱼体力消耗 -{reduction}（不低于0）";
    }
}
