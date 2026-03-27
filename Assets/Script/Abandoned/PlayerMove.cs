/// <summary>
/// 玩家移动控制器
/// 处理玩家的移动、动画和输入系统
/// 创建日期：2026-01-19
/// </summary>

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家移动控制器
/// 使用Unity新版InputSystem处理输入
/// 支持键盘（WASD）和游戏手柄控制
/// 包含移动、动画控制、角色翻转和交互功能
/// </summary>
/// <remarks>
/// 使用要求：
/// 1. 游戏对象必须有Rigidbody2D组件（用于物理移动）
/// 2. 游戏对象必须有Animator组件（用于动画控制）
/// 3. Animator需要包含以下参数：
///    - Horizontal (float): 水平移动输入
///    - Vertical (float): 垂直移动输入
///    - Speed (float): 移动速度
///    - isInteraction (trigger): 交互触发器
/// </remarks>
[System.Obsolete("此脚本已废弃，不再使用。保留仅供历史参考。")]
public class PlayerMove : MonoBehaviour
{
    #region 公开字段

    /// <summary>
    /// 移动速度（单位/秒）
    /// </summary>
    [Header("移动设置")]
    [Tooltip("玩家移动速度")]
    public float moveSpeed = 3f;

    /// <summary>
    /// 刚体组件（用于物理移动）
    /// 如果未在Inspector中指定，会在Awake时自动获取
    /// </summary>
    [Header("组件引用")]
    [Tooltip("玩家的Rigidbody2D组件")]
    public Rigidbody2D rb;

    /// <summary>
    /// 动画控制器组件
    /// 如果未在Inspector中指定，会在Awake时自动获取
    /// </summary>
    [Tooltip("玩家的Animator组件")]
    public Animator animator;

    #endregion

    #region 公开方法

    /// <summary>
    /// 设置输入启用状态
    /// 当UI面板打开时调用此方法禁用玩家输入
    /// </summary>
    /// <param name="enabled">是否启用输入</param>
    public void SetInputEnabled(bool enabled)
    {
        isInputEnabled = enabled;
        Debug.Log($"[PlayerMove] 输入已{(enabled ? "启用" : "禁用")}");
    }

    #endregion

    #region 私有字段

    /// <summary>
    /// 输入是否启用（用于UI面板打开时禁用玩家输入）
    /// </summary>
    private bool isInputEnabled = true;

    /// <summary>
    /// 输入系统实例
    /// 管理所有输入动作的生命周期
    /// </summary>
    private InputSystem inputSystem;

    /// <summary>
    /// 玩家控制动作映射
    /// 包含Movement（移动）和Interaction（交互）两个输入动作
    /// </summary>
    private InputSystem.PlayerControlActions playerControl;

    #endregion

    #region Unity生命周期方法

    /// <summary>
    /// 初始化方法 - 获取组件引用并设置输入系统
    /// 在对象被创建时调用一次
    /// </summary>
    void Awake()
    { 
        // 如果未在Inspector中指定Rigidbody2D，则自动获取
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        // 如果未在Inspector中指定Animator，则自动获取
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        // 初始化新的 InputSystem
        inputSystem = new InputSystem();
        playerControl = inputSystem.PlayerControl;
    }

    /// <summary>
    /// 启用时调用 - 启用输入系统并订阅交互事件
    /// 当对象被激活或脚本被启用时调用
    /// </summary>
    void OnEnable()
    {
        // 启用玩家控制输入动作
        playerControl.Enable();

        // 订阅交互按钮事件
        // started: 按钮开始按下时触发
        // performed: 按钮按下完成时触发
        playerControl.Interaction.started += OnInteraction;
        playerControl.Interaction.performed += OnInteraction;
    }

    /// <summary>
    /// 禁用时调用 - 取消订阅事件并禁用输入系统
    /// 当对象被停用或脚本被禁用时调用
    /// 防止内存泄漏和空引用错误
    /// </summary>
    void OnDisable()
    {
        // 取消订阅交互按钮事件
        playerControl.Interaction.started -= OnInteraction;
        playerControl.Interaction.performed -= OnInteraction;

        // 禁用玩家控制输入动作
        playerControl.Disable();
    }
    
    /// <summary>
    /// 销毁时调用 - 释放输入系统资源
    /// 使用?.安全调用操作符，避免inputSystem为null时报错
    /// </summary>
    void OnDestroy()
    {
        inputSystem?.Dispose();
    }

    /// <summary>
    /// 固定更新 - 处理物理相关的移动逻辑
    /// 以固定时间间隔调用，适合处理物理运算
    /// </summary>
    void FixedUpdate()
    {
        // 如果输入被禁用，停止移动
        if (!isInputEnabled)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        // 获取玩家输入（使用新的 InputSystem）
        // ReadValue<Vector2>() 返回当前输入值（WASD或摇杆）
        Vector2 moveInput = playerControl.Movement.ReadValue<Vector2>();
        float moveX = moveInput.x; // 水平输入 (-1到1)
        float moveY = moveInput.y; // 垂直输入 (-1到1)

        // 计算移动方向并归一化
        // normalized确保斜向移动速度与直线移动速度一致
        Vector2 moveDirection = new Vector2(moveX, moveY).normalized;
        
        // 更新动画参数
        // Horizontal和Vertical用于动画混合树判断移动方向
        animator.SetFloat("Horizontal", moveX);
        animator.SetFloat("Vertical", moveY);
        // Speed用于判断是否在移动（idle或walk动画）
        animator.SetFloat("Speed", moveDirection.sqrMagnitude);

        // 处理角色镜像翻转（左右翻转）
        if (moveX < 0)
        {
            // 向左移动，X轴scale设为负值实现翻转
            transform.localScale = new Vector3(-4, 4, 4);
        }
        else if (moveX > 0)
        {
            // 向右移动，X轴scale设为正值
            transform.localScale = new Vector3(4, 4, 4);
        }
        // 注意：moveX == 0时不改变朝向，保持当前方向

        // 应用移动 - 直接设置刚体速度
        // 使用velocity而不是AddForce，实现更直接的控制
        rb.velocity = moveDirection * moveSpeed;
    }

    #endregion

    #region 输入处理方法

    /// <summary>
    /// 处理交互按钮按下事件
    /// 当玩家按下交互键（F键或手柄X键）时触发
    /// 播放本地交互动画，并通过GameManager触发全局事件
    /// </summary>
    /// <param name="context">输入动作回调上下文，包含输入状态信息</param>
    private void OnInteraction(InputAction.CallbackContext context)
    {
        // 如果输入被禁用，不处理交互
        if (!isInputEnabled)
        {
            return;
        }

        // 检查输入状态是否为started或performed
        // started: 按键开始按下
        // performed: 按键按下完成
        if (context.started || context.performed)
        {
            // 触发本地动画（玩家角色的交互动作）
            animator.SetTrigger("isInteraction");
            
            // 通过 GameManager 触发全局事件
            // 让其他系统（如InteractionPoint）可以响应交互操作
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerInteractionPressed();
            }
        }
    }

    #endregion
}
