/// <summary>
/// 动作类函数 - 执行动作并返回结果给 LLM，继续对话
/// </summary>
/// <remarks>
/// 用于：赠送物品、收取金币、改变状态等
/// 行为：执行动作 → 返回结果给 LLM → LLM 生成回复 → 继续对话
/// </remarks>
public abstract class ActionFunction : GameFunction
{
    public override bool NeedReturnToLLM => true;
    public override bool ShouldEndDialogue => false;
}


