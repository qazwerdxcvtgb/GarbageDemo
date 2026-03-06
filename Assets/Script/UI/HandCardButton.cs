/// <summary>
/// 手牌卡片按钮组件
/// 显示手牌信息并处理选中状态
/// 创建日期：2026-01-21
/// 更新日期：2026-01-27（更新为ItemSystem）
/// </summary>

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ItemSystem;

namespace UISystem
{
    /// <summary>
    /// 手牌卡片按钮组件
    /// 用于在手牌列表中显示卡牌信息
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class HandCardButton : MonoBehaviour
    {
        #region UI组件引用

        [Header("UI组件")]
        [Tooltip("卡牌信息文本组件")]
        public TextMeshProUGUI cardInfoText;

        [Tooltip("选中状态边框（Outline组件）")]
        public Outline outline;

        #endregion

        #region 数据

        /// <summary>
        /// 当前显示的物品数据
        /// </summary>
        private ItemData itemData;

        /// <summary>
        /// 是否被选中
        /// </summary>
        private bool isSelected = false;

        /// <summary>
        /// 所属的手牌面板
        /// </summary>
        private HandUIPanel parentPanel;

        /// <summary>
        /// 按钮组件
        /// </summary>
        private Button button;

        #endregion

        #region 初始化

        private void Awake()
        {
            button = GetComponent<Button>();

            // 如果没有指定Outline组件，尝试获取
            if (outline == null)
            {
                outline = GetComponent<Outline>();
            }

            // 添加点击事件监听
            if (button != null)
            {
                button.onClick.AddListener(OnButtonClicked);
            }
        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 设置卡片数据并更新显示
        /// </summary>
        /// <param name="item">物品数据</param>
        /// <param name="panel">所属的手牌面板</param>
        public void SetCardData(ItemData item, HandUIPanel panel)
        {
            itemData = item;
            parentPanel = panel;
            UpdateDisplay();
        }

        /// <summary>
        /// 设置选中状态
        /// </summary>
        /// <param name="selected">是否选中</param>
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            UpdateVisualState();
        }

        /// <summary>
        /// 获取当前物品数据
        /// </summary>
        public ItemData GetCardData()
        {
            return itemData;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 更新卡牌信息显示（根据物品类型显示不同信息）
        /// </summary>
        private void UpdateDisplay()
        {
            if (itemData == null || cardInfoText == null)
            {
                return;
            }

            string displayText = "";

            // 根据物品类型显示不同信息
            if (itemData is FishData fish)
            {
                // 鱼类：显示名称、体积、消耗、类型、价值、使用效果
                displayText = $"<b>{fish.itemName}</b>\n";
                displayText += $"体积: {fish.size.ToChineseText()}\n";
                displayText += $"消耗: {fish.staminaCost}\n";
                displayText += $"类型: {fish.fishType.ToChineseText()}\n";
                displayText += $"价值: {fish.value}\n";
                displayText += $"使用: {GetEffectDescriptions(fish.effects, EffectTrigger.OnUse)}";
            }
            else if (itemData is TrashData trash)
            {
                // 杂鱼：显示名称、价值、使用效果
                displayText = $"<b>{trash.itemName}</b>\n";
                displayText += $"类型: 杂鱼\n";
                displayText += $"价值: {trash.value}\n";
                displayText += $"使用: {GetEffectDescriptions(trash.effects, EffectTrigger.OnUse)}";
            }
            else if (itemData is ConsumableData consumable)
            {
                // 消耗品：显示名称、价值、使用效果
                displayText = $"<b>{consumable.itemName}</b>\n";
                displayText += $"类型: 消耗品\n";
                displayText += $"价值: {consumable.value}\n";
                displayText += $"使用: {GetEffectDescriptions(consumable.effects, EffectTrigger.OnUse)}";
            }
            else if (itemData is EquipmentData equipment)
            {
                // 装备：显示名称、槽位、被动效果
                displayText = $"<b>{equipment.itemName}</b>\n";
                displayText += $"类型: 装备\n";
                displayText += $"槽位: {equipment.slot.ToChineseText()}\n";
                displayText += $"价值: {equipment.value}\n";
                displayText += $"被动: {GetPassiveEffectNames(equipment.passiveEffects)}";
            }
            else
            {
                // 通用显示
                displayText = itemData.GetItemInfo();
            }

            cardInfoText.text = displayText;
        }

        /// <summary>
        /// 获取指定触发时机的效果描述（用于新的 EffectBase 系统）
        /// </summary>
        private string GetEffectDescriptions(System.Collections.Generic.List<EffectBase> effects, EffectTrigger trigger)
        {
            if (effects == null || effects.Count == 0)
            {
                return "无";
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            bool first = true;

            foreach (var effect in effects)
            {
                if (effect != null && effect.trigger == trigger)
                {
                    if (!first) sb.Append(", ");
                    sb.Append(effect.GetDescription());
                    first = false;
                }
            }

            return sb.Length > 0 ? sb.ToString() : "无";
        }

        /// <summary>
        /// 获取被动效果名称列表
        /// </summary>
        private string GetPassiveEffectNames(System.Collections.Generic.List<PassiveEffect> effects)
        {
            if (effects == null || effects.Count == 0)
            {
                return "无";
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i] != null)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(effects[i].effectName);
                }
            }

            return sb.Length > 0 ? sb.ToString() : "无";
        }

        /// <summary>
        /// 更新选中状态的视觉效果
        /// </summary>
        private void UpdateVisualState()
        {
            if (outline != null)
            {
                outline.enabled = isSelected;

                if (isSelected)
                {
                    // 选中时使用高亮颜色（黄色）
                    outline.effectColor = Color.yellow;
                    outline.effectDistance = new Vector2(3, 3);
                }
            }
        }

        /// <summary>
        /// 按钮点击事件处理
        /// </summary>
        private void OnButtonClicked()
        {
            if (parentPanel != null)
            {
                parentPanel.OnHandCardButtonClicked(this);
            }
        }

        #endregion
    }
}
