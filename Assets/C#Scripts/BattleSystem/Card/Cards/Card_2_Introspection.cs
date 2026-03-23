/// <summary>
/// 自省：1寂静，抽3张牌
/// </summary>
public class Card_2_Introspection : Card
{
    public Card_2_Introspection(CardData data) : base(data) { }

    public override void OnPlay(BattleContext ctx)
    {
        ctx.DrawCards(1);
    }
}
