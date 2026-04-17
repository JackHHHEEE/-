using System.Collections;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    [Header("战场扫描雷达")]
    public Transform playerFrontline;
    public Transform enemyFrontline;

    void Awake()
    {
        Instance = this;
    }

    public void StartBattlePhase()
    {
        StartCoroutine(ResolveBattleRoutine());
    }

    IEnumerator ResolveBattleRoutine()
    {
        Debug.Log("⚔️ --- 战斗结算阶段开始 --- ⚔️");

        for (int i = 0; i < 3; i++)
        {
            Transform playerSlot = playerFrontline.GetChild(i);
            Transform enemySlot = enemyFrontline.GetChild(i);

            CardDisplay playerCard = playerSlot.childCount > 0 ? playerSlot.GetChild(0).GetComponent<CardDisplay>() : null;
            CardDisplay enemyCard = enemySlot.childCount > 0 ? enemySlot.GetChild(0).GetComponent<CardDisplay>() : null;

            // 💥 规则 1：硬碰硬
            if (playerCard != null && enemyCard != null)
            {
                bool didAttack = false;

                // 玩家没睡觉，就砍一刀
                if (!playerCard.isSleeping)
                {
                    enemyCard.TakeDamage(playerCard.cardData.attack);
                    Debug.Log($"[{playerCard.cardData.cardName}] 砍了 [{enemyCard.cardData.cardName}] 一刀！");
                    didAttack = true;
                }
                else Debug.Log($"[{playerCard.cardData.cardName}] 还在睡觉 Zzz...");

                // 敌人没睡觉，也砍一刀
                if (!enemyCard.isSleeping)
                {
                    playerCard.TakeDamage(enemyCard.cardData.attack);
                    Debug.Log($"[{enemyCard.cardData.cardName}] 砍了 [{playerCard.cardData.cardName}] 一刀！");
                    didAttack = true;
                }
                else Debug.Log($"[{enemyCard.cardData.cardName}] 还在睡觉 Zzz...");

                if (didAttack) yield return new WaitForSeconds(0.8f);
            }
            // 🗡️ 规则 2：打敌方主将
            else if (playerCard != null && enemyCard == null)
            {
                if (!playerCard.isSleeping)
                {
                    Debug.Log($"🗡️ 槽位 {i + 1} 空门！[{playerCard.cardData.cardName}] 直接攻击 Boss Cao！");
                    EnemyManager.Instance.TakeDamage(playerCard.cardData.attack);
                    yield return new WaitForSeconds(0.8f);
                }
                else Debug.Log($"槽位 {i + 1} 空门！但 [{playerCard.cardData.cardName}] 还在睡觉 Zzz...");
            }
            // 🚨 规则 2反转：打我方主将
            else if (playerCard == null && enemyCard != null)
            {
                if (!enemyCard.isSleeping)
                {
                    Debug.Log($"🚨 槽位 {i + 1} 空门！[{enemyCard.cardData.cardName}] 偷袭 Bob Liu！");
                    PlayerManager.Instance.TakeDamage(enemyCard.cardData.attack);
                    yield return new WaitForSeconds(0.8f);
                }
                else Debug.Log($"槽位 {i + 1} 空门！但 [{enemyCard.cardData.cardName}] 还在睡觉 Zzz...");
            }
        }

        Debug.Log("🧹 --- 打扫战场 --- 🧹");
        CleanupDeadCards(playerFrontline);
        CleanupDeadCards(enemyFrontline);

        Debug.Log("⏰ --- 叫醒所有活下来的士兵 --- ⏰");
        // 这回合结束了，场上还没死的兵，下回合就能打人了！
        WakeUpCards(playerFrontline);
        WakeUpCards(enemyFrontline);

        Debug.Log("✅ 战斗结算完毕！");
    }

    void CleanupDeadCards(Transform frontline)
    {
        foreach (Transform slot in frontline)
        {
            if (slot.childCount > 0)
            {
                CardDisplay card = slot.GetChild(0).GetComponent<CardDisplay>();
                if (card != null && card.currentHP <= 0)
                {
                    Debug.Log($"☠️ {card.cardData.cardName} 阵亡了...");
                    Destroy(card.gameObject);
                }
            }
        }
    }

    // ⏰ 叫醒士兵的专用函数
    void WakeUpCards(Transform frontline)
    {
        foreach (Transform slot in frontline)
        {
            if (slot.childCount > 0)
            {
                CardDisplay card = slot.GetChild(0).GetComponent<CardDisplay>();
                if (card != null)
                {
                    card.isSleeping = false; // 睁开眼睛，下回合火力全开！
                }
            }
        }
    }
}