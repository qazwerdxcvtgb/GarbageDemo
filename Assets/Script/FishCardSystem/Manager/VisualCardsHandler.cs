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
            }
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
