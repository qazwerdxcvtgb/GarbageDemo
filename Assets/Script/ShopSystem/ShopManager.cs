using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ItemSystem;

namespace ShopSystem
{
    /// <summary>
    /// 商店数据管理器（单例，DontDestroyOnLoad）
    /// 职责：持有消耗品和装备商店牌序、持久化悬挂槽数据、供 GameManager 终局结算读取
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

            isPoolInitialized = true;
            Debug.Log($"[ShopManager] 牌序初始化完成：消耗品 {consumablePool.Count} 张，装备 {equipmentPool.Count} 张");
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

            for (int i = 0; i < hangSlots.Length; i++)
                hangSlots[i] = null;

            isPoolInitialized = false;
            Debug.Log("[ShopManager] 商店数据已重置");
        }

        #endregion
    }
}
