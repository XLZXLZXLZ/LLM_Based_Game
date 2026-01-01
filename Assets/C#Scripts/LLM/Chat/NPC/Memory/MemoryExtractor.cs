using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 记忆提取器 - 负责从对话中提取需要长期记忆的关键信息
/// </summary>
public class MemoryExtractor : Singleton<MemoryExtractor>
{
    /// <summary>
    /// 从对话中提取长期记忆
    /// </summary>
    /// <param name="npcProfile">NPC配置</param>
    /// <param name="messages">对话消息列表</param>
    /// <param name="onComplete">完成回调，返回提取的记忆事实列表</param>
    public void ExtractMemories(NPCProfile npcProfile, List<LLMManager.Message> messages, Action<List<MemoryFact>> onComplete)
    {
        StartCoroutine(ExtractMemoriesCoroutine(npcProfile, messages, onComplete));
    }

    private IEnumerator ExtractMemoriesCoroutine(NPCProfile npcProfile, List<LLMManager.Message> messages, Action<List<MemoryFact>> onComplete)
    {
        // 构建提取提示词
        string extractPrompt = BuildExtractionPrompt(npcProfile, messages);

        Debug.Log("[MemoryExtractor] 开始提取长期记忆...");

        bool extractCompleted = false;
        string extractedFacts = string.Empty;

        // 调用LLM提取关键信息
        LLMManager.Instance.SendMessage(
            userMessage: extractPrompt,
            onResponse: (content, toolCalls) =>
            {
                extractedFacts = content;
                extractCompleted = true;
                Debug.Log($"[MemoryExtractor] 提取完成: {content}");
            },
            onError: error =>
            {
                Debug.LogError($"[MemoryExtractor] 提取失败: {error}");
                extractCompleted = true;
            },
            systemPrompt: "你是一个专业的记忆分析助手，擅长从对话中提取需要长期记住的关键信息。",
            profile: npcProfile.llmProfile
        );

        // 等待提取完成
        while (!extractCompleted)
        {
            yield return null;
        }

        // 如果没有提取到内容，直接返回空列表
        if (string.IsNullOrEmpty(extractedFacts) || extractedFacts.Contains("无") || extractedFacts.Contains("没有"))
        {
            Debug.Log("[MemoryExtractor] 本次对话无需记忆的关键信息");
            onComplete?.Invoke(new List<MemoryFact>());
            yield break;
        }

        // 解析提取的事实（按行分割）
        string[] factLines = extractedFacts.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var memoryFacts = new List<MemoryFact>();

        // 为每条事实生成向量嵌入
        foreach (var line in factLines)
        {
            string rawLine = line.Trim();
            
            // 跳过空行和特殊标记
            if (string.IsNullOrEmpty(rawLine) || rawLine.Length < 5 || rawLine.StartsWith("#"))
            {
                continue;
            }

            // 解析格式：[类型|重要度] 内容
            (string type, float importance, string content) = ParseMemoryLine(rawLine);
            
            // 如果解析失败，跳过这条
            if (string.IsNullOrEmpty(content))
            {
                Debug.LogWarning($"[MemoryExtractor] 无法解析记忆格式: {rawLine}");
                continue;
            }

            bool embeddingCompleted = false;
            float[] embedding = null;

            // 获取向量嵌入
            LLMManager.Instance.GetEmbedding(
                text: content,
                onSuccess: emb =>
                {
                    embedding = emb;
                    embeddingCompleted = true;
                },
                onError: error =>
                {
                    Debug.LogError($"[MemoryExtractor] 向量嵌入失败: {error}");
                    embeddingCompleted = true;
                }
            );

            // 等待嵌入完成
            while (!embeddingCompleted)
            {
                yield return null;
            }

            if (embedding != null)
            {
                var memoryFact = new MemoryFact(content, embedding, type, importance);
                memoryFacts.Add(memoryFact);
                Debug.Log($"[MemoryExtractor] 提取记忆: [{type}|{importance:F2}] {content}");
            }
        }

        Debug.Log($"[MemoryExtractor] 共提取{memoryFacts.Count}条长期记忆");
        onComplete?.Invoke(memoryFacts);
    }

