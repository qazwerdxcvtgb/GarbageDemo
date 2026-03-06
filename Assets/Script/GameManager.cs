/// <summary>
/// 游戏管理器
/// 使用单例模式管理全局游戏事件和状态
/// 创建日期：2026-01-19
/// 最后更新：2026-01-20（添加疯狂值系统）
/// </summary>

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 游戏管理器 - 单例模式
/// 负责管理全局事件系统和全局游戏状态
/// 使用DontDestroyOnLoad确保场景切换时不被销毁
/// </summary>
public class GameManager : MonoBehaviour
{
    #region 单例模式
    
    /// <summary>
    /// 单例实例
    /// </summary>
    public static GameManager Instance { get; private set; }
    
    #endregion
    
    #region 全局事件系统

    /// <summary>
    /// 交互按钮按下事件
    /// 当玩家按下交互键（F键或手柄X键）时触发
    /// 其他系统可以通过Unity事件系统订阅此事件
    /// </summary>
    [Header("输入事件")]
    [Tooltip("玩家按下交互按钮时触发的事件")]
    public UnityEvent OnInteractionPressed;
    
    /// <summary>
    /// 疯狂值变化事件
    /// </summary>
    [Header("状态变化事件")]
    [Tooltip("疯狂值变化时触发（参数：新的疯狂值）")]
    public IntValueChangedEvent OnSanityChanged;
    
    /// <summary>
    /// 疯狂等级变化事件
    /// </summary>
    [Tooltip("疯狂等级变化时触发（参数：新的疯狂等级）")]
    public SanityLevelChangedEvent OnSanityLevelChanged;
    
    #endregion
    
    #region 全局游戏状态
    
    /// <summary>
    /// 当前疯狂值
    /// </summary>
    [Header("全局游戏状态")]
    [Tooltip("当前疯狂值（范围：>=0，无上限）")]
    [SerializeField] private int sanityValue = 0;
    
    /// <summary>
    /// 当前疯狂等级
    /// </summary>
    [Tooltip("当前疯狂等级（0-5级）")]
    [SerializeField] private SanityLevel currentSanityLevel = SanityLevel.Level0;
    
    #endregion
    
    #region 疯狂值属性
    
    /// <summary>
    /// 疯狂值属性（带数据验证）
    /// 设置时自动确保不为负数，并触发变化事件
    /// </summary>
    public int SanityValue
    {
        get => sanityValue;
        set
        {
            // 确保不为负数
            if (value < 0) value = 0;
            if (sanityValue != value)
            {
                sanityValue = value;
                OnSanityChanged?.Invoke(sanityValue);
                Debug.Log($"[GameManager] 疯狂值变化为: {sanityValue}");
                
                // 检查疯狂等级是否变化
                UpdateSanityLevel();
            }
        }
    }
    
    /// <summary>
    /// 当前疯狂等级属性（只读）
    /// </summary>
    public SanityLevel CurrentSanityLevel
    {
        get => currentSanityLevel;
        private set
        {
            if (currentSanityLevel != value)
            {
                currentSanityLevel = value;
                OnSanityLevelChanged?.Invoke(currentSanityLevel);
                Debug.Log($"[GameManager] 疯狂等级变化为: {currentSanityLevel} ({GetSanityLevelDescription(currentSanityLevel)})");
            }
        }
    }
    
    #endregion
    
    #region Unity生命周期

