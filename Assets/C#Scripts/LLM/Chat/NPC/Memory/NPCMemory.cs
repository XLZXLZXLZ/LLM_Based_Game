using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NPC记忆类，存储NPC的各种记忆信息
/// 三层记忆架构：
/// 1. 瞬时记忆 - 最近的对话上下文（conversationHistory）
/// 2. 短期记忆 - 较早对话的总结（shortTermMemory）
/// 3. 长期记忆 - RAG知识库（预留）
/// </summary>
[Serializable]
public class NPCMemory
{
    /// <summary>
    /// NPC的唯一标识
    /// </summary>
    public string npcId;

    /// <summary>
    /// 瞬时记忆 - 最近的对话上下文历史
    /// </summary>
    public List<LLMManager.Message> conversationHistory;

    /// <summary>
    /// 短期记忆 - 较早对话的总结文本
    /// 当瞬时记忆超出限制时，旧对话会被总结并存储在此
    /// </summary>
    public string shortTermMemory;

    /// <summary>
    /// 长期记忆 - RAG知识库
    /// </summary>
    public LongTermMemory longTermMemory;

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime lastUpdateTime;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="npcId">NPC的唯一ID</param>
    public NPCMemory(string npcId)
    {
        this.npcId = npcId;
        this.conversationHistory = new List<LLMManager.Message>();
        this.shortTermMemory = string.Empty;
        this.longTermMemory = new LongTermMemory(npcId);
        this.lastUpdateTime = DateTime.Now;
    }

    /// <summary>
    /// 添加一条消息到瞬时记忆
    /// </summary>
    /// <param name="role">角色（user/assistant）</param>
    /// <param name="content">消息内容</param>
    public void AddMessage(string role, string content)
    {
        conversationHistory.Add(new LLMManager.Message(role, content));
        lastUpdateTime = DateTime.Now;
    }

    /// <summary>
    /// 获取对话历史
    /// </summary>
    /// <returns>对话历史列表</returns>
    public List<LLMManager.Message> GetConversationHistory()
    {
        return new List<LLMManager.Message>(conversationHistory);
    }

    /// <summary>
    /// 清除瞬时记忆
    /// </summary>
    public void ClearConversationHistory()
    {
        conversationHistory.Clear();
        lastUpdateTime = DateTime.Now;
        Debug.Log($"[NPCMemory] 已清除NPC({npcId})的瞬时记忆");
    }

    /// <summary>
    /// 设置短期记忆
    /// </summary>
    /// <param name="summary">对话总结</param>
    public void SetShortTermMemory(string summary)
    {
        shortTermMemory = summary;
        lastUpdateTime = DateTime.Now;
        Debug.Log($"[NPCMemory] 已更新NPC({npcId})的短期记忆");
    }

    /// <summary>
    /// 追加内容到短期记忆
    /// </summary>
    /// <param name="additionalSummary">要追加的总结内容</param>
    public void AppendShortTermMemory(string additionalSummary)
    {
        if (string.IsNullOrEmpty(shortTermMemory))
        {
            shortTermMemory = additionalSummary;
        }
        else
        {
            shortTermMemory += "\n\n" + additionalSummary;
        }
        lastUpdateTime = DateTime.Now;
        Debug.Log($"[NPCMemory] 已追加内容到NPC({npcId})的短期记忆");
    }

    /// <summary>
    /// 获取短期记忆
    /// </summary>
    /// <returns>短期记忆内容</returns>
    public string GetShortTermMemory()
    {
        return shortTermMemory ?? string.Empty;
    }

    /// <summary>
    /// 清除短期记忆
    /// </summary>
    public void ClearShortTermMemory()
    {
        shortTermMemory = string.Empty;
        lastUpdateTime = DateTime.Now;
        Debug.Log($"[NPCMemory] 已清除NPC({npcId})的短期记忆");
    }

    /// <summary>
    /// 清除所有记忆（瞬时+短期+长期）
    /// </summary>
    public void ClearAllMemory()
    {
        conversationHistory.Clear();
        shortTermMemory = string.Empty;
        longTermMemory.ClearAllFacts();
        lastUpdateTime = DateTime.Now;
        Debug.Log($"[NPCMemory] 已清除NPC({npcId})的所有记忆");
    }

    /// <summary>
    /// 裁剪对话历史，保留最近的N条消息
    /// </summary>
    /// <param name="maxCount">保留的最大消息数量</param>
    public void TrimConversationHistory(int maxCount)
    {
        if (conversationHistory.Count > maxCount)
        {
            int removeCount = conversationHistory.Count - maxCount;
            conversationHistory.RemoveRange(0, removeCount);
            Debug.Log($"[NPCMemory] NPC({npcId})的对话历史已裁剪，移除了{removeCount}条旧消息");
        }
    }

    /// <summary>
    /// 获取对话历史的消息数量
    /// </summary>
    /// <returns>消息数量</returns>
    public int GetMessageCount()
    {
        return conversationHistory.Count;
    }
}

