/// <summary>
/// 追问：2狂热，造成2点动摇并抽1张牌
/// </summary>
public class Card_5_Questioning : Card
{
    public Card_5_Questioning(CardData data) : base(data) { }

    public override void OnPlay(BattleContext ctx)
    {
        ctx.DealDamageToEnemy(2);
        ctx.DrawCards(1);
    }
}