    /// <summary>
    /// 初始化方法 - 确保单例唯一性
    /// 如果场景中已存在GameManager实例，则销毁当前对象
    /// 否则将当前对象设为单例实例并设置为跨场景持久化
    /// </summary>
    void Awake()
    {
        // 确保单例唯一性
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 场景切换时不销毁
            
            // 初始化事件
            if (OnInteractionPressed == null) OnInteractionPressed = new UnityEvent();
            if (OnSanityChanged == null) OnSanityChanged = new IntValueChangedEvent();
            if (OnSanityLevelChanged == null) OnSanityLevelChanged = new SanityLevelChangedEvent();
        }
        else if (Instance != this)
        {
            Destroy(gameObject); // 销毁重复的实例
        }
    }
    
    #endregion
    
    #region 交互事件方法

    /// <summary>
    /// 触发交互按钮按下事件
    /// 供PlayerMove等系统调用，通知所有订阅者玩家按下了交互键
    /// </summary>
    public void TriggerInteractionPressed()
    {
        // 使用?.安全调用操作符，避免事件为null时报错
        OnInteractionPressed?.Invoke();
    }
    
    #endregion
    
    #region 疯狂值管理方法
    
    /// <summary>
    /// 修改疯狂值（增加或减少）
    /// </summary>
    /// <param name="amount">变化量（正数为增加，负数为减少）</param>
    public void ModifySanity(int amount)
    {
        SanityValue += amount;
        Debug.Log($"[GameManager] 疯狂值修改: {amount:+#;-#;0}，当前疯狂值: {sanityValue}");
    }
    
    /// <summary>
    /// 直接设置疯狂值
    /// </summary>
    /// <param name="value">新的疯狂值</param>
    public void SetSanity(int value)
    {
        SanityValue = value;
        Debug.Log($"[GameManager] 疯狂值设置为: {sanityValue}");
    }
    
    /// <summary>
    /// 获取当前疯狂值
    /// </summary>
    /// <returns>当前疯狂值</returns>
    public int GetSanity()
    {
        return sanityValue;
    }
    
    /// <summary>
    /// 重置疯狂值为0
    /// </summary>
    public void ResetSanity()
    {
        SanityValue = 0;
        Debug.Log($"[GameManager] 疯狂值已重置为0");
    }
    
    #endregion
    
    #region 疯狂等级管理方法
    
    /// <summary>
    /// 根据当前疯狂值计算疯狂等级
    /// </summary>
    /// <param name="sanity">疯狂值</param>
    /// <returns>对应的疯狂等级</returns>
    public SanityLevel CalculateSanityLevel(int sanity)
    {
        if (sanity == 0) return SanityLevel.Level0;
        if (sanity >= 1 && sanity <= 3) return SanityLevel.Level1;
        if (sanity >= 4 && sanity <= 6) return SanityLevel.Level2;
        if (sanity >= 7 && sanity <= 9) return SanityLevel.Level3;
        if (sanity >= 10 && sanity <= 12) return SanityLevel.Level4;
        return SanityLevel.Level5; // 13+
    }
    
    /// <summary>
    /// 更新疯狂等级（在疯狂值变化时自动调用）
    /// </summary>
    private void UpdateSanityLevel()
    {
        SanityLevel newLevel = CalculateSanityLevel(sanityValue);
        CurrentSanityLevel = newLevel;
    }
    
    /// <summary>
    /// 获取当前疯狂等级
    /// </summary>
    /// <returns>当前疯狂等级</returns>
    public SanityLevel GetSanityLevel()
    {
        return currentSanityLevel;
    }
    
    /// <summary>
    /// 获取疯狂等级描述
    /// </summary>
    /// <param name="level">疯狂等级</param>
    /// <returns>等级描述文本</returns>
    public string GetSanityLevelDescription(SanityLevel level)
    {
        switch (level)
        {
            case SanityLevel.Level0: return "疯狂值:0";
            case SanityLevel.Level1: return "疯狂值:1-3";
            case SanityLevel.Level2: return "疯狂值:4-6";
            case SanityLevel.Level3: return "疯狂值:7-9";
            case SanityLevel.Level4: return "疯狂值:10-12";
            case SanityLevel.Level5: return "疯狂值:13+";
            default: return "未知等级";
        }
    }
    
    #endregion
    
    #region 空牌统计（2026-01-26新增）

    /// <summary>
    /// 玩家抽取的空牌总数
    /// </summary>
    public int EmptyCardDrawnCount { get; private set; }

    /// <summary>
    /// 记录抽取的空牌数量
    /// </summary>
    /// <param name="count">本次抽取的空牌数量</param>
    public void RecordEmptyCardDrawn(int count)
    {
        if (count <= 0) return;
        
        EmptyCardDrawnCount += count;
        Debug.Log($"[GameManager] 记录空牌抽取：+{count}，总计：{EmptyCardDrawnCount}");
    }

    /// <summary>
    /// 获取空牌抽取总数
    /// </summary>
    public int GetEmptyCardDrawnCount()
    {
        return EmptyCardDrawnCount;
    }

    /// <summary>
    /// 重置空牌统计（调试或新游戏时使用）
    /// </summary>
    public void ResetEmptyCardCount()
    {
        EmptyCardDrawnCount = 0;
        Debug.Log("[GameManager] 重置空牌统计");
    }

    #endregion
}
