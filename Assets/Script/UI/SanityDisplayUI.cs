/// <summary>
/// 疯狂值显示UI控制器
/// 负责显示世界疯狂值，并根据疯狂等级改变文字底色
/// 创建日期：2026-01-21
/// </summary>

using System.Collections;
using UnityEngine;
using TMPro;

namespace UISystem
{
    /// <summary>
    /// 疯狂值显示UI控制器
    /// 自动订阅GameManager的疯狂值变化事件
    /// 实现数字渐变效果和等级颜色变化
    /// </summary>
    public class SanityDisplayUI : MonoBehaviour
    {
        #region UI组件引用

        [Header("UI组件")]
        [Tooltip("疯狂值文字显示组件（TextMeshPro）")]
        public TextMeshProUGUI sanityText;

        #endregion

        #region 动画设置

        [Header("动画设置")]
        [Tooltip("变化频率（次/秒），例如：20表示每秒变化20次，间隔0.05秒")]
        [Range(1f, 60f)]
        public float changeFrequency = 20f;

        #endregion

        #region 颜色设置

        [Header("疯狂等级颜色设置")]
        [Tooltip("等级0底色（疯狂值0）")]
        public Color level0Color = new Color(1f, 1f, 0.7f, 0.5f); // 淡黄色

        [Tooltip("等级1底色（疯狂值1-3）")]
        public Color level1Color = new Color(1f, 1f, 0f, 0.6f); // 黄色

        [Tooltip("等级2底色（疯狂值4-6）")]
        public Color level2Color = new Color(1f, 0.8f, 0f, 0.7f); // 橙黄色

        [Tooltip("等级3底色（疯狂值7-9）")]
        public Color level3Color = new Color(1f, 0.6f, 0f, 0.8f); // 橙色

        [Tooltip("等级4底色（疯狂值10-12）")]
        public Color level4Color = new Color(1f, 0.3f, 0f, 0.9f); // 橙红色

        [Tooltip("等级5底色（疯狂值13+）")]
        public Color level5Color = new Color(1f, 0f, 0f, 1f); // 红色

        #endregion

        #region 私有字段

        /// <summary>
        /// 当前显示的疯狂值
        /// </summary>
        private int displayedSanity = 0;

        /// <summary>
        /// 疯狂值变化协程引用
        /// </summary>
        private Coroutine sanityChangeCoroutine;

        #endregion

        #region Unity生命周期

        /// <summary>
        /// 初始化
        /// 订阅GameManager的疯狂值变化事件
        /// </summary>
        private void Start()
        {
            // 检查GameManager是否存在
            if (GameManager.Instance == null)
            {
                Debug.LogError("[SanityDisplayUI] GameManager不存在！请确保场景中有GameManager对象。");
                return;
            }

            // 检查UI组件引用
            if (sanityText == null)
            {
                Debug.LogError("[SanityDisplayUI] sanityText未分配！请在Inspector中拖拽TextMeshProUGUI组件。");
                return;
            }

            // 订阅疯狂值变化事件
            GameManager.Instance.OnSanityChanged.AddListener(UpdateSanityUI);

            // 订阅疯狂等级变化事件
            GameManager.Instance.OnSanityLevelChanged.AddListener(UpdateSanityColor);

            // 初始化显示
            displayedSanity = GameManager.Instance.GetSanity();
            sanityText.text = displayedSanity.ToString();

            // 初始化颜色
            UpdateSanityColor(GameManager.Instance.GetSanityLevel());

            Debug.Log("[SanityDisplayUI] 疯狂值显示UI初始化成功");
        }

        /// <summary>
        /// 清理
        /// 取消事件订阅并停止协程，避免内存泄漏
        /// </summary>
        private void OnDestroy()
        {
            // 取消订阅事件
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnSanityChanged.RemoveListener(UpdateSanityUI);
                GameManager.Instance.OnSanityLevelChanged.RemoveListener(UpdateSanityColor);
            }

