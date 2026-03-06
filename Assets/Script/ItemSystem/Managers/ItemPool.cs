using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 物品池管理器（从CardPool重构）
    /// </summary>
    public class ItemPool : MonoBehaviour
    {
        private static ItemPool instance;
        public static ItemPool Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<ItemPool>();
                    if (instance == null)
                    {
                        Debug.LogError("[ItemPool] 场景中未找到ItemPool对象");
                    }
                }
                return instance;
            }
        }
        
        [Header("调试信息")]
        [SerializeField] private int totalItemCount = 0;
        [SerializeField] private int fishCount = 0;
        [SerializeField] private int trashCount = 0;
        [SerializeField] private int consumableCount = 0;
        [SerializeField] private int equipmentCount = 0;
        
        private List<ItemData> allItems = new List<ItemData>();

        // === 新增结构：存储分池后的数据 ===
        // Key: 深度枚举, Value: 该深度下的3个列表
        private Dictionary<FishDepth, List<List<FishData>>> fragmentedFishPools;

        // 定义每个深度需要拆分成几个池子
        private const int POOLS_PER_DEPTH = 3;

        // 非鱼类通用牌堆结构
        // 使用字典统一管理：Key=物品类型, Value=该类型的洗牌后列表
        private Dictionary<ItemCategory, List<ItemData>> categoryDecks = new Dictionary<ItemCategory, List<ItemData>>();

        // 为了在Inspector中方便调试，我们仍然保留这几个List，但在逻辑中只作为“显示镜像”
        // 实际逻辑走上面的 Dictionary
        [Header("运行时牌堆状态 (调试用)")]
        [SerializeField] private List<TrashData> debugTrashPool;
        [SerializeField] private List<ConsumableData> debugConsumablePool;
        [SerializeField] private List<EquipmentData> debugEquipmentPool;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            LoadAllItems();
        }
        
        /// <summary>
        /// 加载所有物品数据
        /// </summary>
        private void LoadAllItems()
        {
            // 加载所有ItemData子类
            allItems.Clear();
            
            // 加载鱼类
            FishData[] fish = Resources.LoadAll<FishData>("Items/Fish");
            allItems.AddRange(fish);
            fishCount = fish.Length;
            
            // 加载杂鱼
            TrashData[] trash = Resources.LoadAll<TrashData>("Items/Trash");
            allItems.AddRange(trash);
            trashCount = trash.Length;
            
            // 加载消耗品
            ConsumableData[] consumables = Resources.LoadAll<ConsumableData>("Items/Consumable");
            allItems.AddRange(consumables);
            consumableCount = consumables.Length;
            
            // 加载装备
            EquipmentData[] equipment = Resources.LoadAll<EquipmentData>("Items/Equipment");
            allItems.AddRange(equipment);
            equipmentCount = equipment.Length;
            
            totalItemCount = allItems.Count;
            
            Debug.Log($"[ItemPool] 加载完成：总计{totalItemCount}个物品 " +
                      $"(鱼类:{fishCount} 杂鱼:{trashCount} 消耗品:{consumableCount} 装备:{equipmentCount})");
            
            // 加载完所有物品后，立即初始化鱼类卡牌池
            InitializeFragmentedPools();
            Debug.Log($"[ItemPool] 加载完成。鱼类卡牌池初始化完毕。");

            // 初始化通用牌堆 
            categoryDecks.Clear(); // 清空字典防止重复
            InitializeDeck(ItemCategory.Trash);
            InitializeDeck(ItemCategory.Consumable);
            InitializeDeck(ItemCategory.Equipment);

            // 更新调试视图 (可选)
            UpdateDebugLists();
        }

        /// <summary>
        /// 初始化 9 个分池 (3深度 x 3列表)
        /// </summary>
        private void InitializeFragmentedPools()
        {
            fragmentedFishPools = new Dictionary<FishDepth, List<List<FishData>>>();

            // 1. 获取所有鱼类数据
            var allFish = allItems.OfType<FishData>().ToList();

            // 2. 遍历深度枚举 
            foreach (FishDepth depth in System.Enum.GetValues(typeof(FishDepth)))
            {
                // A. 筛选出当前深度的所有鱼
                List<FishData> fishInDepth = allFish.Where(f => f.depth == depth).ToList();

                // B. 先完全打乱顺序 (Fisher-Yates Shuffle)
                ShuffleList(fishInDepth);

                // C. 创建该深度下的 3 个空列表
                List<List<FishData>> subPools = new List<List<FishData>>();
                for (int i = 0; i < POOLS_PER_DEPTH; i++)
                {
                    subPools.Add(new List<FishData>());
                }

                // D. 轮询分发 (Round-Robin) 实现平均分配
                // 比如有 10 条鱼：
                // 列表0: 0, 3, 6, 9 (4条)
                // 列表1: 1, 4, 7    (3条)
                // 列表2: 2, 5, 8    (3条)
                for (int i = 0; i < fishInDepth.Count; i++)
                {
                    int poolIndex = i % POOLS_PER_DEPTH; // 取余数决定放进哪个池子
                    subPools[poolIndex].Add(fishInDepth[i]);
                }

                // E. 存入字典
                fragmentedFishPools.Add(depth, subPools);

                // 调试日志
                Debug.Log($"[ItemPool] 深度 {depth} 分池情况: " +
                          $"池1[{subPools[0].Count}], 池2[{subPools[1].Count}], 池3[{subPools[2].Count}]");
            }
        }

        /// <summary>
        /// 获取特定的分池列表
        /// </summary>
        /// <param name="depth">深度类型</param>
        /// <param name="poolIndex">池子索引 (0-2)</param>
        public List<FishData> GetFragmentedPool(FishDepth depth, int poolIndex)
        {
            if (fragmentedFishPools.ContainsKey(depth))
            {
                var pools = fragmentedFishPools[depth];
                // 防止越界，用取余保护一下
                int safeIndex = Mathf.Abs(poolIndex) % pools.Count;
                return pools[safeIndex];
            }

            Debug.LogWarning($"[ItemPool] 未找到深度 {depth} 的分池信息");
            return new List<FishData>();
        }

        /// <summary>
        /// 获取指定深度+子池索引的顶牌（只读，不移除）
        /// </summary>
        /// <param name="depth">深度</param>
        /// <param name="poolIndex">子池索引（0-2）</param>
        /// <returns>顶牌数据，如果牌堆为空返回null</returns>
        public FishData PeekTopCard(FishDepth depth, int poolIndex)
        {
            if (!fragmentedFishPools.ContainsKey(depth))
            {
                return null;
            }
            
            var pools = fragmentedFishPools[depth];
            int safeIndex = Mathf.Abs(poolIndex) % pools.Count;
            var targetPool = pools[safeIndex];
            
            return targetPool.Count > 0 ? targetPool[0] : null;
        }

        /// <summary>
        /// 列表洗牌算法 (Fisher-Yates)
        /// </summary>
        private void ShuffleList<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        /// <summary>
        /// 【只读】获取当前关卡应该抽取的卡牌列表
        /// 注意：此方法不会从池中移除卡牌！移除操作请调用 RemoveTopCard
        /// </summary>
        public List<FishData> GetCardsForStage(FishDepth currentStageDepth)
        {
            List<FishData> resultCards = new List<FishData>();

            // 1. 获取浅水层3张
            resultCards.AddRange(PeekTopThreeFromDepth(FishDepth.Depth1));

            // 2. 深度 >= 中层，获取中层3张
            if (currentStageDepth >= FishDepth.Depth2)
            {
                resultCards.AddRange(PeekTopThreeFromDepth(FishDepth.Depth2));
            }

            // 3. 深度 >= 深水，获取深水3张
            if (currentStageDepth >= FishDepth.Depth3)
            {
                resultCards.AddRange(PeekTopThreeFromDepth(FishDepth.Depth3));
            }

            return resultCards;
        }

        /// <summary>
        /// [内部方法] 查看指定深度3个池子的顶层卡牌
        /// </summary>
        private List<FishData> PeekTopThreeFromDepth(FishDepth depth)
        {
            List<FishData> peekedItems = new List<FishData>();

            if (!fragmentedFishPools.ContainsKey(depth))
            {
                return peekedItems;
            }

            var threePools = fragmentedFishPools[depth];

            // 依次遍历 3 个子池子
            for (int i = 0; i < threePools.Count; i++)
            {
                var singlePool = threePools[i];

                if (singlePool.Count > 0)
                {
                    // === 核心修改：只获取引用，不调用 RemoveAt ===
                    peekedItems.Add(singlePool[0]);
                }
                else
                {
                    // 如果池子空了，可以根据需求决定是跳过还是填 null
                    // 这里保持不填入，这样返回的 List 长度可能会小于 3
                }
            }

            return peekedItems;
        }

        /// <summary>
        /// 【精准移除】从池中移除指定的鱼类卡牌
        /// 逻辑：根据鱼的深度找到对应的3个池子，检查哪个池子的顶部是这张卡，然后移除。
        /// </summary>
        /// <param name="cardToRemove">要移除的鱼类数据引用</param>
        /// <returns>是否移除成功</returns>
        public bool RemoveCard(FishData cardToRemove)
        {
            if (cardToRemove == null) return false;

            // 1. 利用 FishData 自带的 depth 属性快速定位大组
            // 这样就不需要遍历所有的深度了，大大节省性能
            FishDepth targetDepth = cardToRemove.depth;

            if (!fragmentedFishPools.ContainsKey(targetDepth))
            {
                Debug.LogWarning($"[ItemPool] 移除失败：找不到深度 {targetDepth} 的分池数据");
                return false;
            }

            // 获取该深度下的 3 个子列表
            List<List<FishData>> targetPools = fragmentedFishPools[targetDepth];

            // 2. 遍历这 3 个列表，看谁的【第一张】是这张卡
            foreach (var pool in targetPools)
            {
                // 确保池子不为空
                if (pool.Count > 0)
                {
                    // 检查第一张是否匹配（因为我们只允许抽最上面的）
                    if (pool[0] == cardToRemove)
                    {
                        pool.RemoveAt(0);
                        Debug.Log($"[ItemPool] 已移除卡牌: {cardToRemove.itemName} (深度: {targetDepth})");
                        return true; // 找到并移除后立即返回
                    }
                }
            }

            Debug.LogWarning($"[ItemPool] 移除失败：在 {targetDepth} 的顶层池中未找到卡牌 {cardToRemove.itemName}");
            return false;
        }

        /// <summary>
        /// 通用初始化牌堆方法
        /// 根据传入的类型，从allItems中筛选、洗牌并存入字典
        /// </summary>
        /// <param name="category">物品类型</param>
        public void InitializeDeck(ItemCategory category)
        {
            // 1. 筛选
            List<ItemData> pool = allItems.Where(item => item.category == category).ToList();

            if (pool.Count == 0)
            {
                Debug.LogWarning($"[ItemPool] 初始化警告：类型 {category} 没有找到任何物品数据！");
            }

            // 2. 洗牌
            ShuffleList(pool);

            // 3. 存入字典
            if (categoryDecks.ContainsKey(category))
            {
                categoryDecks[category] = pool;
            }
            else
            {
                categoryDecks.Add(category, pool);
            }

            Debug.Log($"[ItemPool] 已生成 {category} 牌堆，数量: {pool.Count}");
        }

        #region 鱼类专用筛选和抽取（保留原接口）

        /// <summary>
        /// 筛选并抽取鱼类卡牌（仅限鱼类）
        /// </summary>
        public FishData DrawCardWithFilter(FishDepth? depth = null, FishSize? size = null)
        {
            List<FishData> filtered = FilterFishCards(depth, size);
            return DrawRandomFish(filtered);
        }
        
        /// <summary>
        /// 抽取指定数量的不重复鱼类卡牌（仅限鱼类）
        /// </summary>
        public List<FishData> DrawUniqueCards(int count, FishDepth? depth = null, FishSize? size = null)
        {
            List<FishData> filtered = FilterFishCards(depth, size);
            return DrawUniqueFish(count, filtered);
        }
        
        /// <summary>
        /// 筛选鱼类卡牌
        /// </summary>
        private List<FishData> FilterFishCards(FishDepth? depth = null, FishSize? size = null)
        {
            var fishCards = allItems.OfType<FishData>();
            
            if (depth.HasValue)
            {
                fishCards = fishCards.Where(f => f.depth == depth.Value);
            }
            
            if (size.HasValue)
            {
                fishCards = fishCards.Where(f => f.size == size.Value);
            }
            
            return fishCards.ToList();
        }
        
        /// <summary>
        /// 从鱼类池中随机抽取一张（加权随机）
        /// </summary>
        private FishData DrawRandomFish(List<FishData> pool)
        {
            if (pool == null || pool.Count == 0)
            {
                Debug.LogWarning("[ItemPool] 鱼类池为空");
                return null;
            }
            
            float totalWeight = pool.Sum(f => f.weight);
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;
            
            foreach (var fish in pool)
            {
                currentWeight += fish.weight;
                if (randomValue <= currentWeight)
                {
                    return fish;
                }
            }
            
            return pool[pool.Count - 1];
        }
        
        /// <summary>
        /// 抽取不重复鱼类（按名称去重，不放回）
        /// </summary>
        private List<FishData> DrawUniqueFish(int count, List<FishData> pool)
        {
            if (pool == null || pool.Count == 0)
            {
                Debug.LogWarning("[ItemPool] 鱼类池为空");
                return new List<FishData>();
            }
            
            var uniquePool = pool.GroupBy(f => f.itemName)
                                 .Select(g => g.First())
                                 .ToList();
            
            int actualCount = Mathf.Min(count, uniquePool.Count);
            List<FishData> result = new List<FishData>();
            List<FishData> tempPool = new List<FishData>(uniquePool);
            
            for (int i = 0; i < actualCount; i++)
            {
                if (tempPool.Count == 0) break;
                
                FishData drawn = DrawRandomFish(tempPool);
                if (drawn != null)
                {
                    result.Add(drawn);
                    tempPool.RemoveAll(f => f.itemName == drawn.itemName);
                }
            }
            
            return result;
        }

        #endregion

        #region 通用抽取（新增重载）

        /// <summary>
        /// 通用抽取方法 (重载版本)
        /// 根据传入的类型，从对应牌堆抽一张
        /// </summary>
        /// <param name="category">要抽取的类型</param>
        /// <returns>抽到的物品 (如果没牌了返回null)</returns>
        public ItemData DrawItem(ItemCategory category)
        {
            // 鱼类有特殊的抽取逻辑(分层、去重)，一般走DrawCardsForStage
            // 如果你非要用这个方法抽鱼，这里只是简单的随机抽取
            if (category == ItemCategory.Fish)
            {
                Debug.LogWarning("[ItemPool] 建议使用 DrawCardsForStage 来抽取鱼类以符合关卡逻辑。");
            }

            if (!categoryDecks.ContainsKey(category))
            {
                Debug.LogWarning($"[ItemPool] 无法抽取：不存在类型 {category} 的牌堆。");
                return null;
            }

            List<ItemData> deck = categoryDecks[category];

            if (deck.Count == 0)
            {
                Debug.LogWarning($"[ItemPool] {category} 牌堆已空！");
                return null;
            }

            // 1. 取出第一张
            ItemData drawnItem = deck[0];

            // 2. 移除
            deck.RemoveAt(0);

            // 更新调试视图 (如果需要实时在Inspector看到变化)
            // UpdateDebugLists(); 

            return drawnItem;
        }

        /// <summary>
        /// 按物品类型抽取指定数量（加权随机）
        /// </summary>
        /// <param name="category">物品类型</param>
        /// <param name="count">抽取数量</param>
        /// <returns>抽取结果列表（可能少于请求数量）</returns>
        public List<ItemData> DrawItemsByCategory(ItemCategory category, int count)
        {
            var pool = allItems.Where(item => item.category == category).ToList();
            
            if (pool.Count == 0)
            {
                Debug.LogWarning($"[ItemPool] {category}类型物品池为空");
                return new List<ItemData>();
            }
            
            List<ItemData> result = new List<ItemData>();
            
            for (int i = 0; i < count; i++)
            {
                ItemData drawn = DrawRandomItem(pool);
                if (drawn != null)
                {
                    result.Add(drawn);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 按物品类型抽取不重复物品（按名称去重）
        /// </summary>
        public List<ItemData> DrawUniqueItemsByCategory(ItemCategory category, int count)
        {
            var pool = allItems.Where(item => item.category == category).ToList();
            
            if (pool.Count == 0)
            {
                Debug.LogWarning($"[ItemPool] {category}类型物品池为空");
                return new List<ItemData>();
            }
            
            var uniquePool = pool.GroupBy(item => item.itemName)
                                 .Select(g => g.First())
                                 .ToList();
            
            int actualCount = Mathf.Min(count, uniquePool.Count);
            List<ItemData> result = new List<ItemData>();
            List<ItemData> tempPool = new List<ItemData>(uniquePool);
            
            for (int i = 0; i < actualCount; i++)
            {
                if (tempPool.Count == 0) break;
                
                ItemData drawn = DrawRandomItem(tempPool);
                if (drawn != null)
                {
                    result.Add(drawn);
                    tempPool.RemoveAll(item => item.itemName == drawn.itemName);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 从物品池中随机抽取一个（加权随机）
        /// </summary>
        private ItemData DrawRandomItem(List<ItemData> pool)
        {
            if (pool == null || pool.Count == 0)
            {
                return null;
            }
            
            float totalWeight = pool.Sum(item => item.weight);
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;
            
            foreach (var item in pool)
            {
                currentWeight += item.weight;
                if (randomValue <= currentWeight)
                {
                    return item;
                }
            }
            
            return pool[pool.Count - 1];
        }
        
        #endregion
        
        #region 查询方法
        
        /// <summary>
        /// 获取所有物品（副本）
        /// </summary>
        public List<ItemData> GetAllItems()
        {
            return new List<ItemData>(allItems);
        }
        
        /// <summary>
        /// 获取指定类型的所有物品
        /// </summary>
        public List<ItemData> GetItemsByCategory(ItemCategory category)
        {
            return allItems.Where(item => item.category == category).ToList();
        }
        
        /// <summary>
        /// 根据名称查找物品
        /// </summary>
        public ItemData GetItemByName(string itemName)
        {
            return allItems.FirstOrDefault(item => item.itemName == itemName);
        }
        
        /// <summary>
        /// 获取物品总数
        /// </summary>
        public int GetItemCount()
        {
            return allItems.Count;
        }
        
        #endregion
        
        #region 工具方法
        
        /// <summary>
        /// 重新加载所有物品
        /// </summary>
        public void ReloadAllItems()
        {
            LoadAllItems();
        }
        
        /// <summary>
        /// 打印所有物品信息（调试用）
        /// </summary>
        [ContextMenu("调试/打印所有物品")]
        public void DebugPrintAllItems()
        {
            Debug.Log($"=== ItemPool 物品列表 ===");
            Debug.Log($"总计：{totalItemCount}个物品");
            
            foreach (var item in allItems)
            {
                Debug.Log(item.GetBriefInfo());
            }
        }

        // 仅用于Inspector显示的同步方法
        private void UpdateDebugLists()
        {
            if (categoryDecks.ContainsKey(ItemCategory.Trash))
                debugTrashPool = categoryDecks[ItemCategory.Trash].Cast<TrashData>().ToList();

            if (categoryDecks.ContainsKey(ItemCategory.Consumable))
                debugConsumablePool = categoryDecks[ItemCategory.Consumable].Cast<ConsumableData>().ToList();

            if (categoryDecks.ContainsKey(ItemCategory.Equipment))
                debugEquipmentPool = categoryDecks[ItemCategory.Equipment].Cast<EquipmentData>().ToList();
        }

        #endregion
    }
}
