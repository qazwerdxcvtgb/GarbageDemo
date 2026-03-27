using UnityEngine;
using ItemSystem;

namespace FishCardSystem
{
    /// <summary>
    /// 鱼类卡牌逻辑控制器（逻辑卡）
    /// 继承 ItemCard，负责 FishData 数据绑定与视觉卡实例化
    /// </summary>
    public class FishCard : ItemCard
    {
        [Header("视觉设置")]
        [SerializeField] private bool instantiateVisual = true;
        [SerializeField] private GameObject cardVisualPrefab;

        protected override void Start()
        {
            base.Start();

            if (instantiateVisual && cardVisualPrefab != null)
            {
                cardVisual = Instantiate(cardVisualPrefab, ResolveVisualParent()).GetComponent<FishCardVisual>();
                cardVisual.Initialize(this);
                cardVisual.UpdateIndex();
                if (CardContextMode == CardContextMode.Pile)
                    cardVisual.EnableFrontEffectDisplay();
            }
        }

        private void OnDestroy()
        {
            if (cardVisual != null)
                Destroy(cardVisual.gameObject);
        }

        /// <summary>
        /// 初始化鱼类卡牌数据
        /// </summary>
        public void Initialize(FishData data)
        {
            base.Initialize(data);
        }

        public override void Initialize(ItemData data)
        {
            if (data is FishData fd)
                Initialize(fd);
            else
                base.Initialize(data);
        }
    }
}