            // 停止正在运行的协程
            if (sanityChangeCoroutine != null)
            {
                StopCoroutine(sanityChangeCoroutine);
                sanityChangeCoroutine = null;
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 更新疯狂值显示
        /// 当疯狂值变化时被调用
        /// </summary>
        /// <param name="newSanity">新的疯狂值</param>
        private void UpdateSanityUI(int newSanity)
        {
            if (sanityText == null)
            {
                return;
            }

            // 如果有正在运行的协程，先停止
            if (sanityChangeCoroutine != null)
            {
                StopCoroutine(sanityChangeCoroutine);
            }

            // 启动疯狂值数字渐变协程
            sanityChangeCoroutine = StartCoroutine(SanityChangeCoroutine(newSanity));

            Debug.Log($"[SanityDisplayUI] 疯狂值变化: {displayedSanity} → {newSanity}");
        }

        /// <summary>
        /// 疯狂值数字渐变协程
        /// 实现数字逐步增加或减少的效果（步长固定为1）
        /// </summary>
        /// <param name="targetSanity">目标疯狂值</param>
        private IEnumerator SanityChangeCoroutine(int targetSanity)
        {
            int difference = targetSanity - displayedSanity;

            // 如果没有变化，直接返回
            if (difference == 0)
            {
                yield break;
            }

            // 步长固定为1
            int stepSize = 1;

            // 确定方向（增加或减少）
            int direction = difference > 0 ? 1 : -1;

            // 计算每步的时间间隔
            float interval = 1f / changeFrequency;

            // 逐步更新显示的疯狂值
            while (displayedSanity != targetSanity)
            {
                // 计算下一个值
                int nextValue = displayedSanity + (stepSize * direction);

                // 防止越界，确保不超过目标值
                if (direction > 0)
                {
                    displayedSanity = Mathf.Min(nextValue, targetSanity);
                }
                else
                {
                    displayedSanity = Mathf.Max(nextValue, targetSanity);
                }

                // 更新文本显示
                sanityText.text = displayedSanity.ToString();

                // 等待间隔时间
                yield return new WaitForSeconds(interval);
            }

            // 确保最终值准确
            displayedSanity = targetSanity;
            sanityText.text = displayedSanity.ToString();

            sanityChangeCoroutine = null;
        }

        /// <summary>
        /// 更新疯狂值文字底色
        /// 根据疯狂等级改变颜色
        /// </summary>
        /// <param name="level">疯狂等级</param>
        private void UpdateSanityColor(SanityLevel level)
        {
            if (sanityText == null)
            {
                return;
            }

            Color targetColor;

            // 根据等级选择颜色
            switch (level)
            {
                case SanityLevel.Level0:
                    targetColor = level0Color;
                    break;
                case SanityLevel.Level1:
                    targetColor = level1Color;
                    break;
                case SanityLevel.Level2:
                    targetColor = level2Color;
                    break;
                case SanityLevel.Level3:
                    targetColor = level3Color;
                    break;
                case SanityLevel.Level4:
                    targetColor = level4Color;
                    break;
                case SanityLevel.Level5:
                    targetColor = level5Color;
                    break;
                default:
                    targetColor = level0Color;
                    break;
            }

            // 设置文字底色（faceColor是TextMeshPro的底色）
            sanityText.faceColor = targetColor;

            Debug.Log($"[SanityDisplayUI] 疯狂等级变化: {level}，颜色更新");
        }

        #endregion

        #region 调试方法

        /// <summary>
        /// 测试增加疯狂值（调试用）
        /// </summary>
        [ContextMenu("测试/增加疯狂值+1")]
        private void TestAddSanity()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ModifySanity(1);
            }
        }

        /// <summary>
        /// 测试大幅增加疯狂值（调试用）
        /// </summary>
        [ContextMenu("测试/增加疯狂值+5")]
        private void TestAddLargeSanity()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ModifySanity(5);
            }
        }

        /// <summary>
        /// 测试减少疯狂值（调试用）
        /// </summary>
        [ContextMenu("测试/减少疯狂值-1")]
        private void TestReduceSanity()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ModifySanity(-1);
            }
        }

        /// <summary>
        /// 测试重置疯狂值（调试用）
        /// </summary>
        [ContextMenu("测试/重置疯狂值")]
        private void TestResetSanity()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResetSanity();
            }
        }

        #endregion
    }
}
