/// <summary>
/// 手牌管理器 - 单例模式
/// 负责管理玩家的手牌数据
/// 创建日期：2026-01-20
/// </summary>

using System;
using System.Collections.Generic;
using UnityEngine;
using ItemSystem;

namespace HandSystem
{
    /// <summary>
    /// 手牌管理器
    /// 使用单例模式确保全局唯一访问点
    /// </summary>
    public class HandManager : MonoBehaviour
    {
        #region 单例模式

        /// <summary>
        /// 单例实例
        /// </summary>
        public static HandManager Instance { get; private set; }

        /// <summary>
        /// 初始化单例
        /// </summary>
        private void Awake()
        {
            // 单例模式实现
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // 场景切换时不销毁
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #endregion

        #region 数据存储

        /// <summary>
        /// 玩家手牌列表
        /// </summary>
        private List<ItemData> handCards = new List<ItemData>();

        #endregion

        #region 事件

        /// <summary>
        /// 手牌变化事件（添加或移除卡牌时触发）
        /// </summary>
        public event System.Action OnHandChanged;

        /// <summary>
        /// 新增卡牌事件（仅在 AddCard 时触发，携带具体卡牌数据供视觉层创建实例）
        /// 触发顺序：OnHandChanged（先，槽位数量更新）→ OnCardAdded（后，视觉创建）
        /// </summary>
        public event System.Action<ItemData> OnCardAdded;

        #endregion

        #region 手牌管理方法

        /// <summary>
        /// 添加卡牌到手牌
        /// </summary>
        /// <param name="card">要添加的卡牌</param>
        public void AddCard(ItemData card)
        {
            if (card == null)
            {
                Debug.LogWarning("[HandManager] 尝试添加空卡牌到手牌");
                return;
            }

            handCards.Add(card);
            Debug.Log($"[HandManager] 添加卡牌到手牌: {card.itemName}，当前手牌数量: {handCards.Count}");

            // 先触发通用变化事件（HandPanelUI 借此更新槽位数量）
            OnHandChanged?.Invoke();
            // 再触发新增事件（HandPanelUI 借此创建视觉卡实例，此时新槽位已就绪）
            OnCardAdded?.Invoke(card);
        }

        /// <summary>
        /// 仅将卡牌数据加入手牌列表并通知槽位数量变化，不触发 OnCardAdded。
        /// 用于视觉卡已存在的场景（如装备卡从装备槽归还手牌），避免重复创建视觉卡。
        /// </summary>
        public void AddCardData(ItemData card)
        {
            if (card == null)
            {
                Debug.LogWarning("[HandManager] 尝试添加空卡牌到手牌");
                return;
            }

            handCards.Add(card);
            Debug.Log($"[HandManager] 添加卡牌数据到手牌(无视觉): {card.itemName}，当前手牌数量: {handCards.Count}");

            // 只通知槽位数量更新，不创建新视觉卡
            OnHandChanged?.Invoke();
        }

        /// <summary>
        /// 从手牌中移除卡牌
        /// </summary>
        /// <param name="card">要移除的卡牌</param>
        /// <returns>是否移除成功</returns>
        public bool RemoveCard(ItemData card)
        {
            if (card == null)
            {
                Debug.LogWarning("[HandManager] 尝试移除空卡牌");
                return false;
            }

            bool removed = handCards.Remove(card);
            if (removed)
            {
                Debug.Log($"[HandManager] 从手牌移除卡牌: {card.itemName}，当前手牌数量: {handCards.Count}");

                // 触发手牌变化事件
                OnHandChanged?.Invoke();
            }
            else
            {
                Debug.LogWarning($"[HandManager] 手牌中未找到卡牌: {card.itemName}");
            }
            return removed;
        }

        /// <summary>
        /// 获取手牌列表的只读副本
        /// </summary>
        /// <returns>手牌列表副本</returns>
        public List<ItemData> GetHandCards()
        {
            return new List<ItemData>(handCards); // 返回副本，避免外部修改
        }

        /// <summary>
        /// 获取手牌数量
        /// </summary>
        /// <returns>手牌数量</returns>
        public int GetHandCount()
        {
            return handCards.Count;
        }

        /// <summary>
        /// 检查手牌中是否包含指定卡牌
        /// </summary>
        /// <param name="card">要检查的卡牌</param>
        /// <returns>是否包含</returns>
        public bool ContainsCard(ItemData card)
        {
            return handCards.Contains(card);
        }

        /// <summary>
        /// 清空所有手牌
        /// </summary>
        public void ClearHand()
        {
            int count = handCards.Count;
            handCards.Clear();
            Debug.Log($"[HandManager] 清空所有手牌，已移除{count}张卡牌");

            // 触发手牌变化事件
            OnHandChanged?.Invoke();
        }

        #endregion

        #region 调试方法

        /// <summary>
        /// 打印所有手牌信息（调试用）
        /// </summary>
        public void DebugPrintHandCards()
        {
            Debug.Log($"===== 手牌信息 =====");
            Debug.Log($"手牌数量: {handCards.Count}");
            foreach (var card in handCards)
            {
                Debug.Log(card.GetItemInfo());
            }
            Debug.Log($"==================");
        }

        #endregion
    }
}
