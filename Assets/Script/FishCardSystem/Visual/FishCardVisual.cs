using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using ItemSystem;

namespace FishCardSystem
{
    /// <summary>
    /// 手牌卡牌视觉控制器（视觉卡，支持所有 ItemCard 子类）
    /// 负责：所有视觉效果和动画；数据显示委托给对应的 FrontDisplay 组件
    /// </summary>
    public class FishCardVisual : MonoBehaviour
    {
        #region Fields

        private bool initialized = false;

        [Header("逻辑卡引用")]
        public ItemCard parentCard;
        private Transform cardTransform;
        private Canvas canvas;
        private int homeSortingOrder = 0;

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
        [SerializeField] private TrashCardFrontDisplay trashFrontDisplay;
        [SerializeField] private ConsumableCardFrontDisplay consumableFrontDisplay;
        [SerializeField] private EquipmentCardFrontDisplay equipmentFrontDisplay;

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
        /// 初始化视觉卡（支持所有 ItemCard 子类）
        /// </summary>
        public void Initialize(ItemCard target)
        {
            parentCard    = target;
            cardTransform = target.transform;
            canvas        = GetComponent<Canvas>();

            parentCard.PointerEnterEvent.AddListener(PointerEnter);
            parentCard.PointerExitEvent.AddListener(PointerExit);
            parentCard.BeginDragEvent.AddListener(BeginDrag);
            parentCard.EndDragEvent.AddListener(EndDrag);
            parentCard.PointerDownEvent.AddListener(PointerDown);
            parentCard.PointerUpEvent.AddListener(PointerUp);
            parentCard.SelectEvent.AddListener(Select);

            parentCard.ActiveChangedEvent.AddListener(OnParentActiveChanged);

            initialized = true;

            if (parentCard.cardData != null)
                UpdateCardData(parentCard.cardData);
        }

        private void OnDestroy()
        {
            if (parentCard != null)
                parentCard.ActiveChangedEvent.RemoveListener(OnParentActiveChanged);
        }

        /// <summary>
        /// 响应逻辑卡的显隐变化。
        /// 重新激活时先对齐逻辑卡位置再显示，防止第一帧从旧坐标开始 Lerp。
        /// </summary>
        private void OnParentActiveChanged(bool active)
        {
            if (active)
            {
                if (cardTransform != null)
                    transform.position = cardTransform.position;
                gameObject.SetActive(true);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 启用正面体力消耗的效果修改显示（仅钓鱼上下文调用）
        /// </summary>
        public void EnableFrontEffectDisplay()
        {
            if (frontDisplay != null)
                frontDisplay.EnableEffectDisplay();
        }

        /// <summary>
        /// 更新卡牌数据显示，根据数据类型路由到对应 FrontDisplay
        /// </summary>
        public void UpdateCardData(ItemData data)
        {
            if (data is FishData fd && frontDisplay != null)
                frontDisplay.UpdateDisplay(fd);
            else if (data is TrashData td && trashFrontDisplay != null)
                trashFrontDisplay.UpdateDisplay(td);
            else if (data is ConsumableData cd && consumableFrontDisplay != null)
                consumableFrontDisplay.UpdateDisplay(cd);
            else if (data is EquipmentData ed && equipmentFrontDisplay != null)
                equipmentFrontDisplay.UpdateDisplay(ed);
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

            // 牌堆模式下禁用移动驱动的Z轴自转，平滑归零后提前返回
            if (parentCard.IsDisplayOnly)
            {
                Vector3 pileRot = transform.eulerAngles;
                pileRot.z = Mathf.LerpAngle(pileRot.z, 0f, rotationSpeed * Time.deltaTime);
                transform.eulerAngles = pileRot;
                return;
            }

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

            // Z轴倾斜（拖拽/牌堆模式/curve为null时归零，其余时由弧线曲线驱动）
            float tiltZ = (parentCard.isDragging || parentCard.IsDisplayOnly || curve == null) ? 0f : 
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

        private void PointerEnter(ItemCard card)
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

        private void PointerExit(ItemCard card)
        {
            if (!scaleAnimations)
                return;

            if (!card.wasDragged)
            {
                transform.DOScale(1f, scaleTransition).SetEase(scaleEase);
            }
        }

        private void PointerDown(ItemCard card)
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

        private void PointerUp(ItemCard card, bool longPress)
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

        private void BeginDrag(ItemCard card)
        {
            if (scaleAnimations)
                transform.DOScale(scaleOnSelect, scaleTransition).SetEase(scaleEase);

            // 拖拽时置顶（与 scaleAnimations 无关，始终执行）
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 200;
            }
        }

        private void EndDrag(ItemCard card)
        {
            if (scaleAnimations)
                transform.DOScale(1f, scaleTransition).SetEase(scaleEase);

            // 恢复归属层级（与 scaleAnimations 无关，始终执行）
            ApplyHomeSortingOrder();
        }

        /// <summary>
        /// 设置视觉卡的归属层级（面板 sortingOrder + 1）。
        /// 传入 0 表示不覆盖，继承父容器（VisualCardsHandler = 190）。
        /// </summary>
        public void SetHomeSortingOrder(int order)
        {
            homeSortingOrder = order;
            ApplyHomeSortingOrder();
        }

        private void ApplyHomeSortingOrder()
        {
            if (canvas == null) return;
            if (homeSortingOrder > 0)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder    = homeSortingOrder;
            }
            else
            {
                canvas.overrideSorting = false;
                canvas.sortingOrder    = 0;
            }
        }

        private void Select(ItemCard card, bool state)
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
