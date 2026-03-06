using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 物品数据基类
    /// </summary>
    public abstract class ItemData : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("物品名称")]
        public string itemName;
        
        [Tooltip("物品图标")]
        public Sprite icon;
        
        [Tooltip("物品描述")]
        [TextArea(3, 5)]
        public string description;
        
        [Header("通用属性")]
        [Tooltip("价值（金币）")]
        public int value;
        
        [Tooltip("抽取权重（用于随机抽取）")]
        public float weight = 1.0f;
        
        [Header("分类标识")]
        [Tooltip("物品大类")]
        public ItemCategory category;
        
        /// <summary>
        /// 获取物品信息字符串（用于UI显示和调试）
        /// </summary>
        public abstract string GetItemInfo();
        
        /// <summary>
        /// 获取物品简要信息（用于列表显示）
        /// </summary>
        public virtual string GetBriefInfo()
        {
            return $"{itemName} ({category})";
        }
    }
}
