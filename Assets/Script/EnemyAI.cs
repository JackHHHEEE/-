using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("AI 的出牌配置")]
    public GameObject cardPrefab;      // 卡牌的预制体（模板）
    public Transform enemyFrontline;   // AI 的出牌区(前线)

    [Header("AI 的虚拟手牌 (放几个 CardData 进去)")]
    public List<CardData> virtualHand; // AI 脑子里的手牌数据

    [Header("法术卡展示区")]
    public Transform tacticShowcaseZone; //  专门用来展示战术卡的坑位

    // 这个函数由“结束回合”按钮呼叫
    public void StartEnemyTurn()
    {
        Debug.Log("[敌方AI] 轮到我了！开始虚空出牌...");
        StartCoroutine(PlayCardAndFlipRoutine());
    }

    IEnumerator PlayCardAndFlipRoutine()
    {
        // 假装思考 1 秒
        yield return new WaitForSeconds(1f);

        // 如果 AI 手里有牌，就打出第一张
        // 如果 AI 手里有牌
        if (virtualHand.Count > 0)
        {
            CardData cardToPlay = virtualHand[0];
            virtualHand.RemoveAt(0);

            // 🔮 【核心分流】判断这张牌到底是小兵，还是战术法术！
            if (cardToPlay.type == CardType.Tactic)
            {
                Debug.Log($"[敌方AI] 发动了战术卡：{cardToPlay.cardName}！");

                // 1. 【高调出场】生成在屏幕正中央的展示区里！
                GameObject tacticCard = Instantiate(cardPrefab, tacticShowcaseZone);

                // 强制正骨：确保它在屏幕中间站直、大小正常
                tacticCard.transform.localPosition = Vector3.zero;
                tacticCard.transform.localRotation = Quaternion.Euler(0, 0, 0);
                tacticCard.transform.localScale = Vector3.one;

                CardDisplay display = tacticCard.GetComponent<CardDisplay>();
                display.cardData = cardToPlay;
                display.SetupCard();
                display.SetFaceUp(true); // 直接翻开，高调展示给玩家看！

                // 2. 【定格 2 秒】正如主策划所要求，停留两秒钟，让玩家看清技能！
                yield return new WaitForSeconds(2f);

                // 3. 【触发伤害】
                Debug.Log("战术卡生效！玩家扣血！");
                if (PlayerManager.Instance != null)
                {
                    PlayerManager.Instance.TakeDamage(2); // 假设固定打玩家 2 血
                }

                // 4. 【灰飞烟灭】效果展示完毕，彻底销毁这张牌
                Destroy(tacticCard);
            }
        }
        else
        {
            Debug.Log("[敌方AI] 没牌了，空过。");
        }
    }
}