/// <summary>
/// 游戏通用事件定义
/// 存放项目中所有通用的事件类型
/// 创建日期：2026-01-20
/// </summary>

using UnityEngine.Events;
using ItemSystem;

/// <summary>
/// 整数值变化事件
/// 用于传递整数类型的参数（如体力值、金币、疯狂值等）
/// </summary>
[System.Serializable]
public class IntValueChangedEvent : UnityEvent<int> { }

/// <summary>
/// 疯狂等级枚举
/// 根据疯狂值范围划分为6个等级
/// 创建日期：2026-01-21
/// </summary>
[System.Serializable]
public enum SanityLevel
{
    /// <summary>等级0：疯狂值为0</summary>
    Level0 = 0,
    
    /// <summary>等级1：疯狂值1-3</summary>
    Level1 = 1,
    
    /// <summary>等级2：疯狂值4-6</summary>
    Level2 = 2,
    
    /// <summary>等级3：疯狂值7-9</summary>
    Level3 = 3,
    
    /// <summary>等级4：疯狂值10-12</summary>
    Level4 = 4,
    
    /// <summary>等级5：疯狂值13+</summary>
    Level5 = 5
}

/// <summary>
/// 疯狂等级变化事件
/// 用于传递疯狂等级参数
/// 创建日期：2026-01-21
/// </summary>
[System.Serializable]
public class SanityLevelChangedEvent : UnityEvent<SanityLevel> { }

/// <summary>
/// 玩家深度变化事件
/// 用于传递玩家当前所处深度参数
/// 创建日期：2026-03-27
/// </summary>
[System.Serializable]
public class FishDepthChangedEvent : UnityEvent<FishDepth> { }

/// <summary>
/// 装备面板开关事件（无参数，订阅方自行决定开/关逻辑）
/// 由场景中的按钮或其他触发源调用，EquipmentPanel 订阅此事件。
/// 创建日期：2026-03-29
/// </summary>
[System.Serializable]
public class EquipmentPanelToggleEvent : UnityEvent { }

/// <summary>
/// 游戏阶段枚举
/// 定义每日循环中的各个阶段
/// 创建日期：2026-04-02
/// </summary>
[System.Serializable]
public enum GamePhase
{
    /// <summary>每日开始阶段（自动执行：天数+1、丢弃揭示牌、每日效果、深度回退）</summary>
    DayStart,

    /// <summary>刷新阶段（自动执行：恢复体力满值）</summary>
    Refresh,

    /// <summary>声明阶段（玩家选择：去商店或去钓鱼）</summary>
    Declaration,

    /// <summary>行动阶段（玩家自由行动：商店或钓鱼）</summary>
    Action,

    /// <summary>日终结算阶段（展示当日信息，等待玩家关闭）</summary>
    DayEnd,

    /// <summary>游戏结束</summary>
    GameOver
}

/// <summary>
/// 天数变化事件
/// 参数：新的天数值（1-6）
/// 创建日期：2026-04-02
/// </summary>
[System.Serializable]
public class DayChangedEvent : UnityEvent<int> { }

/// <summary>
/// 游戏阶段变化事件
/// 参数：新的游戏阶段
/// 创建日期：2026-04-02
/// </summary>
[System.Serializable]
public class GamePhaseChangedEvent : UnityEvent<GamePhase> { }
