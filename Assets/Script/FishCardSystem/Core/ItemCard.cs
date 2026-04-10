using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ItemSystem;
using DG.Tweening;

namespace FishCardSystem
{
    /// <summary>
    /// 手牌逻辑卡基类
    /// 负责：数据绑定基础、输入处理（拖拽/点击/选中/悬停）、状态管理、位置计算
    /// 子类负责：具体类型数据绑定、视觉卡实例化与销毁
    /// </summary>
    public class ItemCard : MonoBehaviour,
        IDragHandler, IBeginDragHandler, IEndDragHandler,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerUpHandler, IPointerDownHandler
    {
        #region Fields

        [Header("数据")]
        public ItemData cardData;

        [Header("视觉引用")]
        [HideInInspector] public FishCardVisual cardVisual;
        [HideInInspector] public Transform visualParentOverride;

        [Header("移动参数")]
        [SerializeField] private float moveSpeedLimit = 50f;
        private Vector3 dragOffset;

        [Header("选中参数")]
        public bool selected;
        public float selectionOffset = 50f;
        private float pointerDownTime;
        private float pointerUpTime;

        [Header("模式设置")]
        [SerializeField] private CardContextMode cardContextMode = CardContextMode.Basic;

        [Header("锁定状态")]
        public bool isLocked = false;

        [Header("状态")]
        public bool isHovering;
        public bool isDragging;
        [HideInInspector] public bool wasDragged;

        [Header("事件")]
        [HideInInspector] public UnityEvent<ItemCard> PointerEnterEvent;
        [HideInInspector] public UnityEvent<ItemCard> PointerExitEvent;
        [HideInInspector] public UnityEvent<ItemCard> PointerDownEvent;
        [HideInInspector] public UnityEvent<ItemCard, bool> PointerUpEvent;
        [HideInInspector] public UnityEvent<ItemCard> BeginDragEvent;
        [HideInInspector] public UnityEvent<ItemCard> EndDragEvent;
        [HideInInspector] public UnityEvent<ItemCard, bool> SelectEvent;
        [HideInInspector] public UnityEvent<bool> ActiveChangedEvent;

        private Canvas canvas;
        private Image imageComponent;

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
            imageComponent = GetComponent<Image>();

            PointerEnterEvent = new UnityEvent<ItemCard>();
            PointerExitEvent  = new UnityEvent<ItemCard>();
            PointerDownEvent  = new UnityEvent<ItemCard>();
            PointerUpEvent    = new UnityEvent<ItemCard, bool>();
            BeginDragEvent       = new UnityEvent<ItemCard>();
            EndDragEvent         = new UnityEvent<ItemCard>();
            SelectEvent          = new UnityEvent<ItemCard, bool>();
            ActiveChangedEvent   = new UnityEvent<bool>();
        }

        private void OnEnable()  => ActiveChangedEvent?.Invoke(true);
        private void OnDisable() => ActiveChangedEvent?.Invoke(false);

        protected virtual void Start()
        {
            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();
        }

        private void Update()
        {
            if (!isDragging) return;

            ClampPosition();

            Vector2 targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition) - dragOffset;
            Vector2 direction      = (targetPosition - (Vector2)transform.position).normalized;
            float   distance       = Vector2.Distance(transform.position, targetPosition);
            float   speed          = Mathf.Min(moveSpeedLimit, distance / Time.deltaTime);
            transform.Translate(direction * speed * Time.deltaTime);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 初始化基础数据（子类可 override 以执行额外操作）
        /// </summary>
        public virtual void Initialize(ItemData data)
        {
            cardData = data;
            if (cardVisual != null)
                cardVisual.UpdateCardData(data);
        }

        /// <summary>取消选中</summary>
        public void Deselect()
        {
            if (!selected) return;
            selected = false;
            transform.localPosition = Vector3.zero;
            SelectEvent.Invoke(this, false);
        }

        /// <summary>当前卡牌上下文模式</summary>
        public CardContextMode CardContextMode => cardContextMode;

        /// <summary>展示模式：禁用拖拽、选中、旋转、倾斜（Pile / Equipment / Hang）</summary>
        public bool IsDisplayOnly => cardContextMode == CardContextMode.Pile
            || cardContextMode == CardContextMode.Equipment
            || cardContextMode == CardContextMode.Hang;

        /// <summary>设置卡牌上下文模式，未赋值默认为 Basic</summary>
        public void SetContextMode(CardContextMode mode) => cardContextMode = mode;

