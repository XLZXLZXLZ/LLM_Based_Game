using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// LLM管理器，负责调用ChatAnywhere API
/// </summary>
public class LLMManager : Singleton<LLMManager>
{
    [Header("配置文件")]
    [SerializeField]
    private LLMProfile defaultProfile;

    /// <summary>
    /// 消息结构
    /// </summary>
    [Serializable]
    public class Message
    {
        public string role;       // "system", "user", "assistant", "tool"
        public string content;
        
        // Tool Call 相关字段
        public List<ToolCall> tool_calls;  // LLM 返回的函数调用
        public string tool_call_id;        // 回复函数结果时使用
        public string name;                // 函数名

        public Message() { }

        public Message(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }

    /// <summary>
    /// Tool Call 结构
    /// </summary>
    [Serializable]
    public class ToolCall
    {
        public string id;
        public string type;
        public FunctionCall function;
    }

    /// <summary>
    /// Function Call 结构
    /// </summary>
    [Serializable]
    public class FunctionCall
    {
        public string name;
        public string arguments;  // JSON 字符串
    }

    /// <summary>
    /// Tool 定义
    /// </summary>
    [Serializable]
    public class Tool
    {
        public string type = "function";
        public FunctionDefinition function;
    }

    /// <summary>
    /// 函数定义
    /// </summary>
    [Serializable]
    public class FunctionDefinition
    {
        public string name;
        public string description;
        public object parameters;  // JSON 对象
    }

    /// <summary>
    /// API请求体
    /// </summary>
    [Serializable]
    private class ChatRequest
    {
        public string model;
        public List<Message> messages;
        public float temperature;
        public int max_tokens;
        public float top_p;
        public float frequency_penalty;
        public float presence_penalty;
        
        // Tool Call 相关
        public List<Tool> tools;
        public string tool_choice;
    }

    /// <summary>
    /// API响应体
    /// </summary>
    [Serializable]
    private class ChatResponse
    {
        public string id;
        public string @object;
        public long created;
        public string model;
        public List<Choice> choices;
        public Usage usage;
    }

    [Serializable]
    private class Choice
    {
        public int index;
        public Message message;
        public string finish_reason;
    }

    [Serializable]
    private class Usage
    {
        public int prompt_tokens;
        public int completion_tokens;
        public int total_tokens;
    }

    /// <summary>
    /// API错误响应
    /// </summary>
    [Serializable]
    private class ErrorResponse
    {
        public Error error;
    }

    [Serializable]
    private class Error
    {
        public string message;
        public string type;
        public string code;
    }

    /// <summary>
    /// 发送单条消息
    /// </summary>
    public void SendMessage(
        string userMessage, 
        Action<string, List<ToolCall>> onResponse, 
        Action<string> onError = null, 
        List<Tool> tools = null,
        string toolChoice = "auto",
        string systemPrompt = null, 
        LLMProfile profile = null)
    {
        List<Message> messages = new List<Message> { new Message("user", userMessage) };
        SendMessage(messages, onResponse, onError, tools, toolChoice, systemPrompt, profile);
    }

    /// <summary>
    /// 发送消息（支持上下文和 Tool Call）
    /// </summary>
    public void SendMessage(
        List<Message> messages, 
        Action<string, List<ToolCall>> onResponse, 
        Action<string> onError = null,
        List<Tool> tools = null,
        string toolChoice = "auto",
        string systemPrompt = null, 
        LLMProfile profile = null)
    {
        // 如果提供了 systemPrompt，直接插入到开头
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            messages.Insert(0, new Message("system", systemPrompt));
        }
        
        StartCoroutine(SendChatRequest(messages, tools, toolChoice, onResponse, onError, profile));
    }

