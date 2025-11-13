using System;
using UnityEngine;

/// <summary>
/// NPC思考结果
/// 存储NPC的内心想法和行为指导
/// </summary>
[Serializable]
public class NPCThought
{
    /// <summary>
    /// NPC ID
    /// </summary>
    public string npcId;

    /// <summary>
    /// 内心想法（主观感受、分析）- 会插入到 UserMessage 中
    /// </summary>
    public string innerThought;

    /// <summary>
    /// 行为指导提示词 - 会插入到 SystemPrompt 中
    /// </summary>
    public string behaviorGuidance;

    /// <summary>
    /// 思考生成时间
    /// </summary>
    public DateTime createdTime;

    /// <summary>
    /// 思考触发时的对话计数
    /// </summary>
    public int triggerMessageCount;

    /// <summary>
    /// 思考有效期（持续多少次对话）
    /// </summary>
    public int lifetime;

    /// <summary>
    /// 当前已使用次数
    /// </summary>
    public int usageCount;

    /// <summary>
    /// 是否仍然有效
    /// </summary>
    public bool IsValid => usageCount < lifetime;

    public NPCThought(string npcId, string innerThought, string behaviorGuidance, int triggerMessageCount, int lifetime)
    {
        this.npcId = npcId;
        this.innerThought = innerThought;
        this.behaviorGuidance = behaviorGuidance;
        this.createdTime = DateTime.Now;
        this.triggerMessageCount = triggerMessageCount;
        this.lifetime = lifetime;
        this.usageCount = 0;
    }

    /// <summary>
    /// 使用思考（增加使用计数）
    /// </summary>
    public void Use()
    {
        usageCount++;
    }

    /// <summary>
    /// 重置使用计数
    /// </summary>
    public void Reset()
    {
        usageCount = 0;
    }

    public override string ToString()
    {
        return $"[NPCThought] {npcId} - 使用 {usageCount}/{lifetime}\n" +
               $"想法: {innerThought?.Substring(0, Math.Min(50, innerThought?.Length ?? 0))}...\n" +
               $"指导: {behaviorGuidance?.Substring(0, Math.Min(50, behaviorGuidance?.Length ?? 0))}...";
    }
}

