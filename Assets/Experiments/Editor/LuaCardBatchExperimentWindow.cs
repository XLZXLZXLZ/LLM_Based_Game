#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 批量实验窗口：按测试集对多个模型逐条生成 Lua 卡牌并记录结果。
/// </summary>
public class LuaCardBatchExperimentWindow : EditorWindow
{
    [Serializable]
    private class PromptItem
    {
        public int index;
        public string prompt;
    }

    [Serializable]
    private class PromptResult
    {
        public int promptIndex;
        public string prompt;
        public bool rejected;
        public string outputPath;
        public string message;
        public string createdAtUtc;
    }

    [Serializable]
    private class ModelExperimentResult
    {
        public string model;
        public List<PromptResult> results = new List<PromptResult>();
    }

    [Serializable]
    private class ExperimentResultFile
    {
        public string generatedAtUtc;
        public string promptDatasetFile;
        public List<string> models = new List<string>();
        public int promptCount;
        public List<ModelExperimentResult> modelResults = new List<ModelExperimentResult>();
    }

    private const string PromptFileRelativePath = "Assets/Experiments/测试集.md";
    private const string ResultFolderRelativePath = "Assets/Experiments/Results";

    private string modelsInput = "claude-opus-4-6\ngemini-3-pro-preview\ngpt-4o-mini";
    private float temperature = 0.7f;
    private int maxTokens = 0;
    private bool isRunning;
    private Vector2 scroll;
    private string status = "待命";
    private string lastResultFile = "";

    [MenuItem("Tools/LLM/Lua Batch Experiment")]
    public static void Open()
    {
        var win = GetWindow<LuaCardBatchExperimentWindow>("Lua Batch Experiment");
        win.minSize = new Vector2(620, 460);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Lua 卡牌批量实验", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("测试集文件", PromptFileRelativePath);
        EditorGUILayout.LabelField("结果目录", ResultFolderRelativePath);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("模型列表（每行一个）");
        modelsInput = EditorGUILayout.TextArea(modelsInput, GUILayout.MinHeight(72));
        temperature = EditorGUILayout.Slider("Temperature", temperature, 0f, 2f);
        maxTokens = EditorGUILayout.IntField("Max Tokens (<=0 不限)", maxTokens);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(isRunning))
        {
            if (GUILayout.Button("运行 3 模型 x 20 提示词实验", GUILayout.Height(34)))
            {
                _ = RunExperimentAsync();
            }
        }

