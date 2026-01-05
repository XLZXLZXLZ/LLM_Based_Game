using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 卡牌UI状态
/// </summary>
public enum CardUIState
{
    InHand,      // 正常在手牌
    Hovered,     // 被悬停
    Dragging,    // 被拖拽
    Animating    // 动画中（抽牌/返回/打出/弃牌）
}

/// <summary>
/// 单张卡牌UI
/// </summary>
public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // ==================== UI组件引用 ====================
    [Header("UI组件")]
    [SerializeField] private Image cardBackground;
    [SerializeField] private Image cardArtwork;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image costTypeIcon;
    [SerializeField] private Image cardBorder;          // 卡牌边缘，用于安全/危险高亮
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("安全状态配置")]
    [SerializeField] private Color safeBorderColor = Color.white;
    [SerializeField] private Color dangerBorderColor = Color.red;

    // ==================== 数据 ====================
    private Card cardData;
    public Card CardData => cardData;

    // ==================== 状态 ====================
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

    // ==================== 位置信息 ====================
    private Vector3 homePosition;
    private float homeRotation;

    public Vector3 HomePosition => homePosition;

    // ==================== 动画相关 ====================
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
    public event Action<CardUI, Vector3> OnDragReleased; // 拖拽释放（带位置）

    // ==================== 拖拽相关 ====================
    private Canvas parentCanvas;
    private RectTransform rectTransform;
    private Vector2 dragOffset;

    // ==================== 生命周期 ====================

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
        // 销毁时杀死所有动画，防止报错
        KillAllTweens();
    }

    // ==================== 初始化 ====================

    /// <summary>
    /// 用卡牌数据初始化UI
    /// </summary>
    public void Init(Card data)
    {
        cardData = data;
        RefreshDisplay();
    }

    /// <summary>
    /// 刷新显示内容
    /// </summary>
    public void RefreshDisplay()
    {
        if (cardData == null || cardData.info == null) return;

        // 卡牌名称
        if (nameText != null)
        {
            nameText.text = cardData.info.cardName;
        }

        // 费用数字
        if (costText != null)
        {
            costText.text = cardData.info.baseCost.ToString();
        }

        // 卡牌描述（使用 GetCardDescription() 支持动态描述）
        if (descriptionText != null)
        {
            descriptionText.text = cardData.GetCardDescription();
        }
    }

    // ==================== 状态设置 ====================

    /// <summary>
    /// 设置在手牌中的位置信息
    /// </summary>
    public void SetHomePosition(Vector3 position, float rotation)
    {
        homePosition = position;
        homeRotation = rotation;
    }

    /// <summary>
    /// 设置是否可打出（不可打出时变暗）
    /// </summary>
    public void SetPlayable(bool playable)
    {
        isPlayable = playable;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = playable ? 1f : 0.5f;
        }
    }

    /// <summary>
    /// 设置安全状态（危险时边缘变色，提示打出后会导致天平倾倒）
    /// </summary>
    public void SetSafeState(bool safe)
    {
        isSafe = safe;

        if (cardBorder != null)
        {
            cardBorder.color = safe ? safeBorderColor : dangerBorderColor;
        }
    }

    /// <summary>
    /// 设置卡牌状态
    /// </summary>
    public void SetState(CardUIState newState)
    {
        state = newState;
    }

    /// <summary>
    /// 设置层级
    /// </summary>
    public void SetSortingOrder(int order)
    {
        transform.SetSiblingIndex(order);
    }

    // ==================== 动画方法 ====================

    /// <summary>
    /// 动画移动到指定位置
    /// </summary>
    public void AnimateToPosition(Vector3 position, float rotation, Vector3 scale, float? duration = null, Ease? ease = null)
    {
        float dur = duration ?? animDuration;
        Ease e = ease ?? animEase;

        KillAllTweens();

        moveTween = rectTransform.DOAnchorPos(position, dur).SetEase(e);
        rotateTween = rectTransform.DOLocalRotate(new Vector3(0, 0, rotation), dur).SetEase(e);
        scaleTween = rectTransform.DOScale(scale, dur).SetEase(e);
    }

    /// <summary>
    /// 动画回到手牌位置
    /// </summary>
    public void AnimateToHome(float? duration = null)
    {
        AnimateToPosition(homePosition, homeRotation, Vector3.one, duration);
        SetState(CardUIState.InHand);
    }

    /// <summary>
    /// 立即设置位置（无动画）
    /// </summary>
    public void SetPositionImmediate(Vector3 position, float rotation, Vector3 scale)
    {
        KillAllTweens();
        rectTransform.anchoredPosition = position;
        rectTransform.localEulerAngles = new Vector3(0, 0, rotation);
        rectTransform.localScale = scale;
    }

    /// <summary>
    /// 停止所有动画
    /// </summary>
    private void KillAllTweens()
    {
        moveTween?.Kill();
        rotateTween?.Kill();
        scaleTween?.Kill();
    }

    // ==================== 交互事件处理 ====================

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

            // 计算拖拽偏移，使卡牌不会跳到鼠标位置
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

