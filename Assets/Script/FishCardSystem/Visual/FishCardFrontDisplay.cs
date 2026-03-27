using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using ItemSystem;
using System.Collections;

namespace FishCardSystem
{
    /// <summary>
    /// 鱼类卡牌正面信息显示模块
    /// </summary>
    public class FishCardFrontDisplay : MonoBehaviour
    {
        [Header("UI组件引用")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image fishIcon;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private TextMeshProUGUI depthText;
        [SerializeField] private TextMeshProUGUI staminaCostText;
        [SerializeField] private TextMeshProUGUI typeText;
        [SerializeField] private TextMeshProUGUI sizeText;
        [SerializeField] private TextMeshProUGUI effectsText;

        [Header("效果修改显示")]
        [Tooltip("被动效果修改体力消耗时的文字颜色")]
        [SerializeField] private Color modifiedCostColor = Color.green;

        private Color normalCostColor;
        private FishData currentData;
        private bool effectDisplayEnabled = false;

        private void Awake()
        {
            if (staminaCostText != null)
                normalCostColor = staminaCostText.color;
        }

        private void OnDestroy()
        {
            if (effectDisplayEnabled && EffectBus.Instance != null)
                EffectBus.Instance.OnFishingModifierChanged -= RefreshStaminaDisplay;
        }

        public void EnableEffectDisplay()
        {
            if (effectDisplayEnabled) return;
            effectDisplayEnabled = true;
            if (EffectBus.Instance != null)
                EffectBus.Instance.OnFishingModifierChanged += RefreshStaminaDisplay;
            if (currentData != null) UpdateStaminaCostDisplay(currentData.staminaCost);
        }

        private void RefreshStaminaDisplay()
        {
            if (currentData != null) UpdateStaminaCostDisplay(currentData.staminaCost);
        }

        /// <summary>
        /// 更新所有显示内容
        /// </summary>
        public void UpdateDisplay(FishData data)
        {
            if (data == null)
            {
                Debug.LogWarning("FishCardFrontDisplay: FishData为空，无法更新显示");
                return;
            }

            // 更新文本
            if (nameText != null)
                nameText.text = data.itemName;

            if (valueText != null)
                valueText.text = data.value.ToString();

            if (depthText != null)
                depthText.text = GetDepthText(data.depth);

            currentData = data;
            UpdateStaminaCostDisplay(data.staminaCost);

            if (typeText != null)
                typeText.text = GetTypeText(data.fishType);

            if (sizeText != null)
                sizeText.text = GetSizeText(data.size);

            if (effectsText != null)
                effectsText.text = GenerateEffectDescription(data.effects);

            // 更新图标
            if (fishIcon != null && data.icon != null)
            {
                fishIcon.sprite = data.icon;
                fishIcon.enabled = true;
            }
            else if (fishIcon != null)
            {
                fishIcon.enabled = false;
            }
        }

        private void UpdateStaminaCostDisplay(int baseCost)
        {
            if (staminaCostText == null) return;
            int displayCost = effectDisplayEnabled && EffectBus.Instance != null
                ? EffectBus.Instance.ProcessFishingCost(baseCost)
                : baseCost;
            bool isModified = displayCost != baseCost;
            staminaCostText.text  = displayCost.ToString();
            staminaCostText.color = isModified ? modifiedCostColor : normalCostColor;
        }

        /// <summary>
        /// 获取深度文本
        /// </summary>
        private string GetDepthText(FishDepth depth)
        {
            switch (depth)
            {
                case FishDepth.Depth1: return "浅水层";
                case FishDepth.Depth2: return "中层";
                case FishDepth.Depth3: return "深水层";
                default: return depth.ToString();
            }
        }

        /// <summary>
        /// 获取类型文本
        /// </summary>
        private string GetTypeText(FishType type)
        {
            switch (type)
            {
                case FishType.Pure: return "纯净";
                case FishType.Corrupted: return "污秽";
                default: return type.ToString();
            }
        }

        /// <summary>
        /// 获取尺寸文本
        /// </summary>
        private string GetSizeText(FishSize size)
        {
            switch (size)
            {
                case FishSize.Small: return "小型";
                case FishSize.Medium: return "中型";
                case FishSize.Large: return "大型";
                default: return size.ToString();
            }
        }

        /// <summary>
        /// 生成效果描述文本
        /// </summary>
        private string GenerateEffectDescription(List<EffectBase> effects)
        {
            if (effects == null || effects.Count == 0)
                return "无效果";

            List<string> descriptions = new List<string>();
            foreach (var effect in effects)
            {
                if (effect != null)
                {
                    descriptions.Add(effect.GetFullDescription());
                }
            }

            return string.Join("\n", descriptions);
        }
    }
}
