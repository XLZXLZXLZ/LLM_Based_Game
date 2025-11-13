using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 对话系统测试器 - 可在 Inspector 中直接测试
/// </summary>
public class ChatSystemTester : MonoBehaviour
{
    [Header("NPC配置")]
    [SerializeField]
    [Tooltip("要测试的NPC配置")]
    private NPCProfile npcProfile;

    [Header("对话测试")]
    [SerializeField]
    [TextArea(2, 5)]
    [Tooltip("输入要发送的消息")]
    private string userInput = "";

    [Header("显示区域")]
    [SerializeField]
    [TextArea(10, 20)]
    [Tooltip("对话历史")]
    private string conversationDisplay = "";

    [SerializeField]
    [TextArea(5, 10)]
    [Tooltip("当前思考内容")]
    private string thoughtDisplay = "";

    [Header("记忆信息")]
    [SerializeField]
    [TextArea(3, 8)]
    [Tooltip("短期记忆总结")]
    private string shortTermMemoryDisplay = "";

    [SerializeField]
    [TextArea(5, 10)]
    [Tooltip("长期记忆列表")]
    private string longTermMemoryDisplay = "";

    [Header("状态信息")]
    [SerializeField]
    private string statusMessage = "准备就绪";

    [SerializeField]
    private int messageCount = 0;

    [SerializeField]
    private int longTermMemoryCount = 0;

    private bool isProcessing = false;

    private void OnValidate()
    {
        // 每次修改时更新显示
        if (npcProfile != null && !isProcessing)
        {
            UpdateDisplays();
        }
    }

    /// <summary>
    /// 发送消息（在 Inspector 中调用）
    /// </summary>
    [ContextMenu("发送消息")]
    public void SendMessage()
    {
        if (isProcessing)
        {
            statusMessage = "正在处理中，请稍候...";
            return;
        }

        if (npcProfile == null)
        {
            statusMessage = "错误: 请先设置 NPCProfile！";
            Debug.LogError("[ChatSystemTester] NPCProfile 未设置！");
            return;
        }

        if (string.IsNullOrEmpty(userInput))
        {
            statusMessage = "错误: 请输入消息内容！";
            Debug.LogWarning("[ChatSystemTester] 用户输入为空！");
            return;
        }

        isProcessing = true;
        statusMessage = "正在发送消息...";

        string messageToSend = userInput;
        userInput = ""; // 清空输入框

        // 添加到对话显示
        conversationDisplay += $"\n【玩家】: {messageToSend}\n";

        Debug.Log($"[ChatSystemTester] 发送消息: {messageToSend}");

        // 调用 ChatAgent
        ChatAgent.Instance.SendMessage(
            npcProfile: npcProfile,
            userMessage: messageToSend,
            onSuccess: response =>
            {
                // 添加NPC回复
                conversationDisplay += $"【{npcProfile.characterName}】: {response}\n";
                conversationDisplay += "----------------------------------------\n";

                statusMessage = "消息发送成功！";
                isProcessing = false;

                // 更新显示
                UpdateDisplays();

                Debug.Log($"[ChatSystemTester] 收到回复: {response}");
            },
            onError: error =>
            {
                conversationDisplay += $"【错误】: {error}\n";
                conversationDisplay += "----------------------------------------\n";

                statusMessage = $"错误: {error}";
                isProcessing = false;

                Debug.LogError($"[ChatSystemTester] 发送失败: {error}");
            }
        );
    }

    /// <summary>
    /// 清除对话历史
    /// </summary>
    [ContextMenu("清除对话历史")]
    public void ClearConversationHistory()
    {
        if (npcProfile == null)
        {
            statusMessage = "错误: 请先设置 NPCProfile！";
            return;
        }

        ChatAgent.Instance.ClearHistory(npcProfile);
        conversationDisplay = "对话历史已清除。\n";
        statusMessage = "对话历史已清除";

        UpdateDisplays();

        Debug.Log("[ChatSystemTester] 对话历史已清除");
    }

