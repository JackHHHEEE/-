using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; 


public class GameManager : MonoBehaviour
{
    public static GameManager Instance; 

    [Header("发牌设置")]
    public GameObject cardPrefab;
    public Transform playerHandArea;
    public List<CardData> playerDeck;
    [Header("终局结算 UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI resultText;

    [Header("卡牌详情 UI")]
    public GameObject cardDetailPanel;
    public UnityEngine.UI.Image detailArtwork;
    public TextMeshProUGUI detailNameText;
    public TextMeshProUGUI detailDescriptionText;



    void Awake()
    {
        Instance = this; // 游戏一开始，发牌员就在热线旁就绪
    }
    void Start()
    {
        // 1. 游戏刚开始，先把玩家的牌库洗乱！
        ShuffleDeck(playerDeck);

        // 2. 顺便把敌人的牌库也洗了！
        if (EnemyManager.Instance != null && EnemyManager.Instance.enemyDeck != null)
        {
            ShuffleDeck(EnemyManager.Instance.enemyDeck);
        }

        // 3. 洗完牌再抽初始手牌
        DrawCards(4);
    }

    // 专业的洗牌算法
    public void ShuffleDeck(System.Collections.Generic.List<CardData> deck)
    {
        for (int i = 0; i < deck.Count; i++)
        {
            CardData temp = deck[i];
            int randomIndex = Random.Range(i, deck.Count);
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
        Debug.Log(" 牌库洗牌完毕！");
    }
    // 真正的抽牌机器
    public void DrawCards(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            // 防错：如果牌库被抽空了，就不抽了
            if (playerDeck.Count == 0)
            {
                Debug.Log("牌库没牌了！");
                return;
            }

            // 1. 从牌库最上面拿出一张牌
            CardData drawnCardData = playerDeck[0];
            playerDeck.RemoveAt(0); // 把这张牌从牌库里删掉（真正的抽走）

            // 2. 捏出肉体，放到手里
            GameObject newCard = Instantiate(cardPrefab, playerHandArea);
            CardDisplay displayScript = newCard.GetComponent<CardDisplay>();

            // 3. 注入灵魂
            displayScript.cardData = drawnCardData;
            displayScript.SetupCard();
        }
    }
    // 🏆 呼出结算画面！
    public void GameOver(bool isVictory)
    {
        gameOverPanel.SetActive(true); // 让黑板弹出来！

        if (isVictory)
        {
            resultText.text = "VICTORY";
            resultText.color = Color.yellow;
        }
        else
        {
            resultText.text = "DEFEAT";
            resultText.color = Color.red;
        }
    }

    // 🔄 重新开始游戏（绑定给按钮用）
    public void RestartGame()
    {
        // 直接重新加载当前场景，一切清零重来！
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    // 🔍 呼出卡牌详情！
    public void ShowCardDetails(CardData data)
    {
        Debug.Log("[大管家] 收到呼叫！准备展示卡牌：" + data.cardName);

        if (cardDetailPanel == null)
        {
            Debug.LogError("[致命错误] 大管家手里的 Card Detail Panel 遥控器是空的！");
            return;
        }

        Debug.Log($" [大管家] 找到面板实体！它的名字叫：{cardDetailPanel.name}。现在执行 SetActive(true)！");
        cardDetailPanel.SetActive(true);

        if (detailArtwork != null) detailArtwork.sprite = data.cardArt;
        if (detailNameText != null) detailNameText.text = data.cardName;

        string fullDesc = $"Cost: {data.cost} | Upkeep: {data.upkeep}\n";
        if (data.type == CardType.Unit) fullDesc += $"ATK: {data.attack} | HP: {data.health}\n";
        if (data.keyword != Keyword.None) fullDesc += $"\n[ Keyword: {data.keyword} ]\n";
        fullDesc += $"\n<i>{data.description}</i>";

        if (detailDescriptionText != null) detailDescriptionText.text = fullDesc;

        Debug.Log("[大管家] 面板激活代码执行完毕！");
    }
    // ❌ 关闭详情面板
   
    public void CloseCardDetails()
    {
        Debug.Log("🖱玩家点击了关闭按钮！尝试关门！"); 
        if (cardDetailPanel != null)
        {
            cardDetailPanel.SetActive(false);
        }
    }
}