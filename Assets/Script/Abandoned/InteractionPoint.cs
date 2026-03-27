/// <summary>
/// 交互点基类
/// 检测玩家进入/离开交互范围，并控制相应的动画状态
/// 创建日期：2026-01-19
/// </summary>

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 交互点基类
/// 用于创建可与玩家交互的物体（如NPC、宝箱、道具等）
/// 当玩家进入触发范围时，播放"靠近"动画提示
/// 使用protected修饰符方便子类继承和扩展
/// </summary>
/// <remarks>
/// 使用要求：
/// 1. 游戏对象必须有Collider2D组件，并勾选"Is Trigger"
/// 2. 游戏对象必须有Animator组件
/// 3. Animator中需要有"isNear"布尔参数
/// 4. 玩家对象必须有"Player"标签
/// </remarks>
[System.Obsolete("此脚本已废弃，不再使用。保留仅供历史参考。")]
public class InteractionPoint : MonoBehaviour
{
    /// <summary>
    /// 动画控制器组件
    /// 用于控制交互点的动画状态（如提示图标的显示/隐藏）
    /// </summary>
    protected Animator animator;

    /// <summary>
    /// 初始化方法 - 获取Animator组件
    /// 如果未找到Animator组件，会输出警告信息
    /// 使用virtual修饰符允许子类重写此方法
    /// </summary>
    protected virtual void Awake()
    {
        // 获取当前游戏对象上的Animator组件
        animator = GetComponent<Animator>();
        
        // 如果未找到组件，输出警告（不抛出错误，保持游戏继续运行）
        if (animator == null)
        {
            Debug.LogWarning("InteractionPoint: Animator component not found on this GameObject!");
        }
    }

    /// <summary>
    /// 触发器进入事件 - 玩家进入交互范围
    /// 当玩家的Collider2D进入本对象的触发范围时调用
    /// 设置动画参数"isNear"为true，显示交互提示
    /// </summary>
    /// <param name="other">进入触发范围的碰撞体</param>
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        // 检查进入的对象是否是玩家
        if (other.CompareTag("Player"))
        {
            // 设置动画参数，显示"玩家靠近"状态（如显示提示图标）
            if (animator != null)
            {
                animator.SetBool("isNear", true);
            }
        }
    }

    /// <summary>
    /// 触发器离开事件 - 玩家离开交互范围
    /// 当玩家的Collider2D离开本对象的触发范围时调用
    /// 设置动画参数"isNear"为false，隐藏交互提示
    /// </summary>
    /// <param name="other">离开触发范围的碰撞体</param>
    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        // 检查离开的对象是否是玩家
        if (other.CompareTag("Player"))
        {
            // 设置动画参数，隐藏"玩家靠近"状态（如隐藏提示图标）
            if (animator != null)
            {
                animator.SetBool("isNear", false);
            }
        }
    }
}
