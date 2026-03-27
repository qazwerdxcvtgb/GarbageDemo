/// <summary>
/// 角色状态管理器
/// 负责管理玩家的体力值和金币等状态数据
/// 创建日期：2026-01-20
/// </summary>

using UnityEngine;
using UnityEngine.Events;
using ItemSystem;

/// <summary>
/// 角色状态类
/// 挂载到玩家GameObject上，管理体力值、金币等状态
/// </summary>
public class CharacterState : MonoBehaviour
{
    #region 状态数据字段
    
    [Header("体力值设置")]
    [Tooltip("体力值最大值")]
    [SerializeField] private int maxHealth = 10;
    
    [Tooltip("当前体力值")]
    [SerializeField] private int currentHealth;
    
    [Header("金币设置")]
    [Tooltip("当前金币数量")]
    [SerializeField] private int goldAmount = 0;
    
    [Header("深度设置")]
    [Tooltip("玩家当前所处深度等级")]
    [SerializeField] private FishDepth currentDepth = FishDepth.Depth1;
    
    #endregion
    
    #region 测试功能
    
    [Header("测试功能")]
    [Tooltip("测试用金币数量（在Inspector中右键组件选择测试菜单）")]
    public int testGoldAmount = 10;
    
    [Tooltip("测试用体力数量")]
    public int testHealthAmount = 5;
    
    [Tooltip("测试用最大体力数量")]
    public int testMaxHealthAmount = 5;
    
    #endregion
    
    #region 事件系统
    
    [Header("事件系统")]
    [Tooltip("当前体力值变化时触发（参数：新的体力值）")]
    public IntValueChangedEvent OnHealthChanged;
    
    [Tooltip("最大体力值变化时触发（参数：新的最大体力值）")]
    public IntValueChangedEvent OnMaxHealthChanged;
    
    [Tooltip("金币数量变化时触发（参数：新的金币数量）")]
    public IntValueChangedEvent OnGoldChanged;
    
    [Tooltip("玩家深度变化时触发（参数：新的深度等级）")]
    public FishDepthChangedEvent OnDepthChanged;
    
    #endregion
    
    #region 属性访问器
    
    /// <summary>
    /// 体力值最大值（属性）
    /// </summary>
    public int MaxHealth
    {
        get => maxHealth;
        set
        {
            if (value < 0) value = 0;
            if (maxHealth != value)
            {
                maxHealth = value;
                // 如果当前体力超过新的最大值，调整当前体力
                if (currentHealth > maxHealth)
                {
                    CurrentHealth = maxHealth;
                }
                OnMaxHealthChanged?.Invoke(maxHealth);
            }
        }
    }
    
    /// <summary>
    /// 当前体力值（属性）
    /// </summary>
    public int CurrentHealth
    {
        get => currentHealth;
        set
        {
            // 限制在0到最大值之间
            value = Mathf.Clamp(value, 0, maxHealth);
            if (currentHealth != value)
            {
                currentHealth = value;
                OnHealthChanged?.Invoke(currentHealth);
            }
        }
    }
    
    /// <summary>
    /// 金币数量（属性）
    /// </summary>
    public int GoldAmount
    {
        get => goldAmount;
        set
        {
            if (value < 0) value = 0;
            if (goldAmount != value)
            {
                goldAmount = value;
                OnGoldChanged?.Invoke(goldAmount);
            }
        }
    }
    
    /// <summary>
    /// 玩家当前深度（属性）
    /// </summary>
    public FishDepth CurrentDepth
    {
        get => currentDepth;
        set
        {
            if (currentDepth != value)
            {
                currentDepth = value;
                OnDepthChanged?.Invoke(currentDepth);
            }
        }
    }
    
    #endregion
    
    /// <summary>
    /// 初始最大体力值（Awake时记录，用于游戏重置时恢复）
    /// </summary>
    private int initialMaxHealth;

    #region Unity生命周期
    
