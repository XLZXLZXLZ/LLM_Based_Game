using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 手牌UI管理器
/// </summary>
public class HandUI : MonoBehaviour
{
    // ==================== 配置参数 ====================
    [Header("引用")]
    [SerializeField] private Transform cardContainer;
    [SerializeField] private CardUI cardPrefab;
    [SerializeField] private Transform drawPilePosition;    // 抽牌堆位置（动画起点）
    [SerializeField] private Transform discardPilePosition; // 弃牌堆位置（动画终点）

    [Header("扇形布局配置")]
    [SerializeField] private float fanAngle = 30f;          // 扇形总角度
    [SerializeField] private float fanRadius = 1200f;       // 扇形半径
    [SerializeField] private float cardSpacing = 100f;      // 卡牌间距（卡牌少时用）
    [SerializeField] private float maxCardSpacing = 150f;   // 最大卡牌间距
    [SerializeField] private Vector2 handCenterOffset;      // 手牌中心偏移

    [Header("悬停配置")]
    [SerializeField] private float hoverHeight = 80f;       // 悬停上移距离
    [SerializeField] private float hoverScale = 1.3f;       // 悬停放大倍数

    [Header("打出配置")]
    [SerializeField] private float playThresholdY = 100f;   // 打出阈值线Y坐标

    [Header("动画配置")]
    [SerializeField] private float drawAnimDuration = 0.3f;
    [SerializeField] private float discardAnimDuration = 0.2f;
    [SerializeField] private float rearrangeDelay = 0.05f;  // 重排时每张卡的延迟

    // ==================== 状态 ====================
    private List<CardUI> cards = new();
    private CardUI hoveredCard;
    private CardUI draggingCard;
    private bool isInteractable = true;

    public List<CardUI> Cards => cards;
    public bool IsInteractable
    {
        get => isInteractable;
        set => isInteractable = value;
    }

    // ==================== 事件 ====================
    public event Action<CardUI, object> OnCardPlayRequested; // 请求打出卡牌(卡牌, 目标)

    // ==================== 生命周期 ====================

    private void Start()
    {
        // 监听战斗事件
        // BattleEventManager.Instance.OnCardDrawn.OnAfter(OnCardDrawnEvent);
        // BattleEventManager.Instance.OnTurnStart.OnAfter(OnTurnStartEvent);
    }

    private void OnDestroy()
    {
        // 取消监听
        // BattleEventManager.Instance?.OnCardDrawn.Unregister(OnCardDrawnEvent);
        // BattleEventManager.Instance?.OnTurnStart.Unregister(OnTurnStartEvent);
    }

    // ==================== 卡牌管理 ====================

    /// <summary>
    /// 添加卡牌到手牌（带抽牌动画）
    /// </summary>
    public CardUI AddCard(Card cardData, bool animate = true)
    {
        // 创建卡牌UI
        CardUI cardUI = Instantiate(cardPrefab, cardContainer);
        cardUI.Init(cardData);

        // 绑定事件
        BindCardEvents(cardUI);

        // 添加到列表
        cards.Add(cardUI);
        cardUI.HandIndex = cards.Count - 1;

        // 重新计算布局
        RecalculateLayout();

        // 播放抽牌动画
        if (animate)
        {
            StartCoroutine(AnimateDrawCard(cardUI));
        }
        else
        {
            cardUI.AnimateToHome();
        }

        return cardUI;
    }

