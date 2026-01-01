/// <summary>
/// 查询类函数 - 查询信息并返回给 LLM，继续对话
/// </summary>
/// <remarks>
/// 用于：查询背包、查询状态、检索信息等
/// 行为：执行查询 → 返回结果给 LLM → LLM 生成回复 → 继续对话
/// </remarks>
public abstract class QueryFunction : GameFunction
{
    public override bool NeedReturnToLLM => true;
    public override bool ShouldEndDialogue => false;
}


