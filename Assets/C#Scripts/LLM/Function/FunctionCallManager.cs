using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 函数调用管理器，负责管理和执行所有 LLM 可调用的游戏函数
/// </summary>
public class FunctionCallManager : Singleton<FunctionCallManager>
{
    // 存储所有注册的函数：函数名 -> GameFunction实例
    private Dictionary<string, GameFunction> registeredFunctions = new Dictionary<string, GameFunction>();

    protected override void Awake()
    {
        base.Awake();
        InitializeFunctions();
    }

    /// <summary>
    /// 初始化并注册所有可用函数
    /// </summary>
    private void InitializeFunctions()
    {
        // 后续可以在这里添加更多函数
        Debug.Log($"[FunctionCallManager] 已初始化 {registeredFunctions.Count} 个游戏函数");
    }

    /// <summary>
    /// 注册单个函数
    /// </summary>
    public void RegisterFunction(GameFunction function)
    {
        if (function == null) return;

        if (registeredFunctions.ContainsKey(function.Name))
        {
            Debug.LogWarning($"[FunctionCallManager] 函数 {function.Name} 已存在，将被覆盖");
        }
        
        registeredFunctions[function.Name] = function;
    }

    /// <summary>
    /// 获取所有可用工具的定义（用于发送给 LLM）
    /// </summary>
    public List<LLMManager.Tool> GetTools()
    {
        List<LLMManager.Tool> tools = new List<LLMManager.Tool>();
        foreach (var func in registeredFunctions.Values)
        {
            tools.Add(func.ToTool());
        }
        return tools;
    }

    /// <summary>
    /// 执行工具调用
    /// </summary>
    /// <param name="toolCall">LLM 返回的工具调用信息</param>
    /// <returns>执行结果字符串</returns>
    public string ExecuteToolCall(LLMManager.ToolCall toolCall)
    {
        if (toolCall == null || toolCall.function == null)
        {
            return "错误：无效的工具调用请求";
        }

        string functionName = toolCall.function.name;
        string argumentsJson = toolCall.function.arguments;

        if (!registeredFunctions.ContainsKey(functionName))
        {
            string error = $"错误：未找到名为 '{functionName}' 的函数";
            Debug.LogError($"[FunctionCallManager] {error}");
            return error;
        }

        GameFunction gameFunction = registeredFunctions[functionName];
        
        // 解析参数
        Dictionary<string, object> args = null;
        try
        {
            args = ParseArguments(argumentsJson);
        }
        catch (Exception e)
        {
            string error = $"参数解析失败: {e.Message}";
            Debug.LogError($"[FunctionCallManager] {error}, JSON: {argumentsJson}");
            return error;
        }

        Debug.Log($"[FunctionCallManager] 正在执行函数: {functionName}...");
        
        try
        {
            // 执行函数
            string result = gameFunction.Execute(args);
            return result;
        }
        catch (Exception e)
        {
            string error = $"函数执行出错: {e.Message}";
            Debug.LogError($"[FunctionCallManager] {error}");
            return error;
        }
    }

    /// <summary>
    /// 简单的 JSON 参数解析器
    /// </summary>
    private Dictionary<string, object> ParseArguments(string json)
    {
        // 为了兼容性，使用自定义的简易解析器，不依赖外部库 (Newtonsoft)
        // 注意：这是一个非常简化的实现，只支持扁平结构的 JSON 对象
        return MiniJsonParser(json);
    }

    /// <summary>
    /// 简陋的 JSON 解析器，仅支持一层深度的对象 (如 {"key":"value", "count":123})
    /// 不支持嵌套对象或数组
    /// </summary>
    private Dictionary<string, object> MiniJsonParser(string json)
    {
        var dict = new Dictionary<string, object>();
        if (string.IsNullOrEmpty(json)) return dict;

        json = json.Trim();
        if (!json.StartsWith("{") || !json.EndsWith("}")) return dict;

        // 去掉首尾大括号
        json = json.Substring(1, json.Length - 2);

        // 简单的分割逻辑（不支持嵌套对象，遇到带逗号的字符串会出错）
        // 更好的做法是引入 SimpleJSON 或 Newtonsoft.Json
        // 这里为了保持零依赖，采用字符遍历法
        
        bool inQuote = false;
        int start = 0;
        List<string> pairs = new List<string>();

        for (int i = 0; i < json.Length; i++)
        {
            if (json[i] == '"' && (i == 0 || json[i-1] != '\\')) inQuote = !inQuote;
            
            if (json[i] == ',' && !inQuote)
            {
                pairs.Add(json.Substring(start, i - start));
                start = i + 1;
            }
        }
        pairs.Add(json.Substring(start));

        foreach (var pair in pairs)
        {
            var kv = SplitPair(pair);
            if (kv.Key != null)
            {
                dict[kv.Key] = kv.Value;
            }
        }
        return dict;
    }

    private KeyValuePair<string, object> SplitPair(string pair)
    {
        int colonIndex = pair.IndexOf(':');
        if (colonIndex == -1) return new KeyValuePair<string, object>(null, null);

        string key = pair.Substring(0, colonIndex).Trim().Trim('"');
        string valueStr = pair.Substring(colonIndex + 1).Trim();
        
        object value = valueStr;

        // 尝试解析值类型
        if (valueStr.StartsWith("\"") && valueStr.EndsWith("\""))
        {
            value = valueStr.Substring(1, valueStr.Length - 2);
        }
        else if (int.TryParse(valueStr, out int intVal))
        {
            value = intVal;
        }
        else if (float.TryParse(valueStr, out float floatVal))
        {
            value = floatVal;
        }
        else if (bool.TryParse(valueStr, out bool boolVal))
        {
            value = boolVal;
        }

        return new KeyValuePair<string, object>(key, value);
    }
}
