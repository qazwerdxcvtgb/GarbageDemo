using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ItemSystem;
using HandSystem;

namespace FishCardSystem
{
    /// <summary>
    /// 卡牌系统测试脚本
    /// 通过 HandManager 向手牌添加指定种类的卡牌，视觉卡由 HandPanelUI 自动创建。
    /// 使用方式：
    ///   1. 在场景中任意 GameObject 上挂载本组件
    ///   2. 选择 Card Type To Add 指定卡牌种类
    ///   3. 对应数据列表留空则从 Resources 随机加载，也可手动指定
    ///   4. 留空时由 Random Count 决定添加数量
    /// </summary>
    public class CardSystemTester : MonoBehaviour
    {
        public enum CardTypeToAdd { Fish, Trash, Consumable, Equipment }

        [Header("卡牌种类选择")]
        [SerializeField] private CardTypeToAdd cardType = CardTypeToAdd.Fish;

        [Header("随机模式下添加数量（数据列表为空时生效）")]
        [SerializeField] private int randomCount = 3;

        [Header("鱼类数据（留空则从 Resources/Items/Fish 随机加载）")]
        [SerializeField] private List<FishData> testFishData;

        [Header("杂鱼数据（留空则从 Resources/Items/Trash 随机加载）")]
        [SerializeField] private List<TrashData> testTrashData;

        [Header("消耗品数据（留空则从 Resources/Items/Consumable 随机加载）")]
        [SerializeField] private List<ConsumableData> testConsumableData;

        [Header("装备数据（留空则从 Resources/Items/Equipment 随机加载）")]
        [SerializeField] private List<EquipmentData> testEquipmentData;

        private IEnumerator Start()
        {
            // 等待一帧，确保各单例（HandManager、HandPanelUI）完成初始化
            yield return null;

            if (HandManager.Instance == null)
            {
                Debug.LogError("[CardSystemTester] 未找到 HandManager，测试脚本已停止。");
                yield break;
            }

            AddTestCards();
        }

        /// <summary>
        /// 根据当前选择的 cardType 向手牌添加测试卡牌
        /// 可通过 Inspector 右键菜单手动触发
        /// </summary>
        [ContextMenu("添加手牌")]
        public void AddTestCards()
        {
            if (HandManager.Instance == null)
            {
                Debug.LogWarning("[CardSystemTester] HandManager 尚未初始化，请在运行时调用。");
                return;
            }

            switch (cardType)
            {
                case CardTypeToAdd.Fish:
                    AddFishCards();
                    break;
                case CardTypeToAdd.Trash:
                    AddTrashCards();
                    break;
                case CardTypeToAdd.Consumable:
                    AddConsumableCards();
                    break;
                case CardTypeToAdd.Equipment:
                    AddEquipmentCards();
                    break;
            }
        }

        [ContextMenu("添加鱼类手牌")]
        public void AddFishCards()
        {
            var pool = BuildPool<FishData>(testFishData, "Items/Fish");
            if (pool.Count == 0)
            {
                Debug.LogWarning("[CardSystemTester] 没有可用的 FishData，跳过添加。");
                return;
            }

            int count = (testFishData != null && testFishData.Count > 0) ? testFishData.Count : randomCount;
            for (int i = 0; i < count; i++)
                HandManager.Instance.AddCard(pool[Random.Range(0, pool.Count)]);

            Debug.Log($"[CardSystemTester] 已向手牌添加 {count} 张鱼类卡牌。");
        }

        [ContextMenu("添加杂鱼手牌")]
        public void AddTrashCards()
        {
            var pool = BuildPool<TrashData>(testTrashData, "Items/Trash");
            if (pool.Count == 0)
            {
                Debug.LogWarning("[CardSystemTester] 没有可用的 TrashData，跳过添加。");
                return;
            }

            int count = (testTrashData != null && testTrashData.Count > 0) ? testTrashData.Count : randomCount;
            for (int i = 0; i < count; i++)
                HandManager.Instance.AddCard(pool[Random.Range(0, pool.Count)]);

            Debug.Log($"[CardSystemTester] 已向手牌添加 {count} 张杂鱼卡牌。");
        }

        [ContextMenu("添加消耗品手牌")]
        public void AddConsumableCards()
        {
            var pool = BuildPool<ConsumableData>(testConsumableData, "Items/Consumable");
            if (pool.Count == 0)
            {
                Debug.LogWarning("[CardSystemTester] 没有可用的 ConsumableData，跳过添加。");
                return;
            }

            int count = (testConsumableData != null && testConsumableData.Count > 0) ? testConsumableData.Count : randomCount;
            for (int i = 0; i < count; i++)
                HandManager.Instance.AddCard(pool[Random.Range(0, pool.Count)]);

            Debug.Log($"[CardSystemTester] 已向手牌添加 {count} 张消耗品卡牌。");
        }

        [ContextMenu("添加装备手牌")]
        public void AddEquipmentCards()
        {
            var pool = BuildPool<EquipmentData>(testEquipmentData, "Items/Equipment");
            if (pool.Count == 0)
            {
                Debug.LogWarning("[CardSystemTester] 没有可用的 EquipmentData，跳过添加。");
                return;
            }

            int count = (testEquipmentData != null && testEquipmentData.Count > 0) ? testEquipmentData.Count : randomCount;
            for (int i = 0; i < count; i++)
                HandManager.Instance.AddCard(pool[Random.Range(0, pool.Count)]);

            Debug.Log($"[CardSystemTester] 已向手牌添加 {count} 张装备卡牌。");
        }

        private List<T> BuildPool<T>(List<T> provided, string resourcePath) where T : ItemData
        {
            if (provided != null && provided.Count > 0)
                return provided;

            T[] loaded = Resources.LoadAll<T>(resourcePath);
            if (loaded.Length == 0)
                Debug.LogWarning($"[CardSystemTester] Resources/{resourcePath} 中未找到任何 {typeof(T).Name}。");

            return new List<T>(loaded);
        }
    }
}
