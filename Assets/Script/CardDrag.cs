using UnityEngine;
using UnityEngine.EventSystems;

// 继承三个接口：开始拖拽、拖拽中、结束拖拽
public class CardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Transform originalParent;
    private CanvasGroup canvasGroup;

    private bool isOnBoard = false; // 🎯 新增开关：记录这张牌是不是已经下场了
    private CardDisplay myDisplay;  // 🎯 新增引用：获取卡牌的数据和睡觉状态

    void Awake()
    {
        // 自动给卡牌加上 CanvasGroup 组件（用来控制鼠标穿透）
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // 抓取身上的卡牌信息脚本
        myDisplay = GetComponent<CardDisplay>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 🔍 判断分流：这张牌现在坐在哪里？
        // 假设卡牌坐在 Slot 里，Slot 的父亲就是 PlayerFrontline
        if (transform.parent != null && transform.parent.parent != null &&
            transform.parent.parent.name == "PlayerFrontline")
        {
            // ⚔️ 模式 A：它已经在战场上了！开启瞄准模式！
            isOnBoard = true;

            // 如果在睡觉，不准拉线！
            if (myDisplay != null && myDisplay.isSleeping)
            {
                Debug.Log("Zzz... 刚下场的兵不能立刻攻击！");
                return;
            }

            // 🎯 呼叫导演：给我拉红线！
            if (TargetingManager.Instance != null)
            {
                TargetingManager.Instance.StartTargeting(myDisplay);
            }
        }
        else
        {
            // 🃏 模式 B：它还在手里！执行你以前的发牌拖拽逻辑
            isOnBoard = false;

            // 1. 记下它本来在哪个书架里（PlayerHand）
            originalParent = transform.parent;

            // 2. 把卡牌强行提到最顶层（不然拖拽时会被其他UI挡住）
            transform.SetParent(transform.root);
            transform.SetAsLastSibling();

            // 3. 极其关键：拖拽时让鼠标“穿透”这张牌
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isOnBoard)
        {
            // ⚔️ 在场上，卡牌不动，只拉线
            return;
        }
        else
        {
            // 🃏 【终极修复隐形 BUG】把屏幕像素坐标，完美转换成 Camera 下的 3D 世界坐标！
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                (RectTransform)transform.parent,
                eventData.position,
                eventData.pressEventCamera,
                out Vector3 globalMousePos);

            transform.position = globalMousePos;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isOnBoard)
        {
            if (myDisplay != null && myDisplay.isSleeping) return; // 睡觉的兵直接忽略

            // 🛑 1. 断开红线
            if (TargetingManager.Instance != null)
            {
                TargetingManager.Instance.StopTargeting();
            }

            // 🎯 2. 开火判定：鼠标到底指着谁？
            GameObject targetObj = eventData.pointerCurrentRaycast.gameObject;

            if (targetObj != null)
            {
                // 情况 A：打到对面的卡牌了！
                CardDisplay targetCard = targetObj.GetComponentInParent<CardDisplay>();
                if (targetCard != null && targetCard.transform.parent.parent.name == "EnemyFrontline")
                {
                    Debug.Log($"💥 冲锋！{myDisplay.cardData.cardName} 攻击了 {targetCard.cardData.cardName}！");

                    // 互相伤害 (炉石规则：打人自己也会掉血)
                    targetCard.TakeDamage(myDisplay.cardData.attack);
                    myDisplay.TakeDamage(targetCard.cardData.attack);

                    // 攻击完毕，陷入沉睡，这回合不能再动了！
                    myDisplay.isSleeping = true;
                    return;
                }

                // 情况 B：打到曹操的大头贴了！
                if (targetObj.name == "EnemyHeroUI" || (targetObj.transform.parent != null && targetObj.transform.parent.name == "EnemyHeroUI"))
                {
                    Debug.Log($"🗡️ 直捣黄龙！{myDisplay.cardData.cardName} 狠狠砍了 Boss Cao 一刀！");
                    EnemyManager.Instance.TakeDamage(myDisplay.cardData.attack);

                    myDisplay.isSleeping = true; // 攻击完毕
                    return;
                }
            }

            Debug.Log("💨 挥空了！请确保红线指准了对面的卡牌或武将！");
        }
        else
        {
            // 这是手牌区的卡牌落座逻辑，保持原样...
            canvasGroup.blocksRaycasts = true;
            if (transform.parent == transform.root)
            {
                transform.SetParent(originalParent);
            }
        }
    }
}