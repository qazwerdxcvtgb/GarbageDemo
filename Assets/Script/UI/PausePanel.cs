/// <summary>
/// 暂停页面面板
/// 游戏过程中按 ESC 键呼出，提供重新开始与退出游戏按钮
/// 暂停时冻结 Time.timeScale
/// 创建日期：2026-04-12
/// </summary>

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DaySystem;

namespace UISystem
{
    public class PausePanel : MonoBehaviour
    {
        #region Inspector Fields

        [Header("面板根节点")]
        [SerializeField] private GameObject panelRoot;

        [Header("按钮")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitButton;

        #endregion

        #region 运行时状态

        private bool isPaused;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                TogglePause();
        }

        private void OnDestroy()
        {
            if (restartButton != null)
                restartButton.onClick.RemoveListener(OnRestartClicked);

            if (quitButton != null)
                quitButton.onClick.RemoveListener(OnQuitClicked);
        }

        #endregion

        #region Public API

        public void Show()
        {
            if (panelRoot != null)
                panelRoot.SetActive(true);

            Time.timeScale = 0f;
            isPaused = true;
        }

        public void Hide()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            Time.timeScale = 1f;
            isPaused = false;
        }

        #endregion

        #region 暂停切换

        private void TogglePause()
        {
            if (isPaused)
            {
                Hide();
                return;
            }

            if (!CanPause())
                return;

            Show();
        }

        /// <summary>
        /// 仅在 Declaration 或 Action 阶段允许暂停，避免在过场/结算中暂停导致异常
        /// </summary>
        private bool CanPause()
        {
            if (DayManager.Instance == null)
                return false;

            var phase = DayManager.Instance.CurrentPhase;
            return phase == GamePhase.Declaration || phase == GamePhase.Action;
        }

        #endregion

        #region Button Handlers

        private void OnRestartClicked()
        {
            Hide();
            if (DayManager.Instance != null)
                DayManager.Instance.ResetGame();
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion
    }
}
