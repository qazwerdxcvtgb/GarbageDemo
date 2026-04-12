using System.Linq;
using UnityEngine;
using HandSystem;

namespace ItemSystem
{
    /// <summary>
    /// 手牌条件免费捕获的检查维度
    /// </summary>
    public enum FreeCaptureCondition
    {
        [InspectorName("按体积")] BySize,
        [InspectorName("按名称包含")] ByNameContains,
    }

    /// <summary>
    /// 揭示时触发：启动 OnHandChanged → OnFishingModifierChanged 桥接。
    /// 条件始终在鱼卡数据上，ProcessFishingCost 通过 CheckHandCondition 实时评估。
    /// 放弃后再次打开面板仍然生效（条件不随面板关闭清除）。
    /// </summary>
    [System.Serializable]
    public class Effect_FreeCaptureByHand : EffectBase
    {
        [Header("条件配置")]
        [Tooltip("检查维度")]
        public FreeCaptureCondition conditionType = FreeCaptureCondition.BySize;

        [Tooltip("目标鱼体积（仅 BySize 时生效）")]
        public FishSize targetSize = FishSize.Large;

        [Tooltip("名称关键字（仅 ByNameContains 时生效）")]
        public string nameKeyword = "";

        public override string DisplayName => "手牌条件免费捕获";

        public override void Execute(EffectContext context)
        {
            EffectBus.Instance.RegisterFreeCaptureSource();
            Debug.Log($"[Effect_FreeCaptureByHand] 桥接已注册，条件={conditionType}");
        }

        /// <summary>
        /// 检查当前手牌是否满足免费捕获条件。
        /// 由 EffectBus.ProcessFishingCost 在每次费用计算时直接调用。
        /// </summary>
        public bool CheckHandCondition()
        {
            if (HandManager.Instance == null) return false;

            foreach (var card in HandManager.Instance.GetHandCards())
            {
                if (card is FishData fish && MatchesCondition(fish))
                    return true;
            }
            return false;
        }

        private bool MatchesCondition(FishData fish)
        {
            switch (conditionType)
            {
                case FreeCaptureCondition.BySize:
                    return fish.size == targetSize;
                case FreeCaptureCondition.ByNameContains:
                    return !string.IsNullOrEmpty(nameKeyword) && fish.itemName.Contains(nameKeyword);
                default:
                    return false;
            }
        }

        public override string GetDescription()
        {
            switch (conditionType)
            {
                case FreeCaptureCondition.BySize:
                    return $"手牌中有{targetSize.ToChineseText()}鱼时免费捕获";
                case FreeCaptureCondition.ByNameContains:
                    return $"手牌中有名称含\"{nameKeyword}\"的鱼时免费捕获";
                default:
                    return "手牌条件免费捕获";
            }
        }
    }
}
