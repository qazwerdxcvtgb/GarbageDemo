/// <summary>
/// 游戏结束面板
/// D6结束后显示，提供重新开始按钮
/// 创建日期：2026-04-02
/// </summary>

using UnityEngine;
using UnityEngine.UI;

namespace DaySystem
{
    /// <summary>
    /// 游戏结束面板
    /// 当前仅包含重新开始按钮，内容区预留给未来信息展示
    /// </summary>
    public class GameOverPanel : MonoBehaviour
    {
        #region Inspector Fields

        [Header("面板根节点")]
        [SerializeField] private GameObject panelRoot;

        [Header("按钮")]
        [SerializeField] private Button restartButton;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);
        }

        private void OnDestroy()
        {
            if (restartButton != null)
                restartButton.onClick.RemoveListener(OnRestartClicked);
        }

        #endregion

        #region Public API

        /// <summary>
        /// 显示游戏结束面板
        /// </summary>
        public void Show()
        {
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

        private void OnRestartClicked()
        {
            Hide();
            if (DayManager.Instance != null)
                DayManager.Instance.ResetGame();
        }

        #endregion
    }
}
