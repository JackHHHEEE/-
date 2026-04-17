using UnityEngine;

// 定义枚举：卡牌上的关键词
public enum Keyword { None, Rush, Taunt, Produce }
// 定义枚举：卡牌的类型
public enum CardType { Unit, Tactic }

// 这行代码是魔法：它会在Unity的右键菜单里为你增加一个创建卡牌的按钮
[CreateAssetMenu(fileName = "New Card", menuName = "Supply Lines/Create New Card")]

public class CardData : ScriptableObject
{
    [Header("Basic Info (基础信息)")]
    public string cardName;
    public CardType type;
    public int cost;

    [Header("Unit Stats (单位属性 - 战术卡填0)")]
    public int upkeep;
    public int attack;
    public int health;

    [Header("Artwork (卡牌美术)")]
    public Sprite cardArt; // 👈 新增：用来存放卡牌原画图片

    [Header("Special Abilities (特殊能力)")]
    public Keyword keyword;
    public int produceAmount;

    [Header("Description (战术卡描述)")]
    [TextArea]
    public string description;
}