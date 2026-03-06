/// <summary>
/// 体力显示UI控制器
/// 负责显示玩家体力的Slider和Text组件
/// 创建日期：2026-01-20
/// </summary>

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UISystem
{
    /// <summary>
    /// 体力显示UI控制器
    /// 自动查找玩家CharacterState组件并订阅体力变化事件
    /// 实时更新体力Slider和Text显示
    /// </summary>
    public class HealthDisplayUI : MonoBehaviour
    {
        #region UI组件引用

        [Header("UI组件")]
        [Tooltip("体力滑块组件")]
        public Slider healthSlider;

        [Tooltip("体力文字显示组件")]
        public TextMeshProUGUI healthText;

        #endregion

        #region 私有字段

        /// <summary>
        /// 玩家状态组件引用
        /// </summary>
        private CharacterState playerState;

        #endregion

        #region Unity生命周期

        /// <summary>
        /// 初始化
        /// 查找玩家对象并订阅体力变化事件
        /// </summary>
        private void Start()
        {
            // 查找场景中的player对象
            GameObject playerObject = GameObject.Find("player");

            if (playerObject == null)
            {
                Debug.LogError("[HealthDisplayUI] 未找到名为'player'的GameObject！请检查场景中玩家对象的名称。");
                return;
            }

            // 获取CharacterState组件
            playerState = playerObject.GetComponent<CharacterState>();

            if (playerState == null)
            {
                Debug.LogError("[HealthDisplayUI] player对象上未找到CharacterState组件！");
                return;
            }

            // 检查UI组件引用
            if (healthSlider == null)
            {
                Debug.LogError("[HealthDisplayUI] healthSlider未分配！请在Inspector中拖拽Slider组件。");
                return;
            }

            if (healthText == null)
            {
                Debug.LogError("[HealthDisplayUI] healthText未分配！请在Inspector中拖拽TextMeshProUGUI组件。");
                return;
            }

            // 订阅体力变化事件
            playerState.OnHealthChanged.AddListener(UpdateHealthUI);
            playerState.OnMaxHealthChanged.AddListener(UpdateMaxHealthUI);

            // 初始化UI显示
            UpdateMaxHealthUI(playerState.MaxHealth);
            UpdateHealthUI(playerState.CurrentHealth);

            Debug.Log("[HealthDisplayUI] 体力显示UI初始化成功");
        }

        /// <summary>
        /// 清理
        /// 取消事件订阅，避免内存泄漏
        /// </summary>
        private void OnDestroy()
        {
            // 取消订阅事件
            if (playerState != null)
            {
                playerState.OnHealthChanged.RemoveListener(UpdateHealthUI);
                playerState.OnMaxHealthChanged.RemoveListener(UpdateMaxHealthUI);
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 更新当前体力显示
        /// </summary>
        /// <param name="currentHealth">当前体力值</param>
        private void UpdateHealthUI(int currentHealth)
        {
            if (playerState == null || healthSlider == null || healthText == null)
            {
                return;
            }

            // 更新Slider的值（0-1之间的比例）
            float healthRatio = playerState.MaxHealth > 0 
                ? (float)currentHealth / (float)playerState.MaxHealth 
                : 0f;
            healthSlider.value = healthRatio;

            // 更新Text显示（格式："当前/最大"）
            healthText.text = $"{currentHealth}/{playerState.MaxHealth}";

            Debug.Log($"[HealthDisplayUI] 体力更新: {currentHealth}/{playerState.MaxHealth} ({healthRatio:P0})");
        }

        /// <summary>
        /// 更新最大体力值
        /// </summary>
        /// <param name="maxHealth">新的最大体力值</param>
        private void UpdateMaxHealthUI(int maxHealth)
        {
            if (playerState == null)
            {
                return;
            }

            // 刷新当前体力显示（因为比例可能改变）
            UpdateHealthUI(playerState.CurrentHealth);

            Debug.Log($"[HealthDisplayUI] 最大体力更新: {maxHealth}");
        }

        #endregion
    }
}
