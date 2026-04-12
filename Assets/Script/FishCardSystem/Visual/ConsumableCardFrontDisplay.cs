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
                effectsText.text = data.description ?? "";

            if (backgroundImage != null && data.icon != null)
                backgroundImage.sprite = data.icon;
        }

    }
}