        /// <summary>
        /// 主动归位：将卡牌动画回到槽位中心（或选中偏移位置）。
        /// 调用前请先 DOKill() 以避免与已有 Tween 冲突。
        /// 由 CrossHolderSystem 在非法落点时调用，也可供其他系统使用。
        /// </summary>
        public void ReturnToSlot(float duration = 0.15f)
        {
            Vector3 targetLocalPos = selected
                ? new Vector3(0, selectionOffset, 0)
                : Vector3.zero;

            transform.DOKill();
            transform.DOLocalMove(targetLocalPos, duration).SetEase(Ease.OutBack);
        }

        #endregion

        #region Position Calculation

        private void ClampPosition()
        {
            if (Camera.main == null) return;
            Vector2 screenBounds = Camera.main.ScreenToWorldPoint(
                new Vector3(Screen.width, Screen.height, Camera.main.transform.position.z));
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, -screenBounds.x, screenBounds.x);
            pos.y = Mathf.Clamp(pos.y, -screenBounds.y, screenBounds.y);
            transform.position = pos;
        }

        public int ParentIndex()
        {
            if (transform.parent != null && transform.parent.CompareTag("Slot"))
                return transform.parent.GetSiblingIndex();
            return 0;
        }

        public int SiblingAmount()
        {
            if (transform.parent != null && transform.parent.CompareTag("Slot")
                && transform.parent.parent != null)
                return transform.parent.parent.childCount - 1;
            return 0;
        }

        public float NormalizedPosition()
        {
            if (transform.parent != null && transform.parent.CompareTag("Slot"))
            {
                int siblingAmount = SiblingAmount();
                if (siblingAmount > 0)
                    return ((float)ParentIndex()).Remap(0, siblingAmount, 0, 1);
            }
            return 0;
        }

        #endregion

        #region Visual Parent Resolution

        /// <summary>
        /// 设置视觉卡的归属排序层级（代理到 FishCardVisual.SetHomeSortingOrder）。
        /// 传入 0 表示继承父容器（VisualCardsHandler）的层级。
        /// </summary>
        public void SetVisualHomeSortingOrder(int order)
        {
            if (cardVisual != null)
                cardVisual.SetHomeSortingOrder(order);
        }

        /// <summary>
        /// 决定视觉卡实例化后挂载的父节点
        /// 优先级：visualParentOverride → VisualCardsHandler → 最近 Canvas → 直接父节点
        /// </summary>
        protected Transform ResolveVisualParent()
        {
            if (visualParentOverride != null)
                return visualParentOverride;

            VisualCardsHandler handler = FindObjectOfType<VisualCardsHandler>();
            if (handler != null)
                return handler.transform;

            Canvas localCanvas = GetComponentInParent<Canvas>();
            return localCanvas != null ? localCanvas.transform : transform.parent;
        }

        #endregion

        #region Input Event Handlers

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isLocked) return;
            isHovering = true;
            PointerEnterEvent.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (isLocked) return;
            isHovering = false;
            PointerExitEvent.Invoke(this);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (isLocked) return;
            if (eventData.button != PointerEventData.InputButton.Left) return;
            pointerDownTime = Time.time;
            PointerDownEvent.Invoke(this);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (isLocked) return;
            if (eventData.button != PointerEventData.InputButton.Left) return;

            pointerUpTime = Time.time;
            bool longPress = (pointerUpTime - pointerDownTime) > 0.2f;
            PointerUpEvent.Invoke(this, longPress);

            if (longPress || wasDragged) return;

            if (IsDisplayOnly)
            {
                SelectEvent.Invoke(this, false);
                return;
            }

            selected = !selected;
            SelectEvent.Invoke(this, selected);

            if (selected)
            {
                transform.localPosition += cardVisual != null
                    ? cardVisual.transform.up * selectionOffset
                    : Vector3.up * selectionOffset;
            }
            else
            {
                transform.localPosition = Vector3.zero;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isLocked || cardContextMode == CardContextMode.Pile || Camera.main == null) return;

            BeginDragEvent.Invoke(this);

            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            dragOffset = mousePosition - (Vector2)transform.position;
            isDragging = true;
            wasDragged = true;

            if (canvas != null)
            {
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster != null) raycaster.enabled = false;
            }
            if (imageComponent != null) imageComponent.raycastTarget = false;
        }

        public void OnDrag(PointerEventData eventData) { }

        public void OnEndDrag(PointerEventData eventData)
        {
            EndDragEvent.Invoke(this);
            isDragging = false;

            if (canvas != null)
            {
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster != null) raycaster.enabled = true;
            }
            if (imageComponent != null) imageComponent.raycastTarget = true;

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
