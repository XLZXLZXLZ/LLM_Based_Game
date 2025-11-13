using System;
using UnityEngine;

/// <summary>
/// 记忆事实 - 长期记忆的基本单元
/// </summary>
[Serializable]
public class MemoryFact
{
    /// <summary>
    /// 记忆内容（陈述句形式）
    /// </summary>
    public string content;

    /// <summary>
    /// 内容的向量嵌入
    /// </summary>
    public float[] embedding;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime createdTime;

    /// <summary>
    /// 记忆类型（如：承诺、偏好、关系、事实等）
    /// </summary>
    public string type;

    /// <summary>
    /// 重要度（0-1）
    /// </summary>
    public float importance;

    /// <summary>
    /// 唯一标识
    /// </summary>
    public string id;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MemoryFact(string content, float[] embedding, string type = "fact", float importance = 0.5f)
    {
        this.content = content;
        this.embedding = embedding;
        this.type = type;
        this.importance = importance;
        this.createdTime = DateTime.Now;
        this.id = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// 计算与另一个向量的余弦相似度
    /// </summary>
    public float CalculateSimilarity(float[] otherEmbedding)
    {
        if (embedding == null || otherEmbedding == null || embedding.Length != otherEmbedding.Length)
        {
            return 0f;
        }

        float dotProduct = 0f;
        float normA = 0f;
        float normB = 0f;

        for (int i = 0; i < embedding.Length; i++)
        {
            dotProduct += embedding[i] * otherEmbedding[i];
            normA += embedding[i] * embedding[i];
            normB += otherEmbedding[i] * otherEmbedding[i];
        }

        if (normA == 0f || normB == 0f)
        {
            return 0f;
        }

        return dotProduct / (Mathf.Sqrt(normA) * Mathf.Sqrt(normB));
    }

    public override string ToString()
    {
        return $"[{type}] {content} (重要度: {importance:F2})";
    }
}

