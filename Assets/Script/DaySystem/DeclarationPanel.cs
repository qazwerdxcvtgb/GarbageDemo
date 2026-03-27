/// <summary>
/// 声明阶段选择面板
/// 每日刷新后显示，玩家选择当天去商店还是去钓鱼
/// 创建日期：2026-04-02
/// </summary>

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DaySystem
{
    /// <summary>
    /// 声明阶段选择面板
    /// 全屏遮罩 + 两个按钮（去商店 / 去钓鱼）
    /// </summary>
    public class DeclarationPanel : MonoBehaviour
    {
        #region Inspector Fields

        [Header("面板根节点")]
        [SerializeField] private GameObject panelRoot;

        [Header("按钮")]
        [SerializeField] private Button shopButton;
        [SerializeField] private Button fishingButton;

        [Header("信息显示")]
        [Tooltip("显示当天星期信息")]
        [SerializeField] private TextMeshProUGUI dayInfoText;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            if (shopButton != null)
                shopButton.onClick.AddListener(OnShopClicked);
            if (fishingButton != null)
                fishingButton.onClick.AddListener(OnFishingClicked);
        }

        private void OnDestroy()
        {
            if (shopButton != null)
                shopButton.onClick.RemoveListener(OnShopClicked);
            if (fishingButton != null)
                fishingButton.onClick.RemoveListener(OnFishingClicked);
        }

        #endregion

        #region Public API

        /// <summary>
        /// 显示声明选择面板
        /// </summary>
        /// <param name="currentDay">当前天数（1-6）</param>
        public void Show(int currentDay)
        {
            if (dayInfoText != null)
                dayInfoText.text = DayManager.GetDayName(currentDay);

            if (panelRoot != null)
                panelRoot.SetActive(true);
        }

        /// <summary>
        /// 隐藏面板
        /// </summary>
        public void Hide()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        #endregion

        #region Button Handlers

        private void OnShopClicked()
        {
            Hide();
            if (DayManager.Instance != null)
                DayManager.Instance.OnDeclarationChoice(DayAction.Shopping);
        }

        private void OnFishingClicked()
        {
            Hide();
            if (DayManager.Instance != null)
                DayManager.Instance.OnDeclarationChoice(DayAction.Fishing);
        }

        #endregion
    }
}
