using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ItemSystem;

namespace ShopSystem
{
    /// <summary>
    /// 商店数据管理器（单例，DontDestroyOnLoad）
    /// 职责：持有消耗品、装备、杂鱼商店牌序、持久化悬挂槽数据、供 GameManager 终局结算读取
    /// </summary>
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        // 悬挂槽数据（索引 0-2，null 表示空槽）
        private FishData[] hangSlots = new FishData[3];

        // 消耗品商店牌序（从 ItemPool 深拷贝，顺序抽取）
        private List<ConsumableData> consumablePool = new List<ConsumableData>();

        // 装备商店牌序（从 ItemPool 深拷贝，顺序抽取）
        private List<EquipmentData> equipmentPool = new List<EquipmentData>();

        // 杂鱼牌序（从 ItemPool 深拷贝，顺序抽取）
        private List<TrashData> trashPool = new List<TrashData>();

        private bool isPoolInitialized = false;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #endregion

        #region Pool Management

        /// <summary>
        /// 从 ItemPool 深拷贝消耗品牌序（首次进入商店场景时调用一次）
        /// 遵循 FishingTableManager 的深拷贝模式，与 ItemPool 运行时状态完全解耦
        /// </summary>
        public void InitializePools()
        {
            if (isPoolInitialized) return;

            if (ItemPool.Instance == null)
            {
                Debug.LogError("[ShopManager] ItemPool 不存在，无法初始化牌序");
                return;
            }

            List<ItemData> consumableSource = ItemPool.Instance.GetCategoryDeck(ItemCategory.Consumable);
            consumablePool = new List<ConsumableData>(consumableSource.OfType<ConsumableData>());

            List<ItemData> equipmentSource = ItemPool.Instance.GetCategoryDeck(ItemCategory.Equipment);
            equipmentPool = new List<EquipmentData>(equipmentSource.OfType<EquipmentData>());

            List<ItemData> trashSource = ItemPool.Instance.GetCategoryDeck(ItemCategory.Trash);
            trashPool = new List<TrashData>(trashSource.OfType<TrashData>());

            isPoolInitialized = true;
            Debug.Log($"[ShopManager] 牌序初始化完成：消耗品 {consumablePool.Count} 张，装备 {equipmentPool.Count} 张，杂鱼 {trashPool.Count} 张");
        }

        /// <summary>
        /// 顺序抽取一张消耗品并从列表移除。池空时返回 null。
        /// </summary>
        public ConsumableData DrawConsumable()
        {
            if (consumablePool.Count == 0)
            {
                Debug.Log("[ShopManager] 消耗品牌池已耗尽");
                return null;
            }

            ConsumableData drawn = consumablePool[0];
            consumablePool.RemoveAt(0);
            Debug.Log($"[ShopManager] 抽取消耗品：{drawn.itemName}，剩余 {consumablePool.Count} 张");
            return drawn;
        }

        /// <summary>
        /// 消耗品牌池是否已耗尽
        /// </summary>
        public bool IsConsumablePoolEmpty => consumablePool.Count == 0;

        /// <summary>
        /// 顺序抽取一张装备并从列表移除。池空时返回 null。
        /// </summary>
        public EquipmentData DrawEquipment()
        {
            if (equipmentPool.Count == 0)
            {
                Debug.Log("[ShopManager] 装备牌池已耗尽");
                return null;
            }

            EquipmentData drawn = equipmentPool[0];
            equipmentPool.RemoveAt(0);
            Debug.Log($"[ShopManager] 抽取装备：{drawn.itemName}，剩余 {equipmentPool.Count} 张");
            return drawn;
        }

        /// <summary>
        /// 装备牌池是否已耗尽
        /// </summary>
        public bool IsEquipmentPoolEmpty => equipmentPool.Count == 0;

        /// <summary>
        /// 预览下一张装备（不移除），供价格显示用。池空时返回 null。
        /// </summary>
        public EquipmentData PeekEquipment()
        {
            return equipmentPool.Count > 0 ? equipmentPool[0] : null;
        }

        /// <summary>
        /// 顺序抽取一张杂鱼并从列表移除。池空时返回 null。
        /// </summary>
        public TrashData DrawTrash()
        {
            if (trashPool.Count == 0)
            {
                Debug.Log("[ShopManager] 杂鱼牌池已耗尽");
                return null;
            }

            TrashData drawn = trashPool[0];
            trashPool.RemoveAt(0);
            Debug.Log($"[ShopManager] 抽取杂鱼：{drawn.itemName}，剩余 {trashPool.Count} 张");
            return drawn;
        }

        /// <summary>
        /// 杂鱼牌池是否已耗尽
        /// </summary>
        public bool IsTrashPoolEmpty => trashPool.Count == 0;

