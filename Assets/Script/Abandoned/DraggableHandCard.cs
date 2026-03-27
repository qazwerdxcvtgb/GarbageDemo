/// <summary>
/// 可拖拽手牌组件
/// 创建日期：2026-01-21
/// 功能：实现手牌卡牌拖拽到鱼店面板
/// </summary>

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ItemSystem;

namespace UISystem
{
    /// <summary>
    /// 可拖拽手牌组件，实现拖拽交互
    /// </summary>
    [System.Obsolete("此脚本已废弃，不再使用。保留仅供历史参考。")]
    [RequireComponent(typeof(HandCardButton))]
    public class DraggableHandCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("组件引用")]
        private HandCardButton handCardButton;
        private Canvas canvas;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;

        [Header("拖拽状态")]
        private GameObject dragPreview;
        private Vector3 originalPosition;
        private Transform originalParent;

        void Awake()
        {
            // 获取必需的本地组件
            handCardButton = GetComponent<HandCardButton>();
            if (handCardButton == null)
            {
                Debug.LogError("[DraggableHandCard] 未找到HandCardButton组件");
                enabled = false;
                return;
            }

            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                Debug.LogError("[DraggableHandCard] 未找到RectTransform组件");
                enabled = false;
                return;
            }

            // 获取或添加CanvasGroup
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        void Start()
        {
            // 在Start中获取可能依赖初始化顺序的组件
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[DraggableHandCard] 未找到Canvas组件");
                enabled = false;
            }
        }

        /// <summary>
        /// 开始拖拽
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            // 检查FishShopPanel是否存在
            if (FishShopPanel.Instance == null)
            {
                Debug.LogWarning("[DraggableHandCard] FishShopPanel.Instance为空，无法拖拽");
                return;
            }

            // 检查鱼店面板是否打开
            if (FishShopPanel.Instance.panelRoot == null || !FishShopPanel.Instance.panelRoot.activeSelf)
            {
                Debug.Log("[DraggableHandCard] 鱼店面板未打开，无法拖拽");
                return;
            }

            // 获取物品数据
            ItemData item = handCardButton.GetCardData();
            if (item == null)
            {
                Debug.LogWarning("[DraggableHandCard] 物品数据为空");
                return;
            }
            
            // 类型检查：只能拖拽鱼类到鱼店
            if (!(item is FishData))
            {
                Debug.LogWarning($"[DraggableHandCard] 只能将鱼类拖到鱼店，当前物品类型: {item.category.ToChineseText()}");
                return;
            }

            // 记录原始位置和父对象
            originalPosition = rectTransform.position;
            originalParent = transform.parent;

            // 创建拖拽预览
            dragPreview = Instantiate(gameObject, canvas.transform);

            // 设置为Canvas的最后一个子对象（确保在最上层）
            dragPreview.transform.SetAsLastSibling();

            // 添加独立的Canvas组件以确保显示在最顶层
            Canvas previewCanvas = dragPreview.GetComponent<Canvas>();
            if (previewCanvas == null)
            {
                previewCanvas = dragPreview.AddComponent<Canvas>();
            }
            previewCanvas.overrideSorting = true;
            previewCanvas.sortingOrder = 1000; // 设置非常高的sortOrder确保在最上层

            // 添加GraphicRaycaster（Canvas需要）
            if (dragPreview.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            {
                dragPreview.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            // 禁用预览的DraggableHandCard组件（避免递归）
            DraggableHandCard previewDraggable = dragPreview.GetComponent<DraggableHandCard>();
            if (previewDraggable != null)
            {
                previewDraggable.enabled = false;
            }

            // 设置预览的透明度
            CanvasGroup previewCanvasGroup = dragPreview.GetComponent<CanvasGroup>();
            if (previewCanvasGroup != null)
            {
                previewCanvasGroup.alpha = 0.6f;
                previewCanvasGroup.blocksRaycasts = false;
            }

            // 隐藏原始对象（半透明）
            canvasGroup.alpha = 0.3f;

            Debug.Log($"[DraggableHandCard] 开始拖拽: {item.itemName}");
        }

        /// <summary>
        /// 拖拽中
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            // 如果预览不存在，不执行
            if (dragPreview == null)
            {
                return;
            }

            // 更新拖拽预览位置
            dragPreview.transform.position = eventData.position;
        }

        /// <summary>
        /// 结束拖拽
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            // 恢复原始对象显示
            canvasGroup.alpha = 1.0f;

            // 如果预览不存在，不执行
            if (dragPreview == null)
            {
                return;
            }

            // 检测是否在鱼店面板的拖拽区域内
            bool addedToShop = false;

            if (FishShopPanel.Instance != null)
            {
                // 优先使用dropZone（Viewport），如果没有配置则使用cardContainer
                RectTransform targetRect = FishShopPanel.Instance.dropZone;
                if (targetRect == null && FishShopPanel.Instance.cardContainer != null)
                {
                    targetRect = FishShopPanel.Instance.cardContainer.GetComponent<RectTransform>();
                }

                if (targetRect != null)
                {
                    // 检测鼠标是否在目标区域内
                    bool isOverShop = RectTransformUtility.RectangleContainsScreenPoint(
                        targetRect,
                        eventData.position,
                        eventData.pressEventCamera
                    );

                    if (isOverShop)
                    {
                        // 在鱼店区域内，添加到鱼店
                        ItemData item = handCardButton.GetCardData();
                        if (item != null)
                        {
                            FishShopPanel.Instance.AddFish(item);
                            addedToShop = true;
                            
                            // 从手牌管理器中移除卡牌
                            if (HandSystem.HandManager.Instance != null)
                            {
                                HandSystem.HandManager.Instance.RemoveCard(item);
                                Debug.Log($"[DraggableHandCard] 放入鱼店并从手牌移除: {item.itemName}");
                            }
                            else
                            {
                                Debug.LogError("[DraggableHandCard] HandManager.Instance为空");
                            }
                        }
                    }
                    else
                    {
                        Debug.Log($"[DraggableHandCard] 鼠标位置: {eventData.position}, 不在鱼店区域内");
                    }
                }
                else
                {
                    Debug.LogWarning("[DraggableHandCard] 鱼店面板的dropZone和cardContainer都未配置");
                }
            }

            if (!addedToShop)
            {
                Debug.Log("[DraggableHandCard] 拖拽取消，未放入鱼店");
            }

            // 销毁拖拽预览
            Destroy(dragPreview);
            dragPreview = null;
        }
    }
}
