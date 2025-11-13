using UnityEditor;
using UnityEngine;

/// <summary>
/// ChatSystemTester 的自定义 Inspector
/// </summary>
[CustomEditor(typeof(ChatSystemTester))]
public class ChatSystemTesterEditor : Editor
{
    private ChatSystemTester tester;
    private GUIStyle headerStyle;
    private GUIStyle buttonStyle;
    private GUIStyle successButtonStyle;
    private GUIStyle dangerButtonStyle;
    private bool stylesInitialized = false;

    private void OnEnable()
    {
        tester = (ChatSystemTester)target;
    }

    private void InitializeStyles()
    {
        if (stylesInitialized) return;

        // 标题样式
        headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 14;
        headerStyle.normal.textColor = Color.cyan;

        // 普通按钮样式
        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 12;
        buttonStyle.fixedHeight = 30;

        // 成功按钮样式
        successButtonStyle = new GUIStyle(GUI.skin.button);
        successButtonStyle.fontSize = 12;
        successButtonStyle.fixedHeight = 30;
        successButtonStyle.normal.textColor = Color.green;
        successButtonStyle.fontStyle = FontStyle.Bold;

        // 危险按钮样式
        dangerButtonStyle = new GUIStyle(GUI.skin.button);
        dangerButtonStyle.fontSize = 12;
        dangerButtonStyle.fixedHeight = 30;
        dangerButtonStyle.normal.textColor = Color.red;
        dangerButtonStyle.fontStyle = FontStyle.Bold;

        stylesInitialized = true;
    }

    public override void OnInspectorGUI()
    {
        InitializeStyles();

        serializedObject.Update();

        // 标题
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("🎮 对话系统测试器", headerStyle);
        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "在编辑器中直接测试 NPC 对话系统\n" +
            "功能：对话测试、记忆查看、思考查看、强制记忆转化",
            MessageType.Info
        );
        EditorGUILayout.Space(10);

        // NPC配置
        DrawSection("📋 NPC 配置", () =>
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("npcProfile"), new GUIContent("NPC Profile"));
        });

        // 对话测试
        DrawSection("💬 对话测试", () =>
        {
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("userInput"),
                new GUIContent("输入消息"),
                GUILayout.Height(60)
            );

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            
            // 发送消息按钮
            if (GUILayout.Button("📤 发送消息", successButtonStyle, GUILayout.Height(40)))
            {
                tester.SendMessage();
            }

            // 刷新显示按钮
            if (GUILayout.Button("🔄 刷新", buttonStyle, GUILayout.Height(40), GUILayout.Width(80)))
            {
                tester.UpdateDisplays();
            }

            EditorGUILayout.EndHorizontal();
        });

        // 对话显示
        DrawSection("💭 对话历史", () =>
        {
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("conversationDisplay"),
                new GUIContent(""),
                GUILayout.Height(200)
            );

            if (GUILayout.Button("📜 显示完整历史", buttonStyle))
            {
                tester.ShowConversationHistory();
            }
        });

        // 思考内容
        DrawSection("🧠 当前思考", () =>
        {
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("thoughtDisplay"),
                new GUIContent(""),
                GUILayout.Height(120)
            );

            if (GUILayout.Button("💭 强制重新思考", buttonStyle))
            {
                tester.ForceThinking();
            }
        });

        // 记忆信息
        DrawSection("📚 记忆系统", () =>
        {
            EditorGUILayout.LabelField("短期记忆（对话总结）:", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("shortTermMemoryDisplay"),
                new GUIContent(""),
                GUILayout.Height(80)
            );

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("长期记忆（RAG知识库）:", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("longTermMemoryDisplay"),
                new GUIContent(""),
                GUILayout.Height(150)
            );

            EditorGUILayout.Space(5);

            if (GUILayout.Button("🔥 强制记忆转化", successButtonStyle))
            {
                tester.ForceMemoryExtraction();
            }
        });

        // 统计信息
        DrawSection("📊 统计信息", () =>
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("对话次数:", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("messageCount"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("长期记忆数:", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("longTermMemoryCount"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 状态消息
            var statusProp = serializedObject.FindProperty("statusMessage");
            EditorGUILayout.HelpBox(statusProp.stringValue, MessageType.None);
        });

        // 操作按钮
        DrawSection("🛠️ 操作", () =>
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("📤 导出完整状态", buttonStyle))
            {
                tester.ExportFullState();
            }

            if (GUILayout.Button("🗑️ 清除对话", dangerButtonStyle))
            {
                if (EditorUtility.DisplayDialog("确认", "确定要清除对话历史吗？", "确定", "取消"))
                {
                    tester.ClearConversationHistory();
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (GUILayout.Button("💥 清除所有记忆", dangerButtonStyle))
            {
                if (EditorUtility.DisplayDialog("确认", "确定要清除所有记忆（包括对话、短期记忆、长期记忆、思考）吗？", "确定", "取消"))
                {
                    tester.ClearAllMemory();
                }
            }
        });

        serializedObject.ApplyModifiedProperties();

        // 自动刷新
        if (GUI.changed)
        {
            EditorUtility.SetDirty(tester);
        }
    }

    private void DrawSection(string title, System.Action content)
    {
        EditorGUILayout.Space(10);
        
        // 绘制带背景的标题
        Rect rect = EditorGUILayout.GetControlRect(false, 25);
        EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 0.3f));
        EditorGUI.LabelField(rect, title, EditorStyles.boldLabel);

        EditorGUILayout.Space(5);

        // 内容区域
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        content?.Invoke();
        EditorGUILayout.EndVertical();
    }
}




