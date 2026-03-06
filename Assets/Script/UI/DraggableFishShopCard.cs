/// <summary>
/// 可拖拽的鱼店卡牌组件
/// 创建日期：2026-01-21
/// 功能：允许从鱼店拖拽卡牌回到手牌
/// </summary>

using UnityEngine;
using UnityEngine.EventSystems;
using ItemSystem;
using HandSystem;

namespace UISystem
{
    /// <summary>
    /// 可拖拽的鱼店卡牌组件
    /// 附加到FishShopCardSlot上，使其可以拖拽回手牌
    /// </summary>
    [RequireComponent(typeof(FishShopCardSlot))]
    [RequireComponent(typeof(CanvasGroup))]
    public class DraggableFishShopCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        #region 组件引用

        private FishShopCardSlot cardSlot;
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Canvas canvas;

        #endregion

        #region 拖拽数据

        private GameObject dragPreview;
        private Vector3 originalPosition;
        private Transform originalParent;

        #endregion

        #region Unity生命周期

        void Awake()
        {
            // 获取组件引用
            cardSlot = GetComponent<FishShopCardSlot>();
            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();
        }

        void Start()
        {
            // 获取Canvas引用（需要在Start中获取，确保Canvas已初始化）
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[DraggableFishShopCard] 未找到父级Canvas");
            }
        }

        #endregion

        #region 拖拽事件

        /// <summary>
        /// 开始拖拽
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (canvas == null)
            {
                Debug.LogError("[DraggableFishShopCard] Canvas未初始化");
                return;
            }

            // 检查手牌面板是否打开
            if (HandUIPanel.Instance == null || !HandUIPanel.Instance.isOpen)
            {
                Debug.Log("[DraggableFishShopCard] 手牌面板未打开，无法拖拽");
                return;
            }

            // 获取鱼类数据
            FishData fish = cardSlot.GetCardData();
            if (fish == null)
            {
                Debug.LogWarning("[DraggableFishShopCard] 鱼类数据为空");
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

            // 禁用预览的DraggableFishShopCard组件（避免递归）
            DraggableFishShopCard previewDraggable = dragPreview.GetComponent<DraggableFishShopCard>();
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

            Debug.Log($"[DraggableFishShopCard] 开始拖拽: {fish.itemName}");
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

            // 检测是否在手牌面板区域内
            bool addedToHand = false;

            if (HandUIPanel.Instance != null)
            {
                // 使用handListPanel作为检测区域（更大的区域，更容易拖拽）
                RectTransform targetRect = null;
                if (HandUIPanel.Instance.handListPanelRect != null)
                {
                    targetRect = HandUIPanel.Instance.handListPanelRect;
                    Debug.Log("[DraggableFishShopCard] 使用handListPanel作为检测区域");
                }
                else if (HandUIPanel.Instance.cardContainer != null)
                {
                    targetRect = HandUIPanel.Instance.cardContainer.GetComponent<RectTransform>();
                    Debug.Log("[DraggableFishShopCard] 使用cardContainer作为检测区域");
                }

                if (targetRect != null)
                {
                    // 检测鼠标是否在目标区域内
                    bool isOverHand = RectTransformUtility.RectangleContainsScreenPoint(
                        targetRect,
                        eventData.position,
                        eventData.pressEventCamera
                    );

                    if (isOverHand)
                    {
                        // 在手牌区域内，添加回手牌
                        FishData fish = cardSlot.GetCardData();
                        if (fish != null)
                        {
                            // 从鱼店移除
                            if (FishShopPanel.Instance != null)
                            {
                                FishShopPanel.Instance.RemoveFish(fish);
                            }

                            // 添加回手牌管理器
                            if (HandManager.Instance != null)
                            {
                                HandManager.Instance.AddCard(fish);
                                addedToHand = true;
                                Debug.Log($"[DraggableFishShopCard] 放回手牌: {fish.itemName}");
                            }
                            else
                            {
                                Debug.LogError("[DraggableFishShopCard] HandManager.Instance为空");
                            }
                        }
                    }
                    else
                    {
                        Debug.Log($"[DraggableFishShopCard] 鼠标位置: {eventData.position}, 不在手牌区域内");
                    }
                }
            }

            if (!addedToHand)
            {
                Debug.Log("[DraggableFishShopCard] 拖拽取消，未放回手牌");
            }

            // 销毁拖拽预览
            Destroy(dragPreview);
            dragPreview = null;
        }

        #endregion
    }
}
