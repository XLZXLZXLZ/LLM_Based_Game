using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 聊天代理，负责处理NPC对话，自动管理上下文
/// </summary>
public class ChatAgent : Singleton<ChatAgent>
{
    [Header("调试选项")]
    [SerializeField]
    [Tooltip("是否在控制台打印系统提示词")]
    private bool logSystemPrompt = true;

    [Header("记忆管理设置")]
    [SerializeField]
    [Tooltip("是否启用记忆系统（包括短期记忆、长期记忆和记忆总结）")]
    private bool enableMemorySystem = true;

    [SerializeField]
    [Tooltip("上下文溢出时保留的比例（0-1）")]
    [Range(0.1f, 0.9f)]
    private float retainRatio = 0.5f;

    [SerializeField]
    [Tooltip("长期记忆检索数量")]
    [Range(1, 10)]
    private int longTermMemoryTopK = 5;

    /// <summary>
    /// 发送消息给NPC并获取回复（自动管理对话历史）
    /// </summary>
    /// <param name="npcProfile">NPC配置文件</param>
    /// <param name="userMessage">用户输入的消息</param>
    /// <param name="onSuccess">成功回调，返回NPC的回复</param>
    /// <param name="onError">错误回调</param>
    public void SendMessage(NPCProfile npcProfile, string userMessage, Action<string, List<LLMManager.ToolCall>> onSuccess, Action<string> onError = null)
    {
        if (npcProfile == null)
        {
            string error = "NPCProfile不能为空！";
            Debug.LogError(error);
            onError?.Invoke(error);
            return;
        }

        if (string.IsNullOrEmpty(npcProfile.npcId))
        {
            string error = $"NPCProfile({npcProfile.characterName})的npcId不能为空！";
            Debug.LogError(error);
            onError?.Invoke(error);
            return;
        }

        if (string.IsNullOrEmpty(userMessage))
        {
            string error = "用户消息不能为空！";
            Debug.LogError(error);
            onError?.Invoke(error);
            return;
        }

        // 启动协程处理完整的发送流程
        StartCoroutine(SendMessageCoroutine(npcProfile, userMessage, onSuccess, onError));
    }

