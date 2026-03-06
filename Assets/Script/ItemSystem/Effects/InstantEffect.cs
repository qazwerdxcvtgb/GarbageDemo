namespace ItemSystem
{
    /// <summary>
    /// 即时效果基类（已废弃）
    /// 揭示/捕获/使用时触发
    /// 已被 EffectBase 替代
    /// </summary>
    [System.Obsolete("InstantEffect 已废弃，请直接继承 EffectBase。参见 Docs/效果系统迁移指南.md")]
    public abstract class InstantEffect : EffectData
    {
        // 即时执行，无需额外逻辑
        // 子类实现Execute()方法即可
    }
}
