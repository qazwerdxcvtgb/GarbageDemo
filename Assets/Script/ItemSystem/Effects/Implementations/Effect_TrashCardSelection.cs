using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 被动效果：杂鱼卡三选一
    /// 装备后，放弃捕获获取杂鱼卡时从杂鱼牌库抽取3张展示选择面板，
    /// 玩家选择1张加入手牌，未选中的归还牌库并洗牌。
    /// 天数推进自动获取的杂鱼卡不触发此被动。
    /// 通过 EffectBus 引用计数实现，支持多件装备叠加（行为不变）。
    /// </summary>
    [CreateAssetMenu(fileName = "Effect_TrashCardSelection", menuName = "ItemSystem/Effects/TrashCardSelection")]
    public class Effect_TrashCardSelection : PassiveEffect
    {
        private bool isRegistered;

        private void OnEnable()
        {
            isRegistered = false;
        }

        public override void Register()
        {
            if (isRegistered)
            {
                Debug.LogWarning($"[TrashCardSelection] {effectName} 已注册，请勿重复注册");
                return;
            }

            EffectBus.Instance.RegisterTrashSelection();
            isRegistered = true;
            Debug.Log($"[TrashCardSelection] 注册：{effectName}");
        }

        public override void Unregister()
        {
            if (!isRegistered)
            {
                Debug.LogWarning($"[TrashCardSelection] {effectName} 尚未注册，无法注销");
                return;
            }

            EffectBus.Instance.UnregisterTrashSelection();
            isRegistered = false;
            Debug.Log($"[TrashCardSelection] 注销：{effectName}");
        }

        public override string GetEffectInfo()
            => $"{effectName}：放弃捕获时可从3张杂鱼卡中选择1张";
    }
}
