/// <summary>
/// 天数显示 UI
/// 订阅 DayManager 的天数变化事件，在界面上显示当前"星期X"
/// 创建日期：2026-04-02
/// </summary>

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DaySystem
{
    /// <summary>
    /// 天数显示 UI 组件
    /// 挂载于 Text 所在的 GameObject 上
    /// </summary>
    public class DayDisplayUI : MonoBehaviour
    {
        #region Inspector Fields

        [Header("显示组件")]
        [SerializeField] private TextMeshProUGUI dayText;

        [Header("格式设置")]
        [Tooltip("显示格式，{0} 会被替换为星期名称")]
        [SerializeField] private string displayFormat = "{0}";

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (DayManager.Instance != null)
            {
                DayManager.Instance.OnDayChanged += OnDayChanged;
                OnDayChanged(DayManager.Instance.CurrentDay);
            }
        }

        private void OnDestroy()
        {
            if (DayManager.Instance != null)
                DayManager.Instance.OnDayChanged -= OnDayChanged;
        }

        #endregion

        #region Event Handlers

        private void OnDayChanged(int day)
        {
            if (dayText != null)
                dayText.text = string.Format(displayFormat, DayManager.GetDayName(day));
        }

        #endregion
    }
}
