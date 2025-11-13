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
        public string role;    // "system", "user", "assistant"
        public string content;

        public Message(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
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
    /// 发送单条消息（重载方法）
    /// </summary>
    /// <param name="userMessage">用户消息内容</param>
    /// <param name="onSuccess">成功回调</param>
    /// <param name="onError">错误回调</param>
    /// <param name="systemPrompt">系统提示词（可选）</param>
    /// <param name="profile">配置文件（可选，使用默认配置）</param>
    public void SendMessage(string userMessage, Action<string> onSuccess, Action<string> onError = null, string systemPrompt = null, LLMProfile profile = null)
    {
        List<Message> messages = new List<Message>
        {
            new Message("user", userMessage)
        };
        SendMessageWithContext(messages, onSuccess, onError, systemPrompt, profile);
    }

    /// <summary>
    /// 发送带上下文的消息
    /// </summary>
    /// <param name="messages">消息列表（包含历史对话）</param>
    /// <param name="onSuccess">成功回调，返回助手的回复</param>
    /// <param name="onError">错误回调</param>
    /// <param name="systemPrompt">系统提示词（可选，如果为null且messages中没有system角色，则不添加系统提示）</param>
    /// <param name="profile">配置文件（可选，使用默认配置）</param>
    public void SendMessageWithContext(List<Message> messages, Action<string> onSuccess, Action<string> onError = null, string systemPrompt = null, LLMProfile profile = null)
    {
        StartCoroutine(SendChatRequest(messages, onSuccess, onError, systemPrompt, profile));
    }

    /// <summary>
    /// 发送聊天请求的协程
    /// </summary>
    private IEnumerator SendChatRequest(List<Message> messages, Action<string> onSuccess, Action<string> onError, string systemPrompt, LLMProfile profile)
    {
        // 使用指定的profile或默认profile
        LLMProfile activeProfile = profile ?? defaultProfile;
        
        if (activeProfile == null)
        {
            string error = "LLMProfile未设置！请在LLMManager中指定defaultProfile或传入profile参数。";
            Debug.LogError(error);
            onError?.Invoke(error);
            yield break;
        }

        // 如果消息列表中没有系统提示，且提供了systemPrompt参数，则添加系统提示
        List<Message> finalMessages = new List<Message>();
        bool hasSystemMessage = false;
        foreach (var msg in messages)
        {
            if (msg.role == "system")
            {
                hasSystemMessage = true;
                break;
            }
        }

        if (!hasSystemMessage && !string.IsNullOrEmpty(systemPrompt))
        {
            finalMessages.Add(new Message("system", systemPrompt));
        }
        finalMessages.AddRange(messages);

        // 构建请求体
        ChatRequest requestBody = new ChatRequest
        {
            model = activeProfile.model,
            messages = finalMessages,
            temperature = activeProfile.temperature,
            max_tokens = activeProfile.maxTokens,
            top_p = activeProfile.topP,
            frequency_penalty = activeProfile.frequencyPenalty,
            presence_penalty = activeProfile.presencePenalty
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
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string responseText = request.downloadHandler.text;
                    ChatResponse response = JsonUtility.FromJson<ChatResponse>(responseText);

                    if (response.choices != null && response.choices.Count > 0)
                    {
                        string assistantMessage = response.choices[0].message.content;
                        Debug.Log($"LLM响应成功: {assistantMessage}");
                        Debug.Log($"Token使用: Prompt={response.usage.prompt_tokens}, Completion={response.usage.completion_tokens}, Total={response.usage.total_tokens}");
                        onSuccess?.Invoke(assistantMessage);
                    }
                    else
                    {
                        string error = "API返回的响应中没有choices数据";
                        Debug.LogError(error);
                        onError?.Invoke(error);
                    }
                }
                catch (Exception e)
                {
                    string error = $"解析响应失败: {e.Message}\n响应内容: {request.downloadHandler.text}";
                    Debug.LogError(error);
                    onError?.Invoke(error);
                }
            }
            else
            {
                string errorMessage = $"请求失败: {request.error}";
                
                // 尝试解析错误响应
                try
                {
                    if (!string.IsNullOrEmpty(request.downloadHandler.text))
                    {
                        ErrorResponse errorResponse = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text);
                        if (errorResponse.error != null)
                        {
                            errorMessage = $"API错误: {errorResponse.error.message} (类型: {errorResponse.error.type})";
                        }
                    }
                }
                catch
                {
                    // 如果解析失败，使用原始错误信息
                }

                Debug.LogError(errorMessage);
                onError?.Invoke(errorMessage);
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
}


