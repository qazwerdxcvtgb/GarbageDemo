/// <summary>
/// 交互点1 - 打开卡片选择面板
/// 继承InteractionPoint基类，玩家交互时打开卡片选择窗口
/// 创建日期：2026-01-20
/// </summary>

using UnityEngine;
using UISystem;
using ItemSystem;


/// <summary>
/// 交互点1
/// 玩家与此交互点交互时，打开卡片选择面板
/// </summary>
public class InteractionPoint_1 : InteractionPoint
{
    #region 配置

    [Header("卡片选择面板")]
    [Tooltip("卡片选择面板的引用")]
    public CardSelectionPanel cardSelectionPanel;

    [Header("卡池配置")]
    [Tooltip("抽取的卡牌深度")]
    public FishDepth drawCardDepth = FishDepth.Depth1;

    #endregion

    #region 交互逻辑

    /// <summary>
    /// 玩家进入触发范围
    /// 重写基类方法，添加订阅交互事件的逻辑
    /// </summary>
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        // 调用基类方法，显示交互提示
        base.OnTriggerEnter2D(other);

        // 检查是否是玩家
        if (other.CompareTag("Player"))
        {
            // 订阅交互事件
            GameManager.Instance.OnInteractionPressed.AddListener(OnPlayerInteract);
        }
    }

    /// <summary>
    /// 玩家离开触发范围
    /// 重写基类方法，添加取消订阅交互事件的逻辑
    /// </summary>
    protected override void OnTriggerExit2D(Collider2D other)
    {
        // 调用基类方法，隐藏交互提示
        base.OnTriggerExit2D(other);

        // 检查是否是玩家
        if (other.CompareTag("Player"))
        {
            // 取消订阅交互事件
            GameManager.Instance.OnInteractionPressed.RemoveListener(OnPlayerInteract);
        }
    }

    /// <summary>
    /// 玩家按下交互键时调用
    /// </summary>
    private void OnPlayerInteract()
    {
        if (cardSelectionPanel != null)
        {
            Debug.Log("[InteractionPoint_1] 玩家交互，打开卡片选择面板");
            cardSelectionPanel.OpenPanel(drawCardDepth);
        }
        else
        {
            Debug.LogWarning("[InteractionPoint_1] CardSelectionPanel引用为空，无法打开面板");
        }
    }

    #endregion

    #region 清理

    /// <summary>
    /// 对象销毁时取消事件订阅（防止内存泄漏）
    /// </summary>
    private void OnDestroy()
    {
        // 确保取消订阅
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnInteractionPressed.RemoveListener(OnPlayerInteract);
        }
    }

    #endregion
}