    /// <summary>
    /// 发送消息的协程（包含长期记忆检索、工具调用循环）
    /// </summary>
    private IEnumerator SendMessageCoroutine(NPCProfile npcProfile, string userMessage, Action<string, List<LLMManager.ToolCall>> onSuccess, Action<string> onError)
    {
        // 获取对话历史
        List<LLMManager.Message> history = ConversationManager.Instance.GetConversationHistory(npcProfile.npcId);

        // 构建用户消息
        string finalUserMessage = BuildUserMessage(userMessage);

        // 添加用户消息到临时历史
        history.Add(new LLMManager.Message("user", finalUserMessage));

        // 处理上下文溢出（如果需要）
        if (enableMemorySystem)
        {
            history = HandleContextOverflow(npcProfile, history);
        }

        // ========== 构建最终的系统提示词 ==========
        bool systemPromptCompleted = false;
        string systemPrompt = string.Empty;

        StartCoroutine(BuildSystemPromptAsync(npcProfile, finalUserMessage, prompt =>
        {
            systemPrompt = prompt;
            systemPromptCompleted = true;
        }));

        // 等待系统提示词构建完成
        while (!systemPromptCompleted)
        {
            yield return null;
        }

        if (logSystemPrompt)
        {
            Debug.Log($"[ChatAgent] 系统提示词:\n{systemPrompt}");
        }

        // 获取工具列表 (由 FunctionCallManager 管理)
        List<LLMManager.Tool> tools = FunctionCallManager.Instance.GetTools();

        // 循环处理 LLM 请求（直到 LLM 返回文本回复）
        bool isLooping = true;
        int loopCount = 0;
        const int MAX_LOOPS = 5; // 防止无限递归

        while (isLooping && loopCount < MAX_LOOPS)
        {
            loopCount++;
            bool requestCompleted = false;

            // 调用 LLM API
            LLMManager.Instance.SendMessage(
                messages: history,
                tools: tools,
                toolChoice: "auto",
                onResponse: (content, toolCalls) =>
                {
                    requestCompleted = true;
                    
                    // 情况1: LLM 返回纯文本
                    if (toolCalls == null || toolCalls.Count == 0)
                    {
                        Debug.Log($"[ChatAgent] {npcProfile.characterName}的回复: {content}");
                        
                        // 记录回复到历史
                        history.Add(new LLMManager.Message("assistant", content));
                        ConversationManager.Instance.AddMessage(npcProfile.npcId, "user", finalUserMessage); // 注意：这里可能重复添加用户消息，实际ConversationManager应该只同步最后的状态，简化起见先保留逻辑
                        ConversationManager.Instance.AddMessage(npcProfile.npcId, "assistant", content);
                        
                        onSuccess?.Invoke(content, null);
                        isLooping = false; // 结束循环
                    }
                    // 情况2: LLM 请求调用工具
                    else
                    {
                        Debug.Log($"[ChatAgent] LLM 请求调用 {toolCalls.Count} 个工具");
                        
                        // 1. 将 LLM 的工具调用请求添加到历史 (role: assistant, tool_calls: [...])
                        var assistantMsg = new LLMManager.Message
                        {
                            role = "assistant",
                            content = content, // 通常为 null，但也可能有部分思考内容
                            tool_calls = toolCalls
                        };
                        history.Add(assistantMsg);

                        // 2. 执行每个工具，并将结果作为新消息添加到历史 (role: tool)
                        foreach (var toolCall in toolCalls)
                        {
                            // 委托 FunctionCallManager 执行工具
                            string result = FunctionCallManager.Instance.ExecuteToolCall(toolCall);
                            
                            // 构建 Tool 结果消息
                            var toolMsg = new LLMManager.Message
                            {
                                role = "tool",
                                content = result,
                                tool_call_id = toolCall.id,
                                name = toolCall.function.name
                            };
                            history.Add(toolMsg);
                        }

                        // 3. 保持 isLooping = true，进行下一轮请求（将结果发回给 LLM）
                        Debug.Log("[ChatAgent] 工具执行完毕，正在将结果回传给 LLM...");
                    }
                },
                onError: error =>
                {
                    requestCompleted = true;
                    isLooping = false;
                    Debug.LogError($"[ChatAgent] 请求失败: {error}");
                    onError?.Invoke(error);
                },
                systemPrompt: systemPrompt,
                profile: npcProfile.llmProfile
            );

            // 等待请求完成
            while (!requestCompleted)
            {
                yield return null;
            }
        }

        if (loopCount >= MAX_LOOPS)
        {
            Debug.LogWarning("[ChatAgent] 工具调用循环次数过多，强制终止对话");
            onError?.Invoke("对话异常：工具调用循环次数过多");
        }
    }

    /// <summary>
    /// 清除NPC的瞬时记忆（对话历史）
    /// </summary>
    /// <param name="npcProfile">NPC配置文件</param>
    public void ClearHistory(NPCProfile npcProfile)
    {
        if (npcProfile == null || string.IsNullOrEmpty(npcProfile.npcId))
        {
            Debug.LogError("[ChatAgent] 无法清除历史：NPCProfile或npcId为空");
            return;
        }

        ConversationManager.Instance.ClearConversationHistory(npcProfile.npcId);
    }

    /// <summary>
    /// 清除NPC的所有记忆（瞬时+短期+长期）
    /// </summary>
    /// <param name="npcProfile">NPC配置文件</param>
    public void ClearAllMemory(NPCProfile npcProfile)
    {
        if (npcProfile == null || string.IsNullOrEmpty(npcProfile.npcId))
        {
            Debug.LogError("[ChatAgent] 无法清除记忆：NPCProfile或npcId为空");
            return;
        }

        ConversationManager.Instance.ClearAllMemory(npcProfile.npcId);
    }

