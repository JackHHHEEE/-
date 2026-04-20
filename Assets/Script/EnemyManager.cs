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
    // 🤖 AI 的回合逻辑 (终极版：经济 + 战斗)
    public void ExecuteAITurn()
    {
        Debug.Log("🤖 Bob Liu 开始了回合...");
        StartCoroutine(AITurnRoutine());
    }

    private System.Collections.IEnumerator AITurnRoutine()
    {
        // ==========================================
        // 🌾 阶段一：补给与维持 (Logistics & Provision Phase)
        // ==========================================
        int totalProduce = 0;
        int totalUpkeep = 0;

        // 1. 统计种田收益和工资
        for (int i = 0; i < 3; i++)
        {
            Transform slot = enemyFrontline.GetChild(i);
            if (slot.childCount > 0)
            {
                CardDisplay card = slot.GetChild(0).GetComponent<CardDisplay>();
                if (card != null)
                {
                    totalUpkeep += card.cardData.upkeep;
                    if (card.cardData.keyword == Keyword.Produce) totalProduce += card.cardData.produceAmount;
                }
            }
        }

        // 2. 发低保并封顶
        int baseIncome = 4;
        currentProvision += (baseIncome + totalProduce);
        currentProvision = Mathf.Clamp(currentProvision, 0, 10);
        
        Debug.Log($"🤖 敌方发薪：获得 {baseIncome}+{totalProduce}。当前余额: {currentProvision}");

        // 3. 挨个发工资，没钱就饿死！
        for (int i = 0; i < 3; i++)
        {
            Transform slot = enemyFrontline.GetChild(i);
            if (slot.childCount > 0)
            {
                CardDisplay card = slot.GetChild(0).GetComponent<CardDisplay>();
                if (card != null)
                {
                    if (currentProvision >= card.cardData.upkeep)
                    {
                        currentProvision -= card.cardData.upkeep;
                        card.isSleeping = false; // 唤醒，准备打架
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ 敌方断粮！{card.cardData.cardName} 扣1血并罚站！");
                        card.TakeDamage(1);
                        if (card != null) card.isSleeping = true;
                    }
                }
            }
        }
        UpdateUI();

        // 💀 4. 破产清算 (Starvation Loss)
        CheckStarvationLoss();

        yield return new WaitForSeconds(0.5f); // 停顿一下

        // ==========================================
        // 🃏 阶段二：行动 (Action Phase - AI 出牌)
        // ==========================================
        if (enemyDeck.Count > 0)
        {
            CardData toPlay = enemyDeck[0];
            
            if (currentProvision >= toPlay.cost)
            {
                currentProvision -= toPlay.cost;
                enemyDeck.RemoveAt(0);

                if (toPlay.type == CardType.Tactic)
                {
                    Debug.Log($"🔥 敌将发动战术卡：{toPlay.cardName}！(此处暂用扣2血代替)");
                    PlayerManager.Instance.TakeDamage(2);
                }
                else
                {
                    Transform emptySlot = null;
                    foreach (Transform slot in enemyFrontline)
                    {
                        if (slot.childCount == 0) { emptySlot = slot; break; }
                    }

                    if (emptySlot != null)
                    {
                        GameObject newCard = Instantiate(cardPrefab, emptySlot);
                        newCard.transform.localRotation = Quaternion.Euler(0, 0, 180);
                        CardDisplay display = newCard.GetComponent<CardDisplay>();
                        display.cardData = toPlay;
                        display.SetupCard();
                        display.isSleeping = true; // 刚下的兵必须睡觉 (召唤失调)
                        
                        if (toPlay.keyword == Keyword.Rush)
                        {
                            display.isSleeping = false; // 冲锋怪立刻苏醒
                            Debug.Log($"⚡ 敌军 {toPlay.cardName} 发动突袭！");
                        }
                    }
                }
                UpdateUI();
            }
        }

        yield return new WaitForSeconds(1f); // 停顿一下

        // ==========================================
        // ⚔️ 阶段三：战斗 (Combat Rules)
        // ==========================================
        Debug.Log("⚔️ 敌军发动冲锋！");

        // 1. 扫描玩家是否有嘲讽怪
        CardDisplay tauntTarget = null;
        bool isPlayerFrontlineEmpty = true;
        foreach (Transform pSlot in playerFrontline)
        {
            if (pSlot.childCount > 0)
            {
                isPlayerFrontlineEmpty = false; // 发现掩体
                CardDisplay pCard = pSlot.GetChild(0).GetComponent<CardDisplay>();
                if (pCard != null && pCard.cardData.keyword == Keyword.Taunt)
                {
                    tauntTarget = pCard; // 锁定嘲讽
                }
            }
        }

        // 2. 敌军进攻结算
        for (int i = 0; i < 3; i++)
        {
            Transform enemySlot = enemyFrontline.GetChild(i);
            Transform playerSlot = playerFrontline.GetChild(i);

            if (enemySlot.childCount > 0)
            {
                CardDisplay enemyCard = enemySlot.GetChild(0).GetComponent<CardDisplay>();
                if (enemyCard == null || enemyCard.isSleeping) continue; // 睡觉的兵不打人

                int eAtk = enemyCard.cardData.attack;

                // 嘲讽强制拦截
                if (tauntTarget != null)
                {
                    Debug.Log($"🛡️ 敌军被嘲讽吸引！攻击 {tauntTarget.cardData.cardName}");
                    tauntTarget.TakeDamage(eAtk);
                    int pAtk = tauntTarget.cardData.attack;
                    if (pAtk > 0) enemyCard.TakeDamage(pAtk);
                }
                // 正对面有兵
                else if (playerSlot.childCount > 0)
                {
                    CardDisplay playerCard = playerSlot.GetChild(0).GetComponent<CardDisplay>();
                    Debug.Log($"⚔️ 敌军攻击正前方：{playerCard.cardData.cardName}");
                    playerCard.TakeDamage(eAtk);
                    int pAtk = playerCard.cardData.attack;
                    if (pAtk > 0) enemyCard.TakeDamage(pAtk);
                }
                // 必须满足掩体法则才能打脸
                else if (isPlayerFrontlineEmpty)
                {
                    Debug.Log($"🗡️ 敌军突破防线！攻击玩家主将！");
                    PlayerManager.Instance.TakeDamage(eAtk);
                }
                
                enemyCard.isSleeping = true; // 打完收工
            }
        }

        Debug.Log("✅ 敌方回合结束，玩家回合开始！");
        // 如果这里有交给玩家回合的代码，写在这里
    }

    // 💀 添加破产检测机制
    public void CheckStarvationLoss()
    {
        bool isBankrupt = (currentProvision <= 0);
        bool isBoardEmpty = true;
        for (int i = 0; i < 3; i++)
        {
            if (enemyFrontline.GetChild(i).childCount > 0)
            {
                isBoardEmpty = false; break;
            }
        }
        if (isBankrupt && isBoardEmpty)
        {
            Debug.Log("💀 【粮绝兵败】！敌方破产，你赢了！");
            GameManager.Instance.GameOver(true);
        }
    }
}