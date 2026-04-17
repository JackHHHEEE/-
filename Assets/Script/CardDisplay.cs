using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems; // 👈 新增这个，用来检测点击！

public class CardDisplay : MonoBehaviour, IPointerClickHandler
{
    [Header("绑定的卡牌数据")]
    public CardData cardData;

    public int currentHP;
    public bool isNewlyPlayed = true; // 刚出场时，默认是“新下的牌”

    [Header("UI 文本组件")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText; // 👈 绑定左上角的粮草数字
    public TextMeshProUGUI atkText;
    public TextMeshProUGUI hpText;

    public GameObject cardBack; // 👈 卡背的“盖布”

    [Header("战斗状态")]
    public bool isSleeping = true; // 刚下场默认在睡觉 Zzz...

    void Start()
    {
        if (cardData != null)
        {
            SetupCard();
        }
    }

    public void SetupCard()
    {
        currentHP = cardData.health;
        nameText.text = cardData.cardName;
        transform.localRotation = Quaternion.Euler(0, 0, 0);
        transform.localScale = Vector3.one;

        if (costText != null)
        {
            costText.text = cardData.cost.ToString(); // 把数据里的费写到牌面上
        }

        if (cardData.type == CardType.Tactic)
        {
            atkText.text = "";
            hpText.text = "";
        }
        else
        {
            // 使用 \n 实现强制换行，上面是字母，下面是数字
            atkText.text = "ATK\n" + cardData.attack.ToString();
            hpText.text = "HP\n" + cardData.health.ToString();

        }
    }
    // 真正的肉搏挨揍逻辑！
    // 真正的肉搏挨揍逻辑！
    // 🩸 挨揍后更新牌面血量显示的魔法 (全村唯一的 TakeDamage)
    // 🩸 挨揍后更新牌面血量显示的魔法
    public void TakeDamage(int damage)
    {
        currentHP -= damage;

        // 刷新屏幕上的数字
        if (hpText != null)
        {
            hpText.text = "HP\n" + currentHP.ToString();
        }

        // 👇 新增这 4 行代码：一旦血量归零或变成负数，当场灰飞烟灭！
        if (currentHP <= 0)
        {
            Debug.Log($"☠️ {cardData.cardName} 顶不住了，阵亡！");
            Destroy(gameObject); // 物理消灭自己，把坑位腾出来
        }
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"👁️ 玩家点击了卡牌：{cardData.cardName}，显示详情！");
        GameManager.Instance.ShowCardDetails(cardData);
    }

    // 🃏 控制卡牌翻面的魔法
    // 🃏 控制卡牌翻面的魔法函数
    public void SetFaceUp(bool isFaceUp)
    {
        if (cardBack != null)
        {
            // 如果要求朝上(true)，卡背就隐藏(false)；要求盖住(false)，卡背就显示(true)
            cardBack.SetActive(!isFaceUp);
        }
        else
        {
            Debug.LogWarning("⚠️ 警告：卡牌没有绑定 CardBack 卡背对象！");
        }
    }
}