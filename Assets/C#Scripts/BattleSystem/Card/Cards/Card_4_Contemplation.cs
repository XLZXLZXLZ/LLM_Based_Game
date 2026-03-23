/// <summary>
/// 思索：3寂静，抽2张牌
/// </summary>
public class Card_4_Contemplation : Card
{
    public Card_4_Contemplation(CardData data) : base(data) { }

    public override void OnPlay(BattleContext ctx)
    {
        ctx.DrawCards(2);
    }
}
