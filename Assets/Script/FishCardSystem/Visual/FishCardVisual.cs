using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using ItemSystem;

namespace FishCardSystem
{
    /// <summary>
    /// 鱼类卡牌视觉控制器（视觉卡）
    /// 负责：所有视觉效果和动画
    /// </summary>
    public class FishCardVisual : MonoBehaviour
    {
        #region Fields

        private bool initialized = false;

        [Header("逻辑卡引用")]
        public FishCard parentCard;
        private Transform cardTransform;
        private Canvas canvas;

        [Header("视觉组件引用")]
        // [SerializeField] private Transform visualShadow;  // 阴影功能暂时禁用
        [SerializeField] private Transform shakeParent;
        [SerializeField] private Transform tiltParent;
        // private Canvas shadowCanvas;  // 阴影功能暂时禁用
        // private Vector2 shadowDistance;  // 阴影功能暂时禁用
        
        // [Header("阴影参数")]  // 阴影功能暂时禁用
        // [SerializeField] private bool shadowFollowRotation = true;
        // [SerializeField] private float shadowRotationInfluence = 0.5f;
        // [SerializeField] private float shadowBlurAmount = 10f;
        // [SerializeField] private float shadowOffset = 20f;
        // private bool isPressed = false;

        [Header("显示模块")]
        [SerializeField] private FishCardFrontDisplay frontDisplay;

        [Header("跟随参数")]
        [SerializeField] private float followSpeed = 30f;

        [Header("旋转参数")]
        [SerializeField] private float rotationAmount = 10f;
        [SerializeField] private float rotationSpeed = 12f;
        [SerializeField] private float autoTiltAmount = 0f;    // 设为0禁用自动旋转
        [SerializeField] private float manualTiltAmount = 15f;
        [SerializeField] private float tiltSpeed = 10f;
        private Vector3 rotationDelta;
        private Vector3 movementDelta;
        private int savedIndex;

        [Header("缩放参数")]
        [SerializeField] private bool scaleAnimations = true;
        [SerializeField] private float scaleOnHover = 1.15f;
        [SerializeField] private float scaleOnSelect = 1.25f;
        [SerializeField] private float scaleTransition = 0.15f;
        [SerializeField] private Ease scaleEase = Ease.OutBack;

        [Header("选中Punch参数")]
        [SerializeField] private float selectPunchAmount = 20f;

        [Header("悬停Punch参数")]
        [SerializeField] private float hoverPunchAngle = 5f;
        [SerializeField] private float hoverTransition = 0.15f;

        [Header("交换参数")]
        [SerializeField] private bool swapAnimations = true;
        [SerializeField] private float swapRotationAngle = 30f;
        [SerializeField] private float swapTransition = 0.15f;
        [SerializeField] private int swapVibrato = 5;

        [Header("弧线参数")]
        [SerializeField] private CurveParameters curve;
        private float curveYOffset;
        private float curveRotationOffset;

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (!initialized || parentCard == null)
                return;

            HandPositioning();
            SmoothFollow();
            FollowRotation();
            CardTilt();
            // UpdateShadow();  // 阴影功能暂时禁用
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 初始化视觉卡
        /// </summary>
        public void Initialize(FishCard target)
        {
            parentCard = target;
            cardTransform = target.transform;
            canvas = GetComponent<Canvas>();
            
            // 阴影功能暂时禁用
            // if (visualShadow != null)
            // {
            //     shadowCanvas = visualShadow.GetComponent<Canvas>();
            //     shadowDistance = visualShadow.localPosition;
            // }

            // 订阅逻辑卡的事件
            parentCard.PointerEnterEvent.AddListener(PointerEnter);
            parentCard.PointerExitEvent.AddListener(PointerExit);
            parentCard.BeginDragEvent.AddListener(BeginDrag);
            parentCard.EndDragEvent.AddListener(EndDrag);
            parentCard.PointerDownEvent.AddListener(PointerDown);
            parentCard.PointerUpEvent.AddListener(PointerUp);
            parentCard.SelectEvent.AddListener(Select);

            initialized = true;

            // 更新卡牌数据显示
            if (parentCard.cardData != null)
            {
                UpdateCardData(parentCard.cardData);
            }
        }

        /// <summary>
        /// 更新卡牌数据显示
        /// </summary>
        public void UpdateCardData(FishData data)
        {
            if (frontDisplay != null)
            {
                frontDisplay.UpdateDisplay(data);
            }

        }

