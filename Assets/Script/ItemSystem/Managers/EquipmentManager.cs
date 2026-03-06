using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 装备管理器（基础版本）
    /// </summary>
    public class EquipmentManager : MonoBehaviour
    {
        private static EquipmentManager instance;
        public static EquipmentManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<EquipmentManager>();
                    if (instance == null)
                    {
                        Debug.LogError("[EquipmentManager] 场景中未找到EquipmentManager对象");
                    }
                }
                return instance;
            }
        }
        
        [Header("装备槽位")]
        [SerializeField] private EquipmentData equippedRod;      // 鱼竿槽
        [SerializeField] private EquipmentData equippedGear;     // 渔具槽
        
        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        /// <summary>
        /// 装备物品
        /// </summary>
        public bool Equip(EquipmentData equipment)
        {
            if (equipment == null)
            {
                Debug.LogWarning("[EquipmentManager] 装备数据为空");
                return false;
            }
            
            switch (equipment.slot)
            {
                case EquipmentSlot.FishingRod:
                    // 卸下旧鱼竿
                    if (equippedRod != null)
                    {
                        Unequip(EquipmentSlot.FishingRod);
                    }
                    
                    // 装备新鱼竿
                    equippedRod = equipment;
                    equippedRod.OnEquip();
                    Debug.Log($"[EquipmentManager] 装备鱼竿：{equippedRod.itemName}");
                    return true;
                
                case EquipmentSlot.FishingGear:
                    // 卸下旧渔具
                    if (equippedGear != null)
                    {
                        Unequip(EquipmentSlot.FishingGear);
                    }
                    
                    // 装备新渔具
                    equippedGear = equipment;
                    equippedGear.OnEquip();
                    Debug.Log($"[EquipmentManager] 装备渔具：{equippedGear.itemName}");
                    return true;
                
                default:
                    Debug.LogWarning($"[EquipmentManager] 未知装备槽位：{equipment.slot}");
                    return false;
            }
        }
        
        /// <summary>
        /// 卸下装备
        /// </summary>
        public EquipmentData Unequip(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.FishingRod:
                    if (equippedRod != null)
                    {
                        EquipmentData oldRod = equippedRod;
                        oldRod.OnUnequip();
                        equippedRod = null;
                        Debug.Log($"[EquipmentManager] 卸下鱼竿：{oldRod.itemName}");
                        return oldRod;
                    }
                    break;
                
                case EquipmentSlot.FishingGear:
                    if (equippedGear != null)
                    {
                        EquipmentData oldGear = equippedGear;
                        oldGear.OnUnequip();
                        equippedGear = null;
                        Debug.Log($"[EquipmentManager] 卸下渔具：{oldGear.itemName}");
                        return oldGear;
                    }
                    break;
            }
            
            return null;
        }
        
        /// <summary>
        /// 获取当前装备
        /// </summary>
        public EquipmentData GetEquipment(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.FishingRod:
                    return equippedRod;
                case EquipmentSlot.FishingGear:
                    return equippedGear;
                default:
                    return null;
            }
        }
        
        /// <summary>
        /// 检查槽位是否有装备
        /// </summary>
        public bool HasEquipment(EquipmentSlot slot)
        {
            return GetEquipment(slot) != null;
        }
        
        /// <summary>
        /// 卸下所有装备
        /// </summary>
        public void UnequipAll()
        {
            Unequip(EquipmentSlot.FishingRod);
            Unequip(EquipmentSlot.FishingGear);
        }
    }
}
