using UnityEngine;

namespace FishCardSystem
{
    /// <summary>
    /// 视觉卡管理器（单例）
    /// 作为所有FishCardVisual实例的父容器
    /// </summary>
    public class VisualCardsHandler : MonoBehaviour
    {
        public static VisualCardsHandler Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning("场景中存在多个VisualCardsHandler实例，销毁多余的");
                Destroy(gameObject);
                return;
            }

            // 确保手牌视觉层渲染在所有面板之上（HandPanel=180，CardPilePanel=160）
            // 拖拽时视觉卡自身会临时设置 sortingOrder=200 置顶，此处设置静止时的层级
            Canvas handlerCanvas = GetComponent<Canvas>();
            if (handlerCanvas == null)
                handlerCanvas = gameObject.AddComponent<Canvas>();

            handlerCanvas.overrideSorting = true;
            handlerCanvas.sortingOrder    = 190;

            // overrideSorting Canvas 必须配套 GraphicRaycaster 才能正确处理输入
            if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
