using UnityEngine;

/// <summary>
/// 卡牌抽象基类（非MonoBehaviour）。
/// 所有对战斗状态的操作通过 BattleContext 完成，卡牌不直接访问任何外部系统。
/// </summary>
public abstract class Card
{
    public CardData Data { get; }

    public virtual string Name => Data != null ? (Data.cardName ?? "") : "";
    public virtual CostType CostType => Data.costType;
    public virtual int CostValue => Data.baseCost;
    public virtual CostType GetCostType(BattleContext ctx) => CostType;
    public virtual int GetCostValue(BattleContext ctx) => CostValue;

    public Card(CardData data)
    {
        Data = data;
    }

    /// <summary>
    /// 是否可以打出（默认始终可打出，子类可重写添加条件）
    /// </summary>
    public virtual bool CanPlay(BattleContext ctx)
    {
        return true;
    }

    /// <summary>
    /// 卡牌效果（子类必须实现）。
    /// 费用由 BattleContext.PlayCard 流水线统一扣除，此方法只负责效果本身。
    /// </summary>
    public abstract void OnPlay(BattleContext ctx);

    /// <summary>
    /// 战斗开始时初始化钩子（默认无行为）。
    /// 可用于注册事件监听等一次性准备逻辑。
    /// </summary>
    public virtual void OnInitialize(BattleContext ctx)
    {
    }

    /// <summary>
    /// 获取卡牌描述（支持动态内容，子类可重写）
    /// </summary>
    public virtual string GetDescription(BattleContext ctx)
    {
        return Data != null ? Data.descriptionTemplate : "";
    }

    /// <summary>
    /// 检查打出后天平是否安全（供UI提示用，使用有效费用与减费等一致）
    /// </summary>
    public virtual bool IsSafe(BattleContext ctx)
    {
        var (costType, costValue) = ctx.GetEffectiveCost(this);
        var balance = ctx.Balance;
        int newAnger = balance.AngerPoint;
        int newCalm = balance.CalmPoint;

        if (costType == CostType.Anger)
            newAnger += costValue;
        else
            newCalm += costValue;

        return Mathf.Abs(newAnger - newCalm) <= balance.MaxDifference;
    }
}