        EditorGUILayout.Space();
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.HelpBox(status, MessageType.Info);
        if (!string.IsNullOrWhiteSpace(lastResultFile))
        {
            EditorGUILayout.LabelField("最新结果文件", lastResultFile);
        }
        EditorGUILayout.EndScrollView();
    }

    private async Task RunExperimentAsync()
    {
        var models = ParseModels(modelsInput);
        if (models.Count == 0)
        {
            status = "模型列表为空，请至少填写一个模型。";
            Repaint();
            return;
        }

        string promptFileFullPath = GetAbsolutePathFromAssetsRelative(PromptFileRelativePath);
        if (!File.Exists(promptFileFullPath))
        {
            status = $"找不到测试集文件：{PromptFileRelativePath}";
            Repaint();
            return;
        }

        List<PromptItem> prompts;
        try
        {
            prompts = LoadPrompts(promptFileFullPath);
        }
        catch (Exception ex)
        {
            status = $"读取测试集失败：{ex.Message}";
            Repaint();
            return;
        }

        if (prompts.Count == 0)
        {
            status = "测试集中没有解析到提示词（请检查编号格式，如 1. xxx）。";
            Repaint();
            return;
        }

        isRunning = true;
        status = $"开始运行：{models.Count} 个模型 x {prompts.Count} 条提示词";
        Repaint();

        var resultFile = new ExperimentResultFile
        {
            generatedAtUtc = DateTime.UtcNow.ToString("o"),
            promptDatasetFile = PromptFileRelativePath,
            promptCount = prompts.Count
        };
        resultFile.models.AddRange(models);

        try
        {
            for (int modelIndex = 0; modelIndex < models.Count; modelIndex++)
            {
                string model = models[modelIndex];
                var modelResult = new ModelExperimentResult { model = model };
                resultFile.modelResults.Add(modelResult);

                for (int i = 0; i < prompts.Count; i++)
                {
                    PromptItem p = prompts[i];
                    status = $"[{modelIndex + 1}/{models.Count}] {model} | 提示词 {i + 1}/{prompts.Count}";
                    Repaint();

                    var pr = new PromptResult
                    {
                        promptIndex = p.index,
                        prompt = p.prompt,
                        rejected = false,
                        outputPath = "",
                        message = "",
                        createdAtUtc = DateTime.UtcNow.ToString("o")
                    };

                    try
                    {
                        var options = new LuaCardGenerationService.GenerateOptions
                        {
                            Model = model,
                            Temperature = temperature,
                            MaxTokens = maxTokens,
                            SystemPrompt = LuaCardPromptTemplate.SystemPromptFramework
                        };

                        LuaCardGenerationService.LuaCardRecord record =
                            await LuaCardGenerationService.GenerateAndSaveAsync(p.prompt, options);

                        pr.outputPath = record.filePath;
                        pr.message = "ok";
                    }
                    catch (Exception ex)
                    {
                        bool isRejected = IsRejected(ex.Message);
                        pr.rejected = isRejected;
                        pr.message = ex.Message;
                    }

                    modelResult.results.Add(pr);
                    await Task.Delay(120);
                }
            }

            string savedPath = SaveResultFile(resultFile);
            lastResultFile = savedPath.Replace("\\", "/");
            status = $"实验完成，结果已保存：{lastResultFile}";
            AssetDatabase.Refresh();
        }
        catch (Exception ex)
        {
            status = $"实验中断：{ex.Message}";
            Debug.LogError($"[LuaCardBatchExperimentWindow] {ex}");
        }
        finally
        {
            isRunning = false;
            Repaint();
        }
    }

    private static List<string> ParseModels(string input)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(input)) return result;

        string[] lines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string raw in lines)
        {
            string model = raw.Trim();
            if (!string.IsNullOrWhiteSpace(model))
                result.Add(model);
        }
        return result;
    }

    private static List<PromptItem> LoadPrompts(string fullPath)
    {
        string text = File.ReadAllText(fullPath, Encoding.UTF8);
        string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var prompts = new List<PromptItem>();
        var regex = new Regex(@"^\s*\d+\.\s*(.+?)\s*$");

        int index = 1;
        foreach (string line in lines)
        {
            Match m = regex.Match(line);
            if (!m.Success) continue;

            string prompt = m.Groups[1].Value.Trim();
            if (string.IsNullOrWhiteSpace(prompt)) continue;

            prompts.Add(new PromptItem
            {
                index = index++,
                prompt = prompt
            });
        }

        return prompts;
    }

    private static bool IsRejected(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return false;
        string msg = message.ToLowerInvariant();

        return msg.Contains(LuaCardPromptTemplate.UnsupportedErrorIdentifier.ToLowerInvariant())
            || msg.Contains("unsupported")
            || msg.Contains("refuse")
            || msg.Contains("rejected")
            || msg.Contains("safety")
            || msg.Contains("policy");
    }

    private static string SaveResultFile(ExperimentResultFile result)
    {
        string folder = GetAbsolutePathFromAssetsRelative(ResultFolderRelativePath);
        Directory.CreateDirectory(folder);

        string fileName = $"lua_batch_experiment_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
        string fullPath = Path.Combine(folder, fileName);

        string json = JsonUtility.ToJson(result, true);
        File.WriteAllText(fullPath, json, new UTF8Encoding(false));
        return fullPath;
    }

    private static string GetAbsolutePathFromAssetsRelative(string assetsRelativePath)
    {
        string root = Directory.GetParent(Application.dataPath)!.FullName.Replace("\\", "/");
        return Path.Combine(root, assetsRelativePath).Replace("\\", "/");
    }
}
#endif
