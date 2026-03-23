using UnityEngine;

/// <summary>
/// 测试用卡牌 - 打出时输出Debug信息
/// </summary>
public class Card_99_Test : Card
{
    public Card_99_Test(CardData data) : base(data) { }

    public override void OnPlay(BattleContext ctx)
    {
        Debug.Log($"========================================");
        Debug.Log($"[Card_99_Test] 打出卡牌: {Data.cardName}");
        Debug.Log($"[Card_99_Test] 费用类型: {CostType}");
        Debug.Log($"[Card_99_Test] 费用数值: {CostValue}");
        Debug.Log($"[Card_99_Test] 卡牌品质: {Data.quality}");
        Debug.Log($"[Card_99_Test] 当前回合: {ctx.CurrentTurn}");
        Debug.Log($"[Card_99_Test] 天平状态: 狂热={ctx.Balance.AngerPoint} 寂静={ctx.Balance.CalmPoint}");
        Debug.Log($"========================================");
    }
}
