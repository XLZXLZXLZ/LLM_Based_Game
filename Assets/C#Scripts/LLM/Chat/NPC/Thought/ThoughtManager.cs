using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 思考管理器 - 负责NPC的思考流程
/// </summary>
public class ThoughtManager : Singleton<ThoughtManager>
{
    [Header("思考配置")]
    [SerializeField]
    [Tooltip("思考间隔（每N次对话触发一次思考，0表示仅首次）")]
    private int thoughtInterval = 5;

    [SerializeField]
    [Tooltip("思考结果有效期（持续多少次对话）")]
    private int thoughtLifetime = 5;

    [Header("调试选项")]
    [SerializeField]
    [Tooltip("是否在控制台打印思考内容")]
    private bool logThoughtContent = true;

    /// <summary>
    /// 检查是否需要触发思考
    /// </summary>
    /// <param name="npcId">NPC ID</param>
    /// <returns>是否需要思考</returns>
    public bool ShouldThink(string npcId)
    {
        if (string.IsNullOrEmpty(npcId))
        {
            return false;
        }

        // 从 ConversationManager 获取当前对话计数
        int currentCount = ConversationManager.Instance.GetMessageCountForThought(npcId);

        // 如果是第一次对话，需要思考
        if (currentCount == 0)
        {
            return true;
        }

        // 从 ConversationManager 获取思考结果
        var currentThought = ConversationManager.Instance.GetThought(npcId);

        // 如果没有有效的思考结果，需要思考
        if (currentThought == null || !currentThought.IsValid)
        {
            return true;
        }

        // 如果达到思考间隔，需要思考
        if (thoughtInterval > 0)
        {
            int lastThinkCount = currentThought.triggerMessageCount;
            if (currentCount - lastThinkCount >= thoughtInterval)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 执行思考流程
    /// </summary>
    /// <param name="npcProfile">NPC配置</param>
    /// <param name="systemPrompt">当前的系统提示词</param>
    /// <param name="userMessage">用户消息</param>
    /// <param name="conversationHistory">对话历史</param>
    /// <param name="onComplete">完成回调</param>
    public void PerformThink(
        NPCProfile npcProfile,
        string systemPrompt,
        string userMessage,
        List<LLMManager.Message> conversationHistory,
        Action<NPCThought> onComplete)
    {
        StartCoroutine(PerformThinkCoroutine(npcProfile, systemPrompt, userMessage, conversationHistory, onComplete));
    }

    private IEnumerator PerformThinkCoroutine(
        NPCProfile npcProfile,
        string systemPrompt,
        string userMessage,
        List<LLMManager.Message> conversationHistory,
        Action<NPCThought> onComplete)
    {
        Debug.Log($"[ThoughtManager] NPC({npcProfile.characterName})开始思考...");

        // 构建思考提示词
        string thinkPrompt = BuildThinkPrompt(npcProfile, systemPrompt, userMessage, conversationHistory);

        bool thinkCompleted = false;
        string thinkResult = string.Empty;

        // 调用LLM进行思考
        LLMManager.Instance.SendMessage(
            userMessage: thinkPrompt,
            onSuccess: response =>
            {
                thinkResult = response;
                thinkCompleted = true;
            },
            onError: error =>
            {
                Debug.LogError($"[ThoughtManager] 思考失败: {error}");
                thinkCompleted = true;
            },
            systemPrompt: "你是一个角色扮演助手，擅长分析角色的内心想法和制定行为准则。",
            profile: npcProfile.llmProfile
        );

        // 等待思考完成
        while (!thinkCompleted)
        {
            yield return null;
        }

        // 解析思考结果
        if (!string.IsNullOrEmpty(thinkResult))
        {
            // 检查是否选择保持不变
            if (thinkResult.Contains("<<<保持不变>>>"))
            {
                var previousThought = ConversationManager.Instance.GetThought(npcProfile.npcId);
                if (previousThought != null)
                {
                    // 重置思考的有效期和使用计数，但保持内容不变
                    int currentCount = ConversationManager.Instance.GetMessageCountForThought(npcProfile.npcId);
                    var refreshedThought = new NPCThought(
                        npcProfile.npcId,
                        previousThought.innerThought,
                        previousThought.behaviorGuidance,
                        currentCount,
                        thoughtLifetime
                    );

                    ConversationManager.Instance.SetThought(npcProfile.npcId, refreshedThought);

                    if (logThoughtContent)
                    {
                        Debug.Log($"[ThoughtManager] NPC({npcProfile.characterName})决定保持之前的思考不变");
                    }

                    onComplete?.Invoke(refreshedThought);
                }
                else
                {
                    Debug.LogWarning($"[ThoughtManager] 选择保持不变但没有找到之前的思考");
                    onComplete?.Invoke(null);
                }
            }
            else
            {
                // 解析新的思考结果
                (string innerThought, string behaviorGuidance) = ParseThinkResult(thinkResult);

                if (!string.IsNullOrEmpty(innerThought) && !string.IsNullOrEmpty(behaviorGuidance))
                {
                    // 创建新的思考结果
                    int currentCount = ConversationManager.Instance.GetMessageCountForThought(npcProfile.npcId);
                    var thought = new NPCThought(
                        npcProfile.npcId,
                        innerThought,
                        behaviorGuidance,
                        currentCount,
                        thoughtLifetime
                    );

                    // 保存思考结果到 ConversationManager
                    ConversationManager.Instance.SetThought(npcProfile.npcId, thought);

                    if (logThoughtContent)
                    {
                        Debug.Log($"[ThoughtManager] NPC({npcProfile.characterName})更新了思考:\n" +
                                  $"【内心想法】\n{innerThought}\n" +
                                  $"【行为指导】\n{behaviorGuidance}");
                    }

                    onComplete?.Invoke(thought);
                }
                else
                {
                    Debug.LogWarning($"[ThoughtManager] 思考结果解析失败");
                    onComplete?.Invoke(null);
                }
            }
        }
        else
        {
            Debug.LogWarning($"[ThoughtManager] 思考结果为空");
            onComplete?.Invoke(null);
        }
    }

    /// <summary>
    /// 构建思考提示词
    /// </summary>
    private string BuildThinkPrompt(
        NPCProfile npcProfile,
        string systemPrompt,
        string userMessage,
        List<LLMManager.Message> conversationHistory)
    {
        StringBuilder promptBuilder = new StringBuilder();

        promptBuilder.AppendLine("请以第一人称角色视角进行深度思考和分析。");
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("【角色设定】");
        promptBuilder.AppendLine(systemPrompt);
        promptBuilder.AppendLine();

        // 获取上次的思考结果
        var previousThought = ConversationManager.Instance.GetThought(npcProfile.npcId);
        if (previousThought != null)
        {
            promptBuilder.AppendLine("【你之前的思考】");
            promptBuilder.AppendLine($"内心想法: {previousThought.innerThought}");
            promptBuilder.AppendLine($"行为指导: {previousThought.behaviorGuidance}");
            promptBuilder.AppendLine();
        }

        // 如果有对话历史，提供上下文
        if (conversationHistory != null && conversationHistory.Count > 0)
        {
            promptBuilder.AppendLine("【最近的对话】");
            int recentCount = Math.Min(5, conversationHistory.Count);
            for (int i = conversationHistory.Count - recentCount; i < conversationHistory.Count; i++)
            {
                var msg = conversationHistory[i];
                string roleName = msg.role == "user" ? "玩家" : npcProfile.characterName;
                promptBuilder.AppendLine($"{roleName}: {msg.content}");
            }
            promptBuilder.AppendLine();
        }

        promptBuilder.AppendLine("【当前情况】");
        promptBuilder.AppendLine($"玩家说: {userMessage}");
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("【思考任务】");
        if (previousThought != null)
        {
            promptBuilder.AppendLine("首先，判断你之前的思考是否仍然适用：");
            promptBuilder.AppendLine("- 对话情境是否发生了重大变化？");
            promptBuilder.AppendLine("- 你对玩家的看法是否需要调整？");
            promptBuilder.AppendLine("- 之前的行为策略是否还合适？");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("如果之前的思考仍然适用，可以选择保持不变。");
            promptBuilder.AppendLine("如果需要更新，请提供新的思考。");
            promptBuilder.AppendLine();
        }
        
        promptBuilder.AppendLine("请分两个部分进行深度思考：");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("1. 【内心想法】（第一人称）");
        promptBuilder.AppendLine("   - 我对当前情况的真实感受和想法是什么？");
        promptBuilder.AppendLine("   - 我对玩家的态度和印象如何？");
        promptBuilder.AppendLine("   - 控制在100字以内，简洁表达核心想法");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("2. 【行为指导】（第三人称）");
        promptBuilder.AppendLine("   - 接下来几轮对话中，角色计划以怎样的语气、态度、回复长度来回应玩家？");
        promptBuilder.AppendLine("   - 角色应当积极探讨，简单地应付，还是尝试结束话题？");
        promptBuilder.AppendLine("   - 角色每次对话应当大约多长？请注意，所有对话都应保持和用户你来我往的短促风格，不能过长或唠叨。");
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("【输出格式】");
        promptBuilder.AppendLine("严格按照以下格式输出（包括标记）：");
        promptBuilder.AppendLine();
        
        if (previousThought != null)
        {
            promptBuilder.AppendLine("如果决定保持之前的思考不变：");
            promptBuilder.AppendLine("<<<保持不变>>>");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("如果需要更新思考：");
        }
        
        promptBuilder.AppendLine("<<<内心想法>>>");
        promptBuilder.AppendLine("（在这里写内心想法，第一人称）");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("<<<行为指导>>>");
        promptBuilder.AppendLine("（在这里写行为指导，第三人称）");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("注意：必须包含完整的两个部分，且使用指定的标记分隔。");

        return promptBuilder.ToString();
    }

    /// <summary>
    /// 解析思考结果
    /// </summary>
    /// <param name="thinkResult">LLM返回的思考内容</param>
    /// <returns>(内心想法, 行为指导)</returns>
    private (string innerThought, string behaviorGuidance) ParseThinkResult(string thinkResult)
    {
        string innerThought = string.Empty;
        string behaviorGuidance = string.Empty;

        try
        {
            // 使用标记分割
            string[] sections = thinkResult.Split(new[] { "<<<内心想法>>>", "<<<行为指导>>>" }, StringSplitOptions.RemoveEmptyEntries);

            if (sections.Length >= 2)
            {
                innerThought = sections[0].Trim();
                behaviorGuidance = sections[1].Trim();
            }
            else
            {
                // 尝试备用解析方式
                var thoughtMatch = System.Text.RegularExpressions.Regex.Match(
                    thinkResult,
                    @"<<<内心想法>>>\s*(.+?)\s*<<<行为指导>>>\s*(.+)",
                    System.Text.RegularExpressions.RegexOptions.Singleline
                );

                if (thoughtMatch.Success)
                {
                    innerThought = thoughtMatch.Groups[1].Value.Trim();
                    behaviorGuidance = thoughtMatch.Groups[2].Value.Trim();
                }
                else
                {
                    Debug.LogWarning($"[ThoughtManager] 无法解析思考结果，尝试使用整段作为想法");
                    innerThought = thinkResult.Trim();
                    behaviorGuidance = "保持角色一致性，自然地回应玩家。";
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[ThoughtManager] 解析思考结果时出错: {e.Message}");
        }

        return (innerThought, behaviorGuidance);
    }

    /// <summary>
    /// 获取NPC当前的思考结果
    /// </summary>
    /// <param name="npcId">NPC ID</param>
    /// <returns>思考结果，如果没有则返回null</returns>
    public NPCThought GetThought(string npcId)
    {
        var thought = ConversationManager.Instance.GetThought(npcId);
        if (thought != null && thought.IsValid)
        {
            return thought;
        }
        return null;
    }

    /// <summary>
    /// 记录一次对话（用于计数）
    /// </summary>
    /// <param name="npcId">NPC ID</param>
    public void RecordMessage(string npcId)
    {
        if (string.IsNullOrEmpty(npcId))
        {
            return;
        }

        // 记录对话计数到 ConversationManager
        ConversationManager.Instance.RecordMessageForThought(npcId);

        // 如果有有效的思考，增加使用计数
        var thought = ConversationManager.Instance.GetThought(npcId);
        if (thought != null && thought.IsValid)
        {
            thought.Use();
            // 更新回 ConversationManager
            ConversationManager.Instance.SetThought(npcId, thought);
        }
    }

    /// <summary>
    /// 清除NPC的思考数据
    /// </summary>
    /// <param name="npcId">NPC ID</param>
    public void ClearThought(string npcId)
    {
        ConversationManager.Instance.ClearThought(npcId);
        Debug.Log($"[ThoughtManager] 已清除NPC({npcId})的思考数据");
    }

    /// <summary>
    /// 清除所有思考数据
    /// </summary>
    public void ClearAllThoughts()
    {
        ConversationManager.Instance.ClearAllThoughts();
        Debug.Log("[ThoughtManager] 已清除所有思考数据");
    }

    /// <summary>
    /// 思考间隔
    /// </summary>
    public int ThoughtInterval
    {
        get => thoughtInterval;
        set => thoughtInterval = Mathf.Max(0, value);
    }

    /// <summary>
    /// 思考有效期
    /// </summary>
    public int ThoughtLifetime
    {
        get => thoughtLifetime;
        set => thoughtLifetime = Mathf.Max(1, value);
    }

    /// <summary>
    /// 是否记录思考内容
    /// </summary>
    public bool LogThoughtContent
    {
        get => logThoughtContent;
        set => logThoughtContent = value;
    }
}

