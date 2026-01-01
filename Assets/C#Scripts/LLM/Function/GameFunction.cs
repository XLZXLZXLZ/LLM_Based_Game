using System.Collections.Generic;

/// <summary>
/// 游戏函数基类 - LLM 可调用的函数抽象
/// </summary>
public abstract class GameFunction
{
    /// <summary>
    /// 函数名称（对应 API 的 function name）
    /// </summary>
    public string Name { get; protected set; }

    /// <summary>
    /// 函数描述（给 LLM 看的）
    /// </summary>
    public string Description { get; protected set; }

    /// <summary>
    /// 函数参数定义（JSON 对象）
    /// </summary>
    public object Parameters { get; protected set; }

    /// <summary>
    /// 是否需要把结果返回给 LLM
    /// </summary>
    public abstract bool NeedReturnToLLM { get; }

    /// <summary>
    /// 是否会结束对话
    /// </summary>
    public abstract bool ShouldEndDialogue { get; }

    /// <summary>
    /// 执行函数
    /// </summary>
    /// <param name="args">函数参数（从 LLM 的 arguments JSON 解析而来）</param>
    /// <returns>执行结果（如果 NeedReturnToLLM 为 true，则返回给 LLM）</returns>
    public abstract string Execute(Dictionary<string, object> args);

    /// <summary>
    /// 转换为 LLMManager.Tool 格式
    /// </summary>
    public LLMManager.Tool ToTool()
    {
        return new LLMManager.Tool
        {
            type = "function",
            function = new LLMManager.FunctionDefinition
            {
                name = Name,
                description = Description,
                parameters = Parameters
            }
        };
    }
}


