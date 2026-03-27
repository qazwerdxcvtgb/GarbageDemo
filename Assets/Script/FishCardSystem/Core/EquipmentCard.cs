using UnityEngine;
using ItemSystem;

namespace FishCardSystem
{
    /// <summary>
    /// 装备卡牌逻辑控制器（逻辑卡）
    /// 继承 ItemCard，负责 EquipmentData 数据绑定与视觉卡实例化。
    /// 手牌中的装备卡可通过 CrossHolderSystem 拖入 EquipmentSlotUI 完成装备。
    /// </summary>
    public class EquipmentCard : ItemCard
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
        /// 初始化装备卡牌数据
        /// </summary>
        public void Initialize(EquipmentData data)
        {
            base.Initialize(data);
        }

        public override void Initialize(ItemData data)
        {
            if (data is EquipmentData ed)
                Initialize(ed);
            else
                base.Initialize(data);
        }
    }
}
