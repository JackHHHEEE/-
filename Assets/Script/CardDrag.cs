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
                // 📡 提前开启雷达：扫描敌方前线 (寻找 EnemyFrontline)
                Transform enemyFrontline = GameObject.Find("EnemyFrontline")?.transform;
                bool hasTauntOnBoard = false;
                bool isFrontlineEmpty = true;

                if (enemyFrontline != null)
                {
                    foreach (Transform slot in enemyFrontline)
                    {
                        if (slot.childCount > 0)
                        {
                            isFrontlineEmpty = false; // 发现活着的敌军！掩体法则生效！
                            
                            CardDisplay card = slot.GetChild(0).GetComponent<CardDisplay>();
                            if (card != null && card.cardData.keyword == Keyword.Taunt) 
                            {
                                hasTauntOnBoard = true; // 滴滴滴！发现嘲讽怪！
                            }
                        }
                    }
                }

                // ================= 情况 A：你瞄准了对面的某张卡牌 =================
                CardDisplay targetCard = targetObj.GetComponentInParent<CardDisplay>();
                if (targetCard != null && targetCard.transform.parent.parent.name == "EnemyFrontline")
                {
                    // 🛡️ 嘲讽拦截判定
                    if (hasTauntOnBoard && targetCard.cardData.keyword != Keyword.Taunt)
                    {
                        Debug.LogWarning("🛡️ 敌方场上有【坚守/Taunt】随从！你必须优先攻击嘲讽目标！");
                        return; // 攻击被无情取消
                    }

                    Debug.Log($"💥 冲锋！{myDisplay.cardData.cardName} 攻击了 {targetCard.cardData.cardName}！");
                    
                    // 互相伤害
                    targetCard.TakeDamage(myDisplay.cardData.attack);
                    myDisplay.TakeDamage(targetCard.cardData.attack);
                    
                    myDisplay.isSleeping = true; 
                    return;
                }

                // ================= 情况 B：你瞄准了曹操的大头贴（打脸） =================
                if (targetObj.name == "EnemyHeroUI" || (targetObj.transform.parent != null && targetObj.transform.parent.name == "EnemyHeroUI"))
                {
                    // 🛡️ 嘲讽拦截判定 (嘲讽怪也能挡住打脸)
                    if (hasTauntOnBoard)
                    {
                        Debug.LogWarning("🛡️ 敌方场上有【坚守/Taunt】随从！绝对不能直接攻击主公！");
                        return; // 攻击取消
                    }

                    // 🧱 前线掩体法则判定
                    if (!isFrontlineEmpty)
                    {
                        Debug.LogWarning("🧱 前线掩体法则：敌方前线还有士兵，你必须先解决他们才能攻击主公！");
                        return; // 攻击取消
                    }

                    Debug.Log($"🗡️ 直捣黄龙！{myDisplay.cardData.cardName} 狠狠砍了 Boss Cao 一刀！");
                    EnemyManager.Instance.TakeDamage(myDisplay.cardData.attack);
                    
                    myDisplay.isSleeping = true; 
                    return;
                }
            }
            
            Debug.Log("💨 挥空了！请确保红线指准了对面的卡牌或武将！");
        }
        else
        {
            // 这是手牌区的卡牌落座逻辑
            canvasGroup.blocksRaycasts = true;

            // 1. 如果松开鼠标时，它还在最外层飘着，说明没放进坑里，退回手牌！
            if (transform.parent == transform.root)
            {
                transform.SetParent(originalParent);
            }
            // 2. 否则，如果它成功坐进了前线 (PlayerFrontline) 的坑位里：
            else if (transform.parent != null && transform.parent.parent != null && transform.parent.parent.name == "PlayerFrontline")
            {
                // 💥 检查曹操大招是否处于激活状态？
                if (PlayerManager.Instance != null && PlayerManager.Instance.isCaoCaoBuffActive)
                {
                    Debug.Log($"🔥 曹操大招加持！{myDisplay.cardData.cardName} 获得【突袭】，立刻苏醒！");
                    
                    myDisplay.isSleeping = false; // 打入突袭激素，解除睡眠状态！
                    
                    // 大招消耗完毕，关掉开关，防止下一张牌也免费！
                    PlayerManager.Instance.isCaoCaoBuffActive = false; 
                }
            }
    
        }
    }
}