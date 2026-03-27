/// <summary>
/// 鱼店卡牌槽
/// 创建日期：2026-01-21
/// 更新日期：2026-01-27（更新为ItemSystem）
/// 功能：显示鱼店中的单张鱼类卡牌信息
/// </summary>

using UnityEngine;
using TMPro;
using ItemSystem;

namespace UISystem
{
    /// <summary>
    /// 鱼店卡牌槽组件
    /// </summary>
    [System.Obsolete("此脚本已废弃，不再使用。保留仅供历史参考。")]
    public class FishShopCardSlot : MonoBehaviour
    {
        [Header("UI组件")]
        [Tooltip("卡牌信息文本")]
        public TextMeshProUGUI cardInfoText;

        [Header("数据")]
        private FishData fishData;
        private FishShopPanel shopPanel;

        /// <summary>
        /// 设置鱼类数据并更新显示
        /// </summary>
        /// <param name="fish">鱼类数据</param>
        /// <param name="panel">所属面板引用</param>
        public void SetCardData(FishData fish, FishShopPanel panel)
        {
            if (fish == null)
            {
                Debug.LogError("[FishShopCardSlot] 鱼类数据为空");
                return;
            }

            if (panel == null)
            {
                Debug.LogError("[FishShopCardSlot] 面板引用为空");
                return;
            }

            // 保存引用
            fishData = fish;
            shopPanel = panel;

            // 更新显示
            UpdateDisplay();
        }

        /// <summary>
        /// 更新鱼类信息显示
        /// </summary>
        private void UpdateDisplay()
        {
            if (fishData == null || cardInfoText == null)
            {
                return;
            }

            // 计算售价和调整值
            int originalPrice = fishData.value;
            int sellPrice = FishPriceCalculator.CalculatePrice(fishData);
            int adjustment = FishPriceCalculator.GetAdjustment(fishData);

            // 格式化调整值显示（+2、-2、0）
            string adjustmentText;
            if (adjustment > 0)
            {
                adjustmentText = $"+{adjustment}";
            }
            else if (adjustment < 0)
            {
                adjustmentText = $"{adjustment}";
            }
            else
            {
                adjustmentText = "±0";
            }

            // 格式化显示文本
            string displayText = $"<b>{fishData.itemName}</b>\n";
            displayText += $"体积: {fishData.size.ToChineseText()}\n";
            displayText += $"消耗: {fishData.staminaCost}\n";
            displayText += $"类型: {fishData.fishType.ToChineseText()}\n";
            displayText += $"价值: {originalPrice} → {sellPrice} ({adjustmentText})\n";
            displayText += $"使用: {GetUseEffectsText()}";

            cardInfoText.text = displayText;
        }

        /// <summary>
        /// 获取使用效果文本
        /// </summary>
        private string GetUseEffectsText()
        {
            if (fishData.effects == null || fishData.effects.Count == 0)
            {
                return "无";
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            bool first = true;

            foreach (var effect in fishData.effects)
            {
                if (effect != null && effect.trigger == EffectTrigger.OnUse)
                {
                    if (!first) sb.Append(", ");
                    sb.Append(effect.GetDescription());
                    first = false;
                }
            }

            return sb.Length > 0 ? sb.ToString() : "无";
        }

        /// <summary>
        /// 获取当前鱼类数据
        /// </summary>
        /// <returns>鱼类数据</returns>
        public FishData GetCardData()
        {
            return fishData;
        }
    }
}
