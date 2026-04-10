/// <summary>
/// 天数管理器（游戏循环核心）
/// 单例模式，DontDestroyOnLoad
/// 负责管理游戏天数（D1-D6）、阶段流转、每日结算、游戏重置
/// 创建日期：2026-04-02
/// </summary>

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using ItemSystem;
using HandSystem;
using FishingSystem;
using ShopSystem;

namespace DaySystem
{
    /// <summary>
    /// 玩家在声明阶段的行动选择
    /// </summary>
    public enum DayAction
    {
        Fishing,
        Shopping
    }

    /// <summary>
    /// 天数管理器
    /// 驱动整个游戏的每日循环：Start → Refresh → Declaration → Action → DayEnd
    /// </summary>
    public class DayManager : MonoBehaviour
    {
        #region 单例

        public static DayManager Instance { get; private set; }

        #endregion

        #region 常量

        public const int MAX_DAY = 6;
        private static readonly string[] DayNames = { "", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六" };

        #endregion

        #region Inspector 引用

        [Header("玩家状态")]
        [SerializeField] private CharacterState playerState;

        [Header("面板引用")]
        [SerializeField] private DeclarationPanel declarationPanel;
        [SerializeField] private DayEndPanel dayEndPanel;
        [SerializeField] private GameOverPanel gameOverPanel;

        [Header("下一天按钮")]
        [Tooltip("在 Editor 中绑定按钮的 OnClick 到 OnNextDayClicked()")]
        [SerializeField] private Button nextDayButton;
        [SerializeField] private TextMeshProUGUI nextDayButtonText;

        [Header("调试")]
        [SerializeField] private bool showDebugInfo = true;

        #endregion

        #region 运行时状态

        [Header("运行时状态（只读调试）")]
        [SerializeField] private int currentDay = 0;
        [SerializeField] private GamePhase currentPhase;
        [SerializeField] private DayAction currentAction;

        #endregion

        #region 属性

        /// <summary>当前天数（1-6）</summary>
        public int CurrentDay => currentDay;

        /// <summary>当前游戏阶段</summary>
        public GamePhase CurrentPhase => currentPhase;

        /// <summary>当前行动选择</summary>
        public DayAction CurrentAction => currentAction;

        /// <summary>获取天数对应的中文名称</summary>
        public static string GetDayName(int day)
        {
            if (day < 1 || day > MAX_DAY) return "";
            return DayNames[day];
        }

        #endregion

        #region 事件

        /// <summary>天数变化事件（参数：新的天数 1-6）</summary>
        public event Action<int> OnDayChanged;

        /// <summary>游戏阶段变化事件（参数：新的阶段）</summary>
        public event Action<GamePhase> OnPhaseChanged;

        #endregion

        #region Unity 生命周期

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // 场景重载后，将新场景中的引用移交给持久实例，再销毁自身
                Instance.ApplySceneReferences(
                    declarationPanel, dayEndPanel, gameOverPanel,
                    nextDayButton, nextDayButtonText, playerState);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 场景重载后，由新场景的 DayManager 副本调用，将新场景中的所有引用更新到持久实例
        /// </summary>
        private void ApplySceneReferences(
            DeclarationPanel dp, DayEndPanel dep, GameOverPanel gop,
            Button btn, TextMeshProUGUI btnText, CharacterState state)
        {
            declarationPanel = dp;
            dayEndPanel = dep;
            gameOverPanel = gop;
            playerState = state;

            // 从旧按钮移除监听，绑定到新按钮
            if (nextDayButton != null)
                nextDayButton.onClick.RemoveListener(OnNextDayClicked);
            nextDayButton = btn;
            nextDayButtonText = btnText;
            if (nextDayButton != null)
                nextDayButton.onClick.AddListener(OnNextDayClicked);

            if (showDebugInfo)
                Debug.Log("[DayManager] 场景引用已更新（场景重载后移交）");
        }

        private void Start()
        {
            if (nextDayButton != null)
                nextDayButton.onClick.AddListener(OnNextDayClicked);

            StartGame();
        }

        private void OnDestroy()
        {
            if (nextDayButton != null)
                nextDayButton.onClick.RemoveListener(OnNextDayClicked);

            if (Instance == this)
                Instance = null;
        }

        #endregion

        #region 游戏启动

        /// <summary>
        /// 开始新游戏（从D1开始）
        /// D1跳过Start阶段，直接进入Refresh → Declaration
        /// </summary>
        public void StartGame()
        {
            currentDay = 1;
            OnDayChanged?.Invoke(currentDay);

            if (showDebugInfo)
                Debug.Log($"[DayManager] 游戏开始 - {GetDayName(currentDay)}");

            SetNextDayButtonVisible(false);
            ExecuteRefreshPhase();
        }

        #endregion

        #region 下一天按钮

        /// <summary>
        /// 下一天按钮的点击回调（public，在 Editor 中绑定 Button.OnClick）
        /// D1-D5：关闭商店 → 显示日终面板
        /// D6：关闭商店 → 显示游戏结束面板
        /// </summary>
        public void OnNextDayClicked()
        {
            if (currentPhase != GamePhase.Action)
            {
                if (showDebugInfo)
                    Debug.LogWarning($"[DayManager] 当前阶段 {currentPhase} 不允许点击下一天按钮");
                return;
            }

            // 关闭商店（如果正在商店中）
            if (currentAction == DayAction.Shopping && ShopPanel.Instance != null)
                ShopPanel.Instance.ClosePanel();

            SetNextDayButtonVisible(false);

            if (currentDay >= MAX_DAY)
            {
                EnterGameOver();
            }
            else
            {
                EnterDayEnd();
            }
        }

        #endregion

        #region 阶段流转

        /// <summary>
        /// 进入日终结算阶段（D1-D5）
        /// </summary>
        private void EnterDayEnd()
        {
            SetPhase(GamePhase.DayEnd);

            if (dayEndPanel != null)
                dayEndPanel.Show(currentDay);

            if (showDebugInfo)
                Debug.Log($"[DayManager] 进入日终结算 - {GetDayName(currentDay)}");
        }

        /// <summary>
        /// 日终面板关闭回调，由 DayEndPanel 调用
        /// 关闭后执行下一天的 Start 阶段
        /// </summary>
        public void OnDayEndPanelClosed()
        {
            ExecuteStartPhase();
        }

        /// <summary>
        /// 执行每日开始阶段（D2-D6）
        /// 顺序：天数+1 → 丢弃揭示牌 → 每日效果 → 深度回退 → 进入Refresh
        /// </summary>
        private void ExecuteStartPhase()
        {
            SetPhase(GamePhase.DayStart);

            currentDay++;
            OnDayChanged?.Invoke(currentDay);

            if (showDebugInfo)
                Debug.Log($"[DayManager] ===== {GetDayName(currentDay)} 开始 =====");

            DiscardRevealedCards();
            ApplyDailyEffects();
            ReduceDepth();

            ExecuteRefreshPhase();
        }

        /// <summary>
        /// 执行刷新阶段：恢复体力满值，然后进入声明阶段（D1 自动跳过声明和装备准备）
        /// </summary>
        private void ExecuteRefreshPhase()
        {
            SetPhase(GamePhase.Refresh);

            if (playerState != null)
            {
                playerState.RestoreFullHealth();
                if (showDebugInfo)
                    Debug.Log($"[DayManager] 体力已恢复满值: {playerState.CurrentHealth}/{playerState.MaxHealth}");
            }

            ItemSystem.EffectBus.Instance.NotifyDayRefreshCompleted();

            if (currentDay == 1)
                EnterFishingDirectly();
            else
                EnterDeclarationPhase();
        }

        /// <summary>
        /// D1 专用：跳过声明和装备准备，直接进入钓鱼行动阶段
        /// </summary>
        private void EnterFishingDirectly()
        {
            currentAction = DayAction.Fishing;
            SetPhase(GamePhase.Action);

            UpdateNextDayButtonText();
            SetNextDayButtonVisible(true);

            if (showDebugInfo)
                Debug.Log("[DayManager] D1 自动跳过声明和装备准备，直接进入钓鱼");
        }

        /// <summary>
        /// 进入声明阶段：显示选择面板，隐藏下一天按钮
        /// </summary>
        private void EnterDeclarationPhase()
        {
            SetPhase(GamePhase.Declaration);
            SetNextDayButtonVisible(false);

            if (declarationPanel != null)
                declarationPanel.Show(currentDay);

            if (showDebugInfo)
                Debug.Log($"[DayManager] 进入声明阶段 - 等待玩家选择");
        }

        /// <summary>
        /// 声明选择回调，由 DeclarationPanel 调用
        /// </summary>
        /// <param name="action">玩家选择的行动</param>
        public void OnDeclarationChoice(DayAction action)
        {
            currentAction = action;
            SetPhase(GamePhase.Action);

            UpdateNextDayButtonText();
            SetNextDayButtonVisible(true);

            if (action == DayAction.Shopping)
            {
                if (ShopPanel.Instance != null)
                    ShopPanel.Instance.OpenPanel();

                if (showDebugInfo)
                    Debug.Log($"[DayManager] 玩家选择去商店");
            }
            else
            {
                if (EquipmentPanel.Instance != null)
                    EquipmentPanel.Instance.OpenPanelForFishing();

                if (showDebugInfo)
                    Debug.Log($"[DayManager] 玩家选择去钓鱼");
            }
        }

        /// <summary>
        /// 进入游戏结束
        /// </summary>
        private void EnterGameOver()
        {
            SetPhase(GamePhase.GameOver);

            if (gameOverPanel != null)
                gameOverPanel.Show();

            if (showDebugInfo)
                Debug.Log($"[DayManager] ===== 游戏结束 =====");
        }

        #endregion

        #region 每日结算逻辑

        /// <summary>
        /// 丢弃钓鱼桌上所有已揭示（FaceUp）的牌堆顶牌
        /// </summary>
        private void DiscardRevealedCards()
        {
            if (FishingTableManager.Instance == null) return;

            var piles = FishingTableManager.Instance.GetAllPiles();
            int discardCount = 0;

            foreach (var pile in piles)
            {
                if (pile != null && pile.State == PileState.FaceUp)
                {
                    var discarded = pile.RemoveTopCard();
                    if (discarded != null)
                    {
                        discardCount++;
                        if (showDebugInfo)
                            Debug.Log($"[DayManager] 丢弃揭示牌: {discarded.itemName}");
                    }
                }
            }

            if (showDebugInfo)
                Debug.Log($"[DayManager] 共丢弃 {discardCount} 张揭示牌");
        }

        /// <summary>
        /// 根据当天天数应用每日效果
        /// D3/D5: +1张杂鱼牌  D4: +3金币  D6: 体力上限+3
        /// </summary>
        private void ApplyDailyEffects()
        {
            switch (currentDay)
            {
                case 3:
                case 5:
                    AddTrashCardToHand();
                    break;
                case 4:
                    if (playerState != null)
                    {
                        playerState.ModifyGold(3);
                        if (showDebugInfo)
                            Debug.Log($"[DayManager] D{currentDay} 每日效果: +3 金币");
                    }
                    break;
                case 6:
                    if (playerState != null)
                    {
                        playerState.ModifyMaxHealth(3);
                        if (showDebugInfo)
                            Debug.Log($"[DayManager] D{currentDay} 每日效果: 体力上限 +3");
                    }
                    break;
            }
        }

        /// <summary>
        /// 从杂鱼牌库抽取一张加入手牌
        /// </summary>
        private void AddTrashCardToHand()
        {
            if (ShopManager.Instance == null || HandManager.Instance == null) return;

            TrashData trash = ShopManager.Instance.DrawTrash();
            if (trash != null)
            {
                HandManager.Instance.AddCard(trash);
                if (showDebugInfo)
                    Debug.Log($"[DayManager] D{currentDay} 每日效果: 获得杂鱼牌 {trash.itemName}");
            }
            else
            {
                if (showDebugInfo)
                    Debug.Log($"[DayManager] D{currentDay} 每日效果: 杂鱼牌库为空，未获得卡牌");
            }
        }

        /// <summary>
        /// 深度回退：Depth3→Depth2, Depth2→Depth1, Depth1不变
        /// </summary>
        private void ReduceDepth()
        {
            if (playerState == null) return;

            FishDepth current = playerState.CurrentDepth;

            if (current == FishDepth.Depth3)
            {
                playerState.SetDepth(FishDepth.Depth2);
                if (showDebugInfo)
                    Debug.Log($"[DayManager] 深度回退: Depth3 → Depth2");
            }
            else if (current == FishDepth.Depth2)
            {
                playerState.SetDepth(FishDepth.Depth1);
                if (showDebugInfo)
                    Debug.Log($"[DayManager] 深度回退: Depth2 → Depth1");
            }
            else
            {
                if (showDebugInfo)
                    Debug.Log($"[DayManager] 深度已在最浅层，不变");
            }
        }

        #endregion

        #region 游戏重置

        /// <summary>
        /// 重置整局游戏并重新开始
        /// 由 GameOverPanel 的重新开始按钮调用
        /// </summary>
        public void ResetGame()
        {
            if (showDebugInfo)
                Debug.Log("[DayManager] ===== 重置游戏 =====");

            // 1. 重新洗牌所有卡池
            if (ItemPool.Instance != null)
                ItemPool.Instance.ReshuffleAll();

            // 2. 重置全局状态
            if (GameManager.Instance != null)
                GameManager.Instance.ResetAll();

            // 3. 清空手牌
            if (HandManager.Instance != null)
                HandManager.Instance.ClearHand();

            // 4. 重置商店
            if (ShopManager.Instance != null)
                ShopManager.Instance.ResetPools();

            // 5. 重置角色属性
            if (playerState != null)
                playerState.ResetState();

            // 6. 重载场景（重建 FishingTableManager、EquipmentManager、EffectBus 等）
            SceneManager.sceneLoaded += OnSceneLoadedForReset;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnSceneLoadedForReset(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnSceneLoadedForReset;

            // 场景加载完成后重新开始 D1
            // 延迟一帧确保所有 Awake/Start 已执行
            StartCoroutine(StartGameNextFrame());
        }

        private System.Collections.IEnumerator StartGameNextFrame()
        {
            yield return null;
            StartGame();
        }

        #endregion

        #region UI 辅助

        private void SetPhase(GamePhase phase)
        {
            currentPhase = phase;
            OnPhaseChanged?.Invoke(phase);
        }

        private void SetNextDayButtonVisible(bool visible)
        {
            if (nextDayButton != null)
                nextDayButton.gameObject.SetActive(visible);
        }

        private void UpdateNextDayButtonText()
        {
            if (nextDayButtonText == null) return;
            nextDayButtonText.text = currentDay >= MAX_DAY ? "结束" : "下一天";
        }

        #endregion
    }
}
