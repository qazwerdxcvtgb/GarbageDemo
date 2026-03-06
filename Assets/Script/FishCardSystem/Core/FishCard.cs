using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using ItemSystem;

namespace FishCardSystem
{
    /// <summary>
    /// 鱼类卡牌逻辑控制器（逻辑卡）
    /// 负责：数据绑定、输入处理、状态管理、位置计算
    /// </summary>
    public class FishCard : MonoBehaviour,
        IDragHandler, IBeginDragHandler, IEndDragHandler,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerUpHandler, IPointerDownHandler
    {
        #region Fields

        [Header("数据")]
        public FishData cardData;

        [Header("组件引用")]
        private Canvas canvas;
        private Image imageComponent;
        private CardFaceController faceController;

        [Header("视觉设置")]
        [SerializeField] private bool instantiateVisual = true;
        [SerializeField] private GameObject cardVisualPrefab;
        [HideInInspector] public FishCardVisual cardVisual;

        [Header("移动参数")]
        [SerializeField] private float moveSpeedLimit = 50f;
        private Vector3 dragOffset;

        [Header("选中参数")]
        public bool selected;
        public float selectionOffset = 50f;
        private float pointerDownTime;
        private float pointerUpTime;

        [Header("模式设置")]
        [SerializeField] private bool pileMode = false;  // 牌堆模式（禁用拖拽、选中偏移）

        [Header("状态")]
        public bool isHovering;
        public bool isDragging;
        [HideInInspector] public bool wasDragged;

        [Header("事件")]
        [HideInInspector] public UnityEvent<FishCard> PointerEnterEvent;
        [HideInInspector] public UnityEvent<FishCard> PointerExitEvent;
        [HideInInspector] public UnityEvent<FishCard> PointerDownEvent;
        [HideInInspector] public UnityEvent<FishCard, bool> PointerUpEvent;
        [HideInInspector] public UnityEvent<FishCard> BeginDragEvent;
        [HideInInspector] public UnityEvent<FishCard> EndDragEvent;
        [HideInInspector] public UnityEvent<FishCard, bool> SelectEvent;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // 获取组件
            canvas = GetComponentInParent<Canvas>();
            imageComponent = GetComponent<Image>();
            faceController = GetComponent<CardFaceController>();

            // 初始化事件
            if (PointerEnterEvent == null) PointerEnterEvent = new UnityEvent<FishCard>();
            if (PointerExitEvent == null) PointerExitEvent = new UnityEvent<FishCard>();
            if (PointerDownEvent == null) PointerDownEvent = new UnityEvent<FishCard>();
            if (PointerUpEvent == null) PointerUpEvent = new UnityEvent<FishCard, bool>();
            if (BeginDragEvent == null) BeginDragEvent = new UnityEvent<FishCard>();
            if (EndDragEvent == null) EndDragEvent = new UnityEvent<FishCard>();
            if (SelectEvent == null) SelectEvent = new UnityEvent<FishCard, bool>();

            // 实例化视觉卡
            if (instantiateVisual && cardVisualPrefab != null)
            {
                VisualCardsHandler visualHandler = FindObjectOfType<VisualCardsHandler>();
                // 优先使用主Canvas，确保统一的缩放
                Transform parent = canvas != null ? canvas.transform : 
                                  (visualHandler != null ? visualHandler.transform : transform.parent);
                cardVisual = Instantiate(cardVisualPrefab, parent).GetComponent<FishCardVisual>();
                cardVisual.Initialize(this);
            }
        }

