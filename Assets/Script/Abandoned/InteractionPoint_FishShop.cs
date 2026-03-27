/// <summary>
/// 鱼店交互点
/// 创建日期：2026-01-21
/// 功能：玩家与鱼店NPC交互，打开鱼店面板
/// </summary>

using UnityEngine;
using UISystem;

/// <summary>
/// 鱼店交互点（继承InteractionPoint基类）
/// </summary>
[System.Obsolete("此脚本已废弃，不再使用。保留仅供历史参考。")]
public class InteractionPoint_FishShop : InteractionPoint
{
    /// <summary>
    /// 玩家进入触发范围
    /// </summary>
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        // 调用基类方法（显示交互提示动画）
        base.OnTriggerEnter2D(other);

        if (other.CompareTag("Player"))
        {
            // 订阅交互事件
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnInteractionPressed.AddListener(OnPlayerInteract);
                Debug.Log("[InteractionPoint_FishShop] 玩家靠近鱼店");
            }
            else
            {
                Debug.LogError("[InteractionPoint_FishShop] GameManager.Instance为空");
            }
        }
    }

    /// <summary>
    /// 玩家离开触发范围
    /// </summary>
    protected override void OnTriggerExit2D(Collider2D other)
    {
        // 调用基类方法（隐藏交互提示动画）
        base.OnTriggerExit2D(other);

        if (other.CompareTag("Player"))
        {
            // 取消订阅交互事件
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnInteractionPressed.RemoveListener(OnPlayerInteract);
                Debug.Log("[InteractionPoint_FishShop] 玩家离开鱼店");
            }
        }
    }

    /// <summary>
    /// 玩家交互（按F键）
    /// </summary>
    private void OnPlayerInteract()
    {
        // 使用懒加载单例（确保已初始化）
        if (FishShopPanel.Instance == null)
        {
            Debug.LogError("[InteractionPoint_FishShop] FishShopPanel.Instance为空");
            return;
        }

        // 打开鱼店面板
        FishShopPanel.Instance.OpenPanel();
        Debug.Log("[InteractionPoint_FishShop] 打开鱼店面板");
    }
}
