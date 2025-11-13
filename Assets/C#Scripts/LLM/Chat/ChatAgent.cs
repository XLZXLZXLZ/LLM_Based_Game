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

    [Header("思考系统设置")]
    [SerializeField]
    [Tooltip("是否启用思考系统（NPC会定期进行内心思考）")]
    private bool enableThoughtSystem = true;

    /// <summary>
    /// 发送消息给NPC并获取回复（自动管理对话历史）
    /// </summary>
    /// <param name="npcProfile">NPC配置文件</param>
    /// <param name="userMessage">用户输入的消息</param>
    /// <param name="onSuccess">成功回调，返回NPC的回复</param>
    /// <param name="onError">错误回调</param>
    public void SendMessage(NPCProfile npcProfile, string userMessage, Action<string> onSuccess, Action<string> onError = null)
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
    /// 发送消息的协程（包含思考、长期记忆检索）
    /// </summary>
    private IEnumerator SendMessageCoroutine(NPCProfile npcProfile, string userMessage, Action<string> onSuccess, Action<string> onError)
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

        // ========== 思考系统 ==========
        NPCThought currentThought = null;
        
        if (enableThoughtSystem && ThoughtManager.Instance.ShouldThink(npcProfile.npcId))
        {
            Debug.Log($"[ChatAgent] NPC({npcProfile.characterName})需要进行思考...");

            // 先构建基础系统提示词（用于思考）
            bool basePromptCompleted = false;
            string baseSystemPrompt = string.Empty;

            StartCoroutine(BuildSystemPromptAsync(npcProfile, finalUserMessage, prompt =>
            {
                baseSystemPrompt = prompt;
                basePromptCompleted = true;
            }));

            while (!basePromptCompleted)
            {
                yield return null;
            }

            // 执行思考
            bool thinkCompleted = false;
            ThoughtManager.Instance.PerformThink(
                npcProfile,
                baseSystemPrompt,
                finalUserMessage,
                history,
                thought =>
                {
                    currentThought = thought;
                    thinkCompleted = true;
                }
            );

            while (!thinkCompleted)
            {
                yield return null;
            }
        }
        else if (enableThoughtSystem)
        {
            // 获取现有的思考结果
            currentThought = ThoughtManager.Instance.GetThought(npcProfile.npcId);
        }

        // ========== 构建最终的系统提示词和用户消息 ==========
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

        // 如果有思考结果，整合到系统提示词和用户消息中
        if (currentThought != null)
        {
            systemPrompt = IntegrateThoughtIntoSystemPrompt(systemPrompt, currentThought);
            finalUserMessage = IntegrateThoughtIntoUserMessage(finalUserMessage, currentThought);
            
            // 更新历史中的最后一条用户消息
            if (history.Count > 0 && history[history.Count - 1].role == "user")
            {
                history[history.Count - 1] = new LLMManager.Message("user", finalUserMessage);
            }
        }

        if (logSystemPrompt)
        {
            Debug.Log($"[ChatAgent] 系统提示词:\n{systemPrompt}");
        }

        // 调用LLMManager发送带上下文的消息
        LLMManager.Instance.SendMessageWithContext(
            messages: history,
            onSuccess: response =>
            {
                Debug.Log($"[ChatAgent] {npcProfile.characterName}的回复: {response}");

                // 将用户消息和AI回复保存到ConversationManager
                ConversationManager.Instance.AddMessage(npcProfile.npcId, "user", finalUserMessage);
                ConversationManager.Instance.AddMessage(npcProfile.npcId, "assistant", response);

                // 记录对话次数（用于思考系统）
                if (enableThoughtSystem)
                {
                    ThoughtManager.Instance.RecordMessage(npcProfile.npcId);
                }

                onSuccess?.Invoke(response);
            },
            onError: error =>
            {
                Debug.LogError($"[ChatAgent] 请求失败: {error}");
                onError?.Invoke(error);
            },
            systemPrompt: systemPrompt,
            profile: npcProfile.llmProfile
        );
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
    /// 清除NPC的所有记忆（瞬时+短期+长期）和思考数据
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
        
        // 同时清除思考数据
        if (enableThoughtSystem)
        {
            ThoughtManager.Instance.ClearThought(npcProfile.npcId);
        }
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

        // 如果启用记忆系统，异步生成总结并更新记忆
        if (enableMemorySystem)
        {
            StartCoroutine(SummarizeAndUpdateMemory(npcProfile, oldMessages));
        }

        // 立即返回保留的新对话
        return newMessages;
    }

    /// <summary>
    /// 总结旧对话并更新短期记忆的协程
    /// </summary>
    /// <param name="npcProfile">NPC配置文件</param>
    /// <param name="oldMessages">需要总结的旧对话</param>
    private IEnumerator SummarizeAndUpdateMemory(NPCProfile npcProfile, List<LLMManager.Message> oldMessages)
    {
        // 获取当前的短期记忆
        string currentShortTermMemory = ConversationManager.Instance.GetShortTermMemory(npcProfile.npcId);

        // 构建总结提示词
        string summarizePrompt = BuildSummarizePrompt(npcProfile, currentShortTermMemory, oldMessages);

        Debug.Log($"[ChatAgent] 开始生成记忆总结...");

        bool summarizeCompleted = false;
        string newSummary = string.Empty;

        // 调用LLM生成总结
        LLMManager.Instance.SendMessage(
            userMessage: summarizePrompt,
            onSuccess: response =>
            {
                newSummary = response;
                summarizeCompleted = true;
                Debug.Log($"[ChatAgent] 记忆总结生成成功");
            },
            onError: error =>
            {
                Debug.LogError($"[ChatAgent] 记忆总结生成失败: {error}");
                summarizeCompleted = true;
            },
            systemPrompt: "你是一个专业的对话总结助手，擅长提取对话中的关键信息并生成简洁的总结。",
            profile: npcProfile.llmProfile
        );

        // 等待总结完成
        while (!summarizeCompleted)
        {
            yield return null;
        }

        // 如果生成成功，更新短期记忆
        if (!string.IsNullOrEmpty(newSummary))
        {
            // 如果已有短期记忆，追加新总结；否则直接设置
            if (string.IsNullOrEmpty(currentShortTermMemory))
            {
                ConversationManager.Instance.SetShortTermMemory(npcProfile.npcId, newSummary);
            }
            else
            {
                ConversationManager.Instance.AppendShortTermMemory(npcProfile.npcId, newSummary);
            }

            Debug.Log($"[ChatAgent] NPC({npcProfile.characterName})的短期记忆已更新");
        }

        // 同时提取长期记忆
        MemoryExtractor.Instance.ExtractMemories(
            npcProfile: npcProfile,
            messages: oldMessages,
            onComplete: memoryFacts =>
            {
                if (memoryFacts != null && memoryFacts.Count > 0)
                {
                    ConversationManager.Instance.AddMemoryFacts(npcProfile.npcId, memoryFacts);
                    Debug.Log($"[ChatAgent] NPC({npcProfile.characterName})添加了{memoryFacts.Count}条长期记忆");
                }
            }
        );
    }

    /// <summary>
    /// 构建用于总结对话的提示词
    /// </summary>
    /// <param name="npcProfile">NPC配置文件</param>
    /// <param name="currentShortTermMemory">当前的短期记忆</param>
    /// <param name="oldMessages">需要总结的旧对话</param>
    /// <returns>总结提示词</returns>
    private string BuildSummarizePrompt(NPCProfile npcProfile, string currentShortTermMemory, List<LLMManager.Message> oldMessages)
    {
        StringBuilder promptBuilder = new StringBuilder();

        promptBuilder.AppendLine("请帮我总结以下对话内容，要求：");
        promptBuilder.AppendLine("1. 提取关键信息、重要事件和决策");
        promptBuilder.AppendLine("2. 主要关注于内容中的事实和细节");
        promptBuilder.AppendLine("3. 使用第三人称客观描述");
        promptBuilder.AppendLine("4. 简洁明了，控制在200字以内");
        promptBuilder.AppendLine();

        // 如果有之前的短期记忆，包含进来
        if (!string.IsNullOrEmpty(currentShortTermMemory))
        {
            promptBuilder.AppendLine("【之前的对话总结】");
            promptBuilder.AppendLine(currentShortTermMemory);
            promptBuilder.AppendLine();
        }

        promptBuilder.AppendLine("【需要总结的对话】");
        foreach (var message in oldMessages)
        {
            string roleName = message.role == "user" ? "玩家" : npcProfile.characterName;
            promptBuilder.AppendLine($"{roleName}: {message.content}");
        }
        promptBuilder.AppendLine();

        if (!string.IsNullOrEmpty(currentShortTermMemory))
        {
            promptBuilder.AppendLine("请将新的对话内容与之前的总结整合，生成一个更完整的总结。");
        }
        else
        {
            promptBuilder.AppendLine("请生成这段对话的总结。");
        }

        return promptBuilder.ToString();
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

    /// <summary>
    /// 是否启用思考系统
    /// </summary>
    public bool EnableThoughtSystem
    {
        get => enableThoughtSystem;
        set => enableThoughtSystem = value;
    }

    // ==================== 思考系统辅助方法 ====================

    /// <summary>
    /// 将思考结果整合到系统提示词中
    /// </summary>
    /// <param name="systemPrompt">原始系统提示词</param>
    /// <param name="thought">思考结果</param>
    /// <returns>整合后的系统提示词</returns>
    private string IntegrateThoughtIntoSystemPrompt(string systemPrompt, NPCThought thought)
    {
        if (thought == null || string.IsNullOrEmpty(thought.behaviorGuidance))
        {
            return systemPrompt;
        }

        StringBuilder promptBuilder = new StringBuilder(systemPrompt);

        // 在行为指导部分之前插入思考的行为指导
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("【当前行为指导】");
        promptBuilder.AppendLine(thought.behaviorGuidance);
        promptBuilder.AppendLine("（这是你近期思考后制定的行为准则，请在接下来的对话中遵循）");

        return promptBuilder.ToString();
    }

    /// <summary>
    /// 将思考结果整合到用户消息中
    /// </summary>
    /// <param name="userMessage">原始用户消息</param>
    /// <param name="thought">思考结果</param>
    /// <returns>整合后的用户消息</returns>
    private string IntegrateThoughtIntoUserMessage(string userMessage, NPCThought thought)
    {
        if (thought == null || string.IsNullOrEmpty(thought.innerThought))
        {
            return userMessage;
        }

        // 在用户消息前添加内心想法作为上下文
        StringBuilder messageBuilder = new StringBuilder();
        
        messageBuilder.AppendLine($"[我的内心想法: {thought.innerThought}]");
        messageBuilder.AppendLine();
        messageBuilder.Append(userMessage);

        return messageBuilder.ToString();
    }

    /// <summary>
    /// 清除NPC的思考数据
    /// </summary>
    /// <param name="npcProfile">NPC配置文件</param>
    public void ClearThought(NPCProfile npcProfile)
    {
        if (npcProfile == null || string.IsNullOrEmpty(npcProfile.npcId))
        {
            Debug.LogError("[ChatAgent] 无法清除思考：NPCProfile或npcId为空");
            return;
        }

        ThoughtManager.Instance.ClearThought(npcProfile.npcId);
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


