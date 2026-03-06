/// <summary>
/// 金币显示UI控制器
/// 负责显示玩家金币数量，并控制金币图标的帧动画
/// 创建日期：2026-01-20
/// </summary>

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UISystem
{
    /// <summary>
    /// 金币显示UI控制器
    /// 自动查找玩家CharacterState组件并订阅金币变化事件
    /// 实现数字渐变增加效果和金币图标动画控制
    /// </summary>
    public class GoldDisplayUI : MonoBehaviour
    {
        #region UI组件引用

        [Header("UI组件")]
        [Tooltip("金币文字显示组件")]
        public TextMeshProUGUI goldText;

        [Tooltip("金币图标动画控制器")]
        public Animator coinAnimator;

        #endregion

        #region 动画设置

        [Header("动画设置")]
        [Tooltip("金币动画Bool参数名（在Animator中创建的Bool参数名）")]
        public string coinAnimParamName = "isPlayingCoinAnim";

        [Tooltip("变化频率（次/秒），例如：10表示每秒变化10次，间隔0.1秒")]
        [Range(1f, 60f)]
        public float changeFrequency = 10f;

        #endregion

        #region 私有字段

        /// <summary>
        /// 玩家状态组件引用
        /// </summary>
        private CharacterState playerState;

        /// <summary>
        /// 当前显示的金币数量
        /// </summary>
        private int displayedGold = 0;

        /// <summary>
        /// 金币变化协程引用
        /// </summary>
        private Coroutine goldChangeCoroutine;

        #endregion

        #region Unity生命周期

        /// <summary>
        /// 初始化
        /// 查找玩家对象并订阅金币变化事件
        /// </summary>
        private void Start()
        {
            // 查找场景中的player对象
            GameObject playerObject = GameObject.Find("player");

            if (playerObject == null)
            {
                Debug.LogError("[GoldDisplayUI] 未找到名为'player'的GameObject！请检查场景中玩家对象的名称。");
                return;
            }

            // 获取CharacterState组件
            playerState = playerObject.GetComponent<CharacterState>();

            if (playerState == null)
            {
                Debug.LogError("[GoldDisplayUI] player对象上未找到CharacterState组件！");
                return;
            }

            // 检查UI组件引用
            if (goldText == null)
            {
                Debug.LogError("[GoldDisplayUI] goldText未分配！请在Inspector中拖拽TextMeshProUGUI组件。");
                return;
            }

            if (coinAnimator == null)
            {
                Debug.LogWarning("[GoldDisplayUI] coinAnimator未分配！金币图标动画将不会播放。");
            }

            // 订阅金币变化事件
            playerState.OnGoldChanged.AddListener(UpdateGoldUI);

            // 初始化显示
            displayedGold = playerState.GoldAmount;
            goldText.text = displayedGold.ToString();

            Debug.Log("[GoldDisplayUI] 金币显示UI初始化成功");
        }

        /// <summary>
        /// 清理
        /// 取消事件订阅并停止协程，避免内存泄漏
        /// </summary>
        private void OnDestroy()
        {
            // 取消订阅事件
            if (playerState != null)
            {
                playerState.OnGoldChanged.RemoveListener(UpdateGoldUI);
            }

            // 停止正在运行的协程
            if (goldChangeCoroutine != null)
            {
                StopCoroutine(goldChangeCoroutine);
                goldChangeCoroutine = null;
            }

            // 确保动画停止
            if (coinAnimator != null && !string.IsNullOrEmpty(coinAnimParamName))
            {
                coinAnimator.SetBool(coinAnimParamName, false);
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 更新金币显示
        /// 当金币数量变化时被调用
        /// </summary>
        /// <param name="newGold">新的金币数量</param>
        private void UpdateGoldUI(int newGold)
        {
            if (goldText == null)
            {
                return;
            }

            // 开始播放金币图标动画
            if (coinAnimator != null && !string.IsNullOrEmpty(coinAnimParamName))
            {
                coinAnimator.SetBool(coinAnimParamName, true);
            }

            // 如果有正在运行的协程，先停止
            if (goldChangeCoroutine != null)
            {
                StopCoroutine(goldChangeCoroutine);
            }

            // 启动金币数字渐变协程
            goldChangeCoroutine = StartCoroutine(GoldChangeCoroutine(newGold));

            Debug.Log($"[GoldDisplayUI] 金币变化: {displayedGold} → {newGold}");
        }

        /// <summary>
        /// 金币数字渐变协程
        /// 实现数字逐步增加或减少的效果
        /// </summary>
        /// <param name="targetGold">目标金币数量</param>
        private IEnumerator GoldChangeCoroutine(int targetGold)
        {
            int difference = targetGold - displayedGold;

            // 如果没有变化，直接返回
            if (difference == 0)
            {
                yield break;
            }

            // 计算步长（智能优化）
            int stepSize = CalculateStepSize(difference);
            
            // 确定方向（增加或减少）
            int direction = difference > 0 ? 1 : -1;
            
            // 计算每步的时间间隔
            float interval = 1f / changeFrequency;

            // 逐步更新显示的金币数量
            while (displayedGold != targetGold)
            {
                // 计算下一个值
                int nextValue = displayedGold + (stepSize * direction);

                // 防止越界，确保不超过目标值
                if (direction > 0)
                {
                    displayedGold = Mathf.Min(nextValue, targetGold);
                }
                else
                {
                    displayedGold = Mathf.Max(nextValue, targetGold);
                }

                // 更新文本显示
                goldText.text = displayedGold.ToString();

                // 等待间隔时间
                yield return new WaitForSeconds(interval);
            }

            // 确保最终值准确
            displayedGold = targetGold;
            goldText.text = displayedGold.ToString();

            // 数字渐变完成，停止金币动画
            if (coinAnimator != null && !string.IsNullOrEmpty(coinAnimParamName))
            {
                coinAnimator.SetBool(coinAnimParamName, false);
            }

            goldChangeCoroutine = null;
        }

        /// <summary>
        /// 根据金币变化量计算合适的步长
        /// 优化大数值变化时的显示效果
        /// </summary>
        /// <param name="difference">金币变化量（可正可负）</param>
        /// <returns>每步变化的数值</returns>
        private int CalculateStepSize(int difference)
        {
            int absDifference = Mathf.Abs(difference);

            // 根据变化量选择合适的步长
            if (absDifference <= 10)
            {
                return 1;           // ≤10：每次+1（如+7 = 7步）
            }
            else if (absDifference <= 100)
            {
                return 10;          // 10-100：每次+10（如+50 = 5步）
            }
            else if (absDifference <= 1000)
            {
                return 100;         // 100-1000：每次+100（如+500 = 5步）
            }
            else
            {
                return 1000;        // >1000：每次+1000（如+5000 = 5步）
            }
        }

        #endregion

        #region 调试方法

        /// <summary>
        /// 测试金币增加（调试用）
        /// 可在Inspector中通过按钮调用
        /// </summary>
        [ContextMenu("测试增加10金币")]
        private void TestAddGold()
        {
            if (playerState != null)
            {
                playerState.ModifyGold(10);
            }
        }

        /// <summary>
        /// 测试金币大量增加（调试用）
        /// </summary>
        [ContextMenu("测试增加500金币")]
        private void TestAddLargeGold()
        {
            if (playerState != null)
            {
                playerState.ModifyGold(500);
            }
        }

        /// <summary>
        /// 测试金币减少（调试用）
        /// </summary>
        [ContextMenu("测试减少5金币")]
        private void TestReduceGold()
        {
            if (playerState != null)
            {
                playerState.ModifyGold(-5);
            }
        }

        #endregion
    }
}