        /// <summary>
        /// 通用批量抽取：从指定类型牌堆顶部取 count 张并移除。
        /// 牌堆不足时返回实际可用数量。
        /// </summary>
        public List<ItemData> DrawTopItems(ItemCategory category, int count)
        {
            switch (category)
            {
                case ItemCategory.Consumable: return DrawTopFromPool(consumablePool, category, count);
                case ItemCategory.Equipment:  return DrawTopFromPool(equipmentPool, category, count);
                case ItemCategory.Trash:      return DrawTopFromPool(trashPool, category, count);
                default:
                    Debug.LogWarning($"[ShopManager] DrawTopItems 不支持的牌堆类型：{category}");
                    return new List<ItemData>();
            }
        }

        /// <summary>
        /// 将卡牌插回指定类型牌堆顶部（保持传入列表的原始顺序）。
        /// 例如传入 [A, B, C]，结果牌堆顶部为 A, B, C, ...原有牌...
        /// </summary>
        public void ReturnToTop(ItemCategory category, List<ItemData> cards)
        {
            if (cards == null || cards.Count == 0) return;

            switch (category)
            {
                case ItemCategory.Consumable: InsertToPool(consumablePool, cards, category); break;
                case ItemCategory.Equipment:  InsertToPool(equipmentPool, cards, category);  break;
                case ItemCategory.Trash:      InsertToPool(trashPool, cards, category);      break;
                default:
                    Debug.LogWarning($"[ShopManager] ReturnToTop 不支持的牌堆类型：{category}");
                    break;
            }
        }

        private List<ItemData> DrawTopFromPool<T>(List<T> pool, ItemCategory category, int count) where T : ItemData
        {
            int actual = Mathf.Min(count, pool.Count);
            var result = new List<ItemData>(actual);
            for (int i = 0; i < actual; i++)
            {
                result.Add(pool[0]);
                pool.RemoveAt(0);
            }
            Debug.Log($"[ShopManager] 批量抽取 {category}：请求 {count} 张，实际 {actual} 张，剩余 {pool.Count} 张");
            return result;
        }

        private void InsertToPool<T>(List<T> pool, List<ItemData> cards, ItemCategory category) where T : ItemData
        {
            for (int i = cards.Count - 1; i >= 0; i--)
            {
                if (cards[i] is T typed)
                    pool.Insert(0, typed);
            }
            Debug.Log($"[ShopManager] 归还 {cards.Count} 张 {category} 到牌堆顶部，当前牌堆 {pool.Count} 张");
        }

        /// <summary>
        /// 对杂鱼牌池执行 Fisher-Yates 洗牌
        /// </summary>
        public void ShuffleTrashPool()
        {
            int n = trashPool.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                TrashData temp = trashPool[k];
                trashPool[k] = trashPool[n];
                trashPool[n] = temp;
            }
            Debug.Log($"[ShopManager] 杂鱼牌池已洗牌，共 {trashPool.Count} 张");
        }

        #endregion

        #region Hang Slot Management

        /// <summary>
        /// 写入悬挂数据（支持替换，外部保证索引合法）
        /// </summary>
        public bool TryHangFish(int slotIndex, FishData fish)
        {
            if (slotIndex < 0 || slotIndex >= hangSlots.Length)
            {
                Debug.LogWarning($"[ShopManager] 无效的悬挂槽索引：{slotIndex}");
                return false;
            }

            hangSlots[slotIndex] = fish;
            Debug.Log($"[ShopManager] 悬挂槽 {slotIndex} 写入：{fish?.itemName ?? "（清空）"}");
            return true;
        }

        /// <summary>
        /// 读取悬挂数据（供 ShopHangController 恢复视觉）
        /// </summary>
        public FishData GetHangSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= hangSlots.Length) return null;
            return hangSlots[slotIndex];
        }

        /// <summary>
        /// 返回完整悬挂槽数组（供 GameManager 终局结算）
        /// </summary>
        public FishData[] GetAllHangSlots() => hangSlots;

        #endregion

        #region 游戏重置（2026-04-02新增）

        /// <summary>
        /// 重置商店数据（新游戏时由 DayManager 调用）
        /// 清空牌序和悬挂槽，标记为未初始化
        /// </summary>
        public void ResetPools()
        {
            consumablePool.Clear();
            equipmentPool.Clear();
            trashPool.Clear();

            for (int i = 0; i < hangSlots.Length; i++)
                hangSlots[i] = null;

            ItemSystem.Effect_AutoHang.ResetAutoHungState();

            isPoolInitialized = false;
            Debug.Log("[ShopManager] 商店数据已重置");
        }

        #endregion
    }
}
