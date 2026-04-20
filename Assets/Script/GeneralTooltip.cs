using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class GeneralTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("绑定 UI 面板")]
    public GameObject tooltipPanel;      
    public TextMeshProUGUI tooltipText;  

    void Start()
    {
        // 游戏刚开始时，强行隐藏提示框
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    // 🖱️ 鼠标移入时触发！
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipPanel == null || tooltipText == null || PlayerManager.Instance == null) return;

        // 获取当前选的是哪个主公 (判断条件依然保持中文，只是屏幕上显示英文)
        string generalName = PlayerManager.Instance.myGeneral;

        if (generalName == "曹操")
        {
            tooltipText.text = "<b><color=#FFD700>Cao Cao (Wei) - \"Counter Attack\"</color></b>\n\n<b>When to use:</b> If you have 0 units on your board.\n\n<b>The Effect:</b> Play 1 unit card from your hand for free (pay 0 Supplies). This unit gets [Rush]. It can attack immediately.";
        }
        else if (generalName == "刘备")
        {
            tooltipText.text = "<b><color=#FFD700>Liu Bei (Shu) - \"Iron Shield\"</color></b>\n\n<b>When to use:</b> If your General has 10 HP or less.\n\n<b>The Effect:</b> Choose 1 of your units on the board. Heal it to full HP. This unit gets +2 Max HP and gains [Taunt] forever.";
        }
        else
        {
            tooltipText.text = "Unknown General Skill.";
        }

        // 填好文字后，把面板显示出来！
        tooltipPanel.SetActive(true);
    }

    // 🖱️ 鼠标移走时触发！
    public void OnPointerExit(PointerEventData eventData)
    {
        // 鼠标离开，马上隐藏面板
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }
}