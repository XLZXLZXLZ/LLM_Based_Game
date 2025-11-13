using UnityEngine;

/// <summary>
/// LLM配置文件，存储API调用的参数
/// </summary>
[CreateAssetMenu(fileName = "LLMProfile", menuName = "ScriptableObjects/LLMProfile", order = 1)]
public class LLMProfile : ScriptableObject
{
    [Header("模型配置")]
    [Tooltip("使用的模型名称")]
    public string model = "gpt-3.5-turbo";

    [Header("生成参数")]
    [Tooltip("温度参数，控制输出的随机性 (0-2)")]
    [Range(0f, 2f)]
    public float temperature = 0.7f;

    [Tooltip("最大生成token数")]
    public int maxTokens = 2000;

    [Tooltip("核采样参数 (0-1)")]
    [Range(0f, 1f)]
    public float topP = 1f;

    [Tooltip("频率惩罚 (-2.0 到 2.0)")]
    [Range(-2f, 2f)]
    public float frequencyPenalty = 0f;

    [Tooltip("存在惩罚 (-2.0 到 2.0)")]
    [Range(-2f, 2f)]
    public float presencePenalty = 0f;

    [Header("请求配置")]
    [Tooltip("请求超时时间（秒）")]
    public int requestTimeout = 30;
}