    /// <summary>
    /// 清除所有记忆（包括思考）
    /// </summary>
    [ContextMenu("清除所有记忆")]
    public void ClearAllMemory()
    {
        if (npcProfile == null)
        {
            statusMessage = "错误: 请先设置 NPCProfile！";
            return;
        }

        ChatAgent.Instance.ClearAllMemory(npcProfile);
        conversationDisplay = "所有记忆已清除。\n";
        shortTermMemoryDisplay = "";
        longTermMemoryDisplay = "";
        thoughtDisplay = "";
        statusMessage = "所有记忆已清除";

        UpdateDisplays();

        Debug.Log("[ChatSystemTester] 所有记忆已清除");
    }

    /// <summary>
    /// 强制触发记忆转化
    /// </summary>
    [ContextMenu("强制记忆转化")]
    public void ForceMemoryExtraction()
    {
        if (npcProfile == null)
        {
            statusMessage = "错误: 请先设置 NPCProfile！";
            return;
        }

        if (isProcessing)
        {
            statusMessage = "正在处理中，请稍候...";
            return;
        }

        // 获取对话历史
        var history = ConversationManager.Instance.GetConversationHistory(npcProfile.npcId);

        if (history.Count == 0)
        {
            statusMessage = "没有对话历史可供提取";
            Debug.LogWarning("[ChatSystemTester] 对话历史为空");
            return;
        }

        isProcessing = true;
        statusMessage = "正在提取记忆...";

        Debug.Log("[ChatSystemTester] 开始强制记忆提取...");

        // 调用 MemoryExtractor
        MemoryExtractor.Instance.ExtractMemories(
            npcProfile: npcProfile,
            messages: history,
            onComplete: memoryFacts =>
            {
                if (memoryFacts != null && memoryFacts.Count > 0)
                {
                    // 添加到长期记忆
                    ConversationManager.Instance.AddMemoryFacts(npcProfile.npcId, memoryFacts);
                    
                    statusMessage = $"成功提取 {memoryFacts.Count} 条长期记忆！";
                    Debug.Log($"[ChatSystemTester] 提取了 {memoryFacts.Count} 条记忆");
                }
                else
                {
                    statusMessage = "未提取到需要记忆的内容";
                    Debug.Log("[ChatSystemTester] 未提取到记忆");
                }

                isProcessing = false;
                UpdateDisplays();
            }
        );
    }

    /// <summary>
    /// 强制触发思考
    /// </summary>
    [ContextMenu("强制思考")]
    public void ForceThinking()
    {
        if (npcProfile == null)
        {
            statusMessage = "错误: 请先设置 NPCProfile！";
            return;
        }

        // 清除当前思考，下次对话会自动触发
        ChatAgent.Instance.ClearThought(npcProfile);
        thoughtDisplay = "思考已清除，下次对话将重新思考。";
        statusMessage = "已清除思考，等待重新思考";

        Debug.Log("[ChatSystemTester] 已清除思考");
    }

    /// <summary>
    /// 刷新显示
    /// </summary>
    [ContextMenu("刷新显示")]
    public void UpdateDisplays()
    {
        if (npcProfile == null || string.IsNullOrEmpty(npcProfile.npcId))
        {
            return;
        }

        // 更新思考显示
        UpdateThoughtDisplay();

        // 更新短期记忆显示
        UpdateShortTermMemoryDisplay();

        // 更新长期记忆显示
        UpdateLongTermMemoryDisplay();

        // 更新统计信息
        messageCount = ConversationManager.Instance.GetMessageCount(npcProfile.npcId);
        longTermMemoryCount = ConversationManager.Instance.GetLongTermMemoryCount(npcProfile.npcId);
    }