        /// <summary>
        /// 更新渲染顺序
        /// </summary>
        public void UpdateIndex()
        {
            if (parentCard != null && parentCard.transform.parent != null)
            {
                transform.SetSiblingIndex(parentCard.transform.parent.GetSiblingIndex());
            }
        }

        #endregion

        #region Follow and Layout

        /// <summary>
        /// 计算手牌弧线偏移
        /// </summary>
        private void HandPositioning()
        {
            if (curve == null || parentCard == null)
            {
                curveYOffset = 0;
                curveRotationOffset = 0;
                return;
            }

            float normalizedPos = parentCard.NormalizedPosition();
            int siblingAmount = parentCard.SiblingAmount();

            // 计算位置偏移（单张牌时无需弧线）
            curveYOffset = siblingAmount >= 1
                ? curve.positioning.Evaluate(normalizedPos) * curve.positioningInfluence * siblingAmount
                : 0f;

            // 计算旋转偏移
            curveRotationOffset = curve.rotation.Evaluate(normalizedPos);
        }

        /// <summary>
        /// 平滑跟随逻辑卡位置
        /// </summary>
        private void SmoothFollow()
        {
            if (cardTransform == null)
                return;

            // 拖拽时不应用弧线偏移
            float yOffset = parentCard.isDragging ? 0 : curveYOffset;
            Vector3 targetPosition = cardTransform.position + Vector3.up * yOffset;

            transform.position = Vector3.Lerp(transform.position, targetPosition, 
                                             followSpeed * Time.deltaTime);
        }

        /// <summary>
        /// 基于移动的旋转效果
        /// </summary>
        private void FollowRotation()
        {
            if (cardTransform == null)
                return;

            Vector3 movement = transform.position - cardTransform.position;
            movementDelta = Vector3.Lerp(movementDelta, movement, 25f * Time.deltaTime);

            Vector3 deltaToUse = parentCard.isDragging ? movementDelta : movement;
            rotationDelta = Vector3.Lerp(rotationDelta, deltaToUse * rotationAmount, 
                                        rotationSpeed * Time.deltaTime);

            // 只影响Z轴旋转
            float zRotation = -rotationDelta.x;
            zRotation = Mathf.Clamp(zRotation, -60f, 60f);

            Vector3 currentRotation = transform.eulerAngles;
            currentRotation.z = zRotation;
            transform.eulerAngles = currentRotation;
        }

        /// <summary>
        /// 卡牌倾斜效果（自动+手动）
        /// </summary>
        private void CardTilt()
        {
            if (tiltParent == null || parentCard == null)
                return;

            // 保存索引用于自动倾斜
            if (!parentCard.isDragging)
            {
                savedIndex = parentCard.ParentIndex();
            }

            // 自动倾斜（正弦/余弦波动）
            float sineWave = Mathf.Sin(Time.time + savedIndex) * (parentCard.isHovering ? 0.2f : 1f);
            float cosineWave = Mathf.Cos(Time.time + savedIndex) * (parentCard.isHovering ? 0.2f : 1f);

            // 手动倾斜（基于鼠标位置）
            float tiltX = 0;
            float tiltY = 0;
            if (parentCard.isHovering && Camera.main != null)
            {
                Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector3 offset = transform.position - mouseWorld;
                tiltX = -offset.y * manualTiltAmount;
                tiltY = offset.x * manualTiltAmount;
            }

            // Z轴倾斜（拖拽时归零，松手后还原弧线旋转）
            float tiltZ = parentCard.isDragging ? 0f : 
                         (curveRotationOffset * curve.rotationInfluence * parentCard.SiblingAmount());

            // 应用倾斜
            Vector3 targetTilt = new Vector3(
                tiltX + sineWave * autoTiltAmount,
                tiltY + cosineWave * autoTiltAmount,
                tiltZ
            );

            Vector3 currentTilt = tiltParent.eulerAngles;
            currentTilt.x = Mathf.LerpAngle(currentTilt.x, targetTilt.x, tiltSpeed * Time.deltaTime);
            currentTilt.y = Mathf.LerpAngle(currentTilt.y, targetTilt.y, tiltSpeed * Time.deltaTime);
            currentTilt.z = Mathf.LerpAngle(currentTilt.z, targetTilt.z, tiltSpeed * Time.deltaTime);
            tiltParent.eulerAngles = currentTilt;
        }

