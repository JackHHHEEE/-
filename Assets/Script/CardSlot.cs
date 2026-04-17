using UnityEngine;
using UnityEngine.EventSystems;

public class CardSlot : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null && transform.childCount == 0)
        {
            GameObject droppedCard = eventData.pointerDrag;
            CardDrag cardDrag = droppedCard.GetComponent<CardDrag>();
            CardDisplay cardDisplay = droppedCard.GetComponent<CardDisplay>();

            // 1. 如果是从手牌区拖出来的，必须先付钱
            if (cardDrag.originalParent.name == "PlayerHand")
            {
                int cardCost = cardDisplay.cardData.cost;

                // 没钱？直接拒绝，让它弹回手里
                if (!PlayerManager.Instance.TrySpendProvision(cardCost))
                {
                    return;
                }

                // 💰 走到这里说明：扣钱成功！出牌生效！

                // 2. 核心分流：判断是“法术”还是“小兵”
                // 2. 核心分流：判断是“法术”还是“小兵”
                if (cardDisplay.cardData.type == CardType.Tactic)
                {
                    Debug.Log($"【系统提示】发动战术卡：{cardDisplay.cardData.cardName}！");

                    // 👇 新增这行：为了演示，我们先让所有战术卡固定扣敌方 2 滴血！
                    EnemyManager.Instance.TakeDamage(2);

                    Destroy(droppedCard);
                    return;
                }
            }

            // 3. 如果是单位卡（或者是场上小兵换位置），就老老实实进卡槽站岗
            droppedCard.transform.SetParent(transform);

            // 强行居中对齐
            RectTransform cardRect = droppedCard.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
        }
    }
}