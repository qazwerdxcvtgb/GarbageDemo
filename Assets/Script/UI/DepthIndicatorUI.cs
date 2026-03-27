/// <summary>
/// 玩家深度指示器 UI
/// 在一个固定大小的容器内，根据玩家当前深度用 DOTween 动画平滑移动图标
/// 深度一：顶对齐；深度二：居中对齐；深度三：底对齐
/// 容器最下方的下潜按钮：花费 1 点体力下降一个深度等级
/// 创建日期：2026-03-27
/// </summary>

using UnityEngine;
using UnityEngine.UI;
using ItemSystem;
using DG.Tweening;

namespace UISystem
{
    /// <summary>
    /// 深度指示器 UI 控制器
    /// 挂载于图标 GameObject（DepthIndicatorIcon）上；容器为其直接父节点 RectTransform
    /// 图标锚点固定在容器中心，通过 anchoredPosition.y 的 DOTween 动画表达深度位置
    /// descentButton 为容器的兄弟或子节点按钮，在 Inspector 中手动赋值
    /// </summary>
    public class DepthIndicatorUI : MonoBehaviour
    {
        #region Inspector Fields

        [Header("玩家引用")]
        [Tooltip("玩家 CharacterState 组件引用")]
        [SerializeField] private CharacterState playerState;

        [Header("下潜按钮")]
        [Tooltip("点击花费 1 点体力下降一个深度等级（拖入容器内的 Button 节点）")]
        [SerializeField] private Button descentButton;

        [Header("动画参数")]
        [Tooltip("深度切换动画时长（秒）")]
        [SerializeField] private float animDuration = 0.4f;

        [Tooltip("动画缓动曲线")]
        [SerializeField] private Ease animEase = Ease.OutCubic;

        [Header("图标样式（可选）")]
        [Tooltip("深度一对应的图标 Sprite")]
        [SerializeField] private Sprite depth1Sprite;

        [Tooltip("深度二对应的图标 Sprite")]
        [SerializeField] private Sprite depth2Sprite;

        [Tooltip("深度三对应的图标 Sprite")]
        [SerializeField] private Sprite depth3Sprite;

        #endregion

        #region Private Fields

        private RectTransform iconRect;
        private RectTransform containerRect;
        private Image iconImage;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            iconRect      = GetComponent<RectTransform>();
            containerRect = transform.parent as RectTransform;
            iconImage     = GetComponent<Image>();

            if (containerRect == null)
                Debug.LogWarning("[DepthIndicatorUI] 未找到父容器 RectTransform，图标对齐将无法正常工作");

            // 固定锚点为容器中心，后续只通过 anchoredPosition.y 控制位置
            if (iconRect != null)
            {
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot     = new Vector2(0.5f, 0.5f);
            }

            if (descentButton != null)
                descentButton.onClick.AddListener(OnDescentClicked);
        }

        private void Start()
        {
            if (playerState == null)
            {
                Debug.LogWarning("[DepthIndicatorUI] playerState 未指定，无法订阅深度事件");
                return;
            }

            playerState.OnDepthChanged.AddListener(OnDepthChanged);
            playerState.OnHealthChanged.AddListener(OnHealthChanged);

            // 初始化时直接定位，不播放动画
            SnapToDepth(playerState.CurrentDepth);
            RefreshButtonState();
        }

        private void OnDestroy()
        {
            iconRect?.DOKill();

            if (playerState != null)
            {
                playerState.OnDepthChanged.RemoveListener(OnDepthChanged);
                playerState.OnHealthChanged.RemoveListener(OnHealthChanged);
            }

            if (descentButton != null)
                descentButton.onClick.RemoveListener(OnDescentClicked);
        }

        #endregion

        #region Event Handlers

        private void OnDepthChanged(FishDepth depth)
        {
            AnimateToDepth(depth);
            RefreshButtonState();
        }

        private void OnHealthChanged(int health)
        {
            RefreshButtonState();
        }

        private void OnDescentClicked()
        {
            if (playerState == null) return;

            FishDepth current = playerState.CurrentDepth;

            // 已在最深层，无法继续下潜
            if (current == FishDepth.Depth3)
            {
                Debug.Log("[DepthIndicatorUI] 已到达最深层，无法继续下潜");
                return;
            }

            // 体力不足
            if (playerState.CurrentHealth < 1)
            {
                Debug.Log("[DepthIndicatorUI] 体力不足，无法下潜");
                return;
            }

            FishDepth next = current == FishDepth.Depth1 ? FishDepth.Depth2 : FishDepth.Depth3;
            playerState.ModifyHealth(-1);
            playerState.SetDepth(next);

            Debug.Log($"[DepthIndicatorUI] 下潜：{current} → {next}，剩余体力={playerState.CurrentHealth}");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 根据当前体力和深度刷新下潜按钮的可交互状态
        /// 体力不足 1 或已到 Depth3 时禁用按钮
        /// </summary>
        private void RefreshButtonState()
        {
            if (descentButton == null || playerState == null) return;

            bool canDescend = playerState.CurrentDepth != FishDepth.Depth3
                           && playerState.CurrentHealth >= 1;
            descentButton.interactable = canDescend;
        }

        /// <summary>
        /// 根据深度计算图标的目标 Y 坐标（相对于容器中心）
        /// Depth1 = 顶部边缘，Depth2 = 中心，Depth3 = 底部边缘
        /// </summary>
        private float GetTargetY(FishDepth depth)
        {
            if (containerRect == null || iconRect == null) return 0f;

            float containerHalf = containerRect.rect.height * 0.5f;
            float iconHalf      = iconRect.rect.height * 0.5f;

            switch (depth)
            {
                case FishDepth.Depth1: return  containerHalf - iconHalf;   // 顶对齐
                case FishDepth.Depth2: return  0f;                         // 居中
                case FishDepth.Depth3: return -(containerHalf - iconHalf); // 底对齐
                default:               return  0f;
            }
        }

        /// <summary>
        /// 初始化时直接定位，不播放动画
        /// </summary>
        private void SnapToDepth(FishDepth depth)
        {
            if (iconRect == null) return;

            iconRect.DOKill();
            float targetY = GetTargetY(depth);
            iconRect.anchoredPosition = new Vector2(iconRect.anchoredPosition.x, targetY);
            UpdateSprite(depth);
        }

        /// <summary>
        /// 深度变化时播放 DOTween 移动动画
        /// </summary>
        private void AnimateToDepth(FishDepth depth)
        {
            if (iconRect == null) return;

            float targetY = GetTargetY(depth);
            iconRect.DOKill();
            iconRect.DOAnchorPosY(targetY, animDuration).SetEase(animEase);
            UpdateSprite(depth);
        }

        /// <summary>
        /// 切换图标 Sprite（未配置对应 Sprite 时保持原样）
        /// </summary>
        private void UpdateSprite(FishDepth depth)
        {
            if (iconImage == null) return;

            Sprite target = depth switch
            {
                FishDepth.Depth1 => depth1Sprite,
                FishDepth.Depth2 => depth2Sprite,
                FishDepth.Depth3 => depth3Sprite,
                _                => null
            };

            if (target != null)
                iconImage.sprite = target;
        }

        #endregion

        #region Debug

        [ContextMenu("测试/切换深度一")]
        private void TestDepth1() => AnimateToDepth(FishDepth.Depth1);

        [ContextMenu("测试/切换深度二")]
        private void TestDepth2() => AnimateToDepth(FishDepth.Depth2);

        [ContextMenu("测试/切换深度三")]
        private void TestDepth3() => AnimateToDepth(FishDepth.Depth3);

        #endregion
    }
}