        // 阴影功能暂时禁用
        // /// <summary>
        // /// 更新阴影效果
        // /// </summary>
        // private void UpdateShadow()
        // {
        //     if (visualShadow == null)
        //         return;
        //
        //     if (shadowFollowRotation && tiltParent != null)
        //     {
        //         Vector3 tiltRotation = tiltParent.eulerAngles;
        //         
        //         Vector3 shadowRotation = visualShadow.localEulerAngles;
        //         shadowRotation.x = tiltRotation.x * shadowRotationInfluence;
        //         shadowRotation.y = tiltRotation.y * shadowRotationInfluence;
        //         shadowRotation.z = tiltRotation.z * shadowRotationInfluence;
        //         visualShadow.localEulerAngles = shadowRotation;
        //         
        //         float xTilt = tiltRotation.x;
        //         float yTilt = tiltRotation.y;
        //         
        //         if (xTilt > 180f) xTilt -= 360f;
        //         if (yTilt > 180f) yTilt -= 360f;
        //         
        //         float xOffset = Mathf.Sin(yTilt * Mathf.Deg2Rad) * shadowBlurAmount;
        //         float yOffset = -Mathf.Sin(xTilt * Mathf.Deg2Rad) * shadowBlurAmount;
        //         
        //         float pressOffset = isPressed ? shadowOffset : 0f;
        //         
        //         Vector2 dynamicOffset = shadowDistance + new Vector2(xOffset, yOffset + pressOffset);
        //         visualShadow.localPosition = dynamicOffset;
        //     }
        // }

        #endregion

        #region Animation Methods

        /// <summary>
        /// 交换动画
        /// </summary>
        public void Swap(float direction)
        {
            if (!swapAnimations || shakeParent == null)
                return;

            DOTween.Kill(3);
            shakeParent.DOPunchRotation(
                Vector3.forward * swapRotationAngle * direction,
                swapTransition,
                swapVibrato,
                1
            ).SetId(3);
        }

        #endregion

        #region Event Handlers

        private void PointerEnter(FishCard card)
        {
            if (!scaleAnimations)
                return;

            transform.DOScale(scaleOnHover, scaleTransition).SetEase(scaleEase);

            if (shakeParent != null)
            {
                DOTween.Kill(2);
                shakeParent.DOPunchRotation(
                    Vector3.forward * hoverPunchAngle,
                    hoverTransition,
                    20,
                    1
                ).SetId(2);
            }
        }

        private void PointerExit(FishCard card)
        {
            if (!scaleAnimations)
                return;

            if (!card.wasDragged)
            {
                transform.DOScale(1f, scaleTransition).SetEase(scaleEase);
            }
        }

        private void PointerDown(FishCard card)
        {
            if (!scaleAnimations)
                return;

            transform.DOScale(scaleOnSelect, scaleTransition).SetEase(scaleEase);

            // 阴影功能暂时禁用
            // isPressed = true;
            // if (shadowCanvas != null)
            // {
            //     shadowCanvas.overrideSorting = false;
            // }
        }

        private void PointerUp(FishCard card, bool longPress)
        {
            if (!scaleAnimations)
                return;

            // 根据是否长按决定缩放
            float targetScale = longPress ? 1f : scaleOnHover;
            transform.DOScale(targetScale, scaleTransition).SetEase(scaleEase);

            // 阴影功能暂时禁用
            // isPressed = false;
            // if (shadowCanvas != null)
            // {
            //     shadowCanvas.overrideSorting = true;
            // }
        }

        private void BeginDrag(FishCard card)
        {
            if (!scaleAnimations)
                return;

            transform.DOScale(scaleOnSelect, scaleTransition).SetEase(scaleEase);

            // 提升Canvas排序层级
            if (canvas != null)
            {
                canvas.overrideSorting = true;
            }
        }

        private void EndDrag(FishCard card)
        {
            if (!scaleAnimations)
                return;

            transform.DOScale(1f, scaleTransition).SetEase(scaleEase);

            // 恢复Canvas排序
            if (canvas != null)
            {
                canvas.overrideSorting = false;
            }
        }

        private void Select(FishCard card, bool state)
        {
            DOTween.Kill(2);

            // 选中时Punch动画
            if (shakeParent != null)
            {
                float punchDirection = state ? 1f : 0f;
                shakeParent.DOPunchPosition(
                    shakeParent.up * selectPunchAmount * punchDirection,
                    scaleTransition,
                    10,
                    1
                );

                shakeParent.DOPunchRotation(
                    Vector3.forward * (hoverPunchAngle / 2),
                    hoverTransition,
                    20,
                    1
                ).SetId(2);
            }

            // 选中时缩放
            if (scaleAnimations && state)
            {
                transform.DOScale(scaleOnHover, scaleTransition).SetEase(scaleEase);
            }
        }

        #endregion
    }
}
