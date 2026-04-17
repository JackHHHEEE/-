using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    [Header("敌将状态")]
    public string enemyName = "Bob Liu";
    public int currentHP = 10;
    public int currentProvision = 4;

    [Header("新版武将 UI 绑定")]
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI provisionText;

    [Header("AI 出牌配置")]
    public Transform enemyFrontline; // 敌人的前线卡槽
    public List<CardData> enemyDeck; // 敌人的牌库
    public Transform playerFrontline;
    public GameObject cardPrefab;    // 还是用同一个预制体

    void Awake() { Instance = this; }
    void Start() { UpdateUI(); }

    public void UpdateUI()
    {
        if (hpText != null) hpText.text = currentHP.ToString();
        if (provisionText != null) provisionText.text = currentProvision.ToString();
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;
        UpdateUI();
        if (currentHP == 0)
        {
            Debug.Log("🏆 敌将讨死！");
            GameManager.Instance.GameOver(true); // 👈 呼叫胜利画面
        }
    }

    // 🤖 AI 的回合逻辑
    public void ExecuteAITurn()
    {
        Debug.Log("🤖 Bob Liu 正在思考...");

        // 👇 修复 1：每回合给 AI 重新发放 4 点粮草，防止他破产！
        currentProvision = 4;
        UpdateUI();

        // 👇 修复 2：加上这句，如果他真的没牌了，控制台会明确告诉你
        if (enemyDeck.Count == 0)
        {
            Debug.Log("⚠️ 报告主公！Bob Liu 的牌库已经彻底打空了！");
            return;
        }

        // 1. 检查是否有空位
        foreach (Transform slot in enemyFrontline)
        {
            if (slot.childCount == 0 && enemyDeck.Count > 0)
            {
                // 2. 模拟从牌库抽一张
                CardData toPlay = enemyDeck[0];

                // 3. 检查钱够不够
                if (currentProvision >= toPlay.cost)
                {
                    currentProvision -= toPlay.cost;
                    enemyDeck.RemoveAt(0);

                    // 👇 核心修复：判断是战术卡还是单位卡！
                    if (toPlay.type == CardType.Tactic)
                    {
                        Debug.Log($"🔥 敌将 Bob Liu 发动了战术卡：{toPlay.cardName}！");
                        // 就像你之前测法术一样，让 AI 的法术固定扣你 2 滴血作为测试
                        PlayerManager.Instance.currentHP -= 2;
                        PlayerManager.Instance.UpdateUI();
                    }
                    else
                    {
                        // 是单位卡，才实例化并放到槽位里
                        GameObject newCard = Instantiate(cardPrefab, slot);
                        newCard.transform.localRotation = Quaternion.Euler(0, 0, 180);

                        CardDisplay display = newCard.GetComponent<CardDisplay>();
                        display.cardData = toPlay;
                        display.SetupCard();
                        display.isNewlyPlayed = true;

                        Debug.Log($"🤖 Bob Liu 派出了 {toPlay.cardName}！");
                    }

                    UpdateUI();
                    break; // 每回合 AI 只出一张
                }
            }
        }
        // ==========================================
        // 👇 敌军出完牌后，场上的老兵开始发起反击！
        // ==========================================
        Debug.Log("⚔️ 敌军吹响反击号角！");
        for (int i = 0; i < 3; i++)
        {
            Transform enemySlot = enemyFrontline.GetChild(i);
            Transform playerSlot = playerFrontline.GetChild(i);

            if (enemySlot.childCount > 0)
            {
                CardDisplay enemyCard = enemySlot.GetChild(0).GetComponent<CardDisplay>();
                if (enemyCard == null) continue;

                // 1. 检查是不是刚下的新兵？有没有冲锋？
                if (enemyCard.isNewlyPlayed)
                {
                    if (enemyCard.cardData.keyword == Keyword.Rush)
                    {
                        enemyCard.isNewlyPlayed = false;
                    }
                    else
                    {
                        enemyCard.isNewlyPlayed = false;
                        continue; // 敌人的新兵也要乖乖罚站！
                    }
                }

                int enemyAtk = enemyCard.cardData.attack;

                // 2. 敌军雷达：扫描你的阵地有没有 [Taunt] 嘲讽怪
                CardDisplay tauntTarget = null;
                foreach (Transform pSlot in playerFrontline)
                {
                    if (pSlot.childCount > 0)
                    {
                        CardDisplay pCard = pSlot.GetChild(0).GetComponent<CardDisplay>();
                        if (pCard != null && pCard.cardData.keyword == Keyword.Taunt)
                        {
                            tauntTarget = pCard;
                            break;
                        }
                    }
                }

                // 3. 敌军开打！
                if (playerSlot.childCount > 0)
                {
                    // 对面有你的兵，互殴！
                    CardDisplay playerCard = playerSlot.GetChild(0).GetComponent<CardDisplay>();
                    playerCard.TakeDamage(enemyAtk);
                    int pAtk = playerCard.cardData.attack;
                    if (pAtk > 0) enemyCard.TakeDamage(pAtk);
                }
                else
                {
                    // 对面是空的，检查有没有被你的嘲讽怪吸走
                    if (tauntTarget != null)
                    {
                        tauntTarget.TakeDamage(enemyAtk);
                        int tauntAtk = tauntTarget.cardData.attack;
                        if (tauntAtk > 0) enemyCard.TakeDamage(tauntAtk);
                    }
                    else
                    {
                        // 狠狠打你的脸！
                        if (enemyAtk > 0)
                        {
                            PlayerManager.Instance.TakeDamage(enemyAtk);
                        }
                    }
                }
            }
        }
    }
}