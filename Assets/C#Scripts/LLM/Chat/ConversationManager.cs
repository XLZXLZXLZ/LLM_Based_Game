using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 对话管理器，管理所有NPC的记忆和对话历史
/// </summary>
public class ConversationManager : Singleton<ConversationManager>
{
    [Header("对话设置")]
    [SerializeField]
    [Tooltip("每个NPC保留的最大对话条数（0表示无限制）")]
    private int maxHistoryCount = 20;

    [Header("调试选项")]
    [SerializeField]
    [Tooltip("是否在控制台打印记忆操作日志")]
    private bool logMemoryOperations = true;

    /// <summary>
    /// 存储所有NPC的记忆
    /// Key: NPC ID
    /// Value: NPCMemory对象
    /// </summary>
    private Dictionary<string, NPCMemory> npcMemories = new Dictionary<string, NPCMemory>();

    /// <summary>
    /// 获取或创建NPC记忆
    /// </summary>
    /// <param name="npcId">NPC的唯一ID</param>
    /// <returns>NPC记忆对象</returns>
    public NPCMemory GetOrCreateMemory(string npcId)
    {
        if (string.IsNullOrEmpty(npcId))
        {
            Debug.LogError("[ConversationManager] NPC ID不能为空！");
            return null;
        }

        if (!npcMemories.ContainsKey(npcId))
        {
            npcMemories[npcId] = new NPCMemory(npcId);
            if (logMemoryOperations)
            {
                Debug.Log($"[ConversationManager] 为NPC({npcId})创建了新的记忆");
            }
        }

        return npcMemories[npcId];
    }

    /// <summary>
    /// 获取NPC的对话历史
    /// </summary>
    /// <param name="npcId">NPC的唯一ID</param>
    /// <returns>对话历史列表，如果不存在则返回空列表</returns>
    public List<LLMManager.Message> GetConversationHistory(string npcId)
    {
        var memory = GetOrCreateMemory(npcId);
        return memory?.GetConversationHistory() ?? new List<LLMManager.Message>();
    }

    /// <summary>
    /// 添加一条消息到NPC的对话历史
    /// </summary>
    /// <param name="npcId">NPC的唯一ID</param>
    /// <param name="role">角色（user/assistant）</param>
    /// <param name="content">消息内容</param>
    public void AddMessage(string npcId, string role, string content)
    {
        var memory = GetOrCreateMemory(npcId);
        if (memory != null)
        {
            memory.AddMessage(role, content);

            if (logMemoryOperations)
            {
                Debug.Log($"[ConversationManager] NPC({npcId})添加消息 [{role}]: {content}");
            }

            // 自动裁剪历史
            if (maxHistoryCount > 0)
            {
                memory.TrimConversationHistory(maxHistoryCount);
            }
        }
    }

    /// <summary>
    /// 清除NPC的对话历史（仅瞬时记忆）
    /// </summary>
    /// <param name="npcId">NPC的唯一ID</param>
    public void ClearConversationHistory(string npcId)
    {
        var memory = GetOrCreateMemory(npcId);
        memory?.ClearConversationHistory();
    }

    /// <summary>
    /// 清除NPC的所有记忆（瞬时+短期）
    /// </summary>
    /// <param name="npcId">NPC的唯一ID</param>
    public void ClearAllMemory(string npcId)
    {
        var memory = GetOrCreateMemory(npcId);
        memory?.ClearAllMemory();
    }

    /// <summary>
    /// 清除所有NPC的记忆
    /// </summary>
    public void ClearAllMemories()
    {
        npcMemories.Clear();
        Debug.Log("[ConversationManager] 已清除所有NPC的记忆");
    }

    /// <summary>
    /// 设置NPC的短期记忆
    /// </summary>
    /// <param name="npcId">NPC的唯一ID</param>
    /// <param name="summary">对话总结</param>
    public void SetShortTermMemory(string npcId, string summary)
    {
        var memory = GetOrCreateMemory(npcId);
        memory?.SetShortTermMemory(summary);
    }

    /// <summary>
    /// 追加内容到NPC的短期记忆
    /// </summary>
    /// <param name="npcId">NPC的唯一ID</param>
    /// <param name="additionalSummary">要追加的总结内容</param>
    public void AppendShortTermMemory(string npcId, string additionalSummary)
    {
        var memory = GetOrCreateMemory(npcId);
        memory?.AppendShortTermMemory(additionalSummary);
    }

    /// <summary>
    /// 获取NPC的短期记忆
    /// </summary>
    /// <param name="npcId">NPC的唯一ID</param>
    /// <returns>短期记忆内容</returns>
    public string GetShortTermMemory(string npcId)
    {
        var memory = GetOrCreateMemory(npcId);
        return memory?.GetShortTermMemory() ?? string.Empty;
    }

    /// <summary>
    /// 清除NPC的短期记忆
    /// </summary>
    /// <param name="npcId">NPC的唯一ID</param>
    public void ClearShortTermMemory(string npcId)
    {
        var memory = GetOrCreateMemory(npcId);
        memory?.ClearShortTermMemory();
    }

    /// <summary>
    /// 检查是否存在某个NPC的记忆
    /// </summary>
    /// <param name="npcId">NPC的唯一ID</param>
    /// <returns>是否存在</returns>
    public bool HasMemory(string npcId)
    {
        return npcMemories.ContainsKey(npcId);
    }

    /// <summary>
    /// 获取某个NPC的对话消息数量
    /// </summary>
    /// <param name="npcId">NPC的唯一ID</param>
    /// <returns>消息数量</returns>
    public int GetMessageCount(string npcId)
    {
        var memory = GetOrCreateMemory(npcId);
        return memory?.GetMessageCount() ?? 0;
    }