    /// <summary>
    /// 从手牌移除卡牌
    /// </summary>
    public void RemoveCard(CardUI cardUI, bool animate = true)
    {
        if (!cards.Contains(cardUI)) return;

        // 解绑事件
        UnbindCardEvents(cardUI);

        // 从列表移除
        cards.Remove(cardUI);

        // 更新索引
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].HandIndex = i;
        }

        // 重新计算布局
        RecalculateLayout();

        // 播放弃牌动画或直接销毁
        if (animate)
        {
            StartCoroutine(AnimateDiscardCard(cardUI));
        }
        else
        {
            Destroy(cardUI.gameObject);
        }
    }

    /// <summary>
    /// 清空所有手牌
    /// </summary>
    public void Clear()
    {
        foreach (var card in cards)
        {
            UnbindCardEvents(card);
            Destroy(card.gameObject);
        }
        cards.Clear();
        hoveredCard = null;
        draggingCard = null;
    }

    /// <summary>
    /// 刷新所有卡牌的可打出状态和安全状态
    /// </summary>
    public void RefreshPlayableStates()
    {
        foreach (var cardUI in cards)
        {
            if (cardUI.CardData == null) continue;

            bool canPlay = cardUI.CardData.CanPlay();
            bool isSafe = cardUI.CardData.IsSafe();

            cardUI.SetPlayable(canPlay);
            cardUI.SetSafeState(isSafe); // 危险卡牌边缘高亮不同
        }
    }

    // ==================== 布局计算 ====================

    /// <summary>
    /// 重新计算所有卡牌位置
    /// </summary>
    public void RecalculateLayout()
    {
        int count = cards.Count;
        if (count == 0) return;

        for (int i = 0; i < count; i++)
        {
            var (position, rotation) = CalculateCardPosition(i, count);
            cards[i].SetHomePosition(position, rotation);
            cards[i].SetSortingOrder(i);

            // 如果不是正在拖拽或悬停的卡，更新位置
            if (cards[i] != draggingCard && cards[i] != hoveredCard)
            {
                cards[i].AnimateToHome();
            }
        }
    }

    /// <summary>
    /// 计算单张卡牌的位置和旋转
    /// </summary>
    private (Vector3 position, float rotation) CalculateCardPosition(int index, int totalCount)
    {
        if (totalCount == 1)
        {
            // 单张卡牌居中
            return (handCenterOffset, 0f);
        }

        // 计算实际使用的间距和角度
        float actualSpacing = Mathf.Min(cardSpacing, maxCardSpacing);
        float totalWidth = actualSpacing * (totalCount - 1);
        float actualAngle = Mathf.Min(fanAngle, fanAngle * (totalCount - 1) / 5f);

        // 计算这张卡的位置比例 (-0.5 到 0.5)
        float t = (float)index / (totalCount - 1) - 0.5f;

        // X位置：线性分布
        float x = t * totalWidth + handCenterOffset.x;

        // Y位置：扇形弧度（中间高，两边低）
        float normalizedT = t * 2; // -1 到 1
        float y = -Mathf.Abs(normalizedT) * fanRadius * 0.02f + handCenterOffset.y;

        // 旋转：中间正，两边倾斜
        float rotation = -t * actualAngle;

        return (new Vector3(x, y, 0), rotation);
    }

    // ==================== 事件绑定 ====================

    private void BindCardEvents(CardUI cardUI)
    {
        cardUI.OnHovered += HandleCardHovered;
        cardUI.OnUnhovered += HandleCardUnhovered;
        cardUI.OnDragStarted += HandleCardDragStarted;
        cardUI.OnDragReleased += HandleCardDragReleased;
    }

    private void UnbindCardEvents(CardUI cardUI)
    {
        cardUI.OnHovered -= HandleCardHovered;
        cardUI.OnUnhovered -= HandleCardUnhovered;
        cardUI.OnDragStarted -= HandleCardDragStarted;
        cardUI.OnDragReleased -= HandleCardDragReleased;
    }

    // ==================== 交互处理 ====================

    private void HandleCardHovered(CardUI cardUI)
    {
        if (!isInteractable) return;
        if (draggingCard != null) return; // 拖拽时不响应悬停

        hoveredCard = cardUI;
        cardUI.SetState(CardUIState.Hovered);

        // 计算悬停位置（上移、放大、旋转归零）
        Vector3 hoverPos = cardUI.HomePosition + new Vector3(0, hoverHeight, 0);
        cardUI.AnimateToPosition(hoverPos, 0, Vector3.one * hoverScale);

        // 置顶
        cardUI.SetSortingOrder(cards.Count + 10);
    }

    private void HandleCardUnhovered(CardUI cardUI)
    {
        if (cardUI != hoveredCard) return;

        hoveredCard = null;
        cardUI.SetState(CardUIState.InHand);
        cardUI.AnimateToHome();

        // 恢复层级
        cardUI.SetSortingOrder(cardUI.HandIndex);
    }

    private void HandleCardDragStarted(CardUI cardUI)
    {
        if (!isInteractable) return;

        draggingCard = cardUI;
        hoveredCard = null;

        // 置顶
        cardUI.SetSortingOrder(cards.Count + 20);

        // 其他卡牌收拢（可选）
        // RecalculateLayoutExcluding(cardUI);
    }

    private void HandleCardDragReleased(CardUI cardUI, Vector3 releasePosition)
    {
        draggingCard = null;

        // 检查是否超过打出阈值
        if (releasePosition.y > playThresholdY)
        {
            // 尝试打出
            TryPlayCard(cardUI, releasePosition);
        }
        else
        {
            // 返回手牌
            cardUI.SetState(CardUIState.Animating);
            cardUI.AnimateToHome();
            cardUI.SetSortingOrder(cardUI.HandIndex);
        }
    }

    // ==================== 打出逻辑 ====================

    /// <summary>
    /// 尝试打出卡牌
    /// </summary>
    private void TryPlayCard(CardUI cardUI, Vector3 releasePosition)
    {
        // 检查是否可打出（由Card.CanPlay()决定）
        if (!cardUI.IsPlayable)
        {
            // 不可打出，返回手牌
            cardUI.SetState(CardUIState.Animating);
            cardUI.AnimateToHome();
            return;
        }

        // 检测目标（如果是需要目标的卡）
        object target = DetectTarget(releasePosition);

        // 检查卡牌是否需要选择目标
        bool needsTarget = cardUI.CardData != null && cardUI.CardData.targetType == CardTargetType.ChooseTarget;

        if (needsTarget && target == null)
        {
            // 需要目标但没有选中，返回手牌
            cardUI.SetState(CardUIState.Animating);
            cardUI.AnimateToHome();
            return;
        }

        // 请求打出卡牌
        cardUI.SetState(CardUIState.Animating);
        OnCardPlayRequested?.Invoke(cardUI, target);
    }

    /// <summary>
    /// 检测释放位置的目标
    /// </summary>
    private object DetectTarget(Vector3 releasePosition)
    {
        // TODO: 射线检测或区域检测，找到鼠标下的敌人
        // 可以用 Physics2D.Raycast 或 EventSystem.RaycastAll

        return null;
    }

    /// <summary>
    /// 检查卡牌是否可打出
    /// </summary>
    private bool CheckCanPlayCard(CardUI cardUI)
    {
        if (cardUI.CardData == null) return false;
        return cardUI.CardData.CanPlay();
    }

    // ==================== 动画协程 ====================

    private IEnumerator AnimateDrawCard(CardUI cardUI)
    {
        cardUI.SetState(CardUIState.Animating);

        // 设置起始位置（抽牌堆）
        if (drawPilePosition != null)
        {
            cardUI.SetPositionImmediate(
                drawPilePosition.localPosition,
                0,
                Vector3.one * 0.5f
            );
        }

        // 动画到手牌位置
        cardUI.AnimateToHome();

        yield return new WaitForSeconds(drawAnimDuration);

        cardUI.SetState(CardUIState.InHand);
    }

    private IEnumerator AnimateDiscardCard(CardUI cardUI)
    {
        cardUI.SetState(CardUIState.Animating);

        // 动画到弃牌堆
        Vector3 targetPos = discardPilePosition != null
            ? discardPilePosition.localPosition
            : new Vector3(500, -300, 0);

        cardUI.AnimateToPosition(targetPos, 0, Vector3.one * 0.5f);

        yield return new WaitForSeconds(discardAnimDuration);

        Destroy(cardUI.gameObject);
    }

    // ==================== 战斗事件响应 ====================

    private void OnCardDrawnEvent(CardDrawnEventArgs args)
    {
        // AddCard(args.Card);
    }

    private void OnTurnStartEvent(TurnEventArgs args)
    {
        RefreshPlayableStates();
        isInteractable = true;
    }
}

