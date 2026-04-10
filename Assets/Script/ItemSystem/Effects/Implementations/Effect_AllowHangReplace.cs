using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 使用效果：允许更换悬挂
    /// 使用后当日内可以取下和替换悬挂槽中的鱼（不影响装备槽）。
    /// 进入下一天时由 EffectBus 自动重置。
    /// 挂载在 ConsumableData 的 effects 列表中，trigger = OnUse。
    /// </summary>
    [System.Serializable]
    public class Effect_AllowHangReplace : EffectBase
    {
        public override string DisplayName => "允许更换悬挂";

        public override void Execute(EffectContext context)
        {
            EffectBus.Instance.EnableHangReplace();
        }

        public override string GetDescription() => "本日内可以取下和更换悬挂的鱼";
    }
}