    /// <summary>
    /// 异步构建系统提示词（包含长期记忆检索）
    /// </summary>
    /// <param name="npcProfile">NPC配置文件</param>
    /// <param name="userMessage">用户当前输入（用于检索相关记忆）</param>
    /// <param name="onComplete">完成回调</param>
    private IEnumerator BuildSystemPromptAsync(NPCProfile npcProfile, string userMessage, Action<string> onComplete)
    {
        if (npcProfile == null)
        {
            Debug.LogWarning("[ChatAgent] NPCProfile为空，返回空系统提示词");
            onComplete?.Invoke(string.Empty);
            yield break;
        }

        StringBuilder promptBuilder = new StringBuilder();

        // 角色身份
        promptBuilder.AppendLine($"你现在要扮演一个名叫\"{npcProfile.characterName}\"的角色。");
        promptBuilder.AppendLine();

        // 角色背景
        if (!string.IsNullOrEmpty(npcProfile.background))
        {
            promptBuilder.AppendLine("【角色背景】");
            promptBuilder.AppendLine(npcProfile.background.Trim());
            promptBuilder.AppendLine();
        }

        // 性格特征
        if (!string.IsNullOrEmpty(npcProfile.personality))
        {
            promptBuilder.AppendLine("【性格特征】");
            promptBuilder.AppendLine(npcProfile.personality.Trim());
            promptBuilder.AppendLine();
        }

        // 对话风格
        if (!string.IsNullOrEmpty(npcProfile.speakingStyle))
        {
            promptBuilder.AppendLine("【对话风格】");
            promptBuilder.AppendLine(npcProfile.speakingStyle.Trim());
            promptBuilder.AppendLine();
        }

        // 角色目标
        if (!string.IsNullOrEmpty(npcProfile.goals))
        {
            promptBuilder.AppendLine("【角色目标】");
            promptBuilder.AppendLine(npcProfile.goals.Trim());
            promptBuilder.AppendLine();
        }

        // 其他信息
        if (!string.IsNullOrEmpty(npcProfile.additionalInfo))
        {
            promptBuilder.AppendLine("【其他信息】");
            promptBuilder.AppendLine(npcProfile.additionalInfo.Trim());
            promptBuilder.AppendLine();
        }

        // 如果启用记忆系统，添加记忆内容
        if (enableMemorySystem)
        {
            // 短期记忆（较早对话的总结）
            string shortTermMemory = ConversationManager.Instance.GetShortTermMemory(npcProfile.npcId);
            if (!string.IsNullOrEmpty(shortTermMemory))
            {
                promptBuilder.AppendLine("【历史对话总结】");
                promptBuilder.AppendLine(shortTermMemory.Trim());
                promptBuilder.AppendLine();
            }

            // 长期记忆（检索相关记忆）
            bool memoryRetrievalCompleted = false;
            List<MemoryFact> relevantMemories = new List<MemoryFact>();

            // 异步检索长期记忆
            yield return RetrieveMemoriesCoroutine(npcProfile, userMessage, longTermMemoryTopK, memories =>
            {
                relevantMemories = memories;
                memoryRetrievalCompleted = true;
            });

            // 添加长期记忆到提示词
            if (relevantMemories.Count > 0)
            {
                promptBuilder.AppendLine("【相关记忆】");
                foreach (var memory in relevantMemories)
                {
                    promptBuilder.AppendLine($"- {memory.content}");
                }
                promptBuilder.AppendLine();
            }
        }

        // 行为指导
        promptBuilder.AppendLine("【行为指导】");
        promptBuilder.AppendLine("请严格按照以上设定进行角色扮演，保持角色的一致性。");
        promptBuilder.AppendLine("用第一人称回应，不要跳出角色。");
        promptBuilder.AppendLine("回答要符合角色的背景、性格和说话风格。");
        
        if (enableMemorySystem)
        {
            promptBuilder.AppendLine("注意参考历史对话总结和相关记忆中的信息，保持对话的连贯性和记忆的一致性。");
        }

        onComplete?.Invoke(promptBuilder.ToString());
    }

    /// <summary>
    /// 构建用户消息，可以在用户输入基础上添加额外的上下文信息
    /// </summary>
    /// <param name="userInput">用户的原始输入</param>
    /// <returns>最终发送给LLM的用户消息</returns>
    public string BuildUserMessage(string userInput)
    {
        // 目前仅返回用户输入占位
        // 后续可以在这里添加场景信息、角色状态等动态上下文
        return userInput;
    }

