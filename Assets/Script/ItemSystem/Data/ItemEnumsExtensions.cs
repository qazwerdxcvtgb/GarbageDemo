namespace ItemSystem
{
    /// <summary>
    /// 物品枚举扩展方法（中文转换）
    /// </summary>
    public static class ItemEnumsExtensions
    {
        /// <summary>
        /// 深度枚举转中文
        /// </summary>
        public static string ToChineseText(this FishDepth depth)
        {
            switch (depth)
            {
                case FishDepth.Depth1: return "深度1";
                case FishDepth.Depth2: return "深度2";
                case FishDepth.Depth3: return "深度3";
                default: return depth.ToString();
            }
        }
        
        /// <summary>
        /// 体积枚举转中文
        /// </summary>
        public static string ToChineseText(this FishSize size)
        {
            switch (size)
            {
                case FishSize.Small: return "小";
                case FishSize.Medium: return "中";
                case FishSize.Large: return "大";
                default: return size.ToString();
            }
        }
        
        /// <summary>
        /// 鱼类类型枚举转中文
        /// </summary>
        public static string ToChineseText(this FishType type)
        {
            switch (type)
            {
                case FishType.Pure: return "纯净";
                case FishType.Corrupted: return "污秽";
                default: return type.ToString();
            }
        }
        
        /// <summary>
        /// 物品类型枚举转中文
        /// </summary>
        public static string ToChineseText(this ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Fish: return "鱼类";
                case ItemCategory.Trash: return "杂鱼";
                case ItemCategory.Consumable: return "消耗品";
                case ItemCategory.Equipment: return "装备";
                default: return category.ToString();
            }
        }
        
        /// <summary>
        /// 装备槽位枚举转中文
        /// </summary>
        public static string ToChineseText(this EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.FishingRod: return "鱼竿";
                case EquipmentSlot.FishingGear: return "渔具";
                default: return slot.ToString();
            }
        }
        
        /// <summary>
        /// 被动触发时机枚举转中文
        /// </summary>
        public static string ToChineseText(this PassiveTrigger trigger)
        {
            switch (trigger)
            {
                case PassiveTrigger.OnFishing: return "钓鱼时";
                case PassiveTrigger.OnCapture: return "捕获时";
                case PassiveTrigger.OnUse: return "使用时";
                case PassiveTrigger.OnDamage: return "受伤时";
                case PassiveTrigger.Always: return "始终生效";
                default: return trigger.ToString();
            }
        }
        
        /// <summary>
        /// 效果触发时机枚举转中文
        /// </summary>
        public static string ToChineseText(this EffectTrigger trigger)
        {
            switch (trigger)
            {
                case EffectTrigger.OnReveal: return "揭示时";
                case EffectTrigger.OnCapture: return "捕获时";
                case EffectTrigger.OnUse: return "使用时";
                case EffectTrigger.OnDiscard: return "丢弃时";
                case EffectTrigger.OnEquip: return "装备时";
                case EffectTrigger.OnUnequip: return "卸下时";
                case EffectTrigger.WhileInHand: return "手牌中持续";
                default: return trigger.ToString();
            }
        }
    }
}
