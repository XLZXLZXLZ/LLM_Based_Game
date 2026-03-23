/// <summary>
/// 独白：2狂热，获得5点信念
/// </summary>
public class Card_3_Monologue : Card
{
    public Card_3_Monologue(CardData data) : base(data) { }

    public override void OnPlay(BattleContext ctx)
    {
        ctx.GainShield(5);
    }
}
