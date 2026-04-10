using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ItemSystem;
using HandSystem;

namespace FishCardSystem
{
    /// <summary>
    /// 卡牌系统测试脚本
    /// 通过 HandManager 向手牌添加指定的卡牌，视觉卡由 HandPanelUI 自动创建。
    /// 使用方式：
    ///   1. 在场景中任意 GameObject 上挂载本组件
    ///   2. 在对应数据列表中手动指定要生成的卡牌
    ///   3. 列表中所有数据都会被生成进手牌，留空的类型会被跳过
    /// </summary>
    public class CardSystemTester : MonoBehaviour
    {
        [Header("鱼类数据")]
        [SerializeField] private List<FishData> testFishData;

        [Header("杂鱼数据")]
        [SerializeField] private List<TrashData> testTrashData;

        [Header("消耗品数据")]
        [SerializeField] private List<ConsumableData> testConsumableData;

        [Header("装备数据")]
        [SerializeField] private List<EquipmentData> testEquipmentData;

        private IEnumerator Start()
        {
            yield return null;

            if (HandManager.Instance == null)
            {
                Debug.LogError("[CardSystemTester] 未找到 HandManager，测试脚本已停止。");
                yield break;
            }

            AddTestCards();
        }

        [ContextMenu("添加手牌")]
        public void AddTestCards()
        {
            if (HandManager.Instance == null)
            {
                Debug.LogWarning("[CardSystemTester] HandManager 尚未初始化，请在运行时调用。");
                return;
            }

            int total = 0;
            total += AddAll(testFishData, "鱼类");
            total += AddAll(testTrashData, "杂鱼");
            total += AddAll(testConsumableData, "消耗品");
            total += AddAll(testEquipmentData, "装备");

            if (total == 0)
                Debug.LogWarning("[CardSystemTester] 所有数据列表均为空，未添加任何卡牌。");
            else
                Debug.Log($"[CardSystemTester] 共向手牌添加 {total} 张卡牌。");
        }

        private int AddAll<T>(List<T> dataList, string label) where T : ItemData
        {
            if (dataList == null || dataList.Count == 0)
                return 0;

            int count = 0;
            foreach (var data in dataList)
            {
                if (data == null)
                {
                    Debug.LogWarning($"[CardSystemTester] {label}列表中存在空引用，已跳过。");
                    continue;
                }
                HandManager.Instance.AddCard(data);
                count++;
            }

            Debug.Log($"[CardSystemTester] 已向手牌添加 {count} 张{label}卡牌。");
            return count;
        }
    }
}
