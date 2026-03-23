/// <summary>
/// 虔诚 Buff 行为：每次对敌造成动摇后，获得固定数值的信念（护盾）。
/// </summary>
public class DevotionBuffBehavior : IBuffBehavior
{
    private readonly int shieldAmount;

    public DevotionBuffBehavior(int shieldAmount = 3)
    {
        this.shieldAmount = shieldAmount;
    }

    public void OnApply(BuffInstance buff, BattleContext ctx)
    {
        ctx.Events.OnDamageDealt.OnAfter(args =>
        {
            if (args.Target == DamageTarget.Enemy && args.Amount > 0)
                ctx.GainShield(shieldAmount);
        }, EventPriority.Normal, buff);
    }

    public void OnRemove(BuffInstance buff, BattleContext ctx)
    {
        ctx.Events.UnregisterAllByOwner(buff);
    }

    public void OnStackChanged(BuffInstance buff, BattleContext ctx, int oldStacks) { }
}
