namespace FishCardSystem
{
    /// <summary>
    /// 卡牌上下文模式，标识卡牌在何种场景下被实例化使用。
    /// 默认值 Basic = 0，未显式赋值的卡牌自动保持完整交互模式。
    /// Basic / Hand → 完整交互（可拖拽、选中、旋转、倾斜）
    /// Pile / Equipment / Hang → 展示模式（禁用拖拽、选中、旋转、倾斜）
    /// </summary>
    public enum CardContextMode
    {
        Basic     = 0,  // 默认，完整交互
        Hand      = 1,  // 手牌，完整交互
        Pile      = 2,  // 钓鱼牌堆，展示模式
        Equipment = 3,  // 装备槽，展示模式
        Hang      = 4   // 商店悬挂，展示模式
    }
}
