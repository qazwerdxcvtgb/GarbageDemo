using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ItemSystem;

namespace FishCardSystem
{
    /// <summary>
    /// 装备卡牌正面信息显示模块
    /// UI 字段结构与 ConsumableCardFrontDisplay 一致，复用相同 Prefab 布局。
    /// </summary>
    public class EquipmentCardFrontDisplay : MonoBehaviour
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
        public void UpdateDisplay(EquipmentData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[EquipmentCardFrontDisplay] EquipmentData 为空，无法更新显示");
                return;
            }

            if (nameText != null)
                nameText.text = data.itemName;

            if (typeText != null)
                typeText.text = $"装备 · {data.slot.ToChineseText()}";

            if (valueText != null)
                valueText.text = data.value.ToString();

            if (effectsText != null)
                effectsText.text = data.description ?? "";

            if (backgroundImage != null && data.icon != null)
                backgroundImage.sprite = data.icon;
        }

    }
}
