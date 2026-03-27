using UnityEngine;
using ItemSystem;

namespace FishCardSystem
{
    /// <summary>
    /// 消耗品卡牌逻辑控制器（逻辑卡）
    /// 继承 ItemCard，负责 ConsumableData 数据绑定与视觉卡实例化
    /// </summary>
    public class ConsumableCard : ItemCard
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
            }
        }

        private void OnDestroy()
        {
            if (cardVisual != null)
                Destroy(cardVisual.gameObject);
        }

        /// <summary>
        /// 初始化消耗品卡牌数据
        /// </summary>
        public void Initialize(ConsumableData data)
        {
            base.Initialize(data);
        }

        public override void Initialize(ItemData data)
        {
            if (data is ConsumableData cd)
                Initialize(cd);
            else
                base.Initialize(data);
        }
    }
}
