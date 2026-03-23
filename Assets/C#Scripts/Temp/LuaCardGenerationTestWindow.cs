#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Lua 卡牌生成测试窗口（编辑器态可直接发请求并落盘）。
/// </summary>
public class LuaCardGenerationTestWindow : EditorWindow
{
    private string userInput = "请生成一张偏防御、寂静侧的卡牌。";
    private string model = "gpt-4o-mini";
    private float temperature = 0.7f;
    private int maxTokens = 0;

    private bool isGenerating;
    private string status = "待命";
    private Vector2 scroll;

    [MenuItem("Tools/LLM/Lua Card Generator")]
    public static void Open()
    {
        var win = GetWindow<LuaCardGenerationTestWindow>("Lua Card Generator");
        win.minSize = new Vector2(560, 420);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Lua 卡牌生成测试", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        model = EditorGUILayout.TextField("模型", model);
        temperature = EditorGUILayout.Slider("Temperature", temperature, 0f, 2f);
        maxTokens = EditorGUILayout.IntField("Max Tokens (<=0 不限)", maxTokens);

        EditorGUILayout.LabelField("用户需求输入");
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(180));
        userInput = EditorGUILayout.TextArea(userInput, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(isGenerating))
        {
            if (GUILayout.Button("生成并保存 Lua 卡牌", GUILayout.Height(32)))
            {
                _ = GenerateAsync();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(status, MessageType.Info);
        EditorGUILayout.LabelField("输出目录", "Assets/LuaCards/<model>/generated_*.lua");
        EditorGUILayout.LabelField("索引文件", "Assets/LuaCards/lua_card_index.json");
    }

    private async System.Threading.Tasks.Task GenerateAsync()
    {
        if (string.IsNullOrWhiteSpace(userInput))
        {
            status = "请输入用户需求。";
            Repaint();
            return;
        }

        isGenerating = true;
        status = "请求中...";
        Repaint();

        try
        {
            var options = new LuaCardGenerationService.GenerateOptions
            {
                Model = model,
                Temperature = temperature,
                MaxTokens = maxTokens,
                // 提示词内容先走框架；你可直接在 LuaCardPromptTemplate 里填完整版本
                SystemPrompt = LuaCardPromptTemplate.SystemPromptFramework
            };

            LuaCardGenerationService.LuaCardRecord record =
                await LuaCardGenerationService.GenerateAndSaveAsync(userInput, options);

            AssetDatabase.Refresh();
            status = $"生成成功\n模型: {record.model}\n文件: {record.filePath}\n记录ID: {record.id}";
        }
        catch (Exception ex)
        {
            status = $"生成失败: {ex.Message}";
            Debug.LogError($"[LuaCardGenerationTestWindow] {ex}");
        }
        finally
        {
            isGenerating = false;
            Repaint();
        }
    }
}
#endif

