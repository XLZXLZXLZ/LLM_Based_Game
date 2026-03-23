/// <summary>
/// 言语：1狂热，造成5点动摇
/// </summary>
public class Card_1_Speech : Card
{
    public Card_1_Speech(CardData data) : base(data) { }

    public override void OnPlay(BattleContext ctx)
    {
        ctx.DealDamageToEnemy(5);
    }
}
