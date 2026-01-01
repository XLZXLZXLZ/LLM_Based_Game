/// <summary>
/// 转场类函数 - 执行动作，LLM 说最后一句话，然后结束对话并转场
/// </summary>
/// <remarks>
/// 用于：进入战斗、打开商店、结束对话等
/// 行为：执行动作 → 返回结果给 LLM → LLM 生成最后一句话 → 结束对话 → 转场
/// </remarks>
public abstract class TransitionFunction : GameFunction
{
    /// <summary>
    /// 目标场景/界面名称
    /// </summary>
    public string TargetScene { get; protected set; }

    public override bool NeedReturnToLLM => true;
    public override bool ShouldEndDialogue => true;
}


