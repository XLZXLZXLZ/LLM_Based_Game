using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Lua 卡牌生成服务：
/// 1) 根据用户输入请求 LLM 生成 Lua
/// 2) 按模型写入 Assets/LuaCards/{model}/
/// 3) 维护全局索引 JSON（Assets/LuaCards/lua_card_index.json）
/// </summary>
public static class LuaCardGenerationService
{
    [Serializable]
    private class Message
    {
        public string role;
        public string content;

        public Message(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }

    [Serializable]
    private class ChatRequest
    {
        public string model;
        public List<Message> messages;
        public float temperature;
        public int max_tokens;
    }

    [Serializable]
    private class ChatRequestNoLimit
    {
        public string model;
        public List<Message> messages;
        public float temperature;
    }

    [Serializable]
    private class ChatResponse
    {
        public List<Choice> choices;
    }

    [Serializable]
    private class Choice
    {
        public ResponseMessage message;
    }

    [Serializable]
    private class ResponseMessage
    {
        public string content;
    }

    [Serializable]
    private class ErrorResponse
    {
        public ErrorBody error;
    }

    [Serializable]
    private class ErrorBody
    {
        public string message;
    }

    [Serializable]
    public class LuaCardIndex
    {
        public List<LuaCardRecord> records = new List<LuaCardRecord>();
    }

    [Serializable]
    public class LuaCardRecord
    {
        public string id;
        public string model;
        public string userInput;
        public string filePath;
        public string createdAtUtc;
    }

    public class GenerateOptions
    {
        public string Model = "gpt-4o-mini";
        public float Temperature = 0.7f;
        /// <summary>
        /// <= 0 表示不传 max_tokens，由模型默认上限决定。
        /// </summary>
        public int MaxTokens = 0;
        public string SystemPrompt = null;
    }

    public static async Task<LuaCardRecord> GenerateAndSaveAsync(string userInput, GenerateOptions options = null)
    {
        if (string.IsNullOrWhiteSpace(userInput))
            throw new ArgumentException("用户输入不能为空。", nameof(userInput));

        options ??= new GenerateOptions();
        string model = string.IsNullOrWhiteSpace(options.Model) ? "gpt-4o-mini" : options.Model.Trim();
        string systemPrompt = string.IsNullOrWhiteSpace(options.SystemPrompt)
            ? LuaCardPromptTemplate.SystemPromptFramework
            : options.SystemPrompt;

        string userPrompt = LuaCardPromptTemplate.BuildUserPrompt(userInput);
        string luaText = await RequestLuaScriptAsync(model, systemPrompt, userPrompt, options.Temperature, options.MaxTokens);
        ThrowIfContainsUnsupportedMarker(luaText);
        string cleanedLua = ExtractLuaCode(luaText);
        ThrowIfContainsUnsupportedMarker(cleanedLua);

        string filePath = SaveLuaToModelFolder(cleanedLua, model);
        LuaCardRecord record = AppendIndex(model, userInput, filePath);
        return record;
    }

    public static LuaCardIndex LoadIndex()
    {
        string indexPath = GetIndexPath();
        if (!File.Exists(indexPath))
            return new LuaCardIndex();

        string json = File.ReadAllText(indexPath, Encoding.UTF8);
        if (string.IsNullOrWhiteSpace(json))
            return new LuaCardIndex();

        var loaded = JsonUtility.FromJson<LuaCardIndex>(json);
        return loaded ?? new LuaCardIndex();
    }

    private static async Task<string> RequestLuaScriptAsync(string model, string systemPrompt, string userPrompt, float temperature, int maxTokens)
    {
        List<Message> messages = new List<Message>
        {
            new Message("system", systemPrompt),
            new Message("user", userPrompt)
        };

        string json;
        if (maxTokens > 0)
        {
            var payload = new ChatRequest
            {
                model = model,
                messages = messages,
                temperature = temperature,
                max_tokens = maxTokens
            };
            json = JsonUtility.ToJson(payload);
        }
        else
        {
            var payload = new ChatRequestNoLimit
            {
                model = model,
                messages = messages,
                temperature = temperature
            };
            json = JsonUtility.ToJson(payload);
        }
        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, Consts.ChatApiUrl);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Consts.DefaultApiKey);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await client.SendAsync(request);
        string responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            string message = $"请求失败: {(int)response.StatusCode} {response.ReasonPhrase}";
            try
            {
                var err = JsonUtility.FromJson<ErrorResponse>(responseText);
                if (err != null && err.error != null && !string.IsNullOrEmpty(err.error.message))
                    message = $"API错误: {err.error.message}";
            }
            catch
            {
                // ignore parse error
            }
            throw new Exception(message);
        }

        var parsed = JsonUtility.FromJson<ChatResponse>(responseText);
        if (parsed?.choices == null || parsed.choices.Count == 0 || parsed.choices[0].message == null)
            throw new Exception("响应解析失败：未找到 choices[0].message.content。");

        return parsed.choices[0].message.content ?? "";
    }

    private static string ExtractLuaCode(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "-- empty response";

        var match = Regex.Match(text, "```lua\\s*(.*?)\\s*```", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (match.Success)
            return match.Groups[1].Value.Trim() + Environment.NewLine;

        match = Regex.Match(text, "```\\s*(.*?)\\s*```", RegexOptions.Singleline);
        if (match.Success)
            return match.Groups[1].Value.Trim() + Environment.NewLine;

        return text.Trim() + Environment.NewLine;
    }

    private static string SaveLuaToModelFolder(string luaContent, string model)
    {
        string assetsRoot = Application.dataPath;
        string luaRoot = Path.Combine(assetsRoot, "LuaCards");
        string modelFolder = SanitizeFolderName(model);
        string modelPath = Path.Combine(luaRoot, modelFolder);
        Directory.CreateDirectory(modelPath);

        string fileName = $"generated_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.lua";
        string fullPath = Path.Combine(modelPath, fileName);
        File.WriteAllText(fullPath, luaContent, new UTF8Encoding(false));
        return fullPath.Replace("\\", "/");
    }

    private static LuaCardRecord AppendIndex(string model, string userInput, string filePath)
    {
        LuaCardIndex index = LoadIndex();
        var record = new LuaCardRecord
        {
            id = Guid.NewGuid().ToString("N"),
            model = model,
            userInput = userInput,
            filePath = filePath,
            createdAtUtc = DateTime.UtcNow.ToString("o")
        };
        index.records.Add(record);

        string indexJson = JsonUtility.ToJson(index, true);
        File.WriteAllText(GetIndexPath(), indexJson, new UTF8Encoding(false));
        return record;
    }

    private static string GetIndexPath()
    {
        return Path.Combine(Application.dataPath, "LuaCards", "lua_card_index.json");
    }

    private static string SanitizeFolderName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "unknown_model";

        string name = raw.Trim();
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c.ToString(), "_");

        name = name.Replace("/", "_").Replace("\\", "_").Replace(":", "_");
        return string.IsNullOrWhiteSpace(name) ? "unknown_model" : name;
    }

    private static void ThrowIfContainsUnsupportedMarker(string content)
    {
        if (string.IsNullOrEmpty(content))
            return;

        if (content.IndexOf(LuaCardPromptTemplate.UnsupportedErrorIdentifier, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            throw new InvalidOperationException(
                $"模型返回无法实现标识符：{LuaCardPromptTemplate.UnsupportedErrorIdentifier}");
        }
    }
}

