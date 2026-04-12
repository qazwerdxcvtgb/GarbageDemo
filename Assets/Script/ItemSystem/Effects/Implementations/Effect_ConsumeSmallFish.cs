using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HandSystem;
using FishCardSystem;

namespace ItemSystem
{
    /// <summary>
    /// 捕获时额外消耗手牌中一条随机小型鱼。
    /// 手牌中无小型鱼时不消耗，不阻止捕获。
    /// 时序：先移除视觉卡（含 slot），再移除数据层，与 UseCardHandler/ShopSellController 一致。
    /// </summary>
    [System.Serializable]
    public class Effect_ConsumeSmallFish : EffectBase
    {
        public override string DisplayName => "捕获消耗小型鱼";

        public override void Execute(EffectContext context)
        {
            if (HandManager.Instance == null) return;

            var handCards = HandManager.Instance.GetHandCards();
            var smallFish = handCards
                .OfType<FishData>()
                .Where(f => f.size == FishSize.Small)
                .ToList();

            if (smallFish.Count == 0)
            {
                Debug.Log("[Effect_ConsumeSmallFish] 手牌中无小型鱼，跳过消耗");
                return;
            }

            FishData chosen = smallFish[Random.Range(0, smallFish.Count)];

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
            Debug.Log($"[Effect_ConsumeSmallFish] 消耗小型鱼：{chosen.itemName}");
        }

        public override string GetDescription() => "捕获时额外消耗手牌中一条小型鱼";
    }
}
