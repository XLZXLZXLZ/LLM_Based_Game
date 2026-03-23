using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 角色对话测试窗口 - 在编辑器中直接与NPC进行对话测试
/// </summary>
public class CharacterChatTestWindow : EditorWindow
{
    [MenuItem("Tools/角色对话测试")]
    public static void ShowWindow()
    {
        var window = GetWindow<CharacterChatTestWindow>("角色对话测试");
        window.minSize = new Vector2(500, 600);
    }

    // 配置
    private NPCProfile npcProfile;
    private LLMProfile llmProfile;

    // 对话状态
    private string userInput = "";
    private List<ChatMessage> chatHistory = new List<ChatMessage>();
    private bool isWaitingResponse = false;
    private string statusMessage = "";

    // 滚动位置
    private Vector2 chatScrollPos;
    private Vector2 inputScrollPos;

    // 样式
    private GUIStyle userMessageStyle;
    private GUIStyle npcMessageStyle;
    private GUIStyle systemMessageStyle;
    private GUIStyle headerStyle;
    private bool stylesInitialized = false;

    /// <summary>
    /// 聊天消息结构
    /// </summary>
    private class ChatMessage
    {
        public string sender;
        public string content;
        public bool isUser;
        public bool isSystem;

        public ChatMessage(string sender, string content, bool isUser, bool isSystem = false)
        {
            this.sender = sender;
            this.content = content;
            this.isUser = isUser;
            this.isSystem = isSystem;
        }
    }

