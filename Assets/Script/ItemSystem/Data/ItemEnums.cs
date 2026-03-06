namespace ItemSystem
{
    /// <summary>
    /// 物品大类
    /// </summary>
    public enum ItemCategory
    {
        Fish,           // 鱼类
        Trash,          // 杂鱼
        Consumable,     // 消耗品
        Equipment       // 装备
    }
    
    /// <summary>
    /// 鱼类深度
    /// </summary>
    public enum FishDepth
    {
        Depth1,
        Depth2,
        Depth3
    }
    
    /// <summary>
    /// 鱼类体积
    /// </summary>
    public enum FishSize
    {
        Small,
        Medium,
        Large
    }
    
    /// <summary>
    /// 鱼类类型
    /// </summary>
    public enum FishType
    {
        Pure,           // 纯净
        Corrupted       // 污秽
    }
    
    /// <summary>
    /// 装备槽位
    /// </summary>
    public enum EquipmentSlot
    {
        FishingRod,     // 鱼竿
        FishingGear     // 渔具
    }
    
    /// <summary>
    /// 被动效果触发时机
    /// </summary>
    public enum PassiveTrigger
    {
        OnFishing,      // 钓鱼时
        OnCapture,      // 捕获时
        OnUse,          // 使用物品时
        OnDamage,       // 受到伤害时
        Always          // 始终生效（属性加成）
    }
}
