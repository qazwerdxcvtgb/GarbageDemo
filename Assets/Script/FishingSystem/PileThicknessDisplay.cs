using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FishingSystem
{
    /// <summary>
    /// 牌堆厚度层视觉控制
    /// 在 Awake 时自动生成边缘层，根据牌堆张数动态控制显示数量
    /// </summary>
    public class PileThicknessDisplay : MonoBehaviour
    {
        [Header("生成参数")]
        [Tooltip("最多生成的边缘层数（决定厚度上限）")]
        [SerializeField][Min(1)] private int maxLayers = 8;

        [Tooltip("每层使用的 Sprite，留空则使用纯色")]
        [SerializeField] private Sprite layerSprite;

        [Tooltip("边缘层颜色")]
        [SerializeField] private Color layerColor = new Color(0.75f, 0.75f, 0.75f, 1f);

        [Tooltip("每层的宽度和高度（建议与卡牌宽度一致，高度 4~8px）")]
        [SerializeField] private Vector2 layerSize = new Vector2(220f, 6f);

        [Tooltip("每层相对上一层的位置偏移（建议向右下偏移以产生堆叠感）")]
        [SerializeField] private Vector2 layerOffset = new Vector2(1f, -4f);

        [Header("厚度计算")]
        [Tooltip("每 N 张卡显示一层，调整厚度增长速率")]
        [SerializeField][Min(1)] private int cardsPerLayer = 2;

        private readonly List<RectTransform> layers = new List<RectTransform>();

        #region Unity Lifecycle

        private void Awake()
        {
            GenerateLayers();
        }

        #endregion

        #region Layer Generation

        private void GenerateLayers()
        {
            // 清理旧层（重新生成时使用）
            foreach (var layer in layers)
            {
                if (layer != null) Destroy(layer.gameObject);
            }
            layers.Clear();

            for (int i = 0; i < maxLayers; i++)
            {
                GameObject layerObj = new GameObject($"EdgeLayer_{i + 1}");
                layerObj.transform.SetParent(transform, false);

                RectTransform rt = layerObj.AddComponent<RectTransform>();
                rt.sizeDelta = layerSize;
                // 每层依次按 layerOffset 向外偏移
                rt.anchoredPosition = layerOffset * (i + 1);

                Image img = layerObj.AddComponent<Image>();
                img.color = layerColor;
                if (layerSprite != null) img.sprite = layerSprite;

                // 将边缘层放到最底部，保证主卡牌渲染在上方
                layerObj.transform.SetAsFirstSibling();

                layerObj.SetActive(false);
                layers.Add(rt);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// 根据卡牌总数更新显示层数
        /// </summary>
        public void UpdateThickness(int cardCount)
        {
            int visibleLayers = cardCount > 0
                ? Mathf.Min(Mathf.CeilToInt((float)cardCount / cardsPerLayer), layers.Count)
                : 0;

            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i] != null)
                    layers[i].gameObject.SetActive(i < (visibleLayers-1));
            }
        }

        #endregion
    }
}
