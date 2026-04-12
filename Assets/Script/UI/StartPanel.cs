/// <summary>
/// 开始页面面板
/// 游戏启动时显示，提供开始游戏与退出游戏按钮
/// 创建日期：2026-04-12
/// </summary>

using UnityEngine;
using UnityEngine.UI;
using DaySystem;

namespace UISystem
{
    public class StartPanel : MonoBehaviour
    {
        #region Inspector Fields

        [Header("面板根节点")]
        [SerializeField] private GameObject panelRoot;

        [Header("按钮")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            if (startButton != null)
                startButton.onClick.AddListener(OnStartClicked);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);
        }

        private void OnDestroy()
        {
            if (startButton != null)
                startButton.onClick.RemoveListener(OnStartClicked);

            if (quitButton != null)
                quitButton.onClick.RemoveListener(OnQuitClicked);
        }

        #endregion

        #region Public API

        public void Show()
        {
            if (panelRoot != null)
                panelRoot.SetActive(true);
        }

        public void Hide()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        #endregion

        #region Button Handlers

        private void OnStartClicked()
        {
            Hide();
            if (DayManager.Instance != null)
                DayManager.Instance.StartGame();
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
