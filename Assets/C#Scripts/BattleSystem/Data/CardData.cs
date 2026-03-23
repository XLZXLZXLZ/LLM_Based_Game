using UnityEngine;

public enum CostType
{
    Anger,
    Calm
}

public enum CardQuality
{
    Common,
    Rare,
    Epic,
    Legendary
}

/// <summary>
/// 卡牌类型ID（底层值用于稳定索引）。
/// 0 预留为 Lua 特殊卡。
/// </summary>
public enum CardType
{
    Lua_Card = 0,
    Speech = 1,
    Introspection = 2,
    Monologue = 3,
    Contemplation = 4,
    Questioning = 5,
    Simple = 6,
    Devotion = 7,
    Test = 99
}

[CreateAssetMenu(fileName = "NewCard", menuName = "Battle/CardData")]
public class CardData : ScriptableObject
{
    [Header("绑定")]
    [Tooltip("用于 CardFactory 路由到具体卡牌类的类型索引。0 预留给 Lua_Card。")]
    public CardType cardType = CardType.Lua_Card;

    public string cardName;
    [TextArea(2, 4)]
    public string descriptionTemplate;
    public Sprite artwork;
    public CostType costType;
    public int baseCost;
    public CardQuality quality;
}
