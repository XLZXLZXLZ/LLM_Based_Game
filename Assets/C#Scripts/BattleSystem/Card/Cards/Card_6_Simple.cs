/// <summary>
/// 基础卡牌实现，对敌人造成等同费用的伤害（用于测试和作为默认实现）
/// </summary>
public class Card_6_Simple : Card
{
    public Card_6_Simple(CardData data) : base(data) { }

    public override void OnPlay(BattleContext ctx)
    {
        ctx.DealDamageToEnemy(CostValue);
    }
}
