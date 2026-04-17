using UnityEngine;
using TMPro;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;

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
        // 1. 发放下一回合薪水 (你的封顶代码)
        int totalUpkeep = 0;
        int totalProduce = 0;

        // 统计一下工资和产出，顺便把活着的士兵全部叫醒！
        for (int i = 0; i < 3; i++)
        {
            Transform mySlot = playerFrontline.GetChild(i);
            if (mySlot.childCount > 0)
            {
                CardDisplay myCard = mySlot.GetChild(0).GetComponent<CardDisplay>();
                if (myCard != null)
                {
                    totalUpkeep += myCard.cardData.upkeep;
                    if (myCard.cardData.keyword == Keyword.Produce) totalProduce += myCard.cardData.produceAmount;

                    // ⏰ 叫醒士兵！它下回合就能手动拖拽打人了！
                    myCard.isSleeping = false;
                }
            }
        }

        int baseIncome = 4;
        currentProvision += (baseIncome + totalProduce - totalUpkeep);
        currentProvision = Mathf.Clamp(currentProvision, 0, 10);
        UpdateUI();

        // 2. 抽牌并交接回合
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
}