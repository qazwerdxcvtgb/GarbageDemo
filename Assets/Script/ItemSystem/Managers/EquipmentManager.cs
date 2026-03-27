using System;
using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 装备管理器
    /// 管理鱼竿和渔具两个槽位的装备/卸下，触发被动效果注册/注销，并对外提供状态变化事件。
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

        #region 事件

        /// <summary>装备成功时触发（包含新装备数据）</summary>
        public event Action<EquipmentData> OnEquipped;

        /// <summary>卸下装备时触发（包含被卸下的装备数据）</summary>
        public event Action<EquipmentData> OnUnequipped;

        #endregion

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
                    if (equippedRod != null)
                        Unequip(EquipmentSlot.FishingRod);

                    equippedRod = equipment;
                    equippedRod.OnEquip();
                    OnEquipped?.Invoke(equippedRod);
                    Debug.Log($"[EquipmentManager] 装备鱼竿：{equippedRod.itemName}");
                    return true;

                case EquipmentSlot.FishingGear:
                    if (equippedGear != null)
                        Unequip(EquipmentSlot.FishingGear);

                    equippedGear = equipment;
                    equippedGear.OnEquip();
                    OnEquipped?.Invoke(equippedGear);
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
                        OnUnequipped?.Invoke(oldRod);
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
                        OnUnequipped?.Invoke(oldGear);
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
