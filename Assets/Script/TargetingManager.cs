using UnityEngine;

public class TargetingManager : MonoBehaviour
{
    public static TargetingManager Instance;

    private LineRenderer lineRenderer;
    public bool isTargeting = false; // 当前是否正在瞄准中？

    [HideInInspector]
    public CardDisplay attackerCard; // 记住是谁在发起攻击

    void Awake()
    {
        Instance = this;
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false; // 一开始隐藏红线
    }

    void Update()
    {
        // 如果正在瞄准，让红线的终点死死跟着鼠标！
        if (isTargeting && attackerCard != null)
        {
            DrawTargetingLine();
        }
    }

    // 🎯 开启瞄准模式（卡牌脚本等下会呼叫这个）
    public void StartTargeting(CardDisplay attacker)
    {
        // 只有没睡觉的兵才能发起攻击！
        if (attacker.isSleeping)
        {
            Debug.Log("Zzz... 刚下场的兵不能立刻攻击！");
            return;
        }

        isTargeting = true;
        attackerCard = attacker;
        lineRenderer.enabled = true; // 显示红线
    }

    // 🛑 结束瞄准模式（松开鼠标时呼叫）
    public void StopTargeting()
    {
        isTargeting = false;
        attackerCard = null;
        lineRenderer.enabled = false; // 隐藏红线
    }

    // ✏️ 画线逻辑：起点是卡牌，终点是鼠标
    // ✏️ 画线逻辑：完美适配 Screen Space - Camera
    // ✏️ 画线逻辑：强行把线拉到你的脸上！
    void DrawTargetingLine()
    {
        Vector3 startPoint = attackerCard.transform.position;
        startPoint.z -= 5f; // 强行让线的起点往摄像机方向拉近 5 米，摆脱 UI 遮挡！

        Vector3 mousePos = Input.mousePosition;
        // 在 Camera 模式下，需要给鼠标一个距离摄像机的深度。
        // 一般 Canvas 默认是 100，我们写 90 让鼠标飘在 Canvas 上方
        mousePos.z = 90f;
        Vector3 endPoint = Camera.main.ScreenToWorldPoint(mousePos);
        endPoint.z = startPoint.z; // 保持整根线在一个平面上

        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);
    }
}