/// <summary>
/// 虔诚：寂静系，本回合每次对敌造成动摇时获得3信念
/// </summary>
public class Card_7_Devotion : Card
{
    public Card_7_Devotion(CardData data) : base(data)
    {
        EnsureBuffRegistered();
    }

    public override void OnPlay(BattleContext ctx)
    {
        ctx.Buffs.ApplyBuff("devotion", 1, this);
    }

    public override string GetDescription(BattleContext ctx)
    {
        return "本回合每次造成动摇时，获得3信念";
    }

    private static void EnsureBuffRegistered()
    {
        if (BuffRegistry.IsRegistered("devotion")) return;
        BuffRegistry.Register("devotion", new BuffDefinition
        {
            Id = "devotion",
            DisplayName = "虔诚",
            Description = "本回合每次造成动摇时，获得3信念",
            MaxStacks = 1,
            StackMode = BuffStackMode.Refresh,
            DurationType = BuffDurationType.TurnBased,
            DefaultDuration = 1
        }, new DevotionBuffBehavior(3));
    }
}