        private void Update()
        {
            if (isDragging)
            {
                ClampPosition();

                Vector2 targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition) - dragOffset;
                Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
                float distance = Vector2.Distance(transform.position, targetPosition);
                float speed = Mathf.Min(moveSpeedLimit, distance / Time.deltaTime);
                Vector2 velocity = direction * speed;
                transform.Translate(velocity * Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            if (cardVisual != null)
            {
                Destroy(cardVisual.gameObject);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 初始化卡牌数据
        /// </summary>
        public void Initialize(FishData data)
        {
            cardData = data;
            if (cardVisual != null)
            {
                cardVisual.UpdateCardData(data);
            }
        }

        /// <summary>
        /// 取消选中
        /// </summary>
        public void Deselect()
        {
            if (selected)
            {
                selected = false;
                transform.localPosition = Vector3.zero;
                SelectEvent.Invoke(this, false);
            }
        }

        /// <summary>
        /// 翻转到正面
        /// </summary>
        public void FlipToFront(float duration = 0.5f)
        {
            if (cardVisual != null)
            {
                cardVisual.FlipToFront(duration);
            }
        }

        /// <summary>
        /// 翻转到背面
        /// </summary>
        public void FlipToBack(float duration = 0.5f)
        {
            if (cardVisual != null)
            {
                cardVisual.FlipToBack(duration);
            }
        }

        /// <summary>
        /// 设置牌堆模式
        /// </summary>
        public void SetPileMode(bool enabled)
        {
            pileMode = enabled;
        }

        /// <summary>
        /// 获取是否处于牌堆模式
        /// </summary>
        public bool IsPileMode => pileMode;

        #endregion

        #region Position Calculation

        /// <summary>
        /// 限制卡牌位置在屏幕边界内
        /// </summary>
        private void ClampPosition()
        {
            if (Camera.main == null) return;

            Vector2 screenBounds = Camera.main.ScreenToWorldPoint(
                new Vector3(Screen.width, Screen.height, Camera.main.transform.position.z));
            Vector3 clampedPosition = transform.position;
            clampedPosition.x = Mathf.Clamp(clampedPosition.x, -screenBounds.x, screenBounds.x);
            clampedPosition.y = Mathf.Clamp(clampedPosition.y, -screenBounds.y, screenBounds.y);
            transform.position = clampedPosition;
        }

        /// <summary>
        /// 获取父节点索引
        /// </summary>
        public int ParentIndex()
        {
            if (transform.parent != null && transform.parent.CompareTag("Slot"))
            {
                return transform.parent.GetSiblingIndex();
            }
            return 0;
        }

        /// <summary>
        /// 获取兄弟数量
        /// </summary>
        public int SiblingAmount()
        {
            if (transform.parent != null && transform.parent.CompareTag("Slot"))
            {
                return transform.parent.parent.childCount - 1;
            }
            return 0;
        }

        /// <summary>
        /// 获取归一化位置（0~1）
        /// </summary>
        public float NormalizedPosition()
        {
            if (transform.parent != null && transform.parent.CompareTag("Slot"))
            {
                int siblingAmount = SiblingAmount();
                if (siblingAmount > 0)
                {
                    return ((float)ParentIndex()).Remap(0, siblingAmount, 0, 1);
                }
            }
            return 0;
        }

        #endregion

        #region Input Event Handlers

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovering = true;
            PointerEnterEvent.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
            PointerExitEvent.Invoke(this);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            pointerDownTime = Time.time;
            PointerDownEvent.Invoke(this);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            pointerUpTime = Time.time;
            bool longPress = (pointerUpTime - pointerDownTime) > 0.2f;
            PointerUpEvent.Invoke(this, longPress);

            // 长按或拖拽过不触发选中
            if (longPress || wasDragged)
                return;

            // 牌堆模式下不触发选中偏移
            if (pileMode)
            {
                // 只触发点击事件，不改变选中状态和位置
                SelectEvent.Invoke(this, false);
                return;
            }

            // 切换选中状态
            selected = !selected;
            SelectEvent.Invoke(this, selected);

            if (selected)
            {
                // 选中时向上偏移
                if (cardVisual != null)
                {
                    transform.localPosition += cardVisual.transform.up * selectionOffset;
                }
                else
                {
                    transform.localPosition += Vector3.up * selectionOffset;
                }
            }
            else
            {
                transform.localPosition = Vector3.zero;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // 牌堆模式下禁止拖拽
            if (pileMode)
                return;

            if (Camera.main == null)
                return;

            BeginDragEvent.Invoke(this);

            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            dragOffset = mousePosition - (Vector2)transform.position;

            isDragging = true;
            wasDragged = true;

            // 禁用射线检测避免干扰
            if (canvas != null)
            {
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster != null)
                    raycaster.enabled = false;
            }
            if (imageComponent != null)
                imageComponent.raycastTarget = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // 拖拽逻辑在Update中处理
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            EndDragEvent.Invoke(this);

            isDragging = false;

            // 恢复射线检测
            if (canvas != null)
            {
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster != null)
                    raycaster.enabled = true;
            }
            if (imageComponent != null)
                imageComponent.raycastTarget = true;

            // 延迟一帧清除wasDragged标记
            StartCoroutine(FrameWait());
        }

        private IEnumerator FrameWait()
        {
            yield return new WaitForEndOfFrame();
            wasDragged = false;
        }

        #endregion
    }
}