    /// <summary>
    /// 更新思考显示
    /// </summary>
    private void UpdateThoughtDisplay()
    {
        var thought = ConversationManager.Instance.GetThought(npcProfile.npcId);

        if (thought != null && thought.IsValid)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== 当前思考 ===");
            sb.AppendLine();
            sb.AppendLine("【内心想法】");
            sb.AppendLine(thought.innerThought);
            sb.AppendLine();
            sb.AppendLine("【行为指导】");
            sb.AppendLine(thought.behaviorGuidance);
            sb.AppendLine();
            sb.AppendLine($"有效期: {thought.usageCount}/{thought.lifetime}");
            sb.AppendLine($"创建时间: {thought.createdTime:yyyy-MM-dd HH:mm:ss}");

            thoughtDisplay = sb.ToString();
        }
        else
        {
            thoughtDisplay = "暂无思考数据";
        }
    }

    /// <summary>
    /// 更新短期记忆显示
    /// </summary>
    private void UpdateShortTermMemoryDisplay()
    {
        string shortTermMemory = ConversationManager.Instance.GetShortTermMemory(npcProfile.npcId);

        if (!string.IsNullOrEmpty(shortTermMemory))
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== 短期记忆（对话总结） ===");
            sb.AppendLine();
            sb.AppendLine(shortTermMemory);

            shortTermMemoryDisplay = sb.ToString();
        }
        else
        {
            shortTermMemoryDisplay = "暂无短期记忆";
        }
    }

    /// <summary>
    /// 更新长期记忆显示
    /// </summary>
    private void UpdateLongTermMemoryDisplay()
    {
        var facts = ConversationManager.Instance.GetAllMemoryFacts(npcProfile.npcId);

        if (facts.Count > 0)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"=== 长期记忆（共 {facts.Count} 条） ===");
            sb.AppendLine();

            // 按重要度排序
            facts.Sort((a, b) => b.importance.CompareTo(a.importance));

            for (int i = 0; i < facts.Count; i++)
            {
                var fact = facts[i];
                sb.AppendLine($"{i + 1}. [{fact.type}|{fact.importance:F2}] {fact.content}");
                sb.AppendLine($"   时间: {fact.createdTime:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine();
            }

            longTermMemoryDisplay = sb.ToString();
        }
        else
        {
            longTermMemoryDisplay = "暂无长期记忆";
        }
    }

    /// <summary>
    /// 显示对话历史
    /// </summary>
    [ContextMenu("显示对话历史")]
    public void ShowConversationHistory()
    {
        if (npcProfile == null)
        {
            statusMessage = "错误: 请先设置 NPCProfile！";
            return;
        }

        var history = ConversationManager.Instance.GetConversationHistory(npcProfile.npcId);

        if (history.Count == 0)
        {
            conversationDisplay = "暂无对话历史\n";
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== 对话历史 ===\n");

        foreach (var message in history)
        {
            string roleName = message.role == "user" ? "玩家" : npcProfile.characterName;
            sb.AppendLine($"【{roleName}】: {message.content}");
            sb.AppendLine();
        }

        conversationDisplay = sb.ToString();
        statusMessage = $"显示了 {history.Count} 条对话记录";
    }

    /// <summary>
    /// 导出完整状态
    /// </summary>
    [ContextMenu("导出完整状态")]
    public void ExportFullState()
    {
        if (npcProfile == null)
        {
            statusMessage = "错误: 请先设置 NPCProfile！";
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("==================== 完整状态导出 ====================");
        sb.AppendLine();
        sb.AppendLine($"NPC: {npcProfile.characterName} (ID: {npcProfile.npcId})");
        sb.AppendLine($"对话次数: {messageCount}");
        sb.AppendLine($"长期记忆数量: {longTermMemoryCount}");
        sb.AppendLine();

        // 思考
        sb.AppendLine(thoughtDisplay);
        sb.AppendLine();

        // 短期记忆
        sb.AppendLine(shortTermMemoryDisplay);
        sb.AppendLine();

        // 长期记忆
        sb.AppendLine(longTermMemoryDisplay);
        sb.AppendLine();

        // 对话历史
        var history = ConversationManager.Instance.GetConversationHistory(npcProfile.npcId);
        sb.AppendLine($"=== 对话历史（{history.Count} 条） ===");
        foreach (var message in history)
        {
            string roleName = message.role == "user" ? "玩家" : npcProfile.characterName;
            sb.AppendLine($"【{roleName}】: {message.content}");
        }

        string fullState = sb.ToString();
        Debug.Log(fullState);

        statusMessage = "完整状态已导出到控制台";
    }
}




