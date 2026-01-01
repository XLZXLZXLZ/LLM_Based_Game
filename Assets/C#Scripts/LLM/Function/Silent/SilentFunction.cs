using System.Collections.Generic;

/// <summary>
/// 静默类函数 - 静默执行，不返回给 LLM，继续对话
/// </summary>
/// <remarks>
/// 用于：增加好感度、设置 Flag、记录事件等
/// 行为：静默执行 → 不告诉 LLM → 继续对话（LLM 不知道发生了什么）
/// </remarks>
public abstract class SilentFunction : GameFunction
{
    public override bool NeedReturnToLLM => false;
    public override bool ShouldEndDialogue => false;

    public override string Execute(Dictionary<string, object> args)
    {
        ExecuteSilently(args);
        return null;  // 不返回内容给 LLM
    }

    /// <summary>
    /// 静默执行函数（子类实现）
    /// </summary>
    protected abstract void ExecuteSilently(Dictionary<string, object> args);
}


