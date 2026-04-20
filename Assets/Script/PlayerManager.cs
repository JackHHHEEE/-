using UnityEngine;
using TMPro;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;

    [Header("主公大招设置")]
    public string myGeneral = "曹操"; // 在面板里填 "曹操" 或 "刘备"
    public bool isSkillUsed = false;  // 记录这局是不是已经翻过面了
    public bool isCaoCaoBuffActive = false; // 记录曹操大招是否处于待命状态

    [Header("玩家当前状态")]
    public int currentHP = 10;
    public int currentProvision = 4;

    [Header("新版武将 UI 绑定")]
    public TextMeshProUGUI hpText;         // 绑红色的血量字
    public TextMeshProUGUI provisionText;  // 绑蓝色的费用字

    [Header("战场关联")]
    public Transform playerFrontline;
    public Transform enemyFrontline; // 👈 新增：主管现在必须要能看到敌人的阵地了！

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        UpdateUI();
    }

    public void UpdateUI()
    {
        // 直接把纯数字写进球里！
        if (hpText != null) hpText.text = currentHP.ToString();
        if (provisionText != null) provisionText.text = currentProvision.ToString();
    }

    public bool TrySpendProvision(int costAmount)
    {
        // 💥 曹操大招拦截：直接免单！
        if (isCaoCaoBuffActive)
        {
            Debug.Log("🔥 触发曹操【魏武挥鞭】加持！本次部署不消耗粮草！");
            return true; // 放行，不扣钱！
        }

        if (currentProvision >= costAmount)
        {
            currentProvision -= costAmount;
            UpdateUI();
            return true;
        }
        return false;
    }

    // 真正的对位交锋结算与疲劳系统！
    public void OnEndTurn()
    {
        int totalProduce = 0;

        // 🌾 1. 先统计场上有多少种田收益 (Produce)
        for (int i = 0; i < 3; i++)
        {
            Transform mySlot = playerFrontline.GetChild(i);
            if (mySlot.childCount > 0)
            {
                CardDisplay myCard = mySlot.GetChild(0).GetComponent<CardDisplay>();
                if (myCard != null && myCard.cardData.keyword == Keyword.Produce)
                {
                    totalProduce += myCard.cardData.produceAmount;
                }
            }
        }

        // 💰 2. 发放基础低保和种田收益 (先发钱，再扣钱)
        int baseIncome = 4;
        currentProvision += (baseIncome + totalProduce);
        currentProvision = Mathf.Clamp(currentProvision, 0, 10); // 仓储上限 10

        Debug.Log($"✅ 回合结算：获得低保 {baseIncome} + 种田 {totalProduce}。当前粮草: {currentProvision}");

        // 💀 3. 挨个发工资与【断粮惩罚】结算！
        for (int i = 0; i < 3; i++)
        {
            Transform mySlot = playerFrontline.GetChild(i);
            if (mySlot.childCount > 0)
            {
                CardDisplay myCard = mySlot.GetChild(0).GetComponent<CardDisplay>();
                if (myCard != null)
                {
                    int cost = myCard.cardData.upkeep; // 获取这个兵的工资要求

                    if (currentProvision >= cost)
                    {
                        // 发得起工资！
                        currentProvision -= cost;
                        myCard.isSleeping = false; // 吃饱喝足，准备下回合战斗！
                        Debug.Log($"🍖 支付了 {myCard.cardData.cardName} 的维持费 {cost} 点粮草。");
                    }
                    else
                    {
                        // 发不起工资了！触发断粮！
                        Debug.LogWarning($"⚠️ 粮草耗尽！{myCard.cardData.cardName} 没饭吃，扣 1 血并进入【疲惫】状态！");
                        
                        myCard.TakeDamage(1); // 饿掉 1 滴血
                        
                        // 极其关键：如果它只剩 1 滴血，上面这句 TakeDamage 已经把它销毁了！
                        // 所以必须判断它是不是还活着，活着才能强制罚站
                        if (myCard != null) 
                        {
                            myCard.isSleeping = true; // 疲惫状态，拉不出红线！
                        }
                    }
                }
            }
        }

        UpdateUI();

        // 4. 抽牌与回合交接
        GameManager.Instance.DrawCards(1);
        EnemyManager.Instance.ExecuteAITurn();
    }

    // 📡 嘲讽雷达：扫描一整排阵地，看看有没有带 [Taunt] 技能的兵
    private CardDisplay FindTauntTarget(Transform frontline)
    {
        foreach (Transform slot in frontline)
        {
            if (slot.childCount > 0)
            {
                CardDisplay card = slot.GetChild(0).GetComponent<CardDisplay>();
                if (card != null && card.cardData.keyword == Keyword.Taunt)
                {
                    return card; // 滴滴滴！发现嘲讽目标！
                }
            }
        }
        return null; // 扫描完毕，没有嘲讽怪
    }
    
    // 🩸 玩家真实挨揍逻辑
    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;

        UpdateUI(); // 刷新屏幕左上角你的血量

        Debug.Log($"💥 遭到敌军痛击！你失去了 {damage} 点血量，剩余血量：{currentHP}");

        if (currentHP == 0)
        {
            Debug.Log("💀 满盘皆输... 玩家战败！");
            GameManager.Instance.GameOver(false); // 👈 呼叫失败画面
        }
    }
    
    // 💀 终极审判：粮绝兵败 (Starvation Loss)
    public void CheckStarvationLoss()
    {
        // 条件 1：国库空虚 (粮草 <= 0)
        bool isBankrupt = (currentProvision <= 0);

        // 条件 2：光杆司令 (前线 3 个坑全空)
        bool isBoardEmpty = true;
        for (int i = 0; i < 3; i++)
        {
            if (playerFrontline.GetChild(i).childCount > 0)
            {
                isBoardEmpty = false;
                break; // 只要发现哪怕一个兵，就不是光杆司令
            }
        }

        // 💥 触发破产判定！
        if (isBankrupt && isBoardEmpty)
        {
            Debug.Log("💀 【粮绝兵败】！国库空虚且无兵可用，你彻底破产了！");
            
            // 呼叫 GameManager 弹出失败画面 (借用你之前挨揍扣血里的逻辑)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver(false); 
            }
        }
    }

    // 👑 发动主公大招！
    public void UseGeneralSkill()
    {
        if (isSkillUsed)
        {
            Debug.LogWarning("⚠️ 大招每局只能用一次！已经翻过面了！");
            return;
        }

        if (myGeneral == "曹操")
        {
            // 魏武挥鞭：己方前线必须为 0 个兵
            bool isEmpty = true;
            for (int i = 0; i < 3; i++)
            {
                if (playerFrontline.GetChild(i).childCount > 0) isEmpty = false;
            }

            if (!isEmpty)
            {
                Debug.LogWarning("⚠️ 【魏武挥鞭】发动失败：你的前线必须空无一人（绝境状态）！");
                return;
            }

            isSkillUsed = true;
            isCaoCaoBuffActive = true;
            Debug.Log("🔥 曹操将卡牌翻面！【魏武挥鞭】发动！你部署的下一张单位卡费用为 0，并获得【突袭(Rush)】！");
        }
        else if (myGeneral == "刘备")
        {
            // 白帝托孤：HP 必须 <= 10
            if (currentHP > 10)
            {
                Debug.LogWarning("⚠️ 【白帝托孤】发动失败：刘备的血量必须降至 10 或以下！");
                return;
            }

            // 找一个幸存的士兵托孤
            CardDisplay targetSoldier = null;
            for (int i = 0; i < 3; i++)
            {
                if (playerFrontline.GetChild(i).childCount > 0)
                {
                    targetSoldier = playerFrontline.GetChild(i).GetChild(0).GetComponent<CardDisplay>();
                    break; // 简单起见，自动选择你场上最左边的第一个兵
                }
            }

            if (targetSoldier == null)
            {
                Debug.LogWarning("⚠️ 【白帝托孤】发动失败：场上没有任何士兵可以托孤！");
                return;
            }

            isSkillUsed = true;
            // 奶满并增加上限 (这里简单粗暴加 10 血模拟)、赋予永久嘲讽
            targetSoldier.currentHP += 10; 
            targetSoldier.cardData.keyword = Keyword.Taunt; 
            
            // 刷新卡牌面板显示
            if (targetSoldier.hpText != null) targetSoldier.hpText.text = "HP\n" + targetSoldier.currentHP;
            
            Debug.Log($"🛡️ 刘备将卡牌翻面！【白帝托孤】发动！{targetSoldier.cardData.cardName} 获得史诗级强化并化身【叹息之墙】！");
        }
    }
}