    private void OnEnable()
    {
        // 确保Play Mode时单例存在
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            // Play Mode进入时，确保单例初始化
            EnsureSingletons();
        }
    }

    /// <summary>
    /// 确保所需的单例存在
    /// </summary>
    private void EnsureSingletons()
    {
        if (!Application.isPlaying) return;

        // 检查LLMManager
        if (LLMManager.Instance == null)
        {
            var go = new GameObject("[LLMManager]");
            go.AddComponent<LLMManager>();
        }

        // 检查ChatAgent
        if (ChatAgent.Instance == null)
        {
            var go = new GameObject("[ChatAgent]");
            go.AddComponent<ChatAgent>();
        }

        // 检查ConversationManager
        if (ConversationManager.Instance == null)
        {
            var go = new GameObject("[ConversationManager]");
            go.AddComponent<ConversationManager>();
        }

        // 检查FunctionCallManager
        if (FunctionCallManager.Instance == null)
        {
            var go = new GameObject("[FunctionCallManager]");
            go.AddComponent<FunctionCallManager>();
        }

        // 设置默认LLMProfile
        if (llmProfile != null && LLMManager.Instance.DefaultProfile == null)
        {
            LLMManager.Instance.DefaultProfile = llmProfile;
        }
    }

    /// <summary>
    /// 初始化GUI样式
    /// </summary>
    private void InitializeStyles()
    {
        if (stylesInitialized) return;

        // 用户消息样式 - 蓝色背景
        userMessageStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 8, 8),
            margin = new RectOffset(50, 10, 5, 5),
            wordWrap = true,
            richText = true,
            fontSize = 12
        };

        // NPC消息样式 - 默认背景
        npcMessageStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 8, 8),
            margin = new RectOffset(10, 50, 5, 5),
            wordWrap = true,
            richText = true,
            fontSize = 12
        };

        // 系统消息样式 - 居中
        systemMessageStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
        {
            padding = new RectOffset(10, 10, 5, 5),
            margin = new RectOffset(10, 10, 10, 10),
            wordWrap = true,
            richText = true,
            fontSize = 11
        };

        // 标题样式
        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };

        stylesInitialized = true;
    }

    private void OnGUI()
    {
        InitializeStyles();

        // 标题
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("🎭 角色对话测试", headerStyle);
        EditorGUILayout.Space(10);

        // 配置区域
        DrawConfigSection();

        EditorGUILayout.Space(5);

        // 对话区域
        DrawChatSection();

        // 输入区域
        DrawInputSection();

        // 状态栏
        DrawStatusBar();
    }

    /// <summary>
    /// 绘制配置区域
    /// </summary>
    private void DrawConfigSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("配置", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        npcProfile = (NPCProfile)EditorGUILayout.ObjectField("NPC配置", npcProfile, typeof(NPCProfile), false);
        if (EditorGUI.EndChangeCheck() && npcProfile != null)
        {
            // NPC变更时，清空对话历史
            chatHistory.Clear();
            AddSystemMessage($"已加载角色: {npcProfile.characterName}");
        }

        llmProfile = (LLMProfile)EditorGUILayout.ObjectField("LLM配置", llmProfile, typeof(LLMProfile), false);

        // 如果NPC有LLMProfile，自动使用
        if (npcProfile != null && npcProfile.llmProfile != null && llmProfile == null)
        {
            llmProfile = npcProfile.llmProfile;
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制对话区域
    /// </summary>
    private void DrawChatSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true));

        EditorGUILayout.LabelField("对话记录", EditorStyles.boldLabel);

        // 对话滚动区域
        chatScrollPos = EditorGUILayout.BeginScrollView(chatScrollPos, GUILayout.ExpandHeight(true));

        if (chatHistory.Count == 0)
        {
            EditorGUILayout.LabelField("（暂无对话记录，请在下方输入消息开始对话）", systemMessageStyle);
        }
        else
        {
            foreach (var msg in chatHistory)
            {
                DrawChatMessage(msg);
            }
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制单条聊天消息
    /// </summary>
    private void DrawChatMessage(ChatMessage msg)
    {
        if (msg.isSystem)
        {
            EditorGUILayout.LabelField($"—— {msg.content} ——", systemMessageStyle);
        }
        else
        {
            GUIStyle style = msg.isUser ? userMessageStyle : npcMessageStyle;
            string prefix = msg.isUser ? "🧑 你" : $"🤖 {msg.sender}";

            EditorGUILayout.BeginVertical(style);
            EditorGUILayout.LabelField($"<b>{prefix}</b>", new GUIStyle { richText = true, fontStyle = FontStyle.Bold });
            EditorGUILayout.LabelField(msg.content, new GUIStyle(EditorStyles.wordWrappedLabel) { richText = true });
            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// 绘制输入区域
    /// </summary>
    private void DrawInputSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("输入", EditorStyles.boldLabel);

        // 多行输入框
        inputScrollPos = EditorGUILayout.BeginScrollView(inputScrollPos, GUILayout.Height(60));
        GUI.enabled = !isWaitingResponse;
        userInput = EditorGUILayout.TextArea(userInput, GUILayout.ExpandHeight(true));
        GUI.enabled = true;
        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginHorizontal();

        // 发送按钮
        GUI.enabled = !isWaitingResponse && !string.IsNullOrWhiteSpace(userInput) && npcProfile != null && Application.isPlaying;
        if (GUILayout.Button("发送 (Enter)", GUILayout.Height(30)))
        {
            SendMessage();
        }
        GUI.enabled = true;

        // 清空对话按钮
        if (GUILayout.Button("清空对话", GUILayout.Width(80), GUILayout.Height(30)))
        {
            ClearChat();
        }

        // 清空记忆按钮
        GUI.enabled = npcProfile != null && Application.isPlaying;
        if (GUILayout.Button("清空记忆", GUILayout.Width(80), GUILayout.Height(30)))
        {
            ClearMemory();
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        // 处理Enter键发送
        HandleKeyboardInput();
    }

    /// <summary>
    /// 绘制状态栏
    /// </summary>
    private void DrawStatusBar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        // 运行状态
        string playModeStatus = Application.isPlaying ? "✅ 运行中" : "⚠️ 请先运行游戏";
        EditorGUILayout.LabelField(playModeStatus, GUILayout.Width(100));

        // NPC状态
        string npcStatus = npcProfile != null ? $"NPC: {npcProfile.characterName}" : "未选择NPC";
        EditorGUILayout.LabelField(npcStatus, GUILayout.Width(150));

        // 状态消息
        if (!string.IsNullOrEmpty(statusMessage))
        {
            EditorGUILayout.LabelField(statusMessage);
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 处理键盘输入
    /// </summary>
    private void HandleKeyboardInput()
    {
        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return && !e.shift)
        {
            if (!isWaitingResponse && !string.IsNullOrWhiteSpace(userInput) && npcProfile != null && Application.isPlaying)
            {
                SendMessage();
                e.Use();
            }
        }
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    private void SendMessage()
    {
        if (!Application.isPlaying)
        {
            statusMessage = "请先运行游戏！";
            return;
        }

        EnsureSingletons();

        string message = userInput.Trim();
        userInput = "";

        // 添加用户消息到UI
        chatHistory.Add(new ChatMessage("你", message, true));
        ScrollToBottom();

        isWaitingResponse = true;
        statusMessage = "正在等待回复...";

        // 调用ChatAgent发送消息
        ChatAgent.Instance.SendMessage(
            npcProfile: npcProfile,
            userMessage: message,
            onSuccess: (response, toolCalls) =>
            {
                // 添加NPC回复到UI
                chatHistory.Add(new ChatMessage(npcProfile.characterName, response, false));
                ScrollToBottom();

                isWaitingResponse = false;
                statusMessage = "回复完成";
                Repaint();
            },
            onError: error =>
            {
                AddSystemMessage($"错误: {error}");
                isWaitingResponse = false;
                statusMessage = "请求失败";
                Repaint();
            }
        );

        Repaint();
    }

    /// <summary>
    /// 清空对话
    /// </summary>
    private void ClearChat()
    {
        chatHistory.Clear();
        AddSystemMessage("对话已清空");

        if (Application.isPlaying && npcProfile != null)
        {
            EnsureSingletons();
            ChatAgent.Instance.ClearHistory(npcProfile);
        }
    }

    /// <summary>
    /// 清空记忆
    /// </summary>
    private void ClearMemory()
    {
        if (!Application.isPlaying || npcProfile == null) return;

        EnsureSingletons();
        ChatAgent.Instance.ClearAllMemory(npcProfile);
        chatHistory.Clear();
        AddSystemMessage("所有记忆已清空（包括对话历史、短期记忆和长期记忆）");
    }

    /// <summary>
    /// 添加系统消息
    /// </summary>
    private void AddSystemMessage(string message)
    {
        chatHistory.Add(new ChatMessage("", message, false, true));
        ScrollToBottom();
    }

    /// <summary>
    /// 滚动到底部
    /// </summary>
    private void ScrollToBottom()
    {
        chatScrollPos.y = float.MaxValue;
    }
}


