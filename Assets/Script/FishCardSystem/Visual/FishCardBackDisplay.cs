using UnityEngine;
using UnityEngine.UI;
using ItemSystem;

namespace FishCardSystem
{
    /// <summary>
    /// 鱼类卡牌背面显示模块
    /// </summary>
    public class FishCardBackDisplay : MonoBehaviour
    {
        [Header("背面图片")]
        [SerializeField] private Image backImage;

        [Header("背面Sprite资源")]
        [SerializeField] private Sprite smallBackSprite;   // Fish_minback
        [SerializeField] private Sprite mediumBackSprite;  // Fish_mediumback
        [SerializeField] private Sprite largeBackSprite;   // Fish_maxback

        /// <summary>
        /// 根据鱼类尺寸更新背面显示
        /// </summary>
        public void UpdateDisplay(FishSize size)
        {
            if (backImage == null)
            {
                Debug.LogWarning("FishCardBackDisplay: backImage未配置");
                return;
            }

            Sprite spriteToUse = null;

            switch (size)
            {
                case FishSize.Small:
                    spriteToUse = smallBackSprite;
                    break;
                case FishSize.Medium:
                    spriteToUse = mediumBackSprite;
                    break;
                case FishSize.Large:
                    spriteToUse = largeBackSprite;
                    break;
                default:
                    Debug.LogWarning($"未知的FishSize: {size}");
                    spriteToUse = mediumBackSprite; // 默认使用中型
                    break;
            }

            if (spriteToUse != null)
            {
                backImage.sprite = spriteToUse;
            }
            else
            {
                Debug.LogWarning($"FishCardBackDisplay: 未找到尺寸 {size} 对应的背面Sprite");
            }
        }
    }
}
