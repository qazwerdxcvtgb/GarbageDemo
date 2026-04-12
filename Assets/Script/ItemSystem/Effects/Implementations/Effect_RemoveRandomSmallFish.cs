using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HandSystem;
using FishCardSystem;

namespace ItemSystem
{
    /// <summary>
    /// 随机移除手牌中 N 张小型鱼。不足 N 张时移除全部，无小型鱼则跳过。
    /// 时序：先移除视觉卡（含 slot），再移除数据层，与 UseCardHandler 一致。
    /// </summary>
    [System.Serializable]
    public class Effect_RemoveRandomSmallFish : EffectBase
    {
        [Tooltip("移除的小型鱼数量")]
        [Min(1)]
        public int count = 1;

        public override string DisplayName => "移除随机小型鱼";

        public override void Execute(EffectContext context)
        {
            if (HandManager.Instance == null) return;

            var smallFish = HandManager.Instance.GetHandCards()
                .OfType<FishData>()
                .Where(f => f.size == FishSize.Small)
                .ToList();

            if (smallFish.Count == 0)
            {
                Debug.Log("[Effect_RemoveRandomSmallFish] 手牌中无小型鱼，跳过");
                return;
            }

            int removeCount = Mathf.Min(count, smallFish.Count);

            for (int i = 0; i < removeCount; i++)
            {
                int idx = Random.Range(0, smallFish.Count);
                FishData chosen = smallFish[idx];
                smallFish.RemoveAt(idx);

                var panelUI = Object.FindObjectOfType<HandPanelUI>();
                if (panelUI != null && panelUI.CardHolder != null)
                {
                    foreach (var card in panelUI.CardHolder.GetCards())
                    {
                        if (card != null && card.cardData == chosen)
                        {
                            panelUI.CardHolder.RemoveCardAndCollapse(card);
                            break;
                        }
                    }
                }

                HandManager.Instance.RemoveCard(chosen);
                Debug.Log($"[Effect_RemoveRandomSmallFish] 移除小型鱼：{chosen.itemName}（{i + 1}/{removeCount}）");
            }
        }

        public override string GetDescription()
        {
            return count == 1
                ? "随机移除手牌中1张小型鱼"
                : $"随机移除手牌中{count}张小型鱼";
        }
    }
}
