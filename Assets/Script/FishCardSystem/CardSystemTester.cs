using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ItemSystem;

namespace FishCardSystem
{
    /// <summary>
    /// 卡牌系统测试脚本
    /// 根据 FishCardHolder 的槽位数量自动生成卡牌，用于验证交互效果。
    /// 使用方式：
    ///   1. 在场景中任意 GameObject 上挂载本组件
    ///   2. 将场景中的 FishCardHolder 实例拖入 Card Holder 字段
    ///   3. 将 Assets/Prefab/FishCardSystem/FishCard.prefab 拖入 Fish Card Prefab 字段
    ///   4. Test Fish Data 可留空（自动从 Resources/Items/Fish 随机加载），也可手动指定
    /// </summary>
    public class CardSystemTester : MonoBehaviour
    {
        [Header("必填引用")]
        [SerializeField] private FishCardHolder cardHolder;
        [SerializeField] private GameObject fishCardPrefab;

        [Header("可选数据（留空则自动从 Resources 随机加载）")]
        [SerializeField] private List<FishData> testFishData;

        private IEnumerator Start()
        {
            if (cardHolder == null)
            {
                Debug.LogError("[CardSystemTester] 未指定 FishCardHolder，测试脚本已停止。");
                yield break;
            }

            if (fishCardPrefab == null)
            {
                Debug.LogError("[CardSystemTester] 未指定 FishCard 预制体，测试脚本已停止。");
                yield break;
            }

            // 等待一帧，确保 FishCardHolder.Start() 已完成（槽位已生成）
            yield return null;

            // 若未手动指定数据，从 Resources 中自动加载所有鱼数据
            if (testFishData == null || testFishData.Count == 0)
            {
                FishData[] loaded = Resources.LoadAll<FishData>("Items/Fish");
                testFishData = new List<FishData>(loaded);

                if (testFishData.Count == 0)
                {
                    Debug.LogWarning("[CardSystemTester] Resources/Items/Fish 中未找到任何 FishData，将生成无数据卡牌。");
                }
            }

            int slotCount = cardHolder.transform.childCount;
            if (slotCount == 0)
            {
                Debug.LogWarning("[CardSystemTester] FishCardHolder 中没有槽位，请检查 cardsToSpawn 配置。");
                yield break;
            }

            for (int i = 0; i < slotCount; i++)
            {
                // Instantiate 时 FishCard.Awake() 同步执行，事件已初始化
                GameObject cardObj = Instantiate(fishCardPrefab);
                cardObj.name = $"TestCard_{i}";

                FishCard card = cardObj.GetComponent<FishCard>();
                if (card == null)
                {
                    Debug.LogError("[CardSystemTester] FishCard 预制体缺少 FishCard 组件！");
                    Destroy(cardObj);
                    continue;
                }

                // 随机分配鱼数据（如果有数据可用）
                if (testFishData.Count > 0)
                {
                    FishData data = testFishData[Random.Range(0, testFishData.Count)];
                    card.cardData = data;
                }

                // 通过 AddCard 注册到 Holder（设置父节点 + 绑定事件）
                cardHolder.AddCard(card, i);
            }

            Debug.Log($"[CardSystemTester] 已生成 {slotCount} 张测试卡牌。");
        }
    }
}
