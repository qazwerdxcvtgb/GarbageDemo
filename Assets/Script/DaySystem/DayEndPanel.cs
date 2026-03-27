/// <summary>
/// 日终结算面板
/// 每天结束（D1-D5）时弹出的过渡面板
/// 当前仅包含关闭按钮，内容区预留给未来信息展示
/// 创建日期：2026-04-02
/// </summary>

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DaySystem
{
    /// <summary>
    /// 日终结算面板
    /// D6不会显示此面板（直接进入GameOver）
    /// </summary>
    public class DayEndPanel : MonoBehaviour
    {
        #region Inspector Fields

        [Header("面板根节点")]
        [SerializeField] private GameObject panelRoot;

        [Header("按钮")]
        [SerializeField] private Button closeButton;

        [Header("信息显示（预留）")]
        [Tooltip("显示当天星期信息")]
        [SerializeField] private TextMeshProUGUI dayInfoText;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        #endregion

        #region Public API

        /// <summary>
        /// 显示日终结算面板
        /// </summary>
        /// <param name="currentDay">当前结束的天数（1-6）</param>
        public void Show(int currentDay)
        {
            if (dayInfoText != null)
                dayInfoText.text = DayManager.GetDayName(currentDay) + " 结束";

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

        private void OnCloseClicked()
        {
            Hide();
            if (DayManager.Instance != null)
                DayManager.Instance.OnDayEndPanelClosed();
        }

        #endregion
    }
}