    /// <summary>
    /// 构建提取提示词
    /// </summary>
    private string BuildExtractionPrompt(NPCProfile npcProfile, List<LLMManager.Message> messages)
    {
        StringBuilder promptBuilder = new StringBuilder();

        promptBuilder.AppendLine("请从以下对话中提取需要长期记住的关键信息。");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("信息类型说明：");
        promptBuilder.AppendLine("- promise: 承诺或约定");
        promptBuilder.AppendLine("- preference: 喜好、厌恶、偏好");
        promptBuilder.AppendLine("- relationship: 角色之间的关系变化");
        promptBuilder.AppendLine("- fact: 重要的事实信息、决定");
        promptBuilder.AppendLine("- detail: 人类会记住的小细节（如名字、特征等）");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("输出格式要求：");
        promptBuilder.AppendLine("每行一条信息，格式为：[类型|重要度] 内容");
        promptBuilder.AppendLine("- 类型：从上述5种中选择最合适的");
        promptBuilder.AppendLine("- 重要度：0.0-1.0之间的数字，越重要数值越高");
        promptBuilder.AppendLine("- 内容：用一句完整的陈述句，第三人称客观描述");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("示例：");
        promptBuilder.AppendLine("[promise|0.9] 玩家答应帮助艾莉娅寻找失踪的妹妹");
        promptBuilder.AppendLine("[preference|0.7] 玩家曾说喜欢在月光下散步");
        promptBuilder.AppendLine("[relationship|0.8] 玩家与奥伦彻底敌对");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("注意：");
        promptBuilder.AppendLine("- 只提取真正重要的、需要长期记住的信息");
        promptBuilder.AppendLine("- 日常寒暄、普通对话不需要提取");
        promptBuilder.AppendLine("- 如果没有值得记忆的信息，回复\"无\"");
        promptBuilder.AppendLine();

        promptBuilder.AppendLine($"【角色信息】");
        promptBuilder.AppendLine($"角色名称：{npcProfile.characterName}");
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("【对话内容】");
        foreach (var message in messages)
        {
            string roleName = message.role == "user" ? "玩家" : npcProfile.characterName;
            promptBuilder.AppendLine($"{roleName}: {message.content}");
        }
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("请提取需要长期记住的关键信息（每行一条）：");

        return promptBuilder.ToString();
    }

    /// <summary>
    /// 解析记忆行格式：[类型|重要度] 内容
    /// </summary>
    /// <returns>(类型, 重要度, 内容)</returns>
    private (string type, float importance, string content) ParseMemoryLine(string line)
    {
        // 尝试匹配格式：[类型|重要度] 内容
        var match = System.Text.RegularExpressions.Regex.Match(line, @"^\[(\w+)\|([\d\.]+)\]\s*(.+)$");
        
        if (match.Success)
        {
            string type = match.Groups[1].Value.ToLower();
            float importance = 0.6f;
            float.TryParse(match.Groups[2].Value, out importance);
            
            // 确保重要度在0-1之间
            importance = Mathf.Clamp01(importance);
            
            string content = match.Groups[3].Value.Trim();
            
            // 验证类型是否有效
            if (!IsValidType(type))
            {
                Debug.LogWarning($"[MemoryExtractor] 未知类型'{type}'，使用默认'fact'");
                type = "fact";
            }
            
            return (type, importance, content);
        }
        
        // 如果格式不匹配，尝试兼容旧格式（纯文本）
        // 移除可能的序号前缀（如"1. "）
        string cleanedLine = System.Text.RegularExpressions.Regex.Replace(line, @"^\d+[\.\、]\s*", "");
        
        if (!string.IsNullOrEmpty(cleanedLine))
        {
            // 使用默认值作为fallback
            return ("fact", 0.5f, cleanedLine);
        }
        
        return (null, 0f, null);
    }

    /// <summary>
    /// 验证类型是否有效
    /// </summary>
    private bool IsValidType(string type)
    {
        return type == "promise" || type == "preference" || type == "relationship" || 
               type == "fact" || type == "detail";
    }
}

