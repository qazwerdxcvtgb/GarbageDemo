using System.Linq;
using UnityEngine;
using HandSystem;
using FishCardSystem;

namespace ItemSystem
{
    /// <summary>
    /// 随机失去手牌中一张消耗品。无消耗品则跳过，不阻止效果触发。
    /// 时序：先移除视觉卡（含 slot），再移除数据层，与 UseCardHandler 一致。
    /// </summary>
    [System.Serializable]
    public class Effect_LoseRandomConsumable : EffectBase
    {
        public override string DisplayName => "失去随机消耗品";

        public override void Execute(EffectContext context)
        {
            if (HandManager.Instance == null) return;

            var consumables = HandManager.Instance.GetHandCards()
                .OfType<ConsumableData>()
                .ToList();

            if (consumables.Count == 0)
            {
                Debug.Log("[Effect_LoseRandomConsumable] 手牌中无消耗品，跳过");
                return;
            }

            ConsumableData chosen = consumables[Random.Range(0, consumables.Count)];

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
            Debug.Log($"[Effect_LoseRandomConsumable] 失去消耗品：{chosen.itemName}");
        }

        public override string GetDescription() => "随机失去手牌中一张消耗品";
    }
}
