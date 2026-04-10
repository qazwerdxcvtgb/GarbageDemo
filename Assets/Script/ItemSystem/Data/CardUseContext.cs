namespace ItemSystem
{
    /// <summary>
    /// 卡牌使用场合（Flags 枚举）
    /// 用于 Inspector 中配置卡牌在哪些行动阶段可以被使用
    /// </summary>
    [System.Flags]
    public enum CardUseContext
    {
        None     = 0,
        Fishing  = 1 << 0,
        Shopping = 1 << 1,
        All      = Fishing | Shopping
    }
}