    /// <summary>
    /// 处理上下文溢出
    /// 当对话历史超出限制时，将旧对话总结为短期记忆，并保留最新的部分对话
    /// </summary>
    /// <param name="npcProfile">NPC配置文件</param>
    /// <param name="history">当前对话历史</param>
    /// <returns>处理后的对话历史</returns>
    protected virtual List<LLMManager.Message> HandleContextOverflow(NPCProfile npcProfile, List<LLMManager.Message> history)
    {
        // 获取最大历史记录数量（从ConversationManager获取配置）
        int maxHistoryCount = ConversationManager.Instance.MaxHistoryCount;
        
        // 如果未设置限制或未超出限制，直接返回
        if (maxHistoryCount <= 0 || history.Count <= maxHistoryCount)
        {
            return history;
        }

        Debug.Log($"[ChatAgent] 检测到上下文溢出：当前{history.Count}条，限制{maxHistoryCount}条，开始处理...");

        // 计算需要总结的消息数量（最久远的50%）
        int summarizeCount = Mathf.CeilToInt(history.Count * (1 - retainRatio));
        
        // 至少要总结1条，至多总结到只剩1条
        summarizeCount = Mathf.Max(1, Mathf.Min(summarizeCount, history.Count - 1));

        // 提取要总结的旧对话
        List<LLMManager.Message> oldMessages = history.GetRange(0, summarizeCount);
        
        // 提取要保留的新对话
        List<LLMManager.Message> newMessages = history.GetRange(summarizeCount, history.Count - summarizeCount);

        Debug.Log($"[ChatAgent] 将总结最旧的{summarizeCount}条消息，保留最新的{newMessages.Count}条消息");

        // 如果启用记忆系统，通过 ConversationManager 处理记忆总结
        if (enableMemorySystem)
        {
            ConversationManager.Instance.SummarizeAndUpdateMemory(npcProfile, oldMessages);
        }

        // 立即返回保留的新对话
        return newMessages;
    }

    /// <summary>
    /// 是否记录系统提示词到控制台
    /// </summary>
    public bool LogSystemPrompt
    {
        get => logSystemPrompt;
        set => logSystemPrompt = value;
    }

    /// <summary>
    /// 是否启用记忆系统
    /// </summary>
    public bool EnableMemorySystem
    {
        get => enableMemorySystem;
        set => enableMemorySystem = value;
    }

    /// <summary>
    /// 长期记忆检索数量
    /// </summary>
    public int LongTermMemoryTopK
    {
        get => longTermMemoryTopK;
        set => longTermMemoryTopK = Mathf.Clamp(value, 1, 20);
    }

    // ==================== 内部辅助方法 ====================

    /// <summary>
    /// 内部使用：检索相关的长期记忆（用于构建系统提示词）
    /// </summary>
    private IEnumerator RetrieveMemoriesCoroutine(NPCProfile npcProfile, string query, int topK, Action<List<MemoryFact>> onComplete)
    {
        bool embeddingCompleted = false;
        float[] queryEmbedding = null;

        // 获取查询的向量嵌入
        LLMManager.Instance.GetEmbedding(
            text: query,
            onSuccess: embedding =>
            {
                queryEmbedding = embedding;
                embeddingCompleted = true;
            },
            onError: error =>
            {
                Debug.LogError($"[ChatAgent] 获取查询向量失败: {error}");
                embeddingCompleted = true;
            }
        );

        while (!embeddingCompleted)
        {
            yield return null;
        }

        if (queryEmbedding != null)
        {
            var relevantMemories = ConversationManager.Instance.RetrieveRelevantMemories(npcProfile.npcId, queryEmbedding, topK);
            Debug.Log($"[ChatAgent] 检索到{relevantMemories.Count}条相关记忆");
            onComplete?.Invoke(relevantMemories);
        }
        else
        {
            onComplete?.Invoke(new List<MemoryFact>());
        }
    }
}
