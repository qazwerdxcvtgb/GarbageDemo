using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DaySystem;

namespace ItemSystem
{
    /// <summary>
    /// 物品数据基类
    /// </summary>
    public abstract class ItemData : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("物品名称")]
        public string itemName;
        
        [Tooltip("物品图标")]
        public Sprite icon;
        
        [Tooltip("物品描述")]
        [TextArea(3, 5)]
        public string description;
        
        [Header("通用属性")]
        [Tooltip("价值（金币）")]
        public int value;
        
        [Tooltip("抽取权重（用于随机抽取）")]
        public float weight = 1.0f;
        
        [Header("分类标识")]
        [Tooltip("物品大类")]
        public ItemCategory category;
        
        [Header("使用限制")]
        [Tooltip("允许使用此卡牌的行动场合")]
        public CardUseContext allowedUseContext = CardUseContext.All;
        
        /// <summary>
        /// 获取物品信息字符串（用于UI显示和调试）
        /// </summary>
        public abstract string GetItemInfo();
        
        /// <summary>
        /// 获取物品简要信息（用于列表显示）
        /// </summary>
        public virtual string GetBriefInfo()
        {
            return $"{itemName} ({category})";
        }
        
        /// <summary>
        /// 检查此物品是否可以被"使用"。默认返回 false，子类按需 override。
        /// </summary>
        /// <param name="reason">不可用时的原因文本</param>
        public virtual bool CanUse(out string reason)
        {
            reason = "此物品无法使用";
            return false;
        }
        
        /// <summary>
        /// 共享的"使用"前检查逻辑：阶段 → 场合 → OnUse 效果存在性 → 逐效果 CanExecute。
        /// 供 FishData / TrashData / ConsumableData 的 CanUse override 调用。
        /// </summary>
        protected bool CheckUseEffects(List<EffectBase> effects, out string reason)
        {
            if (DayManager.Instance == null || DayManager.Instance.CurrentPhase != GamePhase.Action)
            {
                reason = "当前阶段无法使用卡牌";
                return false;
            }

            var action = DayManager.Instance.CurrentAction;
            CardUseContext current = action == DayAction.Fishing
                ? CardUseContext.Fishing
                : CardUseContext.Shopping;

            if ((allowedUseContext & current) == 0)
            {
                reason = action == DayAction.Fishing
                    ? "此卡牌无法在钓鱼时使用"
                    : "此卡牌无法在商店中使用";
                return false;
            }

            var useEffects = effects?.Where(e => e != null && e.trigger == EffectTrigger.OnUse).ToList();
            if (useEffects == null || useEffects.Count == 0)
            {
                reason = "没有可用效果";
                return false;
            }

            var context = new EffectContext { Target = GameObject.Find("player") };
            foreach (var effect in useEffects)
            {
                var result = effect.CanExecute(context);
                if (!result.canUse)
                {
                    reason = result.reason ?? "效果条件不满足";
                    return false;
                }
            }

            reason = null;
            return true;
        }
    }
}
