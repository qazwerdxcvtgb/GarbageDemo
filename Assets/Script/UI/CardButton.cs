/// <summary>
/// 卡片按钮组件
/// 显示卡牌信息并处理选中状态
/// 创建日期：2026-01-20
/// 更新日期：2026-01-26（重构为ItemSystem）
/// </summary>

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ItemSystem;

namespace UISystem
{
    /// <summary>
    /// 卡片按钮组件
    /// 用于在UI中显示卡牌信息并处理选中逻辑
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class CardButton : MonoBehaviour
    {
        /// <summary>
        /// 卡片显示模式
        /// </summary>
        public enum DisplayMode
        {

            Brief,    // 简略模式（仅显示体积）
            Detailed, // 详细模式（显示所有信息）
            Empty     // 空牌模式（显示"空白"）
        }

        #region UI组件引用

        [Header("UI组件")]
        [Tooltip("卡牌信息文本组件")]
        public TextMeshProUGUI cardInfoText;

        [Tooltip("按钮组件")]
        private Button button;

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
        /// 所属的卡片选择面板
        /// </summary>
        private CardSelectionPanel parentPanel;

        /// <summary>
        /// 当前显示模式
        /// </summary>
        private DisplayMode displayMode = DisplayMode.Brief;

        /// <summary>
        /// 是否为空牌
        /// </summary>
        private bool isEmptyCard = false;

        /// <summary>
        /// 背景图片组件（用于设置半透明）
        /// </summary>
        private Image backgroundImage;

        #endregion

        #region 初始化

        private void Awake()
        {
            button = GetComponent<Button>();
            backgroundImage = GetComponent<Image>();
            
            // 如果没有指定Outline组件，尝试获取
            if (outline == null)
            {
                outline = GetComponent<Outline>();
            }

            // 添加点击事件监听
            button.onClick.AddListener(OnButtonClicked);
        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 设置卡片数据并更新显示
        /// </summary>
        /// <param name="item">物品数据</param>
        /// <param name="panel">所属的选择面板</param>
        /// <param name="mode">显示模式（默认为简略模式）</param>
        public void SetCardData(ItemData item, CardSelectionPanel panel, DisplayMode mode = DisplayMode.Brief)
        {
            itemData = item;
            parentPanel = panel;
            displayMode = mode;
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

        /// <summary>
        /// 设置显示模式
        /// </summary>
        /// <param name="mode">显示模式</param>
        public void SetDisplayMode(DisplayMode mode)
        {
            displayMode = mode;
            UpdateDisplay();
        }

        /// <summary>
        /// 设置为空牌
        /// </summary>
        /// <param name="panel">所属的选择面板</param>
        public void SetAsEmptyCard(CardSelectionPanel panel)
        {
            itemData = null;
            parentPanel = panel;
            isEmptyCard = true;
            displayMode = DisplayMode.Empty;
            
            // 禁用按钮交互
            if (button != null)
            {
                button.interactable = false;
            }
            
            // 设置半透明背景
            if (backgroundImage != null)
            {
                Color bgColor = backgroundImage.color;
                bgColor.a = 0.5f; // 半透明
                backgroundImage.color = bgColor;
            }
            
            // 更新显示
            UpdateDisplay();
            
            Debug.Log("[CardButton] 设置为空牌");
        }

        /// <summary>
        /// 检查是否为空牌
        /// </summary>
        /// <returns>如果是空牌返回true，否则返回false</returns>
        public bool IsEmptyCard()
        {
            return isEmptyCard;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 更新卡牌信息显示
        /// </summary>
        private void UpdateDisplay()
        {
            if (cardInfoText == null)
            {
                return;
            }
            
            // 空牌模式：显示"空白"
            if (isEmptyCard)
            {
                cardInfoText.text = "<b>空白</b>";
                return;
            }
            
            if (itemData == null)
            {
                return;
            }

            string displayText;

            // 根据显示模式显示不同内容
            if (displayMode == DisplayMode.Brief)
            {
                // 简略模式：仅显示体积（仅限鱼类）
                if (itemData is FishData fish)
                {
                    displayText = $"<size=24><b>{fish.size.ToChineseText()}</b></size>";
                }
                else
                {
                    displayText = $"<size=24><b>{itemData.itemName}</b></size>";
                }
            }
            else
            {
                // 详细模式：显示所有信息
                displayText = itemData.GetItemInfo();
            }

            cardInfoText.text = displayText;
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
                parentPanel.OnCardButtonClicked(this);
            }
        }

        #endregion
    }
}
