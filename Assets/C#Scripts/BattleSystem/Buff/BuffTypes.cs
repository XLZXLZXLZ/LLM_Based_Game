public enum BuffStackMode
{
    Stack,
    KeepHigher,
    Refresh,
    Ignore
}

public enum BuffDurationType
{
    Permanent,
    TurnBased
}

public class BuffDefinition
{
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public int MaxStacks { get; set; }
    public BuffStackMode StackMode { get; set; }
    public BuffDurationType DurationType { get; set; }
    public int DefaultDuration { get; set; }
}

public class BuffInstance
{
    public BuffDefinition Definition { get; set; }
    public int Stacks { get; set; }
    public int RemainingTurns { get; set; }
    public object Source { get; set; }
}

public interface IBuffBehavior
{
    void OnApply(BuffInstance buff, BattleContext ctx);
    void OnRemove(BuffInstance buff, BattleContext ctx);
    void OnStackChanged(BuffInstance buff, BattleContext ctx, int oldStacks);
}
