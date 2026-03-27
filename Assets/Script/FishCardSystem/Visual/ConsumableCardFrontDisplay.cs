using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ItemSystem;

namespace FishCardSystem
{
    /// <summary>
    /// 消耗品卡牌正面信息显示模块
    /// </summary>
    public class ConsumableCardFrontDisplay : MonoBehaviour
    {
        [Header("UI组件引用")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI typeText;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private TextMeshProUGUI effectsText;

        /// <summary>
        /// 更新所有显示内容
        /// </summary>
        public void UpdateDisplay(ConsumableData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[ConsumableCardFrontDisplay] ConsumableData 为空，无法更新显示");
                return;
            }

            if (nameText != null)
                nameText.text = data.itemName;

            if (typeText != null)
                typeText.text = "消耗品";

            if (valueText != null)
                valueText.text = data.value.ToString();

            if (effectsText != null)
                effectsText.text = GenerateEffectDescription(data.effects);

            if (backgroundImage != null && data.icon != null)
                backgroundImage.sprite = data.icon;
        }

        /// <summary>
        /// 生成 OnUse 效果描述文本
        /// </summary>
        private string GenerateEffectDescription(List<EffectBase> effects)
        {
            if (effects == null || effects.Count == 0)
                return "无效果";

            var useEffects = effects
                .Where(e => e != null && e.trigger == EffectTrigger.OnUse)
                .Select(e => e.GetFullDescription())
                .ToList();

            return useEffects.Count > 0 ? string.Join("\n", useEffects) : "无效果";
        }
    }
}
