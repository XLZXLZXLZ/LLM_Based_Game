# NPC对话系统框架 API 文档

> **版本**: 1.0  
> **更新日期**: 2025-11-11  
> **框架位置**: `Assets/C#Scripts/LLM/Chat/`

---

## 📖 目录

1. [架构总览](#架构总览)
2. [核心类说明](#核心类说明)
3. [使用指南](#使用指南)
4. [API 参考](#api-参考)
5. [最佳实践](#最佳实践)

---

## 架构总览

### 设计理念

本框架实现了一个**基于LLM的NPC对话系统**，具备**三层记忆架构**（瞬时记忆、短期记忆、长期记忆），支持智能上下文管理和记忆提取。

### 系统架构图

```
┌─────────────────────────────────────────────────────────┐
│                    外部调用层                              │
│                  (游戏逻辑/UI系统)                         │
└────────────────────┬────────────────────────────────────┘
                     │
         ┌───────────▼───────────┐
         │    ChatAgent          │  ← 对话流程编排（核心入口）
         │   (Singleton)         │
         └───────────┬───────────┘
                     │
         ┌───────────▼───────────────────────────────┐
         │  ConversationManager (Singleton)          │  ← 记忆数据管理
         │  ┌─────────────────────────────────────┐  │
         │  │  NPCMemory (per NPC)                │  │
         │  │  ├─ 瞬时记忆 (Instant Memory)       │  │
         │  │  ├─ 短期记忆 (Short-Term Memory)    │  │
         │  │  └─ 长期记忆 (Long-Term Memory)     │  │
         │  └─────────────────────────────────────┘  │
         └───────────┬───────────────────────────────┘
                     │
         ┌───────────┴───────────┐
         │                       │
    ┌────▼──────┐         ┌─────▼──────────┐
    │ LLMManager│         │ MemoryExtractor│  ← 记忆提取器
    │(Singleton)│         │  (Singleton)   │
    └───────────┘         └────────────────┘
         │
    ┌────▼────────────┐
    │  LLM API        │  ← ChatAnywhere API (对话 + 向量嵌入)
    │  - Chat         │
    │  - Embedding    │
    └─────────────────┘
```

### 三层记忆系统

| 记忆层级 | 类型 | 存储内容 | 生命周期 | 用途 |
|---------|------|---------|---------|------|
| **瞬时记忆** | `List<Message>` | 最近的对话记录 | 对话进行中 | 提供即时对话上下文 |
| **短期记忆** | `string` | 旧对话的总结文本 | 会话持续期间 | 压缩历史信息，节省Token |
| **长期记忆** | `List<MemoryFact>` | RAG知识库（承诺、偏好、关系等） | 永久保存 | 提供跨会话的角色记忆 |

---

## 核心类说明

### 🎯 ChatAgent (对话流程编排)

**职责**: 作为对话系统的**统一入口**，负责编排完整的对话流程。

**核心功能**:
- ✅ 编排对话流程（检索记忆 → 构建提示词 → 调用LLM → 保存记录）
- ✅ 自动管理上下文溢出（触发总结和长期记忆提取）
- ✅ 动态构建系统提示词（整合角色信息、短期记忆、长期记忆）
- ✅ 提供记忆清理的便捷接口（瞬时/全部记忆）

**不负责**:
- ❌ 直接管理记忆数据（交给 `ConversationManager`）
- ❌ 提供记忆查询接口（外部应直接访问 `ConversationManager`）

---

### 📦 ConversationManager (记忆数据管理)

**职责**: 作为**记忆数据中心**，管理所有NPC的三层记忆。

**核心功能**:
- ✅ 存储和管理所有NPC的记忆实例（`Dictionary<npcId, NPCMemory>`）
- ✅ 提供记忆的增删改查接口（瞬时、短期、长期）
- ✅ 自动裁剪瞬时记忆（当达到 `MaxHistoryCount` 时）
- ✅ 支持记忆检索（基于向量相似度的Top-K检索）

**使用场景**:
- 查询NPC的对话历史
- 直接访问/修改NPC记忆
- 实现记忆持久化（保存/加载）

---

### 🧠 MemoryExtractor (记忆提取器)

**职责**: 从对话中智能提取**需要长期记忆的关键信息**。

**核心功能**:
- ✅ 使用LLM分析对话，识别关键信息（承诺、偏好、关系等）
- ✅ 自动分类记忆类型（promise/preference/relationship/fact/detail）
- ✅ 评估记忆重要度（0.0-1.0）
- ✅ 生成向量嵌入并存储到长期记忆

**工作流程**:
```
对话记录 → LLM分析 → 提取关键信息 → 向量嵌入 → 存入长期记忆
```

---

### 🌐 LLMManager (LLM接口)

**职责**: 封装对外部LLM API的调用。

**核心功能**:
- ✅ 对话补全（支持单条消息和带上下文的对话）
- ✅ 文本向量嵌入（用于长期记忆检索）
- ✅ 错误处理和异步回调

**支持的API**:
- `SendMessage()` - 单条消息（无上下文）
- `SendMessageWithContext()` - 带对话历史
- `GetEmbedding()` - 获取文本向量

---

### 📄 数据类

#### NPCProfile (ScriptableObject)

存储NPC的角色信息：
- `npcId`: 唯一标识符（用于记忆管理）
- `characterName`: 角色名称
- `background`: 角色背景
- `personality`: 性格特征
- `speakingStyle`: 对话风格
- `goals`: 角色目标
- `llmProfile`: 关联的LLM配置

#### NPCMemory

每个NPC的记忆容器：
- `instantMemory`: 瞬时记忆（对话记录列表）
- `shortTermMemory`: 短期记忆（总结文本）
- `longTermMemory`: 长期记忆（`LongTermMemory` 实例）

#### MemoryFact

长期记忆的单个事实：
- `id`: 唯一标识符
- `content`: 记忆内容
- `embedding`: 向量嵌入（float[]）
- `type`: 类型（promise/preference/relationship/fact/detail）
- `importance`: 重要度（0.0-1.0）
- `createdTime`: 创建时间

#### LongTermMemory

长期记忆管理器：
- `facts`: 记忆事实列表
- 支持相似度检索、批量添加、类型过滤等

---

## 使用指南

### 快速开始

#### 1. 创建 NPCProfile

在Unity编辑器中：
```
右键 → Create → ScriptableObject → NPCProfile
```

配置角色信息：
```csharp
npcId = "npc_merchant_01";
characterName = "铁匠老张";
background = "在村口开了三十年铁匠铺的老师傅...";
personality = "热情豪爽，略显固执...";
speakingStyle = "说话带有浓重的地方口音，喜欢用俗语...";
llmProfile = <引用你创建的 LLMProfile>
```

#### 2. 发起对话

```csharp
using UnityEngine;

public class DialogueTest : MonoBehaviour
{
    public NPCProfile merchantProfile;

    void Start()
    {
        // 发送消息给NPC
        ChatAgent.Instance.SendMessage(
            npcProfile: merchantProfile,
            userMessage: "你好，有什么武器出售吗？",
            onSuccess: response =>
            {
                Debug.Log($"NPC回复: {response}");
                // 在UI上显示对话...
            },
            onError: error =>
            {
                Debug.LogError($"对话失败: {error}");
            }
        );
    }
}
```

#### 3. 查询记忆

```csharp
// 获取对话历史
var history = ConversationManager.Instance.GetConversationHistory(merchantProfile.npcId);
Debug.Log($"共有 {history.Count} 条对话记录");

// 获取短期记忆（总结）
string summary = ConversationManager.Instance.GetShortTermMemory(merchantProfile.npcId);
Debug.Log($"对话总结: {summary}");

// 获取长期记忆
var longTermMemory = ConversationManager.Instance.GetLongTermMemory(merchantProfile.npcId);
Debug.Log($"共有 {longTermMemory.FactCount} 条长期记忆");
```

#### 4. 清除记忆

```csharp
// 清除瞬时记忆（对话历史）
ChatAgent.Instance.ClearHistory(merchantProfile);

// 清除所有记忆（瞬时+短期+长期）
ChatAgent.Instance.ClearAllMemory(merchantProfile);
```

---

## API 参考

### ChatAgent

#### 核心方法

##### `SendMessage()`
发送消息给NPC并获取回复（自动管理上下文和记忆）

```csharp
public void SendMessage(
    NPCProfile npcProfile,     // NPC配置
    string userMessage,        // 用户输入
    Action<string> onSuccess,  // 成功回调（返回NPC回复）
    Action<string> onError = null  // 错误回调
)
```

**示例**:
```csharp
ChatAgent.Instance.SendMessage(
    npcProfile: myNPC,
    userMessage: "你记得我上次说的话吗？",
    onSuccess: reply => Debug.Log(reply)
);
```

---

##### `ClearHistory()`
清除NPC的瞬时记忆（对话历史）

```csharp
public void ClearHistory(NPCProfile npcProfile)
```

---

##### `ClearAllMemory()`
清除NPC的所有记忆（瞬时+短期+长期）

```csharp
public void ClearAllMemory(NPCProfile npcProfile)
```

---

#### 配置属性

##### `EnableMemorySystem`
是否启用记忆系统（包括短期记忆、长期记忆和记忆总结）

```csharp
public bool EnableMemorySystem { get; set; }
```

**默认值**: `true`

**示例**:
```csharp
// 禁用记忆系统（适合简单对话或测试）
ChatAgent.Instance.EnableMemorySystem = false;
```

---

##### `LongTermMemoryTopK`
长期记忆检索数量（构建系统提示词时检索多少条相关记忆）

```csharp
public int LongTermMemoryTopK { get; set; }
```

**取值范围**: 1-20  
**默认值**: 5

---

##### `LogSystemPrompt`
是否在控制台打印系统提示词（用于调试）

```csharp
public bool LogSystemPrompt { get; set; }
```

---

### ConversationManager

#### 瞬时记忆管理

##### `GetConversationHistory()`
获取NPC的对话历史

```csharp
public List<LLMManager.Message> GetConversationHistory(string npcId)
```

**返回**: 消息列表（`role` + `content`）

---

##### `AddMessage()`
添加一条消息到NPC的对话历史

```csharp
public void AddMessage(string npcId, string role, string content)
```

**参数**:
- `role`: "user" 或 "assistant"
- `content`: 消息内容

---

##### `ClearConversationHistory()`
清除NPC的对话历史

```csharp
public void ClearConversationHistory(string npcId)
```

---

##### `GetMessageCount()`
获取NPC的对话消息数量

```csharp
public int GetMessageCount(string npcId)
```

---

#### 短期记忆管理

##### `GetShortTermMemory()`
获取NPC的短期记忆（对话总结）

```csharp
public string GetShortTermMemory(string npcId)
```

---

##### `SetShortTermMemory()`
设置NPC的短期记忆

```csharp
public void SetShortTermMemory(string npcId, string summary)
```

---

##### `AppendShortTermMemory()`
追加内容到NPC的短期记忆

```csharp
public void AppendShortTermMemory(string npcId, string additionalSummary)
```

---

##### `ClearShortTermMemory()`
清除NPC的短期记忆

```csharp
public void ClearShortTermMemory(string npcId)
```

---

#### 长期记忆管理

##### `GetLongTermMemory()`
获取NPC的长期记忆实例

```csharp
public LongTermMemory GetLongTermMemory(string npcId)
```

---

##### `AddMemoryFact()`
添加记忆事实到长期记忆

```csharp
public void AddMemoryFact(string npcId, MemoryFact fact)
```

---

##### `AddMemoryFacts()`
批量添加记忆事实

```csharp
public void AddMemoryFacts(string npcId, List<MemoryFact> facts)
```

---

##### `RetrieveRelevantMemories()`
检索相关记忆（基于向量相似度）

```csharp
public List<MemoryFact> RetrieveRelevantMemories(
    string npcId,
    float[] queryEmbedding,
    int topK = 5,
    float? threshold = null  // 相似度阈值（可选）
)
```

**示例**:
```csharp
// 获取用户输入的向量嵌入
LLMManager.Instance.GetEmbedding(
    text: "我答应过你什么？",
    onSuccess: embedding =>
    {
        // 检索相关记忆
        var memories = ConversationManager.Instance.RetrieveRelevantMemories(
            npcId: "npc_001",
            queryEmbedding: embedding,
            topK: 3,
            threshold: 0.7f  // 只返回相似度 >= 0.7 的记忆
        );

        foreach (var memory in memories)
        {
            Debug.Log($"[{memory.type}] {memory.content}");
        }
    }
);
```

---

##### `GetAllMemoryFacts()`
获取所有长期记忆事实

```csharp
public List<MemoryFact> GetAllMemoryFacts(string npcId)
```

---

##### `ClearLongTermMemory()`
清空长期记忆

```csharp
public void ClearLongTermMemory(string npcId)
```

---

##### `GetLongTermMemoryCount()`
获取长期记忆数量

```csharp
public int GetLongTermMemoryCount(string npcId)
```

---

#### 全局管理

##### `ClearAllMemory()`
清除NPC的所有记忆（瞬时+短期+长期）

```csharp
public void ClearAllMemory(string npcId)
```

---

##### `ClearAllMemories()`
清除所有NPC的记忆

```csharp
public void ClearAllMemories()
```

---

##### `HasMemory()`
检查是否存在某个NPC的记忆

```csharp
public bool HasMemory(string npcId)
```

---

#### 配置属性

##### `MaxHistoryCount`
每个NPC保留的最大对话条数（0表示无限制）

```csharp
public int MaxHistoryCount { get; set; }
```

**默认值**: 20

**说明**: 当瞬时记忆超过此限制时，`ChatAgent` 会自动触发总结和长期记忆提取。

---

##### `NPCCount`
当前管理的NPC数量（只读）

```csharp
public int NPCCount { get; }
```

---

##### `LogMemoryOperations`
是否记录内存操作日志（用于调试）

```csharp
public bool LogMemoryOperations { get; set; }
```

---

### MemoryExtractor

#### `ExtractMemories()`
从对话中提取长期记忆

```csharp
public void ExtractMemories(
    NPCProfile npcProfile,
    List<LLMManager.Message> messages,
    Action<List<MemoryFact>> onComplete  // 完成回调
)
```

**说明**: 此方法通常由 `ChatAgent` 自动调用，外部不需要手动调用。

**工作流程**:
1. 使用LLM分析对话内容
2. 提取关键信息（格式：`[类型|重要度] 内容`）
3. 为每条信息生成向量嵌入
4. 返回 `MemoryFact` 列表

---

### LLMManager

#### 对话接口

##### `SendMessage()`
发送单条消息（无上下文）

```csharp
public void SendMessage(
    string userMessage,
    Action<string> onSuccess,
    Action<string> onError = null,
    string systemPrompt = null,
    LLMProfile profile = null
)
```

---

##### `SendMessageWithContext()`
发送带上下文的消息

```csharp
public void SendMessageWithContext(
    List<Message> messages,  // 完整的对话历史
    Action<string> onSuccess,
    Action<string> onError = null,
    string systemPrompt = null,
    LLMProfile profile = null
)
```

---

#### 向量嵌入接口

##### `GetEmbedding()`
获取文本的向量嵌入

```csharp
public void GetEmbedding(
    string text,
    Action<float[]> onSuccess,  // 返回向量数组
    Action<string> onError = null
)
```

**示例**:
```csharp
LLMManager.Instance.GetEmbedding(
    text: "玩家答应帮助我找回失物",
    onSuccess: embedding =>
    {
        Debug.Log($"向量维度: {embedding.Length}");
    }
);
```

---

#### 配置属性

##### `DefaultProfile`
默认的LLM配置（如果未指定profile则使用此配置）

```csharp
public LLMProfile DefaultProfile { get; set; }
```

---

### LongTermMemory

#### `AddFact()`
添加新的记忆事实（自动检测并覆盖相似记忆）

```csharp
public void AddFact(MemoryFact fact)
```

**行为**: 如果检测到相似度 >= 0.95 的记忆，会用新记忆覆盖旧记忆。

---

#### `AddFacts()`
批量添加记忆事实

```csharp
public void AddFacts(List<MemoryFact> newFacts)
```

---

#### `RetrieveRelevantMemories()`
检索相关记忆

```csharp
public List<MemoryFact> RetrieveRelevantMemories(
    float[] queryEmbedding,
    int topK = 5,
    float? threshold = null
)
```

**返回**: 按相似度降序排序的记忆列表（最多 `topK` 条）

---

#### `GetAllFacts()`
获取所有记忆事实

```csharp
public List<MemoryFact> GetAllFacts()
```

---

#### `ClearAllFacts()`
清空所有记忆事实

```csharp
public void ClearAllFacts()
```

---

#### `CleanLowImportanceMemories()`
清理重要度低于指定阈值的记忆

```csharp
public void CleanLowImportanceMemories(float minImportance)
```

**示例**:
```csharp
// 清理重要度低于 0.3 的记忆
longTermMemory.CleanLowImportanceMemories(0.3f);
```

---

#### `GetFactsByType()`
获取指定类型的记忆

```csharp
public List<MemoryFact> GetFactsByType(string type)
```

**支持的类型**:
- `"promise"` - 承诺或约定
- `"preference"` - 喜好、厌恶、偏好
- `"relationship"` - 角色之间的关系变化
- `"fact"` - 重要的事实信息、决定
- `"detail"` - 人类会记住的小细节

**示例**:
```csharp
// 获取所有承诺类记忆
var promises = longTermMemory.GetFactsByType("promise");
foreach (var promise in promises)
{
    Debug.Log($"承诺: {promise.content}");
}
```

---

### MemoryFact

#### `CalculateSimilarity()`
计算与另一个向量的余弦相似度

```csharp
public float CalculateSimilarity(float[] otherEmbedding)
```

**返回**: 0.0-1.0 之间的相似度值（1.0 表示完全相同）

---

## 最佳实践

### 1. 记忆系统使用建议

#### 何时禁用记忆系统？
```csharp
// 场景1: 简单的一次性对话（如商店交易确认）
ChatAgent.Instance.EnableMemorySystem = false;
ChatAgent.Instance.SendMessage(npc, "确定购买这把剑吗？", ...);

// 场景2: 性能测试或调试
ChatAgent.Instance.EnableMemorySystem = false;
```

#### 何时启用记忆系统？
```csharp
// 场景1: 主线剧情NPC（需要记住玩家的承诺和选择）
ChatAgent.Instance.EnableMemorySystem = true;

// 场景2: 陪伴型NPC（需要记住玩家的喜好和过往对话）
ChatAgent.Instance.EnableMemorySystem = true;
```

---

### 2. 性能优化

#### 控制上下文长度
```csharp
// 设置合理的最大历史记录数量（减少Token消耗）
ConversationManager.Instance.MaxHistoryCount = 10;
```

#### 定期清理低重要度记忆
```csharp
// 每隔一段时间清理不重要的记忆
var memory = ConversationManager.Instance.GetLongTermMemory(npcId);
memory.CleanLowImportanceMemories(0.4f);
```

---

### 3. 记忆持久化

框架目前不包含持久化功能，但可以轻松扩展：

```csharp
// 保存记忆到本地
public void SaveMemory(string npcId)
{
    var memory = ConversationManager.Instance.GetOrCreateMemory(npcId);
    
    var data = new SaveData
    {
        instantMemory = memory.GetConversationHistory(),
        shortTermMemory = memory.GetShortTermMemory(),
        longTermMemory = memory.longTermMemory.GetAllFacts()
    };
    
    string json = JsonUtility.ToJson(data);
    File.WriteAllText($"save_{npcId}.json", json);
}

// 加载记忆
public void LoadMemory(string npcId)
{
    string json = File.ReadAllText($"save_{npcId}.json");
    var data = JsonUtility.FromJson<SaveData>(json);
    
    // 恢复瞬时记忆
    foreach (var msg in data.instantMemory)
    {
        ConversationManager.Instance.AddMessage(npcId, msg.role, msg.content);
    }
    
    // 恢复短期记忆
    ConversationManager.Instance.SetShortTermMemory(npcId, data.shortTermMemory);
    
    // 恢复长期记忆
    ConversationManager.Instance.AddMemoryFacts(npcId, data.longTermMemory);
}
```

---

### 4. 调试技巧

#### 查看系统提示词
```csharp
// 启用系统提示词日志
ChatAgent.Instance.LogSystemPrompt = true;
```

#### 查看记忆操作
```csharp
// 启用记忆操作日志
ConversationManager.Instance.LogMemoryOperations = true;
```

#### 手动触发记忆提取
```csharp
// 手动提取长期记忆（通常由ChatAgent自动触发）
var messages = ConversationManager.Instance.GetConversationHistory(npcId);
MemoryExtractor.Instance.ExtractMemories(npcProfile, messages, facts =>
{
    Debug.Log($"提取了 {facts.Count} 条记忆");
    ConversationManager.Instance.AddMemoryFacts(npcId, facts);
});
```

---

### 5. 常见问题

#### Q: 对话没有上下文怎么办？
**A**: 检查 `NPCProfile.npcId` 是否设置，相同的 `npcId` 才能共享对话历史。

#### Q: 记忆总结不够准确？
**A**: 可以调整 `ConversationManager.MaxHistoryCount`，增大此值会延迟总结触发，保留更多原始对话。

#### Q: 长期记忆检索不到相关内容？
**A**: 
1. 检查 `ChatAgent.LongTermMemoryTopK` 是否足够大
2. 检查是否已经提取了长期记忆（需要先触发上下文溢出）
3. 尝试降低相似度阈值

#### Q: 如何实现"遗忘"功能？
**A**: 
```csharp
// 方案1: 清理低重要度记忆
memory.CleanLowImportanceMemories(0.5f);

// 方案2: 清除整个长期记忆
ConversationManager.Instance.ClearLongTermMemory(npcId);

// 方案3: 只清除特定类型的记忆
var facts = memory.GetFactsByType("promise");
foreach (var fact in facts)
{
    memory.facts.Remove(fact);
}
```

---

## 版本历史

### v1.0 (2025-11-11)
- ✅ 实现三层记忆架构
- ✅ 支持自动上下文管理和记忆总结
- ✅ 支持基于RAG的长期记忆检索
- ✅ 优化API职责分离（ChatAgent vs ConversationManager）
- ✅ 添加记忆系统开关（`EnableMemorySystem`）

---

## 贡献与反馈

如有问题或建议，请联系开发团队。

**Happy Coding!** 🎮✨




