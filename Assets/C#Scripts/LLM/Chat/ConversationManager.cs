using System.Collections.Generic;
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
    /// 存储所有NPC的思考结果
    /// Key: NPC ID
    /// Value: NPCThought对象
    /// </summary>
    private Dictionary<string, NPCThought> npcThoughts = new Dictionary<string, NPCThought>();

    /// <summary>
    /// 记录每个NPC的对话计数（用于判断是否需要思考）
    /// Key: NPC ID
    /// Value: 对话次数
    /// </summary>
    private Dictionary<string, int> messageCounters = new Dictionary<string, int>();

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

    // ==================== 思考数据管理 ====================

    /// <summary>
    /// 保存NPC的思考结果
    /// </summary>
    public void SetThought(string npcId, NPCThought thought)
    {
        if (string.IsNullOrEmpty(npcId))
        {
            Debug.LogError("[ConversationManager] NPC ID不能为空！");
            return;
        }

        npcThoughts[npcId] = thought;
        
        if (logMemoryOperations)
        {
            Debug.Log($"[ConversationManager] NPC({npcId})的思考已保存");
        }
    }

    /// <summary>
    /// 获取NPC的思考结果
    /// </summary>
    public NPCThought GetThought(string npcId)
    {
        if (string.IsNullOrEmpty(npcId))
        {
            return null;
        }

        if (npcThoughts.ContainsKey(npcId))
        {
            return npcThoughts[npcId];
        }

        return null;
    }

    /// <summary>
    /// 清除NPC的思考数据
    /// </summary>
    public void ClearThought(string npcId)
    {
        if (string.IsNullOrEmpty(npcId))
        {
            return;
        }

        npcThoughts.Remove(npcId);
        messageCounters.Remove(npcId);
        
        if (logMemoryOperations)
        {
            Debug.Log($"[ConversationManager] 已清除NPC({npcId})的思考数据");
        }
    }

    /// <summary>
    /// 记录一次对话（用于思考系统计数）
    /// </summary>
    public void RecordMessageForThought(string npcId)
    {
        if (string.IsNullOrEmpty(npcId))
        {
            return;
        }

        if (!messageCounters.ContainsKey(npcId))
        {
            messageCounters[npcId] = 0;
        }

        messageCounters[npcId]++;
    }

    /// <summary>
    /// 获取NPC的对话计数
    /// </summary>
    public int GetMessageCountForThought(string npcId)
    {
        if (string.IsNullOrEmpty(npcId))
        {
            return 0;
        }

        if (messageCounters.ContainsKey(npcId))
        {
            return messageCounters[npcId];
        }

        return 0;
    }

    /// <summary>
    /// 清除所有思考数据
    /// </summary>
    public void ClearAllThoughts()
    {
        npcThoughts.Clear();
        messageCounters.Clear();
        Debug.Log("[ConversationManager] 已清除所有思考数据");
    }
}


