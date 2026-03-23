using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 手牌UI管理器。
/// 通过订阅 DeckState 事件自动响应手牌变化，不直接参与战斗逻辑。
/// </summary>
public class HandUI : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private Transform cardContainer;
    [SerializeField] private CardUI angerCardPrefab;
    [SerializeField] private CardUI calmCardPrefab;
    [SerializeField] private CardUI neutralCardPrefab;
    [SerializeField] private Transform drawPilePosition;
    [SerializeField] private Transform discardPilePosition;

    [Header("扇形布局配置")]
    [SerializeField] private float fanAngle = 30f;
    [SerializeField] private float fanRadius = 1200f;
    [SerializeField] private float cardSpacing = 100f;
    [SerializeField] private float maxCardSpacing = 150f;
    [SerializeField] private Vector2 handCenterOffset;

    [Header("悬停配置")]
    [SerializeField] private float hoverHeight = 80f;
    [SerializeField] private float hoverScale = 1.3f;

    [Header("打出配置")]
    [SerializeField] private float playThresholdY = 100f;

    [Header("动画配置")]
    [SerializeField] private float drawAnimDuration = 0.3f;
    [SerializeField] private float discardAnimDuration = 0.2f;

    private BattleContext ctx;
    private List<CardUI> cardUIs = new();
    private Dictionary<Card, CardUI> cardToUI = new();
    private CardUI hoveredCard;
    private CardUI draggingCard;
    private Card playingCard;
    private bool isInteractable = true;

    public List<CardUI> CardUIs => cardUIs;

    public bool IsInteractable
    {
        get => isInteractable;
        set => isInteractable = value;
    }

    // ==================== 初始化 ====================

    public void Initialize(BattleContext ctx)
    {
        this.ctx = ctx;
        ctx.Deck.OnCardAddedToHand += OnCardEnteredHand;
        ctx.Deck.OnCardRemovedFromHand += OnCardLeftHand;
    }

    private void OnDestroy()
    {
        if (ctx != null)
        {
            ctx.Deck.OnCardAddedToHand -= OnCardEnteredHand;
            ctx.Deck.OnCardRemovedFromHand -= OnCardLeftHand;
        }
    }

    // ==================== DeckState 事件响应 ====================

    private void OnCardEnteredHand(Card card)
    {
        CardUI prefab = ResolveCardPrefab(card);
        if (prefab == null)
        {
            Debug.LogError("[HandUI] 未配置可用的卡牌UI Prefab。");
            return;
        }

        CardUI cardUI = Instantiate(prefab, cardContainer);
        cardUI.Init(card, ctx);
        BindCardEvents(cardUI);

        cardUIs.Add(cardUI);
        cardToUI[card] = cardUI;
        cardUI.HandIndex = cardUIs.Count - 1;

        RecalculateLayout();
        StartCoroutine(AnimateDrawCard(cardUI));
    }

    private CardUI ResolveCardPrefab(Card card)
    {
        if (card == null) return null;
        var (costType, costValue) = ctx != null ? ctx.GetEffectiveCost(card) : (card.CostType, card.CostValue);

        if (costValue <= 0)
            return neutralCardPrefab != null ? neutralCardPrefab : calmCardPrefab ?? angerCardPrefab;

        if (costType == CostType.Anger)
            return angerCardPrefab != null ? angerCardPrefab : neutralCardPrefab ?? calmCardPrefab;

        return calmCardPrefab != null ? calmCardPrefab : neutralCardPrefab ?? angerCardPrefab;
    }

    private void OnCardLeftHand(Card card)
    {
        if (!cardToUI.TryGetValue(card, out var cardUI)) return;

        UnbindCardEvents(cardUI);
        cardUIs.Remove(cardUI);
        cardToUI.Remove(card);

        if (hoveredCard == cardUI) hoveredCard = null;
        if (draggingCard == cardUI) draggingCard = null;

        for (int i = 0; i < cardUIs.Count; i++)
            cardUIs[i].HandIndex = i;

        RecalculateLayout();

        bool wasPlayed = (playingCard == card);
        if (wasPlayed)
            StartCoroutine(AnimatePlayCard(cardUI));
        else
            StartCoroutine(AnimateDiscardCard(cardUI));
    }

    // ==================== 公共方法 ====================

    /// <summary>
    /// 强制清空所有卡牌UI（不触发动画）
    /// </summary>
    public void Clear()
    {
        foreach (var cardUI in cardUIs)
        {
            UnbindCardEvents(cardUI);
            Destroy(cardUI.gameObject);
        }
        cardUIs.Clear();
        cardToUI.Clear();
        hoveredCard = null;
        draggingCard = null;
    }

    public void RefreshPlayableStates()
    {
        if (ctx == null) return;
        foreach (var cardUI in cardUIs)
        {
            if (cardUI.CardData == null) continue;
            cardUI.SetPlayable(cardUI.CardData.CanPlay(ctx));
            cardUI.SetSafeState(cardUI.CardData.IsSafe(ctx));
            cardUI.RefreshDisplay(); // 同步有效费用等（减费 buff 会体现在这里）
        }
    }

    // ==================== 布局计算 ====================

    public void RecalculateLayout()
    {
        int count = cardUIs.Count;
        if (count == 0) return;

        for (int i = 0; i < count; i++)
        {
            var (position, rotation) = CalculateCardPosition(i, count);
            cardUIs[i].SetHomePosition(position, rotation);
            cardUIs[i].SetSortingOrder(i);

            if (cardUIs[i] != draggingCard && cardUIs[i] != hoveredCard)
            {
                cardUIs[i].AnimateToHome();
            }
        }
    }

    private (Vector3 position, float rotation) CalculateCardPosition(int index, int totalCount)
    {
        if (totalCount == 1)
            return (handCenterOffset, 0f);

        float actualSpacing = Mathf.Min(cardSpacing, maxCardSpacing);
        float totalWidth = actualSpacing * (totalCount - 1);
        float actualAngle = Mathf.Min(fanAngle, fanAngle * (totalCount - 1) / 5f);

        float t = (float)index / (totalCount - 1) - 0.5f;

        float x = t * totalWidth + handCenterOffset.x;

        float normalizedT = t * 2;
        float y = -Mathf.Abs(normalizedT) * fanRadius * 0.02f + handCenterOffset.y;

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
        if (!isInteractable || draggingCard != null) return;

        hoveredCard = cardUI;
        cardUI.SetState(CardUIState.Hovered);

        Vector3 hoverPos = cardUI.HomePosition + new Vector3(0, hoverHeight, 0);
        cardUI.AnimateToPosition(hoverPos, 0, Vector3.one * hoverScale);
        cardUI.SetSortingOrder(cardUIs.Count + 10);
    }

    private void HandleCardUnhovered(CardUI cardUI)
    {
        if (cardUI != hoveredCard) return;

        hoveredCard = null;
        cardUI.SetState(CardUIState.InHand);
        cardUI.AnimateToHome();
        cardUI.SetSortingOrder(cardUI.HandIndex);
    }

    private void HandleCardDragStarted(CardUI cardUI)
    {
        if (!isInteractable) return;

        draggingCard = cardUI;
        hoveredCard = null;
        cardUI.SetSortingOrder(cardUIs.Count + 20);
    }

    private void HandleCardDragReleased(CardUI cardUI, Vector3 releasePosition)
    {
        draggingCard = null;

        if (releasePosition.y > playThresholdY && ctx != null)
        {
            TryPlayCard(cardUI);
        }
        else
        {
            ReturnToHand(cardUI);
        }
    }

    private void TryPlayCard(CardUI cardUI)
    {
        if (cardUI.CardData == null || !cardUI.CardData.CanPlay(ctx))
        {
            ReturnToHand(cardUI);
            return;
        }

        playingCard = cardUI.CardData;
        bool success = ctx.PlayCard(cardUI.CardData);
        playingCard = null;

        if (!success)
        {
            ReturnToHand(cardUI);
        }
        else
        {
            RefreshPlayableStates();
        }
    }

    private void ReturnToHand(CardUI cardUI)
    {
        cardUI.SetState(CardUIState.Animating);
        cardUI.AnimateToHome();
        cardUI.SetSortingOrder(cardUI.HandIndex);
    }

    // ==================== 动画 ====================

    private IEnumerator AnimateDrawCard(CardUI cardUI)
    {
        cardUI.SetState(CardUIState.Animating);

        if (drawPilePosition != null)
        {
            cardUI.SetPositionImmediate(
                drawPilePosition.localPosition,
                0,
                Vector3.one * 0.5f
            );
        }

        cardUI.AnimateToHome();

        yield return new WaitForSeconds(drawAnimDuration);

        cardUI.SetState(CardUIState.InHand);
    }

    private IEnumerator AnimatePlayCard(CardUI cardUI)
    {
        cardUI.SetState(CardUIState.Animating);

        Vector3 targetPos = cardUI.transform.localPosition + new Vector3(0, 200, 0);
        cardUI.AnimateToPosition(targetPos, 0, Vector3.one * 0.8f);

        yield return new WaitForSeconds(discardAnimDuration);

        Destroy(cardUI.gameObject);
    }

    private IEnumerator AnimateDiscardCard(CardUI cardUI)
    {
        cardUI.SetState(CardUIState.Animating);

        Vector3 targetPos = discardPilePosition != null
            ? discardPilePosition.localPosition
            : new Vector3(500, -300, 0);

        cardUI.AnimateToPosition(targetPos, 0, Vector3.one * 0.5f);

        yield return new WaitForSeconds(discardAnimDuration);

        Destroy(cardUI.gameObject);
    }
}