    /// <summary>
    /// 最大历史记录数量（0表示无限制）
    /// </summary>
    public int MaxHistoryCount
    {
        get => maxHistoryCount;
        set
        {
            maxHistoryCount = value;
            Debug.Log($"[ConversationManager] 最大历史记录数量已设置为: {value}");
        }
    }

    /// <summary>
    /// 当前管理的NPC数量
    /// </summary>
    public int NPCCount => npcMemories.Count;

    /// <summary>
    /// 是否记录内存操作日志
    /// </summary>
    public bool LogMemoryOperations
    {
        get => logMemoryOperations;
        set => logMemoryOperations = value;
    }

    // ==================== 长期记忆管理 ====================

    /// <summary>
    /// 获取NPC的长期记忆
    /// </summary>
    public LongTermMemory GetLongTermMemory(string npcId)
    {
        var memory = GetOrCreateMemory(npcId);
        return memory?.longTermMemory;
    }

    /// <summary>
    /// 添加记忆事实到长期记忆
    /// </summary>
    public void AddMemoryFact(string npcId, MemoryFact fact)
    {
        var longTermMemory = GetLongTermMemory(npcId);
        longTermMemory?.AddFact(fact);
    }

    /// <summary>
    /// 批量添加记忆事实
    /// </summary>
    public void AddMemoryFacts(string npcId, List<MemoryFact> facts)
    {
        var longTermMemory = GetLongTermMemory(npcId);
        longTermMemory?.AddFacts(facts);
    }

    /// <summary>
    /// 检索相关记忆
    /// </summary>
    public List<MemoryFact> RetrieveRelevantMemories(string npcId, float[] queryEmbedding, int topK = 5, float? threshold = null)
    {
        var longTermMemory = GetLongTermMemory(npcId);
        return longTermMemory?.RetrieveRelevantMemories(queryEmbedding, topK, threshold) ?? new List<MemoryFact>();
    }

    /// <summary>
    /// 获取所有长期记忆事实
    /// </summary>
    public List<MemoryFact> GetAllMemoryFacts(string npcId)
    {
        var longTermMemory = GetLongTermMemory(npcId);
        return longTermMemory?.GetAllFacts() ?? new List<MemoryFact>();
    }

    /// <summary>
    /// 清空长期记忆
    /// </summary>
    public void ClearLongTermMemory(string npcId)
    {
        var longTermMemory = GetLongTermMemory(npcId);
        longTermMemory?.ClearAllFacts();
    }

    /// <summary>
    /// 获取长期记忆数量
    /// </summary>
    public int GetLongTermMemoryCount(string npcId)
    {
        var longTermMemory = GetLongTermMemory(npcId);
        return longTermMemory?.FactCount ?? 0;
    }

    // ==================== 记忆总结 ====================

    /// <summary>
    /// 启动总结旧对话并更新短期记忆的流程
    /// </summary>
    /// <param name="npcProfile">NPC配置文件</param>
    /// <param name="oldMessages">需要总结的旧对话</param>
    public void SummarizeAndUpdateMemory(NPCProfile npcProfile, List<LLMManager.Message> oldMessages)
    {
        StartCoroutine(SummarizeAndUpdateMemoryCoroutine(npcProfile, oldMessages));
    }

    /// <summary>
    /// 总结旧对话并更新短期记忆的协程
    /// </summary>
    private IEnumerator SummarizeAndUpdateMemoryCoroutine(NPCProfile npcProfile, List<LLMManager.Message> oldMessages)
    {
        // 获取当前的短期记忆
        string currentShortTermMemory = GetShortTermMemory(npcProfile.npcId);

        // 构建总结提示词
        string summarizePrompt = BuildSummarizePrompt(npcProfile, currentShortTermMemory, oldMessages);

        if (logMemoryOperations)
        {
            Debug.Log($"[ConversationManager] 开始生成记忆总结...");
        }

        bool summarizeCompleted = false;
        string newSummary = string.Empty;

        // 调用LLM生成总结
        LLMManager.Instance.SendMessage(
            userMessage: summarizePrompt,
            onResponse: (content, toolCalls) =>
            {
                newSummary = content;
                summarizeCompleted = true;
                if (logMemoryOperations)
                {
                    Debug.Log($"[ConversationManager] 记忆总结生成成功");
                }
            },
            onError: error =>
            {
                Debug.LogError($"[ConversationManager] 记忆总结生成失败: {error}");
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
                SetShortTermMemory(npcProfile.npcId, newSummary);
            }
            else
            {
                AppendShortTermMemory(npcProfile.npcId, newSummary);
            }

            if (logMemoryOperations)
            {
                Debug.Log($"[ConversationManager] NPC({npcProfile.characterName})的短期记忆已更新");
            }
        }

        // 同时提取长期记忆
        MemoryExtractor.Instance.ExtractMemories(
            npcProfile: npcProfile,
            messages: oldMessages,
            onComplete: memoryFacts =>
            {
                if (memoryFacts != null && memoryFacts.Count > 0)
                {
                    AddMemoryFacts(npcProfile.npcId, memoryFacts);
                    if (logMemoryOperations)
                    {
                        Debug.Log($"[ConversationManager] NPC({npcProfile.characterName})添加了{memoryFacts.Count}条长期记忆");
                    }
                }
            }
        );
    }

    /// <summary>
    /// 构建用于总结对话的提示词
    /// </summary>
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
}