    /// <summary>
    /// 初始化
    /// </summary>
    private void Awake()
    {
        initialMaxHealth = maxHealth;
        currentHealth = maxHealth;
        
        // 初始化事件
        if (OnHealthChanged == null) OnHealthChanged = new IntValueChangedEvent();
        if (OnMaxHealthChanged == null) OnMaxHealthChanged = new IntValueChangedEvent();
        if (OnGoldChanged == null) OnGoldChanged = new IntValueChangedEvent();
        if (OnDepthChanged == null) OnDepthChanged = new FishDepthChangedEvent();
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 修改体力值（增加或减少）
    /// </summary>
    /// <param name="amount">变化量（正数为增加，负数为减少）</param>
    public void ModifyHealth(int amount)
    {
        CurrentHealth += amount;
        Debug.Log($"[CharacterState] 体力值变化: {amount:+#;-#;0}，当前体力: {currentHealth}/{maxHealth}");
    }
    
    /// <summary>
    /// 修改最大体力值
    /// </summary>
    /// <param name="amount">变化量（正数为增加，负数为减少）</param>
    public void ModifyMaxHealth(int amount)
    {
        MaxHealth += amount;
        Debug.Log($"[CharacterState] 最大体力值变化: {amount:+#;-#;0}，新的最大体力: {maxHealth}");
    }
    
    /// <summary>
    /// 修改金币数量（增加或减少）
    /// </summary>
    /// <param name="amount">变化量（正数为增加，负数为减少）</param>
    public void ModifyGold(int amount)
    {
        GoldAmount += amount;
        Debug.Log($"[CharacterState] 金币变化: {amount:+#;-#;0}，当前金币: {goldAmount}");
    }
    
    /// <summary>
    /// 恢复满体力
    /// </summary>
    public void RestoreFullHealth()
    {
        CurrentHealth = maxHealth;
        Debug.Log($"[CharacterState] 体力已恢复满值: {currentHealth}/{maxHealth}");
    }
    
    /// <summary>
    /// 设置玩家深度等级
    /// </summary>
    /// <param name="depth">目标深度等级</param>
    public void SetDepth(FishDepth depth)
    {
        CurrentDepth = depth;
        Debug.Log($"[CharacterState] 深度变化：{currentDepth}");
    }
    
    /// <summary>
    /// 检查玩家是否可以访问指定深度的牌堆
    /// </summary>
    /// <param name="pileDepth">牌堆深度</param>
    /// <returns>当前深度 >= 牌堆深度时返回 true</returns>
    public bool CanAccessDepth(FishDepth pileDepth)
    {
        return (int)currentDepth >= (int)pileDepth;
    }
    
    /// <summary>
    /// 检查是否有足够的金币
    /// </summary>
    /// <param name="amount">需要的金币数量</param>
    /// <returns>是否足够</returns>
    public bool HasEnoughGold(int amount)
    {
        return goldAmount >= amount;
    }
    
    /// <summary>
    /// 检查是否体力已满
    /// </summary>
    /// <returns>体力是否已满</returns>
    public bool IsHealthFull()
    {
        return currentHealth >= maxHealth;
    }
    
    /// <summary>
    /// 检查角色是否已死亡（体力为0）
    /// </summary>
    /// <returns>是否死亡</returns>
    public bool IsDead()
    {
        return currentHealth <= 0;
    }
    
    #endregion
    
    #region 游戏重置（2026-04-02新增）

    /// <summary>
    /// 重置角色状态到初始值（新游戏时由 DayManager 调用）
    /// 最大体力恢复为初始值、当前体力恢复满值、金币归零、深度设为Depth1
    /// </summary>
    public void ResetState()
    {
        MaxHealth = initialMaxHealth;
        CurrentHealth = initialMaxHealth;
        GoldAmount = 0;
        CurrentDepth = FishDepth.Depth1;
        Debug.Log($"[CharacterState] 角色状态已重置: 体力={currentHealth}/{maxHealth}, 金币={goldAmount}, 深度={currentDepth}");
    }

    #endregion

    #region 调试方法
    
    /// <summary>
    /// 打印当前状态信息（调试用）
    /// </summary>
    public void DebugPrintState()
    {
        Debug.Log($"===== 角色状态信息 =====");
        Debug.Log($"体力: {currentHealth}/{maxHealth}");
        Debug.Log($"金币: {goldAmount}");
        Debug.Log($"深度: {currentDepth}");
        Debug.Log($"========================");
    }
    
    #endregion
    
    #region 测试方法
    
    /// <summary>
    /// 测试增加金币
    /// 在Inspector中右键组件 → 测试/增加金币
    /// </summary>
    [ContextMenu("测试/增加金币")]
    private void TestAddGold()
    {
        ModifyGold(testGoldAmount);
        Debug.Log($"[测试] 增加 {testGoldAmount} 金币");
    }
    
    /// <summary>
    /// 测试减少金币
    /// 在Inspector中右键组件 → 测试/减少金币
    /// </summary>
    [ContextMenu("测试/减少金币")]
    private void TestReduceGold()
    {
        ModifyGold(-testGoldAmount);
        Debug.Log($"[测试] 减少 {testGoldAmount} 金币");
    }
    
    /// <summary>
    /// 测试增加体力
    /// 在Inspector中右键组件 → 测试/增加体力
    /// </summary>
    [ContextMenu("测试/增加体力")]
    private void TestAddHealth()
    {
        ModifyHealth(testHealthAmount);
        Debug.Log($"[测试] 增加 {testHealthAmount} 体力");
    }
    
    /// <summary>
    /// 测试减少体力
    /// 在Inspector中右键组件 → 测试/减少体力
    /// </summary>
    [ContextMenu("测试/减少体力")]
    private void TestReduceHealth()
    {
        ModifyHealth(-testHealthAmount);
        Debug.Log($"[测试] 减少 {testHealthAmount} 体力");
    }
    
    /// <summary>
    /// 测试增加最大体力
    /// 在Inspector中右键组件 → 测试/增加最大体力
    /// </summary>
    [ContextMenu("测试/增加最大体力")]
    private void TestAddMaxHealth()
    {
        ModifyMaxHealth(testMaxHealthAmount);
        Debug.Log($"[测试] 增加 {testMaxHealthAmount} 最大体力");
    }
    
    /// <summary>
    /// 测试减少最大体力
    /// 在Inspector中右键组件 → 测试/减少最大体力
    /// </summary>
    [ContextMenu("测试/减少最大体力")]
    private void TestReduceMaxHealth()
    {
        ModifyMaxHealth(-testMaxHealthAmount);
        Debug.Log($"[测试] 减少 {testMaxHealthAmount} 最大体力");
    }
    
    /// <summary>
    /// 测试恢复满体力
    /// 在Inspector中右键组件 → 测试/恢复满体力
    /// </summary>
    [ContextMenu("测试/恢复满体力")]
    private void TestRestoreFullHealth()
    {
        RestoreFullHealth();
        Debug.Log($"[测试] 恢复满体力");
    }
    
    /// <summary>
    /// 测试打印状态
    /// 在Inspector中右键组件 → 测试/打印当前状态
    /// </summary>
    [ContextMenu("测试/打印当前状态")]
    private void TestPrintState()
    {
        DebugPrintState();
    }
    
    /// <summary>
    /// 测试设置深度一
    /// 在Inspector中右键组件 → 测试/设置深度一
    /// </summary>
    [ContextMenu("测试/设置深度一")]
    private void TestSetDepth1()
    {
        SetDepth(ItemSystem.FishDepth.Depth1);
        Debug.Log($"[测试] 深度设置为 Depth1");
    }
    
    /// <summary>
    /// 测试设置深度二
    /// 在Inspector中右键组件 → 测试/设置深度二
    /// </summary>
    [ContextMenu("测试/设置深度二")]
    private void TestSetDepth2()
    {
        SetDepth(ItemSystem.FishDepth.Depth2);
        Debug.Log($"[测试] 深度设置为 Depth2");
    }
    
    /// <summary>
    /// 测试设置深度三
    /// 在Inspector中右键组件 → 测试/设置深度三
    /// </summary>
    [ContextMenu("测试/设置深度三")]
    private void TestSetDepth3()
    {
        SetDepth(ItemSystem.FishDepth.Depth3);
        Debug.Log($"[测试] 深度设置为 Depth3");
    }
    
    #endregion
}
