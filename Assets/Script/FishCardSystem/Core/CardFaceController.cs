using UnityEngine;

namespace FishCardSystem
{
    /// <summary>
    /// 卡牌正反面控制器
    /// </summary>
    public class CardFaceController : MonoBehaviour
    {
        [Header("正反面引用")]
        [SerializeField] private GameObject frontFace;
        [SerializeField] private GameObject backFace;

        private bool isFrontVisible;

        /// <summary>
        /// 初始化时显示背面
        /// </summary>
        private void Start()
        {
            ShowBack();
        }

        /// <summary>
        /// 显示正面
        /// </summary>
        public void ShowFront()
        {
            SetFaceVisible(true, false);
        }

        /// <summary>
        /// 显示背面
        /// </summary>
        public void ShowBack()
        {
            SetFaceVisible(false, false);
        }

        /// <summary>
        /// 设置正反面可见性
        /// </summary>
        /// <param name="showFront">是否显示正面</param>
        /// <param name="immediate">是否立即切换（不带动画）</param>
        public void SetFaceVisible(bool showFront, bool immediate)
        {
            if (isFrontVisible == showFront && !immediate)
                return;

            isFrontVisible = showFront;

            if (frontFace != null)
                frontFace.SetActive(showFront);

            if (backFace != null)
                backFace.SetActive(!showFront);
        }

        /// <summary>
        /// 获取当前是否显示正面
        /// </summary>
        public bool IsFrontVisible => isFrontVisible;
    }
}
