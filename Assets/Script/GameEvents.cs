/// <summary>
/// 游戏通用事件定义
/// 存放项目中所有通用的事件类型
/// 创建日期：2026-01-20
/// </summary>

using UnityEngine.Events;

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
