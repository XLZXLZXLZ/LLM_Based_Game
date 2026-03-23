using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public enum CardUIState
{
    InHand,
    Hovered,
    Dragging,
    Animating
}

/// <summary>
/// 单张卡牌UI，负责显示和交互。持有 Card（纯逻辑对象）的引用。
/// </summary>
public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI组件")]
    [SerializeField] private Image cardBackground;
    [SerializeField] private Image cardArtwork;
    [SerializeField] private Text nameText;
    [SerializeField] private Text costText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Image costTypeIcon;
    [SerializeField] private Image cardBorder;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("安全状态配置")]
    [SerializeField] private Color safeBorderColor = Color.white;
    [SerializeField] private Color dangerBorderColor = Color.red;

    private Card cardData;
    private BattleContext ctx;

    public Card CardData => cardData;

    private CardUIState state = CardUIState.InHand;
    public CardUIState State => state;

    private int handIndex;
    public int HandIndex
    {
        get => handIndex;
        set => handIndex = value;
    }

    private bool isPlayable = true;
    public bool IsPlayable => isPlayable;

    private bool isSafe = true;
    public bool IsSafe => isSafe;

    private Vector3 homePosition;
    private float homeRotation;
    public Vector3 HomePosition => homePosition;

    // ==================== 动画 ====================

    private Tweener moveTween;
    private Tweener rotateTween;
    private Tweener scaleTween;

    [Header("动画配置")]
    [SerializeField] private float animDuration = 0.2f;
    [SerializeField] private Ease animEase = Ease.OutQuad;

    // ==================== 事件 ====================

    public event Action<CardUI> OnHovered;
    public event Action<CardUI> OnUnhovered;
    public event Action<CardUI> OnDragStarted;
    public event Action<CardUI, Vector3> OnDragReleased;

    // ==================== 拖拽 ====================

    private Canvas parentCanvas;
    private RectTransform rectTransform;
    private Vector2 dragOffset;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    private void OnDestroy()
    {
        KillAllTweens();
    }

    // ==================== 初始化 ====================

    public void Init(Card data, BattleContext ctx)
    {
        cardData = data;
        this.ctx = ctx;
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        if (cardData?.Data == null) return;

        if (nameText != null)
            nameText.text = cardData.Name;

        if (costText != null)
        {
            int displayCost = ctx != null ? ctx.GetEffectiveCost(cardData).value : cardData.CostValue;
            costText.text = displayCost.ToString();
        }

        if (descriptionText != null)
            descriptionText.text = cardData.GetDescription(ctx);
    }

    // ==================== 状态设置 ====================

    public void SetHomePosition(Vector3 position, float rotation)
    {
        homePosition = position;
        homeRotation = rotation;
    }

    public void SetPlayable(bool playable)
    {
        isPlayable = playable;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = playable ? 1f : 0.5f;
        }
    }

    public void SetSafeState(bool safe)
    {
        isSafe = safe;

        if (cardBorder != null)
        {
            cardBorder.color = safe ? safeBorderColor : dangerBorderColor;
        }
    }

    public void SetState(CardUIState newState)
    {
        state = newState;
    }

    public void SetSortingOrder(int order)
    {
        transform.SetSiblingIndex(order);
    }

    // ==================== 动画 ====================

    public void AnimateToPosition(Vector3 position, float rotation, Vector3 scale, float? duration = null, Ease? ease = null)
    {
        float dur = duration ?? animDuration;
        Ease e = ease ?? animEase;

        KillAllTweens();

        moveTween = rectTransform.DOAnchorPos(position, dur).SetEase(e);
        rotateTween = rectTransform.DOLocalRotate(new Vector3(0, 0, rotation), dur).SetEase(e);
        scaleTween = rectTransform.DOScale(scale, dur).SetEase(e);
    }

    public void AnimateToHome(float? duration = null)
    {
        AnimateToPosition(homePosition, homeRotation, Vector3.one, duration);
        SetState(CardUIState.InHand);
    }

    public void SetPositionImmediate(Vector3 position, float rotation, Vector3 scale)
    {
        KillAllTweens();
        rectTransform.anchoredPosition = position;
        rectTransform.localEulerAngles = new Vector3(0, 0, rotation);
        rectTransform.localScale = scale;
    }

    private void KillAllTweens()
    {
        moveTween?.Kill();
        rotateTween?.Kill();
        scaleTween?.Kill();
    }

    // ==================== 交互事件 ====================

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (state == CardUIState.InHand)
        {
            OnHovered?.Invoke(this);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (state == CardUIState.Hovered)
        {
            OnUnhovered?.Invoke(this);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (state == CardUIState.InHand || state == CardUIState.Hovered)
        {
            SetState(CardUIState.Dragging);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                eventData.position,
                parentCanvas.worldCamera,
                out Vector2 localPoint);

            dragOffset = rectTransform.anchoredPosition - localPoint;

            OnDragStarted?.Invoke(this);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (state == CardUIState.Dragging)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                eventData.position,
                parentCanvas.worldCamera,
                out Vector2 localPoint);

            rectTransform.anchoredPosition = localPoint + dragOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (state == CardUIState.Dragging)
        {
            Vector3 releasePosition = rectTransform.anchoredPosition;
            OnDragReleased?.Invoke(this, releasePosition);
        }
    }
}
