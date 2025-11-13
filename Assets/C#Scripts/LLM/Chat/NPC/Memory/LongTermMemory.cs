using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// 长期记忆 - RAG知识库
/// 存储重要的、需要长期记住的事实信息
/// </summary>
[Serializable]
public class LongTermMemory
{
    /// <summary>
    /// NPC的唯一标识
    /// </summary>
    public string npcId;

    /// <summary>
    /// 所有记忆事实
    /// </summary>
    public List<MemoryFact> facts;

    /// <summary>
    /// 相似度阈值（低于此值的记忆不会被检索）
    /// </summary>
    public float similarityThreshold = 0.7f;

    /// <summary>
    /// 构造函数
    /// </summary>
    public LongTermMemory(string npcId)
    {
        this.npcId = npcId;
        this.facts = new List<MemoryFact>();
    }

    /// <summary>
    /// 检索相关记忆
    /// </summary>
    /// <param name="queryEmbedding">查询向量</param>
    /// <param name="topK">返回最多K条</param>
    /// <param name="threshold">相似度阈值（可选，使用默认值）</param>
    /// <returns>相关的记忆事实列表</returns>
    public List<MemoryFact> RetrieveRelevantMemories(float[] queryEmbedding, int topK = 5, float? threshold = null)
    {
        if (facts.Count == 0 || queryEmbedding == null)
        {
            return new List<MemoryFact>();
        }

        float actualThreshold = threshold ?? similarityThreshold;

        // 计算所有记忆的相似度
        var memoryScores = new List<(MemoryFact fact, float similarity)>();
        
        foreach (var fact in facts)
        {
            float similarity = fact.CalculateSimilarity(queryEmbedding);
            if (similarity >= actualThreshold)
            {
                memoryScores.Add((fact, similarity));
            }
        }

        // 按相似度降序排序，取前K个
        var topMemories = memoryScores
            .OrderByDescending(x => x.similarity)
            .Take(topK)
            .Select(x => x.fact)
            .ToList();

        Debug.Log($"[LongTermMemory] NPC({npcId}) 检索到{topMemories.Count}条相关记忆");

        return topMemories;
    }

    /// <summary>
    /// 添加新的记忆事实
    /// </summary>
    /// <param name="fact">记忆事实</param>
    public void AddFact(MemoryFact fact)
    {
        if (fact == null || string.IsNullOrEmpty(fact.content))
        {
            Debug.LogWarning("[LongTermMemory] 尝试添加空记忆");
            return;
        }

        // 检查是否有重复的记忆（基于相似度）
        if (fact.embedding != null)
        {
            var similar = RetrieveRelevantMemories(fact.embedding, 1, 0.95f);
            if (similar.Count > 0)
            {
                // 发现相似记忆，用新记忆覆盖旧记忆
                var oldFact = similar[0];
                facts.Remove(oldFact);
                facts.Add(fact);
                Debug.Log($"[LongTermMemory] NPC({npcId}) 覆盖旧记忆: \n旧: {oldFact.content}\n新: {fact.content}");
                return;
            }
        }

        facts.Add(fact);
        Debug.Log($"[LongTermMemory] NPC({npcId}) 添加新记忆: {fact}");
    }

    /// <summary>
    /// 添加新的记忆事实（带嵌入）
    /// </summary>
    /// <param name="content">记忆内容</param>
    /// <param name="embedding">向量嵌入</param>
    /// <param name="type">记忆类型</param>
    /// <param name="importance">重要度</param>
    public void AddFact(string content, float[] embedding, string type = "fact", float importance = 0.5f)
    {
        var fact = new MemoryFact(content, embedding, type, importance);
        AddFact(fact);
    }

    /// <summary>
    /// 批量添加记忆事实
    /// </summary>
    public void AddFacts(List<MemoryFact> newFacts)
    {
        foreach (var fact in newFacts)
        {
            AddFact(fact);
        }
    }

    /// <summary>
    /// 获取所有记忆事实
    /// </summary>
    public List<MemoryFact> GetAllFacts()
    {
        return new List<MemoryFact>(facts);
    }

    /// <summary>
    /// 获取特定类型的记忆
    /// </summary>
    public List<MemoryFact> GetFactsByType(string type)
    {
        return facts.Where(f => f.type == type).ToList();
    }

    /// <summary>
    /// 清空所有记忆
    /// </summary>
    public void ClearAllFacts()
    {
        int count = facts.Count;
        facts.Clear();
        Debug.Log($"[LongTermMemory] NPC({npcId}) 已清除{count}条长期记忆");
    }

    /// <summary>
    /// 删除特定记忆
    /// </summary>
    public bool RemoveFact(string factId)
    {
        int removed = facts.RemoveAll(f => f.id == factId);
        if (removed > 0)
        {
            Debug.Log($"[LongTermMemory] NPC({npcId}) 删除记忆: {factId}");
            return true;
        }
        return false;
    }

    /// <summary>
    /// 清理低重要度的记忆
    /// </summary>
    /// <param name="minImportance">最小重要度阈值</param>
    public void CleanLowImportanceMemories(float minImportance = 0.3f)
    {
        int beforeCount = facts.Count;
        facts.RemoveAll(f => f.importance < minImportance);
        int removed = beforeCount - facts.Count;
        
        if (removed > 0)
        {
            Debug.Log($"[LongTermMemory] NPC({npcId}) 清理了{removed}条低重要度记忆");
        }
    }

    /// <summary>
    /// 获取记忆总数
    /// </summary>
    public int FactCount => facts.Count;

    /// <summary>
    /// 获取记忆摘要（用于调试）
    /// </summary>
    public string GetMemorySummary()
    {
        if (facts.Count == 0)
        {
            return "暂无长期记忆";
        }

        var grouped = facts.GroupBy(f => f.type);
        StringBuilder summary = new StringBuilder();
        summary.AppendLine($"长期记忆总数: {facts.Count}");
        
        foreach (var group in grouped)
        {
            summary.AppendLine($"  - {group.Key}: {group.Count()}条");
        }

        return summary.ToString();
    }
}