    /// <summary>
    /// 发送聊天请求的协程
    /// </summary>
    private IEnumerator SendChatRequest(
        List<Message> messages, 
        List<Tool> tools,
        string toolChoice,
        Action<string, List<ToolCall>> onResponse, 
        Action<string> onError, 
        LLMProfile profile)
    {
        LLMProfile activeProfile = profile ?? defaultProfile;
        
        if (activeProfile == null)
        {
            string error = "LLMProfile未设置！";
            Debug.LogError(error);
            onError?.Invoke(error);
            yield break;
        }

        // 构建请求体
        ChatRequest requestBody = new ChatRequest
        {
            model = activeProfile.model,
            messages = messages,
            temperature = activeProfile.temperature,
            max_tokens = activeProfile.maxTokens,
            top_p = activeProfile.topP,
            frequency_penalty = activeProfile.frequencyPenalty,
            presence_penalty = activeProfile.presencePenalty,
            tools = tools,
            tool_choice = (tools != null && tools.Count > 0) ? toolChoice : null
        };

        string jsonData = JsonUtility.ToJson(requestBody);
        byte[] postData = Encoding.UTF8.GetBytes(jsonData);

        // 创建Web请求
        using (UnityWebRequest request = new UnityWebRequest(Consts.ChatApiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(postData);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {Consts.DefaultApiKey}");
            request.timeout = activeProfile.requestTimeout;

            // 发送请求
            yield return request.SendWebRequest();

            // 处理响应
            if (request.result != UnityWebRequest.Result.Success)
            {
                HandleError(request, onError);
                yield break;
            }

            try
            {
                ChatResponse response = JsonUtility.FromJson<ChatResponse>(request.downloadHandler.text);
                
                if (response.choices == null || response.choices.Count == 0)
                {
                    HandleError("API返回的响应中没有choices数据", onError);
                    yield break;
                }

                var choice = response.choices[0];
                string content = choice.message.content;
                List<ToolCall> toolCalls = choice.message.tool_calls;
                
                Debug.Log($"Token: prompt_tokens {response.usage.prompt_tokens}+completion_tokens {response.usage.completion_tokens}=total_tokens {response.usage.total_tokens}");
                
                // 判断返回类型
                if (choice.finish_reason == "tool_calls" && toolCalls != null && toolCalls.Count > 0)
                {
                    Debug.Log($"LLM 请求调用 {toolCalls.Count} 个函数");
                    foreach (var call in toolCalls)
                    {
                        Debug.Log($"  - 函数: {call.function.name}, 参数: {call.function.arguments}");
                    }
                    onResponse?.Invoke(content, toolCalls);
                }
                else
                {
                    Debug.Log($"LLM 文本回复: {content}");
                    onResponse?.Invoke(content, null);
                }
            }
            catch (Exception e)
            {
                HandleError($"解析失败: {e.Message}", onError);
            }
        }
    }

    /// <summary>
    /// 默认配置文件
    /// </summary>
    public LLMProfile DefaultProfile
    {
        get => defaultProfile;
        set => defaultProfile = value;
    }

    /// <summary>
    /// 获取文本的向量嵌入
    /// </summary>
    /// <param name="text">要嵌入的文本</param>
    /// <param name="onSuccess">成功回调，返回向量数组</param>
    /// <param name="onError">错误回调</param>
    public void GetEmbedding(string text, Action<float[]> onSuccess, Action<string> onError = null)
    {
        StartCoroutine(GetEmbeddingRequest(text, onSuccess, onError));
    }

    /// <summary>
    /// 获取向量嵌入的协程
    /// </summary>
    private IEnumerator GetEmbeddingRequest(string text, Action<float[]> onSuccess, Action<string> onError)
    {
        // 构建请求体
        var requestBody = new EmbeddingRequest
        {
            input = text,
            model = Consts.EmbeddingModel
        };

        string jsonData = JsonUtility.ToJson(requestBody);
        byte[] postData = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(Consts.EmbeddingApiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(postData);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {Consts.DefaultApiKey}");
            request.timeout = 30;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string responseText = request.downloadHandler.text;
                    // 简单解析（实际需要根据API响应格式调整）
                    var response = JsonUtility.FromJson<EmbeddingResponse>(responseText);
                    
                    if (response.data != null && response.data.Length > 0)
                    {
                        Debug.Log($"[LLMManager] 向量嵌入成功，维度: {response.data[0].embedding.Length}");
                        onSuccess?.Invoke(response.data[0].embedding);
                    }
                    else
                    {
                        string error = "向量嵌入响应中没有数据";
                        Debug.LogError(error);
                        onError?.Invoke(error);
                    }
                }
                catch (Exception e)
                {
                    string error = $"解析向量嵌入响应失败: {e.Message}";
                    Debug.LogError(error);
                    onError?.Invoke(error);
                }
            }
            else
            {
                string errorMessage = $"向量嵌入请求失败: {request.error}";
                Debug.LogError(errorMessage);
                onError?.Invoke(errorMessage);
            }
        }
    }

    [Serializable]
    private class EmbeddingRequest
    {
        public string input;
        public string model;
    }

    [Serializable]
    private class EmbeddingResponse
    {
        public EmbeddingData[] data;
    }

    [Serializable]
    private class EmbeddingData
    {
        public float[] embedding;
    }

    /// <summary>
    /// 错误处理辅助方法
    /// </summary>
    private void HandleError(UnityWebRequest request, Action<string> onError)
    {
        string errorMessage = $"请求失败: {request.error}";
        
        // 尝试解析 API 错误信息
        try
        {
            if (!string.IsNullOrEmpty(request.downloadHandler.text))
            {
                ErrorResponse errorResponse = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text);
                if (errorResponse?.error != null)
                {
                    errorMessage = $"API错误: {errorResponse.error.message}";
                }
            }
        }
        catch { }
        
        Debug.LogError(errorMessage);
        onError?.Invoke(errorMessage);
    }

    private void HandleError(string errorMessage, Action<string> onError)
    {
        Debug.LogError(errorMessage);
        onError?.Invoke(errorMessage);
    }
}


